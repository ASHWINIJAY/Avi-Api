using AviAppFinal.Server.Models;
using AviFinal.Api.DTO;
using AviFinal.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/cockpit-allocation")]
public class CockpitAllocationController : ControllerBase
{
    private readonly AviDbContext _context;
    private string GenerateRefNo(string assetType)
    {
        string prefix = assetType switch
        {
            "Wagon" => "WA",
            "Loco" => "LO",
            _ => "AS"
        };

        return $"{prefix}-{DateTime.Now:ddMMyy-HHmmss}";
    }

    public CockpitAllocationController(AviDbContext context)
    {
        _context = context;
    }

    [HttpGet("grouped")]
    public async Task<IActionResult> GetGroupedByRefNo(
    [FromQuery] string? assetType)
    {
        

        // 1️⃣ Load allocations ONLY
        var allocations = await _context.CockpitAllocations
            .Select(x => new
            {
                x.RefNo,
                x.AssetType,
                x.TeamId,
                x.AssetId
            })
            .ToListAsync();   // DB CALL ENDS HERE

        if (!allocations.Any())
            return Ok(new List<object>());

        // 2️⃣ Load teams lookup
        var teams = await _context.Teams
            .ToDictionaryAsync(t => t.TeamId, t => t.TeamName);

        // 3️⃣ Group IN MEMORY (no EF translation)
        var result = allocations
            .GroupBy(x => x.RefNo)
            .Select((g, index) => new
            {
                sno = index + 1,
                refNo = g.Key,
                assetType = g.First().AssetType,

                teamNames = string.Join(", ",
                    g.Select(x => x.TeamId)
                     .Distinct()
                     .Where(id => teams.ContainsKey(id))
                     .Select(id => teams[id])
                ),

                assetNumbers = string.Join(", ",
                    g.Select(x => x.AssetId)
                     .Distinct()
                )
            })
            .OrderByDescending(x => x.refNo)
            .ToList();

        return Ok(result);
    }



    // =========================================================
    // 1️⃣ GET: Asset Types
    // =========================================================
    [HttpGet("asset-types")]
    public IActionResult GetAssetTypes()
    {
        var assetTypes = new[]
        {
            new { name = "Loco" },
            new { name = "Wagon" }
        };

        return Ok(assetTypes);
    }

    // =========================================================
    // 2️⃣ GET: Teams
    // =========================================================
    [HttpGet("teams")]
    public async Task<IActionResult> GetTeams()
    {
        var teams = await _context.Teams
            .Select(t => new
            {
                id = t.TeamId,
                teamName = t.TeamName
            })
            .ToListAsync();

        return Ok(teams);
    }

    // =========================================================
    // 3️⃣ GET: Assets by Asset Type
    // =========================================================
    [HttpGet("assets")]
    public async Task<IActionResult> GetAssets([FromQuery] string assetType)
    {
        if (string.IsNullOrWhiteSpace(assetType))
            return BadRequest("AssetType is required");

        // ===========================
        // LOCOS
        // ===========================
        if (assetType == "Loco")
        {
            var locos = await _context.MasterLocos
                .Where(l =>
                    // ❌ Not already allocated
                    !_context.CockpitAllocations.Any(c =>
                        c.AssetType == "Loco" &&
                        c.AssetId == l.LocoNumber)

                    // ❌ Not captured in LocoInfoCaptures
                    && !_context.LocoInfoCaptures.Any(i =>
                        i.LocoNumber == l.LocoNumber)
                )
                .Select(l => new
                {
                    id = l.LocoNumber,
                    assetNumber = l.LocoNumber
                })
                .ToListAsync();

            return Ok(locos);
        }

        // ===========================
        // WAGONS
        // ===========================
        if (assetType == "Wagon")
        {
            var wagons = await _context.MasterWagons
                .Where(w =>
                    // ❌ Not already allocated
                    !_context.CockpitAllocations.Any(c =>
                        c.AssetType == "Wagon" &&
                        c.AssetId == w.WagonNumber)

                    // ❌ Not captured in LocoInfoCaptures
                    && !_context.WagonInfoCaptures.Any(i =>
                        i.WagonNumber == w.WagonNumber)
                )
                .Select(w => new
                {
                    id = w.WagonNumber,
                    assetNumber = w.WagonNumber
                })
                .ToListAsync();

            return Ok(wagons);
        }

        return BadRequest("Invalid AssetType");
    }

    // =========================================================
    // 4️⃣ POST: Save Cockpit Allocation
    // =========================================================
    [HttpPost]
    public async Task<IActionResult> SaveAllocation([FromBody] CockpitAllocationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.AssetType)
            || !request.TeamIds.Any()
            || !request.AssetIds.Any())
        {
            return BadRequest("Invalid allocation data");
        }

        var newAllocations = new List<CockpitAllocation>();
        string refNo = GenerateRefNo(request.AssetType);
        foreach (var teamId in request.TeamIds)
        {
            foreach (var assetId in request.AssetIds)
            {
                bool exists = await _context.CockpitAllocations.AnyAsync(x =>
                    x.AssetType == request.AssetType &&
                    x.TeamId == teamId &&
                    x.AssetId == assetId);

                if (!exists)
                {
                    newAllocations.Add(new CockpitAllocation
                    {
                        AssetType = request.AssetType,
                        TeamId = teamId,
                        AssetId = assetId,
                        RefNo = refNo,
                        CreatedBy = User.Identity?.Name ?? "Admin",
                        CreatedDate = DateTime.Now
                    });
                }
            }
        }

        if (!newAllocations.Any())
            return Ok("No new allocations to save");

        _context.CockpitAllocations.AddRange(newAllocations);
        await _context.SaveChangesAsync();

        return Ok("Cockpit allocation saved successfully");
    }
    [HttpDelete("by-refno/{refNo}")]
    public async Task<IActionResult> DeleteByRefNo(string refNo)
    {
        var records = await _context.CockpitAllocations
            .Where(x => x.RefNo == refNo)
            .ToListAsync();

        if (!records.Any())
            return NotFound("No allocation found for this RefNo");

        _context.CockpitAllocations.RemoveRange(records);
        await _context.SaveChangesAsync();

        return Ok("Allocation batch deleted successfully");
    }

    // =========================================================
    // 5️⃣ GET: View Existing Allocations
    // =========================================================
    [HttpGet]
    public async Task<IActionResult> GetAllocations([FromQuery] string assetType)
    {
        var allocations = await _context.CockpitAllocations
            .Where(x => x.AssetType == assetType)
            .Select(x => new
            {
                x.AllocationId,
                x.AssetType,
                x.TeamId,
                TeamName = _context.Teams
                    .Where(t => t.TeamId == x.TeamId)
                    .Select(t => t.TeamName)
                    .FirstOrDefault(),
                x.AssetId
            })
            .ToListAsync();

        return Ok(allocations);
    }

    // =========================================================
    // 6️⃣ DELETE: Remove Allocation
    // =========================================================
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAllocation(int id)
    {
        var allocation = await _context.CockpitAllocations.FindAsync(id);
        if (allocation == null)
            return NotFound();

        _context.CockpitAllocations.Remove(allocation);
        await _context.SaveChangesAsync();

        return Ok("Allocation removed successfully");
    }
}
