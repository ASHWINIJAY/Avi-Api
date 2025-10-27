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

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTeamById(int id)
        {
            using (var conn = CreateConnection())
            {
                string teamSql = @"
                    SELECT TeamID, TeamName, CreatedBy, CreatedDate
                    FROM Teams
                    WHERE TeamID = @TeamID;";

                string inspectorsSql = @"
                    SELECT U.UserID AS Id, U.Name, U.UserEmail AS Email
                    FROM TeamInspectors TI
                    INNER JOIN LeaseCoUsers U ON TI.InspectorID = U.UserID
                    WHERE TI.TeamID = @TeamID;";

                var team = await conn.QueryFirstOrDefaultAsync(teamSql, new { TeamID = id });
                if (team == null)
                    return NotFound(new { message = "Team not found." });

                var inspectors = await conn.QueryAsync(inspectorsSql, new { TeamID = id });
                return Ok(new
                {
                    Team = team,
                    Inspectors = inspectors
                });
            }
        }

        [HttpPost("update/{id}")]
        public async Task<IActionResult> UpdateTeam(int id, [FromBody] TeamCreateDto dto)
        {
            if (dto == null)
                return BadRequest(new { message = "Invalid data." });

            if (string.IsNullOrWhiteSpace(dto.TeamName))
                return BadRequest(new { message = "Team name is required." });

            using (var conn = CreateConnection())
            {
                await conn.OpenAsync();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // 1️⃣ Update team name
                        string updateTeamSql = @"
                    UPDATE Teams
                    SET TeamName = @TeamName
                    WHERE TeamID = @TeamID;";

                        await conn.ExecuteAsync(updateTeamSql, new { dto.TeamName, TeamID = id }, transaction);

                        // 2️⃣ Clear existing inspector mappings
                        string deleteMappings = "DELETE FROM TeamInspectors WHERE TeamID = @TeamID;";
                        await conn.ExecuteAsync(deleteMappings, new { TeamID = id }, transaction);

                        // 3️⃣ Add new inspector mappings
                        if (dto.InspectorIds != null && dto.InspectorIds.Count > 0)
                        {
                            string insertSql = @"
                        INSERT INTO TeamInspectors (TeamID, InspectorID, AssignedDate)
                        VALUES (@TeamID, @InspectorID, GETDATE());";

                            foreach (var inspectorId in dto.InspectorIds)
                            {
                                await conn.ExecuteAsync(
                                    insertSql,
                                    new { TeamID = id, InspectorID = inspectorId },
                                    transaction
                                );
                            }
                        }

                        transaction.Commit();
                        return Ok(new { message = "Team updated successfully." });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Console.WriteLine(ex.Message);
                        return StatusCode(500, new { message = "Error updating team." });
                    }
                }
            }
        }


        [HttpPost("delete/{id}")]
        public async Task<IActionResult> DeleteTeam(int id)
        {
            using (var conn = CreateConnection())
            {
                await conn.OpenAsync();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        string deleteInspectors = "DELETE FROM TeamInspectors WHERE TeamID = @TeamID;";
                        string deleteTeam = "DELETE FROM Teams WHERE TeamID = @TeamID;";

                        await conn.ExecuteAsync(deleteInspectors, new { TeamID = id }, transaction);
                        await conn.ExecuteAsync(deleteTeam, new { TeamID = id }, transaction);

                        transaction.Commit();
                        return Ok(new { message = "Team deleted successfully." });
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Console.WriteLine(ex.Message);
                        return StatusCode(500, new { message = "Error deleting team." });
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
                string sql = @"
            SELECT 
                T.TeamID,
                T.TeamName,
                T.CreatedDate,
                U.UserID AS InspectorID,
                U.Name AS InspectorName,
                U.UserEmail AS InspectorEmail
            FROM Teams T
            INNER JOIN TeamInspectors TI ON T.TeamID = TI.TeamID
            INNER JOIN LeaseCoUsers U ON TI.InspectorID = U.UserID
            ORDER BY T.TeamName;";

                var result = await conn.QueryAsync(sql);

                // ✅ Group inspectors under each team
                var teams = result
                    .GroupBy(r => new
                    {
                        TeamID = (int)r.TeamID,
                        TeamName = (string)r.TeamName,
                        CreatedDate = (DateTime)r.CreatedDate
                    })
                    .Select(g => new
                    {
                        TeamID = g.Key.TeamID,
                        TeamName = g.Key.TeamName,
                        CreatedDate = g.Key.CreatedDate,
                        Inspectors = g.Select(i => new
                        {
                            Id = (string)i.InspectorID,
                            Name = (string)i.InspectorName,
                            Email = (string)i.InspectorEmail
                        }).ToList()
                    })
                    .ToList();

                return Ok(teams);
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
