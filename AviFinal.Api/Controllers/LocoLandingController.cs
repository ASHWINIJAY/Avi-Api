using AviAppFinal.Server.Models;
using AviFinal.Api.Models;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AviAppFinal.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LocoLandingController : ControllerBase
    {
        private readonly AviDbContext _context;

        public LocoLandingController(AviDbContext context)
        {
            _context = context;
        }

        //(↓ entire method was changed)
        [HttpGet("validateLoco/{locoNumber}")]
        public async Task<IActionResult> ValidateLoco(int locoNumber,
    [FromQuery] decimal? latitude,
    [FromQuery] decimal? longitude)
        {
            if (locoNumber <= 0)
                return await ReturnWithLog(
                    locoNumber,
                    "Invalid Loco/Asset number.", latitude, longitude
                );

            bool existsInDashboard = await _context.LocoDashboards
               .AnyAsync(d => d.LocoNumber == locoNumber);

            if (existsInDashboard)
                return await ReturnWithLog(
                    locoNumber,
                    "Loco/Asset number has already been inspected.", latitude, longitude
                );

            bool existsP1 = await _context.MasterLocos
                .AnyAsync(e => e.LocoNumber == locoNumber);

            bool existsP2 = await _context.MasterLocosTFR
                .AnyAsync(e => e.LocoNumber == locoNumber);

            bool existsP3 = await _context.MasterLocosTE
                .AnyAsync(e => e.LocoNumber == locoNumber);

            string locoClass = "";

            string locoModel = "";

            int phase = 0;

            if (existsP1)
            {
                var master = await _context.MasterLocos
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.LocoNumber == locoNumber);

                if (master != null)
                {
                    locoClass = master.LocoClass;
                    locoModel = master.LocoModel ?? "N/A";
                    phase = master.Phase;
                }
                else
                {
                    return await ReturnWithLog(
                    locoNumber,
                    "Loco/Asset number not found in phase 1 master data.", latitude, longitude
                    );
                }
            }
            else if (existsP2)
            {
                var master = await _context.MasterLocosTFR
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.LocoNumber == locoNumber);

                if (master != null)
                {
                    locoClass = master.LocoClass;
                    locoModel = master.LocoModel;
                    phase = master.Phase;
                }
                else
                {
                    return await ReturnWithLog(
                     locoNumber,
                     "Loco/Asset number not found in phase 2 master data.", latitude, longitude
                     );
                }
            }
            else if (existsP3)
            {
                var master = await _context.MasterLocosTE
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.LocoNumber == locoNumber);

                if (master != null)
                {
                    locoClass = master.LocoClass;
                    locoModel = master.LocoModel;
                    phase = master.Phase;
                }
                else
                {
                    return await ReturnWithLog(
                    locoNumber,
                    "Loco/Asset number not found in phase 1 master data.", latitude, longitude
                    );
                }
            }
            else
            {
                return await ReturnWithLog(
                    locoNumber,
                    "Loco/Asset number not found in master data.", latitude, longitude
                    );
            }

            //var masterLoco = await _context.MasterLocos
            //    .AsNoTracking()
            //    .FirstOrDefaultAsync(m => m.LocoNumber == locoNumber);

            //if (masterLoco == null)
            //    return await ReturnWithLog(
            //        locoNumber,
            //        "Loco/Asset number not found in master data.", latitude, longitude
            //    );

            //if (string.IsNullOrEmpty(masterLoco.LocoClass))
            //    return await ReturnWithLog(
            //        locoNumber,
            //        "Loco/Asset class not found in master data.", latitude, longitude
            //    );

            //if (string.IsNullOrEmpty(masterLoco.LocoModel))
            //    return await ReturnWithLog(
            //        locoNumber,
            //        "Loco/Asset model not found in master data.", latitude, longitude
            //    );

            return Ok(new
            {
                isValid = true,
                locoClass = locoClass,
                locoModel = locoModel,
                phase = phase.ToString(),
            });


            
        }
        private async Task<IActionResult> ReturnWithLog(
       int assetNumber,
       string message,
       decimal? latitude = null,
       decimal? longitude = null
   )
        {
            var inspectionInfo = new InspectionWarnInfo
            {
                InspectionNumber = assetNumber > 0 ? assetNumber.ToString() : "N/A",
                InspectionType = "Loco",
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


