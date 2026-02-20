using AviFinal.Api.Models;
using Dapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Drawing;

namespace AviFinal.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GM36InspectController : ControllerBase
    {
        private readonly AviDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<GE36InspectController> _logger;
        private readonly IConfiguration _config;

        public GM36InspectController(AviDbContext context, IWebHostEnvironment env, ILogger<GE36InspectController> logger, IConfiguration config)
        {
            _context = context;
            _env = env;
            _logger = logger;
            _config = config;
        }

        [HttpGet("getParts/{formID}")]
        public async Task<IActionResult> GetParts(string formID)
        {
            if (string.IsNullOrWhiteSpace(formID))
                return BadRequest("FormId is required.");

            try
            {
                var partsList = await _context.Gm36finalParts
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
                    PartId = p.PartId,
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
                return BadRequest("PartId and field are required.");

            try
            {
                var part = await _context.Gm36finalParts
                    .FirstOrDefaultAsync(p => p.PartId == partId);

                if (part == null) return NotFound();

                string cost = "0.00";
                string laborValue = "0.00";

                switch (field)
                {
                    case "Refurbish":
                        cost = part.RefurbishValue ?? "0.00";
                        laborValue = part.LabourValue + ".00" ?? "0.00";
                        break;

                    case "Missing":
                        cost = part.MissingValue ?? "0.00";
                        laborValue = part.LabourValue + ".00" ?? "0.00";
                        break;

                    case "Replace":
                        cost = part.ReplaceValue ?? "0.00";
                        laborValue = part.LabourValue + ".00" ?? "0.00";
                        break;

                    default:
                        break;
                }

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
        public async Task<IActionResult> UploadPhoto([FromForm] IFormFile file, [FromForm] string formId, [FromForm] string partId, [FromForm] string photoType, [FromForm] string locoNumber, [FromForm] string locoModel)
        {
            if (file == null || string.IsNullOrEmpty(photoType) || string.IsNullOrEmpty(partId))
                return BadRequest("Missing required parameters.");

            try
            {
                string baseFolder = Path.Combine(_env.WebRootPath, locoModel.ToUpper(), formId.ToUpper());

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
                string relativePath = Path.Combine(locoModel.ToUpper(), formId.ToUpper(), subFolder, fileName).Replace("\\", "/");
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

        public class InspectDto
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
            public string? LaborValue { get; set; }
        }

        private void MapInspection(dynamic entity, InspectDto d, int phase)
        {
            entity.LocoNumber = d.LocoNumber;
            entity.LocoClass = d.LocoClass ?? "";
            entity.LocoModel = d.LocoModel ?? "";
            entity.FormId = d.FormId ?? "";
            entity.PartId = d.PartId ?? "";
            entity.PartDescr = d.PartDescr ?? "";
            entity.GoodCheck = d.GoodCheck ?? "No";
            entity.RefurbishCheck = d.RefurbishCheck ?? "No";
            entity.MissingCheck = d.MissingCheck ?? "No";
            entity.ReplaceCheck = d.DamageCheck ?? "No";
            entity.RefurbishValue = d.RefurbishValue;
            entity.MissingValue = d.MissingValue;
            entity.ReplaceValue = d.ReplaceValue;
            entity.MissingPhoto = d.MissingPhoto;
            entity.ReplacePhoto = d.DamagePhoto;
            entity.LaborValue = d.LaborValue;
            entity.Phase = phase;
        }

        [HttpPost("SubmitInspection")]
        public async Task<IActionResult> SubmitInspection([FromBody] List<InspectDto> dtos)
        {
            if (dtos == null || !dtos.Any())
                return BadRequest("No data received.");
            using var con = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            await con.OpenAsync();
            var partIds = dtos.Select(d => d.PartId).ToList();
            var partCostData = await _context.Gm36finalParts
                .Where(p => partIds.Contains(p.PartId))
                .ToDictionaryAsync(p => p.PartId, p => new
                {
                    p.RefurbishValue,
                    p.MissingValue,
                    p.ReplaceValue,
                    p.LabourValue
                });
            foreach (var d in dtos)
            {
                if (partCostData.TryGetValue(d.PartId, out var cost))
                {
                    if (d.RefurbishCheck == "Yes")
                    {
                        d.RefurbishValue = cost.RefurbishValue ?? "0.00";
                        d.LaborValue = cost.LabourValue ?? "0.00";
                    }
                    if (d.MissingCheck == "Yes")
                    {
                        d.MissingValue = cost.MissingValue ?? "0.00";
                        d.LaborValue = cost.LabourValue ?? "0.00";
                    }
                    if (d.DamageCheck == "Yes")
                    {
                        d.ReplaceValue = cost.ReplaceValue ?? "0.00";
                        d.LaborValue = cost.LabourValue ?? "0.00";
                    }
                }
            }

            var firstDto = dtos.First();
            var prefix = firstDto.FormId.Substring(0, 2);
            var model = firstDto.LocoModel;

            bool existsTFR = await _context.MasterLocosTFR
                .AnyAsync(e => e.LocoNumber == firstDto.LocoNumber);

            bool existsTE = await _context.MasterLocosTE
                .AnyAsync(e => e.LocoNumber == firstDto.LocoNumber);

            int phase = 0;

            if (existsTFR)
            {
                phase = 2;
            }
            else if (existsTE)
            {
                phase = 3;
            }
            else
            {
                phase = 1;
            }

            try
            {
                var locoNumbers = dtos.Select(d => d.LocoNumber).Distinct().ToList();

                switch ($"{model}_{prefix}")
                {
                    case "GM36_CA":

                        var existingRecords = await _context.Gm36cainspects
                            .Where(x => locoNumbers.Contains(x.LocoNumber))
                            .ToListAsync();

                        foreach (var dto in dtos)
                        {
                            var existing = existingRecords.FirstOrDefault(x =>
                                x.LocoNumber == dto.LocoNumber &&
                                x.PartId == dto.PartId);

                            if (existing != null)
                            {
                                MapInspection(existing, dto, phase);
                            }
                            else
                            {
                                var entity = new Gm36cainspect();
                                MapInspection(entity, dto, phase);
                                await _context.Gm36cainspects.AddAsync(entity);
                            }
                        }
                        break;

                    case "GM36_BP":

                        var existingRecords1 = await _context.Gm36bpinspects
                            .Where(x => locoNumbers.Contains(x.LocoNumber))
                            .ToListAsync();

                        foreach (var dto in dtos)
                        {
                            var existing = existingRecords1.FirstOrDefault(x =>
                                x.LocoNumber == dto.LocoNumber &&
                                x.PartId == dto.PartId);

                            if (existing != null)
                            {
                                MapInspection(existing, dto, phase);
                            }
                            else
                            {
                                var entity = new Gm36bpinspect();
                                MapInspection(entity, dto, phase);
                                await _context.Gm36bpinspects.AddAsync(entity);
                            }
                        }
                        break;

                    case "GM36_WA":

                        var existingRecords2 = await _context.Gm36wainspects
                            .Where(x => locoNumbers.Contains(x.LocoNumber))
                            .ToListAsync();

                        foreach (var dto in dtos)
                        {
                            var existing = existingRecords2.FirstOrDefault(x =>
                                x.LocoNumber == dto.LocoNumber &&
                                x.PartId == dto.PartId);

                            if (existing != null)
                            {
                                MapInspection(existing, dto, phase);
                            }
                            else
                            {
                                var entity = new Gm36wainspect();
                                MapInspection(entity, dto, phase);
                                await _context.Gm36wainspects.AddAsync(entity);
                            }
                        }
                        break;

                    case "GM36_BS":

                        var existingRecords3 = await _context.Gm36bsinspects
                            .Where(x => locoNumbers.Contains(x.LocoNumber))
                            .ToListAsync();

                        foreach (var dto in dtos)
                        {
                            var existing = existingRecords3.FirstOrDefault(x =>
                                x.LocoNumber == dto.LocoNumber &&
                                x.PartId == dto.PartId);

                            if (existing != null)
                            {
                                MapInspection(existing, dto, phase);
                            }
                            else
                            {
                                var entity = new Gm36bsinspect();
                                MapInspection(entity, dto, phase);
                                await _context.Gm36bsinspects.AddAsync(entity);
                            }
                        }
                        break;

                    case "GM36_CF":

                        var existingRecords4 = await _context.Gm36cfinspects
                            .Where(x => locoNumbers.Contains(x.LocoNumber))
                            .ToListAsync();

                        foreach (var dto in dtos)
                        {
                            var existing = existingRecords4.FirstOrDefault(x =>
                                x.LocoNumber == dto.LocoNumber &&
                                x.PartId == dto.PartId);

                            if (existing != null)
                            {
                                MapInspection(existing, dto, phase);
                            }
                            else
                            {
                                var entity = new Gm36cfinspect();
                                MapInspection(entity, dto, phase);
                                await _context.Gm36cfinspects.AddAsync(entity);
                            }
                        }
                        break;

                    case "GM36_CL":

                        var existingRecords5 = await _context.Gm36clinspects
                            .Where(x => locoNumbers.Contains(x.LocoNumber))
                            .ToListAsync();

                        foreach (var dto in dtos)
                        {
                            var existing = existingRecords5.FirstOrDefault(x =>
                                x.LocoNumber == dto.LocoNumber &&
                                x.PartId == dto.PartId);

                            if (existing != null)
                            {
                                MapInspection(existing, dto, phase);
                            }
                            else
                            {
                                var entity = new Gm36clinspect();
                                MapInspection(entity, dto, phase);
                                await _context.Gm36clinspects.AddAsync(entity);
                            }
                        }
                        break;

                    case "GM36_DE":

                        var existingRecords6 = await _context.Gm36deinspects
                            .Where(x => locoNumbers.Contains(x.LocoNumber))
                            .ToListAsync();

                        foreach (var dto in dtos)
                        {
                            var existing = existingRecords6.FirstOrDefault(x =>
                                x.LocoNumber == dto.LocoNumber &&
                                x.PartId == dto.PartId);

                            if (existing != null)
                            {
                                MapInspection(existing, dto, phase);
                            }
                            else
                            {
                                var entity = new Gm36deinspect();
                                MapInspection(entity, dto, phase);
                                await _context.Gm36deinspects.AddAsync(entity);
                            }
                        }
                        break;

                    case "GM36_EC":

                        var existingRecords7 = await _context.Gm36ecinspects
                            .Where(x => locoNumbers.Contains(x.LocoNumber))
                            .ToListAsync();

                        foreach (var dto in dtos)
                        {
                            var existing = existingRecords7.FirstOrDefault(x =>
                                x.LocoNumber == dto.LocoNumber &&
                                x.PartId == dto.PartId);

                            if (existing != null)
                            {
                                MapInspection(existing, dto, phase);
                            }
                            else
                            {
                                var entity = new Gm36ecinspect();
                                MapInspection(entity, dto, phase);
                                await _context.Gm36ecinspects.AddAsync(entity);
                            }
                        }
                        break;

                    case "GM36_ED":

                        var existingRecords8 = await _context.Gm36edinspects
                            .Where(x => locoNumbers.Contains(x.LocoNumber))
                            .ToListAsync();

                        foreach (var dto in dtos)
                        {
                            var existing = existingRecords8.FirstOrDefault(x =>
                                x.LocoNumber == dto.LocoNumber &&
                                x.PartId == dto.PartId);

                            if (existing != null)
                            {
                                MapInspection(existing, dto, phase);
                            }
                            else
                            {
                                var entity = new Gm36edinspect();
                                MapInspection(entity, dto, phase);
                                await _context.Gm36edinspects.AddAsync(entity);
                            }
                        }
                        break;

                    case "GM36_FL":

                        var existingRecords9 = await _context.Gm36flinspects
                            .Where(x => locoNumbers.Contains(x.LocoNumber))
                            .ToListAsync();

                        foreach (var dto in dtos)
                        {
                            var existing = existingRecords9.FirstOrDefault(x =>
                                x.LocoNumber == dto.LocoNumber &&
                                x.PartId == dto.PartId);

                            if (existing != null)
                            {
                                MapInspection(existing, dto, phase);
                            }
                            else
                            {
                                var entity = new Gm36flinspect();
                                MapInspection(entity, dto, phase);
                                await _context.Gm36flinspects.AddAsync(entity);
                            }
                        }
                        break;

                    case "GM36_LC":

                        var existingRecords10 = await _context.Gm36lcinspects
                            .Where(x => locoNumbers.Contains(x.LocoNumber))
                            .ToListAsync();

                        foreach (var dto in dtos)
                        {
                            var existing = existingRecords10.FirstOrDefault(x =>
                                x.LocoNumber == dto.LocoNumber &&
                                x.PartId == dto.PartId);

                            if (existing != null)
                            {
                                MapInspection(existing, dto, phase);
                            }
                            else
                            {
                                var entity = new Gm36lcinspect();
                                MapInspection(entity, dto, phase);
                                await _context.Gm36lcinspects.AddAsync(entity);
                            }
                        }
                        break;

                    case "GM36_RF":

                        var existingRecords11 = await _context.Gm36rfinspects
                            .Where(x => locoNumbers.Contains(x.LocoNumber))
                            .ToListAsync();

                        foreach (var dto in dtos)
                        {
                            var existing = existingRecords11.FirstOrDefault(x =>
                                x.LocoNumber == dto.LocoNumber &&
                                x.PartId == dto.PartId);

                            if (existing != null)
                            {
                                MapInspection(existing, dto, phase);
                            }
                            else
                            {
                                var entity = new Gm36rfinspect();
                                MapInspection(entity, dto, phase);
                                await _context.Gm36rfinspects.AddAsync(entity);
                            }
                        }
                        break;

                    case "GM36_SN":

                        var existingRecords12 = await _context.Gm36sninspects
                            .Where(x => locoNumbers.Contains(x.LocoNumber))
                            .ToListAsync();

                        foreach (var dto in dtos)
                        {
                            var existing = existingRecords12.FirstOrDefault(x =>
                                x.LocoNumber == dto.LocoNumber &&
                                x.PartId == dto.PartId);

                            if (existing != null)
                            {
                                MapInspection(existing, dto, phase);
                            }
                            else
                            {
                                var entity = new Gm36sninspect();
                                MapInspection(entity, dto, phase);
                                await _context.Gm36sninspects.AddAsync(entity);
                            }
                        }
                        break;

                    case "GM36_CB":

                        var existingRecords13 = await _context.Gm36cbinspects
                            .Where(x => locoNumbers.Contains(x.LocoNumber))
                            .ToListAsync();

                        foreach (var dto in dtos)
                        {
                            var existing = existingRecords13.FirstOrDefault(x =>
                                x.LocoNumber == dto.LocoNumber &&
                                x.PartId == dto.PartId);

                            if (existing != null)
                            {
                                MapInspection(existing, dto, phase);
                            }
                            else
                            {
                                var entity = new Gm36cbinspect();
                                MapInspection(entity, dto, phase);
                                await _context.Gm36cbinspects.AddAsync(entity);
                            }
                        }
                        break;

                    case "GM36_TR":

                        var existingRecords14 = await _context.Gm36trinspects
                            .Where(x => locoNumbers.Contains(x.LocoNumber))
                            .ToListAsync();

                        foreach (var dto in dtos)
                        {
                            var existing = existingRecords14.FirstOrDefault(x =>
                                x.LocoNumber == dto.LocoNumber &&
                                x.PartId == dto.PartId);

                            if (existing != null)
                            {
                                MapInspection(existing, dto, phase);
                            }
                            else
                            {
                                var entity = new Gm36trinspect();
                                MapInspection(entity, dto, phase);
                                await _context.Gm36trinspects.AddAsync(entity);
                            }
                        }
                        break;

                    case "GM36_LM":

                        var existingRecords15 = await _context.Gm36lminspects
                            .Where(x => locoNumbers.Contains(x.LocoNumber))
                            .ToListAsync();

                        foreach (var dto in dtos)
                        {
                            var existing = existingRecords15.FirstOrDefault(x =>
                                x.LocoNumber == dto.LocoNumber &&
                                x.PartId == dto.PartId);

                            if (existing != null)
                            {
                                MapInspection(existing, dto, phase);
                            }
                            else
                            {
                                var entity = new Gm36lminspect();
                                MapInspection(entity, dto, phase);
                                await _context.Gm36lminspects.AddAsync(entity);
                            }
                        }
                        break;

                    case "GM36_BV":

                        var existingRecords16 = await _context.Gm36bvinspects
                            .Where(x => locoNumbers.Contains(x.LocoNumber))
                            .ToListAsync();

                        foreach (var dto in dtos)
                        {
                            var existing = existingRecords16.FirstOrDefault(x =>
                                x.LocoNumber == dto.LocoNumber &&
                                x.PartId == dto.PartId);

                            if (existing != null)
                            {
                                MapInspection(existing, dto, phase);
                            }
                            else
                            {
                                var entity = new Gm36bvinspect();
                                MapInspection(entity, dto, phase);
                                await _context.Gm36bvinspects.AddAsync(entity);
                            }
                        }
                        break;

                    default:
                        return BadRequest("Invalid inspection type.");
                }

                await _context.SaveChangesAsync();


                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SubmitInspection failed");
                return StatusCode(500, "Submit failed.");
            }
        }

        //private static string? GetTableName(string formId) => formId.ToUpper() switch
        //{
        //    "BD001" => "GE34BDInspects",
        //    "FL001" => "GE34FLInspects",
        //    "SN001" => "GE34SNInspects",
        //    "CL001" => "GE34CLInspects",
        //    "EC001" => "GE34ECInspects",
        //    "BS001" => "GE34BSInspects",
        //    "OD001" => "GE34ODInspects",
        //    "BC001" => "GE34BCInspects",
        //    "AC001" => "GE34ACInspects",
        //    "ED001" => "GE34EDInspects",
        //    "CF001" => "GE34CFInspects",
        //    "DE001" => "GE34DEInspects",
        //    "RF001" => "GE34RFInspects",
        //    _ => null
        //};
    }
}
