using AviAppFinal.Server.Models;
using AviFinal.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace AviAppFinal.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LocoInfoCaptureController : ControllerBase
    {
        private readonly AviDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<LocoInfoCaptureController> _logger;

        public LocoInfoCaptureController(AviDbContext context, IWebHostEnvironment env, ILogger<LocoInfoCaptureController> logger)
        {
            _context = context;
            _env = env;
            _logger = logger;
        }

        [HttpGet("{locoNumber}")]
        public async Task<IActionResult> GetLocoInfo(int locoNumber)
        {
            bool existsP1 = await _context.MasterLocos
                .AnyAsync(e => e.LocoNumber == locoNumber);

            bool existsP2 = await _context.MasterLocosTFR
                .AnyAsync(e => e.LocoNumber == locoNumber);

            bool existsP3 = await _context.MasterLocosTE
                .AnyAsync(e => e.LocoNumber == locoNumber);

            string? inventoryNumber = "";

            string? netBookValue = "";

            if (existsP1)
            {
                var master = await _context.MasterLocos
                    .Where(e => e.LocoNumber == locoNumber)
                    .Select(e => new
                    {
                        InventoryNumber = e.InventoryNumber,
                        NetBookValue = e.NetBookValue,
                    })
                    .FirstOrDefaultAsync();

                inventoryNumber = master?.InventoryNumber;
                netBookValue = master?.NetBookValue;
            }
            else if (existsP2)
            {
                var master = await _context.MasterLocosTFR
                    .Where(e => e.LocoNumber == locoNumber)
                    .Select(e => new
                    {
                        InventoryNumber = e.InventoryNumber,
                        NetBookValue = e.NetBookValue,
                    })
                    .FirstOrDefaultAsync();

                inventoryNumber = master?.InventoryNumber;
                netBookValue = master?.NetBookValue.ToString("N2", new CultureInfo("en-ZA"));
            }
            else if (existsP3)
            {
                var master = await _context.MasterLocosTE
                    .Where(e => e.LocoNumber == locoNumber)
                    .Select(e => new
                    {
                        InventoryNumber = e.InventoryNumber,
                        NetBookValue = e.NetBookValue,
                    })
                    .FirstOrDefaultAsync();

                inventoryNumber = master?.InventoryNumber;
                netBookValue = master?.NetBookValue.ToString("N2", new CultureInfo("en-ZA"));
            }
            else
            {
                return NotFound("Loco/Asset number cannot be found.");
            }   

            return Ok( new
            {
                InventoryNumber = inventoryNumber,
                NetBookValue = netBookValue,
            });
        }

        [HttpPost("submit")]
        public async Task<IActionResult> SubmitForm([FromForm] LocomotiveFormModel model)
        {
            if (model == null)
                return BadRequest("No data received.");

            var user = await _context.LeaseCoUsers
                .FirstOrDefaultAsync(e => e.UserId == model.UserID);

            if (user == null)
                return NotFound("User cannot be found.");

            string userName = user.UserName;

            var locoInfo = new LocoInfoCapture
            {
                LocoNumber = model.LocoNumber,
                InventoryNumber = model.InventoryNumber ?? string.Empty,
                NetBookValue = model.NetBookValue ?? string.Empty,
                GpsLatitude = model.GpsLatitude ?? string.Empty,
                GpsLongitude = model.GpsLongitude ?? string.Empty,
                BodyDamage = string.IsNullOrWhiteSpace(model.BodyDamage) ? "No" : model.BodyDamage,
                LocoClass = model.LocoClass ?? string.Empty,
                LocoModel = model.LocoModel ?? string.Empty,
                Phase = Convert.ToInt32(model.Phase),
                CreatedBy = userName,
            };

            // Prepare photo folders under wwwroot/wagons
            string rootPath = Path.Combine(_env.WebRootPath ?? "wwwroot", "locomotives");
            string locoPhotoFolder = Path.Combine(rootPath, "LocoPhotos");
            string bodyFolder = Path.Combine(rootPath, "BodyDamagePhotos");
            string liftFolder = Path.Combine(rootPath, "LiftDatePhotos");

            Directory.CreateDirectory(locoPhotoFolder);
            Directory.CreateDirectory(bodyFolder);
            Directory.CreateDirectory(liftFolder);

            string date = DateTime.Now.ToString("yyyyMMdd");
            string time = DateTime.Now.ToString("HHmmss");

            if (model.LocoPhoto != null && model.LocoPhoto.Length > 0)
            {
                string locoFileName = $"{model.LocoNumber}_{Sanitize(model.LocoModel)}_Loco_{date}_{time}{Path.GetExtension(model.LocoPhoto.FileName)}";
                string locoFilePath = Path.Combine(locoPhotoFolder, locoFileName);
                using (var stream = new FileStream(locoFilePath, FileMode.Create))
                    await model.LocoPhoto.CopyToAsync(stream);

                locoInfo.LocoPhoto = $"/locomotives/LocoPhotos/{locoFileName}";
            }
            else
            {
                locoInfo.LocoPhoto = "No Photo";
            }

            // Body photos — save with sequence number to avoid collisions
            locoInfo.BodyPhoto1 = await SaveBodyPhoto(model.BodyPhoto1, model.LocoNumber, model.LocoModel, bodyFolder, date, time, 1);
            locoInfo.BodyPhoto2 = await SaveBodyPhoto(model.BodyPhoto2, model.LocoNumber, model.LocoModel, bodyFolder, date, time, 2);
            locoInfo.BodyPhoto3 = await SaveBodyPhoto(model.BodyPhoto3, model.LocoNumber, model.LocoModel, bodyFolder, date, time, 3);

            if (model.LiftPhoto != null && model.LiftPhoto.Length > 0)
            {
                locoInfo.LiftPhoto = await SaveSinglePhoto(model.LiftPhoto, model.LocoNumber, model.LocoModel, liftFolder, "Lift", date, time);
                locoInfo.LiftDate = NormalizeDate(model.LiftDate);
            }
            else
            {
                locoInfo.LiftPhoto = "No Photo";
                locoInfo.LiftDate = "No Date";
            }

            _context.LocoInfoCaptures.Add(locoInfo);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Loco info submitted successfully." });
        }

        private async Task<string> SaveBodyPhoto(IFormFile? file, int locoNumber, string locoModel, string folder, string date, string time, int sequence)
        {
            if (file != null && file.Length > 0)
            {
                string ext = Path.GetExtension(file.FileName);
                string fileName = $"{locoNumber}_{Sanitize(locoModel)}_Body_{sequence}_{date}_{time}{ext}";
                string filePath = Path.Combine(folder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                return $"/locomotives/BodyDamagePhotos/{fileName}";
            }
            return "No Photo";
        }

        private async Task<string> SaveSinglePhoto(IFormFile? file, int locoNumber, string locoModel, string folder, string type, string date, string time)
        {
            if (file != null && file.Length > 0)
            {
                string ext = Path.GetExtension(file.FileName);
                string fileName = $"{locoNumber}_{Sanitize(locoModel)}_{type}_{date}_{time}{ext}";
                string filePath = Path.Combine(folder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                string subFolder = type switch
                {
                    "Lift" => "LiftDatePhotos",
                    _ => "MiscPhotos"
                };

                return $"/locomotives/{subFolder}/{fileName}";
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

    public class LocomotiveFormModel
    {
        public int LocoNumber { get; set; }
        public string InventoryNumber { get; set; } = string.Empty;
        public string NetBookValue { get; set; } = string.Empty;
        public string GpsLatitude { get; set; } = string.Empty;
        public string GpsLongitude { get; set; } = string.Empty;
        public IFormFile? LocoPhoto { get; set; }
        public string BodyDamage { get; set; } = "No";
        public IFormFile? BodyPhoto1 { get; set; }
        public IFormFile? BodyPhoto2 { get; set; }
        public IFormFile? BodyPhoto3 { get; set; }
        public string LocoClass { get; set; } = string.Empty;
        public string LocoModel { get; set; } = string.Empty;
        public IFormFile? LiftPhoto { get; set; }
        public string LiftDate { get; set; } = string.Empty;
        public string Phase {  get; set; } = string.Empty;
        public string UserID {  get; set; } = string.Empty;
    }
}
