using AviFinal.Api.DTO;
using AviFinal.Api.Models;
using AviFinal.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
    }
    private readonly AviDbContext _context;
    private readonly IWebHostEnvironment _env;

    //private readonly AppDbContext _localDb;

    public DashboardController(AviDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
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
        string inspectorName = leaseUser?.UserName ?? "Unknown User";

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

										  w.NetBookValue, //PLEASE ADD

										  w.StartInspectTime, //PLEASE ADD

										  w.GpsLatitude, //PLEASE ADD

										  w.GpsLongitude //PLEASE ADD
									  })
                                      .FirstOrDefaultAsync();

        if (wagonInfo == null)
            return NotFound(new { success = false, message = $"No WagonInfoCaptures record found for wagon {wagonNumber}" });

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
        var missingPhotosAll = new List<string>();
        var replacePhotosAll = new List<string>();
		var laborValues = new List<decimal>(); //PLEASE ADD

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

										   LaborValue = p.LaborValue //PLEASE ADD
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

										   LaborValue = p.LaborValue //PLEASE ADD
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

										   LaborValue = p.LaborValue //PLEASE ADD
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

										   LaborValue = p.LaborValue //PLEASE ADD
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

										   LaborValue = p.LaborValue //PLEASE ADD
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

										   LaborValue = p.LaborValue //PLEASE ADD
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

										   LaborValue = p.LaborValue //PLEASE ADD
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

										   LaborValue = p.LaborValue //PLEASE ADD
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

										   LaborValue = p.LaborValue //PLEASE ADD
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

        // ---------- Insert Dashboard ----------
        var dashboardEntry = new WagonDashboard
        {
			InspectorId = userId ?? "No User",

			InspectorName = inspectorName ?? "No User",
			WagonNumber = wagonNumber,
            WagonGroup = wagonInfo.WagonGroup ?? string.Empty,
            WagonType = wagonInfo.WagonType ?? string.Empty,
            DateAssessed = DateTime.Now.ToString("yyyy-MM-dd"),
            TimeAssessed = DateTime.Now.ToString("HH:mm:ss"),
            BodyDamage = bodyDamage,
            BodyPhotos = bodyPhotosSerialized,
            LiftPhoto = wagonInfo.LiftPhoto,
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
            UploadStatus = "Not Uploaded",
            UploadDate = "No Date",
            WagonPhoto = wagonInfo.WagonPhoto,
            MissingPhotos = missingPhotosSerialized,
            ReplacePhotos = replacePhotosSerialized,
            GpsLatitude = wagonInfo.GpsLatitude, //PLEASE ADD

			GpsLongitude = wagonInfo.GpsLongitude, //PLEASE ADD

			StartTimeInspect = wagonInfo.StartInspectTime ?? "Not Available", //PLEASE ADD

			ReplacementValue = "Not Available", //PLEASE ADD

			TotalLaborValue = laborTotal, //PLEASE ADD

			AssetValue = wagonInfo.NetBookValue, //PLEASE ADD
		};
        var existingEntry = await _context.WagonDashboards
            .FirstOrDefaultAsync(w => w.WagonNumber == wagonNumber);
        if (existingEntry != null)
        {
			
			

			// NEW FIELDS
			existingEntry.GpsLatitude = wagonInfo.GpsLatitude;
			existingEntry.GpsLongitude = wagonInfo.GpsLongitude;
			existingEntry.StartTimeInspect = wagonInfo.StartInspectTime ?? "Not Available";
			existingEntry.ReplacementValue = "Not Available";
			existingEntry.TotalLaborValue = laborTotal;
			existingEntry.AssetValue = wagonInfo.NetBookValue;

			await _context.SaveChangesAsync();

			return Ok(new { success = true, message = "Wagon dashboard updated successfully" });
			//return Conflict(new { success = false, message = $"Wagon dashboard entry already exists for wagon {wagonNumber}" });
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

        //PLEASE ADD
        // --- Ensure server folder exists ---
        string serverFolder = @"C:\WagonDashboardItemsUploaded";
        if (!Directory.Exists(serverFolder))
            Directory.CreateDirectory(serverFolder);

        //PLEASE ADD
        // --- Create ZIP file name including wagon numbers ---
        string wagonNumbersPart = string.Join("_", items.Select(i => i.WagonNumber));
        string zipName = $"WagonDashboardUpload_{wagonNumbersPart}_{DateTime.Now:yyyyMMdd_HHmmss}.zip";
        string zipPath = Path.Combine(serverFolder, zipName);

        //PLEASE REMOVE
        //string tempRoot = Path.Combine(_env.WebRootPath ?? "wwwroot", "WagonUploads");
        //if (!Directory.Exists(tempRoot)) Directory.CreateDirectory(tempRoot);

        //PLEASE REMOVE
        //string zipName = $"WagonDashboardUpload_{DateTime.Now:yyyyMMdd_HHmmss}.zip";
        //string zipPath = Path.Combine(tempRoot, zipName);

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
                { "AssessmentCert", Path.Combine(wagonFolderName, "InspectionCert") } //PLEASE ADD
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
                    dashboardEntry.UploadStatus = "Uploaded";
                    dashboardEntry.UploadDate = DateTime.Now.ToString("yyyy-MM-dd");

                    await _context.SaveChangesAsync(); //PLEASE ADD

                    //PLEASE ADD
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
                        UploadStatus = dashboardEntry.UploadStatus,
                        UploadDate = dashboardEntry.UploadDate,
                        WagonPhoto = dashboardEntry.WagonPhoto,
                        MissingPhotos = dashboardEntry.MissingPhotos,
                        ReplacePhotos = dashboardEntry.ReplacePhotos,
                        GpsLatitude = dashboardEntry.GpsLatitude,
                        GpsLongitude = dashboardEntry.GpsLongitude,
                        StartTimeInspect = dashboardEntry.StartTimeInspect,
                        ReplacementValue = dashboardEntry.ReplacementValue,
                        TotalLaborValue = dashboardEntry.TotalLaborValue,
                        AssetValue = dashboardEntry.AssetValue
                    };

                    _context.WagonDashboardUploadeds.Add(uploadedEntry);
                    await _context.SaveChangesAsync();
                }
            }
        }

        return Ok(new { success = true, zipPath, zipName });

        //PLEASE REMOVE
        //byte[] zipBytes = await System.IO.File.ReadAllBytesAsync(zipPath);
        //return File(zipBytes, "application/zip", zipName);
    }

    [HttpGet("getUploadedWagons")] //PLEASE ADD

    //PLEASE ADD
    public async Task<IActionResult> GetUploadedWagons()
    {

        var dashboardEntries = await _context.WagonDashboardUploadeds
            .Where(w => w.UploadStatus == "Uploaded")
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
                w.UploadStatus,
                w.UploadDate,
                w.WagonPhoto,
                w.MissingPhotos,
                w.ReplacePhotos,
                GpsLatitude = w.GpsLatitude ?? "N/A",
                GpsLongitude = w.GpsLongitude ?? "N/A",
                StartTimeInspect = w.StartTimeInspect ?? "N/A",
                ReplacementValue = w.ReplacementValue ?? "0.00",
                TotalLaborValue = w.TotalLaborValue ?? "0.00",
                AssetValue = w.AssetValue ?? "0.00",
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
                { "AssessmentCert", Path.Combine(wagonFolderName, "InspectionCert") }
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
            .Where(w => w.UploadStatus != "Uploaded")
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
                w.UploadStatus,
                w.UploadDate,
                w.WagonPhoto,
                w.MissingPhotos,
                w.ReplacePhotos,
				GpsLatitude = w.GpsLatitude ?? "N/A", //PLEASE ADD

				GpsLongitude = w.GpsLongitude ?? "N/A", //PLEASE ADD

				StartTimeInspect = w.StartTimeInspect ?? "N/A", //PLEASE ADD

				ReplacementValue = w.ReplacementValue ?? "N/A", //PLEASE ADD

				TotalLaborValue = w.TotalLaborValue ?? "N/A", //PLEASE ADD

				AssetValue = w.AssetValue ?? "N/A", //PLEASE ADD
			})
            .ToListAsync();

        return Ok(dashboardEntries);
    }

    [HttpPost("insertLoco")]
    public async Task<IActionResult> InsertLoco(int locoNumber, string userId)
    {
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
            UploadStatus = "Not Uploaded",
            UploadDate = "No Date",
            LocoPhoto = locoInfo.LocoPhoto,
            MissingPhotos = missingPhotosSerialized,
            ReplacePhotos = replacePhotosSerialized,
            GpsLatitude = locoInfo.GpsLatitude, //PLEASE ADD

            GpsLongitude = locoInfo.GpsLongitude, //PLEASE ADD

            StartTimeInspect = locoInfo.CreatedDate?.ToString("HH:mm:ss") ?? "Not Available", //PLEASE ADD

            ReplacementValue = "Not Available", //PLEASE ADD

            AssetValue = locoInfo.NetBookValue, //PLEASE ADD
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
    public class InspectLocoRow
    {
        public string? RefurbishValue { get; set; }
        public string? MissingValue { get; set; }
        public string? ReplaceValue { get; set; }
        public string? MissingPhoto { get; set; }
        public string? ReplacePhoto { get; set; }
    }

    [HttpGet("getAllLocoDashboard")]
    public async Task<IActionResult> GetAllLocoDashboard()
    {
        try {
          

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
                    w.UploadDate,
                    w.LocoPhoto,
                    w.MissingPhotos,
                    w.ReplacePhotos,
                    w.GpsLatitude,
                    w.GpsLongitude,
                    w.StartTimeInspect,
                    w.AssetValue,
                })
                .ToListAsync();

            return Ok(dashboardEntries);
        }
        catch(Exception ex)
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
                    w.UploadStatus,
                    w.UploadDate,
                    w.LocoPhoto,
                    w.MissingPhotos,
                    w.ReplacePhotos,
                    w.GpsLatitude,
                    w.GpsLongitude,
                    w.StartTimeInspect,
                    w.AssetValue,
                })
                .ToListAsync();

            return Ok(dashboardEntries);
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = "An error occurred while retrieving the LocoDashboard entries.", error = ex.Message });//Detailed error for debugging  
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


}
