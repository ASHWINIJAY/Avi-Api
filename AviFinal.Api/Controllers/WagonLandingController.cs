using AviAppFinal.Server.Models;
using AviFinal.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AviAppFinal.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WagonLandingController : ControllerBase
    {
        private readonly AviDbContext _context;

        public WagonLandingController(AviDbContext context)
        {
            _context = context;
        }

        //(↓ entire method was changed)
        [HttpGet("validateWagon/{wagonNumber}")]
        public async Task<IActionResult> ValidateWagon(int wagonNumber,
    [FromQuery] decimal? latitude,
    [FromQuery] decimal? longitude)
        {
            if (wagonNumber <= 0)
                return await ReturnWithLog(
                    wagonNumber,
                    "Wagon",
                    "Invalid Wagon/Asset number.",latitude,longitude
                );

            var masterWagon = await _context.MasterWagons
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.WagonNumber == wagonNumber);

            if (masterWagon == null)
                return await ReturnWithLog(
                    wagonNumber,
                    "Wagon",
                    "Wagon/Asset number not found in master data.", latitude, longitude
                );

            bool existsInDashboard = await _context.WagonDashboards
                .AnyAsync(d => d.WagonNumber == wagonNumber);

            if (existsInDashboard)
                return await ReturnWithLog(
                    wagonNumber,
                    "Wagon",
                    "Wagon/Asset number has already been inspected.", latitude, longitude
                );

            var group = await _context.WagonGroups
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.Group == masterWagon.WagonType);

            if (group == null)
                return await ReturnWithLog(
                    wagonNumber,
                    "Wagon",
                    "Wagon/Asset group not found.", latitude, longitude
                );

            if (string.IsNullOrEmpty(group.Type))
                return await ReturnWithLog(
                    wagonNumber,
                    "Wagon",
                    "Wagon/Asset type not found.", latitude, longitude
                );

            // ✅ SUCCESS (no warning stored)
            return Ok(new
            {
                isValid = true,
                wagonGroup = masterWagon.WagonType,
                wagonType = group.Type
            });
        }

        private async Task<IActionResult> ReturnWithLog(
    int assetNumber,
    string inspectionType,
    string message,
    decimal? latitude = null,
    decimal? longitude = null
)
        {
            var inspectionInfo = new InspectionWarnInfo
            {
                InspectionNumber = assetNumber > 0 ? assetNumber.ToString() : "N/A",
                InspectionType = inspectionType,   // "Loco" / "Wagon"
                Info = message,
                CreatedTime = DateTime.Now,
                Username = User.Identity?.Name,
                Lat = latitude.ToString(),
                Long = longitude.ToString()
            };

            _context.InspectionWarnInfos.Add(inspectionInfo);
            await _context.SaveChangesAsync();

            return Ok(new { isValid = false, message });
        }

    }
}
