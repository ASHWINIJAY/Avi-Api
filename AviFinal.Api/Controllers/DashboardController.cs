using AviFinal.Api.DTO;
using AviFinal.Api.Models;
using AviFinal.Api.Models;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Presentation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
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

        //PLEASE ADD
        decimal marketValue = 0; //PLEASE ADD

        //PLEASE ADD
        if (master?.MarketValue != null && !string.IsNullOrWhiteSpace(master.MarketValue.ToString()))
            decimal.TryParse(master.MarketValue.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out marketValue);

        //PLEASE ADD
        decimal repairTotal = refurbishValues.Sum() + missingValues.Sum() + replaceValues.Sum() + laborValues.Sum() + liftBarrelTotal;
        decimal assetValue = marketValue - repairTotal;
        string totalAssetValue = assetValue.ToString("0.00", CultureInfo.InvariantCulture);
        string rts = repairTotal.ToString("0.00", CultureInfo.InvariantCulture);

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
            MarketValue = master?.MarketValue ?? "0.00", //PLEASE ADJUST
            TotalLaborValue = laborTotal,
            AssetValue = totalAssetValue ?? "0.00", //PLEASE ADJUST
            AssessmentSow = "Not Ready", //PLEASE ADD
            LiftValue = liftCost.ToString("0.00", CultureInfo.InvariantCulture), //PLEASE ADD
            BarrelValue = barrelCost.ToString("0.00", CultureInfo.InvariantCulture), //PLEASE ADD
            TotalValue = rts ?? "0.00", //PLEASE ADD,
            City = city
        };

        _context.WagonDashboards.Add(dashboardEntry);
        await _context.SaveChangesAsync();

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

                void AddFilesToZip(string? source, string targetFolder)
                {
                    if (string.IsNullOrWhiteSpace(source) || source == "N/A") return;

                    List<string> paths = new();
                    if (source.StartsWith("[")) // JSON array
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
                        if (System.IO.File.Exists(sourcePath))
                        {
                            string entryName = Path.Combine(targetFolder, Path.GetFileName(sourcePath));
                            zipArchive.CreateEntryFromFile(sourcePath, entryName);
                        }
                    }
                }

                // Use reflection to loop through all properties dynamically
                var properties = typeof(UploadRequestItem).GetProperties();
                foreach (var prop in properties)
                {
                    if (!folderMap.ContainsKey(prop.Name)) continue;

                    var value = prop.GetValue(item) as string;
                    AddFilesToZip(value, folderMap[prop.Name]);
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
                        };

                        _context.WagonDashboardUploadeds.Add(uploadedEntry);
                    }

                    await _context.SaveChangesAsync();

                }
                }
        }

        return Ok(new { success = true, zipPath, zipName });
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
                OperationalStatus = w.OperationalStatus ?? "N/A" //PLEASE ADD (NEW)
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

                    void AddFilesToZip(string? source, string targetFolder)
                    {
                        if (string.IsNullOrWhiteSpace(source) || source == "N/A") return;

                        List<string> paths = new();
                        if (source.StartsWith("[")) // JSON array
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
                            if (System.IO.File.Exists(sourcePath))
                            {
                                string entryName = Path.Combine(targetFolder, Path.GetFileName(sourcePath));
                                zipArchive.CreateEntryFromFile(sourcePath, entryName);
                            }
                        }
                    }

                    // Use reflection to loop through all properties dynamically
                    var properties = typeof(UploadLocoItem).GetProperties();
                    foreach (var prop in properties)
                    {
                        if (!folderMap.ContainsKey(prop.Name)) continue;

                        var value = prop.GetValue(item) as string;
                        AddFilesToZip(value, folderMap[prop.Name]);
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
                OperationalStatus = w.OperationalStatus ?? "" //PLEASE ADJUST (NEW)
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
                OperationalStatus = w.OperationalStatus ?? "" //PLEASE ADJUST (NEW)

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
            })
            .ToListAsync();

        return Ok(score);
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
            TotalValue = rts ?? "0.00"
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

                multiEntryTables.Add(async num => await _context.Gm36elinspects
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

                multiEntryTables.Add(async num => await _context.Gm36elinspects
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
        try
        {
            string? apiKey = _config["LocationIQ:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
                return "Not Captured";

            var client = _httpClientFactory.CreateClient();
            string url = $"https://us1.locationiq.com/v1/reverse.php?key={apiKey}&lat={latitude.ToString(CultureInfo.InvariantCulture)}&lon={longitude.ToString(CultureInfo.InvariantCulture)}&format=json";

            using (var resp = await client.GetAsync(url))
            {
                if (!resp.IsSuccessStatusCode) return "Not Captured";
                var json = await resp.Content.ReadAsStringAsync();
                var obj = JObject.Parse(json);
                string? city = obj["address"]?["city"]?.ToString()
                           ?? obj["address"]?["town"]?.ToString()
                           ?? obj["address"]?["village"]?.ToString()
                           ?? obj["address"]?["county"]?.ToString();
                return string.IsNullOrWhiteSpace(city) ? "Not Captured" : city;
            }
        }
        catch
        {
            return "Not Captured";
        }
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
    [HttpGet("getUploadedLocoDashboard")]
    public async Task<IActionResult> GetUploadedLocoDashboard()
    {
        try
        {


            var dashboardEntries = await _context.LocoDashboards
                .Where(w => w.UploadStatus == "Uploaded")
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
    public async Task<IActionResult> GetInspectionStatus()
    {
        var result = new List<InspectionStatusDto>();

        using (var cmd = _context.Database.GetDbConnection().CreateCommand())
        {
            cmd.CommandText = "sp_GetInspectionStatus";
            cmd.CommandType = CommandType.StoredProcedure;

            await _context.Database.OpenConnectionAsync();

            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    result.Add(new InspectionStatusDto
                    {
                        InspectionType = reader["InspectionType"].ToString(),
                        Total = Convert.ToInt32(reader["Total"]),
                        Inspected = Convert.ToInt32(reader["Inspected"]),
                        Pending = Convert.ToInt32(reader["Pending"]),
                        CompletionPercent = Convert.ToDecimal(reader["CompletionPercent"])
                    });
                }
            }
        }

        return Ok(result);
    }
    [HttpGet("GetLocoStatusList")]
    public async Task<IActionResult> GetLocoStatusList()
    {
        var sql = @"
SELECT 
    MW.LocoNumber, 
    ISNULL(MW.LocoType, '') AS LocoType,
    ISNULL(MW.LocoModel, '') AS LocoModel,
    CASE 
        WHEN WIC.LocoNumber IS NOT NULL THEN 'Incomplete'
        ELSE 'Not Started'
    END AS Status
FROM MasterLocos MW
LEFT JOIN LocoDashboard WD 
    ON MW.LocoNumber = WD.LocoNumber
LEFT JOIN LocoInfoCaptures WIC 
    ON MW.LocoNumber = WIC.LocoNumber
WHERE WD.LocoNumber IS NULL
ORDER BY MW.LocoNumber ASC";


        var result = await _context.Database
            .SqlQueryRaw<LocoStatusDto>(sql)
            .ToListAsync();

        return Ok(result);
    }
    [HttpGet("GetWagonStatusList")]
    public async Task<IActionResult> GetWagonStatusList()
    {
        var result = await (
            from mw in _context.MasterWagons
            join wd in _context.WagonDashboards
                on mw.WagonNumber equals wd.WagonNumber into wdGroup
            from wd in wdGroup.DefaultIfEmpty()

            join wic in _context.WagonInfoCaptures
                on mw.WagonNumber equals wic.WagonNumber into wicGroup
            from wic in wicGroup.DefaultIfEmpty()

            select new WagonStatusDto
            {
                WagonNumber = mw.WagonNumber.ToString(),   // <-- convert INT to string
                WagonGroup = mw.WagonClass.ToString(),
                WagonType = mw.WagonType,
                Status = (wic != null) ? "Incomplete" : "Not Started"
            }
        ).ToListAsync();

        return Ok(result);
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
