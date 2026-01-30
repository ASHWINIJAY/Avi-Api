using AviAppFinal.Server.Controllers;
using AviFinal.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AviFinal.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InspectionDetailsController : ControllerBase
    {
        private readonly AviDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<InspectionDetailsController> _logger;

        public InspectionDetailsController(AviDbContext context, IConfiguration configuration, ILogger<InspectionDetailsController> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }
        [HttpGet("locolist")]
        public async Task<IActionResult> GetLocoList()
        {
            var locos = await _context.LocoDashboards
                .AsNoTracking()
                .Select(x => x.LocoNumber)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            return Ok(locos);
        }

        [HttpGet("wagonlist")]
        public async Task<IActionResult> GetWagonList()
        {
            var locos = await _context.WagonDashboards
                .AsNoTracking()
                .Select(x => x.WagonNumber)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            return Ok(locos);
        }
        private async Task<List<IInspectionRow>> FetchAsync<T>(
     IQueryable<T> query,
     int locoNumber)
        {
            // 1️⃣ Fetch raw data FIRST
            var raw = await query
                
                .Where(x => EF.Property<int>(x, "LocoNumber") == locoNumber)
                .Select(x => new
                {
                    LocoNumber = EF.Property<int>(x, "LocoNumber"),
                    PartDes = EF.Property<string>(x, "PartDescr"),
                    GoodCheck = EF.Property<string>(x, "GoodCheck"),
                    RefurbishValue = EF.Property<string>(x, "RefurbishValue"),
                    MissingValue = EF.Property<string>(x, "MissingValue"),
                    ReplaceValue = EF.Property<string>(x, "ReplaceValue"),
                    LaborValue = EF.Property<string>(x, "LaborValue")
                })
                .ToListAsync();
            var c = raw.Select(x => new InspectionRowDto
            {
                LocoNumber = x.LocoNumber,
                PartDes = x.PartDes,

                Good = x.GoodCheck,

                Refurbish = ToDecimal(x.RefurbishValue),
                Missing = ToDecimal(x.MissingValue),
                Replace = ToDecimal(x.ReplaceValue),
                Labour = ToDecimal(x.LaborValue)
            })
            .Cast<IInspectionRow>()
            .ToList();
            // 2️⃣ Convert in memory (SAFE)
            return raw.Select(x => new InspectionRowDto
            {
                LocoNumber = x.LocoNumber,
                PartDes = x.PartDes,

                Good =x.GoodCheck,

                Refurbish = ToDecimal(x.RefurbishValue),
                Missing = ToDecimal(x.MissingValue),
                Replace = ToDecimal(x.ReplaceValue),
                Labour = ToDecimal(x.LaborValue)
            })
            .Cast<IInspectionRow>()
            .ToList();
        }

        private async Task<List<IInspectionWagonRow>> FetchWagonAsync<T>(
    IQueryable<T> query,
    int WagonNumber)
        {
            return await query
                .Where(x => EF.Property<int>(x, "WagonNumber") == WagonNumber)
                .Select(x => new InspectionRowWagonDto
                {
                    WagonNumber = EF.Property<int>(x, "WagonNumber"),
                    PartDes = EF.Property<string>(x, "PartDescr"),

                    // ✔ Good has only CHECK column
                    Good = EF.Property<string>(x, "GoodCheck"),

                    // ✔ VALUE columns (NVARCHAR → DECIMAL)
                    Refurbish = ToDecimal(EF.Property<string>(x, "RefurbishValue")),
                    Missing = ToDecimal(EF.Property<string>(x, "MissingValue")),
                    Replace = ToDecimal(EF.Property<string>(x, "ReplaceValue")),
                    Labour = ToDecimal(EF.Property<string>(x, "LaborValue"))
                })
                .Cast<IInspectionWagonRow>()
                .ToListAsync();
        }
        public interface IInspectionWagonRow
        {
            int WagonNumber { get; }
            string PartDes { get; }
            string? Good { get; }
            decimal? Refurbish { get; }
            decimal? Missing { get; }
            decimal? Replace { get; }
            decimal? Labour { get; }
        }
        public interface IInspectionRow
        {
            int LocoNumber { get; }
            string PartDes { get; }
            string? Good { get; }
            decimal? Refurbish { get; }
            decimal? Missing { get; }
            decimal? Replace { get; }
            decimal? Labour { get; }
        }
        public class InspectionRowDto : IInspectionRow
        {
            public int LocoNumber { get; set; }
            public string PartDes { get; set; }
            public string? Good { get; set; }
            public decimal? Refurbish { get; set; }
            public decimal? Missing { get; set; }
            public decimal? Replace { get; set; }
            public decimal? Labour { get; set; }
        }

        public class InspectionRowWagonDto : IInspectionWagonRow
        {
            public int WagonNumber { get; set; }
            public string PartDes { get; set; }
            public string? Good { get; set; }
            public decimal? Refurbish { get; set; }
            public decimal? Missing { get; set; }
            public decimal? Replace { get; set; }
            public decimal? Labour { get; set; }
        }

private static decimal ToDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0;

        value = value.Trim();

        // Remove currency symbols if any
        value = value
            .Replace("₹", "")
            .Replace("Rs.", "")
            .Replace("rs.", "")
            .Replace("/-", "")
            .Replace(" ", "");

        // Try parsing using invariant culture
        if (decimal.TryParse(
            value,
            NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands,
            CultureInfo.InvariantCulture,
            out var result))
        {
            return result;
        }

        // Fallback: try current culture (safety net)
        if (decimal.TryParse(
            value,
            NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands,
            CultureInfo.CurrentCulture,
            out result))
        {
            return result;
        }

        return 0;
    }



    [HttpGet("details/{locoNumber}")]
        public async Task<IActionResult> GetInspectionDetails(int locoNumber)
        {
            // 1️⃣ Get loco basic info
            var model = await _context.LocoDashboards
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.LocoNumber == locoNumber);

            if (model == null)
                return NotFound($"Loco {locoNumber} not found.");

            // 2️⃣ Inspection configuration (ALL TABLES)
            var inspectionConfig = new Dictionary<string, List<(string Title, Func<Task<List<IInspectionRow>>>)>>
            {
                ["E18"] = new()
        {
            ("BELOW DECK Walk Around Inspection", () => FetchAsync(_context.E18bdinspects, locoNumber)),
            ("Front of Loco Inspection", () => FetchAsync(_context.E18flinspects, locoNumber)),
            ("Back of Loco Inspection", () => FetchAsync(_context.E18beinspects, locoNumber)),
            ("18E Cab Inspection", () => FetchAsync(_context.E18eeinspects, locoNumber)),
            ("Low Voltage Compartment Inspection", () => FetchAsync(_context.E18lvinspects, locoNumber)),
            ("Corridor Inspection", () => FetchAsync(_context.E18crinspects, locoNumber)),
            ("HT High Voltage Compartment Inspection", () => FetchAsync(_context.E18hvinspects, locoNumber)),
            ("Motor Alternator Set Inspection", () => FetchAsync(_context.E18mainspects, locoNumber)),
            ("Exhauster Inspection", () => FetchAsync(_context.E18ehinspects, locoNumber)),
            ("Machine Brake Compartment Inspection", () => FetchAsync(_context.E18mbinspects, locoNumber)),
            ("High Speed Circuit Breaker Compartment Inspection", () => FetchAsync(_context.E18hsinspects, locoNumber)),
            ("Exciter Set 2 Inspection", () => FetchAsync(_context.E18esinspects, locoNumber)),
            ("High Voltage Compartment No 1 Inspection", () => FetchAsync(_context.E18hcinspects, locoNumber)),
            ("Compressor Compartment Inspection", () => FetchAsync(_context.E18ccinspects, locoNumber)),
            ("Cab and Toilet No 1 End Inspection", () => FetchAsync(_context.E18ctinspects, locoNumber)),
            ("Roof Top Inspection", () => FetchAsync(_context.E18rfinspects, locoNumber))
        },

                ["GE34"] = new()
        {
            ("Walk Around / Below Deck Inspection", () => FetchAsync(_context.Ge34bdinspects, locoNumber)),
            ("Front of Loco Inspection", () => FetchAsync(_context.Ge34flinspects, locoNumber)),
            ("Short Nose Inspection", () => FetchAsync(_context.Ge34sninspects, locoNumber)),
            ("Cab Loco Inspection", () => FetchAsync(_context.Ge34clinspects, locoNumber)),
            ("Electrical Cab Inspection", () => FetchAsync(_context.Ge34ecinspects, locoNumber)),
            ("Battery Switch Inspection", () => FetchAsync(_context.Ge34bsinspects, locoNumber)),
            ("Outside Driver’s Door Inspection", () => FetchAsync(_context.Ge34odinspects, locoNumber)),
            ("Blower Compartment Inspection", () => FetchAsync(_context.Ge34bcinspects, locoNumber)),
            ("Alternator Compartment Inspection", () => FetchAsync(_context.Ge34acinspects, locoNumber)),
            ("Engine Deck Inspection", () => FetchAsync(_context.Ge34edinspects, locoNumber)),
            ("Compressor Fan Inspection", () => FetchAsync(_context.Ge34cfinspects, locoNumber)),
            ("End Deck Inspection", () => FetchAsync(_context.Ge34deinspects, locoNumber)),
            ("Roof Top Inspection", () => FetchAsync(_context.Ge34rfinspects, locoNumber))
        },
                ["GE35"] = new()
{
    ("Walk Around / Below Deck Inspection", () => FetchAsync(_context.Ge35bdinspects, locoNumber)),
    ("Front of Loco Inspection", () => FetchAsync(_context.Ge35flinspects, locoNumber)),
    ("Short Nose Inspection", () => FetchAsync(_context.Ge35sninspects, locoNumber)),
    ("Cab Loco Inspection", () => FetchAsync(_context.Ge35clinspects, locoNumber)),
    ("Electrical Cab Inspection", () => FetchAsync(_context.Ge35ecinspects, locoNumber)),
    ("Battery Switch Inspection", () => FetchAsync(_context.Ge35bsinspects, locoNumber)),
    ("Outside Driver’s Door Inspection", () => FetchAsync(_context.Ge35odinspects, locoNumber)),
    ("Blower Compartment Inspection", () => FetchAsync(_context.Ge35bcinspects, locoNumber)),
    ("Main Gen Compartment Inspection", () => FetchAsync(_context.Ge35mginspects, locoNumber)),
    ("Engine Deck Inspection", () => FetchAsync(_context.Ge35edinspects, locoNumber)),
    ("Compressor Fan Inspection", () => FetchAsync(_context.Ge35cfinspects, locoNumber)),
    ("End Deck Inspection", () => FetchAsync(_context.Ge35deinspects, locoNumber)),
    ("Roof Top Inspection", () => FetchAsync(_context.Ge35rfinspects, locoNumber))
},
                ["GE36"] = new()
{
    ("Walk Around / Below Deck Inspect", () => FetchAsync(_context.Ge36bdinspects, locoNumber)),
    ("Front Loco Inspect", () => FetchAsync(_context.Ge36flinspects, locoNumber)),
    ("Short Nose Inspect", () => FetchAsync(_context.Ge36sninspects, locoNumber)),
    ("Cab Loco Inspect", () => FetchAsync(_context.Ge36clinspects, locoNumber)),
    ("Elect Cab Inspect", () => FetchAsync(_context.Ge36ecinspects, locoNumber)),
    ("Central Air Inspect", () => FetchAsync(_context.Ge36cainspects, locoNumber)),
    ("Main Gen Compartment Inspect", () => FetchAsync(_context.Ge36mginspects, locoNumber)),
    ("Engine Deck Inspect", () => FetchAsync(_context.Ge36edinspects, locoNumber)),
    ("Compressor Fan Inspect", () => FetchAsync(_context.Ge36cfinspects, locoNumber)),
    ("End Deck Inspect", () => FetchAsync(_context.Ge36deinspects, locoNumber)),
    ("Roof Top Inspect", () => FetchAsync(_context.Ge36rfinspects, locoNumber))
},
                ["GM34"] = new()
{
    ("Below Deck From No.1A to 1B", () => FetchAsync(_context.Gm34bdinspects, locoNumber)),
    ("Front of Loco Above", () => FetchAsync(_context.Gm34flinspects, locoNumber)),
    ("Short Nose", () => FetchAsync(_context.Gm34sninspects, locoNumber)),
    ("Cab of Loco Assistant Entrance", () => FetchAsync(_context.Gm34clinspects, locoNumber)),
    ("Elect Cabinet Top Left", () => FetchAsync(_context.Gm34elinspects, locoNumber)),
    ("Battery Knife Switch Compartment", () => FetchAsync(_context.Gm34bsinspects, locoNumber)),
    ("Left Middle Door", () => FetchAsync(_context.Gm34lminspects, locoNumber)),
    ("Circuit Breaker Control Panel", () => FetchAsync(_context.Gm34cbinspects, locoNumber)),
    ("Top Right Panel", () => FetchAsync(_context.Gm34trinspects, locoNumber)),
    ("Middle Panel", () => FetchAsync(_context.Gm34mpinspects, locoNumber)),
    ("Bottom Left Panel", () => FetchAsync(_context.Gm34blinspects, locoNumber)),
    ("Central Air Compartment", () => FetchAsync(_context.Gm34cainspects, locoNumber)),
    ("Engine and Above Deck", () => FetchAsync(_context.Gm34edinspects, locoNumber)),
    ("Compressor Fan Rad Compartment", () => FetchAsync(_context.Gm34cfinspects, locoNumber)),
    ("No.2 End above deck", () => FetchAsync(_context.Gm34deinspects, locoNumber)),
    ("Roof Top Inspect", () => FetchAsync(_context.Gm34rfinspects, locoNumber))
},
                ["GM35"] = new()
{
    ("Below Deck From No.1A to 1B", () => FetchAsync(_context.Gm35wainspects, locoNumber)),
    ("Front of Loco Above", () => FetchAsync(_context.Gm35flinspects, locoNumber)),
    ("Short Nose", () => FetchAsync(_context.Gm35sninspects, locoNumber)),
    ("Cab of Loco Assistant Entrance", () => FetchAsync(_context.Gm35clinspects, locoNumber)),
    ("Elect Cabinet Top Left", () => FetchAsync(_context.Gm35elinspects, locoNumber)),
    ("Battery Knife Switch Compartment", () => FetchAsync(_context.Gm35bsinspects, locoNumber)),
    ("Left Middle Door", () => FetchAsync(_context.Gm35lminspects, locoNumber)),
    ("Circuit Breaker Control Panel", () => FetchAsync(_context.Gm35cbinspects, locoNumber)),
    ("Top Right Panel", () => FetchAsync(_context.Gm35trinspects, locoNumber)),
    ("Middle Panel", () => FetchAsync(_context.Gm35mpinspects, locoNumber)),
    ("Bottom Left Panel", () => FetchAsync(_context.Gm35blinspects, locoNumber)),
    ("Central Air Compartment", () => FetchAsync(_context.Gm35cainspects, locoNumber)),
    ("Engine and Above Deck", () => FetchAsync(_context.Gm35edinspects, locoNumber)),
    ("Compressor Fan Rad Compartment", () => FetchAsync(_context.Gm35cfinspects, locoNumber)),
    ("No.2 End Above Deck", () => FetchAsync(_context.Gm35deinspects, locoNumber)),
    ("Roof Top Inspect", () => FetchAsync(_context.Gm35rfinspects, locoNumber))
},

                ["GM36"] = new()
        {
            ("Below Deck From No.1A to 1B", () => FetchAsync(_context.Gm36wainspects, locoNumber)),
            ("Front of Loco Above", () => FetchAsync(_context.Gm36flinspects, locoNumber)),
            ("Short Nose", () => FetchAsync(_context.Gm36sninspects, locoNumber)),
            ("Brake Valve Compartment", () => FetchAsync(_context.Gm36bvinspects, locoNumber)),
            ("Cab of Loco Assistant Entrance", () => FetchAsync(_context.Gm36clinspects, locoNumber)),
            ("Elect Cabinet Top Left", () => FetchAsync(_context.Gm36ecinspects, locoNumber)),
            ("Circuit Breaker Control Panel", () => FetchAsync(_context.Gm36cbinspects, locoNumber)),
            ("Battery Knife Switch Compartment", () => FetchAsync(_context.Gm36bsinspects, locoNumber)),
            ("Left Middle Door", () => FetchAsync(_context.Gm36lminspects, locoNumber)),
            ("Left Control Panel", () => FetchAsync(_context.Gm36lcinspects, locoNumber)),
            ("Top Right Panel", () => FetchAsync(_context.Gm36trinspects, locoNumber)),
            ("Bottom Panel", () => FetchAsync(_context.Gm36bpinspects, locoNumber)),
            ("Central Air Compartment", () => FetchAsync(_context.Gm36cainspects, locoNumber)),
            ("Engine and Above Deck", () => FetchAsync(_context.Gm36edinspects, locoNumber)),
            ("Compressor Fan Rad Compartment", () => FetchAsync(_context.Gm36cfinspects, locoNumber)),
            ("No.2 End Above Deck", () => FetchAsync(_context.Gm36deinspects, locoNumber)),
            ("Roof Top Inspect", () => FetchAsync(_context.Gm36rfinspects, locoNumber))
        }
            };

            if (!inspectionConfig.ContainsKey(model.LocoModel))
                return BadRequest($"Inspection not configured for model {model.LocoModel}");

            // 3️⃣ Execute ALL tables
            var inspectionSources = new Dictionary<string, List<IInspectionRow>>();

            foreach (var table in inspectionConfig[model.LocoModel])
            {
                inspectionSources[table.Title] = await table.Item2();
            }

            // 4️⃣ Shape response (Grid + Totals)
            var response = inspectionSources.Select(src => new
            {
                TableName = src.Key,
                Rows = src.Value.Select((r, i) => new
                {
                    Sno = i + 1,
                    r.PartDes,
                    r.Good,
                    r.Refurbish,
                    r.Missing,
                    r.Replace,
                    r.Labour
                }),
                Total = new
                {

                    Refurbish = src.Value.Sum(x => x.Refurbish),
                    Missing = src.Value.Sum(x => x.Missing),
                    Replace = src.Value.Sum(x => x.Replace),
                    Labour = src.Value.Sum(x => x.Labour)
                }
            });

            return Ok(response);
        }

        [HttpGet("wagondetails/{wagonNumber}")]
        public async Task<IActionResult> GetInspectionWagonDetails(int wagonNumber)
        {
            // 1️⃣ Get loco basic info
            var model = await _context.WagonDashboards
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.WagonNumber == wagonNumber);

            if (model == null)
                return NotFound($"Wagon {wagonNumber} not found.");

            var inspectionConfig = new List<(string Title, Func<Task<List<IInspectionWagonRow>>> Query)>
        {
            ("Air Brake Inspection", () => FetchWagonAsync(_context.AirBrakePartsInspects, wagonNumber)),
            ("Bottom Discharge Inspection", () => FetchWagonAsync(_context.BottomDischargeInspects, wagonNumber)),
            ("Doors Inspection", () => FetchWagonAsync(_context.DoorsInspects, wagonNumber)),
            ("Floor Inspection", () => FetchWagonAsync(_context.FloorInspects, wagonNumber)),
            ("Stanchions Inspection", () => FetchWagonAsync(_context.StanchionsInspects, wagonNumber)),
            ("Tankers Inspection", () => FetchWagonAsync(_context.TankersInspects, wagonNumber)),
            ("Twistlocks Inspection", () => FetchWagonAsync(_context.TwistlocksInspects, wagonNumber)),
            ("Vacuum Brake Inspection", () => FetchWagonAsync(_context.VacBrakePartsInspects, wagonNumber)),
            ("Wagon Parts Inspection", () => FetchWagonAsync(_context.WagonPartsInspects, wagonNumber))
        };
            var sources = new Dictionary<string, List<IInspectionWagonRow>>();

            foreach (var table in inspectionConfig)
            {
                sources[table.Title] = await table.Query();
            }


            // 4️⃣ Shape response (Grid + Totals)
            var response = sources.Select(src => new
            {
                TableName = src.Key,
                Rows = src.Value.Select((r, i) => new
                {
                    Sno = i + 1,
                    r.PartDes,
                    r.Good,
                    r.Refurbish,
                    r.Missing,
                    r.Replace,
                    r.Labour
                }),
                Total = new
                {
                    
                    Refurbish = src.Value.Sum(x => x.Refurbish),
                    Missing = src.Value.Sum(x => x.Missing),
                    Replace = src.Value.Sum(x => x.Replace),
                    Labour = src.Value.Sum(x => x.Labour)
                }
            });

            return Ok(response);
        }

    }
}