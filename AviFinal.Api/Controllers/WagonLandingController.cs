using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using AviFinal.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AviFinal.Api.Controllers
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

        [AllowAnonymous]
        [HttpGet("validateWagon/{wagonNumber}")]
        public async Task<IActionResult> ValidateWagon(int wagonNumber)
        {
            if (wagonNumber <= 0)
                return BadRequest(new { isValid = false, message = "Invalid Wagon Number." });

            var wagon = await _context.MasterWagons
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.WagonNumber == wagonNumber);

            if (wagon == null)
                return NotFound(new { isValid = false, message = "Wagon Number not found." });

            string wagonGroup = wagon.WagonType;

            if (string.IsNullOrEmpty(wagonGroup))
                return BadRequest(new { isValid = false, message = "Wagon Group not found." });

            var group = await _context.WagonGroups
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Group == wagonGroup);

            if (group == null)
                return NotFound(new { isValid = false, message = "Wagon Number not found." });

            string? wagonType = group.Type;

            if (string.IsNullOrEmpty(wagonType))
                return BadRequest(new { isValid = false, message = "Wagon Type not found." });

            return Ok(new { isValid = true, wagonGroup = wagonGroup, wagonType = wagonType });
        }
    }
}