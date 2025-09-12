using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Dapper;
using Microsoft.AspNetCore.Authorization;

namespace AviFinal.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LandingController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public LandingController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public class LocoRequest
        {
            public string LocoNumber { get; set; }
        }

        [Authorize]
        [HttpPost("validateloco")]
        public IActionResult ValidateLoco([FromBody] LocoRequest request)
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
    }

    
}


