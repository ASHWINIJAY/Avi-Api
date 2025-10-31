using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.SqlClient;
using Dapper;
using AviFinal.Api.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace AviFinal.Api.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class InspectionController : ControllerBase
    {
        private readonly AviDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<InspectionController> _logger;

        public InspectionController(AviDbContext context, IWebHostEnvironment env, ILogger<InspectionController> logger)
        {
            _context = context;
            _env = env;
            _logger = logger;
        }

        /// <summary>
        /// Get parts for a given locoModel and formId.
        /// Returns PartId and PartDescr.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetParts(string locoModel, string formId)
        {
            if (string.IsNullOrWhiteSpace(locoModel) || string.IsNullOrWhiteSpace(formId))
                return BadRequest("locoModel and formId are required.");

            locoModel = locoModel.Trim().ToUpper();
            formId = formId.Trim();

            try
            {
                // Each FinalParts table should be exposed in your DbContext:
                // Ge34finalParts, Ge35finalParts, Ge36finalParts, E18FinalParts
                if (locoModel == "GE34")
                {
                    var parts = await _context.Ge34finalParts
                        .Where(p => p.FormId.Trim() == formId)
                        .OrderBy(p => p.PartId)
                        .Select(p => new { PartId = p.PartId, PartDescr = p.PartDescr })
                        .ToListAsync();
                    return Ok(parts);
                }

                if (locoModel == "GM34")
                {
                    var parts = await _context.Gm34finalParts
                        .Where(p => p.FormId.Trim() == formId)
                        .OrderBy(p => p.PartId)
                        .Select(p => new { PartId = p.PartId, PartDescr = p.PartDescr })
                        .ToListAsync();

                    return Ok(parts);
                }

                if (locoModel == "GE35")
                {
                    var parts = await _context.Ge35finalParts
                        .Where(p => p.FormId == formId)
                        .OrderBy(p => p.PartId)
                        .Select(p => new { PartId = p.PartId, PartDescr = p.PartDescr })
                        .ToListAsync();
                    return Ok(parts);
                }

                if (locoModel == "GM35")
                {
                    var parts = await _context.Gm35finalParts
                        .Where(p => p.FormId.Trim() == formId)
                        .OrderBy(p => p.PartId)
                        .Select(p => new { PartId = p.PartId, PartDescr = p.PartDescr })
                        .ToListAsync();

                    return Ok(parts);
                }

                if (locoModel == "GE36")
                {
                    var parts = await _context.Ge36finalParts
                        .Where(p => p.FormId == formId)
                        .OrderBy(p => p.PartId)
                        .Select(p => new { PartId = p.PartId, PartDescr = p.PartDescr })
                        .ToListAsync();
                    return Ok(parts);
                }

                if (locoModel == "GM36")
                {
                    var parts = await _context.Gm36finalParts
                        .Where(p => p.FormId.Trim() == formId)
                        .OrderBy(p => p.PartId)
                        .Select(p => new { PartId = p.PartId, PartDescr = p.PartDescr })
                        .ToListAsync();

                    return Ok(parts);
                }

                if (locoModel == "E18")
                {
                    var parts = await _context.E18finalParts
                        .Where(p => p.FormId == formId)
                        .OrderBy(p => p.PartId)
                        .Select(p => new { PartId = p.PartId, PartDescr = p.PartDescr })
                        .ToListAsync();
                    return Ok(parts);
                }

                return BadRequest($"Unsupported locoModel: {locoModel}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetParts failed for {locoModel} {formId}", locoModel, formId);
                return StatusCode(500, "Error retrieving parts.");
            }
        }

        /// <summary>
        /// Get specific cost value for a part (Refurbish, Missing, Replace).
        /// Returns the nvarchar value from FinalParts; returns "0.00" if not found/null.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetPartCost(string locoModel, string partId, string field)
        {
            if (string.IsNullOrWhiteSpace(locoModel) || string.IsNullOrWhiteSpace(partId) || string.IsNullOrWhiteSpace(field))
                return BadRequest("locoModel, partId and field are required.");

            locoModel = locoModel.Trim().ToUpper();
            partId = partId.Trim();
            field = field.Trim().ToLower();

            string? column = field switch
            {
                "refurbish" => "RefurbishValue",
                "missing" => "MissingValue",
                "replace" => "ReplaceValue",
                _ => null
            };

            if (column == null) return BadRequest("field must be 'Refurbish', 'Missing' or 'Replace'.");

            try
            {
                string? value = null!;

                if (locoModel == "GE34")
                {
                    var p = await _context.Ge34finalParts.FirstOrDefaultAsync(x => x.PartId == partId);
                    value = (field == "refurbish") ? p?.RefurbishValue : (field == "missing") ? p?.MissingValue : p?.ReplaceValue;
                }
                else if (locoModel == "GM34")
                {
                    var p = await _context.Gm34finalParts.FirstOrDefaultAsync(x => x.PartId == partId);
                    value = (field == "refurbish") ? p?.RefurbishValue : (field == "missing") ? p?.MissingValue : p?.ReplaceValue;
                }
                else if (locoModel == "GE35")
                {
                    var p = await _context.Ge35finalParts.FirstOrDefaultAsync(x => x.PartId == partId);
                    value = (field == "refurbish") ? p?.RefurbishValue : (field == "missing") ? p?.MissingValue : p?.ReplaceValue;
                }
                else if (locoModel == "GM35")
                {
                    var p = await _context.Gm35finalParts.FirstOrDefaultAsync(x => x.PartId == partId);
                    value = (field == "refurbish") ? p?.RefurbishValue : (field == "missing") ? p?.MissingValue : p?.ReplaceValue;
                }
                else if (locoModel == "GE36")
                {
                    var p = await _context.Ge36finalParts.FirstOrDefaultAsync(x => x.PartId == partId);
                    value = (field == "refurbish") ? p?.RefurbishValue : (field == "missing") ? p?.MissingValue : p?.ReplaceValue;
                }
                else if (locoModel == "GM36")
                {
                    var p = await _context.Gm36finalParts.FirstOrDefaultAsync(x => x.PartId == partId);
                    value = (field == "refurbish") ? p?.RefurbishValue : (field == "missing") ? p?.MissingValue : p?.ReplaceValue;
                }
                else if (locoModel == "E18")
                {
                    var p = await _context.E18finalParts.FirstOrDefaultAsync(x => x.PartId == partId);
                    value = (field == "refurbish") ? p?.RefurbishValue : (field == "missing") ? p?.MissingValue : p?.ReplaceValue;
                }
                else
                {
                    return BadRequest($"Unsupported locoModel: {locoModel}");
                }

                return Ok((value ?? "0.00"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetPartCost failed for {locoModel} {partId} {field}", locoModel, partId, field);
                return StatusCode(500, "Error retrieving part cost.");
            }
        }

        /// <summary>
        /// Upload single photo (Damage or Missing).
        /// Saves to wwwroot/{LocoModel}/{FormId}/{DamagePhotos|MissingPhotos}/ and returns the relative path.
        /// Expects: file, locoModel, formId, partId, photoType ("Missing"|"Damage"), locoNumber
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UploadPhoto([FromForm] IFormFile file,
            [FromForm] string locoModel,
            [FromForm] string formId,
            [FromForm] string partId,
            [FromForm] string photoType,
            [FromForm] string locoNumber)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            if (string.IsNullOrWhiteSpace(locoModel) || string.IsNullOrWhiteSpace(formId) || string.IsNullOrWhiteSpace(partId) || string.IsNullOrWhiteSpace(photoType) || string.IsNullOrWhiteSpace(locoNumber))
                return BadRequest("Missing required fields.");

            locoModel = locoModel.Trim();
            formId = formId.Trim();
            partId = partId.Trim();
            photoType = photoType.Trim();

            // choose folder name
            string subFolder = photoType.Equals("Missing", StringComparison.OrdinalIgnoreCase) ? "MissingPhotos" : "DamagePhotos";

            // ensure folder exists
            string uploadsFolder = Path.Combine(_env.WebRootPath, locoModel, formId, subFolder);
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            // name: {FormID}_{PartID}_{Missing|Damage}_{LocoModel}_{LocoNumber}.jpg
            string safePhotoType = photoType.Equals("Missing", StringComparison.OrdinalIgnoreCase) ? "Missing" : "Damage";
            string fileName = $"{formId}_{partId}_{safePhotoType}_{locoModel}_{locoNumber}.jpg";

            // avoid collisions by optionally appending ticks if file exists
            string fullPath = Path.Combine(uploadsFolder, fileName);

            try
            {
                // DELETE old photo if exists
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }

                await using var stream = new FileStream(fullPath, FileMode.Create);
                await file.CopyToAsync(stream);

                // return relative path from web root
                string relativeUrl = $"/{locoModel}/{formId}/{subFolder}/{fileName}";
                return Ok(new { path = relativeUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UploadPhoto failed for {locoModel} {formId} {partId}", locoModel, formId, partId);
                return StatusCode(500, "Error saving file.");
            }
        }

        [HttpPost]
        public IActionResult DeletePhoto([FromBody] dynamic data)
        {
            string? path = data?.path;
            if (string.IsNullOrWhiteSpace(path)) return BadRequest("No path provided.");

            string fullPath = Path.Combine(_env.WebRootPath, path.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));
            try
            {
                if (System.IO.File.Exists(fullPath))
                    System.IO.File.Delete(fullPath);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "DeletePhoto failed for {path}", path);
                return StatusCode(500, "Failed to delete photo.");
            }
        }

        /// <summary>
        /// Submit a batch of inspection rows.
        /// Accepts JSON array of InspectionRowDto objects in the body.
        /// Each row will be validated and inserted into the correct table based on LocoModel.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SubmitInspection([FromBody] InspectionRowDto[] rows)
        {
            if (rows == null || rows.Length == 0)
                return BadRequest("No rows provided.");

            var insertedCount = 0;
            var errors = new List<string>();

            using var trx = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var r in rows)
                {
                    // Basic validation
                    if (string.IsNullOrWhiteSpace(r.UserId) || r.LocoNumber <= 0 || string.IsNullOrWhiteSpace(r.LocoModel) || string.IsNullOrWhiteSpace(r.FormId) || string.IsNullOrWhiteSpace(r.PartId))
                    {
                        errors.Add($"Invalid row (UserId, LocoNumber, LocoModel, FormId and PartId are required) - PartId: {r.PartId}");
                        continue;
                    }

                    // ensure user exists
                    var userExists = await _context.LeaseCoUsers.AnyAsync(u => u.UserId == r.UserId);
                    if (!userExists)
                    {
                        errors.Add($"User not found: {r.UserId} for PartId {r.PartId}");
                        continue;
                    }

                    // ensure loco exists
                    var locoExists = await _context.MasterLocos.AnyAsync(m => m.LocoNumber == r.LocoNumber);
                    if (!locoExists)
                    {
                        errors.Add($"LocoNumber not found: {r.LocoNumber} for PartId {r.PartId}");
                        continue;
                    }

                    // Normalize check flags to Sentence case "Yes"/"No"
                    string NormalizeFlag(string? value) =>
                        string.IsNullOrWhiteSpace(value) ? "No" :
                        (value.Trim().Equals("Yes", StringComparison.OrdinalIgnoreCase) ? "Yes" : "No");

                    var good = NormalizeFlag(r.GoodCheck);
                    var refurbish = NormalizeFlag(r.RefurbishCheck);
                    var missing = NormalizeFlag(r.MissingCheck);
                    var damage = NormalizeFlag(r.DamageCheck);

                    // ensure only one selected (enforce): if more than one "Yes", prefer order Damage > Missing > Refurbish > Good
                    var checks = new Dictionary<string, string>
                    {
                        { "DamageCheck", damage },
                        { "MissingCheck", missing },
                        { "RefurbishCheck", refurbish },
                        { "GoodCheck", good }
                    };

                    // count yes
                    var yesCount = checks.Values.Count(v => v == "Yes");
                    if (yesCount > 1)
                    {
                        // enforce single selection by priority
                        var chosen = checks.First(kv => kv.Value == "Yes").Key; // default first found
                        // priority list: Damage, Missing, Refurbish, Good
                        var priority = new[] { "DamageCheck", "MissingCheck", "RefurbishCheck", "GoodCheck" };
                        var selected = priority.FirstOrDefault(p => checks[p] == "Yes") ?? chosen;

                        // reset all and set only selected to Yes
                        foreach (var k in checks.Keys.ToList()) checks[k] = "No";
                        checks[selected] = "Yes";
                    }

                    // Build entity using exact property names in your models
                    var locoModelUpper = r.LocoModel.Trim().ToUpper();

                    // prepare entity instance (cast to correct type)
                    if (locoModelUpper == "E18")
                    {
                        var ent = new E18inspect
                        {
                            UserId = r.UserId,
                            LocoNumber = r.LocoNumber,
                            LocoClass = r.LocoClass ?? "",
                            LocoModel = r.LocoModel,
                            FormId = r.FormId,
                            PartId = r.PartId,
                            PartDescr = r.PartDescr ?? "",
                            GoodCheck = checks["GoodCheck"],
                            RefurbishCheck = checks["RefurbishCheck"],
                            MissingCheck = checks["MissingCheck"],
                            ReplaceCheck = checks["DamageCheck"],
                            RefurbishValue = string.IsNullOrWhiteSpace(r.RefurbishValue) ? "0.00" : r.RefurbishValue,
                            MissingValue = string.IsNullOrWhiteSpace(r.MissingValue) ? "0.00" : r.MissingValue,
                            ReplaceValue = string.IsNullOrWhiteSpace(r.ReplaceValue) ? "0.00" : r.ReplaceValue,
                            DamagePhoto = string.IsNullOrWhiteSpace(r.DamagePhoto) ? null : r.DamagePhoto,
                            MissingPhoto = string.IsNullOrWhiteSpace(r.MissingPhoto) ? null : r.MissingPhoto
                        };
                        _context.E18inspects.Add(ent);
                    }
                    else if (locoModelUpper == "GE34")
                    {
                        var ent = new Ge34inspect
                        {
                            UserId = r.UserId,
                            LocoNumber = r.LocoNumber,
                            LocoClass = r.LocoClass ?? "",
                            LocoModel = r.LocoModel,
                            FormId = r.FormId,
                            PartId = r.PartId,
                            PartDescr = r.PartDescr ?? "",
                            GoodCheck = checks["GoodCheck"],
                            RefurbishCheck = checks["RefurbishCheck"],
                            MissingCheck = checks["MissingCheck"],
                            ReplaceCheck = checks["DamageCheck"],
                            RefurbishValue = string.IsNullOrWhiteSpace(r.RefurbishValue) ? "0.00" : r.RefurbishValue,
                            MissingValue = string.IsNullOrWhiteSpace(r.MissingValue) ? "0.00" : r.MissingValue,
                            ReplaceValue = string.IsNullOrWhiteSpace(r.ReplaceValue) ? "0.00" : r.ReplaceValue,
                            DamagePhoto = string.IsNullOrWhiteSpace(r.DamagePhoto) ? null : r.DamagePhoto,
                            MissingPhoto = string.IsNullOrWhiteSpace(r.MissingPhoto) ? null : r.MissingPhoto
                        };
                        _context.Ge34inspects.Add(ent);
                    }
                    else if (locoModelUpper == "GM34")
                    {
                        var ent = new Gm34inspect
                        {
                            UserId = r.UserId,
                            LocoNumber = r.LocoNumber,
                            LocoClass = r.LocoClass ?? "",
                            LocoModel = r.LocoModel,
                            FormId = r.FormId,
                            PartId = r.PartId,
                            PartDescr = r.PartDescr ?? "",
                            GoodCheck = checks["GoodCheck"],
                            RefurbishCheck = checks["RefurbishCheck"],
                            MissingCheck = checks["MissingCheck"],
                            ReplaceCheck = checks["DamageCheck"],
                            RefurbishValue = string.IsNullOrWhiteSpace(r.RefurbishValue) ? "0.00" : r.RefurbishValue,
                            MissingValue = string.IsNullOrWhiteSpace(r.MissingValue) ? "0.00" : r.MissingValue,
                            ReplaceValue = string.IsNullOrWhiteSpace(r.ReplaceValue) ? "0.00" : r.ReplaceValue,
                            DamagePhoto = string.IsNullOrWhiteSpace(r.DamagePhoto) ? null : r.DamagePhoto,
                            MissingPhoto = string.IsNullOrWhiteSpace(r.MissingPhoto) ? null : r.MissingPhoto
                        };
                        _context.Gm34inspects.Add(ent);
                    }
                    else if (locoModelUpper == "GE35")
                    {
                        var ent = new Ge35inspect
                        {
                            UserId = r.UserId,
                            LocoNumber = r.LocoNumber,
                            LocoClass = r.LocoClass ?? "",
                            LocoModel = r.LocoModel,
                            FormId = r.FormId,
                            PartId = r.PartId,
                            PartDescr = r.PartDescr ?? "",
                            GoodCheck = checks["GoodCheck"],
                            RefurbishCheck = checks["RefurbishCheck"],
                            MissingCheck = checks["MissingCheck"],
                            ReplaceCheck = checks["DamageCheck"],
                            RefurbishValue = string.IsNullOrWhiteSpace(r.RefurbishValue) ? "0.00" : r.RefurbishValue,
                            MissingValue = string.IsNullOrWhiteSpace(r.MissingValue) ? "0.00" : r.MissingValue,
                            ReplaceValue = string.IsNullOrWhiteSpace(r.ReplaceValue) ? "0.00" : r.ReplaceValue,
                            DamagePhoto = string.IsNullOrWhiteSpace(r.DamagePhoto) ? null : r.DamagePhoto,
                            MissingPhoto = string.IsNullOrWhiteSpace(r.MissingPhoto) ? null : r.MissingPhoto
                        };
                        _context.Ge35inspects.Add(ent);
                    }
                    else if (locoModelUpper == "GM35")
                    {
                        var ent = new Gm35inspect
                        {
                            UserId = r.UserId,
                            LocoNumber = r.LocoNumber,
                            LocoClass = r.LocoClass ?? "",
                            LocoModel = r.LocoModel,
                            FormId = r.FormId,
                            PartId = r.PartId,
                            PartDescr = r.PartDescr ?? "",
                            GoodCheck = checks["GoodCheck"],
                            RefurbishCheck = checks["RefurbishCheck"],
                            MissingCheck = checks["MissingCheck"],
                            ReplaceCheck = checks["DamageCheck"],
                            RefurbishValue = string.IsNullOrWhiteSpace(r.RefurbishValue) ? "0.00" : r.RefurbishValue,
                            MissingValue = string.IsNullOrWhiteSpace(r.MissingValue) ? "0.00" : r.MissingValue,
                            ReplaceValue = string.IsNullOrWhiteSpace(r.ReplaceValue) ? "0.00" : r.ReplaceValue,
                            DamagePhoto = string.IsNullOrWhiteSpace(r.DamagePhoto) ? null : r.DamagePhoto,
                            MissingPhoto = string.IsNullOrWhiteSpace(r.MissingPhoto) ? null : r.MissingPhoto
                        };
                        _context.Gm35inspects.Add(ent);
                    }
                    else if (locoModelUpper == "GE36")
                    {
                        var ent = new Ge36inspect
                        {
                            UserId = r.UserId,
                            LocoNumber = r.LocoNumber,
                            LocoClass = r.LocoClass ?? "",
                            LocoModel = r.LocoModel,
                            FormId = r.FormId,
                            PartId = r.PartId,
                            PartDescr = r.PartDescr ?? "",
                            GoodCheck = checks["GoodCheck"],
                            RefurbishCheck = checks["RefurbishCheck"],
                            MissingCheck = checks["MissingCheck"],
                            ReplaceCheck = checks["DamageCheck"],
                            RefurbishValue = string.IsNullOrWhiteSpace(r.RefurbishValue) ? "0.00" : r.RefurbishValue,
                            MissingValue = string.IsNullOrWhiteSpace(r.MissingValue) ? "0.00" : r.MissingValue,
                            ReplaceValue = string.IsNullOrWhiteSpace(r.ReplaceValue) ? "0.00" : r.ReplaceValue,
                            DamagePhoto = string.IsNullOrWhiteSpace(r.DamagePhoto) ? null : r.DamagePhoto,
                            MissingPhoto = string.IsNullOrWhiteSpace(r.MissingPhoto) ? null : r.MissingPhoto
                        };
                        _context.Ge36inspects.Add(ent);
                    }
                    else if (locoModelUpper == "GM36")
                    {
                        var ent = new Gm36inspect
                        {
                            UserId = r.UserId,
                            LocoNumber = r.LocoNumber,
                            LocoClass = r.LocoClass ?? "",
                            LocoModel = r.LocoModel,
                            FormId = r.FormId,
                            PartId = r.PartId,
                            PartDescr = r.PartDescr ?? "",
                            GoodCheck = checks["GoodCheck"],
                            RefurbishCheck = checks["RefurbishCheck"],
                            MissingCheck = checks["MissingCheck"],
                            ReplaceCheck = checks["DamageCheck"],
                            RefurbishValue = string.IsNullOrWhiteSpace(r.RefurbishValue) ? "0.00" : r.RefurbishValue,
                            MissingValue = string.IsNullOrWhiteSpace(r.MissingValue) ? "0.00" : r.MissingValue,
                            ReplaceValue = string.IsNullOrWhiteSpace(r.ReplaceValue) ? "0.00" : r.ReplaceValue,
                            DamagePhoto = string.IsNullOrWhiteSpace(r.DamagePhoto) ? null : r.DamagePhoto,
                            MissingPhoto = string.IsNullOrWhiteSpace(r.MissingPhoto) ? null : r.MissingPhoto
                        };
                        _context.Gm36inspects.Add(ent);
                    }
                    else
                    {
                        errors.Add($"Unsupported LocoModel: {r.LocoModel} for PartId {r.PartId}");
                        continue;
                    }

                    insertedCount++;
                } // foreach row

                await _context.SaveChangesAsync();
                await trx.CommitAsync();

                return Ok(new { inserted = insertedCount, errors });
            }
            catch (Exception ex)
            {
                await trx.RollbackAsync();
                _logger.LogError(ex, "SubmitInspection failed.");
                return StatusCode(500, "Error saving inspection rows.");
            }
        }

        // DTO used by SubmitInspection
        public class InspectionRowDto
        {
            public string UserId { get; set; } = null!;
            public int LocoNumber { get; set; }
            public string? LocoClass { get; set; }
            public string LocoModel { get; set; } = null!;
            public string FormId { get; set; } = null!;
            public string PartId { get; set; } = null!;
            public string? PartDescr { get; set; }
            public string? GoodCheck { get; set; } // expected "Yes"/"No" or null
            public string? RefurbishCheck { get; set; }
            public string? MissingCheck { get; set; }
            public string? DamageCheck { get; set; }
            public string? RefurbishValue { get; set; }
            public string? MissingValue { get; set; }
            public string? ReplaceValue { get; set; }
            public string? DamagePhoto { get; set; } // relative url if already uploaded
            public string? MissingPhoto { get; set; } // relative url if already uploaded
        }
    }
}