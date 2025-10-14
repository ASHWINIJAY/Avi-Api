using AviFinal.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;
using System.Text.Json;

namespace AviFinal.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashBoardItemsController : ControllerBase
    {
        private readonly AviDbContext _context;

        public DashBoardItemsController(AviDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboardItems()
        {
            try
            {
                var baseUrl = $"{Request.Scheme}://{Request.Host}/";

                var itemsFromDb = await _context.DashBoardItems
                    .Where(d => d.UploadStatus != "Uploaded")
                    .Select(d => new
                    {
                        record = d.Record,
                        locoNumber = d.LocoNumber,
                        dateAssessed = d.DateAssessed.ToString("yyyy-MM-dd"),
                        timeAssessed = d.TimeAssessed.ToString(@"hh\:mm"),
                        inspectorName = d.InspectorName ?? "",
                        proMain = d.ProMain ?? "",
                        bodyDamage = d.BodyDamage ?? "",
                        bodyPhotos = d.BodyPhotos ?? "",
                        bodyRepairValue = string.IsNullOrEmpty(d.BodyRepairValue) ? "0" : d.BodyRepairValue,
                        replaceValue = string.IsNullOrEmpty(d.ReplaceValue) ? "0" : d.ReplaceValue,
                        refurbishValue = string.IsNullOrEmpty(d.RefurbishValue) ? "0" : d.RefurbishValue,
                        liftingRequired = d.LiftingRequired ?? "",
                        liftPhotos = d.LiftPhotos ?? "",
                        liftDate = d.LiftDate.HasValue ? d.LiftDate.Value.ToString("yyyy-MM-dd") : "",
                        assessmentResults = d.AssessmentResults ?? "Not Functional",
                        assessmentPhotosRaw = d.AssessmentPhotos ?? "",
                        assessmentQuote = d.AssessmentQuote ?? "",
                        assessmentCertRaw = d.AssessmentCert ?? "",
                        uploadStatus = d.UploadStatus ?? "",
                        uploadDate = d.UploadDate.ToString("yyyy-MM-dd")
                    })
                    .ToListAsync();

                var formattedItems = itemsFromDb.Select(d => new
                {
                    d.record,
                    d.locoNumber,
                    d.dateAssessed,
                    d.timeAssessed,
                    d.inspectorName,
                    d.proMain,
                    d.bodyDamage,
                    bodyPhotos = ConvertPathsToUrls(d.bodyPhotos, baseUrl, "uploads"),
                    d.bodyRepairValue,
                    d.replaceValue,
                    d.refurbishValue,
                    d.liftingRequired,
                    liftPhotos = ConvertPathsToUrls(d.liftPhotos, baseUrl, "uploads"),
                    d.liftDate,
                    d.assessmentResults,
                    assessmentPhotos = ConvertPathsToUrls(d.assessmentPhotosRaw, baseUrl, "uploads"),
                    assessmentCert = ConvertSinglePathToUrl(d.assessmentCertRaw, baseUrl, "certificates"),
                    assessmentQuote = ConvertSinglePathToUrl(d.assessmentQuote, baseUrl, "quotes"),
                    d.uploadStatus,
                    d.uploadDate
                });

                return Ok(formattedItems);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadSelected([FromBody] List<string> selectedRecords)
        {
            if (selectedRecords == null || !selectedRecords.Any())
                return BadRequest("No items selected.");

            var items = await _context.DashBoardItems
                .Where(d => selectedRecords.Contains(d.Record))
                .ToListAsync();

            if (!items.Any())
                return NotFound("No matching items found.");

            string zipName = $"DashboardUploads_{DateTime.Now:yyyyMMdd_HHmmss}.zip";

            var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                foreach (var item in items)
                {
                    string parentFolder = $"{item.LocoNumber}_{item.Record}_{DateTime.Now:yyyyMMdd}";

                    foreach (var path in ParsePhotoField(item.BodyPhotos))
                        await AddFileToArchive(archive, path, $"{parentFolder}/Body Photos");

                    foreach (var path in ParsePhotoField(item.LiftPhotos))
                        await AddFileToArchive(archive, path, $"{parentFolder}/Lift Photos");

                    foreach (var path in ParsePhotoField(item.AssessmentPhotos))
                        await AddFileToArchive(archive, path, $"{parentFolder}/Assessment Photos");

                    if (!string.IsNullOrWhiteSpace(item.AssessmentCert))
                        await AddFileToArchive(archive, item.AssessmentCert, $"{parentFolder}/Evaluation Certificate");

                    if (!string.IsNullOrWhiteSpace(item.AssessmentQuote))
                        await AddFileToArchive(archive, item.AssessmentQuote, $"{parentFolder}/Quotes Document");

                    item.UploadStatus = "Uploaded";
                    item.UploadDate = DateOnly.FromDateTime(DateTime.Now);
                }
            }

            await _context.SaveChangesAsync();

            memoryStream.Position = 0;
            return File(memoryStream, "application/zip", zipName);
        }

        #region Helpers

        private async Task AddFileToArchive(ZipArchive archive, string storedPath, string folder)
        {
            var absolute = ResolveAbsolutePath(storedPath);
            if (absolute != null && System.IO.File.Exists(absolute))
            {
                var entry = archive.CreateEntry($"{folder}/{Path.GetFileName(absolute)}");
                using var entryStream = entry.Open();
                using var fileStream = System.IO.File.OpenRead(absolute);
                await fileStream.CopyToAsync(entryStream);
            }
        }

        private IEnumerable<string> ParsePhotoField(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return Enumerable.Empty<string>();

            if (raw.TrimStart().StartsWith("["))
            {
                try
                {
                    var arr = JsonSerializer.Deserialize<List<string>>(raw);
                    if (arr != null) return arr.Where(a => !string.IsNullOrWhiteSpace(a));
                }
                catch { }
            }

            return raw.Split(';', StringSplitOptions.RemoveEmptyEntries)
                      .Select(p => p.Trim())
                      .Where(p => !string.IsNullOrWhiteSpace(p));
        }

        private string? ResolveAbsolutePath(string storedPath)
        {
            if (string.IsNullOrWhiteSpace(storedPath)) return null;

            var p = storedPath.Trim().Replace("\\", "/").Trim('"');

            if (p.StartsWith("http://") || p.StartsWith("https://"))
            {
                try { var uri = new Uri(p); p = uri.AbsolutePath.TrimStart('/'); }
                catch { }
            }

            string folder = "uploads"; // default images
            if (p.StartsWith("certificates/", StringComparison.OrdinalIgnoreCase)) folder = "certificates";
            else if (p.StartsWith("quotes/", StringComparison.OrdinalIgnoreCase)) folder = "quotes";

            p = p.Replace($"{folder}/", "");

            return Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", folder, p.Replace("/", Path.DirectorySeparatorChar.ToString()));
        }

        private List<string> ConvertPathsToUrls(string paths, string baseUrl, string folder)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(paths)) return result;

            try
            {
                if (paths.TrimStart().StartsWith("["))
                {
                    var arr = JsonSerializer.Deserialize<List<string>>(paths);
                    if (arr != null)
                    {
                        foreach (var p in arr)
                        {
                            if (string.IsNullOrWhiteSpace(p)) continue;
                            var clean = p.Replace("\\", "/").Trim().Trim('"');
                            if (clean.StartsWith(folder + "/", StringComparison.OrdinalIgnoreCase))
                                clean = clean.Substring(folder.Length + 1); // remove duplicate folder
                            result.Add(clean.StartsWith("http") ? clean : baseUrl + $"{folder}/{clean}");
                        }
                        return result;
                    }
                }
            }
            catch { }

            foreach (var p in paths.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var clean = p.Replace("\\", "/").Trim().Trim('"');
                if (string.IsNullOrWhiteSpace(clean)) continue;
                if (clean.StartsWith(folder + "/", StringComparison.OrdinalIgnoreCase))
                    clean = clean.Substring(folder.Length + 1);
                result.Add(clean.StartsWith("http") ? clean : baseUrl + $"{folder}/{clean}");
            }

            return result;
        }


        private string ConvertSinglePathToUrl(string path, string baseUrl, string folder)
        {
            if (string.IsNullOrWhiteSpace(path)) return "";

            var clean = path.Replace("\\", "/").Trim().Trim('"');

            // Already a full URL
            if (clean.StartsWith("http://") || clean.StartsWith("https://")) return clean;

            // Remove leading slash
            clean = clean.TrimStart('/');

            // Remove folder prefix if it already exists
            if (clean.StartsWith(folder + "/", StringComparison.OrdinalIgnoreCase))
                clean = clean.Substring(folder.Length + 1);

            return baseUrl + $"{folder}/{clean}";
        }

        #endregion
    }
}
