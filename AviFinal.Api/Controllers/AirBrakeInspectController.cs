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
    public class AirBrakeInspectController : ControllerBase
    {
        private readonly AviDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<AirBrakeInspectController> _logger;

        public AirBrakeInspectController(AviDbContext context, IWebHostEnvironment env, ILogger<AirBrakeInspectController> logger)
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
                var partsList = await _context.AirBrakeFinalParts
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
                var part = await _context.AirBrakeFinalParts
                    .FirstOrDefaultAsync(p => p.PartId == partId);

                if (part == null) return NotFound();

                string cost = "0.00"; //PLEASE ADD
                string laborValue = "0.00"; //PLEASE ADD

                //PLEASE ADD AND ADJUST
                switch (field)
                {
                    case "Refurbish":
                        cost = part.RefurbishValue ?? "0.00";
                        laborValue = part.LaborValue ?? "0.00";
                        break;

                    case "Missing":
                        cost = part.MissingValue ?? "0.00";
                        laborValue = part.LaborValue ?? "0.00";
                        break;

                    case "Replace":
                        cost = part.ReplaceValue ?? "0.00";
                        laborValue = part.LaborValue ?? "0.00";
                        break;

                    default:
                        break;
                }

                //PLEASE ADJUST
                return Ok(new
                {
                    cost,
                    laborValue
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetPartCost failed for {partId}", partId);
                return StatusCode(500, "Error getting part cost.");
            }
        }

        [HttpPost("UploadPhoto")]
        public async Task<IActionResult> UploadPhoto([FromForm] IFormFile file, [FromForm] string formId, [FromForm] string partId, [FromForm] string photoType, [FromForm] string wagonNumber, [FromForm] string wagonGroup)
        {
            if (file == null || string.IsNullOrEmpty(photoType) || string.IsNullOrEmpty(partId))
                return BadRequest("Missing required parameters.");

            try
            {
                string baseFolder = Path.Combine(_env.WebRootPath, "ABP", formId.ToUpper());

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
                string sanitizedWagonGroup = wagonGroup.Replace(" ", "_"); // optional
                string sanitizedPhotoType = photoType.Equals("damage", StringComparison.OrdinalIgnoreCase) ? "Damage" : "Missing";
                string fileName = $"{wagonNumber}_{sanitizedWagonGroup}_{sanitizedPhotoType}_{DateTime.Now:yyyyMMdd_HHmmss}{fileExtension}";

                string fullPath = Path.Combine(fullFolderPath, fileName);

                // Save the file
                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Return relative path for front-end
                string relativePath = Path.Combine("ABP", formId.ToUpper(), subFolder, fileName).Replace("\\", "/");
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

        public class AirBrakeInspectDto
        {
            public int WagonNumber { get; set; }
            public string WagonGroup { get; set; } = null!;
            public string? WagonType { get; set; }
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
            public string? LaborValue { get; set; } //PLEASE ADD
        }

        [HttpPost("SubmitInspection")]
        public async Task<IActionResult> SubmitInspection([FromBody] List<AirBrakeInspectDto> dtos)
        {
            if (dtos == null || !dtos.Any())
                return BadRequest("No data received.");

            try
            {
                var entities = dtos.Select(d => new AirBrakePartsInspect
                {
                    WagonNumber = d.WagonNumber,
                    WagonGroup = d.WagonGroup ?? "",
                    WagonType = d.WagonType ?? "",
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
                    ReplacePhoto = d.DamagePhoto,
                    LaborValue = d.LaborValue //PLEASE ADD
                }).ToList();

                // Bulk insert
                await _context.AirBrakePartsInspects.AddRangeAsync(entities);
                await _context.SaveChangesAsync();

                var wagonNumber = dtos.FirstOrDefault()?.WagonNumber;

                if (wagonNumber == null)
                {
                    return BadRequest("Wagon number missing.");
                }

                var brakeType = await _context.WagonInfoCaptures
               .Where(w => w.WagonNumber == wagonNumber)
               .Select(w => w.BrakeType)
               .FirstOrDefaultAsync();

                if (string.IsNullOrEmpty(brakeType))
                    return Ok(new { message = "Inspection saved, but Brake Type not found.", brakeType = (string?)null });

                // (Luca) Add
                var group = dtos.FirstOrDefault()?.WagonGroup;

                if (group == null)
                {
                    return BadRequest("Wagon group missing.");
                }

                // (Luca) Add
                var wagonData = await _context.WagonGroups
                    .Where(w => w.Group == group)
                    .Select(w => new
                    {
                        Doors = w.Doors ?? "N/A",
                        Twistlocks = w.Twistlocks ?? "N/A",
                        Stanchions = w.Stanchions ?? "N/A"
                    })
                    .FirstOrDefaultAsync() ?? new
                    {
                        Doors = "N/A",
                        Twistlocks = "N/A",
                        Stanchions = "N/A"
                    };

                var wagonDoors = wagonData.Doors;
                var wagonTwist = wagonData.Twistlocks;
                var wagonStan = wagonData.Stanchions;

                return Ok(new {
                    brakeType,
                    wagonDoors, 
                    wagonTwist, 
                    wagonStan, 
                   });
                }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SubmitInspection failed");
                return StatusCode(500, "Submit failed.");
            }
        }

    }
}