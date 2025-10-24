using AviFinal.Api.Models;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace AviFinal.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TeamsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly AviDbContext _context;

        public TeamsController(IConfiguration configuration, AviDbContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        private SqlConnection CreateConnection()
        {
            return new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        }

        // ✅ 1. Get all inspectors
        [HttpGet("inspectors")]
        public async Task<IActionResult> GetInspectors()
        {
            using (var conn = CreateConnection())
            {
                var sql = @"SELECT UserID AS Id, Name, UserEmail AS Email
                            FROM LeaseCoUsers
                            WHERE UserRole = 'Inspection'
                            ORDER BY Name";

                var inspectors = await conn.QueryAsync(sql);
                return Ok(inspectors);
            }
        }

        // ✅ 2. Get only available inspectors (not assigned to any team)
        [HttpGet("available-inspectors")]
        public async Task<IActionResult> GetAvailableInspectors()
        {
            using (var conn = CreateConnection())
            {
                string sql = @"
                    SELECT 
                        U.UserID AS Id,
                        U.Name,
                        U.UserEmail AS Email
                    FROM LeaseCoUsers U
                    WHERE 
                        U.UserRole = 'Inspection'
                        AND U.UserID NOT IN (
                            SELECT DISTINCT InspectorID FROM TeamInspectors
                        )
                    ORDER BY U.Name;";

                var availableInspectors = await conn.QueryAsync(sql);
                return Ok(availableInspectors);
            }
        }

        // ✅ 3. Create new team with inspectors
        [HttpPost("create")]
        public async Task<IActionResult> CreateTeam([FromBody] TeamCreateDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.TeamName))
                return BadRequest("Team name is required.");

            if (dto.InspectorIds == null || dto.InspectorIds.Count == 0)
                return BadRequest("At least one inspector must be selected.");

            using (var conn = CreateConnection())
            {
                await conn.OpenAsync();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // 1️⃣ Insert into Teams table
                        string insertTeamSql = @"
                            INSERT INTO Teams (TeamName, CreatedBy, CreatedDate, IsActive)
                            VALUES (@TeamName, @CreatedBy, GETDATE(), 1);
                            SELECT CAST(SCOPE_IDENTITY() AS INT);";

                        int teamId = await conn.ExecuteScalarAsync<int>(
                            insertTeamSql,
                            new { dto.TeamName, dto.CreatedBy },
                            transaction
                        );

                        // 2️⃣ Insert inspectors mapping
                        string insertInspectorSql = @"
                            INSERT INTO TeamInspectors (TeamID, InspectorID, AssignedDate)
                            VALUES (@TeamID, @InspectorID, GETDATE());";

                        foreach (var inspectorId in dto.InspectorIds)
                        {
                            await conn.ExecuteAsync(
                                insertInspectorSql,
                                new { TeamID = teamId, InspectorID = inspectorId },
                                transaction
                            );
                        }

                        transaction.Commit();

                        return Ok(new
                        {
                            message = "Team created successfully",
                            teamId = teamId
                        });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Console.WriteLine("Error creating team: " + ex.Message);
                        return StatusCode(500, "Error creating team.");
                    }
                }
            }
        }

        // ✅ 4. Get all teams with inspectors
        [HttpGet("all")]
        public async Task<IActionResult> GetAllTeams()
        {
            using (var conn = CreateConnection())
            {
                var sql = @"
                    SELECT 
                        T.TeamID,
                        T.TeamName,
                        T.CreatedDate,
                        U.Name AS InspectorName,
                        U.UserEmail AS InspectorEmail
                    FROM Teams T
                    INNER JOIN TeamInspectors TI ON T.TeamID = TI.TeamID
                    INNER JOIN LeaseCoUsers U ON TI.InspectorID = U.UserID
                    ORDER BY T.TeamName;";

                var teamList = await conn.QueryAsync(sql);
                return Ok(teamList);
            }
        }
    }

    // ✅ DTOs
    public class TeamCreateDto
    {
        public string TeamName { get; set; }
        public string? CreatedBy { get; set; }
        public List<string> InspectorIds { get; set; } = new();
    }
}
