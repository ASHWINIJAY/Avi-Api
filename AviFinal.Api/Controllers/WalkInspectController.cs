using AviFinal.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AviFinal.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WalkInspectController : ControllerBase
    {
        private readonly AviDbContext _context;

        public WalkInspectController(AviDbContext context)
        {
            _context = context;
        }

        public class WalkAroundRowModel
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

        public class WalkAroundFormModel
        {
            public int LocoNumber { get; set; }
            public string UserID { get; set; } = null!;
            public string InspectFormID { get; set; } = null!;
            public List<WalkAroundRowModel> Rows { get; set; } = new();
        }

        [HttpGet("getParts/{locoClass}/{inspectFormId}")]
        public async Task<IActionResult> GetParts(string locoClass, string inspectFormId)
        {
            if (string.IsNullOrEmpty(locoClass) || string.IsNullOrEmpty(inspectFormId))
                return BadRequest("Loco class or Inspect Form ID is missing.");

            List<string> partDescriptions = new List<string>();

            try
            {
                // Example: Dynamically select table based on locoClass
                if (locoClass == "D34")
                {
                    /*partDescriptions = await _context.D34Parts
                        .Where(p => p.FormId == inspectFormId)
                        .Select(p => p.PartDescr)
                        .ToListAsync();*/
                }
                else if (locoClass == "D35")
                {
                    /*partDescriptions = await _context.D35Parts
                        .Where(p => p.FormId == inspectFormId)
                        .Select(p => p.PartDescr)
                        .ToListAsync();*/
                }
                else if (locoClass == "D36")
                {

                }
                else
                {
                    return NotFound("Class not supported.");
                }

                return Ok(partDescriptions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving parts: {ex.Message}");
            }
        }

        [HttpGet("getPartCost")]
        public async Task<IActionResult> GetPartCost(string locoClass, string partDescription, string field)
        {
            if (string.IsNullOrEmpty(locoClass) || string.IsNullOrEmpty(partDescription) || string.IsNullOrEmpty(field))
                return BadRequest("Invalid parameters.");

            try
            {
                string refurbishCost = "";

                if (locoClass == "D34")
                {
                    /*var part = await _context.D34Parts
                        .Where(p => p.PartDescr == partDescription)
                        .FirstOrDefaultAsync();

                    if (part != null)
                    {
                        refurbishCost = field == "Refurbish" ? part.RefurbishValue : "0.00";
                    }*/
                }
                else if (locoClass == "D35")
                {
                    /*var part = await _context.D35Parts
                        .Where(p => p.PartDescr == partDescription)
                        .FirstOrDefaultAsync();

                    if (part != null)
                    {
                        refurbishCost = field == "Refurbish" ? part.RefurbishValue : "0.00";
                    }*/
                }
                else
                {
                    return NotFound("Class not supported.");
                }

                return Ok(new { refurbishCost });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving part cost: {ex.Message}");
            }
        }

        [HttpPost("submit")]
        public async Task<IActionResult> SubmitForm([FromBody] WalkAroundFormModel model)
        {
            if (model.LocoNumber <= 0 || string.IsNullOrEmpty(model.UserID))
                return BadRequest("LocoNumber or UserID is invalid.");

            if (model.Rows == null || !model.Rows.Any())
                return BadRequest("No rows provided.");

            foreach (var row in model.Rows)
            {
                var entity = new WalkAroundInspect
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

                _context.WalkAroundInspects.Add(entity);
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
