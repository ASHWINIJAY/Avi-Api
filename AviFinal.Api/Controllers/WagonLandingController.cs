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

            bool existsInDashboard = await _context.WagonDashboards
               .AnyAsync(d => d.WagonNumber == wagonNumber);

            if (existsInDashboard)
                return await ReturnWithLog(
                    wagonNumber,
                    "Wagon",
                    "Wagon/Asset number has already been inspected.", latitude, longitude
                );

            bool existsP1 = await _context.MasterWagons
                .AnyAsync(e => e.WagonNumber == wagonNumber);

            bool existsP2 = await _context.MasterWagonsTFR
                .AnyAsync(e => e.WagonNumber == wagonNumber);

            string wagonGroup = "";

            string wagonType = "";

            int phase = 0;

            if (existsP1)
            {
                var masterWagon = await _context.MasterWagons
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.WagonNumber == wagonNumber);

                if (masterWagon != null)
                {
                    var group = await _context.WagonGroups
                        .AsNoTracking()
                        .FirstOrDefaultAsync(g => g.Group == masterWagon.WagonType);

                    if (group != null)
                    {
                        wagonGroup = masterWagon.WagonType;
                        wagonType = group.Type ;
                        phase = masterWagon.Phase;
                    }
                    else
                    {
                        return await ReturnWithLog(
                            wagonNumber,
                            "Wagon",
                            "Wagon/Asset group not found (Phase 1).", latitude, longitude
                        );
                    }
                }
                else
                {
                    return await ReturnWithLog(
                    wagonNumber,
                    "Wagon",
                    "Wagon/Asset number not found in phase 1 master data.", latitude, longitude
                    );
                }
            }
            else if (existsP2)
            {
                var masterWagon = await _context.MasterWagonsTFR
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.WagonNumber == wagonNumber);

                if (masterWagon != null)
                {
                    var group = await _context.WagonGroups
                        .AsNoTracking()
                        .FirstOrDefaultAsync(g => g.Group == masterWagon.WagonType);

                    if (group != null)
                    {
                        wagonGroup = masterWagon.WagonType;
                        wagonType = group.Type;
                        phase = masterWagon.Phase;
                    }
                    else
                    {
                        return await ReturnWithLog(
                            wagonNumber,
                            "Wagon",
                            "Wagon/Asset group not found (Phase 2).", latitude, longitude
                        );
                    }
                }
                else
                {
                    return await ReturnWithLog(
                    wagonNumber,
                    "Wagon",
                    "Wagon/Asset number not found in phase 2 master data.", latitude, longitude
                    );
                }
            }
            else
            {
                return await ReturnWithLog(
                   wagonNumber,
                   "Wagon",
                   "Wagon/Asset number not found in master data.", latitude, longitude
                   );
            }

            //var masterWagon = await _context.MasterWagons
            //    .AsNoTracking()
            //    .FirstOrDefaultAsync(m => m.WagonNumber == wagonNumber);

            //if (masterWagon == null)
            //    return await ReturnWithLog(
            //        wagonNumber,
            //        "Wagon",
            //        "Wagon/Asset number not found in master data.", latitude, longitude
            //    );

            //var group = await _context.WagonGroups
            //    .AsNoTracking()
            //    .FirstOrDefaultAsync(g => g.Group == masterWagon.WagonType);

            //if (group == null)
            //    return await ReturnWithLog(
            //        wagonNumber,
            //        "Wagon",
            //        "Wagon/Asset group not found.", latitude, longitude
            //    );

            //if (string.IsNullOrEmpty(group.Type))
            //    return await ReturnWithLog(
            //        wagonNumber,
            //        "Wagon",
            //        "Wagon/Asset type not found.", latitude, longitude
            //    );

            return Ok(new
            {
                isValid = true,
                wagonGroup = wagonGroup,
                wagonType = wagonType,
                phase = phase.ToString(),
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
