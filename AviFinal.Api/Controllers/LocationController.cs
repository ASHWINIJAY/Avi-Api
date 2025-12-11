
using AviAppFinal.Server.Models;
using AviFinal.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AVI.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LocationController : ControllerBase
    {
        private readonly AviDbContext _context;

        public LocationController(AviDbContext context)
        {
            _context = context;
        }
        [AllowAnonymous]
        [HttpPost("save")]
        public async Task<IActionResult> SaveLocation([FromBody] DeviceLocation req)
        {
            if (req == null)
                return BadRequest("Invalid request");

            req.ServerTimestamp = DateTime.Now;
            req.UserName = User.Identity?.Name??req.UserName;
            _context.DeviceLocations.Add(req);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Location saved" });
        }

        [HttpGet("latest/{deviceId}")]
        public IActionResult GetLatest(string deviceId)
        {
            var data = _context.DeviceLocations
                .Where(x => x.DeviceId == deviceId)
                .OrderByDescending(x => x.ServerTimestamp)
                .FirstOrDefault();

            return Ok(data);
        }
        [HttpGet("history/{userName}")]
        public async Task<IActionResult> GetHistory(string userName)
        {
            var data = await _context.DeviceLocations
                .Where(x => x.UserName == userName)
                .OrderByDescending(x => x.ServerTimestamp)
                .Take(30)   // ⭐ Return top 10 only
                .ToListAsync();

            return Ok(data);
        }

        [HttpGet("all-latest")]
        public IActionResult GetAllLatest()
        {
            var latest = _context.DeviceLocations
                .GroupBy(x => x.UserName)
                .Select(g => g.OrderByDescending(x => x.ServerTimestamp).FirstOrDefault())
                .ToList();

            return Ok(latest);
        }
    }
}
