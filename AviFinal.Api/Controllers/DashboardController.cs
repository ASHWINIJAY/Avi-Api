using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AviFinal.Api.Models;
using AviFinal.Api.Models;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly AviDbContext _context;

    //private readonly AppDbContext _localDb;

    public DashboardController(AviDbContext context)
    {
        _context = context;
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
            ReplacePhotos = replacePhotosSerialized
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
                    w.ReplacePhotos
                })
                .ToListAsync();

            return Ok(dashboardEntries);
        }
        catch(Exception ex)
        {
              return BadRequest(new { success = false, message = "An error occurred while retrieving the LocoDashboard entries.", error = ex.Message });//Detailed error for debugging  
        }
        }

}
