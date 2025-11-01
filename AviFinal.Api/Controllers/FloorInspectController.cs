using AviFinal.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AviAppFinal.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FloorInspectController : ControllerBase
    {
        private readonly AviDbContext _context;
        private readonly IWebHostEnvironment _env;

        public FloorInspectController(AviDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpGet("GetFloorPart")]
        public async Task<IActionResult> GetFloorPart(string formID)
        {
            var part = await _context.InternalFinalParts
               .Where(p => p.PartType == "Floor") // or however you filter
               .Select(p => new { p.PartDescr, p.PartType })
               .FirstOrDefaultAsync();

            if (part == null) return NotFound();
            return Ok(part);
        }

        [HttpGet("GetPartValue")]
        public async Task<IActionResult> GetPartValue(string partType, string field)
        {
            var part = await _context.InternalFinalParts.FirstOrDefaultAsync(p => p.PartType == partType);
            if (part == null) return NotFound();

            string value = field switch
            {
                "Refurbish" => part.RefurbishValue,
                "Missing" => part.MissingValue,
                "Replace" => part.ReplaceValue,
                _ => "0.00"
            };
            return Ok(new { value });
        }

        [HttpPost("SubmitInspection")]
        public async Task<IActionResult> SubmitInspection([FromBody] FloorInspect model)
        {
            _context.FloorInspects.Add(model);
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("UploadPhoto")]
        public async Task<IActionResult> UploadPhoto([FromForm] IFormFile file, [FromForm] string wagonNumber, [FromForm] string wagonGroup, [FromForm] string photoType)
        {
            if (file == null || file.Length == 0) return BadRequest("No file uploaded.");

            string folder = Path.Combine(_env.WebRootPath, "FLR", photoType == "Missing" ? "MissingPhotos" : "DamagePhotos");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string extension = Path.GetExtension(file.FileName);
            string fileName = $"{wagonNumber}_{wagonGroup}_{photoType}_{timestamp}{extension}";
            string fullPath = Path.Combine(folder, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            string relativePath = Path.Combine("FLR", photoType == "Missing" ? "MissingPhotos" : "DamagePhotos", fileName);
            return Ok(new { path = relativePath });
        }

        [HttpPost("DeletePhoto")]
        public IActionResult DeletePhoto([FromBody] string path)
        {
            try
            {
                string fullPath = Path.Combine(_env.WebRootPath, path);
                if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);
                return Ok();
            }
            catch { return BadRequest(); }
        }
    }
}
