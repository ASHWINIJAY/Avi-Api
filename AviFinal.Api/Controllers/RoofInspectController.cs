using System.Text.Json;
using AviFinal.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AviFinal.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RoofInspectController : ControllerBase
    {
        private readonly AviDbContext _context;

        public RoofInspectController(AviDbContext context)
        {
            _context = context;
        }

        public class RoofRowModel
        {
            public int Id { get; set; }
            public string PartDescr { get; set; } = null!;
            public string Good { get; set; } = "No";
            public string Refurbish { get; set; } = "No";
            public string Missing { get; set; } = "No";
            public string DamageReplaced { get; set; } = "No";
            public string ReplacementValue { get; set; } = null!;
            public string RefurbishValue { get; set; } = null!;
        }

        public class RoofFormModel
        {
            public int LocoNumber { get; set; }
            public string UserID { get; set; } = null!;
            public string InspectFormID { get; set; } = null!;
            public List<RoofRowModel> Rows { get; set; } = new();
        }

        [HttpPost("submit")]
        public async Task<IActionResult> SubmitForm([FromBody] RoofFormModel model)
        {
            if (model.LocoNumber <= 0 || string.IsNullOrEmpty(model.UserID))
                return BadRequest("LocoNumber or UserID is invalid.");

            if (model.Rows == null || !model.Rows.Any())
                return BadRequest("No rows provided.");

            foreach (var row in model.Rows)
            {
                var entity = new RoofInspect
                {
                    LocoNumber = model.LocoNumber,
                    UserId = model.UserID,
                    InspectFormId = model.InspectFormID,
                    PartDescr = row.PartDescr ?? "",
                    GoodCheck = row.Good ?? "No",
                    RefurbishCheck = row.Refurbish ?? "No",
                    MissingCheck = row.Missing ?? "No",
                    DamageCheck = row.DamageReplaced ?? "No",
                    ReplaceCost = row.ReplacementValue ?? "",
                    RefurbishCost = row.RefurbishValue ?? ""
                };

                _context.RoofInspects.Add(entity);
            }

            var inspector = await _context.LeaseCoUsers
                .FirstOrDefaultAsync(u => u.UserId == model.UserID);

            string inspectorName = inspector?.UserName ?? "Unknown";

            var locoInfo = await _context.InfoLocosFinals
                .FirstOrDefaultAsync(l => l.LocoNumber == model.LocoNumber);

            string proMain = locoInfo?.ProMain ?? "";
            string bodyDamage = locoInfo?.BodyDamage ?? "";
            string bodyPhotos = string.IsNullOrEmpty(locoInfo?.BodyPhotoPaths) ? "No Photos" : locoInfo.BodyPhotoPaths!;
            string bodyRepairValue = string.IsNullOrEmpty(locoInfo?.BodyRepairValue) ? "0" : locoInfo.BodyRepairValue!;
            string liftingRequired = locoInfo?.LiftingRequired ?? "";
            string liftPhotos = string.IsNullOrEmpty(locoInfo?.LiftingPhotoPaths) ? "No Photos" : locoInfo.LiftingPhotoPaths!;
            DateOnly? liftDate = locoInfo?.LiftDate;

            var photoPaths = new List<string>();

            if (!string.IsNullOrEmpty(locoInfo?.BodyPhotoPaths))
                photoPaths.Add(locoInfo.BodyPhotoPaths);

            if (!string.IsNullOrEmpty(locoInfo?.LiftingPhotoPaths))
                photoPaths.Add(locoInfo.LiftingPhotoPaths);

            if (!string.IsNullOrEmpty(locoInfo?.PhotoPath))
                photoPaths.Add(locoInfo.PhotoPath);

            // Convert to JSON string
            string allPhotos = photoPaths.Any()
                ? JsonSerializer.Serialize(photoPaths)
                : "[]";

            var replaceValues = new List<decimal>();
            var refurbishValues = new List<decimal>();

            var WalkReplaceStrings = await _context.WalkAroundInspects
                .Where(x => x.LocoNumber == model.LocoNumber)
                .Select(x => x.ReplaceCost)
                .ToListAsync();

            replaceValues.AddRange(
                WalkReplaceStrings.Select(s => decimal.TryParse(s, out var v) ? v : 0)
            );

            var WalkRefurbishStrings = await _context.WalkAroundInspects
                .Where(x => x.LocoNumber == model.LocoNumber)
                .Select(x => x.RefurbishCost)
                .ToListAsync();

            refurbishValues.AddRange(
                WalkRefurbishStrings.Select(s => decimal.TryParse(s, out var v) ? v : 0)
            );

            var FrontLocoReplaceStrings = await _context.FrontLocoInspects
                .Where(x => x.LocoNumber == model.LocoNumber)
                .Select(x => x.ReplaceCost)
                .ToListAsync();

            replaceValues.AddRange(
                FrontLocoReplaceStrings.Select(s => decimal.TryParse(s, out var v) ? v : 0)
            );

            var FrontLocoRefurbishStrings = await _context.FrontLocoInspects
                .Where(x => x.LocoNumber == model.LocoNumber)
                .Select(x => x.RefurbishCost)
                .ToListAsync();

            refurbishValues.AddRange(
                FrontLocoRefurbishStrings.Select(s => decimal.TryParse(s, out var v) ? v : 0)
            );

            var ShortNoseReplaceStrings = await _context.ShortNoseInspects
                .Where(x => x.LocoNumber == model.LocoNumber)
                .Select(x => x.ReplaceCost)
                .ToListAsync();

            replaceValues.AddRange(
                ShortNoseReplaceStrings.Select(s => decimal.TryParse(s, out var v) ? v : 0)
            );

            var ShortNoseRefurbishStrings = await _context.ShortNoseInspects
                .Where(x => x.LocoNumber == model.LocoNumber)
                .Select(x => x.RefurbishCost)
                .ToListAsync();

            refurbishValues.AddRange(
                ShortNoseRefurbishStrings.Select(s => decimal.TryParse(s, out var v) ? v : 0)
            );

            var CabLocoReplaceStrings = await _context.CabLocoInspects
                .Where(x => x.LocoNumber == model.LocoNumber)
                .Select(x => x.ReplaceCost)
                .ToListAsync();

            replaceValues.AddRange(
                CabLocoReplaceStrings.Select(s => decimal.TryParse(s, out var v) ? v : 0)
            );

            var CabLocoRefurbishStrings = await _context.CabLocoInspects
                .Where(x => x.LocoNumber == model.LocoNumber)
                .Select(x => x.RefurbishCost)
                .ToListAsync();

            refurbishValues.AddRange(
                CabLocoRefurbishStrings.Select(s => decimal.TryParse(s, out var v) ? v : 0)
            );

            var ElectCabReplaceStrings = await _context.ElectCabInspects
                .Where(x => x.LocoNumber == model.LocoNumber)
                .Select(x => x.ReplaceCost)
                .ToListAsync();

            replaceValues.AddRange(
                ElectCabReplaceStrings.Select(s => decimal.TryParse(s, out var v) ? v : 0)
            );

            var ElectCabRefurbishStrings = await _context.ElectCabInspects
                .Where(x => x.LocoNumber == model.LocoNumber)
                .Select(x => x.RefurbishCost)
                .ToListAsync();

            refurbishValues.AddRange(
                ElectCabRefurbishStrings.Select(s => decimal.TryParse(s, out var v) ? v : 0)
            );

            var BatSwitchReplaceStrings = await _context.BatSwitchInspects
                .Where(x => x.LocoNumber == model.LocoNumber)
                .Select(x => x.ReplaceCost)
                .ToListAsync();

            replaceValues.AddRange(
                BatSwitchReplaceStrings.Select(s => decimal.TryParse(s, out var v) ? v : 0)
            );

            var BatSwitchRefurbishStrings = await _context.BatSwitchInspects
                .Where(x => x.LocoNumber == model.LocoNumber)
                .Select(x => x.RefurbishCost)
                .ToListAsync();

            refurbishValues.AddRange(
                BatSwitchRefurbishStrings.Select(s => decimal.TryParse(s, out var v) ? v : 0)
            );

            var LeftMidReplaceStrings = await _context.LeftMidDoorInspects
                .Where(x => x.LocoNumber == model.LocoNumber)
                .Select(x => x.ReplaceCost)
                .ToListAsync();

            replaceValues.AddRange(
                LeftMidReplaceStrings.Select(s => decimal.TryParse(s, out var v) ? v : 0)
            );

            var LeftMidRefurbishStrings = await _context.LeftMidDoorInspects
                .Where(x => x.LocoNumber == model.LocoNumber)
                .Select(x => x.RefurbishCost)
                .ToListAsync();

            refurbishValues.AddRange(
                LeftMidRefurbishStrings.Select(s => decimal.TryParse(s, out var v) ? v : 0)
            );

            var CirBreakReplaceStrings = await _context.CirBreakPanInspects
                .Where(x => x.LocoNumber == model.LocoNumber)
                .Select(x => x.ReplaceCost)
                .ToListAsync();

            replaceValues.AddRange(
                CirBreakReplaceStrings.Select(s => decimal.TryParse(s, out var v) ? v : 0)
            );

            var CirBreakRefurbishStrings = await _context.CirBreakPanInspects
                .Where(x => x.LocoNumber == model.LocoNumber)
                .Select(x => x.RefurbishCost)
                .ToListAsync();

            refurbishValues.AddRange(
                CirBreakRefurbishStrings.Select(s => decimal.TryParse(s, out var v) ? v : 0)
            );

            var TopRightReplaceStrings = await _context.TopRightPanInspects
                .Where(x => x.LocoNumber == model.LocoNumber)
                .Select(x => x.ReplaceCost)
                .ToListAsync();

            replaceValues.AddRange(
                TopRightReplaceStrings.Select(s => decimal.TryParse(s, out var v) ? v : 0)
            );

            var TopRightRefurbishStrings = await _context.TopRightPanInspects
                .Where(x => x.LocoNumber == model.LocoNumber)
                .Select(x => x.RefurbishCost)
                .ToListAsync();

            refurbishValues.AddRange(
                TopRightRefurbishStrings.Select(s => decimal.TryParse(s, out var v) ? v : 0)
            );

            var MidPanReplaceStrings = await _context.MidPanInspects
                .Where(x => x.LocoNumber == model.LocoNumber)
                .Select(x => x.ReplaceCost)
                .ToListAsync();

            replaceValues.AddRange(
                MidPanReplaceStrings.Select(s => decimal.TryParse(s, out var v) ? v : 0)
            );

            var MidPanRefurbishStrings = await _context.MidPanInspects
                .Where(x => x.LocoNumber == model.LocoNumber)
                .Select(x => x.RefurbishCost)
                .ToListAsync();

            refurbishValues.AddRange(
                MidPanRefurbishStrings.Select(s => decimal.TryParse(s, out var v) ? v : 0)
            );

            var BotLeftReplaceStrings = await _context.BotLeftPanInspects
                .Where(x => x.LocoNumber == model.LocoNumber)
                .Select(x => x.ReplaceCost)
                .ToListAsync();

            replaceValues.AddRange(
                BotLeftReplaceStrings.Select(s => decimal.TryParse(s, out var v) ? v : 0)
            );

            var BotLeftRefurbishStrings = await _context.BotLeftPanInspects
                .Where(x => x.LocoNumber == model.LocoNumber)
                .Select(x => x.RefurbishCost)
                .ToListAsync();

            refurbishValues.AddRange(
                BotLeftRefurbishStrings.Select(s => decimal.TryParse(s, out var v) ? v : 0)
            );

            var CenAirReplaceStrings = await _context.CenAirInspects
                .Where(x => x.LocoNumber == model.LocoNumber)
                .Select(x => x.ReplaceCost)
                .ToListAsync();

            replaceValues.AddRange(
                CenAirReplaceStrings.Select(s => decimal.TryParse(s, out var v) ? v : 0)
            );

            var CenAirRefurbishStrings = await _context.CenAirInspects
                .Where(x => x.LocoNumber == model.LocoNumber)
                .Select(x => x.RefurbishCost)
                .ToListAsync();

            refurbishValues.AddRange(
                CenAirRefurbishStrings.Select(s => decimal.TryParse(s, out var v) ? v : 0)
            );

            var EngineReplaceStrings = await _context.EngineDeckInspects
                .Where(x => x.LocoNumber == model.LocoNumber)
                .Select(x => x.ReplaceCost)
                .ToListAsync();

            replaceValues.AddRange(
                EngineReplaceStrings.Select(s => decimal.TryParse(s, out var v) ? v : 0)
            );

            var EngineRefurbishStrings = await _context.EngineDeckInspects
                .Where(x => x.LocoNumber == model.LocoNumber)
                .Select(x => x.RefurbishCost)
                .ToListAsync();

            refurbishValues.AddRange(
                EngineRefurbishStrings.Select(s => decimal.TryParse(s, out var v) ? v : 0)
            );

            var ComFanReplaceStrings = await _context.ComFanInspects
                .Where(x => x.LocoNumber == model.LocoNumber)
                .Select(x => x.ReplaceCost)
                .ToListAsync();

            replaceValues.AddRange(
                ComFanReplaceStrings.Select(s => decimal.TryParse(s, out var v) ? v : 0)
            );

            var ComFanRefurbishStrings = await _context.ComFanInspects
                .Where(x => x.LocoNumber == model.LocoNumber)
                .Select(x => x.RefurbishCost)
                .ToListAsync();

            refurbishValues.AddRange(
                ComFanRefurbishStrings.Select(s => decimal.TryParse(s, out var v) ? v : 0)
            );

            var EndReplaceStrings = await _context.EndDeckInspects
                .Where(x => x.LocoNumber == model.LocoNumber)
                .Select(x => x.ReplaceCost)
                .ToListAsync();

            replaceValues.AddRange(
                EndReplaceStrings.Select(s => decimal.TryParse(s, out var v) ? v : 0)
            );

            var EndRefurbishStrings = await _context.EndDeckInspects
                .Where(x => x.LocoNumber == model.LocoNumber)
                .Select(x => x.RefurbishCost)
                .ToListAsync();

            refurbishValues.AddRange(
                EndRefurbishStrings.Select(s => decimal.TryParse(s, out var v) ? v : 0)
            );

            var CoupReplaceStrings = await _context.CoupGearInspects
                .Where(x => x.LocoNumber == model.LocoNumber)
                .Select(x => x.ReplaceCost)
                .ToListAsync();

            replaceValues.AddRange(
                CoupReplaceStrings.Select(s => decimal.TryParse(s, out var v) ? v : 0)
            );

            var CoupRefurbishStrings = await _context.CoupGearInspects
                .Where(x => x.LocoNumber == model.LocoNumber)
                .Select(x => x.RefurbishCost)
                .ToListAsync();

            refurbishValues.AddRange(
                CoupRefurbishStrings.Select(s => decimal.TryParse(s, out var v) ? v : 0)
            );

            var RoofReplaceStrings = await _context.RoofInspects
                .Where(x => x.LocoNumber == model.LocoNumber)
                .Select(x => x.ReplaceCost)
                .ToListAsync();

            replaceValues.AddRange(
                RoofReplaceStrings.Select(s => decimal.TryParse(s, out var v) ? v : 0)
            );

            var RoofRefurbishStrings = await _context.RoofInspects
                .Where(x => x.LocoNumber == model.LocoNumber)
                .Select(x => x.RefurbishCost)
                .ToListAsync();

            refurbishValues.AddRange(
                RoofRefurbishStrings.Select(s => decimal.TryParse(s, out var v) ? v : 0)
            );

            decimal totalReplaceValue = replaceValues.Sum();
            decimal totalRefurbishValue = refurbishValues.Sum();

            // Step 5: Insert into DashBoardItems
            var dashItem = new DashBoardItem
            {
                LocoNumber = model.LocoNumber,
                DateAssessed = DateOnly.FromDateTime(DateTime.Now),
                TimeAssessed = TimeOnly.FromDateTime(DateTime.Now),
                InspectorName = inspectorName,
                ProMain = proMain,
                BodyDamage = bodyDamage,
                BodyPhotos = bodyPhotos,
                BodyRepairValue = bodyRepairValue,
                ReplaceValue = totalReplaceValue.ToString(),
                RefurbishValue = totalRefurbishValue.ToString(),
                LiftingRequired = liftingRequired,
                LiftPhotos = liftPhotos,
                LiftDate = liftDate,
                AssessmentResults = "Not Functional",
                AssessmentPhotos = allPhotos,
                AssessmentQuote = "Not Functional",
                AssessmentCert = "Not Functional",
                UploadStatus = "Not Uploaded",
                UploadDate = new DateOnly(1, 1, 1) // "0000/00/00" fallback
            };

            _context.DashBoardItems.Add(dashItem);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException dbEx)
            {
                // Log the exception
                return StatusCode(500, $"Database error: {dbEx.Message}");
            }

            return Ok(new { message = "Walk Around Inspect submitted successfully." });
        }
    }
}
