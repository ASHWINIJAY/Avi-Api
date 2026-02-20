using AviAppFinal.Server.Models;
using AviFinal.Api.Controllers;
using AviFinal.Api.Models;
using Dapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Drawing;

// PLEASE MAKE SURE THAT EACH OF THE E18 INSPECTION DATA MODELS HAVE: public int Phase { get; set;}

namespace AviAppFinal.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class E18InspectController : ControllerBase
    {
        private readonly AviDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<GE36InspectController> _logger;
        private readonly IConfiguration _config;

        public E18InspectController(AviDbContext context, IWebHostEnvironment env, ILogger<GE36InspectController> logger, IConfiguration config)
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
                var partsList = await _context.E18finalParts
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
                var part = await _context.E18finalParts
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

            var firstDto = dtos.First();
            var prefix = firstDto.FormId.Substring(0, 2);
            var model = firstDto.LocoModel;
            using var con = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            await con.OpenAsync();
            var partIds = dtos.Select(d => d.PartId).ToList();
            var partCostData = await _context.E18finalParts
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
                    case "E18_BD":

                        var existingRecords = await _context.E18bdinspects
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
                                var entity = new E18bdinspect();
                                MapInspection(entity, dto, phase);
                                await _context.E18bdinspects.AddAsync(entity);
                            }
                        }
                        break;

                    case "E18_BE":

                        var existingRecords1 = await _context.E18beinspects
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
                                var entity = new E18beinspect();
                                MapInspection(entity, dto, phase);
                                await _context.E18beinspects.AddAsync(entity);
                            }
                        }
                        break;

                    case "E18_CC":

                        var existingRecords2 = await _context.E18ccinspects
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
                                var entity = new E18ccinspect();
                                MapInspection(entity, dto, phase);
                                await _context.E18ccinspects.AddAsync(entity);
                            }
                        }
                        break;

                    case "E18_CR":

                        var existingRecords3 = await _context.E18crinspects
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
                                var entity = new E18crinspect();
                                MapInspection(entity, dto, phase);
                                await _context.E18crinspects.AddAsync(entity);
                            }
                        }
                        break;

                    case "E18_CT":

                        var existingRecords4 = await _context.E18ctinspects
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
                                var entity = new E18ctinspect();
                                MapInspection(entity, dto, phase);
                                await _context.E18ctinspects.AddAsync(entity);
                            }
                        }
                        break;

                    case "E18_EE":

                        var existingRecords5 = await _context.E18eeinspects
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
                                var entity = new E18eeinspect();
                                MapInspection(entity, dto, phase);
                                await _context.E18eeinspects.AddAsync(entity);
                            }
                        }
                        break;

                    case "E18_EH":

                        var existingRecords6 = await _context.E18ehinspects
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
                                var entity = new E18ehinspect();
                                MapInspection(entity, dto, phase);
                                await _context.E18ehinspects.AddAsync(entity);
                            }
                        }
                        break;

                    case "E18_ES":

                        var existingRecords7 = await _context.E18esinspects
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
                                var entity = new E18esinspect();
                                MapInspection(entity, dto, phase);
                                await _context.E18esinspects.AddAsync(entity);
                            }
                        }
                        break;

                    case "E18_FL":

                        var existingRecords8 = await _context.E18flinspects
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
                                var entity = new E18flinspect();
                                MapInspection(entity, dto, phase);
                                await _context.E18flinspects.AddAsync(entity);
                            }
                        }
                        break;

                    case "E18_HC":

                        var existingRecords9 = await _context.E18hcinspects
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
                                var entity = new E18hcinspect();
                                MapInspection(entity, dto, phase);
                                await _context.E18hcinspects.AddAsync(entity);
                            }
                        }
                        break;

                    case "E18_HS":

                        var existingRecords10 = await _context.E18hsinspects
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
                                var entity = new E18hsinspect();
                                MapInspection(entity, dto, phase);
                                await _context.E18hsinspects.AddAsync(entity);
                            }
                        }
                        break;

                    case "E18_HV":

                        var existingRecords11 = await _context.E18hvinspects
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
                                var entity = new E18hvinspect();
                                MapInspection(entity, dto, phase);
                                await _context.E18hvinspects.AddAsync(entity);
                            }
                        }
                        break;

                    case "E18_LV":

                        var existingRecords12 = await _context.E18lvinspects
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
                                var entity = new E18lvinspect();
                                MapInspection(entity, dto, phase);
                                await _context.E18lvinspects.AddAsync(entity);
                            }
                        }
                        break;

                    case "E18_MA":

                        var existingRecords13 = await _context.E18mainspects
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
                                var entity = new E18mainspect();
                                MapInspection(entity, dto, phase);
                                await _context.E18mainspects.AddAsync(entity);
                            }
                        }
                        break;

                    case "E18_MB":

                        var existingRecords14 = await _context.E18mbinspects
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
                                var entity = new E18mbinspect();
                                MapInspection(entity, dto, phase);
                                await _context.E18mbinspects.AddAsync(entity);
                            }
                        }
                        break;

                    case "E18_RF":

                        var existingRecords15 = await _context.E18rfinspects
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
                                var entity = new E18rfinspect();
                                MapInspection(entity, dto, phase);
                                await _context.E18rfinspects.AddAsync(entity);
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
