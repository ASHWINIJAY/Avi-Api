using AviAppFinal.Server.Controllers;
using AviFinal.Api.DTO;
using AviFinal.Api.Models;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Presentation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    public class UploadRequestItem
    {
        public int WagonNumber { get; set; }
        public string? BodyPhotos { get; set; }         // could be JSON array string or "No Photos"
        public string? LiftPhoto { get; set; }          // single filename or path
        public string? BarrelPhoto { get; set; }        // single filename or "N/A"
        public string? BrakePhoto { get; set; }         // single filename or path
        public string? AssessmentQuote { get; set; }    // path to pdf or "N/A"
        public string? WagonPhoto { get; set; }         // single filename or path
        public string? MissingPhotos { get; set; }      // JSON array string or single
        public string? ReplacePhotos { get; set; }      // JSON array string or single
        public string? AssessmentCert { get; set; }
        public string? AssessmentSow { get; set; }
    }
    public class WagonDashboardQueryDto
    {
        public int First { get; set; }
        public int Rows { get; set; }
        public string? GlobalFilter { get; set; }
        public string? Phase { get; set; }
    }

    public class PagedResult<T>
    {
        public int TotalRecords { get; set; }
        public List<T>? Data { get; set; }
    }
    public class UploadLocoItem
    {
        public int LocoNumber { get; set; }
        public string? BodyPhotos { get; set; }         // could be JSON array string or "No Photos"
            // single filename or path
        public string? AssessmentQuote { get; set; }    // path to pdf or "N/A"
        public string? LocoPhoto { get; set; }         // single filename or path
        public string? MissingPhotos { get; set; }      // JSON array string or single
        public string? ReplacePhotos { get; set; }      // JSON array string or single
        public string? AssessmentCert { get; set; }

        public string? AssessmentSow { get; set; }
    }
    private readonly AviDbContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;
    //private readonly AppDbContext _localDb;

    public DashboardController(AviDbContext context, IWebHostEnvironment env, IConfiguration config, IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _env = env;
        _config = config;
        _httpClientFactory = httpClientFactory;
        //  _localDb = localDb;
    }

    [HttpPost("insertWagon")]
    public async Task<IActionResult> InsertWagon(int wagonNumber, string userId)
    {
        // ---------- Get User Name ----------
        var leaseUser = await _context.LeaseCoUsers
                                      .Where(u => u.UserId == userId)
                                      .Select(u => new { u.UserName })
                                      .FirstOrDefaultAsync();
        string inspectorName = leaseUser?.UserName ?? "No User";

        //PLEASE ADD
        var master = await _context.MasterWagons
                                      .AsNoTracking()
                                      .FirstOrDefaultAsync(m => m.WagonNumber == wagonNumber);

        var master2 = await _context.MasterWagonsTFR
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.WagonNumber == wagonNumber);

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
                                          w.GpsLongitude
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
        string bodyDamage = wagonInfo.BodyDamage ?? "No";
        List<string> bodyPhotosList = new();
        if (string.Equals(bodyDamage, "Yes", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(wagonInfo.BodyPhoto1)) bodyPhotosList.Add(wagonInfo.BodyPhoto1);
            if (!string.IsNullOrWhiteSpace(wagonInfo.BodyPhoto2)) bodyPhotosList.Add(wagonInfo.BodyPhoto2);
            if (!string.IsNullOrWhiteSpace(wagonInfo.BodyPhoto3)) bodyPhotosList.Add(wagonInfo.BodyPhoto3);
            if (!bodyPhotosList.Any()) bodyPhotosList.Add("No Photos");
        }
        else
        {
            bodyPhotosList.Add("No Photos");
        }
        string bodyPhotosSerialized = JsonSerializer.Serialize(bodyPhotosList);

        // ---------- Helper Lists ----------
        var refurbishValues = new List<decimal>();
        var missingValues = new List<decimal>();
        var replaceValues = new List<decimal>();
        var laborValues = new List<decimal>();
        var missingPhotosAll = new List<string>();
        var replacePhotosAll = new List<string>();

        static bool TryParseDecimal(string? s, out decimal value)
        {
            value = 0m;
            if (string.IsNullOrWhiteSpace(s)) return false;
            return decimal.TryParse(s, NumberStyles.Currency | NumberStyles.Number, CultureInfo.InvariantCulture, out value)
                || decimal.TryParse(s, NumberStyles.Currency | NumberStyles.Number, CultureInfo.CurrentCulture, out value);
        }

        // ---------- Multi-entry tables ----------
        var multiEntryTables = new List<Func<int, Task<List<InspectRow>>>>
        {
            async num => await _context.WagonPartsInspects
                                       .Where(p => p.WagonNumber == num)
                                       .OrderByDescending(p => p.Id)
                                       .Select(p => new InspectRow
                                       {
                                           RefurbishValue = p.RefurbishValue,
                                           MissingValue = p.MissingValue,
                                           ReplaceValue = p.ReplaceValue,
                                           MissingPhoto = p.MissingPhoto,
                                           ReplacePhoto = p.ReplacePhoto,
                                           LaborValue = p.LaborValue
                                       }).ToListAsync(),

            async num => await _context.AirBrakePartsInspects
                                       .Where(p => p.WagonNumber == num)
                                       .OrderByDescending(p => p.Id)
                                       .Select(p => new InspectRow
                                       {
                                           RefurbishValue = p.RefurbishValue,
                                           MissingValue = p.MissingValue,
                                           ReplaceValue = p.ReplaceValue,
                                           MissingPhoto = p.MissingPhoto,
                                           ReplacePhoto = p.ReplacePhoto,
                                           LaborValue = p.LaborValue
                                       }).ToListAsync(),

            async num => await _context.VacBrakePartsInspects
                                       .Where(p => p.WagonNumber == num)
                                       .OrderByDescending(p => p.Id)
                                       .Select(p => new InspectRow
                                       {
                                           RefurbishValue = p.RefurbishValue,
                                           MissingValue = p.MissingValue,
                                           ReplaceValue = p.ReplaceValue,
                                           MissingPhoto = p.MissingPhoto,
                                           ReplacePhoto = p.ReplacePhoto,
                                           LaborValue = p.LaborValue
                                       }).ToListAsync()
        };

        // ---------- Single-entry tables ----------
        var singleEntryTables = new List<Func<int, Task<List<InspectRow>>>>
        {
            num => _context.TankersInspects
                           .Where(p => p.WagonNumber == num)
                           .OrderByDescending(p => p.Id)
                           .Take(1)
                           .Select(p => new InspectRow
                           {
                               RefurbishValue = p.RefurbishValue,
                               MissingValue = p.MissingValue,
                               ReplaceValue = p.ReplaceValue,
                               MissingPhoto = p.MissingPhoto,
                               ReplacePhoto = p.ReplacePhoto,
                               LaborValue = p.LaborValue
                           }).ToListAsync(),

            num => _context.BottomDischargeInspects
                           .Where(p => p.WagonNumber == num)
                           .OrderByDescending(p => p.Id)
                           .Take(1)
                           .Select(p => new InspectRow
                           {
                               RefurbishValue = p.RefurbishValue,
                               MissingValue = p.MissingValue,
                               ReplaceValue = p.ReplaceValue,
                               MissingPhoto = p.MissingPhoto,
                               ReplacePhoto = p.ReplacePhoto,
                               LaborValue = p.LaborValue
                           }).ToListAsync(),

            num => _context.DoorsInspects
                           .Where(p => p.WagonNumber == num)
                           .OrderByDescending(p => p.Id)
                           .Take(1)
                           .Select(p => new InspectRow
                           {
                               RefurbishValue = p.RefurbishValue,
                               MissingValue = p.MissingValue,
                               ReplaceValue = p.ReplaceValue,
                               MissingPhoto = p.MissingPhoto,
                               ReplacePhoto = p.ReplacePhoto,
                               LaborValue = p.LaborValue
                           }).ToListAsync(),

            num => _context.TwistlocksInspects
                           .Where(p => p.WagonNumber == num)
                           .OrderByDescending(p => p.Id)
                           .Take(1)
                           .Select(p => new InspectRow
                           {
                               RefurbishValue = p.RefurbishValue,
                               MissingValue = p.MissingValue,
                               ReplaceValue = p.ReplaceValue,
                               MissingPhoto = p.MissingPhoto,
                               ReplacePhoto = p.ReplacePhoto,
                               LaborValue = p.LaborValue
                           }).ToListAsync(),

            num => _context.StanchionsInspects
                           .Where(p => p.WagonNumber == num)
                           .OrderByDescending(p => p.Id)
                           .Take(1)
                           .Select(p => new InspectRow
                           {
                               RefurbishValue = p.RefurbishValue,
                               MissingValue = p.MissingValue,
                               ReplaceValue = p.ReplaceValue,
                               MissingPhoto = p.MissingPhoto,
                               ReplacePhoto = p.ReplacePhoto,
                               LaborValue = p.LaborValue
                           }).ToListAsync(),

            num => _context.FloorInspects
                           .Where(p => p.WagonNumber == num)
                           .OrderByDescending(p => p.Id)
                           .Take(1)
                           .Select(p => new InspectRow
                           {
                               RefurbishValue = p.RefurbishValue,
                               MissingValue = p.MissingValue,
                               ReplaceValue = p.ReplaceValue,
                               MissingPhoto = p.MissingPhoto,
                               ReplacePhoto = p.ReplacePhoto,
                               LaborValue = p.LaborValue
                           }).ToListAsync()
        };

        // ---------- Aggregate values ----------
        foreach (var tableQuery in multiEntryTables.Concat(singleEntryTables))
        {
            var rows = await tableQuery(wagonNumber);

            foreach (var r in rows)
            {
                if (TryParseDecimal(r.RefurbishValue, out var rv) && rv != 0m) refurbishValues.Add(rv);
                if (TryParseDecimal(r.MissingValue, out var mv) && mv != 0m) missingValues.Add(mv);
                if (TryParseDecimal(r.ReplaceValue, out var xv) && xv != 0m) replaceValues.Add(xv);
                if (TryParseDecimal(r.LaborValue, out var lv) && lv != 0m) laborValues.Add(lv);

                if (!string.IsNullOrWhiteSpace(r.MissingPhoto) && r.MissingPhoto != "No Photo") missingPhotosAll.Add(r.MissingPhoto.Trim());
                if (!string.IsNullOrWhiteSpace(r.ReplacePhoto) && r.ReplacePhoto != "No Photo") replacePhotosAll.Add(r.ReplacePhoto.Trim());
            }
        }

        // ---------- Ensure unique photos ----------
        missingPhotosAll = missingPhotosAll.Distinct().ToList();
        replacePhotosAll = replacePhotosAll.Distinct().ToList();

        // ---------- Totals ----------
        string refurbishTotal = refurbishValues.Any() ? refurbishValues.Sum().ToString("0.00", CultureInfo.InvariantCulture) : "0.00";
        string missingTotal = missingValues.Any() ? missingValues.Sum().ToString("0.00", CultureInfo.InvariantCulture) : "0.00";
        string replaceTotal = replaceValues.Any() ? replaceValues.Sum().ToString("0.00", CultureInfo.InvariantCulture) : "0.00";
        string laborTotal = laborValues.Any() ? laborValues.Sum().ToString("0.00", CultureInfo.InvariantCulture) : "0.00";

        // ---------- Photos Serialization ----------
        string missingPhotosSerialized = missingPhotosAll.Any()
            ? JsonSerializer.Serialize(missingPhotosAll)
            : JsonSerializer.Serialize(new List<string> { "No Photos" });

        string replacePhotosSerialized = replacePhotosAll.Any()
            ? JsonSerializer.Serialize(replacePhotosAll)
            : JsonSerializer.Serialize(new List<string> { "No Photos" });

        //PLEASE ADD
        decimal liftCost = 0;
        decimal barrelCost = 0;

        if (wagonInfo?.LiftLapsed == "Yes")
            liftCost = 420982;
        else if (wagonInfo?.LiftLapsed == "No")
            liftCost = 0;

        if (wagonInfo?.BarrelLapsed == "Yes")
            barrelCost = 351893;
        else if (wagonInfo?.BarrelLapsed == "No" || wagonInfo?.BarrelLapsed == "N/A")
            barrelCost = 0;

        decimal liftBarrelTotal = liftCost + barrelCost;

        decimal marketValue = 0;

        int phase = 0;

        bool existsP1 = await _context.MasterWagons
            .AnyAsync(e => e.WagonNumber == wagonNumber);

        bool existsP2 = await _context.MasterWagonsTFR
            .AnyAsync(e => e.WagonNumber == wagonNumber);

        if (existsP1)
        {
            if (master?.MarketValue != null && !string.IsNullOrWhiteSpace(master.MarketValue.ToString()))
                decimal.TryParse(master.MarketValue.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out marketValue);

            phase = 1;
        }
        if (existsP2)
        {
            if (master2 != null)
            {
                decimal.TryParse(master2.BenchmarkValue.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out marketValue);

                phase = 2;
            } 
        }
        
        //PLEASE ADD
        decimal repairTotal = refurbishValues.Sum() + missingValues.Sum() + replaceValues.Sum() + laborValues.Sum() + liftBarrelTotal;
        decimal assetValue = marketValue - repairTotal;
        string totalAssetValue = assetValue.ToString("0.00", CultureInfo.InvariantCulture);
        string rts = repairTotal.ToString("0.00", CultureInfo.InvariantCulture);
        string markVal = marketValue.ToString("0.00", CultureInfo.InvariantCulture);

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
            LiftDate = wagonInfo.LiftDate,
            LiftLapsed = wagonInfo.LiftLapsed,
            BarrelPhoto = wagonInfo.BarrelPhoto,
            BarrelDate = wagonInfo.BarrelDate,
            BarrelLapsed = wagonInfo.BarrelLapsed,
            BrakePhoto = wagonInfo.BrakePhoto,
            BrakeDate = wagonInfo.BrakeDate,
            BrakeLapsed = wagonInfo.BrakeLapsed,
            RefurbishValue = refurbishTotal,
            MissingValue = missingTotal,
            ReplaceValue = replaceTotal,
            AssessmentQuote = "Not Ready",
            AssessmentCert = "Not Ready",
            WagonStatus = "Inspection Complete", //PLEASE ADJUST
            UploadDate = "No Date",
            WagonPhoto = wagonInfo.WagonPhoto,
            MissingPhotos = missingPhotosSerialized,
            ReplacePhotos = replacePhotosSerialized,
            GpsLatitude = wagonInfo.GpsLatitude,
            GpsLongitude = wagonInfo.GpsLongitude,
            StartTimeInspect = wagonInfo.StartInspectTime ?? "Not Available",
            MarketValue = markVal ?? "0.00", //PLEASE ADJUST
            TotalLaborValue = laborTotal,
            AssetValue = totalAssetValue ?? "0.00", //PLEASE ADJUST
            AssessmentSow = "Not Ready", //PLEASE ADD
            LiftValue = liftCost.ToString("0.00", CultureInfo.InvariantCulture), //PLEASE ADD
            BarrelValue = barrelCost.ToString("0.00", CultureInfo.InvariantCulture), //PLEASE ADD
            TotalValue = rts ?? "0.00", //PLEASE ADD,
            City = city,
             CalScore = score,
            CalOperateStatus = condition?.OperationalStatus ?? "Scrap Only",
            CalCondition = condition?.Condition ?? "Beyond Repair",
            Phase = phase,
        };
        var existingLoco = await _context.WagonDashboards
                                        .FirstOrDefaultAsync(d => d.WagonNumber == wagonNumber);
        if (existingLoco != null)
        {

        }
        else
        {
            _context.WagonDashboards.Add(dashboardEntry);
            await _context.SaveChangesAsync();
        }

        return Ok(new { success = true, message = "Wagon dashboard entry created", id = dashboardEntry.Id });
    }
    [HttpPost("uploadWagons")]
    public async Task<IActionResult> UploadWagons([FromBody] List<UploadRequestItem> items)
    {
        if (items == null || !items.Any())
            return BadRequest("No wagons selected for upload.");

        // --- Ensure server folder exists ---
        string serverFolder = @"C:\WagonDashboardItemsUploaded";
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
                { "AssessmentSow", Path.Combine(wagonFolderName, "InspectionSow") } //PLEASE ADD
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
                    // Check if wagon already exists
                    var existingEntry = await _context.WagonDashboardUploadeds
                        .FirstOrDefaultAsync(x => x.WagonNumber == dashboardEntry.WagonNumber);

                    if (existingEntry != null)
                    {
                        // UPDATE existing record
                        existingEntry.InspectorId = dashboardEntry.InspectorId;
                        existingEntry.InspectorName = dashboardEntry.InspectorName;
                        existingEntry.WagonGroup = dashboardEntry.WagonGroup;
                        existingEntry.WagonType = dashboardEntry.WagonType;
                        existingEntry.DateAssessed = dashboardEntry.DateAssessed;
                        existingEntry.TimeAssessed = dashboardEntry.TimeAssessed;
                        existingEntry.BodyDamage = dashboardEntry.BodyDamage;
                        existingEntry.BodyPhotos = dashboardEntry.BodyPhotos;
                        existingEntry.LiftPhoto = dashboardEntry.LiftPhoto;
                        existingEntry.LiftDate = dashboardEntry.LiftDate;
                        existingEntry.LiftLapsed = dashboardEntry.LiftLapsed;
                        existingEntry.BarrelPhoto = dashboardEntry.BarrelPhoto;
                        existingEntry.BarrelDate = dashboardEntry.BarrelDate;
                        existingEntry.BarrelLapsed = dashboardEntry.BarrelLapsed;
                        existingEntry.BrakePhoto = dashboardEntry.BrakePhoto;
                        existingEntry.BrakeDate = dashboardEntry.BrakeDate;
                        existingEntry.BrakeLapsed = dashboardEntry.BrakeLapsed;
                        existingEntry.RefurbishValue = dashboardEntry.RefurbishValue;
                        existingEntry.MissingValue = dashboardEntry.MissingValue;
                        existingEntry.ReplaceValue = dashboardEntry.ReplaceValue;
                        existingEntry.AssessmentQuote = dashboardEntry.AssessmentQuote;
                        existingEntry.AssessmentCert = dashboardEntry.AssessmentCert;
                        existingEntry.WagonStatus = dashboardEntry.WagonStatus;
                        existingEntry.UploadDate = dashboardEntry.UploadDate;
                        existingEntry.WagonPhoto = dashboardEntry.WagonPhoto;
                        existingEntry.MissingPhotos = dashboardEntry.MissingPhotos;
                        existingEntry.ReplacePhotos = dashboardEntry.ReplacePhotos;
                        existingEntry.GpsLatitude = dashboardEntry.GpsLatitude;
                        existingEntry.GpsLongitude = dashboardEntry.GpsLongitude;
                        existingEntry.StartTimeInspect = dashboardEntry.StartTimeInspect;
                        existingEntry.MarketValue = dashboardEntry.MarketValue;
                        existingEntry.TotalLaborValue = dashboardEntry.TotalLaborValue;
                        existingEntry.AssetValue = dashboardEntry.AssetValue;

                        // NEW FIELDS
                        existingEntry.AssessmentSow = dashboardEntry.AssessmentSow ?? "Not Ready";
                        existingEntry.LiftValue = dashboardEntry.LiftValue;
                        existingEntry.BarrelValue = dashboardEntry.BarrelValue;
                        existingEntry.TotalValue = dashboardEntry.TotalValue;
                        existingEntry. City = dashboardEntry.City;
                        existingEntry.ConditionScore = dashboardEntry.ConditionScore;
                        existingEntry.OperationalStatus =dashboardEntry.OperationalStatus;
                        existingEntry.CalScore = dashboardEntry?.CalScore;
                        existingEntry.CalOperateStatus = dashboardEntry?.CalOperateStatus ?? "Not Captured";
                        existingEntry.CalCondition = dashboardEntry?.CalCondition ?? "Not Captured";
                        existingEntry.Phase = dashboardEntry.Phase;
                    }
                    else
                    {
                        // INSERT new record
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
                            City = dashboardEntry.City,
                            ConditionScore = dashboardEntry.ConditionScore,
                            OperationalStatus = dashboardEntry.OperationalStatus,
                            CalScore = dashboardEntry?.CalScore,
                            CalOperateStatus = dashboardEntry?.CalOperateStatus ?? "Not Captured",
                            CalCondition = dashboardEntry?.CalCondition ?? "Not Captured",
                        };

                        _context.WagonDashboardUploadeds.Add(uploadedEntry);
                    }

                    await _context.SaveChangesAsync();

                }
                }
        }

        return Ok(new { success = true, zipPath, zipName });
    }
    [HttpPost("getUploadedWagonsPaged")]
    public async Task<IActionResult> GetUploadedWagonsPaged([FromBody] WagonDashboardQueryDto query)
    {
        int phase = 1;
        phase = Convert.ToInt32(query.Phase);
        _context.Database.SetCommandTimeout(180);

        IQueryable<WagonDashboardUploaded> q = _context.WagonDashboardUploadeds
            .Where(x => x.WagonStatus == "Uploaded" && x.Phase == phase);

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

    [HttpPost("getUploadedWagonsForExport")]
    public async Task<IActionResult> GetUploadedWagonsForExport([FromBody] WagonDashboardQueryDto query)
    {
        int phase = 1;
        phase = Convert.ToInt32(query.Phase);
        _context.Database.SetCommandTimeout(180);

        IQueryable<WagonDashboardUploaded> q = _context.WagonDashboardUploadeds
            .Where(x => x.WagonStatus == "Uploaded" && x.Phase==phase);

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
    [HttpPost("getUploadedLocosPaged")]
    public async Task<IActionResult> GetUploadedLocosPaged([FromBody] WagonDashboardQueryDto query)
    {
        _context.Database.SetCommandTimeout(180);
        int phase = 1;
        phase = Convert.ToInt32(query.Phase);
        IQueryable<LocoDashboard> q = _context.LocoDashboards
            .Where(x => x.UploadStatus == "Uploaded" && x.Phase == phase);

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
    public async Task<IActionResult> GetUploadedLocosForExport(
    [FromBody] WagonDashboardQueryDto query)
    {
        int phase = 1;
        phase = Convert.ToInt32(query.Phase);
        _context.Database.SetCommandTimeout(180);

        IQueryable<LocoDashboard> q = _context.LocoDashboards
            .AsNoTracking()
            .Where(x => x.UploadStatus == "Uploaded" && x.Phase==phase);

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

    [HttpGet("getUploadedWagons")]
    public async Task<IActionResult> GetUploadedWagons()
    {

        var dashboardEntries = await _context.WagonDashboardUploadeds
            .Where(w => w.WagonStatus == "Uploaded")
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
                w.WagonStatus, w.City, //PLEASE ADJUST
                w.UploadDate,
                w.WagonPhoto,
                w.MissingPhotos,
                w.ReplacePhotos,
                GpsLatitude = w.GpsLatitude ?? "N/A",
                GpsLongitude = w.GpsLongitude ?? "N/A",
                StartTimeInspect = w.StartTimeInspect ?? "N/A",
                MarketValue = w.MarketValue ?? "0.00", //PLEASE ADJUST
                TotalLaborValue = w.TotalLaborValue ?? "0.00",
                AssetValue = w.AssetValue ?? "0.00",
                w.AssessmentSow, //PLEASE ADD
                LiftValue = w.LiftValue ?? "0.00", //PLEASE ADD
                BarrelValue = w.BarrelValue ?? "0.00", //PLEASE ADD
                TotalValue = w.TotalValue ?? "0.00", //PLEASE ADD,
                ConditionScore = w.ConditionScore != 0 ? w.ConditionScore : 0, //PLEASE ADD (NEW)
                OperationalStatus = w.OperationalStatus ?? "N/A" //PLEASE ADD (NEW)
            })
            .ToListAsync();

        return Ok(dashboardEntries);
    }

    //PLEASE ADD FOR ADMIN/SUPER USER (IN PROGRESS)
    [HttpGet("getAllWagons")]
    public async Task<IActionResult> GetAllWagons()
    {
        var dashboardEntries = await _context.WagonDashboardUploadeds
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
                w.WagonStatus, w.City, //PLEASE ADJUST
                w.UploadDate,
                w.WagonPhoto,
                w.MissingPhotos,
                w.ReplacePhotos,
                GpsLatitude = w.GpsLatitude ?? "N/A",
                GpsLongitude = w.GpsLongitude ?? "N/A",
                StartTimeInspect = w.StartTimeInspect ?? "N/A",
                MarketValue = w.MarketValue ?? "0.00", //PLEASE ADJUST
                TotalLaborValue = w.TotalLaborValue ?? "0.00",
                AssetValue = w.AssetValue ?? "0.00",
                w.AssessmentSow, //PLEASE ADD
                LiftValue = w.LiftValue ?? "0.00", //PLEASE ADD
                BarrelValue = w.BarrelValue ?? "0.00", //PLEASE ADD
                TotalValue = w.TotalValue ?? "0.00", //PLEASE ADD
                ConditionScore = w.ConditionScore != 0 ? w.ConditionScore : 0, //PLEASE ADD (NEW)
                OperationalStatus = w.OperationalStatus ?? "N/A", //PLEASE ADD (NEW),
                 CalScore = w.CalScore.ToString() ?? "",
                w.CalOperateStatus,
                w.CalCondition
            })
            .ToListAsync();

        return Ok(dashboardEntries);
    }
    [HttpPost("uploadLocos")]
    public async Task<IActionResult> UploadLocos([FromBody] List<UploadLocoItem> items)
    {
        try
        {
            if (items == null || !items.Any())
                return BadRequest("No locos selected for upload.");
            string serverFolder = @"C:\LocoDashboardItemsUploaded";
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
    [HttpGet("ReuploadAllLocos")]
    public async Task<IActionResult> ReuploadAllLocos()
    {
        var existingDashboard = await _context.LocoDashboards
            .Where(d => d.UploadStatus == "Uploaded").ToListAsync();
        foreach (var dashboard in existingDashboard)
        {
            var payload = new UploadLocoItem();
            var list = new List<UploadLocoItem>();
            payload.LocoNumber = (int)dashboard.LocoNumber;
            payload.AssessmentCert = dashboard.AssessmentCert;
            payload.AssessmentSow = dashboard.AssessmentSow;
            payload.AssessmentQuote = dashboard.AssessmentQuote;
            payload.ReplacePhotos = dashboard.ReplacePhotos;
            payload.LocoPhoto = dashboard.LocoPhoto;
            payload.BodyPhotos = dashboard.BodyPhotos;
            payload.MissingPhotos = dashboard.MissingPhotos;
            list.Add(payload);
            await UploadLocos(list);
        }
        return Ok(new { message = "PDFs generated successfully for all Locos." });
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
    [HttpGet("ReuploadAllLocosNU")]
    public async Task<IActionResult> GenerateAndSaveCertPdfForAllLocoNU()
    {
        var existingDashboard = await _context.LocoDashboards
            .Where(d => d.UploadStatus != "Uploaded").ToListAsync();
        foreach (var dashboard in existingDashboard)
        {
            var payload = new UploadLocoItem();
            var list = new List<UploadLocoItem>();
            payload.LocoNumber = (int)dashboard.LocoNumber;
            payload.AssessmentCert = dashboard.AssessmentCert;
            payload.AssessmentSow = dashboard.AssessmentSow;
            payload.AssessmentQuote = dashboard.AssessmentQuote;
            payload.ReplacePhotos = dashboard.ReplacePhotos;
            payload.LocoPhoto = dashboard.LocoPhoto;
            payload.BodyPhotos = dashboard.BodyPhotos;
            payload.MissingPhotos = dashboard.MissingPhotos;
            list.Add(payload);
            await UploadLocos(list);
        }
        return Ok(new { message = "PDFs generated successfully for all Locos." });
    }
    [HttpPost("markReadyForAssessment")]
    public async Task<IActionResult> MarkReadyForAssessment(
       [FromBody] WagonStatusUpdateDto dto)
    {
       

        var wagon = await _context.WagonDashboards
            .FirstOrDefaultAsync(w => w.WagonNumber == dto.WagonNumber);

        if (wagon == null)
            return NotFound("Wagon not found.");

        // 🔐 Enforce valid transition
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

    // 🔹 ReadyForAssessment → AssessedReadyForUpload
    [HttpPost("markAssessedReadyForUpload")]
    public async Task<IActionResult> MarkAssessedReadyForUpload(
        [FromBody] WagonStatusUpdateDto dto)
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

    [HttpPost("markReadyForAssessmentLoco")]
    public async Task<IActionResult> MarkReadyForAssessmentLoco(
    [FromBody] WagonStatusUpdateDto dto)
    {


        var wagon = await _context.LocoDashboards
            .FirstOrDefaultAsync(w => w.LocoNumber == dto.WagonNumber);

        if (wagon == null)
            return NotFound("Loco not found.");

        // 🔐 Enforce valid transition
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

    // 🔹 ReadyForAssessment → AssessedReadyForUpload
    [HttpPost("markAssessedReadyForUploadLoco")]
    public async Task<IActionResult> MarkAssessedReadyForUploadLoco(
        [FromBody] WagonStatusUpdateDto dto)
    {


        var wagon = await _context.LocoDashboards
            .FirstOrDefaultAsync(w => w.LocoNumber == dto.WagonNumber);

        if (wagon == null)
            return NotFound("Loco not found.");

        // 🔐 Enforce valid transition
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
    [HttpPost("recalculateValues")]
    public async Task<IActionResult> RecalculateValues(RecalculateRequest request)
    {
        int wagonNumber = Convert.ToInt32(request.WagonNumber);

        var master = await _context.MasterWagons
                                      .AsNoTracking()
                                      .FirstOrDefaultAsync(m => m.WagonNumber == wagonNumber);

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

        var refurbishValues = new List<decimal>();
        var missingValues = new List<decimal>();
        var replaceValues = new List<decimal>();
        var laborValues = new List<decimal>();

        static bool TryParseDecimal(string? s, out decimal value)
        {
            value = 0m;
            if (string.IsNullOrWhiteSpace(s)) return false;
            return decimal.TryParse(s, NumberStyles.Currency | NumberStyles.Number, CultureInfo.InvariantCulture, out value)
                || decimal.TryParse(s, NumberStyles.Currency | NumberStyles.Number, CultureInfo.CurrentCulture, out value);
        }

        // ---------- Multi-entry tables ----------
        var multiEntryTables = new List<Func<int, Task<List<InspectRow>>>>
            {
                async num => await _context.WagonPartsInspects
                                           .Where(p => p.WagonNumber == num)
                                           .OrderByDescending(p => p.Id)
                                           .Select(p => new InspectRow
                                           {
                                               RefurbishValue = p.RefurbishValue,
                                               MissingValue = p.MissingValue,
                                               ReplaceValue = p.ReplaceValue,
                                               LaborValue = p.LaborValue
                                           }).ToListAsync(),

                async num => await _context.AirBrakePartsInspects
                                           .Where(p => p.WagonNumber == num)
                                           .OrderByDescending(p => p.Id)
                                           .Select(p => new InspectRow
                                           {
                                               RefurbishValue = p.RefurbishValue,
                                               MissingValue = p.MissingValue,
                                               ReplaceValue = p.ReplaceValue,


                                               LaborValue = p.LaborValue
                                           }).ToListAsync(),

                async num => await _context.VacBrakePartsInspects
                                           .Where(p => p.WagonNumber == num)
                                           .OrderByDescending(p => p.Id)
                                           .Select(p => new InspectRow
                                           {
                                               RefurbishValue = p.RefurbishValue,
                                               MissingValue = p.MissingValue,
                                               ReplaceValue = p.ReplaceValue,


                                               LaborValue = p.LaborValue
                                           }).ToListAsync()
            };

        // ---------- Single-entry tables ----------
        var singleEntryTables = new List<Func<int, Task<List<InspectRow>>>>
            {
                num => _context.TankersInspects
                               .Where(p => p.WagonNumber == num)
                               .OrderByDescending(p => p.Id)
                               .Take(1)
                               .Select(p => new InspectRow
                               {
                                   RefurbishValue = p.RefurbishValue,
                                   MissingValue = p.MissingValue,
                                   ReplaceValue = p.ReplaceValue,


                                   LaborValue = p.LaborValue
                               }).ToListAsync(),

                num => _context.BottomDischargeInspects
                               .Where(p => p.WagonNumber == num)
                               .OrderByDescending(p => p.Id)
                               .Take(1)
                               .Select(p => new InspectRow
                               {
                                   RefurbishValue = p.RefurbishValue,
                                   MissingValue = p.MissingValue,
                                   ReplaceValue = p.ReplaceValue,


                                   LaborValue = p.LaborValue
                               }).ToListAsync(),

                num => _context.DoorsInspects
                               .Where(p => p.WagonNumber == num)
                               .OrderByDescending(p => p.Id)
                               .Take(1)
                               .Select(p => new InspectRow
                               {
                                   RefurbishValue = p.RefurbishValue,
                                   MissingValue = p.MissingValue,
                                   ReplaceValue = p.ReplaceValue,


                                   LaborValue = p.LaborValue
                               }).ToListAsync(),

                num => _context.TwistlocksInspects
                               .Where(p => p.WagonNumber == num)
                               .OrderByDescending(p => p.Id)
                               .Take(1)
                               .Select(p => new InspectRow
                               {
                                   RefurbishValue = p.RefurbishValue,
                                   MissingValue = p.MissingValue,
                                   ReplaceValue = p.ReplaceValue,


                                   LaborValue = p.LaborValue
                               }).ToListAsync(),

                num => _context.StanchionsInspects
                               .Where(p => p.WagonNumber == num)
                               .OrderByDescending(p => p.Id)
                               .Take(1)
                               .Select(p => new InspectRow
                               {
                                   RefurbishValue = p.RefurbishValue,
                                   MissingValue = p.MissingValue,
                                   ReplaceValue = p.ReplaceValue,


                                   LaborValue = p.LaborValue
                               }).ToListAsync(),

                num => _context.FloorInspects
                               .Where(p => p.WagonNumber == num)
                               .OrderByDescending(p => p.Id)
                               .Take(1)
                               .Select(p => new InspectRow
                               {
                                   RefurbishValue = p.RefurbishValue,
                                   MissingValue = p.MissingValue,
                                   ReplaceValue = p.ReplaceValue,


                                   LaborValue = p.LaborValue
                               }).ToListAsync()
            };

        // ---------- Aggregate values ----------
        foreach (var tableQuery in multiEntryTables.Concat(singleEntryTables))
        {
            var rows = await tableQuery(wagonNumber);

            foreach (var r in rows)
            {
                if (TryParseDecimal(r.RefurbishValue, out var rv) && rv != 0m) refurbishValues.Add(rv);
                if (TryParseDecimal(r.MissingValue, out var mv) && mv != 0m) missingValues.Add(mv);
                if (TryParseDecimal(r.ReplaceValue, out var xv) && xv != 0m) replaceValues.Add(xv);
                if (TryParseDecimal(r.LaborValue, out var lv) && lv != 0m) laborValues.Add(lv);
            }
        }

        // ---------- Totals ----------
        string refurbishTotal = refurbishValues.Any() ? refurbishValues.Sum().ToString("0.00", CultureInfo.InvariantCulture) : "0.00";
        string missingTotal = missingValues.Any() ? missingValues.Sum().ToString("0.00", CultureInfo.InvariantCulture) : "0.00";
        string replaceTotal = replaceValues.Any() ? replaceValues.Sum().ToString("0.00", CultureInfo.InvariantCulture) : "0.00";
        string laborTotal = laborValues.Any() ? laborValues.Sum().ToString("0.00", CultureInfo.InvariantCulture) : "0.00";

        decimal liftCost = 0;
        decimal barrelCost = 0;

        if (wagonInfo?.LiftLapsed == "Yes")
            liftCost = 420982;
        else if (wagonInfo?.LiftLapsed == "No")
            liftCost = 0;

        if (wagonInfo?.BarrelLapsed == "Yes")
            barrelCost = 351893;
        else if (wagonInfo?.BarrelLapsed == "No" || wagonInfo?.BarrelLapsed == "N/A")
            barrelCost = 0;

        decimal liftBarrelTotal = liftCost + barrelCost;

        decimal marketValue = 0;

        if (master?.MarketValue != null && !string.IsNullOrWhiteSpace(master.MarketValue.ToString()))
            decimal.TryParse(master.MarketValue.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out marketValue);

        decimal repairTotal = refurbishValues.Sum() + missingValues.Sum() + replaceValues.Sum() + laborValues.Sum() + liftBarrelTotal;
        decimal assetValue = marketValue - repairTotal;
        string totalAssetValue = assetValue.ToString("0.00", CultureInfo.InvariantCulture);
        string rts = repairTotal.ToString("0.00", CultureInfo.InvariantCulture);

        var dash = await _context.WagonDashboards
            .FirstOrDefaultAsync(d => d.WagonNumber == wagonNumber);

        if (dash == null)
            return BadRequest("Wagon does not exist");

        dash.RefurbishValue = refurbishTotal;
        dash.MissingValue = missingTotal;
        dash.ReplaceValue = replaceTotal;
        dash.TotalLaborValue = laborTotal;
        //dash.AssetValue = totalAssetValue ?? "";
        dash.LiftValue = liftCost.ToString("0.00", CultureInfo.InvariantCulture);
        dash.BarrelValue = barrelCost.ToString("0.00", CultureInfo.InvariantCulture);
        //dash.TotalValue = rts ?? "";

        _context.WagonDashboards.Update(dash);

        await _context.SaveChangesAsync();

        return Ok(new { message = "Wagon updated successfully." });
    }

    [HttpPost("recalculateLocoValues")]
    public async Task<IActionResult> recalculateLocoValues(RecalculateRequest request)
    {
        int locoNumber = Convert.ToInt32(request.WagonNumber);
        var master = await _context.MasterLocos
                                      .AsNoTracking()
                                      .FirstOrDefaultAsync(m => m.LocoNumber == locoNumber);
       
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
                                      })
                                      .FirstOrDefaultAsync();

        if (locoInfo == null)
            return NotFound(new { success = false, message = $"No LocoInfoCaptures record found for loco {locoNumber}" });
       
        string bodyDamage = locoInfo.BodyDamage ?? "No";
        List<string> bodyPhotosList = new();
        if (string.Equals(bodyDamage, "Yes", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(locoInfo.BodyPhoto1)) bodyPhotosList.Add(locoInfo.BodyPhoto1);
            if (!string.IsNullOrWhiteSpace(locoInfo.BodyPhoto2)) bodyPhotosList.Add(locoInfo.BodyPhoto2);
            if (!string.IsNullOrWhiteSpace(locoInfo.BodyPhoto3)) bodyPhotosList.Add(locoInfo.BodyPhoto3);
            if (!bodyPhotosList.Any()) bodyPhotosList.Add("No Photos");
        }
        else
        {
            bodyPhotosList.Add("No Photos");
        }
        string bodyPhotosSerialized = JsonSerializer.Serialize(bodyPhotosList);

        var refurbishValues = new List<decimal>();
        var missingValues = new List<decimal>();
        var replaceValues = new List<decimal>();
        var laborValues = new List<decimal>();
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

        if (locoInfo.LocoModel == "E18")
        {
            multiEntryTables.Add(async num => await _context.E18bdinspects
                .Where(p => p.LocoNumber == num)
                .OrderByDescending(p => p.Id)
                .Select(p => new InspectLocoRow
                {
                    RefurbishValue = p.RefurbishValue,
                    MissingValue = p.MissingValue,
                    ReplaceValue = p.ReplaceValue,
                    MissingPhoto = p.MissingPhoto,
                    ReplacePhoto = p.ReplacePhoto,
                    LaborValue = p.LaborValue
                }).ToListAsync());

            multiEntryTables.Add(async num => await _context.E18beinspects
                .Where(p => p.LocoNumber == num)
                .OrderByDescending(p => p.Id)
                .Select(p => new InspectLocoRow
                {
                    RefurbishValue = p.RefurbishValue,
                    MissingValue = p.MissingValue,
                    ReplaceValue = p.ReplaceValue,
                    MissingPhoto = p.MissingPhoto,
                    ReplacePhoto = p.ReplacePhoto,
                    LaborValue = p.LaborValue
                }).ToListAsync());

            multiEntryTables.Add(async num => await _context.E18ccinspects
                .Where(p => p.LocoNumber == num)
                .OrderByDescending(p => p.Id)
                .Select(p => new InspectLocoRow
                {
                    RefurbishValue = p.RefurbishValue,
                    MissingValue = p.MissingValue,
                    ReplaceValue = p.ReplaceValue,
                    MissingPhoto = p.MissingPhoto,
                    ReplacePhoto = p.ReplacePhoto,
                    LaborValue = p.LaborValue
                }).ToListAsync());

            multiEntryTables.Add(async num => await _context.E18crinspects
                .Where(p => p.LocoNumber == num)
                .OrderByDescending(p => p.Id)
                .Select(p => new InspectLocoRow
                {
                    RefurbishValue = p.RefurbishValue,
                    MissingValue = p.MissingValue,
                    ReplaceValue = p.ReplaceValue,
                    MissingPhoto = p.MissingPhoto,
                    ReplacePhoto = p.ReplacePhoto,
                    LaborValue = p.LaborValue
                }).ToListAsync());

            multiEntryTables.Add(async num => await _context.E18ctinspects
                .Where(p => p.LocoNumber == num)
                .OrderByDescending(p => p.Id)
                .Select(p => new InspectLocoRow
                {
                    RefurbishValue = p.RefurbishValue,
                    MissingValue = p.MissingValue,
                    ReplaceValue = p.ReplaceValue,
                    MissingPhoto = p.MissingPhoto,
                    ReplacePhoto = p.ReplacePhoto,
                    LaborValue = p.LaborValue
                }).ToListAsync());

            multiEntryTables.Add(async num => await _context.E18eeinspects
                .Where(p => p.LocoNumber == num)
                .OrderByDescending(p => p.Id)
                .Select(p => new InspectLocoRow
                {
                    RefurbishValue = p.RefurbishValue,
                    MissingValue = p.MissingValue,
                    ReplaceValue = p.ReplaceValue,
                    MissingPhoto = p.MissingPhoto,
                    ReplacePhoto = p.ReplacePhoto,
                    LaborValue = p.LaborValue
                }).ToListAsync());

            multiEntryTables.Add(async num => await _context.E18ehinspects
                .Where(p => p.LocoNumber == num)
                .OrderByDescending(p => p.Id)
                .Select(p => new InspectLocoRow
                {
                    RefurbishValue = p.RefurbishValue,
                    MissingValue = p.MissingValue,
                    ReplaceValue = p.ReplaceValue,
                    MissingPhoto = p.MissingPhoto,
                    ReplacePhoto = p.ReplacePhoto,
                    LaborValue = p.LaborValue
                }).ToListAsync());

            multiEntryTables.Add(async num => await _context.E18esinspects
                .Where(p => p.LocoNumber == num)
                .OrderByDescending(p => p.Id)
                .Select(p => new InspectLocoRow
                {
                    RefurbishValue = p.RefurbishValue,
                    MissingValue = p.MissingValue,
                    ReplaceValue = p.ReplaceValue,
                    MissingPhoto = p.MissingPhoto,
                    ReplacePhoto = p.ReplacePhoto,
                    LaborValue = p.LaborValue
                }).ToListAsync());

            multiEntryTables.Add(async num => await _context.E18flinspects
                .Where(p => p.LocoNumber == num)
                .OrderByDescending(p => p.Id)
                .Select(p => new InspectLocoRow
                {
                    RefurbishValue = p.RefurbishValue,
                    MissingValue = p.MissingValue,
                    ReplaceValue = p.ReplaceValue,
                    MissingPhoto = p.MissingPhoto,
                    ReplacePhoto = p.ReplacePhoto,
                    LaborValue = p.LaborValue
                }).ToListAsync());

            multiEntryTables.Add(async num => await _context.E18hcinspects
                .Where(p => p.LocoNumber == num)
                .OrderByDescending(p => p.Id)
                .Select(p => new InspectLocoRow
                {
                    RefurbishValue = p.RefurbishValue,
                    MissingValue = p.MissingValue,
                    ReplaceValue = p.ReplaceValue,
                    MissingPhoto = p.MissingPhoto,
                    ReplacePhoto = p.ReplacePhoto,
                    LaborValue = p.LaborValue
                }).ToListAsync());

            multiEntryTables.Add(async num => await _context.E18hvinspects
                .Where(p => p.LocoNumber == num)
                .OrderByDescending(p => p.Id)
                .Select(p => new InspectLocoRow
                {
                    RefurbishValue = p.RefurbishValue,
                    MissingValue = p.MissingValue,
                    ReplaceValue = p.ReplaceValue,
                    MissingPhoto = p.MissingPhoto,
                    ReplacePhoto = p.ReplacePhoto,
                    LaborValue = p.LaborValue
                }).ToListAsync());

            multiEntryTables.Add(async num => await _context.E18lvinspects
                .Where(p => p.LocoNumber == num)
                .OrderByDescending(p => p.Id)
                .Select(p => new InspectLocoRow
                {
                    RefurbishValue = p.RefurbishValue,
                    MissingValue = p.MissingValue,
                    ReplaceValue = p.ReplaceValue,
                    MissingPhoto = p.MissingPhoto,
                    ReplacePhoto = p.ReplacePhoto,
                    LaborValue = p.LaborValue
                }).ToListAsync());

            multiEntryTables.Add(async num => await _context.E18mainspects
                .Where(p => p.LocoNumber == num)
                .OrderByDescending(p => p.Id)
                .Select(p => new InspectLocoRow
                {
                    RefurbishValue = p.RefurbishValue,
                    MissingValue = p.MissingValue,
                    ReplaceValue = p.ReplaceValue,
                    MissingPhoto = p.MissingPhoto,
                    ReplacePhoto = p.ReplacePhoto,
                    LaborValue = p.LaborValue
                }).ToListAsync());

            multiEntryTables.Add(async num => await _context.E18mbinspects
                .Where(p => p.LocoNumber == num)
                .OrderByDescending(p => p.Id)
                .Select(p => new InspectLocoRow
                {
                    RefurbishValue = p.RefurbishValue,
                    MissingValue = p.MissingValue,
                    ReplaceValue = p.ReplaceValue,
                    MissingPhoto = p.MissingPhoto,
                    ReplacePhoto = p.ReplacePhoto,
                    LaborValue = p.LaborValue
                }).ToListAsync());

            multiEntryTables.Add(async num => await _context.E18rfinspects
                .Where(p => p.LocoNumber == num)
                .OrderByDescending(p => p.Id)
                .Select(p => new InspectLocoRow
                {
                    RefurbishValue = p.RefurbishValue,
                    MissingValue = p.MissingValue,
                    ReplaceValue = p.ReplaceValue,
                    MissingPhoto = p.MissingPhoto,
                    ReplacePhoto = p.ReplacePhoto,
                    LaborValue = p.LaborValue
                }).ToListAsync());
        }
        else if (locoInfo.LocoModel == "GE34")
        {
            multiEntryTables.Add(async num => await _context.Ge34acinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Ge34bcinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Ge34bdinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Ge34bsinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Ge34cfinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Ge34clinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Ge34deinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Ge34ecinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Ge34edinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Ge34flinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Ge34odinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Ge34sninspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Ge34rfinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());
        }
        else if (locoInfo.LocoModel == "GE35")
        {
            multiEntryTables.Add(async num => await _context.Ge35bcinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Ge35bdinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Ge35bsinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Ge35cfinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Ge35clinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Ge35edinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Ge35deinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Ge35ecinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Ge35flinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Ge35mginspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Ge35odinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Ge35sninspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Ge35rfinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());
        }
        else if (locoInfo.LocoModel == "GE36")
        {
            multiEntryTables.Add(async num => await _context.Ge36deinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Ge36bdinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Ge36cainspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Ge36cfinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Ge36clinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Ge36ecinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Ge36edinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Ge36flinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Ge36mginspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Ge36sninspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Ge36rfinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());
        }
        else if (locoInfo.LocoModel == "GM34")
        {
            multiEntryTables.Add(async num => await _context.Gm34deinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm34bdinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm34blinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm34bsinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm34cainspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm34cbinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm34cfinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm34clinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm34edinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm34elinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm34flinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm34lminspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm34mpinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm34sninspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm34trinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm34rfinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());
        }
        else if (locoInfo.LocoModel == "GM35")
        {
            multiEntryTables.Add(async num => await _context.Gm35deinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm35blinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm35bsinspects
   .Where(p => p.LocoNumber == num)
   .OrderByDescending(p => p.Id)
   .Select(p => new InspectLocoRow
   {
       RefurbishValue = p.RefurbishValue,
       MissingValue = p.MissingValue,
       ReplaceValue = p.ReplaceValue,
       MissingPhoto = p.MissingPhoto,
       ReplacePhoto = p.ReplacePhoto,
       LaborValue = p.LaborValue
   }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm35cainspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm35cbinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm35cfinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm35clinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm35edinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm35elinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm35flinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm35lminspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm35mpinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm35sninspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm35trinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm35wainspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm35rfinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());


        }
        else if (locoInfo.LocoModel == "GM36")
        {
            multiEntryTables.Add(async num => await _context.Gm36wainspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm36flinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm36sninspects
   .Where(p => p.LocoNumber == num)
   .OrderByDescending(p => p.Id)
   .Select(p => new InspectLocoRow
   {
       RefurbishValue = p.RefurbishValue,
       MissingValue = p.MissingValue,
       ReplaceValue = p.ReplaceValue,
       MissingPhoto = p.MissingPhoto,
       ReplacePhoto = p.ReplacePhoto,
       LaborValue = p.LaborValue
   }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm36bvinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm36clinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm36ecinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm36cbinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm36bsinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm36lminspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm36lcinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm36trinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm36bpinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm36cainspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm36ecinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm36cfinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());
            multiEntryTables.Add(async num => await _context.Gm36deinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());
            multiEntryTables.Add(async num => await _context.Gm36rfinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto,
                   LaborValue = p.LaborValue
               }).ToListAsync());


        }
        //Please add tables for LocoModel GM36

        foreach (var tableQuery in multiEntryTables)
        {
            var rows = await tableQuery(locoNumber);

            foreach (var r in rows)
            {
                if (TryParseDecimal(r.RefurbishValue, out var rv) && rv != 0m) refurbishValues.Add(rv);
                if (TryParseDecimal(r.MissingValue, out var mv) && mv != 0m) missingValues.Add(mv);
                if (TryParseDecimal(r.ReplaceValue, out var xv) && xv != 0m) replaceValues.Add(xv);
                if (TryParseDecimal(r.LaborValue, out var lv) && lv != 0m) laborValues.Add(lv);
                if (!string.IsNullOrWhiteSpace(r.MissingPhoto) && r.MissingPhoto != "No Photo") missingPhotosAll.Add(r.MissingPhoto.Trim());
                if (!string.IsNullOrWhiteSpace(r.ReplacePhoto) && r.ReplacePhoto != "No Photo") replacePhotosAll.Add(r.ReplacePhoto.Trim());
            }
        }

        missingPhotosAll = missingPhotosAll.Distinct().ToList();
        replacePhotosAll = replacePhotosAll.Distinct().ToList();

        // ---------- Totals ----------
        string refurbishTotal = refurbishValues.Any() ? refurbishValues.Sum().ToString("0.00", CultureInfo.InvariantCulture) : "0.00";
        string missingTotal = missingValues.Any() ? missingValues.Sum().ToString("0.00", CultureInfo.InvariantCulture) : "0.00";
        string replaceTotal = replaceValues.Any() ? replaceValues.Sum().ToString("0.00", CultureInfo.InvariantCulture) : "0.00";
        string laborTotal = laborValues.Any() ? laborValues.Sum().ToString("0.00", CultureInfo.InvariantCulture) : "0.00";

        // ---------- Photos Serialization ----------
        string missingPhotosSerialized = missingPhotosAll.Any()
            ? JsonSerializer.Serialize(missingPhotosAll)
            : JsonSerializer.Serialize(new List<string> { "No Photos" });

        string replacePhotosSerialized = replacePhotosAll.Any()
            ? JsonSerializer.Serialize(replacePhotosAll)
            : JsonSerializer.Serialize(new List<string> { "No Photos" });
        decimal marketValue = 0; //PLEASE ADD

        //PLEASE ADD
        if (master?.MarketValue != null && !string.IsNullOrWhiteSpace(master.MarketValue.ToString()))
            decimal.TryParse(master.MarketValue.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out marketValue);

        //PLEASE ADD
        decimal repairTotal = refurbishValues.Sum() + missingValues.Sum() + replaceValues.Sum() + laborValues.Sum();
        decimal assetValue = marketValue - repairTotal;
        string totalAssetValue = assetValue.ToString("0.00", CultureInfo.InvariantCulture);
        string rts = repairTotal.ToString("0.00", CultureInfo.InvariantCulture);

        var dashboardEntry = new LocoDashboard
        {
            LocoNumber = locoNumber,
            LocoClass = locoInfo.LocoClass ?? string.Empty,
            LocoModel = locoInfo.LocoModel ?? string.Empty,
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
            LocoPhoto = locoInfo.LocoPhoto,
            MissingPhotos = missingPhotosSerialized,
            ReplacePhotos = replacePhotosSerialized,
            GpsLatitude = locoInfo.GpsLatitude, //PLEASE ADD
            
            GpsLongitude = locoInfo.GpsLongitude, //PLEASE ADD
            TotalLaborValue = laborTotal, //PLEASE ADD
            StartTimeInspect = locoInfo.CreatedDate?.ToString("HH:mm:ss") ?? "Not Available", //PLEASE ADD

            

            AssetValue = totalAssetValue, //PLEASE ADD
            MarketValue = master?.MarketValue ?? "0.00",
            AssessmentSow = "Not Ready", //PLEASE ADD
            TotalValue = rts ?? "0.00"
        };
        var existingLoco = await _context.LocoDashboards
                                        .FirstOrDefaultAsync(d => d.LocoNumber == locoNumber);
        if (existingLoco != null)
        {
            existingLoco.MissingValue = dashboardEntry.MissingValue;
            existingLoco.ReplaceValue = dashboardEntry.ReplaceValue;
            existingLoco.RefurbishValue = dashboardEntry.RefurbishValue;
            existingLoco.TotalLaborValue = dashboardEntry.TotalLaborValue;
            //existingLoco.AssetValue = dashboardEntry.AssetValue;
           // existingLoco.TotalValue = dashboardEntry.TotalValue;
           // existingLoco.MarketValue = dashboardEntry.MarketValue;
            await _context.SaveChangesAsync();
        }
        else
        {
            _context.LocoDashboards.Add(dashboardEntry);
            await _context.SaveChangesAsync();
        }
        return Ok(new { success = true, message = "Loco dashboard entry created", id = dashboardEntry.Id });
    }
    [HttpPost("recalculateValuesUpload")]
    public async Task<IActionResult> RecalculateValuesUpload(RecalculateRequestUpload request)
    {
        int wagonNumber = Convert.ToInt32(request.WagonNumber);

        var master = await _context.MasterWagons
                                      .AsNoTracking()
                                      .FirstOrDefaultAsync(m => m.WagonNumber == wagonNumber);

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

        var refurbishValues = new List<decimal>();
        var missingValues = new List<decimal>();
        var replaceValues = new List<decimal>();
        var laborValues = new List<decimal>();

        static bool TryParseDecimal(string? s, out decimal value)
        {
            value = 0m;
            if (string.IsNullOrWhiteSpace(s)) return false;
            return decimal.TryParse(s, NumberStyles.Currency | NumberStyles.Number, CultureInfo.InvariantCulture, out value)
                || decimal.TryParse(s, NumberStyles.Currency | NumberStyles.Number, CultureInfo.CurrentCulture, out value);
        }

        // ---------- Multi-entry tables ----------
        var multiEntryTables = new List<Func<int, Task<List<InspectRow>>>>
            {
                async num => await _context.WagonPartsInspects
                                           .Where(p => p.WagonNumber == num)
                                           .OrderByDescending(p => p.Id)
                                           .Select(p => new InspectRow
                                           {
                                               RefurbishValue = p.RefurbishValue,
                                               MissingValue = p.MissingValue,
                                               ReplaceValue = p.ReplaceValue,
                                               LaborValue = p.LaborValue
                                           }).ToListAsync(),

                async num => await _context.AirBrakePartsInspects
                                           .Where(p => p.WagonNumber == num)
                                           .OrderByDescending(p => p.Id)
                                           .Select(p => new InspectRow
                                           {
                                               RefurbishValue = p.RefurbishValue,
                                               MissingValue = p.MissingValue,
                                               ReplaceValue = p.ReplaceValue,


                                               LaborValue = p.LaborValue
                                           }).ToListAsync(),

                async num => await _context.VacBrakePartsInspects
                                           .Where(p => p.WagonNumber == num)
                                           .OrderByDescending(p => p.Id)
                                           .Select(p => new InspectRow
                                           {
                                               RefurbishValue = p.RefurbishValue,
                                               MissingValue = p.MissingValue,
                                               ReplaceValue = p.ReplaceValue,


                                               LaborValue = p.LaborValue
                                           }).ToListAsync()
            };

        // ---------- Single-entry tables ----------
        var singleEntryTables = new List<Func<int, Task<List<InspectRow>>>>
            {
                num => _context.TankersInspects
                               .Where(p => p.WagonNumber == num)
                               .OrderByDescending(p => p.Id)
                               .Take(1)
                               .Select(p => new InspectRow
                               {
                                   RefurbishValue = p.RefurbishValue,
                                   MissingValue = p.MissingValue,
                                   ReplaceValue = p.ReplaceValue,


                                   LaborValue = p.LaborValue
                               }).ToListAsync(),

                num => _context.BottomDischargeInspects
                               .Where(p => p.WagonNumber == num)
                               .OrderByDescending(p => p.Id)
                               .Take(1)
                               .Select(p => new InspectRow
                               {
                                   RefurbishValue = p.RefurbishValue,
                                   MissingValue = p.MissingValue,
                                   ReplaceValue = p.ReplaceValue,


                                   LaborValue = p.LaborValue
                               }).ToListAsync(),

                num => _context.DoorsInspects
                               .Where(p => p.WagonNumber == num)
                               .OrderByDescending(p => p.Id)
                               .Take(1)
                               .Select(p => new InspectRow
                               {
                                   RefurbishValue = p.RefurbishValue,
                                   MissingValue = p.MissingValue,
                                   ReplaceValue = p.ReplaceValue,


                                   LaborValue = p.LaborValue
                               }).ToListAsync(),

                num => _context.TwistlocksInspects
                               .Where(p => p.WagonNumber == num)
                               .OrderByDescending(p => p.Id)
                               .Take(1)
                               .Select(p => new InspectRow
                               {
                                   RefurbishValue = p.RefurbishValue,
                                   MissingValue = p.MissingValue,
                                   ReplaceValue = p.ReplaceValue,


                                   LaborValue = p.LaborValue
                               }).ToListAsync(),

                num => _context.StanchionsInspects
                               .Where(p => p.WagonNumber == num)
                               .OrderByDescending(p => p.Id)
                               .Take(1)
                               .Select(p => new InspectRow
                               {
                                   RefurbishValue = p.RefurbishValue,
                                   MissingValue = p.MissingValue,
                                   ReplaceValue = p.ReplaceValue,


                                   LaborValue = p.LaborValue
                               }).ToListAsync(),

                num => _context.FloorInspects
                               .Where(p => p.WagonNumber == num)
                               .OrderByDescending(p => p.Id)
                               .Take(1)
                               .Select(p => new InspectRow
                               {
                                   RefurbishValue = p.RefurbishValue,
                                   MissingValue = p.MissingValue,
                                   ReplaceValue = p.ReplaceValue,


                                   LaborValue = p.LaborValue
                               }).ToListAsync()
            };

        // ---------- Aggregate values ----------
        foreach (var tableQuery in multiEntryTables.Concat(singleEntryTables))
        {
            var rows = await tableQuery(wagonNumber);

            foreach (var r in rows)
            {
                if (TryParseDecimal(r.RefurbishValue, out var rv) && rv != 0m) refurbishValues.Add(rv);
                if (TryParseDecimal(r.MissingValue, out var mv) && mv != 0m) missingValues.Add(mv);
                if (TryParseDecimal(r.ReplaceValue, out var xv) && xv != 0m) replaceValues.Add(xv);
                if (TryParseDecimal(r.LaborValue, out var lv) && lv != 0m) laborValues.Add(lv);
            }
        }

        // ---------- Totals ----------
        string refurbishTotal = refurbishValues.Any() ? refurbishValues.Sum().ToString("0.00", CultureInfo.InvariantCulture) : "0.00";
        string missingTotal = missingValues.Any() ? missingValues.Sum().ToString("0.00", CultureInfo.InvariantCulture) : "0.00";
        string replaceTotal = replaceValues.Any() ? replaceValues.Sum().ToString("0.00", CultureInfo.InvariantCulture) : "0.00";
        string laborTotal = laborValues.Any() ? laborValues.Sum().ToString("0.00", CultureInfo.InvariantCulture) : "0.00";

        decimal liftCost = 0;
        decimal barrelCost = 0;

        if (wagonInfo?.LiftLapsed == "Yes")
            liftCost = 420982;
        else if (wagonInfo?.LiftLapsed == "No")
            liftCost = 0;

        if (wagonInfo?.BarrelLapsed == "Yes")
            barrelCost = 351893;
        else if (wagonInfo?.BarrelLapsed == "No" || wagonInfo?.BarrelLapsed == "N/A")
            barrelCost = 0;

        decimal liftBarrelTotal = liftCost + barrelCost;

        decimal marketValue = 0;

        if (master?.MarketValue != null && !string.IsNullOrWhiteSpace(master.MarketValue.ToString()))
            decimal.TryParse(master.MarketValue.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out marketValue);

        decimal repairTotal = refurbishValues.Sum() + missingValues.Sum() + replaceValues.Sum() + laborValues.Sum() + liftBarrelTotal;
        decimal assetValue = marketValue - repairTotal;
        string totalAssetValue = assetValue.ToString("0.00", CultureInfo.InvariantCulture);
        string rts = repairTotal.ToString("0.00", CultureInfo.InvariantCulture);

        var dash = await _context.WagonDashboardUploadeds
            .FirstOrDefaultAsync(d => d.WagonNumber == wagonNumber);

        if (dash == null)
            return BadRequest("Wagon does not exist");

        dash.RefurbishValue = refurbishTotal;
        dash.MissingValue = missingTotal;
        dash.ReplaceValue = replaceTotal;
        dash.TotalLaborValue = laborTotal;
        //dash.AssetValue = totalAssetValue ?? "";
        dash.LiftValue = liftCost.ToString("0.00", CultureInfo.InvariantCulture);
        dash.BarrelValue = barrelCost.ToString("0.00", CultureInfo.InvariantCulture);
       // dash.TotalValue = rts ?? "";

        _context.WagonDashboardUploadeds.Update(dash);

        await _context.SaveChangesAsync();

        return Ok(new { message = "Wagon updated successfully." });
    }
    [HttpGet("RecalculateUploadLocoAll")]
    public async Task<IActionResult> RecalculateLocosAll()
    {
        var existingDashboard = await _context.LocoDashboards.Where(c => c.UploadStatus == "Uploaded").Select(d => d.LocoNumber).ToListAsync();
    foreach(var item in existingDashboard)
        {
            var payload = new RecalculateRequest();
            payload.WagonNumber = item.ToString();
           await recalculateLocoValues(payload);
        }
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
    [HttpGet("RecalculateUploadLocoAllNU")]
    public async Task<IActionResult> RecalculateLocosAllNU()
    {
        var existingDashboard = await _context.LocoDashboards.Where(c => c.UploadStatus != "Uploaded").Select(d => d.LocoNumber).ToListAsync();
        foreach (var item in existingDashboard)
        {
            var payload = new RecalculateRequest();
            payload.WagonNumber = item.ToString();
            await recalculateLocoValues(payload);
        }
        return Ok(new { message = "Wagon updated successfully." });
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
        else
        {
            bool manualData = _context.ManualDcfinputs.Any(c => c.AssetNumber == wagonNumber);
            if (manualData)
            {
                return Ok(new { message = "Yes" });
            }
        }
        return Ok(new { message = "No" });
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
        else
        {
            bool manualData = _context.ManualDcfinputs.Any(c => c.AssetNumber == locoNumber);
            if (manualData)
            {
                return Ok(new { message = "Yes" });
            }
        }

        return Ok(new { message = "No" });
    }
    public class WagonStatusUpdateDto
    {
        public int WagonNumber { get; set; }
    }


    [HttpPost("insertOldWagon")]
    public async Task<IActionResult> InsertOldWagon()
    {
        // ---------- Get User Name ----------
        var leaseUser = await _context.LeaseCoUsers
                                      .Where(u => u.UserId == "")
                                      .Select(u => new { u.UserName })
                                      .FirstOrDefaultAsync();
        string inspectorName = leaseUser?.UserName ?? "Unknown User";
        var oldVagonList = await _context.WagonInfoCaptures
                                      .OrderByDescending(w => w.Id)
                                      .Select(w => w.WagonNumber)
                                      .ToListAsync();
        foreach (var wagonNumber in oldVagonList)
        {
          InsertWagon(wagonNumber, "").Wait();
		}
        return Ok(new { success = true, message = "Wagon dashboard entry created" });
    }

    [HttpPost("insertOldLocoCity")]
    public async Task<IActionResult> InsertOldLocoCity()
    {
        // ---------- Get User Name ----------
        
        var oldLocoList = await _context.LocoDashboards.Where(c => c.City == null)
                                      .ToListAsync();
        foreach (var loco in oldLocoList)
        {
            await UpdateLocoCityAsync(loco, "N/A");
        }
        return Ok(new { success = true, message = "Wagon dashboard entry created" });
    }

    [HttpPost("insertOldWagonCity")]
    public async Task<IActionResult> InsertOldWagonCity()
    {
        // ---------- Get User Name ----------

        var oldLocoList = await _context.WagonDashboards.Where(c => c.City == null)
                                      .ToListAsync();
        foreach (var loco in oldLocoList)
        {
            await UpdateWagonCity(loco, "N/A");
        }
        return Ok(new { success = true, message = "Wagon dashboard entry created" });
    }

    [HttpPost("insertOldWagonCityV1")]
    public async Task<IActionResult> InsertOldWagonCityV1()
    {
        // ---------- Get User Name ----------

        var oldLocoList = await _context.WagonDashboards.Where(c => c.City == "Not Captured")
                                      .ToListAsync();
        foreach (var loco in oldLocoList)
        {
            await UpdateWagonCity(loco, "N/A");
        }
        return Ok(new { success = true, message = "Wagon dashboard entry created" });
    }

    public async Task UpdateLocoCityAsync(LocoDashboard locoInfo, string city)
    {
        var loco = await _context.LocoDashboards
            .FirstOrDefaultAsync(l => l.Id == locoInfo.Id);

        if (loco == null)
            return;

        // Try parse GPS coordinates
        if (double.TryParse(locoInfo?.GpsLatitude, NumberStyles.Any, CultureInfo.InvariantCulture, out double latitude)
            && double.TryParse(locoInfo?.GpsLongitude, NumberStyles.Any, CultureInfo.InvariantCulture, out double longitude))
        {
            // Resolve city name from coordinates
            var resolved = await GetCityFromCoordinatesAsync(latitude, longitude);

            if (!string.IsNullOrWhiteSpace(resolved) &&
                !resolved.StartsWith("Error", StringComparison.InvariantCultureIgnoreCase))
            {
                city = resolved; // override user input
            }
        }

        // ALWAYS update city, even if GPS decode fails
        loco.City = city;

        await _context.SaveChangesAsync();
    }

    public async Task UpdateWagonCity(WagonDashboard locoInfo, string city)
    {
        var loco = await _context.WagonDashboards
            .FirstOrDefaultAsync(l => l.Id == locoInfo.Id);

        if (loco == null)
            return;

        // Try parse GPS coordinates
        if (double.TryParse(locoInfo?.GpsLatitude, NumberStyles.Any, CultureInfo.InvariantCulture, out double latitude)
            && double.TryParse(locoInfo?.GpsLongitude, NumberStyles.Any, CultureInfo.InvariantCulture, out double longitude))
        {
            // Resolve city name from coordinates
            var resolved = await GetCityFromCoordinatesAsync(latitude, longitude);

            if (!string.IsNullOrWhiteSpace(resolved) &&
                !resolved.StartsWith("Error", StringComparison.InvariantCultureIgnoreCase))
            {
                city = resolved; // override user input
            }
        }

        // ALWAYS update city, even if GPS decode fails
        loco.City = city;

        await _context.SaveChangesAsync();
    }
    public class InspectRow
    {
        public string? RefurbishValue { get; set; }
        public string? MissingValue { get; set; }
        public string? ReplaceValue { get; set; }
        public string? MissingPhoto { get; set; }
        public string? ReplacePhoto { get; set; }
		public string? LaborValue { get; set; } //PLEASE ADD
	}

    [HttpGet("getAllWagonDashboard")]
    public async Task<IActionResult> GetAllWagonDashboard()
    {
        var dashboardEntries = await _context.WagonDashboards
            .Where(w => w.WagonStatus != "Uploaded" )
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
                w.WagonStatus, w.City,
                w.UploadDate,
                w.WagonPhoto,
                w.MissingPhotos,
                w.ReplacePhotos,
                GpsLatitude = w.GpsLatitude ?? "N/A",
                GpsLongitude = w.GpsLongitude ?? "N/A",
                StartTimeInspect = w.StartTimeInspect ?? "N/A",
                MarketValue = w.MarketValue ?? "0.00", //PLEASE ADJUST
                TotalLaborValue = w.TotalLaborValue ?? "0.00",
                AssetValue = w.AssetValue ?? "0.00",
                AssessmentSow = w.AssessmentSow ?? "Not Ready", //PLEASE ADD
                LiftValue = w.LiftValue ?? "0.00", //PLEASE ADD
                BarrelValue = w.BarrelValue ?? "0.00", //PLEASE ADD
                TotalValue = w.TotalValue ?? "0.00" ,//PLEASE ADD
                ConditionScore = w.ConditionScore.ToString() ?? "", //PLEASE ADJUST (NEW)
                OperationalStatus = w.OperationalStatus ?? "",
                CalScore = w.CalScore.ToString() ?? "",
                w.CalOperateStatus,
                w.CalCondition
                //PLEASE ADJUST (NEW)
            })
            .ToListAsync();

        return Ok(dashboardEntries);
    }
    //PLEASE ADD (FOR ASSESSOR MODERATOR)
    [HttpGet("getTickWagonDashboard")]
    public async Task<IActionResult> GetTickWagonDashboard()
    {
        //await AutoInsertMissingWagonsAsync();

        var dashboardEntries = await _context.WagonDashboards
            .Where(w => w.WagonStatus == "Assessor Ticked") //PLEASE ADJUST
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
                w.WagonStatus, w.City, //PLEASE ADJUST
                w.UploadDate,
                w.WagonPhoto,
                w.MissingPhotos,
                w.ReplacePhotos,
                GpsLatitude = w.GpsLatitude ?? "N/A",
                GpsLongitude = w.GpsLongitude ?? "N/A",
                StartTimeInspect = w.StartTimeInspect ?? "N/A",
                MarketValue = w.MarketValue ?? "0.00", //PLEASE ADJUST
                TotalLaborValue = w.TotalLaborValue ?? "0.00",
                AssetValue = w.AssetValue ?? "0.00",
                w.AssessmentSow, //PLEASE ADD
                LiftValue = w.LiftValue ?? "0.00", //PLEASE ADD
                BarrelValue = w.BarrelValue ?? "0.00", //PLEASE ADD
                TotalValue = w.TotalValue ?? "0.00" ,//PLEASE ADD,
                ConditionScore = w.ConditionScore.ToString() ?? "", //PLEASE ADJUST (NEW)
                OperationalStatus = w.OperationalStatus ?? "", //PLEASE ADJUST (NEW)
                CalScore = w.CalScore.ToString() ?? "",
                w.CalOperateStatus,
                w.CalCondition
            })
            .ToListAsync();

        return Ok(dashboardEntries);
    }
    //PLEASE ADD (NEW)
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
    //PLEASE ADD (NEW)
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
    //PLEASE ADD (NEW)
    [HttpPost("tickWagon")]
    public async Task<IActionResult> TickWagon([FromBody] TickWagonRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.WagonNumber))
            return BadRequest("Wagon number is required.");

        if (!int.TryParse(request.WagonNumber, out int wagonNumber))
            return BadRequest("Invalid wagon number.");

        // Fetch wagon data
        var dash = await _context.WagonDashboards
            .FirstOrDefaultAsync(w => w.WagonNumber == wagonNumber);

        if (dash == null)
            return NotFound($"Wagon with number {request.WagonNumber} not found.");

        dash.WagonStatus = "Assessor Ticked";

        try
        {
            _context.WagonDashboards.Update(dash);
            await _context.SaveChangesAsync();
            return Ok(new { message = $"Wagon {request.WagonNumber} status updated to 'Assessor Ticked'." });
        }
        catch (DbUpdateException ex)
        {
            return StatusCode(500, $"Error updating wagon: {ex.Message}");
        }
    }

    [HttpPost("insertLoco")]
    public async Task<IActionResult> InsertLoco(int locoNumber, string userId)
    {
        var leaseUser = await _context.LeaseCoUsers
                                      .Where(u => u.UserId == userId)
                                      .Select(u => new { u.UserName })
                                      .FirstOrDefaultAsync();
        string inspectorName = leaseUser?.UserName ?? "No User";
        var master = await _context.MasterLocos
                                      .AsNoTracking()
                                      .FirstOrDefaultAsync(m => m.LocoNumber == locoNumber);
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
        string bodyDamage = locoInfo.BodyDamage ?? "No";
        List<string> bodyPhotosList = new();
        if (string.Equals(bodyDamage, "Yes", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(locoInfo.BodyPhoto1)) bodyPhotosList.Add(locoInfo.BodyPhoto1);
            if (!string.IsNullOrWhiteSpace(locoInfo.BodyPhoto2)) bodyPhotosList.Add(locoInfo.BodyPhoto2);
            if (!string.IsNullOrWhiteSpace(locoInfo.BodyPhoto3)) bodyPhotosList.Add(locoInfo.BodyPhoto3);
            if (!bodyPhotosList.Any()) bodyPhotosList.Add("No Photos");
        }
        else
        {
            bodyPhotosList.Add("No Photos");
        }
        string bodyPhotosSerialized = JsonSerializer.Serialize(bodyPhotosList);

        var refurbishValues = new List<decimal>();
        var missingValues = new List<decimal>();
        var replaceValues = new List<decimal>();
        var laborValues = new List<decimal>();
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

        if (locoInfo.LocoModel == "E18")
        {
            multiEntryTables.Add(async num => await _context.E18bdinspects
                .Where(p => p.LocoNumber == num)
                .OrderByDescending(p => p.Id)
                .Select(p => new InspectLocoRow
                {
                    RefurbishValue = p.RefurbishValue,
                    MissingValue = p.MissingValue,
                    ReplaceValue = p.ReplaceValue,
                    MissingPhoto = p.MissingPhoto,
                    ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue                 
                }).ToListAsync());

            multiEntryTables.Add(async num => await _context.E18beinspects
                .Where(p => p.LocoNumber == num)
                .OrderByDescending(p => p.Id)
                .Select(p => new InspectLocoRow
                {
                    RefurbishValue = p.RefurbishValue,
                    MissingValue = p.MissingValue,
                    ReplaceValue = p.ReplaceValue,
                    MissingPhoto = p.MissingPhoto,
                    ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
                }).ToListAsync());

            multiEntryTables.Add(async num => await _context.E18ccinspects
                .Where(p => p.LocoNumber == num)
                .OrderByDescending(p => p.Id)
                .Select(p => new InspectLocoRow
                {
                    RefurbishValue = p.RefurbishValue,
                    MissingValue = p.MissingValue,
                    ReplaceValue = p.ReplaceValue,
                    MissingPhoto = p.MissingPhoto,
                    ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
                }).ToListAsync());

            multiEntryTables.Add(async num => await _context.E18crinspects
                .Where(p => p.LocoNumber == num)
                .OrderByDescending(p => p.Id)
                .Select(p => new InspectLocoRow
                {
                    RefurbishValue = p.RefurbishValue,
                    MissingValue = p.MissingValue,
                    ReplaceValue = p.ReplaceValue,
                    MissingPhoto = p.MissingPhoto,
                    ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
                }).ToListAsync());

            multiEntryTables.Add(async num => await _context.E18ctinspects
                .Where(p => p.LocoNumber == num)
                .OrderByDescending(p => p.Id)
                .Select(p => new InspectLocoRow
                {
                    RefurbishValue = p.RefurbishValue,
                    MissingValue = p.MissingValue,
                    ReplaceValue = p.ReplaceValue,
                    MissingPhoto = p.MissingPhoto,
                    ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
                }).ToListAsync());

            multiEntryTables.Add(async num => await _context.E18eeinspects
                .Where(p => p.LocoNumber == num)
                .OrderByDescending(p => p.Id)
                .Select(p => new InspectLocoRow
                {
                    RefurbishValue = p.RefurbishValue,
                    MissingValue = p.MissingValue,
                    ReplaceValue = p.ReplaceValue,
                    MissingPhoto = p.MissingPhoto,
                    ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
                }).ToListAsync());

            multiEntryTables.Add(async num => await _context.E18ehinspects
                .Where(p => p.LocoNumber == num)
                .OrderByDescending(p => p.Id)
                .Select(p => new InspectLocoRow
                {
                    RefurbishValue = p.RefurbishValue,
                    MissingValue = p.MissingValue,
                    ReplaceValue = p.ReplaceValue,
                    MissingPhoto = p.MissingPhoto,
                    ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
                }).ToListAsync());

            multiEntryTables.Add(async num => await _context.E18esinspects
                .Where(p => p.LocoNumber == num)
                .OrderByDescending(p => p.Id)
                .Select(p => new InspectLocoRow
                {
                    RefurbishValue = p.RefurbishValue,
                    MissingValue = p.MissingValue,
                    ReplaceValue = p.ReplaceValue,
                    MissingPhoto = p.MissingPhoto,
                    ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
                }).ToListAsync());

            multiEntryTables.Add(async num => await _context.E18flinspects
                .Where(p => p.LocoNumber == num)
                .OrderByDescending(p => p.Id)
                .Select(p => new InspectLocoRow
                {
                    RefurbishValue = p.RefurbishValue,
                    MissingValue = p.MissingValue,
                    ReplaceValue = p.ReplaceValue,
                    MissingPhoto = p.MissingPhoto,
                    ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
                }).ToListAsync());

            multiEntryTables.Add(async num => await _context.E18hcinspects
                .Where(p => p.LocoNumber == num)
                .OrderByDescending(p => p.Id)
                .Select(p => new InspectLocoRow
                {
                    RefurbishValue = p.RefurbishValue,
                    MissingValue = p.MissingValue,
                    ReplaceValue = p.ReplaceValue,
                    MissingPhoto = p.MissingPhoto,
                    ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
                }).ToListAsync());

            multiEntryTables.Add(async num => await _context.E18hvinspects
                .Where(p => p.LocoNumber == num)
                .OrderByDescending(p => p.Id)
                .Select(p => new InspectLocoRow
                {
                    RefurbishValue = p.RefurbishValue,
                    MissingValue = p.MissingValue,
                    ReplaceValue = p.ReplaceValue,
                    MissingPhoto = p.MissingPhoto,
                    ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
                }).ToListAsync());

            multiEntryTables.Add(async num => await _context.E18lvinspects
                .Where(p => p.LocoNumber == num)
                .OrderByDescending(p => p.Id)
                .Select(p => new InspectLocoRow
                {
                    RefurbishValue = p.RefurbishValue,
                    MissingValue = p.MissingValue,
                    ReplaceValue = p.ReplaceValue,
                    MissingPhoto = p.MissingPhoto,
                    ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
                }).ToListAsync());

            multiEntryTables.Add(async num => await _context.E18mainspects
                .Where(p => p.LocoNumber == num)
                .OrderByDescending(p => p.Id)
                .Select(p => new InspectLocoRow
                {
                    RefurbishValue = p.RefurbishValue,
                    MissingValue = p.MissingValue,
                    ReplaceValue = p.ReplaceValue,
                    MissingPhoto = p.MissingPhoto,
                    ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
                }).ToListAsync());

            multiEntryTables.Add(async num => await _context.E18mbinspects
                .Where(p => p.LocoNumber == num)
                .OrderByDescending(p => p.Id)
                .Select(p => new InspectLocoRow
                {
                    RefurbishValue = p.RefurbishValue,
                    MissingValue = p.MissingValue,
                    ReplaceValue = p.ReplaceValue,
                    MissingPhoto = p.MissingPhoto,
                    ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
                }).ToListAsync());

            multiEntryTables.Add(async num => await _context.E18rfinspects
                .Where(p => p.LocoNumber == num)
                .OrderByDescending(p => p.Id)
                .Select(p => new InspectLocoRow
                {
                    RefurbishValue = p.RefurbishValue,
                    MissingValue = p.MissingValue,
                    ReplaceValue = p.ReplaceValue,
                    MissingPhoto = p.MissingPhoto,
                    ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
                }).ToListAsync());
        }
        else if (locoInfo.LocoModel == "GE34")
        {
            multiEntryTables.Add(async num => await _context.Ge34acinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Ge34bcinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Ge34bdinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Ge34bsinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Ge34cfinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Ge34clinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Ge34deinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Ge34ecinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Ge34edinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Ge34flinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Ge34odinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Ge34sninspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Ge34rfinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());
        }
        else if (locoInfo.LocoModel == "GE35")
        {
            multiEntryTables.Add(async num => await _context.Ge35bcinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Ge35bdinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Ge35bsinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Ge35cfinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Ge35clinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Ge35edinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Ge35deinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Ge35ecinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Ge35flinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Ge35mginspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Ge35odinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Ge35sninspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Ge35rfinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());
        }
        else if (locoInfo.LocoModel == "GE36")
        {
            multiEntryTables.Add(async num => await _context.Ge36deinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Ge36bdinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Ge36cainspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Ge36cfinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Ge36clinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Ge36ecinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Ge36edinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Ge36flinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Ge36mginspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Ge36sninspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Ge36rfinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());
        }
        else if (locoInfo.LocoModel == "GM34")
        {
            multiEntryTables.Add(async num => await _context.Gm34deinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm34bdinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm34blinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm34bsinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm34cainspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm34cbinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm34cfinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm34clinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm34edinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm34elinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm34flinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm34lminspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm34mpinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm34sninspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm34trinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm34rfinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());
        }
        else if (locoInfo.LocoModel == "GM35")
        {
            multiEntryTables.Add(async num => await _context.Gm35deinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm35blinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm35bsinspects
   .Where(p => p.LocoNumber == num)
   .OrderByDescending(p => p.Id)
   .Select(p => new InspectLocoRow
   {
       RefurbishValue = p.RefurbishValue,
       MissingValue = p.MissingValue,
       ReplaceValue = p.ReplaceValue,
       MissingPhoto = p.MissingPhoto,
       ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
   }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm35cainspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm35cbinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm35cfinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm35clinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm35edinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm35elinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm35flinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm35lminspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm35mpinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm35sninspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm35trinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm35wainspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm35rfinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());


        }
        else if (locoInfo.LocoModel == "GM36")
        {
            multiEntryTables.Add(async num => await _context.Gm36wainspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm36flinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm36sninspects
   .Where(p => p.LocoNumber == num)
   .OrderByDescending(p => p.Id)
   .Select(p => new InspectLocoRow
   {
       RefurbishValue = p.RefurbishValue,
       MissingValue = p.MissingValue,
       ReplaceValue = p.ReplaceValue,
       MissingPhoto = p.MissingPhoto,
       ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
   }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm36bvinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm36clinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm36ecinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm36cbinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm36bsinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm36lminspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm36lcinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm36trinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm36bpinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm36cainspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm36ecinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());

            multiEntryTables.Add(async num => await _context.Gm36cfinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());
            multiEntryTables.Add(async num => await _context.Gm36deinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());
            multiEntryTables.Add(async num => await _context.Gm36rfinspects
               .Where(p => p.LocoNumber == num)
               .OrderByDescending(p => p.Id)
               .Select(p => new InspectLocoRow
               {
                   RefurbishValue = p.RefurbishValue,
                   MissingValue = p.MissingValue,
                   ReplaceValue = p.ReplaceValue,
                   MissingPhoto = p.MissingPhoto,
                   ReplacePhoto = p.ReplacePhoto , LaborValue = p.LaborValue
               }).ToListAsync());


        }
        //Please add tables for LocoModel GM36

        foreach (var tableQuery in multiEntryTables)
        {
            var rows = await tableQuery(locoNumber);

            foreach (var r in rows)
            {
                if (TryParseDecimal(r.RefurbishValue, out var rv) && rv != 0m) refurbishValues.Add(rv);
                if (TryParseDecimal(r.MissingValue, out var mv) && mv != 0m) missingValues.Add(mv);
                if (TryParseDecimal(r.ReplaceValue, out var xv) && xv != 0m) replaceValues.Add(xv);
                if (TryParseDecimal(r.LaborValue, out var lv) && lv != 0m) laborValues.Add(lv);
                if (!string.IsNullOrWhiteSpace(r.MissingPhoto) && r.MissingPhoto != "No Photo") missingPhotosAll.Add(r.MissingPhoto.Trim());
                if (!string.IsNullOrWhiteSpace(r.ReplacePhoto) && r.ReplacePhoto != "No Photo") replacePhotosAll.Add(r.ReplacePhoto.Trim());
            }
        }

        missingPhotosAll = missingPhotosAll.Distinct().ToList();
        replacePhotosAll = replacePhotosAll.Distinct().ToList();

        // ---------- Totals ----------
        string refurbishTotal = refurbishValues.Any() ? refurbishValues.Sum().ToString("0.00", CultureInfo.InvariantCulture) : "0.00";
        string missingTotal = missingValues.Any() ? missingValues.Sum().ToString("0.00", CultureInfo.InvariantCulture) : "0.00";
        string replaceTotal = replaceValues.Any() ? replaceValues.Sum().ToString("0.00", CultureInfo.InvariantCulture) : "0.00";
        string laborTotal = laborValues.Any() ? laborValues.Sum().ToString("0.00", CultureInfo.InvariantCulture) : "0.00";

        // ---------- Photos Serialization ----------
        string missingPhotosSerialized = missingPhotosAll.Any()
            ? JsonSerializer.Serialize(missingPhotosAll)
            : JsonSerializer.Serialize(new List<string> { "No Photos" });

        string replacePhotosSerialized = replacePhotosAll.Any()
            ? JsonSerializer.Serialize(replacePhotosAll)
            : JsonSerializer.Serialize(new List<string> { "No Photos" });
        decimal marketValue = 0; //PLEASE ADD

        //PLEASE ADD
        if (master?.MarketValue != null && !string.IsNullOrWhiteSpace(master.MarketValue.ToString()))
            decimal.TryParse(master.MarketValue.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out marketValue);

        //PLEASE ADD
        decimal repairTotal = refurbishValues.Sum() + missingValues.Sum()+ replaceValues.Sum() + laborValues.Sum();
        decimal assetValue = marketValue - repairTotal;
        string totalAssetValue = assetValue.ToString("0.00", CultureInfo.InvariantCulture);
        string rts = repairTotal.ToString("0.00", CultureInfo.InvariantCulture);

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
            LocoClass = locoInfo.LocoClass ?? string.Empty,
            LocoModel = locoInfo.LocoModel ?? string.Empty,
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
            LocoPhoto = locoInfo.LocoPhoto,
            MissingPhotos = missingPhotosSerialized,
            ReplacePhotos = replacePhotosSerialized,
            GpsLatitude = locoInfo.GpsLatitude, //PLEASE ADD
            City = city, //PLEASE ADD
            GpsLongitude = locoInfo.GpsLongitude, //PLEASE ADD
            TotalLaborValue = laborTotal, //PLEASE ADD
            StartTimeInspect = locoInfo.CreatedDate?.ToString("HH:mm:ss") ?? "Not Available", //PLEASE ADD

            ReplacementValue = "Not Available", //PLEASE ADD
            
            AssetValue = totalAssetValue, //PLEASE ADD
           MarketValue = master?.MarketValue ?? "0.00", 
            AssessmentSow = "Not Ready", //PLEASE ADD
            TotalValue = rts ?? "0.00",
            CalScore = score,
            CalOperateStatus = condition?.OperationalStatus ?? "Scrap Only",
            CalCondition = condition?.Condition ?? "Beyond Repair"
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
        }
        return Ok(new { success = true, message = "Loco dashboard entry created", id = dashboardEntry.Id });
    }
    [HttpPost("insertOldLoco")]
    public async Task<IActionResult> InsertOldLoco()
    {
        var leaseUser = await _context.LeaseCoUsers
                                      .Where(u => u.UserId == "")
                                      .Select(u => new { u.UserName })
                                      .FirstOrDefaultAsync();
        string inspectorName = leaseUser?.UserName ?? "No User";
        var locoList = await _context.LocoInfoCaptures
                                      .Select(l => l.LocoNumber)
                                      .Distinct()
                                      .ToListAsync();
        foreach (var locoNumber in locoList)
        {
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
                                          w.LocoPhoto
                                      })
                                      .FirstOrDefaultAsync();

            if (locoInfo == null)
                return NotFound(new { success = false, message = $"No LocoInfoCaptures record found for loco {locoNumber}" });

            string bodyDamage = locoInfo.BodyDamage ?? "No";
            List<string> bodyPhotosList = new();
            if (string.Equals(bodyDamage, "Yes", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(locoInfo.BodyPhoto1)) bodyPhotosList.Add(locoInfo.BodyPhoto1);
                if (!string.IsNullOrWhiteSpace(locoInfo.BodyPhoto2)) bodyPhotosList.Add(locoInfo.BodyPhoto2);
                if (!string.IsNullOrWhiteSpace(locoInfo.BodyPhoto3)) bodyPhotosList.Add(locoInfo.BodyPhoto3);
                if (!bodyPhotosList.Any()) bodyPhotosList.Add("No Photos");
            }
            else
            {
                bodyPhotosList.Add("No Photos");
            }
            string bodyPhotosSerialized = JsonSerializer.Serialize(bodyPhotosList);

            var refurbishValues = new List<decimal>();
            var missingValues = new List<decimal>();
            var replaceValues = new List<decimal>();
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

            if (locoInfo.LocoModel == "E18")
            {
                multiEntryTables.Add(async num => await _context.E18bdinspects
                    .Where(p => p.LocoNumber == num)
                    .OrderByDescending(p => p.Id)
                    .Select(p => new InspectLocoRow
                    {
                        RefurbishValue = p.RefurbishValue,
                        MissingValue = p.MissingValue,
                        ReplaceValue = p.ReplaceValue,
                        MissingPhoto = p.MissingPhoto,
                        ReplacePhoto = p.ReplacePhoto
                    }).ToListAsync());

                multiEntryTables.Add(async num => await _context.E18beinspects
                    .Where(p => p.LocoNumber == num)
                    .OrderByDescending(p => p.Id)
                    .Select(p => new InspectLocoRow
                    {
                        RefurbishValue = p.RefurbishValue,
                        MissingValue = p.MissingValue,
                        ReplaceValue = p.ReplaceValue,
                        MissingPhoto = p.MissingPhoto,
                        ReplacePhoto = p.ReplacePhoto
                    }).ToListAsync());

                multiEntryTables.Add(async num => await _context.E18ccinspects
                    .Where(p => p.LocoNumber == num)
                    .OrderByDescending(p => p.Id)
                    .Select(p => new InspectLocoRow
                    {
                        RefurbishValue = p.RefurbishValue,
                        MissingValue = p.MissingValue,
                        ReplaceValue = p.ReplaceValue,
                        MissingPhoto = p.MissingPhoto,
                        ReplacePhoto = p.ReplacePhoto
                    }).ToListAsync());

                multiEntryTables.Add(async num => await _context.E18crinspects
                    .Where(p => p.LocoNumber == num)
                    .OrderByDescending(p => p.Id)
                    .Select(p => new InspectLocoRow
                    {
                        RefurbishValue = p.RefurbishValue,
                        MissingValue = p.MissingValue,
                        ReplaceValue = p.ReplaceValue,
                        MissingPhoto = p.MissingPhoto,
                        ReplacePhoto = p.ReplacePhoto
                    }).ToListAsync());

                multiEntryTables.Add(async num => await _context.E18ctinspects
                    .Where(p => p.LocoNumber == num)
                    .OrderByDescending(p => p.Id)
                    .Select(p => new InspectLocoRow
                    {
                        RefurbishValue = p.RefurbishValue,
                        MissingValue = p.MissingValue,
                        ReplaceValue = p.ReplaceValue,
                        MissingPhoto = p.MissingPhoto,
                        ReplacePhoto = p.ReplacePhoto
                    }).ToListAsync());

                multiEntryTables.Add(async num => await _context.E18eeinspects
                    .Where(p => p.LocoNumber == num)
                    .OrderByDescending(p => p.Id)
                    .Select(p => new InspectLocoRow
                    {
                        RefurbishValue = p.RefurbishValue,
                        MissingValue = p.MissingValue,
                        ReplaceValue = p.ReplaceValue,
                        MissingPhoto = p.MissingPhoto,
                        ReplacePhoto = p.ReplacePhoto
                    }).ToListAsync());

                multiEntryTables.Add(async num => await _context.E18ehinspects
                    .Where(p => p.LocoNumber == num)
                    .OrderByDescending(p => p.Id)
                    .Select(p => new InspectLocoRow
                    {
                        RefurbishValue = p.RefurbishValue,
                        MissingValue = p.MissingValue,
                        ReplaceValue = p.ReplaceValue,
                        MissingPhoto = p.MissingPhoto,
                        ReplacePhoto = p.ReplacePhoto
                    }).ToListAsync());

                multiEntryTables.Add(async num => await _context.E18esinspects
                    .Where(p => p.LocoNumber == num)
                    .OrderByDescending(p => p.Id)
                    .Select(p => new InspectLocoRow
                    {
                        RefurbishValue = p.RefurbishValue,
                        MissingValue = p.MissingValue,
                        ReplaceValue = p.ReplaceValue,
                        MissingPhoto = p.MissingPhoto,
                        ReplacePhoto = p.ReplacePhoto
                    }).ToListAsync());

                multiEntryTables.Add(async num => await _context.E18flinspects
                    .Where(p => p.LocoNumber == num)
                    .OrderByDescending(p => p.Id)
                    .Select(p => new InspectLocoRow
                    {
                        RefurbishValue = p.RefurbishValue,
                        MissingValue = p.MissingValue,
                        ReplaceValue = p.ReplaceValue,
                        MissingPhoto = p.MissingPhoto,
                        ReplacePhoto = p.ReplacePhoto
                    }).ToListAsync());

                multiEntryTables.Add(async num => await _context.E18hcinspects
                    .Where(p => p.LocoNumber == num)
                    .OrderByDescending(p => p.Id)
                    .Select(p => new InspectLocoRow
                    {
                        RefurbishValue = p.RefurbishValue,
                        MissingValue = p.MissingValue,
                        ReplaceValue = p.ReplaceValue,
                        MissingPhoto = p.MissingPhoto,
                        ReplacePhoto = p.ReplacePhoto
                    }).ToListAsync());

                multiEntryTables.Add(async num => await _context.E18hvinspects
                    .Where(p => p.LocoNumber == num)
                    .OrderByDescending(p => p.Id)
                    .Select(p => new InspectLocoRow
                    {
                        RefurbishValue = p.RefurbishValue,
                        MissingValue = p.MissingValue,
                        ReplaceValue = p.ReplaceValue,
                        MissingPhoto = p.MissingPhoto,
                        ReplacePhoto = p.ReplacePhoto
                    }).ToListAsync());

                multiEntryTables.Add(async num => await _context.E18lvinspects
                    .Where(p => p.LocoNumber == num)
                    .OrderByDescending(p => p.Id)
                    .Select(p => new InspectLocoRow
                    {
                        RefurbishValue = p.RefurbishValue,
                        MissingValue = p.MissingValue,
                        ReplaceValue = p.ReplaceValue,
                        MissingPhoto = p.MissingPhoto,
                        ReplacePhoto = p.ReplacePhoto
                    }).ToListAsync());

                multiEntryTables.Add(async num => await _context.E18mainspects
                    .Where(p => p.LocoNumber == num)
                    .OrderByDescending(p => p.Id)
                    .Select(p => new InspectLocoRow
                    {
                        RefurbishValue = p.RefurbishValue,
                        MissingValue = p.MissingValue,
                        ReplaceValue = p.ReplaceValue,
                        MissingPhoto = p.MissingPhoto,
                        ReplacePhoto = p.ReplacePhoto
                    }).ToListAsync());

                multiEntryTables.Add(async num => await _context.E18mbinspects
                    .Where(p => p.LocoNumber == num)
                    .OrderByDescending(p => p.Id)
                    .Select(p => new InspectLocoRow
                    {
                        RefurbishValue = p.RefurbishValue,
                        MissingValue = p.MissingValue,
                        ReplaceValue = p.ReplaceValue,
                        MissingPhoto = p.MissingPhoto,
                        ReplacePhoto = p.ReplacePhoto
                    }).ToListAsync());

                multiEntryTables.Add(async num => await _context.E18rfinspects
                    .Where(p => p.LocoNumber == num)
                    .OrderByDescending(p => p.Id)
                    .Select(p => new InspectLocoRow
                    {
                        RefurbishValue = p.RefurbishValue,
                        MissingValue = p.MissingValue,
                        ReplaceValue = p.ReplaceValue,
                        MissingPhoto = p.MissingPhoto,
                        ReplacePhoto = p.ReplacePhoto
                    }).ToListAsync());
            }
            else if (locoInfo.LocoModel == "GE34")
            {
                multiEntryTables.Add(async num => await _context.Ge34acinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Ge34bcinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Ge34bdinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Ge34bsinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Ge34cfinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Ge34clinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Ge34deinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Ge34ecinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Ge34edinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Ge34flinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Ge34odinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Ge34sninspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Ge34rfinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());
            }
            else if (locoInfo.LocoModel == "GE35")
            {
                multiEntryTables.Add(async num => await _context.Ge35bcinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Ge35bdinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Ge35bsinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Ge35cfinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Ge35clinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Ge35edinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Ge35deinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Ge35ecinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Ge35flinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Ge35mginspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Ge35odinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Ge35sninspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Ge35rfinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());
            }
            else if (locoInfo.LocoModel == "GE36")
            {
                multiEntryTables.Add(async num => await _context.Ge36deinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Ge36bdinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Ge36cainspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Ge36cfinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Ge36clinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Ge36ecinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Ge36edinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Ge36flinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Ge36mginspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Ge36sninspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Ge36rfinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());
            }
            else if (locoInfo.LocoModel == "GM34")
            {
                multiEntryTables.Add(async num => await _context.Gm34deinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm34bdinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm34blinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm34bsinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm34cainspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm34cbinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm34cfinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm34clinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm34edinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm34elinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm34flinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm34lminspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm34mpinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm34sninspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm34trinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm34rfinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());
            }
            else if (locoInfo.LocoModel == "GM35")
            {
                multiEntryTables.Add(async num => await _context.Gm35deinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm35blinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm35bsinspects
       .Where(p => p.LocoNumber == num)
       .OrderByDescending(p => p.Id)
       .Select(p => new InspectLocoRow
       {
           RefurbishValue = p.RefurbishValue,
           MissingValue = p.MissingValue,
           ReplaceValue = p.ReplaceValue,
           MissingPhoto = p.MissingPhoto,
           ReplacePhoto = p.ReplacePhoto
       }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm35cainspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm35cbinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm35cfinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm35clinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm35edinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm35elinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm35flinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm35lminspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm35mpinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm35sninspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm35trinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm35wainspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm35rfinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());


            }
            else if (locoInfo.LocoModel == "GM35")
            {
                multiEntryTables.Add(async num => await _context.Gm36wainspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm36flinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm36sninspects
       .Where(p => p.LocoNumber == num)
       .OrderByDescending(p => p.Id)
       .Select(p => new InspectLocoRow
       {
           RefurbishValue = p.RefurbishValue,
           MissingValue = p.MissingValue,
           ReplaceValue = p.ReplaceValue,
           MissingPhoto = p.MissingPhoto,
           ReplacePhoto = p.ReplacePhoto
       }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm36bvinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm36clinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm36cbinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm36bsinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm36lminspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm36lcinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm36trinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm36bpinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm36cainspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm36ecinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm36cfinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());
                multiEntryTables.Add(async num => await _context.Gm36deinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());
                multiEntryTables.Add(async num => await _context.Gm36rfinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());


            }
            //Please add tables for LocoModel GM36

            foreach (var tableQuery in multiEntryTables)
            {
                var rows = await tableQuery(locoNumber);

                foreach (var r in rows)
                {
                    if (TryParseDecimal(r.RefurbishValue, out var rv) && rv != 0m) refurbishValues.Add(rv);
                    if (TryParseDecimal(r.MissingValue, out var mv) && mv != 0m) missingValues.Add(mv);
                    if (TryParseDecimal(r.ReplaceValue, out var xv) && xv != 0m) replaceValues.Add(xv);

                    if (!string.IsNullOrWhiteSpace(r.MissingPhoto) && r.MissingPhoto != "No Photo") missingPhotosAll.Add(r.MissingPhoto.Trim());
                    if (!string.IsNullOrWhiteSpace(r.ReplacePhoto) && r.ReplacePhoto != "No Photo") replacePhotosAll.Add(r.ReplacePhoto.Trim());
                }
            }

            missingPhotosAll = missingPhotosAll.Distinct().ToList();
            replacePhotosAll = replacePhotosAll.Distinct().ToList();

            // ---------- Totals ----------
            string refurbishTotal = refurbishValues.Any() ? refurbishValues.Sum().ToString("0.00", CultureInfo.InvariantCulture) : "0.00";
            string missingTotal = missingValues.Any() ? missingValues.Sum().ToString("0.00", CultureInfo.InvariantCulture) : "0.00";
            string replaceTotal = replaceValues.Any() ? replaceValues.Sum().ToString("0.00", CultureInfo.InvariantCulture) : "0.00";

            // ---------- Photos Serialization ----------
            string missingPhotosSerialized = missingPhotosAll.Any()
                ? JsonSerializer.Serialize(missingPhotosAll)
                : JsonSerializer.Serialize(new List<string> { "No Photos" });

            string replacePhotosSerialized = replacePhotosAll.Any()
                ? JsonSerializer.Serialize(replacePhotosAll)
                : JsonSerializer.Serialize(new List<string> { "No Photos" });

            var dashboardEntry = new LocoDashboard
            {
                InspectorId = "No User",
                InspectorName = inspectorName ?? "No User",
                LocoNumber = locoNumber,
                LocoClass = locoInfo.LocoClass ?? string.Empty,
                LocoModel = locoInfo.LocoModel ?? string.Empty,
                DateAssessed = DateTime.Now.ToString("yyyy-MM-dd"),
                TimeAssessed = DateTime.Now.ToString("HH:mm:ss"),
                BodyDamage = bodyDamage,
                BodyPhotos = bodyPhotosSerialized,
                RefurbishValue = refurbishTotal,
                MissingValue = missingTotal,
                ReplaceValue = replaceTotal,
                AssessmentQuote = "Not Ready",
                AssessmentCert = "Not Ready",
                UploadStatus = "Not Uploaded",
                UploadDate = "No Date",
                LocoPhoto = locoInfo.LocoPhoto,
                MissingPhotos = missingPhotosSerialized,
                ReplacePhotos = replacePhotosSerialized
            };

            _context.LocoDashboards.Add(dashboardEntry);
            await _context.SaveChangesAsync();
        }
        return Ok(new { success = true, message = "Loco dashboard entry created" });
    }


    [HttpPost("updateOldLoco")]
    public async Task<IActionResult> updateOldLoco()
    {
        var leaseUser = await _context.LeaseCoUsers
                                      .Where(u => u.UserId == "")
                                      .Select(u => new { u.UserName })
                                      .FirstOrDefaultAsync();
        string inspectorName = leaseUser?.UserName ?? "No User";
        var locoList = await _context.LocoDashboards
                                       .Where(m => m.MissingValue == "0.00")
                                      .Select(l => l.LocoNumber)
                                      .Distinct()
                                      .ToListAsync();
        foreach (var locoNumber in locoList)
        {
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
                                          w.LocoPhoto
                                      })
                                      .FirstOrDefaultAsync();

            if (locoInfo == null)
                return NotFound(new { success = false, message = $"No LocoInfoCaptures record found for loco {locoNumber}" });

            string bodyDamage = locoInfo.BodyDamage ?? "No";
            List<string> bodyPhotosList = new();
            if (string.Equals(bodyDamage, "Yes", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(locoInfo.BodyPhoto1)) bodyPhotosList.Add(locoInfo.BodyPhoto1);
                if (!string.IsNullOrWhiteSpace(locoInfo.BodyPhoto2)) bodyPhotosList.Add(locoInfo.BodyPhoto2);
                if (!string.IsNullOrWhiteSpace(locoInfo.BodyPhoto3)) bodyPhotosList.Add(locoInfo.BodyPhoto3);
                if (!bodyPhotosList.Any()) bodyPhotosList.Add("No Photos");
            }
            else
            {
                bodyPhotosList.Add("No Photos");
            }
            string bodyPhotosSerialized = JsonSerializer.Serialize(bodyPhotosList);

            var refurbishValues = new List<decimal>();
            var missingValues = new List<decimal>();
            var replaceValues = new List<decimal>();
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

            if (locoInfo.LocoModel == "E18")
            {
                multiEntryTables.Add(async num => await _context.E18bdinspects
                    .Where(p => p.LocoNumber == num)
                    .OrderByDescending(p => p.Id)
                    .Select(p => new InspectLocoRow
                    {
                        RefurbishValue = p.RefurbishValue,
                        MissingValue = p.MissingValue,
                        ReplaceValue = p.ReplaceValue,
                        MissingPhoto = p.MissingPhoto,
                        ReplacePhoto = p.ReplacePhoto
                    }).ToListAsync());

                multiEntryTables.Add(async num => await _context.E18beinspects
                    .Where(p => p.LocoNumber == num)
                    .OrderByDescending(p => p.Id)
                    .Select(p => new InspectLocoRow
                    {
                        RefurbishValue = p.RefurbishValue,
                        MissingValue = p.MissingValue,
                        ReplaceValue = p.ReplaceValue,
                        MissingPhoto = p.MissingPhoto,
                        ReplacePhoto = p.ReplacePhoto
                    }).ToListAsync());

                multiEntryTables.Add(async num => await _context.E18ccinspects
                    .Where(p => p.LocoNumber == num)
                    .OrderByDescending(p => p.Id)
                    .Select(p => new InspectLocoRow
                    {
                        RefurbishValue = p.RefurbishValue,
                        MissingValue = p.MissingValue,
                        ReplaceValue = p.ReplaceValue,
                        MissingPhoto = p.MissingPhoto,
                        ReplacePhoto = p.ReplacePhoto
                    }).ToListAsync());

                multiEntryTables.Add(async num => await _context.E18crinspects
                    .Where(p => p.LocoNumber == num)
                    .OrderByDescending(p => p.Id)
                    .Select(p => new InspectLocoRow
                    {
                        RefurbishValue = p.RefurbishValue,
                        MissingValue = p.MissingValue,
                        ReplaceValue = p.ReplaceValue,
                        MissingPhoto = p.MissingPhoto,
                        ReplacePhoto = p.ReplacePhoto
                    }).ToListAsync());

                multiEntryTables.Add(async num => await _context.E18ctinspects
                    .Where(p => p.LocoNumber == num)
                    .OrderByDescending(p => p.Id)
                    .Select(p => new InspectLocoRow
                    {
                        RefurbishValue = p.RefurbishValue,
                        MissingValue = p.MissingValue,
                        ReplaceValue = p.ReplaceValue,
                        MissingPhoto = p.MissingPhoto,
                        ReplacePhoto = p.ReplacePhoto
                    }).ToListAsync());

                multiEntryTables.Add(async num => await _context.E18eeinspects
                    .Where(p => p.LocoNumber == num)
                    .OrderByDescending(p => p.Id)
                    .Select(p => new InspectLocoRow
                    {
                        RefurbishValue = p.RefurbishValue,
                        MissingValue = p.MissingValue,
                        ReplaceValue = p.ReplaceValue,
                        MissingPhoto = p.MissingPhoto,
                        ReplacePhoto = p.ReplacePhoto
                    }).ToListAsync());

                multiEntryTables.Add(async num => await _context.E18ehinspects
                    .Where(p => p.LocoNumber == num)
                    .OrderByDescending(p => p.Id)
                    .Select(p => new InspectLocoRow
                    {
                        RefurbishValue = p.RefurbishValue,
                        MissingValue = p.MissingValue,
                        ReplaceValue = p.ReplaceValue,
                        MissingPhoto = p.MissingPhoto,
                        ReplacePhoto = p.ReplacePhoto
                    }).ToListAsync());

                multiEntryTables.Add(async num => await _context.E18esinspects
                    .Where(p => p.LocoNumber == num)
                    .OrderByDescending(p => p.Id)
                    .Select(p => new InspectLocoRow
                    {
                        RefurbishValue = p.RefurbishValue,
                        MissingValue = p.MissingValue,
                        ReplaceValue = p.ReplaceValue,
                        MissingPhoto = p.MissingPhoto,
                        ReplacePhoto = p.ReplacePhoto
                    }).ToListAsync());

                multiEntryTables.Add(async num => await _context.E18flinspects
                    .Where(p => p.LocoNumber == num)
                    .OrderByDescending(p => p.Id)
                    .Select(p => new InspectLocoRow
                    {
                        RefurbishValue = p.RefurbishValue,
                        MissingValue = p.MissingValue,
                        ReplaceValue = p.ReplaceValue,
                        MissingPhoto = p.MissingPhoto,
                        ReplacePhoto = p.ReplacePhoto
                    }).ToListAsync());

                multiEntryTables.Add(async num => await _context.E18hcinspects
                    .Where(p => p.LocoNumber == num)
                    .OrderByDescending(p => p.Id)
                    .Select(p => new InspectLocoRow
                    {
                        RefurbishValue = p.RefurbishValue,
                        MissingValue = p.MissingValue,
                        ReplaceValue = p.ReplaceValue,
                        MissingPhoto = p.MissingPhoto,
                        ReplacePhoto = p.ReplacePhoto
                    }).ToListAsync());

                multiEntryTables.Add(async num => await _context.E18hvinspects
                    .Where(p => p.LocoNumber == num)
                    .OrderByDescending(p => p.Id)
                    .Select(p => new InspectLocoRow
                    {
                        RefurbishValue = p.RefurbishValue,
                        MissingValue = p.MissingValue,
                        ReplaceValue = p.ReplaceValue,
                        MissingPhoto = p.MissingPhoto,
                        ReplacePhoto = p.ReplacePhoto
                    }).ToListAsync());

                multiEntryTables.Add(async num => await _context.E18lvinspects
                    .Where(p => p.LocoNumber == num)
                    .OrderByDescending(p => p.Id)
                    .Select(p => new InspectLocoRow
                    {
                        RefurbishValue = p.RefurbishValue,
                        MissingValue = p.MissingValue,
                        ReplaceValue = p.ReplaceValue,
                        MissingPhoto = p.MissingPhoto,
                        ReplacePhoto = p.ReplacePhoto
                    }).ToListAsync());

                multiEntryTables.Add(async num => await _context.E18mainspects
                    .Where(p => p.LocoNumber == num)
                    .OrderByDescending(p => p.Id)
                    .Select(p => new InspectLocoRow
                    {
                        RefurbishValue = p.RefurbishValue,
                        MissingValue = p.MissingValue,
                        ReplaceValue = p.ReplaceValue,
                        MissingPhoto = p.MissingPhoto,
                        ReplacePhoto = p.ReplacePhoto
                    }).ToListAsync());

                multiEntryTables.Add(async num => await _context.E18mbinspects
                    .Where(p => p.LocoNumber == num)
                    .OrderByDescending(p => p.Id)
                    .Select(p => new InspectLocoRow
                    {
                        RefurbishValue = p.RefurbishValue,
                        MissingValue = p.MissingValue,
                        ReplaceValue = p.ReplaceValue,
                        MissingPhoto = p.MissingPhoto,
                        ReplacePhoto = p.ReplacePhoto
                    }).ToListAsync());

                multiEntryTables.Add(async num => await _context.E18rfinspects
                    .Where(p => p.LocoNumber == num)
                    .OrderByDescending(p => p.Id)
                    .Select(p => new InspectLocoRow
                    {
                        RefurbishValue = p.RefurbishValue,
                        MissingValue = p.MissingValue,
                        ReplaceValue = p.ReplaceValue,
                        MissingPhoto = p.MissingPhoto,
                        ReplacePhoto = p.ReplacePhoto
                    }).ToListAsync());
            }
            else if (locoInfo.LocoModel == "GE34")
            {
                multiEntryTables.Add(async num => await _context.Ge34acinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Ge34bcinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Ge34bdinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Ge34bsinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Ge34cfinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Ge34clinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Ge34deinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Ge34ecinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Ge34edinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Ge34flinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Ge34odinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Ge34sninspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Ge34rfinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());
            }
            else if (locoInfo.LocoModel == "GE35")
            {
                multiEntryTables.Add(async num => await _context.Ge35bcinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Ge35bdinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Ge35bsinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Ge35cfinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Ge35clinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Ge35edinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Ge35deinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Ge35ecinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Ge35flinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Ge35mginspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Ge35odinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Ge35sninspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Ge35rfinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());
            }
            else if (locoInfo.LocoModel == "GE36")
            {
                multiEntryTables.Add(async num => await _context.Ge36deinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Ge36bdinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Ge36cainspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Ge36cfinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Ge36clinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Ge36ecinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Ge36edinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Ge36flinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Ge36mginspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Ge36sninspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Ge36rfinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());
            }
            else if (locoInfo.LocoModel == "GM34")
            {
                multiEntryTables.Add(async num => await _context.Gm34deinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm34bdinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm34blinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm34bsinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm34cainspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm34cbinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm34cfinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm34clinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm34edinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm34elinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm34flinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm34lminspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm34mpinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm34sninspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm34trinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm34rfinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());
            }
            else if (locoInfo.LocoModel == "GM35")
            {
                multiEntryTables.Add(async num => await _context.Gm35deinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm35blinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm35bsinspects
       .Where(p => p.LocoNumber == num)
       .OrderByDescending(p => p.Id)
       .Select(p => new InspectLocoRow
       {
           RefurbishValue = p.RefurbishValue,
           MissingValue = p.MissingValue,
           ReplaceValue = p.ReplaceValue,
           MissingPhoto = p.MissingPhoto,
           ReplacePhoto = p.ReplacePhoto
       }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm35cainspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm35cbinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm35cfinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm35clinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm35edinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm35elinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm35flinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm35lminspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm35mpinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm35sninspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm35trinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm35wainspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm35rfinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());


            }
            else if (locoInfo.LocoModel == "GM35")
            {
                multiEntryTables.Add(async num => await _context.Gm36wainspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm36flinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm36sninspects
       .Where(p => p.LocoNumber == num)
       .OrderByDescending(p => p.Id)
       .Select(p => new InspectLocoRow
       {
           RefurbishValue = p.RefurbishValue,
           MissingValue = p.MissingValue,
           ReplaceValue = p.ReplaceValue,
           MissingPhoto = p.MissingPhoto,
           ReplacePhoto = p.ReplacePhoto
       }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm36bvinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm36clinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm36cbinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm36bsinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm36lminspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm36lcinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm36trinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm36bpinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm36cainspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm36ecinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());

                multiEntryTables.Add(async num => await _context.Gm36cfinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());
                multiEntryTables.Add(async num => await _context.Gm36deinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());
                multiEntryTables.Add(async num => await _context.Gm36rfinspects
                   .Where(p => p.LocoNumber == num)
                   .OrderByDescending(p => p.Id)
                   .Select(p => new InspectLocoRow
                   {
                       RefurbishValue = p.RefurbishValue,
                       MissingValue = p.MissingValue,
                       ReplaceValue = p.ReplaceValue,
                       MissingPhoto = p.MissingPhoto,
                       ReplacePhoto = p.ReplacePhoto
                   }).ToListAsync());


            }
            //Please add tables for LocoModel GM36

            foreach (var tableQuery in multiEntryTables)
            {
                var rows = await tableQuery((int)locoNumber);

                foreach (var r in rows)
                {
                    if (TryParseDecimal(r.RefurbishValue, out var rv) && rv != 0m) refurbishValues.Add(rv);
                    if (TryParseDecimal(r.MissingValue, out var mv) && mv != 0m) missingValues.Add(mv);
                    if (TryParseDecimal(r.ReplaceValue, out var xv) && xv != 0m) replaceValues.Add(xv);
                    

                    if (!string.IsNullOrWhiteSpace(r.MissingPhoto) && r.MissingPhoto != "No Photo") missingPhotosAll.Add(r.MissingPhoto.Trim());
                    if (!string.IsNullOrWhiteSpace(r.ReplacePhoto) && r.ReplacePhoto != "No Photo") replacePhotosAll.Add(r.ReplacePhoto.Trim());
                }
            }

            missingPhotosAll = missingPhotosAll.Distinct().ToList();
            replacePhotosAll = replacePhotosAll.Distinct().ToList();

            // ---------- Totals ----------
            string refurbishTotal = refurbishValues.Any() ? refurbishValues.Sum().ToString("0.00", CultureInfo.InvariantCulture) : "0.00";
            string missingTotal = missingValues.Any() ? missingValues.Sum().ToString("0.00", CultureInfo.InvariantCulture) : "0.00";
            string replaceTotal = replaceValues.Any() ? replaceValues.Sum().ToString("0.00", CultureInfo.InvariantCulture) : "0.00";

            // ---------- Photos Serialization ----------
            string missingPhotosSerialized = missingPhotosAll.Any()
                ? JsonSerializer.Serialize(missingPhotosAll)
                : JsonSerializer.Serialize(new List<string> { "No Photos" });

            string replacePhotosSerialized = replacePhotosAll.Any()
                ? JsonSerializer.Serialize(replacePhotosAll)
                : JsonSerializer.Serialize(new List<string> { "No Photos" });
var existingEntry = await _context.LocoDashboards.FirstOrDefaultAsync(e => e.LocoNumber == locoNumber);
            if (existingEntry != null)
            {
                existingEntry.MissingPhotos = missingPhotosSerialized;
                existingEntry.ReplacePhotos = replacePhotosSerialized;
                existingEntry.RefurbishValue = refurbishTotal;
                existingEntry.MissingValue = missingTotal;  
                existingEntry.ReplaceValue = replaceTotal;
              await  _context.SaveChangesAsync();
            }
          
        }
        return Ok(new { success = true, message = "Loco dashboard entry created" });
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
            catch(Exception Ex)
            {
                
                // Network failure → retry
            }

            // Delay before next retry (exponential)
            await Task.Delay(delayMs);
            delayMs *= 2; // 500 → 1000 → 2000
        }

        return "Not Captured";
    }

    private async Task<string> GetCityFromCoordinatesAsyncOM(double latitude, double longitude)
    {
        var client = _httpClientFactory.CreateClient();

        string url =
            $"https://api.bigdatacloud.net/data/reverse-geocode-client" +
            $"?latitude={latitude.ToString(CultureInfo.InvariantCulture)}" +
            $"&longitude={longitude.ToString(CultureInfo.InvariantCulture)}" +
            $"&localityLanguage=en";

        const int maxRetries = 3;
        int delayMs = 500;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                using var resp = await client.GetAsync(url);

                if (resp.IsSuccessStatusCode)
                {
                    var json = await resp.Content.ReadAsStringAsync();
                    var obj = JObject.Parse(json);

                    string? city =
                        obj["city"]?.ToString()
                        ?? obj["locality"]?.ToString()
                        ?? obj["principalSubdivision"]?.ToString()
                        ?? obj["localityInfo"]?["administrative"]?
                            .FirstOrDefault(a => a["adminLevel"]?.ToString() == "6")?["name"]?.ToString();

                    return string.IsNullOrWhiteSpace(city) ? "Not Captured" : city;
                }
            }
            catch
            {
                // network error → retry
            }

            await Task.Delay(delayMs);
            delayMs *= 2;
        }

        return "Not Captured";
    }
    [HttpPost("ReuploadLocos")]
    public async Task<IActionResult> ReUploadLocos([FromBody] List<UploadLocoItem> items)
    {
        try
        {
            if (items == null || !items.Any())
                return BadRequest("No locos selected for upload.");
            string serverFolder = @"C:\LocoDashboardItemsUploaded";
            if (!Directory.Exists(serverFolder))
                Directory.CreateDirectory(serverFolder);

            //PLEASE ADD
            // --- Create ZIP file name including wagon numbers ---
            string wagonNumbersPart = string.Join("_", items.Select(i => i.LocoNumber));
            string zipName = $"LocoDashboardReUpload_{wagonNumbersPart}_{DateTime.Now:yyyyMMdd_HHmmss}.zip";
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
    [HttpPost("reUploadWagons")]
    public async Task<IActionResult> ReUploadWagons([FromBody] List<UploadRequestItem> items)
    {
        if (items == null || !items.Any())
            return BadRequest("No wagons selected for upload.");

        // --- Ensure server folder exists ---
        string serverFolder = @"C:\WagonDashboardItemsUploaded";
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

                bool exists = await _context.WagonDashboardUploadeds
                    .AnyAsync(e => e.WagonNumber == item.WagonNumber);

                if (exists)
                {
                    var dashboardEntry = await _context.WagonDashboardUploadeds.FirstOrDefaultAsync(w => w.WagonNumber == item.WagonNumber);

                    if (dashboardEntry != null)
                    {
                        dashboardEntry.WagonStatus = "Uploaded";
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

    private static bool IsImage(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext == ".jpg" || ext == ".jpeg" || ext == ".png";
    }

    //PLEASE ADD (IMAGE COMPRESSION)
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

        // 🔥 STRIP EXIF / IPTC / XMP
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

        // 🚨 Final guard: never make file bigger
        if (output.Length >= originalBytes.Length)
            return new MemoryStream(originalBytes);

        output.Position = 0;
        return output;
    }


    //PLEASE ADD (EXIF STRIP HELPER)
    private static void StripImageMetadata(SixLabors.ImageSharp.Image image)
    {
        // Remove EXIF
        image.Metadata.ExifProfile = null;

        // Remove IPTC (sometimes present)
        image.Metadata.IptcProfile = null;

        // Remove XMP (can be large)
        image.Metadata.XmpProfile = null;
    }
    public class ConditionRequest
    {
        public string WagonNumber { get; set; } = string.Empty;
        public int ConditionScore { get; set; }
    }
    public class LocoConditionRequest
    {
        public string LocoNumber { get; set; } = string.Empty;
        public int ConditionScore { get; set; }
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
    public class RecalculateRequest
    {
        public string WagonNumber { get; set; } = string.Empty;
    }
    public class ConditionRequestUpload
    {
        public string WagonNumber { get; set; } = string.Empty;
        public int ConditionScore { get; set; }
    }
    public class RecalculateRequestUpload
    {
        public string WagonNumber { get; set; } = string.Empty;
    }

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
                    w.UploadStatus, w.City,
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
                    ConditionScore = w.ConditionScore.ToString() ?? "", //PLEASE ADJUST (NEW)
                    OperationalStatus = w.OperationalStatus ?? "" //PLEASE ADJUST (NEW)
                })
                .ToListAsync();

            return Ok(dashboardEntries);
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = "An error occurred while retrieving the LocoDashboard entries.", error = ex.Message });//Detailed error for debugging  
        }
    }

    [HttpGet("getAllLocos")]
    public async Task<IActionResult> GetAllLocos()
    {
        try
        {


            var dashboardEntries = await _context.LocoDashboards
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
                    w.UploadStatus, w.City,
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
                    w.TotalLaborValue,
                    w.MarketValue,
                    ConditionScore = w.ConditionScore.ToString() ?? "", //PLEASE ADJUST (NEW)
                    OperationalStatus = w.OperationalStatus ?? "", //PLEASE ADJUST (NEW)
                    CalScore = w.CalScore.ToString() ?? "",
                    w.CalOperateStatus,
                    w.CalCondition
                })
                .ToListAsync();

            return Ok(dashboardEntries);
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = "An error occurred while retrieving the LocoDashboard entries.", error = ex.Message });//Detailed error for debugging  
        }
    }
    [HttpGet("getUploadedLocoDashboard")]
    public async Task<IActionResult> GetUploadedLocoDashboard()
    {
        try
        {


            var dashboardEntries = await _context.LocoDashboards
                .Where(w => w.UploadStatus == "Uploaded" || w.UploadStatus == "Re-uploaded")
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
                    w.UploadStatus, w.City,
                    w.UploadDate,
                    w.LocoPhoto,
                    w.MissingPhotos,
                    w.ReplacePhotos,
                    w.GpsLatitude,
                    w.GpsLongitude,
                    w.StartTimeInspect,
                    w.AssetValue,
                    w.TotalValue,
                    w.TotalLaborValue,
                    w.AssessmentSow,
                    w.MarketValue,
                    ConditionScore = w.ConditionScore.ToString() ?? "", //PLEASE ADJUST (NEW)
                    OperationalStatus = w.OperationalStatus ?? "", //PLEASE ADJUST (NEW)
                    CalScore = w.CalScore.ToString() ?? "",
                    w.CalOperateStatus,
                    w.CalCondition
                })
                .ToListAsync();

            return Ok(dashboardEntries);
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = "An error occurred while retrieving the LocoDashboard entries.", error = ex.Message });//Detailed error for debugging  
        }
    }
    [HttpGet("getTickLocoDashboard")]
    public async Task<IActionResult> GetTickLocoDashboard()
    {
        //await AutoInsertMissingWagonsAsync();

        try
        {


            var dashboardEntries = await _context.LocoDashboards
                .Where(w => w.UploadStatus == "Assessor Ticked")
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
                    w.UploadStatus, w.City,
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
                    ConditionScore = w.ConditionScore.ToString() ?? "", //PLEASE ADJUST (NEW)
                    OperationalStatus = w.OperationalStatus ?? "" //PLEASE ADJUST (NEW)

                })
                .ToListAsync();

            return Ok(dashboardEntries);
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = "An error occurred while retrieving the LocoDashboard entries.", error = ex.Message });//Detailed error for debugging  
        }
    }

    [HttpPost("tickLoco")]
    public async Task<IActionResult> TickLoco([FromBody] TickLocoRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.LocoNumber))
            return BadRequest("Loco number is required.");

        if (!int.TryParse(request.LocoNumber, out int wagonNumber))
            return BadRequest("Invalid wagon number.");

        // Fetch wagon data
        var dash = await _context.LocoDashboards
            .FirstOrDefaultAsync(w => w.LocoNumber == wagonNumber);

        if (dash == null)
            return NotFound($"Loco with number {request.LocoNumber} not found.");

        dash.UploadStatus = "Assessor Ticked";

        try
        {
            _context.LocoDashboards.Update(dash);
            await _context.SaveChangesAsync();
            return Ok(new { message = $"Loco {request.LocoNumber} status updated to 'Assessor Ticked'." });
        }
        catch (DbUpdateException ex)
        {
            return StatusCode(500, $"Error updating loco: {ex.Message}");
        }
    }


    [HttpGet("GetInspectionStatus")]
    public async Task<IActionResult> GetInspectionStatus(string phase = "Phase1")
    {
        var result = new List<InspectionStatusDto>();

        try
        {
            using (var cmd = _context.Database.GetDbConnection().CreateCommand())
            {
                cmd.CommandText = "sp_GetInspectionStatus";
                cmd.CommandType = CommandType.StoredProcedure;

                // ✅ Add Phase Parameter
                var phaseParam = cmd.CreateParameter();
                phaseParam.ParameterName = "@Phase";
                phaseParam.Value = phase ?? "1";
                phaseParam.DbType = DbType.String;
                cmd.Parameters.Add(phaseParam);

                await _context.Database.OpenConnectionAsync();

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        result.Add(new InspectionStatusDto
                        {
                            InspectionType = reader["InspectionType"]?.ToString(),
                            Total = reader["Total"] != DBNull.Value ? Convert.ToInt32(reader["Total"]) : 0,
                            Inspected = reader["Inspected"] != DBNull.Value ? Convert.ToInt32(reader["Inspected"]) : 0,
                            Pending = reader["Pending"] != DBNull.Value ? Convert.ToInt32(reader["Pending"]) : 0,
                            CompletionPercent = reader["CompletionPercent"] != DBNull.Value
                                ? Convert.ToDecimal(reader["CompletionPercent"])
                                : 0
                        });
                    }
                }
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
    [HttpGet("GetLocoStatusList")]
    public async Task<IActionResult> GetLocoStatusList(int phase = 1)
    {
        string masterTable = phase switch
        {
            1 => "MasterLocos",
            2 => "MasterLocosTFR",
            3 => "MasterLocosTE",
            _ => "MasterLocos"
        };

        var sql = $@"
SELECT 
    MW.LocoNumber, 
    ISNULL(MW.LocoType, '') AS LocoType,
    ISNULL(MW.LocoModel, '') AS LocoModel,
    CASE 
        WHEN WIC.LocoNumber IS NOT NULL THEN 'Incomplete'
        ELSE 'Not Started'
    END AS Status
FROM {masterTable} MW
LEFT JOIN LocoDashboard WD 
    ON MW.LocoNumber = WD.LocoNumber
LEFT JOIN LocoInfoCaptures WIC 
    ON MW.LocoNumber = WIC.LocoNumber
WHERE WD.LocoNumber IS NULL
ORDER BY MW.LocoNumber ASC";

        var result = await _context.Database
            .SqlQueryRaw<LocoStatusDto>(sql)
            .ToListAsync();

        return Ok(result.Distinct());
    }
    [HttpGet("GetWagonStatusList")]
    public async Task<IActionResult> GetWagonStatusList(int phase = 1)
    {
        // Safety check
        if (phase != 1 && phase != 2)
            phase = 1;

        IQueryable<WagonStatusDto> query;

        if (phase == 2)
        {
            // 🔹 PHASE 2 → MasterWagonsTFR
            query =
                from mw in _context.MasterWagonsTFR
                join wd in _context.WagonDashboards
                    on mw.WagonNumber equals wd.WagonNumber into wdGroup
                from wd in wdGroup.DefaultIfEmpty()

                join wic in _context.WagonInfoCaptures
                    on mw.WagonNumber equals wic.WagonNumber into wicGroup
                from wic in wicGroup.DefaultIfEmpty()

                select new WagonStatusDto
                {
                    WagonNumber = mw.WagonNumber.ToString(),
                    WagonGroup = mw.WagonGroup.ToString(),
                    WagonType = mw.WagonType,
                    Status = (wic != null) ? "Incomplete" : "Not Started"
                };
        }
        else
        {
            // 🔹 PHASE 1 → MasterWagons
            query =
                from mw in _context.MasterWagons
                join wd in _context.WagonDashboards
                    on mw.WagonNumber equals wd.WagonNumber into wdGroup
                from wd in wdGroup.DefaultIfEmpty()

                join wic in _context.WagonInfoCaptures
                    on mw.WagonNumber equals wic.WagonNumber into wicGroup
                from wic in wicGroup.DefaultIfEmpty()

                select new WagonStatusDto
                {
                    WagonNumber = mw.WagonNumber.ToString(),
                    WagonGroup = mw.WagonClass.ToString(),
                    WagonType = mw.WagonType,
                    Status = (wic != null) ? "Incomplete" : "Not Started"
                };
        }

        var result = await query.ToListAsync();

        return Ok(result.DistinctBy(x => x.WagonNumber));
    }

    public class WagonStatusDto
    {
        public string WagonNumber { get; set; }
        public string WagonGroup { get; set; }
        public string WagonType { get; set; }
        public string Status { get; set; }
    }


    public class LocoStatusDto
    {
        public int LocoNumber { get; set; }
        public string LocoType { get; set; }
        public string LocoModel { get; set; }
        public string Status { get; set; }
    }
    public class TickWagonRequest
    {
        public string WagonNumber { get; set; } = string.Empty;
    }
    public class TickLocoRequest
    {
        public string LocoNumber { get; set; } = string.Empty;
    }

}
