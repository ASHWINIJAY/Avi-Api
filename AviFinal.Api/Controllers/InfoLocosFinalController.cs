using AviFinal.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AviFinal.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InfoLocosFinalController : ControllerBase
    {
        private readonly AviDbContext _context;
        private readonly IWebHostEnvironment _env;

        public InfoLocosFinalController(AviDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET api/InfoLocosFinal/123
        [HttpGet("{locoNumber}")]
        public async Task<IActionResult> GetMasterLoco(int locoNumber)
        {
            var loco = await _context.MasterLocos
                .Where(m => m.LocoNumber == locoNumber)
                .Select(m => new
                {
                    InventoryNumber = m.InventoryNumber,
                    LocoType = m.LocoType,
                    NetBookValue = m.NetBookValue
                })
                .FirstOrDefaultAsync();

            if (loco == null) return NotFound();
            return Ok(loco);
        }

        // POST api/InfoLocosFinal/submit
        [HttpPost("submit")]
        public async Task<IActionResult> SubmitForm([FromForm] LocoFormModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            try
            {
                string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
                Directory.CreateDirectory(uploadsFolder);

                // ✅ Save Loco Photo safely
                string locoPhotoPath = null;
                if (model.LocoPhoto != null)
                {
                    locoPhotoPath = Path.Combine("uploads", Guid.NewGuid() + Path.GetExtension(model.LocoPhoto.FileName));
                    using var fs = new FileStream(Path.Combine(_env.WebRootPath, locoPhotoPath), FileMode.Create);
                    await model.LocoPhoto.CopyToAsync(fs);
                }

                // ✅ Save Body Photos safely
                List<string> bodyPhotoPaths = new();
                if (model.BodyPhotos != null && model.BodyPhotos.Count > 0)
                {
                    foreach (var file in model.BodyPhotos)
                    {
                        string path = Path.Combine("uploads", Guid.NewGuid() + Path.GetExtension(file.FileName));
                        using var fs = new FileStream(Path.Combine(_env.WebRootPath, path), FileMode.Create);
                        await file.CopyToAsync(fs);
                        bodyPhotoPaths.Add(path);
                    }
                }

                // ✅ Save Lifting Photos safely
                List<string> liftingPhotoPaths = new();
                if (model.LiftingPhotos != null && model.LiftingPhotos.Count > 0)
                {
                    foreach (var file in model.LiftingPhotos)
                    {
                        string path = Path.Combine("uploads", Guid.NewGuid() + Path.GetExtension(file.FileName));
                        using var fs = new FileStream(Path.Combine(_env.WebRootPath, path), FileMode.Create);
                        await file.CopyToAsync(fs);
                        liftingPhotoPaths.Add(path);
                    }
                }

                // ✅ Create InfoLocosFinal entity safely
                var info = new InfoLocosFinal
                {
                    LocoNumber = model.LocoNumTxt,
                    GpsLatitude = model.GpsLat ?? "",
                    GpsLongitude = model.GpsLong ?? "",
                    PhotoPath = locoPhotoPath ?? "",
                    ProMain = model.ProMainSelect ?? "",
                    FleetRenewPro = model.FleetRenewSelect ?? "",
                    BodyDamage = model.BodyDamageTxt ?? "No",
                    BodyPhotoPaths = bodyPhotoPaths.Count > 0 ? string.Join(";", bodyPhotoPaths) : null,
                    BodyRepairValue = string.IsNullOrWhiteSpace(model.BodyRepairVal) ? null : model.BodyRepairVal,
                    LiftingRequired = model.LiftingReqTxt ?? "No",
                    LiftingPhotoPaths = liftingPhotoPaths.Count > 0 ? string.Join(";", liftingPhotoPaths) : null,
                    LiftDate = model.LiftDateTxt.HasValue ? DateOnly.FromDateTime(model.LiftDateTxt.Value) : null,
                    InventoryNumber = model.InventoryNumTxt ?? "",
                    LocoType = model.LocoTypeTxt ?? "",
                    NetBookValue = string.IsNullOrWhiteSpace(model.NetBookVal) ? null : model.NetBookVal
                };

                try
                {
                    _context.InfoLocosFinals.Add(info);
                    await _context.SaveChangesAsync();
                    return Ok(new { message = "Form submitted successfully" });
                }
                catch (Exception ex)
                {
                    // Return exact error detail for debugging
                    return StatusCode(500, new { message = "Internal Server Error", detail = ex.Message });
                }
            }
            catch (Exception ex)
            {
                // Return exact error detail for debugging
                return StatusCode(500, new { message = "Internal Server Error", detail = ex.Message });
            }
        }
    }

    public class LocoFormModel
    {
        public int LocoNumTxt { get; set; }
        public string? GpsLat { get; set; }
        public string? GpsLong { get; set; }
        public IFormFile? LocoPhoto { get; set; }
        public string? ProMainSelect { get; set; }
        public string? FleetRenewSelect { get; set; }
        public string? BodyDamageTxt { get; set; } = "No";
        public List<IFormFile>? BodyPhotos { get; set; }
        public string? BodyRepairVal { get; set; }
        public string? LiftingReqTxt { get; set; } = "No";
        public List<IFormFile>? LiftingPhotos { get; set; }
        public DateTime? LiftDateTxt { get; set; }
        public string? InventoryNumTxt { get; set; }
        public string? LocoTypeTxt { get; set; }
        public string? NetBookVal { get; set; }
    }
}
