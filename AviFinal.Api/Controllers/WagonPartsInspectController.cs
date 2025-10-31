using AviFinal.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AviFinal.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WagonPartsInspectController : ControllerBase
    {
        private readonly AviDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<WagonPartsInspectController> _logger;

        public WagonPartsInspectController(AviDbContext context, IWebHostEnvironment env, ILogger<WagonPartsInspectController> logger)
        {
            _context = context;
            _env = env;
            _logger = logger;
        }

        [HttpGet("{wagonNumber}")]
        public async Task<IActionResult> GetLiftLapsed(int wagonNumber)
        {
            var wagon = await _context.WagonInfoCaptures
            .Where(w => w.WagonNumber == wagonNumber)
            .Select(w => new
            {
                LiftLapsed = w.LiftLapsed,
            })
            .FirstOrDefaultAsync();

            if (wagon == null)
                return NotFound("Wagon cannot be found.");

            return Ok(wagon);
        }
    }
}