using AviFinal.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AviFinal.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ComFanInspectController : ControllerBase
    {
        private readonly AviDbContext _context;

        public ComFanInspectController(AviDbContext context)
        {
            _context = context;
        }

        public class ComFanRowModel
        {
            public int Id { get; set; }
            public string PartDescr { get; set; } = null!;
            public string Good { get; set; } = "No";
            public string Refurbish { get; set; } = "No";
            public string Missing { get; set; } = "No";
            public string DamageReplaced { get; set; } = "No";
            public string ReplacementValue { get; set; } = null!;
            public string RefurbishValue { get; set; } = null!;
        }

        public class ComFanFormModel
        {
            public int LocoNumber { get; set; }
            public string UserID { get; set; } = null!;
            public string InspectFormID { get; set; } = null!;
            public List<ComFanRowModel> Rows { get; set; } = new();
        }

        [HttpPost("submit")]
        public async Task<IActionResult> SubmitForm([FromBody] ComFanFormModel model)
        {
            if (model.LocoNumber <= 0 || string.IsNullOrEmpty(model.UserID))
                return BadRequest("LocoNumber or UserID is invalid.");

            if (model.Rows == null || !model.Rows.Any())
                return BadRequest("No rows provided.");

            foreach (var row in model.Rows)
            {
                var entity = new ComFanInspect
                {
                    LocoNumber = model.LocoNumber,
                    UserId = model.UserID,
                    InspectFormId = model.InspectFormID,
                    PartDescr = row.PartDescr ?? "",
                    GoodCheck = row.Good ?? "No",
                    RefurbishCheck = row.Refurbish ?? "No",
                    MissingCheck = row.Missing ?? "No",
                    DamageCheck = row.DamageReplaced ?? "No",
                    ReplaceCost = row.ReplacementValue ?? "",
                    RefurbishCost = row.RefurbishValue ?? ""
                };

                _context.ComFanInspects.Add(entity);
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException dbEx)
            {
                // Log the exception
                return StatusCode(500, $"Database error: {dbEx.Message}");
            }

            return Ok(new { message = "Walk Around Inspect submitted successfully." });
        }
    }
}
