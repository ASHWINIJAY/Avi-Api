using AviAppFinal.Server.Models;
using AviFinal.Api.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Threading.Tasks;

namespace AviAppFinal.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LocoDashController : Controller
    {
        public class UploadLocoItem
        {
            public int LocoNumber { get; set; }
            public string? BodyPhotos { get; set; }
            public string? AssessmentQuote { get; set; }
            public string? AssessmentCert { get; set; }
            public string? AssessmentSow { get; set; }
            public string? LocoPhoto { get; set; }
            public string? MissingPhotos { get; set; }
            public string? ReplacePhotos { get; set; }
        }

        private readonly AviDbContext _context;

        private readonly IWebHostEnvironment _env;

        private readonly IConfiguration _config;

        private readonly IHttpClientFactory _httpClientFactory;

        public LocoDashController(AviDbContext context, IWebHostEnvironment env, IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _env = env;
            _config = config;
            _httpClientFactory = httpClientFactory;
        }

        // ADJUST ↓
        [HttpPost("insertLoco")]
        public async Task<IActionResult> InsertLoco(int locoNumber, string userId)
        {
            _context.Database.SetCommandTimeout(200);

            var leaseUser = await _context.LeaseCoUsers
                                          .Where(u => u.UserId == userId)
                                          .Select(u => new { u.UserName })
                                          .FirstOrDefaultAsync();

            string inspectorName = leaseUser?.UserName ?? "No User";

            var locoInfo = await _context.LocoInfoCaptures
                                          .Where(w => w.LocoNumber == locoNumber)
                                          .OrderByDescending(w => w.Id)
                                          .Select(w => new
                                          {
                                              w.LocoClass,
                                              w.LocoModel,
                                              w.BodyDamage,
                                              w.BodyPhoto1,
                                              w.BodyPhoto2,
                                              w.BodyPhoto3,
                                              w.LocoPhoto,
                                              w.CreatedDate,
                                              w.GpsLatitude,
                                              w.GpsLongitude,
                                              w.NetBookValue,
                                              w.Phase,
                                          })
                                          .FirstOrDefaultAsync();

            if (locoInfo == null)
                return NotFound(new { success = false, message = $"No LocoInfoCaptures record found for loco {locoNumber}" });

            string city = "Not Captured";

            if (double.TryParse(locoInfo?.GpsLatitude, NumberStyles.Any, CultureInfo.InvariantCulture, out double latitude)
                && double.TryParse(locoInfo?.GpsLongitude, NumberStyles.Any, CultureInfo.InvariantCulture, out double longitude))
            {
                var resolved = await GetCityFromCoordinatesAsync(latitude, longitude);
                if (!string.IsNullOrWhiteSpace(resolved) && !resolved.StartsWith("Error", StringComparison.InvariantCultureIgnoreCase))
                    city = resolved;
            }

            string bodyDamage = locoInfo?.BodyDamage ?? "No";
            List<string> bodyPhotosList = new();

            if (string.Equals(bodyDamage, "Yes", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(locoInfo?.BodyPhoto1)) bodyPhotosList.Add(locoInfo.BodyPhoto1);
                if (!string.IsNullOrWhiteSpace(locoInfo?.BodyPhoto2)) bodyPhotosList.Add(locoInfo.BodyPhoto2);
                if (!string.IsNullOrWhiteSpace(locoInfo?.BodyPhoto3)) bodyPhotosList.Add(locoInfo.BodyPhoto3);
                if (!bodyPhotosList.Any()) bodyPhotosList.Add("No Photos");
            }
            else
            {
                bodyPhotosList.Add("No Photos");
            }

            string bodyPhotosSerialized = JsonSerializer.Serialize(bodyPhotosList);

            var missingPhotosAll = new List<string>();
            var replacePhotosAll = new List<string>();

            static bool TryParseDecimal(string? s, out decimal value)
            {
                value = 0m;
                if (string.IsNullOrWhiteSpace(s)) return false;
                return decimal.TryParse(s, NumberStyles.Currency | NumberStyles.Number, CultureInfo.InvariantCulture, out value)
                    || decimal.TryParse(s, NumberStyles.Currency | NumberStyles.Number, CultureInfo.CurrentCulture, out value);
            }

            var multiEntryTables = new List<Func<int, Task<List<InspectLocoRow>>>>();

            if (locoInfo?.LocoModel == "E18")
            {
                multiEntryTables =
[
    num => GetMultiAsync<E18beinspect>(num),
    num => GetMultiAsync<E18ccinspect>(num),
    num => GetMultiAsync<E18bdinspect>(num),
    num => GetMultiAsync<E18crinspect>(num),
    num => GetMultiAsync<E18ctinspect>(num),
    num => GetMultiAsync<E18eeinspect>(num),
    num => GetMultiAsync<E18ehinspect>(num),
    num => GetMultiAsync<E18esinspect>(num),
    num => GetMultiAsync<E18flinspect>(num),
    num => GetMultiAsync<E18hcinspect>(num),
    num => GetMultiAsync<E18rfinspect>(num),
    num => GetMultiAsync<E18hvinspect>(num),
    num => GetMultiAsync<E18hsinspect>(num),
    num => GetMultiAsync<E18lvinspect>(num),
    num => GetMultiAsync<E18mainspect>(num),
    num => GetMultiAsync<E18mbinspect>(num),
];

            }
            else if (locoInfo?.LocoModel == "GE34")
            {
                multiEntryTables =
[
    num => GetMultiAsync<Ge34acinspect>(num),
    num => GetMultiAsync<Ge34bcinspect>(num),
    num => GetMultiAsync<Ge34bdinspect>(num),
    num => GetMultiAsync<Ge34bsinspect>(num),
    num => GetMultiAsync<Ge34cfinspect>(num),
    num => GetMultiAsync<Ge34clinspect>(num),
    num => GetMultiAsync<Ge34deinspect>(num),
    num => GetMultiAsync<Ge34ecinspect>(num),
    num => GetMultiAsync<Ge34flinspect>(num),
    num => GetMultiAsync<Ge34odinspect>(num),
    num => GetMultiAsync<Ge34rfinspect>(num),
    num => GetMultiAsync<Ge34sninspect>(num),
    num => GetMultiAsync<Ge34edinspect>(num),
];

            }
            else if (locoInfo?.LocoModel == "GE35")
            {
                multiEntryTables =
 [
     num => GetMultiAsync<Ge35mginspect>(num),
    num => GetMultiAsync<Ge35bcinspect>(num),
    num => GetMultiAsync<Ge35bdinspect>(num),
    num => GetMultiAsync<Ge35bsinspect>(num),
    num => GetMultiAsync<Ge35cfinspect>(num),
    num => GetMultiAsync<Ge35clinspect>(num),
    num => GetMultiAsync<Ge35deinspect>(num),
    num => GetMultiAsync<Ge35ecinspect>(num),
    num => GetMultiAsync<Ge35flinspect>(num),
    num => GetMultiAsync<Ge35odinspect>(num),
    num => GetMultiAsync<Ge35rfinspect>(num),
    num => GetMultiAsync<Ge35sninspect>(num),
    num => GetMultiAsync<Ge35edinspect>(num),
];

            }
            else if (locoInfo?.LocoModel == "GE36")
            {
                multiEntryTables =
 [
     num => GetMultiAsync<Ge36cainspect>(num),
    num => GetMultiAsync<Ge36mginspect>(num),
    num => GetMultiAsync<Ge36bdinspect>(num),
    num => GetMultiAsync<Ge36cfinspect>(num),
    num => GetMultiAsync<Ge36clinspect>(num),
    num => GetMultiAsync<Ge36deinspect>(num),
    num => GetMultiAsync<Ge36ecinspect>(num),
    num => GetMultiAsync<Ge36flinspect>(num),
    num => GetMultiAsync<Ge36rfinspect>(num),
    num => GetMultiAsync<Ge36sninspect>(num),
    num => GetMultiAsync<Ge36edinspect>(num),
];

            }
            else if (locoInfo?.LocoModel == "GM34")
            {
                multiEntryTables =
 [
     num => GetMultiAsync<Gm34blinspect>(num),
    num => GetMultiAsync<Gm34bsinspect>(num),
    num => GetMultiAsync<Gm34bdinspect>(num),
    num => GetMultiAsync<Gm34cainspect>(num),
    num => GetMultiAsync<Gm34cbinspect>(num),
    num => GetMultiAsync<Gm34cfinspect>(num),
    num => GetMultiAsync<Gm34clinspect>(num),
    num => GetMultiAsync<Gm34deinspect>(num),
    num => GetMultiAsync<Gm34flinspect>(num),
    num => GetMultiAsync<Gm34edinspect>(num),
    num => GetMultiAsync<Gm34rfinspect>(num),
    num => GetMultiAsync<Gm34elinspect>(num),
    num => GetMultiAsync<Gm34lminspect>(num),
    num => GetMultiAsync<Gm34mpinspect>(num),
    num => GetMultiAsync<Gm34sninspect>(num),
    num => GetMultiAsync<Gm34trinspect>(num),
];

            }
            else if (locoInfo?.LocoModel == "GM35")
            {
                multiEntryTables =
[
    num => GetMultiAsync<Gm35blinspect>(num),
    num => GetMultiAsync<Gm35bsinspect>(num),
    num => GetMultiAsync<Gm35wainspect>(num),
    num => GetMultiAsync<Gm35cainspect>(num),
    num => GetMultiAsync<Gm35cbinspect>(num),
    num => GetMultiAsync<Gm35cfinspect>(num),
    num => GetMultiAsync<Gm35clinspect>(num),
    num => GetMultiAsync<Gm35deinspect>(num),
    num => GetMultiAsync<Gm35flinspect>(num),
    num => GetMultiAsync<Gm35edinspect>(num),
    num => GetMultiAsync<Gm35rfinspect>(num),
    num => GetMultiAsync<Gm35elinspect>(num),
    num => GetMultiAsync<Gm35lminspect>(num),
    num => GetMultiAsync<Gm35mpinspect>(num),
    num => GetMultiAsync<Gm35sninspect>(num),
    num => GetMultiAsync<Gm35trinspect>(num),
];

            }
            else if (locoInfo?.LocoModel == "GM36")
            {
                multiEntryTables =
[
    num => GetMultiAsync<Gm36bpinspect>(num),
    num => GetMultiAsync<Gm36bsinspect>(num),
    num => GetMultiAsync<Gm36wainspect>(num),
    num => GetMultiAsync<Gm36cainspect>(num),
    num => GetMultiAsync<Gm36cbinspect>(num),
    num => GetMultiAsync<Gm36cfinspect>(num),
    num => GetMultiAsync<Gm36clinspect>(num),
    num => GetMultiAsync<Gm36deinspect>(num),
    num => GetMultiAsync<Gm36flinspect>(num),
    num => GetMultiAsync<Gm36edinspect>(num),
    num => GetMultiAsync<Gm36rfinspect>(num),
    num => GetMultiAsync<Gm36ecinspect>(num),
    num => GetMultiAsync<Gm36lminspect>(num),
    num => GetMultiAsync<Gm36bvinspect>(num),
    num => GetMultiAsync<Gm36sninspect>(num),
    num => GetMultiAsync<Gm36trinspect>(num),
    num => GetMultiAsync<Gm36clinspect>(num),
];

            }

            var results = new List<List<InspectLocoRow>>();

            foreach (var table in multiEntryTables)
            {
                results.Add(await table(locoNumber));
            }

            decimal refurbishTotalDec = 0;
            decimal missingTotalDec = 0;
            decimal replaceTotalDec = 0;
            decimal laborTotalDec = 0;

            foreach (var rows in results)
            {
                foreach (var r in rows)
                {
                    if (TryParseDecimal(r.RefurbishValue, out var rv)) refurbishTotalDec += rv;
                    if (TryParseDecimal(r.MissingValue, out var mv)) missingTotalDec += mv;
                    if (TryParseDecimal(r.ReplaceValue, out var xv)) replaceTotalDec += xv;
                    if (TryParseDecimal(r.LaborValue, out var lv)) laborTotalDec += lv;

                    if (!string.IsNullOrWhiteSpace(r.MissingPhoto) && r.MissingPhoto != "No Photo") missingPhotosAll.Add(r.MissingPhoto.Trim());
                    if (!string.IsNullOrWhiteSpace(r.ReplacePhoto) && r.ReplacePhoto != "No Photo") replacePhotosAll.Add(r.ReplacePhoto.Trim());
                }
            }

            missingPhotosAll = missingPhotosAll.Distinct().ToList();
            replacePhotosAll = replacePhotosAll.Distinct().ToList();

            // ---------- Totals ----------
            string refurbishTotal = refurbishTotalDec.ToString("0.00", CultureInfo.InvariantCulture);
            string missingTotal = missingTotalDec.ToString("0.00", CultureInfo.InvariantCulture);
            string replaceTotal = replaceTotalDec.ToString("0.00", CultureInfo.InvariantCulture);
            string laborTotal = laborTotalDec.ToString("0.00", CultureInfo.InvariantCulture);

            // ---------- Photos Serialization ----------
            string missingPhotosSerialized = missingPhotosAll.Any()
                ? JsonSerializer.Serialize(missingPhotosAll)
                : JsonSerializer.Serialize(new List<string> { "No Photos" });

            string replacePhotosSerialized = replacePhotosAll.Any()
                ? JsonSerializer.Serialize(replacePhotosAll)
                : JsonSerializer.Serialize(new List<string> { "No Photos" });

            decimal marketValue = 0;

            var masterValueStr = await _context.MasterLocos
                .AsNoTracking()
                .Where(m => m.LocoNumber == locoNumber)
                .Select(m => m.MarketValue)
                .FirstOrDefaultAsync();

            if (!string.IsNullOrWhiteSpace(masterValueStr))
            {
                decimal.TryParse(
                    masterValueStr,
                    NumberStyles.Currency | NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out marketValue
                );
            }
            else
            {
                var masterTfr = await _context.MasterLocosTFR
                    .AsNoTracking()
                    .Where(m => m.LocoNumber == locoNumber)
                    .Select(m => (decimal?)m.BenchmarkValue)
                    .FirstOrDefaultAsync();

                if (masterTfr != null)
                {
                    marketValue = masterTfr.Value;
                }   
                else
                {
                    var masterTe = await _context.MasterLocosTE
                    .AsNoTracking()
                    .Where(m => m.LocoNumber == locoNumber)
                    .Select(m => (decimal?)m.BenchmarkValue)
                    .FirstOrDefaultAsync();

                    if (masterTe != null)
                    {
                        marketValue += masterTe.Value;
                    }
                    else
                    {
                        return BadRequest("Asset/Loco not found in master data.");
                    }
                }   
            }

            decimal repairTotal = refurbishTotalDec + missingTotalDec + replaceTotalDec + laborTotalDec;
            decimal assetValue = marketValue - repairTotal;// Consider Asset Value as  Market Value due to client req

            string totalAssetValue = assetValue.ToString("0.00", CultureInfo.InvariantCulture);
            string repairTotalStr = repairTotal.ToString("0.00", CultureInfo.InvariantCulture);

            // ADD ↓
            decimal assetValueDec = assetValue;
            decimal marketValueDec = marketValue;
            int score = 0;

            if (assetValueDec < 0)
                score = 1;
            else if (assetValueDec >= Math.Round(marketValueDec * 0.90m, 2))
                score = 10;
            else if (assetValueDec >= Math.Round(marketValueDec * 0.80m, 2))
                score = 9;
            else if (assetValueDec >= Math.Round(marketValueDec * 0.70m, 2))
                score = 8;
            else if (assetValueDec >= Math.Round(marketValueDec * 0.60m, 2))
                score = 7;
            else if (assetValueDec >= Math.Round(marketValueDec * 0.50m, 2))
                score = 6;
            else if (assetValueDec >= Math.Round(marketValueDec * 0.40m, 2))
                score = 5;
            else if (assetValueDec >= Math.Round(marketValueDec * 0.30m, 2))
                score = 4;
            else if (assetValueDec >= Math.Round(marketValueDec * 0.20m, 2))
                score = 3;
            else if (assetValueDec >= Math.Round(marketValueDec * 0.10m, 2))
                score = 2;
            else
                score = 1;

            var condition = await _context.ConditionRatings
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x => x.Score == score);

            var dashboardEntry = new LocoDashboard
            {
                InspectorId = userId ?? "No User",
                InspectorName = inspectorName ?? "No User",
                LocoNumber = locoNumber,
                LocoClass = locoInfo?.LocoClass ?? string.Empty,
                LocoModel = locoInfo?.LocoModel ?? string.Empty,
                DateAssessed = DateTime.Now.ToString("yyyy-MM-dd"),
                TimeAssessed = DateTime.Now.ToString("HH:mm:ss"),
                BodyDamage = bodyDamage,
                BodyPhotos = bodyPhotosSerialized,
                RefurbishValue = refurbishTotal,
                MissingValue = missingTotal,
                ReplaceValue = replaceTotal,
                AssessmentQuote = "Not Ready",
                AssessmentCert = "Not Ready",
                UploadStatus = "Inspection Complete",
                UploadDate = "No Date",
                LocoPhoto = locoInfo?.LocoPhoto,
                MissingPhotos = missingPhotosSerialized,
                ReplacePhotos = replacePhotosSerialized,
                GpsLatitude = locoInfo?.GpsLatitude, 
                City = city, 
                GpsLongitude = locoInfo?.GpsLongitude, 
                TotalLaborValue = laborTotal,
                StartTimeInspect = locoInfo?.CreatedDate?.ToString("HH:mm:ss") ?? "Not Available", 
                ReplacementValue = "Not Available",
                AssetValue = totalAssetValue, 
                MarketValue = marketValue.ToString("0.00", CultureInfo.InvariantCulture),
                AssessmentSow = "Not Ready",
                TotalValue = repairTotalStr ?? "0.00",
                CalScore = score,
                CalOperateStatus = condition?.OperationalStatus ?? "Scrap Only",
                CalCondition = condition?.Condition ?? "Beyond Repair",
                Phase = locoInfo.Phase,
            };
            var existingLoco = await _context.LocoDashboards
                                            .FirstOrDefaultAsync(d => d.LocoNumber == locoNumber);
            if (existingLoco != null)
            {

            }
            else
            {
                _context.LocoDashboards.Add(dashboardEntry);
                await _context.SaveChangesAsync();
                var input = await _context.LocoInputs
                  .FirstOrDefaultAsync(e => e.LocoNumber == locoNumber);

                if (input != null)
                {
                    input.TotalCost = repairTotalStr ?? "0.00";
                    _context.LocoInputs.Update(input);
                }

            }
            return Ok(new { success = true, message = "Loco dashboard entry created", id = dashboardEntry.Id });
        }

        [HttpPost("recalculateLocoValues")]
        public async Task<IActionResult> RecalculateLocoValues(RecalculateRequest request)
        {
            try
            {
                int locoNumber = Convert.ToInt32(request.LocoNumber);

                var locoInfo = await _context.LocoInfoCaptures
                                              .Where(w => w.LocoNumber == locoNumber)
                                              .OrderByDescending(w => w.Id)
                                              .Select(w => new
                                              {
                                                  w.LocoModel
                                              })
                                              .FirstOrDefaultAsync();

                if (locoInfo == null)
                    return NotFound(new { success = false, message = $"No LocoInfoCaptures record found for loco {locoNumber}" });

                static bool TryParseDecimal(string? s, out decimal value)
                {
                    value = 0m;
                    if (string.IsNullOrWhiteSpace(s)) return false;
                    return decimal.TryParse(s, NumberStyles.Currency | NumberStyles.Number, CultureInfo.InvariantCulture, out value)
                        || decimal.TryParse(s, NumberStyles.Currency | NumberStyles.Number, CultureInfo.CurrentCulture, out value);
                }

                var multiEntryTables = new List<Func<int, Task<List<InspectLocoRow>>>>();

                if (locoInfo?.LocoModel == "E18")
                {
                    multiEntryTables =
    [
        num => GetMultiAsync<E18beinspect>(num),
    num => GetMultiAsync<E18ccinspect>(num),
    num => GetMultiAsync<E18bdinspect>(num),
    num => GetMultiAsync<E18crinspect>(num),
    num => GetMultiAsync<E18ctinspect>(num),
    num => GetMultiAsync<E18eeinspect>(num),
    num => GetMultiAsync<E18ehinspect>(num),
    num => GetMultiAsync<E18esinspect>(num),
    num => GetMultiAsync<E18flinspect>(num),
    num => GetMultiAsync<E18hcinspect>(num),
    num => GetMultiAsync<E18rfinspect>(num),
    num => GetMultiAsync<E18hvinspect>(num),
    num => GetMultiAsync<E18hsinspect>(num),
    num => GetMultiAsync<E18lvinspect>(num),
    num => GetMultiAsync<E18mainspect>(num),
    num => GetMultiAsync<E18mbinspect>(num),
];

                }
                else if (locoInfo?.LocoModel == "GE34")
                {
                    multiEntryTables =
    [
        num => GetMultiAsync<Ge34acinspect>(num),
    num => GetMultiAsync<Ge34bcinspect>(num),
    num => GetMultiAsync<Ge34bdinspect>(num),
    num => GetMultiAsync<Ge34bsinspect>(num),
    num => GetMultiAsync<Ge34cfinspect>(num),
    num => GetMultiAsync<Ge34clinspect>(num),
    num => GetMultiAsync<Ge34deinspect>(num),
    num => GetMultiAsync<Ge34ecinspect>(num),
    num => GetMultiAsync<Ge34flinspect>(num),
    num => GetMultiAsync<Ge34odinspect>(num),
    num => GetMultiAsync<Ge34rfinspect>(num),
    num => GetMultiAsync<Ge34sninspect>(num),
    num => GetMultiAsync<Ge34edinspect>(num),
];

                }
                else if (locoInfo?.LocoModel == "GE35")
                {
                    multiEntryTables =
     [
         num => GetMultiAsync<Ge35mginspect>(num),
    num => GetMultiAsync<Ge35bcinspect>(num),
    num => GetMultiAsync<Ge35bdinspect>(num),
    num => GetMultiAsync<Ge35bsinspect>(num),
    num => GetMultiAsync<Ge35cfinspect>(num),
    num => GetMultiAsync<Ge35clinspect>(num),
    num => GetMultiAsync<Ge35deinspect>(num),
    num => GetMultiAsync<Ge35ecinspect>(num),
    num => GetMultiAsync<Ge35flinspect>(num),
    num => GetMultiAsync<Ge35odinspect>(num),
    num => GetMultiAsync<Ge35rfinspect>(num),
    num => GetMultiAsync<Ge35sninspect>(num),
    num => GetMultiAsync<Ge35edinspect>(num),
];

                }
                else if (locoInfo?.LocoModel == "GE36")
                {
                    multiEntryTables =
     [
         num => GetMultiAsync<Ge36cainspect>(num),
    num => GetMultiAsync<Ge36mginspect>(num),
    num => GetMultiAsync<Ge36bdinspect>(num),
    num => GetMultiAsync<Ge36cfinspect>(num),
    num => GetMultiAsync<Ge36clinspect>(num),
    num => GetMultiAsync<Ge36deinspect>(num),
    num => GetMultiAsync<Ge36ecinspect>(num),
    num => GetMultiAsync<Ge36flinspect>(num),
    num => GetMultiAsync<Ge36rfinspect>(num),
    num => GetMultiAsync<Ge36sninspect>(num),
    num => GetMultiAsync<Ge36edinspect>(num),
];

                }
                else if (locoInfo?.LocoModel == "GM34")
                {
                    multiEntryTables =
     [
         num => GetMultiAsync<Gm34blinspect>(num),
    num => GetMultiAsync<Gm34bsinspect>(num),
    num => GetMultiAsync<Gm34bdinspect>(num),
    num => GetMultiAsync<Gm34cainspect>(num),
    num => GetMultiAsync<Gm34cbinspect>(num),
    num => GetMultiAsync<Gm34cfinspect>(num),
    num => GetMultiAsync<Gm34clinspect>(num),
    num => GetMultiAsync<Gm34deinspect>(num),
    num => GetMultiAsync<Gm34flinspect>(num),
    num => GetMultiAsync<Gm34edinspect>(num),
    num => GetMultiAsync<Gm34rfinspect>(num),
    num => GetMultiAsync<Gm34elinspect>(num),
    num => GetMultiAsync<Gm34lminspect>(num),
    num => GetMultiAsync<Gm34mpinspect>(num),
    num => GetMultiAsync<Gm34sninspect>(num),
    num => GetMultiAsync<Gm34trinspect>(num),
];

                }
                else if (locoInfo?.LocoModel == "GM35")
                {
                    multiEntryTables =
    [
        num => GetMultiAsync<Gm35blinspect>(num),
    num => GetMultiAsync<Gm35bsinspect>(num),
    num => GetMultiAsync<Gm35wainspect>(num),
    num => GetMultiAsync<Gm35cainspect>(num),
    num => GetMultiAsync<Gm35cbinspect>(num),
    num => GetMultiAsync<Gm35cfinspect>(num),
    num => GetMultiAsync<Gm35clinspect>(num),
    num => GetMultiAsync<Gm35deinspect>(num),
    num => GetMultiAsync<Gm35flinspect>(num),
    num => GetMultiAsync<Gm35edinspect>(num),
    num => GetMultiAsync<Gm35rfinspect>(num),
    num => GetMultiAsync<Gm35elinspect>(num),
    num => GetMultiAsync<Gm35lminspect>(num),
    num => GetMultiAsync<Gm35mpinspect>(num),
    num => GetMultiAsync<Gm35sninspect>(num),
    num => GetMultiAsync<Gm35trinspect>(num),
];

                }
                else if (locoInfo?.LocoModel == "GM36")
                {
                    multiEntryTables =
    [
        num => GetMultiAsync<Gm36bpinspect>(num),
    num => GetMultiAsync<Gm36bsinspect>(num),
    num => GetMultiAsync<Gm36wainspect>(num),
    num => GetMultiAsync<Gm36cainspect>(num),
    num => GetMultiAsync<Gm36cbinspect>(num),
    num => GetMultiAsync<Gm36cfinspect>(num),
    num => GetMultiAsync<Gm36clinspect>(num),
    num => GetMultiAsync<Gm36deinspect>(num),
    num => GetMultiAsync<Gm36flinspect>(num),
    num => GetMultiAsync<Gm36edinspect>(num),
    num => GetMultiAsync<Gm36rfinspect>(num),
    num => GetMultiAsync<Gm36ecinspect>(num),
    num => GetMultiAsync<Gm36lminspect>(num),
    num => GetMultiAsync<Gm36bvinspect>(num),
    num => GetMultiAsync<Gm36sninspect>(num),
    num => GetMultiAsync<Gm36trinspect>(num),
    num => GetMultiAsync<Gm36clinspect>(num),
];

                }

                var results = new List<List<InspectLocoRow>>();

                foreach (var table in multiEntryTables)
                {
                    results.Add(await table(locoNumber));
                }

                decimal refurbishTotalDec = 0;
                decimal missingTotalDec = 0;
                decimal replaceTotalDec = 0;
                decimal laborTotalDec = 0;

                foreach (var rows in results)
                {
                    foreach (var r in rows)
                    {
                        if (TryParseDecimal(r.RefurbishValue, out var rv)) refurbishTotalDec += rv;
                        if (TryParseDecimal(r.MissingValue, out var mv)) missingTotalDec += mv;
                        if (TryParseDecimal(r.ReplaceValue, out var xv)) replaceTotalDec += xv;
                        if (TryParseDecimal(r.LaborValue, out var lv)) laborTotalDec += lv;
                    }
                }

                string refurbishTotal = refurbishTotalDec.ToString("0.00", CultureInfo.InvariantCulture);
                string missingTotal = missingTotalDec.ToString("0.00", CultureInfo.InvariantCulture);
                string replaceTotal = replaceTotalDec.ToString("0.00", CultureInfo.InvariantCulture);
                string laborTotal = laborTotalDec.ToString("0.00", CultureInfo.InvariantCulture);

                decimal marketValue = 0;

                var masterValueStr = await _context.MasterLocos
                    .AsNoTracking()
                    .Where(m => m.LocoNumber == locoNumber)
                    .Select(m => m.MarketValue)
                    .FirstOrDefaultAsync();

                if (!string.IsNullOrWhiteSpace(masterValueStr))
                {
                    decimal.TryParse(
                        masterValueStr,
                        NumberStyles.Currency | NumberStyles.Number,
                        CultureInfo.InvariantCulture,
                        out marketValue
                    );
                }
                else
                {
                    var masterTfr = await _context.MasterLocosTFR
                        .AsNoTracking()
                        .Where(m => m.LocoNumber == locoNumber)
                        .Select(m => (decimal?)m.BenchmarkValue)
                        .FirstOrDefaultAsync();

                    if (masterTfr != null)
                    {
                        marketValue = masterTfr.Value;
                    }
                    else
                    {
                        var masterTe = await _context.MasterLocosTE
                        .AsNoTracking()
                        .Where(m => m.LocoNumber == locoNumber)
                        .Select(m => (decimal?)m.BenchmarkValue)
                        .FirstOrDefaultAsync();

                        if (masterTe != null)
                        {
                            marketValue += masterTe.Value;
                        }
                        else
                        {
                            return BadRequest("Asset/Loco not found in master data.");
                        }
                    }
                }

                decimal repairTotal = refurbishTotalDec + missingTotalDec + replaceTotalDec + laborTotalDec;
                decimal assetValue = marketValue - repairTotal;

                string totalAssetValue = assetValue.ToString("0.00", CultureInfo.InvariantCulture);
                string repairTotalStr = repairTotal.ToString("0.00", CultureInfo.InvariantCulture);

                decimal assetValueDec = assetValue;
                decimal marketValueDec = marketValue;
                int score = 0;

                if (assetValueDec < 0)
                    score = 1;
                else if (assetValueDec >= Math.Round(marketValueDec * 0.90m, 2))
                    score = 10;
                else if (assetValueDec >= Math.Round(marketValueDec * 0.80m, 2))
                    score = 9;
                else if (assetValueDec >= Math.Round(marketValueDec * 0.70m, 2))
                    score = 8;
                else if (assetValueDec >= Math.Round(marketValueDec * 0.60m, 2))
                    score = 7;
                else if (assetValueDec >= Math.Round(marketValueDec * 0.50m, 2))
                    score = 6;
                else if (assetValueDec >= Math.Round(marketValueDec * 0.40m, 2))
                    score = 5;
                else if (assetValueDec >= Math.Round(marketValueDec * 0.30m, 2))
                    score = 4;
                else if (assetValueDec >= Math.Round(marketValueDec * 0.20m, 2))
                    score = 3;
                else if (assetValueDec >= Math.Round(marketValueDec * 0.10m, 2))
                    score = 2;
                else
                    score = 1;

                var condition = await _context.ConditionRatings
                            .AsNoTracking()
                            .FirstOrDefaultAsync(x => x.Score == score);

                var dash = await _context.LocoDashboards
                    .FirstOrDefaultAsync(d => d.LocoNumber == locoNumber);

                if (dash == null)
                    return BadRequest("Loco does not exist");

                dash.RefurbishValue = refurbishTotal;
                dash.MissingValue = missingTotal;
                dash.ReplaceValue = replaceTotal;
                dash.TotalLaborValue = laborTotal;
                dash.AssetValue = totalAssetValue ?? "";
                dash.TotalValue = repairTotalStr ?? "";
                dash.CalScore = score;
                dash.CalOperateStatus = condition?.OperationalStatus ?? "Not Captured";
                dash.CalCondition = condition?.Condition ?? "Not Captured";

                _context.LocoDashboards.Update(dash);
                //var input = await _context.LocoInputs
                //   .FirstOrDefaultAsync(e => e.LocoNumber == locoNumber);

                //if (input != null)
                //{
                //    input.TotalCost = repairTotalStr ?? "0.00";
                //    _context.LocoInputs.Update(input);
                //}
                await _context.SaveChangesAsync();

                return Ok(new { message = "Loco updated successfully." });
            }
            catch (Exception ex)
            {
                var stackTrace = new System.Diagnostics.StackTrace(ex, true);
                var frame = stackTrace.GetFrame(0);

                var lineNumber = frame?.GetFileLineNumber();
                var fileName = frame?.GetFileName();
                var method = frame?.GetMethod()?.Name;

                return StatusCode(500, new
                {
                    message = "An error occurred while recalculating loco values.",
                    error = ex.Message,
                    method,
                    file = fileName,
                    line = lineNumber,
                    innerError = ex.InnerException?.Message
                });
            }
        }

        private async Task<string> GetCityFromCoordinatesAsync(double latitude, double longitude)
        {
            string? apiKey = _config["LocationIQ:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
                return "Not Captured";

            var client = _httpClientFactory.CreateClient();

            string url =
                $"https://us1.locationiq.com/v1/reverse.php?key={apiKey}&lat={latitude.ToString(CultureInfo.InvariantCulture)}&lon={longitude.ToString(CultureInfo.InvariantCulture)}&format=json";

            const int maxRetries = 3;
            int delayMs = 500; // initial delay

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    using (var resp = await client.GetAsync(url))
                    {
                        if (resp.IsSuccessStatusCode)
                        {
                            string json = await resp.Content.ReadAsStringAsync();
                            var obj = JObject.Parse(json);

                            string? city =
                                obj["address"]?["city"]?.ToString()
                                ?? obj["address"]?["town"]?.ToString()
                                ?? obj["address"]?["village"]?.ToString()
                                ?? obj["address"]?["county"]?.ToString();

                            return string.IsNullOrWhiteSpace(city) ? "Not Captured" : city;
                        }

                        // If API rate limit exceeded → wait longer
                        if (resp.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                        {
                            await Task.Delay(2000 * attempt);
                            continue;
                        }
                    }
                }
                catch (Exception Ex)
                {

                    // Network failure → retry
                }

                // Delay before next retry (exponential)
                await Task.Delay(delayMs);
                delayMs *= 2; // 500 → 1000 → 2000
            }

            return "Not Captured";
        }

        // ADD ↓
        private async Task<List<InspectLocoRow>> GetMultiAsync<TEntity>(int locoNumber)
        where TEntity : class, IInspectLocoEntity
        {
            return await _context.Set<TEntity>()
                .Where(e => e.LocoNumber == locoNumber)
                .OrderByDescending(e => e.Id)
                .Select(e => new InspectLocoRow
                {
                    RefurbishValue = e.RefurbishValue,
                    MissingValue = e.MissingValue,
                    ReplaceValue = e.ReplaceValue,
                    MissingPhoto = e.MissingPhoto,
                    ReplacePhoto = e.ReplacePhoto,
                    LaborValue = e.LaborValue
                })
                .ToListAsync();
        }

        [HttpGet("getScoreList")]
        public async Task<IActionResult> GetScoreList()
        {
            var score = await _context.ConditionRatings
                .Select(s => new
                {
                    ConditionScore = s.Score.ToString(),
                    s.Condition
                })
                .ToListAsync();

            return Ok(score);
        }

        // ADJUST ↓
        [HttpGet("getAllLocoDashboard")]
        public async Task<IActionResult> GetAllLocoDashboard()
        {
            try
            {
                var dashboardEntries = await _context.LocoDashboards
                    .Where(w => w.UploadStatus != "Uploaded")
                    .Select(w => new
                    {
                        w.Id,
                        w.InspectorId,
                        w.InspectorName,
                        w.LocoNumber,
                        w.LocoClass,
                        w.LocoModel,
                        w.DateAssessed,
                        w.TimeAssessed,
                        w.BodyDamage,
                        w.BodyPhotos,
                        w.RefurbishValue,
                        w.MissingValue,
                        w.ReplaceValue,
                        w.AssessmentQuote,
                        w.AssessmentCert,
                        w.UploadStatus,
                        w.City,
                        w.UploadDate,
                        w.LocoPhoto,
                        w.MissingPhotos,
                        w.ReplacePhotos,
                        w.GpsLatitude,
                        w.GpsLongitude,
                        w.StartTimeInspect,
                        w.AssetValue,
                        w.TotalValue,
                        w.AssessmentSow,
                        w.MarketValue,
                        w.TotalLaborValue,
                        ConditionScore = w.ConditionScore.ToString() ?? "", 
                        OperationalStatus = w.OperationalStatus ?? "",
                        CalScore = w.CalScore.ToString() ?? "",
                        w.CalOperateStatus,
                        w.CalCondition,
                        Phase = w.Phase.ToString(),
                    })
                    .ToListAsync();

                return Ok(dashboardEntries);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = "An error occurred while retrieving the LocoDashboard entries.", error = ex.Message });//Detailed error for debugging  
            }
        }

        [HttpPost("updateLocoCondition")]
        public async Task<IActionResult> UpdateLocoCondition([FromBody] LocoConditionRequest request)
        {
            var dash = await _context.LocoDashboards
                .FirstOrDefaultAsync(d => d.LocoNumber == Convert.ToInt32(request.LocoNumber));

            if (dash != null)
            {
                try
                {
                    var score = await _context.ConditionRatings
                        .AsNoTracking()
                        .FirstOrDefaultAsync(s => s.Score == Convert.ToInt32(request.ConditionScore));

                    if (score == null)
                        return BadRequest("Score does not exist");

                    string operatingStatus = score.OperationalStatus;

                    dash.ConditionScore = Convert.ToInt32(request.ConditionScore);
                    dash.OperationalStatus = operatingStatus;

                    _context.LocoDashboards.Update(dash);

                    await _context.SaveChangesAsync();

                    return Ok(new { message = "Loco input updated successfully." });
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { error = "Update failed.", detail = ex.Message });
                }
            }
            else
            {
                return BadRequest("Wagon does not exist");
            }
        }

        [HttpPost("uploadLocos")]
        public async Task<IActionResult> UploadLocos([FromBody] List<UploadLocoItem> items)
        {
            try
            {
                if (items == null || !items.Any())
                    return BadRequest("No locos selected for upload.");
                string serverFolder = @"C:\LocoDashboardItemsUploaded";
                var dashboard = await _context.LocoDashboards.FirstOrDefaultAsync(w => w.LocoNumber == items[0].LocoNumber);
                if (dashboard != null)
                {
                    if(dashboard.Phase==2)
                    {
                        serverFolder = @"C:\TFR_LocoDashboardItemsUploaded";
                    }
                    if (dashboard.Phase == 3)
                    {
                        serverFolder = @"C:\TE_LocoDashboardItemsUploaded";
                    }
                }
                    if (!Directory.Exists(serverFolder))
                    Directory.CreateDirectory(serverFolder);

                //PLEASE ADD
                // --- Create ZIP file name including wagon numbers ---
                string wagonNumbersPart = string.Join("_", items.Select(i => i.LocoNumber));
                string zipName = $"LocoDashboardUpload_{wagonNumbersPart}_{DateTime.Now:yyyyMMdd_HHmmss}.zip";
                string zipPath = Path.Combine(serverFolder, zipName);

                //string tempRoot = Path.Combine(_env.WebRootPath ?? "wwwroot", "LocoUploads");
                //if (!Directory.Exists(tempRoot)) Directory.CreateDirectory(tempRoot);

                //string zipName = $"LocoDashboardUpload_{DateTime.Now:yyyyMMdd_HHmmss}.zip";
                //string zipPath = Path.Combine(tempRoot, zipName);

                using (var zipArchive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
                {
                    foreach (var item in items)
                    {
                        string wagonFolderName = $"{item.LocoNumber}_Dash_{DateTime.Now:yyyyMMdd_HHmmss}";

                        // Mapping folder names for categories
                        var folderMap = new Dictionary<string, string>
                        {
                            { "BodyPhotos", Path.Combine(wagonFolderName, "InfoCapturePhotos") },
                            { "LocoPhoto", Path.Combine(wagonFolderName, "InfoCapturePhotos") },
                            { "MissingPhotos", Path.Combine(wagonFolderName, "InspectionPhotos") },
                            { "ReplacePhotos", Path.Combine(wagonFolderName, "InspectionPhotos") },
                            { "AssessmentQuote", Path.Combine(wagonFolderName, "InspectionQuote") },
                            { "AssessmentCert", Path.Combine(wagonFolderName, "InspectionCert") },
                            { "AssessmentSow", Path.Combine(wagonFolderName, "InspectionSow") }
                        };

                        async Task AddFilesToZipAsync(string? source, string targetFolder)
                        {
                            if (string.IsNullOrWhiteSpace(source) || source == "N/A") return;

                            List<string> paths = new();
                            if (source.StartsWith("["))
                            {
                                var deserialized = JsonSerializer.Deserialize<List<string>>(source);
                                if (deserialized != null) paths.AddRange(deserialized);
                            }
                            else
                            {
                                paths.Add(source);
                            }

                            foreach (var p in paths)
                            {
                                if (string.IsNullOrWhiteSpace(p) || p == "No Photos" || p == "N/A") continue;

                                string sourcePath = Path.Combine(_env.WebRootPath ?? "wwwroot", p.TrimStart('/'));
                                if (!System.IO.File.Exists(sourcePath)) continue;

                                string entryName = Path.Combine(targetFolder, Path.GetFileName(sourcePath));

                                var entry = zipArchive.CreateEntry(entryName, CompressionLevel.SmallestSize);
                                await using var entryStream = entry.Open();

                                if (IsImage(sourcePath))
                                {
                                    // REAL compression happens here
                                    await using var processedImage = await PreprocessImageAsync(sourcePath);
                                    await processedImage.CopyToAsync(entryStream);
                                }
                                else
                                {
                                    // Non-image files copied as-is
                                    await using var fileStream = System.IO.File.OpenRead(sourcePath);
                                    await fileStream.CopyToAsync(entryStream);
                                }
                            }
                        }

                        // Use reflection to loop through all properties dynamically
                        var properties = typeof(UploadLocoItem).GetProperties();
                        foreach (var prop in properties)
                        {
                            if (!folderMap.ContainsKey(prop.Name)) continue;

                            var value = prop.GetValue(item) as string;
                            await AddFilesToZipAsync(value, folderMap[prop.Name]);
                        }

                        // Update DB
                        var dashboardEntry = await _context.LocoDashboards.FirstOrDefaultAsync(w => w.LocoNumber == item.LocoNumber);
                        if (dashboardEntry != null)
                        {
                            dashboardEntry.UploadStatus = "Uploaded";
                            dashboardEntry.UploadDate = DateTime.Now.ToString("yyyy-MM-dd");
                        }

                        await _context.SaveChangesAsync();
                    }
                }

                return Ok(new { success = true, zipPath, zipName });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("markReadyForAssessmentLoco")]
        public async Task<IActionResult> MarkReadyForAssessmentLoco([FromBody] LocoStatusUpdateDto dto)
        {
            var wagon = await _context.LocoDashboards
                .FirstOrDefaultAsync(w => w.LocoNumber == dto.LocoNumber);

            if (wagon == null)
                return NotFound("Loco not found.");

            if (wagon.UploadStatus != "Inspection Complete")
                return BadRequest(
                    $"Invalid transition. Current status is {wagon.UploadStatus}");

            wagon.UploadStatus = "ReadyForAssessment";

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Wagon moved to Ready For Assessment",
                wagon.LocoNumber,
                wagon.UploadStatus
            });
        }

        [HttpPost("markAssessedReadyForUploadLoco")]
        public async Task<IActionResult> MarkAssessedReadyForUploadLoco([FromBody] LocoStatusUpdateDto dto)
        {
            var wagon = await _context.LocoDashboards
                .FirstOrDefaultAsync(w => w.LocoNumber == dto.LocoNumber);

            if (wagon == null)
                return NotFound("Loco not found.");

            if (wagon.UploadStatus != "ReadyForAssessment")
                return BadRequest(
                    $"Invalid transition. Current status is {wagon.UploadStatus}");

            wagon.UploadStatus = "AssessedReadyForUpload";


            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Loco approved for upload",
                wagon.LocoNumber,
                wagon.UploadStatus
            });
        }

        [HttpGet("checkLocoInputs/{locoNumber}")]
        public async Task<IActionResult> CheckLocoInputs(int locoNumber)
        {
            bool exists = await _context.LocoInputs
                .AnyAsync(e => e.LocoNumber == locoNumber);

            if (exists)
            {
                return Ok(new { message = "Yes" });
            }
            //else
            //{
            //    bool manualData = _context.ManualDcfinputs.Any(c => c.AssetNumber == locoNumber);
            //    if (manualData)
            //    {
            //        return Ok(new { message = "Yes" });
            //    }
            //}

            return Ok(new { message = "No" });
        }

        [HttpPost("getUploadedLocosPaged")]
        public async Task<IActionResult> GetUploadedLocosPaged([FromBody] LocoDashboardQueryDto query)
        {
            _context.Database.SetCommandTimeout(180);

            IQueryable<LocoDashboard> q = _context.LocoDashboards
                .Where(x => x.UploadStatus == "Uploaded");

            // Global search
            if (!string.IsNullOrWhiteSpace(query.GlobalFilter))
            {
                string filter = query.GlobalFilter.ToLower();

                q = q.Where(x =>
                    (x.LocoNumber != null &&
                     x.LocoNumber.ToString().ToLower().Contains(filter)) ||

                    (!string.IsNullOrEmpty(x.LocoClass) &&
                     x.LocoClass.ToLower().Contains(filter)) ||

                    (!string.IsNullOrEmpty(x.InspectorName) &&
                     x.InspectorName.ToLower().Contains(filter))
                );
            }

            int totalRecords = await q.CountAsync();

            List<LocoDashboard> data = await q
                .OrderByDescending(x => x.UploadDate)
                .Skip(query.First)
                .Take(query.Rows)
                .ToListAsync();

            return Ok(new PagedResult<LocoDashboard>
            {
                TotalRecords = totalRecords,
                Data = data
            });
        }

        [HttpPost("getUploadedLocosForExport")]
        public async Task<IActionResult> GetUploadedLocosForExport([FromBody] LocoDashboardQueryDto query)
        {
            _context.Database.SetCommandTimeout(180);

            IQueryable<LocoDashboard> q = _context.LocoDashboards
                .AsNoTracking()
                .Where(x => x.UploadStatus == "Uploaded");

            // Global search
            if (!string.IsNullOrWhiteSpace(query.GlobalFilter))
            {
                string filter = $"%{query.GlobalFilter.Trim()}%";

                q = q.Where(x =>
                    (x.LocoNumber != null &&
                     EF.Functions.Like(x.LocoNumber.ToString(), filter)) ||

                    (!string.IsNullOrEmpty(x.LocoClass) &&
                     EF.Functions.Like(x.LocoClass, filter)) ||

                    (!string.IsNullOrEmpty(x.InspectorName) &&
                     EF.Functions.Like(x.InspectorName, filter))
                );
            }

            var data = await q
                .OrderByDescending(x => x.UploadDate)
                .ToListAsync();

            return Ok(new
            {
                totalRecords = data.Count,
                data
            });
        }

        private static bool IsImage(string path)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            return ext == ".jpg" || ext == ".jpeg" || ext == ".png";
        }

        private static async Task<Stream> PreprocessImageAsync(string sourcePath)
        {
            byte[] originalBytes = await System.IO.File.ReadAllBytesAsync(sourcePath);

            using var image = await SixLabors.ImageSharp.Image.LoadAsync(sourcePath);

            // Resize only if larger than 1920px
            bool resized = false;
            if (image.Width > 1920)
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(1920, 0)
                }));
                resized = true;
            }

            StripImageMetadata(image);

            var output = new MemoryStream();
            string ext = Path.GetExtension(sourcePath).ToLowerInvariant();

            if (ext == ".png")
            {
                // PNG: only re-encode if resized (otherwise keep original)
                if (!resized)
                    return new MemoryStream(originalBytes);

                var pngEncoder = new SixLabors.ImageSharp.Formats.Png.PngEncoder
                {
                    CompressionLevel = SixLabors.ImageSharp.Formats.Png.PngCompressionLevel.Level6
                };

                await image.SaveAsync(output, pngEncoder);
            }
            else
            {
                // JPEG: metadata stripped + moderate quality
                var jpegEncoder = new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder
                {
                    Quality = 82
                };

                await image.SaveAsync(output, jpegEncoder);
            }

            if (output.Length >= originalBytes.Length)
                return new MemoryStream(originalBytes);

            output.Position = 0;
            return output;
        }

        private static void StripImageMetadata(SixLabors.ImageSharp.Image image)
        {
            // Remove EXIF
            image.Metadata.ExifProfile = null;

            // Remove IPTC (sometimes present)
            image.Metadata.IptcProfile = null;

            // Remove XMP (can be large)
            image.Metadata.XmpProfile = null;
        }

        public class InspectLocoRow
        {
            public string? RefurbishValue { get; set; }
            public string? MissingValue { get; set; }
            public string? ReplaceValue { get; set; }
            public string? MissingPhoto { get; set; }
            public string? ReplacePhoto { get; set; }
            public string? LaborValue { get; set; }
        }

        public class LocoDashboardQueryDto
        {
            public int First { get; set; }
            public int Rows { get; set; }
            public string? GlobalFilter { get; set; }
        }

        public class PagedResult<T>
        {
            public int TotalRecords { get; set; }
            public List<T>? Data { get; set; }
        }

        public class LocoConditionRequest
        {
            public string LocoNumber { get; set; } = string.Empty;
            public int ConditionScore { get; set; }
        }

        public class ConditionRequestUpload
        {
            public string LocoNumber { get; set; } = string.Empty;
            public int ConditionScore { get; set; }
        }

        public class RecalculateRequest
        {
            public string LocoNumber { get; set; } = string.Empty;
        }

        public class RecalculateRequestUpload
        {
            public string LocoNumber { get; set; } = string.Empty;
        }

        public class TickWagonRequest
        {
            public string LocoNumber { get; set; } = string.Empty;
        }

        public class LocoStatusUpdateDto
        {
            public int LocoNumber { get; set; }
        }
    }
}
