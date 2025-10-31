using AviFinal.Api.Models;
using Dapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AviAppFinal.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GE35RF001Controller : ControllerBase
    {
        private readonly AviDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<GE35RF001Controller> _logger;

        public GE35RF001Controller(AviDbContext context, IWebHostEnvironment env, ILogger<GE35RF001Controller> logger)
        {
            _context = context;
            _env = env;
            _logger = logger;
        }

        [HttpGet("getParts/{formID}")]
        public async Task<IActionResult> GetParts(string formID)
        {
            if (string.IsNullOrWhiteSpace(formID))
                return BadRequest("FormId is required.");

            try
            {
                var partsList = await _context.Ge35finalParts
                    .Where(p => p.FormId == formID)
                    .ToListAsync();

                if (partsList.Count == 0)
                    return BadRequest($"Unsupported formID: {formID}");

                // Step 2: Sort numerically after fetching (in memory)
                var orderedParts = partsList
                    .OrderBy(p =>
                    {
                        // Extract numeric part of PartId (e.g., "PRT12" → 12)
                        var numPart = new string(p.PartId?.Where(char.IsDigit).ToArray());
                        return int.TryParse(numPart, out int n) ? n : int.MaxValue;
                    })
                    .ToList();

                // Step 3: Project and return
                var result = orderedParts.Select(p => new
                {
                    PartID = p.PartId,
                    PartDescr = p.PartDescr
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetParts failed for {formId}", formID);
                return StatusCode(500, "Error retrieving parts.");
            }
        }

        [HttpGet("getPartCost")]
        public async Task<IActionResult> GetPartCost(string partId, string field)
        {
            if (string.IsNullOrWhiteSpace(partId) || string.IsNullOrWhiteSpace(field))
                return BadRequest("partId and field are required.");

            try
            {
                var part = await _context.Ge35finalParts
                    .FirstOrDefaultAsync(p => p.PartId == partId);

                if (part == null) return NotFound();

                string cost = field switch
                {
                    "Refurbish" => part.RefurbishValue,
                    "Missing" => part.MissingValue,
                    "Replace" => part.ReplaceValue,
                    _ => "0.00"
                };

                return Ok(cost);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetPartCost failed for {partId}", partId);
                return StatusCode(500, "Error getting part cost.");
            }
        }

        [HttpPost("UploadPhoto")]
        public async Task<IActionResult> UploadPhoto([FromForm] IFormFile file, [FromForm] string formId, [FromForm] string partId, [FromForm] string photoType, [FromForm] string locoNumber, [FromForm] string locoModel)
        {
            if (file == null || string.IsNullOrEmpty(photoType) || string.IsNullOrEmpty(partId))
                return BadRequest("Missing required parameters.");

            try
            {
                string baseFolder = Path.Combine(_env.WebRootPath, "GE35", formId.ToUpper());

                string subFolder = photoType.ToLower() switch
                {
                    "damage" => "DamagePhotos",
                    "missing" => "MissingPhotos",
                    _ => "Other"
                };

                string fullFolderPath = Path.Combine(baseFolder, subFolder);

                // Ensure folder exists
                if (!Directory.Exists(fullFolderPath))
                    Directory.CreateDirectory(fullFolderPath);

                // Generate file name: LocoNumber_LocoModel_PhotoType_yyyyMMdd_HHmmss.ext
                string fileExtension = Path.GetExtension(file.FileName);
                string sanitizedLocoModel = locoModel.Replace(" ", "_"); // optional
                string sanitizedPhotoType = photoType.Equals("damage", StringComparison.OrdinalIgnoreCase) ? "Damage" : "Missing";
                string fileName = $"{locoNumber}_{sanitizedLocoModel}_{sanitizedPhotoType}_{DateTime.Now:yyyyMMdd_HHmmss}{fileExtension}";

                string fullPath = Path.Combine(fullFolderPath, fileName);

                // Save the file
                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Return relative path for front-end
                string relativePath = Path.Combine("GE35", formId.ToUpper(), subFolder, fileName).Replace("\\", "/");
                return Ok(new { path = relativePath });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Photo upload failed for part {PartId}", partId);
                return StatusCode(500, "Photo upload failed.");
            }
        }

        public class DeletePhotoRequest1
        {
            public string Path { get; set; } = string.Empty;
        }

        [HttpPost("DeletePhoto")]
        public IActionResult DeletePhoto([FromBody] DeletePhotoRequest1 request)
        {
            if (request == null || string.IsNullOrEmpty(request.Path))
                return BadRequest("Missing photo path.");

            try
            {
                // Construct absolute path from relative path
                string fullPath = Path.Combine(_env.WebRootPath, request.Path.Replace("/", Path.DirectorySeparatorChar.ToString()));

                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                    return Ok("Deleted");
                }

                // If file doesn't exist, still return OK since it's non-blocking
                return Ok("File not found, nothing to delete.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "DeletePhoto failed for path {Path}", request.Path);
                return StatusCode(500, "Delete failed.");
            }
        }

        public class GE35RF001InspectDto
        {
            public int LocoNumber { get; set; }
            public string LocoClass { get; set; } = null!;
            public string? LocoModel { get; set; }
            public string FormId { get; set; } = null!;
            public string PartId { get; set; } = null!;
            public string PartDescr { get; set; } = null!;
            public string GoodCheck { get; set; } = null!;
            public string RefurbishCheck { get; set; } = null!;
            public string MissingCheck { get; set; } = null!;
            public string DamageCheck { get; set; } = null!;
            public string? RefurbishValue { get; set; }
            public string? MissingValue { get; set; }
            public string? MissingPhoto { get; set; }
            public string? ReplaceValue { get; set; }
            public string? DamagePhoto { get; set; }
        }

        [HttpPost("SubmitInspection")]
        public async Task<IActionResult> SubmitInspection([FromBody] List<GE35RF001InspectDto> dtos)
        {
            if (dtos == null || !dtos.Any())
                return BadRequest("No data received.");

            try
            {
                var entities = dtos.Select(d => new Ge35rfinspect
                {
                    LocoNumber = d.LocoNumber,
                    LocoClass = d.LocoClass ?? "",
                    LocoModel = d.LocoModel ?? "",
                    FormId = d.FormId ?? "",
                    PartId = d.PartId ?? "",
                    PartDescr = d.PartDescr ?? "",
                    GoodCheck = d.GoodCheck ?? "No",
                    RefurbishCheck = d.RefurbishCheck ?? "No",
                    MissingCheck = d.MissingCheck ?? "No",
                    ReplaceCheck = d.DamageCheck ?? "No",
                    RefurbishValue = d.RefurbishValue,
                    MissingValue = d.MissingValue,
                    ReplaceValue = d.ReplaceValue,
                    MissingPhoto = d.MissingPhoto,
                    ReplacePhoto = d.DamagePhoto
                }).ToList();

                // Bulk insert
                await _context.Ge35rfinspects.AddRangeAsync(entities);
                await _context.SaveChangesAsync();

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SubmitInspection failed");
                return StatusCode(500, "Submit failed.");
            }
        }

    }
}