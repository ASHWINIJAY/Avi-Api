using AviAppFinal.Server.Models;
using DocumentFormat.OpenXml;
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
using System.Linq.Expressions;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Threading.Tasks;

using AviFinal.Api.Models;
namespace AviAppFinal.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WagonDashController : Controller
    {
        public class UploadRequestItem
        {
            public int WagonNumber { get; set; }
            public string? BodyPhotos { get; set; }
            public string? LiftPhoto { get; set; }
            public string? BarrelPhoto { get; set; }
            public string? BrakePhoto { get; set; }
            public string? AssessmentQuote { get; set; }
            public string? AssessmentCert { get; set; }
            public string? AssessmentSow { get; set; }
            public string? WagonPhoto { get; set; }
            public string? MissingPhotos { get; set; }
            public string? ReplacePhotos { get; set; }
        }

        private readonly AviDbContext _context;

        private readonly IWebHostEnvironment _env;

        private readonly IConfiguration _config;

        private readonly IHttpClientFactory _httpClientFactory;

        public WagonDashController(AviDbContext context, IWebHostEnvironment env, IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _env = env;
            _config = config;
            _httpClientFactory = httpClientFactory;
        }

        // ADJUST ↓
        [HttpPost("insertWagon")]
        public async Task<IActionResult> InsertWagon(int wagonNumber, string userId)
        {
            try { 
            _context.Database.SetCommandTimeout(200);

            // ---------- Get User Name ----------
            var leaseUser = await _context.LeaseCoUsers
                                          .Where(u => u.UserId == userId)
                                          .Select(u => new { u.UserName })
                                          .FirstOrDefaultAsync();
            string inspectorName = leaseUser?.UserName ?? "No User";

            // ---------- Get WagonInfoCaptures ----------
            var wagonInfo = await _context.WagonInfoCaptures
                                          .Where(w => w.WagonNumber == wagonNumber)
                                          .OrderByDescending(w => w.Id)
                                          .Select(w => new
                                          {
                                              w.WagonGroup,
                                              w.WagonType,
                                              w.BodyDamage,
                                              w.BodyPhoto1,
                                              w.BodyPhoto2,
                                              w.BodyPhoto3,
                                              w.WagonPhoto,
                                              w.LiftPhoto,
                                              w.LiftDate,
                                              w.LiftLapsed,
                                              w.BarrelPhoto,
                                              w.BarrelDate,
                                              w.BarrelLapsed,
                                              w.BrakePhoto,
                                              w.BrakeDate,
                                              w.BrakeLapsed,
                                              w.NetBookValue,
                                              w.StartInspectTime,
                                              w.GpsLatitude,
                                              w.GpsLongitude,
                                              w.Phase,
                                          })
                                          .FirstOrDefaultAsync();

            if (wagonInfo == null)
                return NotFound(new { success = false, message = $"No WagonInfoCaptures record found for wagon {wagonNumber}" });

            string city = "Not Captured";

            if (double.TryParse(wagonInfo?.GpsLatitude, NumberStyles.Any, CultureInfo.InvariantCulture, out double latitude)
                && double.TryParse(wagonInfo?.GpsLongitude, NumberStyles.Any, CultureInfo.InvariantCulture, out double longitude))
            {
                var resolved = await GetCityFromCoordinatesAsync(latitude, longitude);
                if (!string.IsNullOrWhiteSpace(resolved) && !resolved.StartsWith("Error", StringComparison.InvariantCultureIgnoreCase))
                    city = resolved;
            }

            // ---------- Body Photos ----------
            string bodyDamage = wagonInfo?.BodyDamage ?? "No";
            List<string> bodyPhotosList = new();
            if (string.Equals(bodyDamage, "Yes", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(wagonInfo?.BodyPhoto1)) bodyPhotosList.Add(wagonInfo.BodyPhoto1);
                if (!string.IsNullOrWhiteSpace(wagonInfo?.BodyPhoto2)) bodyPhotosList.Add(wagonInfo.BodyPhoto2);
                if (!string.IsNullOrWhiteSpace(wagonInfo?.BodyPhoto3)) bodyPhotosList.Add(wagonInfo.BodyPhoto3);
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

            var multiEntryTables = new List<Func<int, Task<List<InspectRow>>>>
            {
                num => GetMultiAsync<WagonPartsInspect>(num),
                num => GetMultiAsync<AirBrakePartsInspect>(num),
                num => GetMultiAsync<VacBrakePartsInspect>(num)
            };

            var singleEntryTables = new List<Func<int, Task<List<InspectRow>>>>
            {
                num => GetSingleAsync<TankersInspect>(num),
                num => GetSingleAsync<BottomDischargeInspect>(num),
                num => GetSingleAsync<DoorsInspect>(num),
                num => GetSingleAsync<TwistlocksInspect>(num),
                num => GetSingleAsync<StanchionsInspect>(num),
                num => GetSingleAsync<FloorInspect>(num)
            };

            var results = new List<List<InspectRow>>();

            foreach (var table in multiEntryTables)
            {
                results.Add(await table(wagonNumber));
            }

            foreach (var table in singleEntryTables)
            {
                results.Add(await table(wagonNumber));
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

            // ---------- Ensure unique photos ----------
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

            decimal liftCost = wagonInfo?.LiftLapsed == "Yes" ? 420982 : 0;
            decimal barrelCost = wagonInfo?.BarrelLapsed == "Yes" ? 351893 : 0;

            decimal liftBarrelTotal = liftCost + barrelCost;

            decimal marketValue = 0;

                var master = await _context.MasterWagons
        .AsNoTracking()
        .FirstOrDefaultAsync(m => m.WagonNumber == wagonNumber);
                string masterValueStr="";
                if (master != null)
                {
                     masterValueStr = master?.MarketValue;
                }
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
                var masterTfr = await _context.MasterWagonsTFR
                    .AsNoTracking()
                    .Where(m => m.WagonNumber == wagonNumber)
                    .Select(m => (decimal?)m.BenchmarkValue)
                    .FirstOrDefaultAsync();

                if (masterTfr != null)
                    marketValue = masterTfr.Value;
                else
                    return BadRequest("Asset/Wagon not found in master data.");
            }

            decimal repairTotal = refurbishTotalDec + missingTotalDec + replaceTotalDec + laborTotalDec + liftBarrelTotal;
            decimal assetValue = marketValue - repairTotal;

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

            // ---------- Insert Dashboard ----------
            var dashboardEntry = new WagonDashboard
            {
                InspectorId = userId ?? "No User",
                InspectorName = inspectorName ?? "No User",
                WagonNumber = wagonNumber,
                WagonGroup = wagonInfo?.WagonGroup ?? string.Empty,
                WagonType = wagonInfo?.WagonType ?? string.Empty,
                DateAssessed = DateTime.Now.ToString("yyyy-MM-dd"),
                TimeAssessed = DateTime.Now.ToString("HH:mm:ss"),
                BodyDamage = bodyDamage,
                BodyPhotos = bodyPhotosSerialized,
                LiftPhoto = wagonInfo?.LiftPhoto,
                LiftDate = wagonInfo?.LiftDate,
                LiftLapsed = wagonInfo?.LiftLapsed,
                BarrelPhoto = wagonInfo?.BarrelPhoto,
                BarrelDate = wagonInfo?.BarrelDate,
                BarrelLapsed = wagonInfo?.BarrelLapsed,
                BrakePhoto = wagonInfo?.BrakePhoto,
                BrakeDate = wagonInfo?.BrakeDate,
                BrakeLapsed = wagonInfo?.BrakeLapsed,
                RefurbishValue = refurbishTotal,
                MissingValue = missingTotal,
                ReplaceValue = replaceTotal,
                AssessmentQuote = "Not Ready",
                AssessmentCert = "Not Ready",
                WagonStatus = "Inspection Complete",
                UploadDate = "No Date",
                WagonPhoto = wagonInfo?.WagonPhoto,
                MissingPhotos = missingPhotosSerialized,
                ReplacePhotos = replacePhotosSerialized,
                GpsLatitude = wagonInfo?.GpsLatitude,
                GpsLongitude = wagonInfo?.GpsLongitude,
                StartTimeInspect = wagonInfo?.StartInspectTime ?? "Not Available",
                MarketValue = marketValue.ToString("0.00", CultureInfo.InvariantCulture),
                TotalLaborValue = laborTotal,
                AssetValue = totalAssetValue ?? "0.00",
                AssessmentSow = "Not Ready",
                LiftValue = liftCost.ToString("0.00", CultureInfo.InvariantCulture),
                BarrelValue = barrelCost.ToString("0.00", CultureInfo.InvariantCulture),
                TotalValue = repairTotalStr ?? "0.00",
                CalScore = score,
                CalOperateStatus = condition?.OperationalStatus ?? "Scrap Only",
                CalCondition = condition?.Condition ?? "Beyond Repair",
                Phase = wagonInfo.Phase,
                City = city,
            };

            _context.WagonDashboards.Add(dashboardEntry);
                var input = await _context.WagonInputs
                   .FirstOrDefaultAsync(e => e.WagonNumber == wagonNumber);

                if (input != null)
                {
                    input.TotalCost = repairTotalStr ?? "0.00";
                    _context.WagonInputs.Update(input);
                }
                await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Wagon dashboard entry created", id = dashboardEntry.Id });
        }
            catch (Exception ex)
    {
                return StatusCode(500, new
                {
                    Message = ex.Message,
                    InnerMessage = ex.InnerException?.Message,
                    StackTrace = ex.StackTrace,
                    Source = ex.Source
                });
            }
        }

        // ADJUST ↓
        [HttpPost("recalculateValues")]
        public async Task<IActionResult> RecalculateValues(RecalculateRequest request)
        {
            int wagonNumber = Convert.ToInt32(request.WagonNumber);

            var wagonInfo = await _context.WagonInfoCaptures
                                          .Where(w => w.WagonNumber == wagonNumber)
                                          .OrderByDescending(w => w.Id)
                                          .Select(w => new
                                          {
                                              w.LiftLapsed,
                                              w.BarrelLapsed
                                          })
                                          .FirstOrDefaultAsync();

            if (wagonInfo == null)
                return NotFound(new { success = false, message = $"No WagonInfoCaptures record found for wagon {wagonNumber}" });

            static bool TryParseDecimal(string? s, out decimal value)
            {
                value = 0m;
                if (string.IsNullOrWhiteSpace(s)) return false;
                return decimal.TryParse(s, NumberStyles.Currency | NumberStyles.Number, CultureInfo.InvariantCulture, out value)
                    || decimal.TryParse(s, NumberStyles.Currency | NumberStyles.Number, CultureInfo.CurrentCulture, out value);
            }

            var multiEntryTables = new List<Func<int, Task<List<InspectRow>>>>
            {
                num => GetMultiAsync<WagonPartsInspect>(num),
                num => GetMultiAsync<AirBrakePartsInspect>(num),
                num => GetMultiAsync<VacBrakePartsInspect>(num)
            };

            var singleEntryTables = new List<Func<int, Task<List<InspectRow>>>>
            {
                num => GetSingleAsync<TankersInspect>(num),
                num => GetSingleAsync<BottomDischargeInspect>(num),
                num => GetSingleAsync<DoorsInspect>(num),
                num => GetSingleAsync<TwistlocksInspect>(num),
                num => GetSingleAsync<StanchionsInspect>(num),
                num => GetSingleAsync<FloorInspect>(num)
            };

            var results = new List<List<InspectRow>>();

            foreach (var table in multiEntryTables)
            {
                results.Add(await table(wagonNumber));
            }

            foreach (var table in singleEntryTables)
            {
                results.Add(await table(wagonNumber));
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

            // ---------- Totals ----------
            string refurbishTotal = refurbishTotalDec.ToString("0.00", CultureInfo.InvariantCulture);
            string missingTotal = missingTotalDec.ToString("0.00", CultureInfo.InvariantCulture);
            string replaceTotal = replaceTotalDec.ToString("0.00", CultureInfo.InvariantCulture);
            string laborTotal = laborTotalDec.ToString("0.00", CultureInfo.InvariantCulture);

            decimal liftCost = wagonInfo.LiftLapsed == "Yes" ? 420982 : 0;
            decimal barrelCost = wagonInfo.BarrelLapsed == "Yes" ? 351893 : 0;

            decimal liftBarrelTotal = liftCost + barrelCost;

            decimal marketValue = 0;

            var masterValueStr = await _context.MasterWagons
                .AsNoTracking()
                .Where(m => m.WagonNumber == wagonNumber)
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
                var masterTfr = await _context.MasterWagonsTFR
                    .AsNoTracking()
                    .Where(m => m.WagonNumber == wagonNumber)
                    .Select(m => (decimal?)m.BenchmarkValue)
                    .FirstOrDefaultAsync();

                if (masterTfr != null)
                    marketValue = masterTfr.Value;
                else
                    return BadRequest("Asset/Wagon not found in master data.");
            }

            decimal repairTotal = refurbishTotalDec + missingTotalDec + replaceTotalDec + laborTotalDec + liftBarrelTotal;
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
            var dash = await _context.WagonDashboards
                .FirstOrDefaultAsync(d => d.WagonNumber == wagonNumber);

            if (dash == null)
                return BadRequest("Wagon does not exist");

            dash.RefurbishValue = refurbishTotal;
            dash.MissingValue = missingTotal;
            dash.ReplaceValue = replaceTotal;
            dash.TotalLaborValue = laborTotal;
            dash.AssetValue = totalAssetValue ?? "";
            dash.LiftValue = liftCost.ToString("0.00", CultureInfo.InvariantCulture);
            dash.BarrelValue = barrelCost.ToString("0.00", CultureInfo.InvariantCulture);
            dash.TotalValue = repairTotalStr ?? "";
            dash.CalScore = score;
            dash.CalOperateStatus = condition?.OperationalStatus ?? "Not Captured";
            dash.CalCondition = condition?.Condition ?? "Not Captured";
            //var input = await _context.WagonInputs
            //    .FirstOrDefaultAsync(e => e.WagonNumber == wagonNumber);

            //if (input != null)
            //{
            //    input.TotalCost = repairTotalStr ?? "0.00";
            //    _context.WagonInputs.Update(input);
            //}
            _context.WagonDashboards.Update(dash);

            await _context.SaveChangesAsync();

            return Ok(new { message = "Wagon updated successfully." });
        }

        // ADJUST ↓
        [HttpPost("recalculateValuesUpload")]
        public async Task<IActionResult> RecalculateValuesUpload(RecalculateRequestUpload request)
        {
            int wagonNumber = Convert.ToInt32(request.WagonNumber);

            var wagonInfo = await _context.WagonInfoCaptures
                                          .Where(w => w.WagonNumber == wagonNumber)
                                          .OrderByDescending(w => w.Id)
                                          .Select(w => new
                                          {
                                              w.LiftLapsed,
                                              w.BarrelLapsed
                                          })
                                          .FirstOrDefaultAsync();

            if (wagonInfo == null)
                return NotFound(new { success = false, message = $"No WagonInfoCaptures record found for wagon {wagonNumber}" });

            static bool TryParseDecimal(string? s, out decimal value)
            {
                value = 0m;
                if (string.IsNullOrWhiteSpace(s)) return false;
                return decimal.TryParse(s, NumberStyles.Currency | NumberStyles.Number, CultureInfo.InvariantCulture, out value)
                    || decimal.TryParse(s, NumberStyles.Currency | NumberStyles.Number, CultureInfo.CurrentCulture, out value);
            }

            var multiEntryTables = new List<Func<int, Task<List<InspectRow>>>>
            {
                num => GetMultiAsync<WagonPartsInspect>(num),
                num => GetMultiAsync<AirBrakePartsInspect>(num),
                num => GetMultiAsync<VacBrakePartsInspect>(num)
            };

            var singleEntryTables = new List<Func<int, Task<List<InspectRow>>>>
            {
                num => GetSingleAsync<TankersInspect>(num),
                num => GetSingleAsync<BottomDischargeInspect>(num),
                num => GetSingleAsync<DoorsInspect>(num),
                num => GetSingleAsync<TwistlocksInspect>(num),
                num => GetSingleAsync<StanchionsInspect>(num),
                num => GetSingleAsync<FloorInspect>(num)
            };

            var results = new List<List<InspectRow>>();

            foreach (var table in multiEntryTables)
            {
                results.Add(await table(wagonNumber));
            }

            foreach (var table in singleEntryTables)
            {
                results.Add(await table(wagonNumber));
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

            // ---------- Totals ----------
            string refurbishTotal = refurbishTotalDec.ToString("0.00", CultureInfo.InvariantCulture);
            string missingTotal = missingTotalDec.ToString("0.00", CultureInfo.InvariantCulture);
            string replaceTotal = replaceTotalDec.ToString("0.00", CultureInfo.InvariantCulture);
            string laborTotal = laborTotalDec.ToString("0.00", CultureInfo.InvariantCulture);

            decimal liftCost = wagonInfo.LiftLapsed == "Yes" ? 420982 : 0;
            decimal barrelCost = wagonInfo.BarrelLapsed == "Yes" ? 351893 : 0;

            decimal liftBarrelTotal = liftCost + barrelCost;

            decimal marketValue = 0;

            var masterValueStr = await _context.MasterWagons
                .AsNoTracking()
                .Where(m => m.WagonNumber == wagonNumber)
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
                var masterTfr = await _context.MasterWagonsTFR
                    .AsNoTracking()
                    .Where(m => m.WagonNumber == wagonNumber)
                    .Select(m => (decimal?)m.BenchmarkValue)
                    .FirstOrDefaultAsync();

                if (masterTfr != null)
                    marketValue = masterTfr.Value;
                else
                    return BadRequest("Asset/Wagon not found in master data.");
            }

            decimal repairTotal = refurbishTotalDec + missingTotalDec + replaceTotalDec + laborTotalDec + liftBarrelTotal;
            decimal assetValue = marketValue - repairTotal;

            string totalAssetValue = assetValue.ToString("0.00", CultureInfo.InvariantCulture);
            string repairTotalStr = repairTotal.ToString("0.00", CultureInfo.InvariantCulture);

            var dash = await _context.WagonDashboardUploadeds
                .FirstOrDefaultAsync(d => d.WagonNumber == wagonNumber);

            if (dash == null)
                return BadRequest("Wagon does not exist");

            dash.RefurbishValue = refurbishTotal;
            dash.MissingValue = missingTotal;
            dash.ReplaceValue = replaceTotal;
            dash.TotalLaborValue = laborTotal;
            dash.AssetValue = totalAssetValue ?? "";
            dash.LiftValue = liftCost.ToString("0.00", CultureInfo.InvariantCulture);
            dash.BarrelValue = barrelCost.ToString("0.00", CultureInfo.InvariantCulture);
            dash.TotalValue = repairTotalStr ?? "";

            _context.WagonDashboardUploadeds.Update(dash);

            await _context.SaveChangesAsync();

            return Ok(new { message = "Wagon updated successfully." });
        }

        [HttpGet("RecalculateUploadWagonAll")]
        public async Task<IActionResult> RecalculateWagonsAll()
        {
            var existingDashboard = await _context.WagonDashboardUploadeds.Where(c => c.WagonStatus == "Uploaded").Select(d => d.WagonNumber).ToListAsync();
            foreach (var item in existingDashboard)
            {
                var payload = new RecalculateRequest();
                payload.WagonNumber = item.ToString();
                await RecalculateValues(payload);
            }
            return Ok(new { message = "Wagon updated successfully." });
        }

        [HttpGet("RecalculateUploadWagonAllNU")]
        public async Task<IActionResult> RecalculateUploadWagonAllNU()
        {
            var existingDashboard = await _context.WagonDashboards.Where(c => c.WagonStatus != "Uploaded").Select(d => d.WagonNumber).ToListAsync();
            foreach (var item in existingDashboard)
            {
                var payload = new RecalculateRequest();
                payload.WagonNumber = item.ToString();

                await RecalculateValues(payload);
            }
            return Ok(new { message = "Wagon updated successfully." });
        }

        // ADD ↓
        private async Task<List<InspectRow>> GetMultiAsync<TEntity>(int wagonNumber)
        where TEntity : class, IInspectWagonEntity
        {
            return await _context.Set<TEntity>()
                .Where(e => e.WagonNumber == wagonNumber)
                .OrderByDescending(e => e.Id)
                .Select(e => new InspectRow
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


        // ADD ↓
        private async Task<List<InspectRow>> GetSingleAsync<TEntity>(int wagonNumber)
        where TEntity : class, IInspectWagonEntity
        {
            return await _context.Set<TEntity>()
                .Where(e => e.WagonNumber == wagonNumber)
                .OrderByDescending(e => e.Id)
                .Take(1)
                .Select(e => new InspectRow
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

        [HttpGet("checkWagonInputs/{wagonNumber}")]
        public async Task<IActionResult> CheckWagonInputs(int wagonNumber)
        {
            bool exists = await _context.WagonInputs
                .AnyAsync(e => e.WagonNumber == wagonNumber);

            if (exists)
            {
                return Ok(new { message = "Yes" });
            }

            return Ok(new { message = "No" });
        }

        // ADJUST ↓
        [HttpGet("getAllWagonDashboard")]
        public async Task<IActionResult> GetAllWagonDashboard()
        {
            _context.Database.SetCommandTimeout(200); 

            var dashboardEntries = await _context.WagonDashboards
                .Where(w => w.WagonStatus != "Uploaded")
                .Select(w => new
                {
                    w.Id,
                    w.InspectorId,
                    w.InspectorName,
                    w.WagonNumber,
                    w.WagonGroup,
                    w.WagonType,
                    w.DateAssessed,
                    w.TimeAssessed,
                    w.BodyDamage,
                    w.BodyPhotos,
                    w.LiftPhoto,
                    w.LiftDate,
                    w.LiftLapsed,
                    w.BarrelPhoto,
                    w.BarrelDate,
                    w.BarrelLapsed,
                    w.BrakePhoto,
                    w.BrakeDate,
                    w.BrakeLapsed,
                    w.RefurbishValue,
                    w.MissingValue,
                    w.ReplaceValue,
                    w.AssessmentQuote,
                    w.AssessmentCert,
                    w.WagonStatus,
                    w.City,
                    w.UploadDate,
                    w.WagonPhoto,
                    w.MissingPhotos,
                    w.ReplacePhotos,
                    GpsLatitude = w.GpsLatitude ?? "N/A",
                    GpsLongitude = w.GpsLongitude ?? "N/A",
                    StartTimeInspect = w.StartTimeInspect ?? "N/A",
                    MarketValue = w.MarketValue ?? "0.00",
                    TotalLaborValue = w.TotalLaborValue ?? "0.00",
                    AssetValue = w.AssetValue ?? "0.00",
                    AssessmentSow = w.AssessmentSow ?? "Not Ready",
                    LiftValue = w.LiftValue ?? "0.00",
                    BarrelValue = w.BarrelValue ?? "0.00",
                    TotalValue = w.TotalValue ?? "0.00",
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

        [HttpPost("getUploadedWagonsPaged")]
        public async Task<IActionResult> GetUploadedWagonsPaged([FromBody] WagonDashboardQueryDto query)
        {
            _context.Database.SetCommandTimeout(200);

            IQueryable<WagonDashboardUploaded> q = _context.WagonDashboardUploadeds
                .Where(x => x.WagonStatus == "Uploaded");

            // Global search
            if (!string.IsNullOrWhiteSpace(query.GlobalFilter))
            {
                string filter = query.GlobalFilter.ToLower();

                q = q.Where(x =>
                    x.WagonNumber.ToString().Contains(filter) ||
                    x.WagonGroup.ToLower().Contains(filter) ||
                    x.InspectorName.ToLower().Contains(filter)
                );
            }

            int totalRecords = await q.CountAsync();

            List<WagonDashboardUploaded> data = await q
                .OrderByDescending(x => x.UploadDate)
                .Skip(query.First)
                .Take(query.Rows)
                .ToListAsync();

            return Ok(new PagedResult<WagonDashboardUploaded>
            {
                TotalRecords = totalRecords,
                Data = data
            });
        }

        // ADJUST ↓
        [HttpGet("getAllWagonDashboardUploaded")]
        public async Task<IActionResult> GetAllWagonDashboardUploaded()
        {
            _context.Database.SetCommandTimeout(180);

            var dashboardEntries = await _context.WagonDashboardUploadeds
                .Select(w => new
                {
                    w.InspectorId,
                    w.InspectorName,
                    w.WagonNumber,
                    w.WagonGroup,
                    w.WagonType,
                    w.DateAssessed,
                    w.TimeAssessed,
                    w.BodyDamage,
                    w.LiftDate,
                    w.LiftLapsed,
                    w.BarrelDate,
                    w.BarrelLapsed,
                    w.BrakeDate,
                    w.BrakeLapsed,
                    w.RefurbishValue,
                    w.MissingValue,
                    w.ReplaceValue,
                    w.WagonStatus,
                    w.City,
                    w.UploadDate,
                    GpsLatitude = w.GpsLatitude ?? "N/A",
                    GpsLongitude = w.GpsLongitude ?? "N/A",
                    StartTimeInspect = w.StartTimeInspect ?? "N/A",
                    MarketValue = w.MarketValue ?? "0.00",
                    TotalLaborValue = w.TotalLaborValue ?? "0.00",
                    AssetValue = w.AssetValue ?? "0.00",
                    LiftValue = w.LiftValue ?? "0.00",
                    BarrelValue = w.BarrelValue ?? "0.00",
                    TotalValue = w.TotalValue ?? "0.00",
                    ConditionScore = w.ConditionScore.ToString() ?? "N/A",
                    OperationalStatus = w.OperationalStatus ?? "N/A",
                    CalScore = w.CalScore.ToString() ?? "",
                    w.CalOperateStatus,
                    w.CalCondition,
                    Phase = w.Phase.ToString(),
                })
                .ToListAsync();

            return Ok(dashboardEntries);
        }

        [HttpPost("markReadyForAssessment")]
        public async Task<IActionResult> MarkReadyForAssessment([FromBody] WagonStatusUpdateDto dto)
        {
            var wagon = await _context.WagonDashboards
                .FirstOrDefaultAsync(w => w.WagonNumber == dto.WagonNumber);

            if (wagon == null)
                return NotFound("Wagon not found.");

            //Enforce valid transition
            if (wagon.WagonStatus != "Inspection Complete")
                return BadRequest(
                    $"Invalid transition. Current status is {wagon.WagonStatus}");

            wagon.WagonStatus = "ReadyForAssessment";

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Wagon moved to Ready For Assessment",
                wagon.WagonNumber,
                wagon.WagonStatus
            });
        }

        [HttpPost("markAssessedReadyForUpload")]
        public async Task<IActionResult> MarkAssessedReadyForUpload([FromBody] WagonStatusUpdateDto dto)
        {
            var wagon = await _context.WagonDashboards
                .FirstOrDefaultAsync(w => w.WagonNumber == dto.WagonNumber);

            if (wagon == null)
                return NotFound("Wagon not found.");

            // 🔐 Enforce valid transition
            if (wagon.WagonStatus != "ReadyForAssessment")
                return BadRequest(
                    $"Invalid transition. Current status is {wagon.WagonStatus}");

            wagon.WagonStatus = "AssessedReadyForUpload";


            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Wagon approved for upload",
                wagon.WagonNumber,
                wagon.WagonStatus
            });
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

        [HttpPost("updateCondition")]
        public async Task<IActionResult> UpdateCondition([FromBody] ConditionRequest request)
        {
            var dash = await _context.WagonDashboards
                .FirstOrDefaultAsync(d => d.WagonNumber == Convert.ToInt32(request.WagonNumber));

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

                    _context.WagonDashboards.Update(dash);

                    await _context.SaveChangesAsync();

                    return Ok(new { message = "Wagon input updated successfully." });
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

        [HttpPost("updateConditionUpload")]
        public async Task<IActionResult> UpdateConditionUpload([FromBody] ConditionRequestUpload request)
        {
            var dash = await _context.WagonDashboardUploadeds
                .FirstOrDefaultAsync(d => d.WagonNumber == Convert.ToInt32(request.WagonNumber));

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

                    _context.WagonDashboardUploadeds.Update(dash);

                    await _context.SaveChangesAsync();

                    return Ok(new { message = "Wagon input updated successfully." });
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

        [HttpPost("uploadWagons")]
        public async Task<IActionResult> UploadWagons([FromBody] List<UploadRequestItem> items)
        {
            if (items == null || !items.Any())
                return BadRequest("No wagons selected for upload.");

            // --- Ensure server folder exists ---
            string serverFolder = @"C:\WagonDashboardItemsUploaded";
            var dashboard = await _context.WagonDashboards.FirstOrDefaultAsync(w => w.WagonNumber == items[0].WagonNumber);
            if (dashboard != null)
            {
                if (dashboard.Phase == 2)
                {
                    serverFolder = @"C:\TFR_WagonDashboardItemsUploaded";
                }
                if (dashboard.Phase == 3)
                {
                    serverFolder = @"C:\TE_WagonDashboardItemsUploaded";
                }
            }
            if (!Directory.Exists(serverFolder))
                Directory.CreateDirectory(serverFolder);

            // --- Create ZIP file name including wagon numbers ---
            string wagonNumbersPart = string.Join("_", items.Select(i => i.WagonNumber));
            string zipName = $"WagonDashboardUpload_{wagonNumbersPart}_{DateTime.Now:yyyyMMdd_HHmmss}.zip";
            string zipPath = Path.Combine(serverFolder, zipName);

            using (var zipArchive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                foreach (var item in items)
                {
                    string wagonFolderName = $"{item.WagonNumber}_Dash_{DateTime.Now:yyyyMMdd_HHmmss}";

                    // Mapping folder names for categories
                    var folderMap = new Dictionary<string, string>
                    {
                        { "BodyPhotos", Path.Combine(wagonFolderName, "InfoCapturePhotos") },
                        { "LiftPhoto", Path.Combine(wagonFolderName, "InfoCapturePhotos") },
                        { "BarrelPhoto", Path.Combine(wagonFolderName, "InfoCapturePhotos") },
                        { "BrakePhoto", Path.Combine(wagonFolderName, "InfoCapturePhotos") },
                        { "WagonPhoto", Path.Combine(wagonFolderName, "InfoCapturePhotos") },
                        { "MissingPhotos", Path.Combine(wagonFolderName, "InspectionPhotos") },
                        { "ReplacePhotos", Path.Combine(wagonFolderName, "InspectionPhotos") },
                        { "AssessmentQuote", Path.Combine(wagonFolderName, "InspectionQuote") },
                        { "AssessmentCert", Path.Combine(wagonFolderName, "InspectionCert") },
                        { "AssessmentSow", Path.Combine(wagonFolderName, "InspectionSow") }
                    };

                    //PLEASE ADD (ADDING FILES USING HELPERS)
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
                    var properties = typeof(UploadRequestItem).GetProperties();
                    foreach (var prop in properties)
                    {
                        if (!folderMap.ContainsKey(prop.Name)) continue;

                        var value = prop.GetValue(item) as string;

                        //PLEASE ADD (METHOD IS NOW ASYNC)
                        await AddFilesToZipAsync(value, folderMap[prop.Name]);
                    }

                    // Update DB
                    var dashboardEntry = await _context.WagonDashboards.FirstOrDefaultAsync(w => w.WagonNumber == item.WagonNumber);
                    if (dashboardEntry != null)
                    {
                        dashboardEntry.WagonStatus = "Uploaded";
                        dashboardEntry.UploadDate = DateTime.Now.ToString("yyyy-MM-dd");

                        await _context.SaveChangesAsync();

                        // --- Insert into WagonDashboardUploaded ---
                        var uploadedEntry = new WagonDashboardUploaded
                        {
                            InspectorId = dashboardEntry.InspectorId,
                            InspectorName = dashboardEntry.InspectorName,
                            WagonNumber = dashboardEntry.WagonNumber,
                            WagonGroup = dashboardEntry.WagonGroup,
                            WagonType = dashboardEntry.WagonType,
                            DateAssessed = dashboardEntry.DateAssessed,
                            TimeAssessed = dashboardEntry.TimeAssessed,
                            BodyDamage = dashboardEntry.BodyDamage,
                            BodyPhotos = dashboardEntry.BodyPhotos,
                            LiftPhoto = dashboardEntry.LiftPhoto,
                            LiftDate = dashboardEntry.LiftDate,
                            LiftLapsed = dashboardEntry.LiftLapsed,
                            BarrelPhoto = dashboardEntry.BarrelPhoto,
                            BarrelDate = dashboardEntry.BarrelDate,
                            BarrelLapsed = dashboardEntry.BarrelLapsed,
                            BrakePhoto = dashboardEntry.BrakePhoto,
                            BrakeDate = dashboardEntry.BrakeDate,
                            BrakeLapsed = dashboardEntry.BrakeLapsed,
                            RefurbishValue = dashboardEntry.RefurbishValue,
                            MissingValue = dashboardEntry.MissingValue,
                            ReplaceValue = dashboardEntry.ReplaceValue,
                            AssessmentQuote = dashboardEntry.AssessmentQuote,
                            AssessmentCert = dashboardEntry.AssessmentCert,
                            WagonStatus = dashboardEntry.WagonStatus,
                            UploadDate = dashboardEntry.UploadDate,
                            WagonPhoto = dashboardEntry.WagonPhoto,
                            MissingPhotos = dashboardEntry.MissingPhotos,
                            ReplacePhotos = dashboardEntry.ReplacePhotos,
                            GpsLatitude = dashboardEntry.GpsLatitude,
                            GpsLongitude = dashboardEntry.GpsLongitude,
                            StartTimeInspect = dashboardEntry.StartTimeInspect,
                            MarketValue = dashboardEntry.MarketValue,
                            TotalLaborValue = dashboardEntry.TotalLaborValue,
                            AssetValue = dashboardEntry.AssetValue,
                            AssessmentSow = dashboardEntry.AssessmentSow ?? "Not Ready",
                            LiftValue = dashboardEntry.LiftValue,
                            BarrelValue = dashboardEntry.BarrelValue,
                            TotalValue = dashboardEntry.TotalValue,
                            ConditionScore = dashboardEntry?.ConditionScore,
                            OperationalStatus = dashboardEntry?.OperationalStatus ?? "Not Captured",
                            City = dashboardEntry?.City ?? "Not Captured",
                            CalScore = dashboardEntry?.CalScore,
                            CalOperateStatus = dashboardEntry?.CalOperateStatus ?? "Not Captured",
                            CalCondition = dashboardEntry?.CalCondition ?? "Not Captured",
                            Phase = dashboardEntry.Phase,
                        };

                        _context.WagonDashboardUploadeds.Add(uploadedEntry);
                        await _context.SaveChangesAsync();
                    }
                }
            }

            return Ok(new { success = true, zipPath, zipName });
        }

        [HttpPost("reUploadWagons")]
        public async Task<IActionResult> ReUploadWagons([FromBody] List<UploadRequestItem> items)
        {
            if (items == null || !items.Any())
                return BadRequest("No wagons selected for upload.");

            // --- Ensure server folder exists ---
            string serverFolder = @"C:\WagonDashboardItemsUploaded";
            var dashboard = await _context.WagonDashboards.FirstOrDefaultAsync(w => w.WagonNumber == items[0].WagonNumber);
            if (dashboard != null)
            {
                if (dashboard.Phase == 2)
                {
                    serverFolder = @"C:\TFR_WagonDashboardItemsUploaded";
                }
                if (dashboard.Phase == 3)
                {
                    serverFolder = @"C:\TE_WagonDashboardItemsUploaded";
                }
            }
            if (!Directory.Exists(serverFolder))
                Directory.CreateDirectory(serverFolder);

            // --- Create ZIP file name including wagon numbers ---
            string wagonNumbersPart = string.Join("_", items.Select(i => i.WagonNumber));
            string zipName = $"WagonDashboardReUpload_{wagonNumbersPart}_{DateTime.Now:yyyyMMdd_HHmmss}.zip";
            string zipPath = Path.Combine(serverFolder, zipName);

            using (var zipArchive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                foreach (var item in items)
                {
                    string wagonFolderName = $"{item.WagonNumber}_Dash_{DateTime.Now:yyyyMMdd_HHmmss}";

                    // Mapping folder names for categories
                    var folderMap = new Dictionary<string, string>
                    {
                        { "BodyPhotos", Path.Combine(wagonFolderName, "InfoCapturePhotos") },
                        { "LiftPhoto", Path.Combine(wagonFolderName, "InfoCapturePhotos") },
                        { "BarrelPhoto", Path.Combine(wagonFolderName, "InfoCapturePhotos") },
                        { "BrakePhoto", Path.Combine(wagonFolderName, "InfoCapturePhotos") },
                        { "WagonPhoto", Path.Combine(wagonFolderName, "InfoCapturePhotos") },
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
                    var properties = typeof(UploadRequestItem).GetProperties();
                    foreach (var prop in properties)
                    {
                        if (!folderMap.ContainsKey(prop.Name)) continue;

                        var value = prop.GetValue(item) as string;

                        //PLEASE ADD (METHOD IS NOW ASYNC)
                        await AddFilesToZipAsync(value, folderMap[prop.Name]);
                    }

                    bool exists = await _context.WagonDashboardUploadeds
                        .AnyAsync(e => e.WagonNumber == item.WagonNumber);

                    if (exists)
                    {
                        var dashboardEntry = await _context.WagonDashboardUploadeds.FirstOrDefaultAsync(w => w.WagonNumber == item.WagonNumber);

                        if (dashboardEntry != null)
                        {
                            dashboardEntry.WagonStatus = "Re-uploaded";
                            dashboardEntry.UploadDate = DateTime.Now.ToString("yyyy-MM-dd");

                            _context.WagonDashboardUploadeds.Update(dashboardEntry);
                            await _context.SaveChangesAsync();
                        }
                    }
                    else
                    {
                        return BadRequest("Wagon does not exist.");
                    }
                }
            }

            return Ok(new { success = true, zipPath, zipName });
        }

        [HttpGet("ReuploadAllWagons")]
        public async Task<IActionResult> ReuploadAllWagons()
        {
            var existingDashboard = await _context.WagonDashboardUploadeds
                .Where(d => d.WagonStatus == "Uploaded").ToListAsync();
            foreach (var dashboard in existingDashboard)
            {
                var payload = new UploadRequestItem();
                var list = new List<UploadRequestItem>();
                payload.WagonNumber = (int)dashboard.WagonNumber;
                payload.WagonNumber = (int)dashboard.WagonNumber;
                payload.AssessmentCert = dashboard.AssessmentCert;
                payload.AssessmentSow = dashboard.AssessmentSow;
                payload.AssessmentQuote = dashboard.AssessmentQuote;
                payload.ReplacePhotos = dashboard.ReplacePhotos;
                payload.BrakePhoto = dashboard.BrakePhoto;
                payload.WagonPhoto = dashboard.WagonPhoto;
                payload.LiftPhoto = dashboard.LiftPhoto;
                payload.BarrelPhoto = dashboard.BarrelPhoto;
                payload.BodyPhotos = dashboard.BodyPhotos;
                payload.MissingPhotos = dashboard.MissingPhotos;
                list.Add(payload);
                await ReUploadWagons(list);
            }
            return Ok(new { message = "PDFs generated successfully for all Locos." });
        }

        [HttpGet("ReuploadAllWagonsNU")]
        public async Task<IActionResult> ReuploadAllWagonsNU()
        {
            var existingDashboard = await _context.WagonDashboards
                .Where(d => d.WagonStatus != "Uploaded").ToListAsync();
            foreach (var dashboard in existingDashboard)
            {
                var payload = new UploadRequestItem();
                var list = new List<UploadRequestItem>();
                payload.WagonNumber = (int)dashboard.WagonNumber;
                payload.AssessmentCert = dashboard.AssessmentCert;
                payload.AssessmentSow = dashboard.AssessmentSow;
                payload.AssessmentQuote = dashboard.AssessmentQuote;
                payload.ReplacePhotos = dashboard.ReplacePhotos;
                payload.BrakePhoto = dashboard.BrakePhoto;
                payload.WagonPhoto = dashboard.WagonPhoto;
                payload.LiftPhoto = dashboard.LiftPhoto;
                payload.BarrelPhoto = dashboard.BarrelPhoto;
                payload.BodyPhotos = dashboard.BodyPhotos;
                payload.MissingPhotos = dashboard.MissingPhotos;

                list.Add(payload);
                await UploadWagons(list);
            }
            return Ok(new { message = "PDFs generated successfully for all Locos." });
        }

        [HttpPost("getUploadedWagonsForExport")]
        public async Task<IActionResult> GetUploadedWagonsForExport([FromBody] WagonDashboardQueryDto query)
        {
            _context.Database.SetCommandTimeout(180);

            IQueryable<WagonDashboardUploaded> q = _context.WagonDashboardUploadeds
                .Where(x => x.WagonStatus == "Uploaded" || x.WagonStatus == "Re-uploaded");

            // Global search
            if (!string.IsNullOrWhiteSpace(query.GlobalFilter))
            {
                string filter = query.GlobalFilter.ToLower();

                q = q.Where(x =>
                    x.WagonNumber.ToString().Contains(filter) ||
                    x.WagonGroup.ToLower().Contains(filter) ||
                    x.InspectorName.ToLower().Contains(filter)
                );
            }

            int totalRecords = await q.CountAsync();

            List<WagonDashboardUploaded> data = await q
                .OrderByDescending(x => x.UploadDate)
                .ToListAsync();

            return Ok(new PagedResult<WagonDashboardUploaded>
            {
                TotalRecords = totalRecords,
                Data = data
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

        public class InspectRow
        {
            public string? RefurbishValue { get; set; }
            public string? MissingValue { get; set; }
            public string? ReplaceValue { get; set; }
            public string? MissingPhoto { get; set; }
            public string? ReplacePhoto { get; set; }
            public string? LaborValue { get; set; }
        }

        public class WagonDashboardQueryDto
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

        public class ConditionRequest
        {
            public string WagonNumber { get; set; } = string.Empty;
            public int ConditionScore { get; set; }
        }

        public class ConditionRequestUpload
        {
            public string WagonNumber { get; set; } = string.Empty;
            public int ConditionScore { get; set; }
        }

        public class RecalculateRequest
        {
            public string WagonNumber { get; set; } = string.Empty;
        }

        public class RecalculateRequestUpload
        {
            public string WagonNumber { get; set; } = string.Empty;
        }

        public class TickWagonRequest
        {
            public string WagonNumber { get; set; } = string.Empty;
        }

        public class WagonStatusUpdateDto
        {
            public int WagonNumber { get; set; }
        }
    }
}
