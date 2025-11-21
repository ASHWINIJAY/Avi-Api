using AviFinal.Api.Models;
using Dapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace AviAppFinal.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GM36InspectController : ControllerBase
    {
        private readonly AviDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<GM36InspectController> _logger;
        private readonly string _connectionString;

        public GM36InspectController(AviDbContext context, IWebHostEnvironment env, ILogger<GM36InspectController> logger, IConfiguration config)
        {
            _context = context;
            _env = env;
            _logger = logger;
            _connectionString = config.GetConnectionString("DefaultConnection")!;
        }

        // ✅ Get parts
        [HttpGet("getParts/{formID}")]
        public async Task<IActionResult> GetParts(string formID)
        {
            try
            {
                var partsList = await _context.Gm36finalParts
                    .Where(p => p.FormId == formID)
                    .OrderBy(p => p.PartId)
                    .Select(p => new { p.PartId, p.PartDescr })
                    .ToListAsync();

                return Ok(partsList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetParts failed for {formId}", formID);
                return StatusCode(500, "Error retrieving parts.");
            }
        }

        // ✅ Get cost
        [HttpGet("getPartCost")]
        public async Task<IActionResult> GetPartCost(string partId, string field)
        {
            if (string.IsNullOrWhiteSpace(partId)) return BadRequest("partId required");
            var part = await _context.Gm36finalParts.FirstOrDefaultAsync(p => p.PartId == partId);
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

        // ✅ Upload photo
        [HttpPost("UploadPhoto")]
        public async Task<IActionResult> UploadPhoto([FromForm] IFormFile file, [FromForm] string formId, [FromForm] string partId, [FromForm] string photoType, [FromForm] string locoNumber, [FromForm] string locoModel)
        {
            if (file == null) return BadRequest("Missing file.");

            try
            {
                string baseFolder = Path.Combine(_env.WebRootPath, "GM36", formId.ToUpper());
                string subFolder = photoType.ToLower() switch
                {
                    "damage" => "DamagePhotos",
                    "missing" => "MissingPhotos",
                    _ => "Other"
                };

                string folder = Path.Combine(baseFolder, subFolder);
                Directory.CreateDirectory(folder);

                string fileName = $"{locoNumber}_{locoModel}_{photoType}_{DateTime.Now:yyyyMMdd_HHmmss}{Path.GetExtension(file.FileName)}";
                string fullPath = Path.Combine(folder, fileName);
                using var fs = new FileStream(fullPath, FileMode.Create);
                await file.CopyToAsync(fs);

                string relative = Path.Combine("GM36", formId.ToUpper(), subFolder, fileName).Replace("\\", "/");
                return Ok(new { path = relative });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Upload failed");
                return StatusCode(500, "Photo upload failed.");
            }
        }

        [HttpPost("DeletePhoto")]
        public IActionResult DeletePhoto([FromBody] DeletePhotoRequest request)
        {
            if (string.IsNullOrEmpty(request.Path)) return BadRequest("Missing path.");
            try
            {
                string full = Path.Combine(_env.WebRootPath, request.Path.Replace("/", "\\"));
                if (System.IO.File.Exists(full))
                    System.IO.File.Delete(full);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeletePhoto failed");
                return StatusCode(500, "Delete failed.");
            }
        }

        public class DeletePhotoRequest { public string Path { get; set; } = ""; }

        // ✅ DTO
        public class GM36InspectDto
        {
            public int LocoNumber { get; set; }
            public string LocoClass { get; set; } = "";
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
            public string? MissingPhoto { get; set; }
            public string? ReplaceValue { get; set; }
            public string? ReplacePhoto { get; set; }
        }

        // ✅ Submit (Insert / Update logic)
        [HttpPost("SubmitInspection")]
        public async Task<IActionResult> SubmitInspection([FromBody] List<GM36InspectDto> dtos)
        {
            if (dtos == null || !dtos.Any())
                return BadRequest("No data received.");

            string formId = dtos.First().FormId.ToUpper();
            string tableName = formId switch
            {
                "WA001" => "Gm36wainspects",
                "FL001" => "Gm36flinspects",
                "SN001" => "Gm36sninspects",
                "BV001" => "Gm36bvinspects",
                "CL001" => "Gm36clinspects",
                "EC001" => "Gm36ecinspects",
                "CB001" => "Gm36cbinspects",
                "BS001" => "Gm36bsinspects",
                "LM001" => "Gm36lminspects",
                "LC001" => "Gm36lcinspects",
                "TR001" => "Gm36trinspects",
                "BP001" => "Gm36bpinspects",
                "CA001" => "Gm36cainspects",
                "ED001" => "Gm36edinspects",
                "CF001" => "Gm36cfinspects",
                "DE001" => "Gm36deinspects",
                "RF001" => "Gm36rfinspects",
                _ => null
            };


            if (tableName == null)
                return BadRequest($"Unsupported formId: {formId}");

            try
            {
                using var con = new SqlConnection(_connectionString);
                con.Open();
                var partIds = dtos.Select(d => d.PartId).ToList();
                var partCostData = await _context.Gm36finalParts
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
                    string checkSql = $@"SELECT COUNT(*) FROM {tableName}
                                         WHERE LocoNumber = @LocoNumber AND FormId = @FormId AND PartId = @PartId";

                    int exists = await con.ExecuteScalarAsync<int>(checkSql, d);

                    if (exists > 0)
                    {
                        string updateSql = $@"
                            UPDATE {tableName}
                            SET 
                                LocoClass=@LocoClass,
                                LocoModel=@LocoModel,
                                PartDescr=@PartDescr,
                                GoodCheck=@GoodCheck,
                                RefurbishCheck=@RefurbishCheck,
                                MissingCheck=@MissingCheck,
                                ReplaceCheck=@ReplaceCheck,
                                RefurbishValue=@RefurbishValue,
                                MissingValue=@MissingValue,
                                ReplaceValue=@ReplaceValue,
                                MissingPhoto=@MissingPhoto,
                                ReplacePhoto=@ReplacePhoto
                            WHERE LocoNumber=@LocoNumber AND FormId=@FormId AND PartId=@PartId";
                        await con.ExecuteAsync(updateSql, d);
                    }
                    else
                    {
                        string insertSql = $@"
                            INSERT INTO {tableName}
                            (LocoNumber,LocoClass,LocoModel,FormId,PartId,PartDescr,GoodCheck,RefurbishCheck,MissingCheck,ReplaceCheck,
                             RefurbishValue,MissingValue,ReplaceValue,MissingPhoto,ReplacePhoto)
                            VALUES
                            (@LocoNumber,@LocoClass,@LocoModel,@FormId,@PartId,@PartDescr,@GoodCheck,@RefurbishCheck,@MissingCheck,@ReplaceCheck,
                             @RefurbishValue,@MissingValue,@ReplaceValue,@MissingPhoto,@ReplacePhoto)";
                        await con.ExecuteAsync(insertSql, d);
                    }
                }

                return Ok("Data saved successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SubmitInspection failed for {FormId}", formId);
                return StatusCode(500, "Submit failed.");
            }
        }
    }
}
