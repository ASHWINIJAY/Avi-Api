using AviFinal.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AviAppFinal.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WagonInfoController : ControllerBase
    {
        private readonly AviDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<WagonInfoController> _logger;

        public WagonInfoController(AviDbContext context, IWebHostEnvironment env, ILogger<WagonInfoController> logger)
        {
            _context = context;
            _env = env;
            _logger = logger;
        }

        // GET api/WagonInfo/{wagonNumber}
        [HttpGet("{wagonNumber}")]
        public async Task<IActionResult> GetWagonInfo(int wagonNumber)
        {
            var wagon = await _context.MasterWagons
                .Where(w => w.WagonNumber == wagonNumber)
                .Select(w => new
                {
                    w.InventoryNumber,
                    w.NetBookValue,
                })
                .FirstOrDefaultAsync();

            if (wagon == null)
                return NotFound("Wagon cannot be found.");

            return Ok(wagon);
        }

        // GET api/WagonInfo/getBrakeType/{wagonGroup}
        [HttpGet("getBrakeType/{wagonGroup}")]
        public async Task<IActionResult> GetBrakeType(string wagonGroup)
        {
            var wagon = await _context.WagonGroups
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Group == wagonGroup);

            if (wagon == null)
                return NotFound("Wagon Group cannot be found.");

            string brakeType = wagon.AirBrake == "Yes" ? "Air Brake"
                             : wagon.VacuumBrake == "Yes" ? "Vacuum Brake"
                             : wagon.DualBrake == "Yes" ? "Dual Brake"
                             : "";

            return Ok(new { BrakeType = brakeType });
        }

        // POST api/WagonInfo/submit
        [HttpPost("submit")]
        public async Task<IActionResult> SubmitForm([FromForm] WagonFormModel model)
        {
            if (model == null)
                return BadRequest("No data received.");

            var wagonInfo = new WagonInfoCapture
            {
                WagonNumber = model.WagonNumber,
                InventoryNumber = model.InventoryNumber ?? string.Empty,
                NetBookValue = model.NetBookValue ?? string.Empty,
                GpsLatitude = model.GpsLatitude ?? string.Empty,
                GpsLongitude = model.GpsLongitude ?? string.Empty,
                BodyDamage = string.IsNullOrWhiteSpace(model.BodyDamage) ? "No" : model.BodyDamage,
                WagonGroup = model.WagonGroup ?? string.Empty,
                BrakeType = model.BrakeType ?? string.Empty,
                WagonType = model.WagonType ?? string.Empty
            };

            // Prepare photo folders under wwwroot/wagons
            string rootPath = Path.Combine(_env.WebRootPath ?? "wwwroot", "wagons");
            string wagonPhotoFolder = Path.Combine(rootPath, "WagonPhotos");
            string bodyFolder = Path.Combine(rootPath, "BodyDamagePhotos");
            string liftFolder = Path.Combine(rootPath, "LiftDatePhotos");
            string barrelFolder = Path.Combine(rootPath, "BarrelTestPhotos");
            string brakeFolder = Path.Combine(rootPath, "BrakeTestPhotos");

            Directory.CreateDirectory(wagonPhotoFolder);
            Directory.CreateDirectory(bodyFolder);
            Directory.CreateDirectory(liftFolder);
            Directory.CreateDirectory(barrelFolder);
            Directory.CreateDirectory(brakeFolder);

            string date = DateTime.Now.ToString("yyyyMMdd");
            string time = DateTime.Now.ToString("HHmmss");

            // Wagon Photo
            if (model.WagonPhoto != null && model.WagonPhoto.Length > 0)
            {
                string wagonFileName = $"{model.WagonNumber}_{Sanitize(model.WagonGroup)}_Wagon_{date}_{time}{Path.GetExtension(model.WagonPhoto.FileName)}";
                string wagonFilePath = Path.Combine(wagonPhotoFolder, wagonFileName);
                using (var stream = new FileStream(wagonFilePath, FileMode.Create))
                    await model.WagonPhoto.CopyToAsync(stream);

                wagonInfo.WagonPhoto = $"/wagons/WagonPhotos/{wagonFileName}";
            }
            else
            {
                wagonInfo.WagonPhoto = "N/A";
            }

            wagonInfo.BodyPhoto1 = await SaveBodyPhoto(model.BodyPhoto1, model.WagonNumber, model.WagonGroup, bodyFolder, date, time, 1);
            wagonInfo.BodyPhoto2 = await SaveBodyPhoto(model.BodyPhoto2, model.WagonNumber, model.WagonGroup, bodyFolder, date, time, 2);
            wagonInfo.BodyPhoto3 = await SaveBodyPhoto(model.BodyPhoto3, model.WagonNumber, model.WagonGroup, bodyFolder, date, time, 3);

            wagonInfo.LiftPhoto = await SaveSinglePhoto(model.LiftPhoto, model.WagonNumber, model.WagonGroup, liftFolder, "Lift", date, time);
            wagonInfo.LiftDate = NormalizeDate(model.LiftDate);
            wagonInfo.LiftLapsed = ComputeLapsed(model.LiftDate);

            wagonInfo.BrakePhoto = await SaveSinglePhoto(model.BrakePhoto, model.WagonNumber, model.WagonGroup, brakeFolder, "Brake", date, time);
            wagonInfo.BrakeDate = NormalizeDate(model.BrakeDate);
            wagonInfo.BrakeLapsed = ComputeLapsed(model.BrakeDate);

            string barrelLapsed = "N/A";

            if (string.Equals(model.WagonType, "Tanker", StringComparison.OrdinalIgnoreCase))
            {
                wagonInfo.BarrelPhoto = await SaveSinglePhoto(model.BarrelPhoto, model.WagonNumber, model.WagonGroup, barrelFolder, "Barrel", date, time);
                wagonInfo.BarrelDate = NormalizeDate(model.BarrelDate);
                barrelLapsed = ComputeLapsed(model.BarrelDate);
            }
            else
            {
                wagonInfo.BarrelPhoto = "N/A";
                wagonInfo.BarrelDate = "N/A";
                wagonInfo.BarrelLapsed = "N/A";
            }
            
            if (string.Equals(model.WagonType, "Tanker", StringComparison.OrdinalIgnoreCase))
            {
                wagonInfo.BarrelLapsed = barrelLapsed;
            }

            _context.WagonInfoCaptures.Add(wagonInfo);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Wagon info submitted successfully.",
                LiftLapsed = wagonInfo.LiftLapsed ?? "N/A",
                BarrelLapsed = wagonInfo.BarrelLapsed ?? "N/A",
                BrakeLapsed = wagonInfo.BrakeLapsed ?? "N/A",
                BrakeType = wagonInfo.BrakeType ?? string.Empty   
            });
        }

        private async Task<string> SaveBodyPhoto(IFormFile? file, int wagonNumber, string wagonGroup, string folder, string date, string time, int sequence)
        {
            if (file != null && file.Length > 0)
            {
                string ext = Path.GetExtension(file.FileName);
                string fileName = $"{wagonNumber}_{Sanitize(wagonGroup)}_Body_{sequence}_{date}_{time}{ext}";
                string filePath = Path.Combine(folder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                return $"/wagons/BodyDamagePhotos/{fileName}";
            }
            return "No Photo";
        }

        private async Task<string> SaveSinglePhoto(IFormFile? file, int wagonNumber, string wagonGroup, string folder, string type, string date, string time)
        {
            if (file != null && file.Length > 0)
            {
                string ext = Path.GetExtension(file.FileName);
                string fileName = $"{wagonNumber}_{Sanitize(wagonGroup)}_{type}_{date}_{time}{ext}";
                string filePath = Path.Combine(folder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                string subFolder = type switch
                {
                    "Lift" => "LiftDatePhotos",
                    "Barrel" => "BarrelTestPhotos",
                    "Brake" => "BrakeTestPhotos",
                    _ => "MiscPhotos"
                };

                return $"/wagons/{subFolder}/{fileName}";
            }
            return "No Photo";
        }

        private string NormalizeDate(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "N/A";

            if (DateTime.TryParse(input, out var parsed))
            {
                return parsed.ToString("yyyy-MM-dd");
            }

            string[] formats = new[] { "yyyy/MM/dd", "yyyy-MM-dd", "yyyyMMdd", "MM/dd/yyyy", "dd/MM/yyyy" };
            if (DateTime.TryParseExact(input, formats, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out parsed))
            {
                return parsed.ToString("yyyy-MM-dd");
            }

            // Last resort: return invalid
            return "Invalid Date";
        }

        private string ComputeLapsed(string? userDateRaw)
        {
            var today = DateOnly.FromDateTime(DateTime.Now);

            DateOnly? userDate = null;
            if (!string.IsNullOrWhiteSpace(userDateRaw))
            {
                var normalized = NormalizeDate(userDateRaw);
                if (DateTime.TryParseExact(normalized, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var uDt))
                {
                    userDate = DateOnly.FromDateTime(uDt);
                }
            }

            if (!userDate.HasValue)
            {
                return "Yes";
            }

            if (userDate.Value < today)
                return "Yes";

            return "No";
        }

        private string Sanitize(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "NA";
            var s = input.Trim();
            foreach (var c in Path.GetInvalidFileNameChars())
                s = s.Replace(c, '-');
            s = s.Replace(" ", "_");
            return s;
        }
    }

    public class WagonFormModel
    {
        public int WagonNumber { get; set; }
        public string InventoryNumber { get; set; } = string.Empty;
        public string NetBookValue { get; set; } = string.Empty;
        public string GpsLatitude { get; set; } = string.Empty;
        public string GpsLongitude { get; set; } = string.Empty;
        public IFormFile? WagonPhoto { get; set; }
        public string BodyDamage { get; set; } = "No";
        public IFormFile? BodyPhoto1 { get; set; }
        public IFormFile? BodyPhoto2 { get; set; }
        public IFormFile? BodyPhoto3 { get; set; }
        public string WagonGroup { get; set; } = string.Empty;
        public string BrakeType { get; set; } = string.Empty;
        public string WagonType { get; set; } = string.Empty;
        public IFormFile? LiftPhoto { get; set; }
        public string? LiftDate { get; set; }
        public IFormFile? BarrelPhoto { get; set; }
        public string? BarrelDate { get; set; }
        public IFormFile? BrakePhoto { get; set; }
        public string? BrakeDate { get; set; }
    }
}
