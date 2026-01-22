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
        public async Task<IActionResult> ValidateWagon(int wagonNumber)
        {
            if (wagonNumber <= 0)
                return Ok(new { isValid = false, message = "Invalid Wagon/Asset number." });

            var masterWagon = await _context.MasterWagons
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.WagonNumber == wagonNumber);

            if (masterWagon == null)
                return Ok(new { isValid = false, message = "Wagon/Asset number not found in master data." });

            bool existsInDashboard = await _context.WagonDashboards
                .AnyAsync(d => d.WagonNumber == wagonNumber);

            if (existsInDashboard)
                return Ok(new { isValid = false, message = "Wagon/Asset number has already been inspected." });

            var group = await _context.WagonGroups
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.Group == masterWagon.WagonType);

            if (group == null)
                return Ok(new { isValid = false, message = "Wagon/Asset group not found." });

            if (string.IsNullOrEmpty(group.Type))
                return Ok(new { isValid = false, message = "Wagon/Asset type not found." });

            return Ok(new { isValid = true, 
                            wagonGroup = masterWagon.WagonType, 
                            wagonType = group.Type });
        }
    }
}
