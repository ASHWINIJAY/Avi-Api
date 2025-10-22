using AviFinal.Api.Models;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;


namespace AviFinal.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LandingController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly AviDbContext _context;
        public LandingController(IConfiguration configuration,AviDbContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        public class LocoRequest
        {
            public string LocoNumber { get; set; }
        }
        public class LocoResponse
        {
            public int LocoNumber { get; set; }
        }
        [Authorize]
        [HttpGet("list")]
        public async Task<IActionResult> GetLocoList()
        {
            var locos = await _context.MasterLocos
            .Select(l => new LocoResponse { LocoNumber = l.LocoNumber }).ToListAsync();
            return Ok(locos); // returns JSON array
        }

        [Authorize]
        [HttpGet("validateLoco/{locoNumber}")]
        public async Task<IActionResult> ValidateLoco(int locoNumber)
        {
            if (locoNumber <= 0)
                return BadRequest(new { isValid = false, message = "Invalid Asset Code." });

            // Log the connection string for debugging
            //Console.WriteLine($"DEBUG: Using connection string: {_configuration.GetConnectionString("DefaultConnection")}");

            // Check MasterLocos first
            var masterLoco = await _context.MasterLocos
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.LocoNumber == locoNumber);

            if (masterLoco == null)
                return NotFound(new { isValid = false, message = "Asset Code not found in MasterLocos." });

            // Check if it already exists in dashboard
            bool existsInDashboard = await _context.DashBoardItems
                .AsNoTracking()
                .AnyAsync(d => d.LocoNumber == locoNumber);

            if (existsInDashboard)
                return Ok(new { isValid = true, message = "Asset Code has already been inspected." });

            string locoClass = masterLoco.LocoClass;
            if (string.IsNullOrEmpty(locoClass))
                return BadRequest(new { isValid = false, message = "Loco Class not found." });

            // RAW SQL check in E18Locos for debugging
            var e18Rows = await _context.E18locos
                .FromSqlRaw("SELECT * FROM E18Locos WHERE AssetCode = {0}", locoNumber)
                .AsNoTracking()
                .ToListAsync();

            //Console.WriteLine($"DEBUG: Found {e18Rows.Count} rows in E18Locos for AssetCode {locoNumber}");

            // Take the first row if exists
            var model = e18Rows.FirstOrDefault();

            if (model == null)
                return NotFound(new { isValid = false, message = $"No E18Locos record found for AssetCode {locoNumber}." });

            // Safe null handling for locoModel
            string locoModel = model.LocoModel ?? "";

            //Console.WriteLine($"DEBUG: locoNumber={locoNumber}, locoClass={locoClass}, locoModel={locoModel}");

            return Ok(new
            {
                isValid = true,
                locoClass = locoClass,
                locoModel = locoModel
            });
        }

        [Authorize]
        [HttpPost("validateloco")]
        public IActionResult ValidateLoco([FromBody] LocoRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.LocoNumber))
                    return BadRequest(new { isValid = false, message = "Loco Number is required." });

                using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    connection.Open();
                    var sql = "SELECT COUNT(1) FROM MasterLocos WHERE LocoNumber = @LocoNumber";
                    var count = connection.ExecuteScalar<int>(sql, new { LocoNumber = request.LocoNumber });

                    if (count > 0)
                        return Ok(new { isValid = true });
                    else
                        return Ok(new { isValid = false });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }
    }

    
}


