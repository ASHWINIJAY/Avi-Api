using AviFinal.Api.Models;
using Dapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Drawing;

namespace AviAppFinal.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GE35InspectController : ControllerBase
    {
        private readonly AviDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<GE35InspectController> _logger;
        private readonly IConfiguration _config;

        public GE35InspectController(AviDbContext context, IWebHostEnvironment env, ILogger<GE35InspectController> logger, IConfiguration config)
        {
            _context = context;
            _env = env;
            _logger = logger;
            _config = config;
        }

        // ✅ 1️⃣ Get Parts by FormID
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
                    return NotFound($"No parts found for formID: {formID}");

                var ordered = partsList.OrderBy(p =>
                {
                    var digits = new string(p.PartId?.Where(char.IsDigit).ToArray());
                    return int.TryParse(digits, out int n) ? n : int.MaxValue;
                });

                var result = ordered.Select(p => new
                {
                    PartId = p.PartId,
                    PartDescr = p.PartDescr
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetParts failed for {FormId}", formID);
                return StatusCode(500, "Error retrieving parts.");
            }
        }

        // ✅ 2️⃣ Get Part Cost
        [HttpGet("getPartCost")]
        public async Task<IActionResult> GetPartCost(string partId, string field)
        {
            if (string.IsNullOrWhiteSpace(partId) || string.IsNullOrWhiteSpace(field))
                return BadRequest("partId and field are required.");

            try
            {
                var part = await _context.Ge35finalParts.FirstOrDefaultAsync(p => p.PartId == partId);
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
                return StatusCode(500, "Error retrieving part cost.");
            }
        }

        // ✅ 3️⃣ Upload Photo
        [HttpPost("UploadPhoto")]
        public async Task<IActionResult> UploadPhoto([FromForm] IFormFile file, [FromForm] string formId, [FromForm] string partId, [FromForm] string photoType, [FromForm] string locoNumber, [FromForm] string locoModel)
        {
            if (file == null || string.IsNullOrEmpty(formId) || string.IsNullOrEmpty(partId))
                return BadRequest("Missing parameters.");

            try
            {
                string baseFolder = Path.Combine(_env.WebRootPath, "GE35", formId.ToUpper());
                string subFolder = photoType.ToLower() == "damage" ? "DamagePhotos" : "MissingPhotos";
                string fullFolderPath = Path.Combine(baseFolder, subFolder);
                if (!Directory.Exists(fullFolderPath))
                    Directory.CreateDirectory(fullFolderPath);

                string ext = Path.GetExtension(file.FileName);
                string fileName = $"{locoNumber}_{locoModel}_{photoType}_{DateTime.Now:yyyyMMdd_HHmmss}{ext}";
                string fullPath = Path.Combine(fullFolderPath, fileName);

                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                string relativePath = Path.Combine("GE35", formId.ToUpper(), subFolder, fileName).Replace("\\", "/");
                return Ok(new { path = relativePath });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UploadPhoto failed for {partId}", partId);
                return StatusCode(500, "Upload failed.");
            }
        }

        // ✅ 4️⃣ Delete Photo
        public class DeletePhotoRequest
        {
            public string Path { get; set; } = string.Empty;
        }

        [HttpPost("DeletePhoto")]
        public IActionResult DeletePhoto([FromBody] DeletePhotoRequest req)
        {
            if (req == null || string.IsNullOrEmpty(req.Path))
                return BadRequest("Invalid path.");

            try
            {
                string fullPath = Path.Combine(_env.WebRootPath, req.Path.Replace("/", "\\"));
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                    return Ok("Deleted successfully.");
                }
                return Ok("File not found.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeletePhoto failed");
                return StatusCode(500, "Delete failed.");
            }
        }

        // ✅ 5️⃣ DTO Class
        public class GE35InspectDto
        {
            public int LocoNumber { get; set; }
            public string? LocoClass { get; set; }
            public string? LocoModel { get; set; }
            public string FormId { get; set; } = "";
            public string PartId { get; set; } = "";
            public string PartDescr { get; set; } = "";
            public string GoodCheck { get; set; } = "No";
            public string RefurbishCheck { get; set; } = "No";
            public string MissingCheck { get; set; } = "No";
            public string ReplaceCheck { get; set; } = "No";
            public string? RefurbishValue { get; set; }
            public string? MissingValue { get; set; }
            public string? ReplaceValue { get; set; }
            public string? MissingPhoto { get; set; }
            public string? ReplacePhoto { get; set; }
            public string? CreatedBy { get; set; }
        }

        // ✅ 6️⃣ Submit Inspection (Dynamic Table Insert)
        [HttpPost("SubmitInspection")]
        public async Task<IActionResult> SubmitInspection([FromBody] List<GE35InspectDto> dtos)
        {
            if (dtos == null || dtos.Count == 0)
                return BadRequest("No data received.");

            try
            {
                string formId = dtos.First().FormId;
                string? tableName = GetTableName(formId);
                if (string.IsNullOrEmpty(tableName))
                    return BadRequest($"Unsupported FormId: {formId}");

                using var con = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
                await con.OpenAsync();
                var partIds = dtos.Select(d => d.PartId).ToList();
                var partCostData = await _context.Ge35finalParts
                    .Where(p => partIds.Contains(p.PartId))
                    .ToDictionaryAsync(p => p.PartId, p => new
                    {
                        p.RefurbishValue,
                        p.MissingValue,
                        p.ReplaceValue
                    });
                foreach (var d in dtos)
                {
                    if (partCostData.TryGetValue(d.PartId, out var cost))
                    {
                        if (d.RefurbishCheck == "Yes")
                            d.RefurbishValue = cost.RefurbishValue ?? "0.00";
                        if (d.MissingCheck == "Yes")
                            d.MissingValue = cost.MissingValue ?? "0.00";
                        if (d.ReplaceCheck == "Yes")
                            d.ReplaceValue = cost.ReplaceValue ?? "0.00";
                    }
                    string checkSql = $@"
    SELECT COUNT(*) 
    FROM {tableName}
    WHERE LocoNumber = @LocoNumber 
      AND FormId = @FormId 
      AND PartId = @PartId";

                    int exists = await con.ExecuteScalarAsync<int>(checkSql, d);

                    if (exists > 0)
                    {
                        // 🔹 Record already exists → perform UPDATE
                        string updateSql = $@"
        UPDATE {tableName}
        SET 
            GoodCheck = @GoodCheck,
            RefurbishCheck = @RefurbishCheck,
            MissingCheck = @MissingCheck,
            ReplaceCheck = @ReplaceCheck,
            RefurbishValue = @RefurbishValue,
            MissingValue = @MissingValue,
            ReplaceValue = @ReplaceValue,
            MissingPhoto = @MissingPhoto,
            ReplacePhoto = @ReplacePhoto
        WHERE 
            LocoNumber = @LocoNumber 
            AND FormId = @FormId 
            AND PartId = @PartId";

                        await con.ExecuteAsync(updateSql, d);
                    }
                    else
                    {
                        // 🔹 Record doesn’t exist → perform INSERT
                        string insertSql = $@"
        INSERT INTO {tableName}
        (LocoNumber, LocoClass, LocoModel, FormId, PartId, PartDescr,
         GoodCheck, RefurbishCheck, MissingCheck, ReplaceCheck,
         RefurbishValue, MissingValue, ReplaceValue,
         MissingPhoto, ReplacePhoto)
        VALUES
        (@LocoNumber, @LocoClass, @LocoModel, @FormId, @PartId, @PartDescr,
         @GoodCheck, @RefurbishCheck, @MissingCheck, @ReplaceCheck,
         @RefurbishValue, @MissingValue, @ReplaceValue,
         @MissingPhoto, @ReplacePhoto)";

                        await con.ExecuteAsync(insertSql, d);
                    }

                }

                return Ok("Saved successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SubmitInspection failed.");
                return StatusCode(500, ex.Message);
            }
        }

        // ✅ 7️⃣ Table Mapper
        private static string? GetTableName(string formId) => formId.ToUpper() switch
        {
            "BD001" => "GE35BDInspects",
            "FL001" => "GE35FLInspects",
            "SN001" => "GE35SNInspects",
            "CL001" => "GE35CLInspects",
            "EC001" => "GE35ECInspects",
            "BS001" => "GE35BSInspects",
            "OD001" => "GE35ODInspects",
            "BC001" => "GE35BCInspects",
            "MG001" => "GE35MGInspects",
            "ED001" => "GE35EDInspects",
            "CF001" => "GE35CFInspects",
            "DE001" => "GE35DEInspects",
            "RF001" => "GE35RFInspects",
            _ => null
        };
    }
}
