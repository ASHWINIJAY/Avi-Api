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


