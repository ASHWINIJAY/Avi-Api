using AviFinal.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AviFinal.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MasterLocosController : ControllerBase
    {
        private readonly AviDbContext _context;

        public MasterLocosController(AviDbContext context)
        {
            _context = context;
        }

        // GET api/MasterLocos/{locoNumber}
        [HttpGet("{locoNumber}")]
        public async Task<IActionResult> GetByLoco(string locoNumber)
        {
            var loco = await _context.MasterLocos
            .Where(l => Convert.ToString(l.LocoNumber) == locoNumber)
            .Select(l => new {
                l.InventoryNumber,
                l.LocoType,
                l.NetBookValue
            })
            .FirstOrDefaultAsync();

            if (loco == null)
                return NotFound("Loco not found");

            return Ok(loco);
        }
    }
}
