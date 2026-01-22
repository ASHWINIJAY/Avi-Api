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
        public async Task<IActionResult> ValidateLoco(int locoNumber)
        {
            if (locoNumber <= 0)
                return Ok(new { isValid = false, message = "Invalid Loco/Asset number." });

            var masterLoco = await _context.MasterLocos
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.LocoNumber == locoNumber);

            if (masterLoco == null)
                return Ok(new { isValid = false, message = "Loco/Asset number not found in master data." });

            bool existsInDashboard = await _context.LocoDashboards
                .AnyAsync(d => d.LocoNumber == locoNumber);

            if (existsInDashboard)
                return Ok(new { isValid = false, message = "Loco/Asset number has already been inspected." });

            if (string.IsNullOrEmpty(masterLoco.LocoClass))
                return Ok(new { isValid = false, message = "Loco/Asset class not found in master data." });

            if (string.IsNullOrEmpty(masterLoco.LocoModel))
                return Ok(new { isValid = false, message = "Loco/Asset model not found in master data." });

            return Ok(new
            {
                isValid = true,
                locoClass = masterLoco.LocoClass,
                locoModel = masterLoco.LocoModel
            });
        }
    }
}


