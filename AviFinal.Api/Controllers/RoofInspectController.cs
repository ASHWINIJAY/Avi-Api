using System.Text.Json;
using System.IO;
using iText.IO.Font.Constants;
using iText.IO.Image;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using System.Net.Http;
using AviFinal.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using iText.Kernel.Colors;

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

            public double Latitude { get; set; }
            public double Longitude { get; set; }
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


            //Get InspectFormId for inspection screens
            var frontLoco = await _context.FrontLocoInspects.FirstOrDefaultAsync(l => l.LocoNumber == model.LocoNumber);
            var shortNose = await _context.ShortNoseInspects.FirstOrDefaultAsync(l => l.LocoNumber == model.LocoNumber);
            var cabLoco = await _context.CabLocoInspects.FirstOrDefaultAsync(l => l.LocoNumber == model.LocoNumber);
            var electCab = await _context.ElectCabInspects.FirstOrDefaultAsync(l => l.LocoNumber == model.LocoNumber);
            var batSwitch = await _context.BatSwitchInspects.FirstOrDefaultAsync(l => l.LocoNumber == model.LocoNumber);
            var leftMidDoor = await _context.LeftMidDoorInspects.FirstOrDefaultAsync(l => l.LocoNumber == model.LocoNumber);
            var cirBreakPan = await _context.CirBreakPanInspects.FirstOrDefaultAsync(l => l.LocoNumber == model.LocoNumber);
            var topRightPan = await _context.TopRightPanInspects.FirstOrDefaultAsync(l => l.LocoNumber == model.LocoNumber);
            var midPan = await _context.MidPanInspects.FirstOrDefaultAsync(l => l.LocoNumber == model.LocoNumber);
            var botLeftPan = await _context.BotLeftPanInspects.FirstOrDefaultAsync(l => l.LocoNumber == model.LocoNumber);
            var cenAir = await _context.CenAirInspects.FirstOrDefaultAsync(l => l.LocoNumber == model.LocoNumber);
            var engineDeck = await _context.EngineDeckInspects.FirstOrDefaultAsync(l => l.LocoNumber == model.LocoNumber);
            var comFan = await _context.ComFanInspects.FirstOrDefaultAsync(l => l.LocoNumber == model.LocoNumber);
            var endDeck = await _context.EndDeckInspects.FirstOrDefaultAsync(l => l.LocoNumber == model.LocoNumber);
            var coupGear = await _context.CoupGearInspects.FirstOrDefaultAsync(l => l.LocoNumber == model.LocoNumber);
            var roof = await _context.RoofInspects.FirstOrDefaultAsync(l => l.LocoNumber == model.LocoNumber);


            //Store InspectFormId for inspection screens
            string frontLocoID = frontLoco?.InspectFormId ?? "Unknown";
            string shortNoseID = shortNose?.InspectFormId ?? "Unknown";
            string cabLocoID = cabLoco?.InspectFormId ?? "Unknown";
            string electCabID = electCab?.InspectFormId ?? "Unknown";
            string batSwitchID = batSwitch?.InspectFormId ?? "Unknown";
            string leftMidDoorID = leftMidDoor?.InspectFormId ?? "Unknown";
            string cirBreakPanID = cirBreakPan?.InspectFormId ?? "Unknown";
            string topRightPanID = topRightPan?.InspectFormId ?? "Unknown";
            string midPanID = midPan?.InspectFormId ?? "Unknown";
            string botLeftPanID = botLeftPan?.InspectFormId ?? "Unknown";
            string cenAirID = cenAir?.InspectFormId ?? "Unknown";
            string engineDeckID = engineDeck?.InspectFormId ?? "Unknown";
            string comFanID = comFan?.InspectFormId ?? "Unknown";
            string endDeckID = endDeck?.InspectFormId ?? "Unknown";
            string coupGearID = coupGear?.InspectFormId ?? "Unknown";
            string roofID = roof?.InspectFormId ?? "Unknown";

            //Get Net Book Value of Locomotive
            var netBook = await _context.MasterLocos.FirstOrDefaultAsync(l => l.LocoNumber == model.LocoNumber);

            //Store Net Book Value of Locomotive
            decimal netBookValue = decimal.TryParse(netBook?.NetBookValue, out var v) ? v : 0;

            // Collect first photo
            string mainPhoto = locoInfo?.PhotoPath ?? "";

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

            var frontLocoRows = await _context.FrontLocoInspects
            .Where(x => x.LocoNumber == model.LocoNumber)
            .ToListAsync();

            var shortNoseRows = await _context.ShortNoseInspects
            .Where(x => x.LocoNumber == model.LocoNumber)
            .ToListAsync();

            var cabLocoRows = await _context.CabLocoInspects
            .Where(x => x.LocoNumber == model.LocoNumber)
            .ToListAsync();

            var electCabRows = await _context.ElectCabInspects
            .Where(x => x.LocoNumber == model.LocoNumber)
            .ToListAsync();

            var batSwitchRows = await _context.BatSwitchInspects
            .Where(x => x.LocoNumber == model.LocoNumber)
            .ToListAsync();

            var leftMidRows = await _context.LeftMidDoorInspects
            .Where(x => x.LocoNumber == model.LocoNumber)
            .ToListAsync();

            var cirBreakRows = await _context.CirBreakPanInspects
            .Where(x => x.LocoNumber == model.LocoNumber)
            .ToListAsync();

            var topRightRows = await _context.TopRightPanInspects
            .Where(x => x.LocoNumber == model.LocoNumber)
            .ToListAsync();

            var midPanRows = await _context.MidPanInspects
            .Where(x => x.LocoNumber == model.LocoNumber)
            .ToListAsync();

            var botLeftRows = await _context.BotLeftPanInspects
            .Where(x => x.LocoNumber == model.LocoNumber)
            .ToListAsync();

            var cenAirRows = await _context.CenAirInspects
            .Where(x => x.LocoNumber == model.LocoNumber)
            .ToListAsync();

            var engineRows = await _context.EngineDeckInspects
            .Where(x => x.LocoNumber == model.LocoNumber)
            .ToListAsync();

            var comFanRows = await _context.ComFanInspects
            .Where(x => x.LocoNumber == model.LocoNumber)
            .ToListAsync();

            var endRows = await _context.EndDeckInspects
            .Where(x => x.LocoNumber == model.LocoNumber)
            .ToListAsync();

            var coupGearRows = await _context.CoupGearInspects
            .Where(x => x.LocoNumber == model.LocoNumber)
            .ToListAsync();

            var roofRows = await _context.RoofInspects
            .Where(x => x.LocoNumber == model.LocoNumber)
            .ToListAsync();

            decimal frontLocoReplace = frontLocoRows.Sum(r => decimal.TryParse(r.ReplaceCost, out var v) ? v : 0);
            decimal frontLocoRefurbish = frontLocoRows.Sum(r => decimal.TryParse(r.RefurbishCost, out var v) ? v : 0);

            decimal shortNoseReplace = shortNoseRows.Sum(r => decimal.TryParse(r.ReplaceCost, out var v) ? v : 0);
            decimal shortNoseRefurbish = shortNoseRows.Sum(r => decimal.TryParse(r.RefurbishCost, out var v) ? v : 0);

            decimal cabLocoReplace = cabLocoRows.Sum(r => decimal.TryParse(r.ReplaceCost, out var v) ? v : 0);
            decimal cabLocoRefurbish = cabLocoRows.Sum(r => decimal.TryParse(r.RefurbishCost, out var v) ? v : 0);

            decimal electCabReplace = electCabRows.Sum(r => decimal.TryParse(r.ReplaceCost, out var v) ? v : 0);
            decimal electCabRefurbish = electCabRows.Sum(r => decimal.TryParse(r.RefurbishCost, out var v) ? v : 0);

            decimal batSwitchReplace = batSwitchRows.Sum(r => decimal.TryParse(r.ReplaceCost, out var v) ? v : 0);
            decimal batSwitchRefurbish = batSwitchRows.Sum(r => decimal.TryParse(r.RefurbishCost, out var v) ? v : 0);

            decimal leftMidReplace = leftMidRows.Sum(r => decimal.TryParse(r.ReplaceCost, out var v) ? v : 0);
            decimal leftMidRefurbish = leftMidRows.Sum(r => decimal.TryParse(r.RefurbishCost, out var v) ? v : 0);

            decimal cirBreakReplace = cirBreakRows.Sum(r => decimal.TryParse(r.ReplaceCost, out var v) ? v : 0);
            decimal cirBreakRefurbish = cirBreakRows.Sum(r => decimal.TryParse(r.RefurbishCost, out var v) ? v : 0);

            decimal topRightReplace = topRightRows.Sum(r => decimal.TryParse(r.ReplaceCost, out var v) ? v : 0);
            decimal topRightRefurbish = topRightRows.Sum(r => decimal.TryParse(r.RefurbishCost, out var v) ? v : 0);

            decimal midPanReplace = midPanRows.Sum(r => decimal.TryParse(r.ReplaceCost, out var v) ? v : 0);
            decimal midPanRefurbish = midPanRows.Sum(r => decimal.TryParse(r.RefurbishCost, out var v) ? v : 0);

            decimal botLeftReplace = botLeftRows.Sum(r => decimal.TryParse(r.ReplaceCost, out var v) ? v : 0);
            decimal botLeftRefurbish = botLeftRows.Sum(r => decimal.TryParse(r.RefurbishCost, out var v) ? v : 0);

            decimal cenAirReplace = cenAirRows.Sum(r => decimal.TryParse(r.ReplaceCost, out var v) ? v : 0);
            decimal cenAirRefurbish = cenAirRows.Sum(r => decimal.TryParse(r.RefurbishCost, out var v) ? v : 0);

            decimal engineReplace = engineRows.Sum(r => decimal.TryParse(r.ReplaceCost, out var v) ? v : 0);
            decimal engineRefurbish = engineRows.Sum(r => decimal.TryParse(r.RefurbishCost, out var v) ? v : 0);

            decimal comFanReplace = comFanRows.Sum(r => decimal.TryParse(r.ReplaceCost, out var v) ? v : 0);
            decimal comFanRefurbish = comFanRows.Sum(r => decimal.TryParse(r.RefurbishCost, out var v) ? v : 0);

            decimal endReplace = endRows.Sum(r => decimal.TryParse(r.ReplaceCost, out var v) ? v : 0);
            decimal endRefurbish = endRows.Sum(r => decimal.TryParse(r.RefurbishCost, out var v) ? v : 0);

            decimal coupGearReplace = coupGearRows.Sum(r => decimal.TryParse(r.ReplaceCost, out var v) ? v : 0);
            decimal coupGearRefurbish = coupGearRows.Sum(r => decimal.TryParse(r.RefurbishCost, out var v) ? v : 0);

            decimal roofReplace = roofRows.Sum(r => decimal.TryParse(r.ReplaceCost, out var v) ? v : 0);
            decimal roofRefurbish = roofRows.Sum(r => decimal.TryParse(r.RefurbishCost, out var v) ? v : 0);

            decimal subTotal = totalRefurbishValue + totalReplaceValue;
            decimal finalTotal = subTotal + netBookValue;

            // Get user location city/area
            string location = await GetCityFromCoordinatesAsync(model.Latitude, model.Longitude);

            // Generate PDF certificate
            string pdfFileName = $"InspectionCertificate_{model.LocoNumber}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
            string pdfPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "certificates", pdfFileName);
            Directory.CreateDirectory(Path.GetDirectoryName(pdfPath)!);

            GenerateEvaluationCertificatePdf(pdfPath, model.LocoNumber, inspectorName, mainPhoto, totalReplaceValue, totalRefurbishValue, location);

            string pdfQuote = $"InspectionQuote_{model.LocoNumber}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
            string pdfQuotePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "quotes", pdfQuote);
            Directory.CreateDirectory(Path.GetDirectoryName(pdfQuotePath)!);

            /*GenerateQuotePdf(
                pdfQuotePath,
                model.LocoNumber,
                inspectorName,
                frontLocoID,
                shortNoseID,
                cabLocoID,
                electCabID,
                batSwitchID,
                leftMidDoorID,
                cirBreakPanID,
                topRightPanID,
                midPanID,
                botLeftPanID,
                cenAirID,
                engineDeckID,
                comFanID,
                endDeckID,
                coupGearID,
                roofID,
                netBookValue,
                frontLocoRefurbish,
                frontLocoReplace,
                shortNoseRefurbish,
                shortNoseReplace,
                cabLocoRefurbish,
                cabLocoReplace,
                electCabRefurbish,
                electCabReplace,
                batSwitchRefurbish,
                batSwitchReplace,
                leftMidRefurbish,
                leftMidReplace,
                cirBreakRefurbish,
                cirBreakReplace,
                topRightRefurbish,
                topRightReplace,
                midPanRefurbish,
                midPanReplace,
                botLeftRefurbish,
                botLeftReplace,
                cenAirRefurbish,
                cenAirReplace,
                engineRefurbish,
                engineReplace,
                comFanRefurbish,
                comFanReplace,
                subTotal,
                finalTotal
            );*/

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
                AssessmentQuote = $"/quotes/{pdfQuote}",
                AssessmentCert = $"/certificates/{pdfFileName}",
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

            return Ok(new { message = "Roof Inspect submitted successfully." });
        }

        // Reverse geocoding helper
        public async Task<string> GetCityFromCoordinatesAsync(double latitude, double longitude)
        {
            try
            {
                string url = $"https://nominatim.openstreetmap.org/reverse?format=jsonv2&lat={latitude}&lon={longitude}&namedetails=1";
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "AVI-Inspection-App");

                var response = await client.GetStringAsync(url);
                using JsonDocument doc = JsonDocument.Parse(response);

                if (doc.RootElement.TryGetProperty("address", out var address))
                {
                    string? location = null;

                    string[] keys = { "city", "town", "village", "municipality", "county", "state", "region" };
                    foreach (var key in keys)
                    {
                        if (address.TryGetProperty(key, out var value) && !string.IsNullOrEmpty(value.GetString()))
                        {
                            location = value.GetString();
                            break;
                        }
                    }

                    // Fallback to "namedetails"
                    if (location == null && doc.RootElement.TryGetProperty("namedetails", out var namedetails))
                    {
                        var firstName = namedetails.EnumerateObject().FirstOrDefault();
                        if (firstName.Value.ValueKind == JsonValueKind.String)
                            location = firstName.Value.GetString();
                    }

                    // Fallback to display_name
                    if (location == null && doc.RootElement.TryGetProperty("display_name", out var display))
                    {
                        location = display.GetString()?.Split(',').FirstOrDefault();
                    }

                    return location ?? "Unknown Location";
                }

                return "Unknown Location";
            }
            catch
            {
                return "Unknown Location";
            }
        }

        private void GenerateEvaluationCertificatePdf(
            string path,
            int locoNumber,
            string inspectorName,
            string photoPath,
            decimal replaceValue,
            decimal refurbishValue,
            string location)
        {
            using var writer = new PdfWriter(path);
            using var pdf = new PdfDocument(writer);
            var document = new Document(pdf);

            // Fonts
            var headerFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var bodyFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            // Header
            var header = new Paragraph("AVI")
                .SetFont(headerFont)
                .SetFontSize(24)
                .SetFontColor(ColorConstants.BLUE)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(10);
            document.Add(header);

            var title = new Paragraph("Evaluation Certificate")
                .SetFont(headerFont)
                .SetFontSize(16)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(20);
            document.Add(title);

            // Handle image safely
            string absolutePath = string.Empty;

            if (!string.IsNullOrEmpty(photoPath))
            {
                absolutePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", photoPath.TrimStart('/', '\\'));
            }

            if (!string.IsNullOrEmpty(absolutePath) && System.IO.File.Exists(absolutePath))
            {
                try
                {
                    var img = new Image(ImageDataFactory.Create(absolutePath))
                        .SetHorizontalAlignment(HorizontalAlignment.CENTER)
                        .SetMaxWidth(400)
                        .SetAutoScale(true)
                        .SetMarginBottom(20);
                    document.Add(img);
                }
                catch
                {
                    // Image failed to load, ignore and continue
                    var placeholder = new Paragraph("[Photo Unavailable]")
                        .SetFont(bodyFont)
                        .SetFontSize(12)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetMarginBottom(20);
                    document.Add(placeholder);
                }
            }
            else
            {
                // Photo missing
                var placeholder = new Paragraph("[Photo Unavailable]")
                    .SetFont(bodyFont)
                    .SetFontSize(12)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginBottom(20);
                document.Add(placeholder);
            }

            // Styled Table
            var table = new Table(UnitValue.CreatePercentArray(new float[] { 1, 2 })).UseAllAvailableWidth();

            void AddHeaderCell(string text)
            {
                table.AddHeaderCell(new Cell()
                    .Add(new Paragraph(text).SetFont(headerFont).SetFontColor(ColorConstants.WHITE))
                    .SetBackgroundColor(ColorConstants.BLUE)
                    .SetTextAlignment(TextAlignment.LEFT)
                    .SetPadding(5));
            }

            void AddBodyCell(string text)
            {
                table.AddCell(new Cell()
                    .Add(new Paragraph(text).SetFont(bodyFont))
                    .SetTextAlignment(TextAlignment.LEFT)
                    .SetPadding(5));
            }

            // Add rows
            AddHeaderCell("Field"); AddHeaderCell("Value");

            AddBodyCell("Loco Number"); AddBodyCell(locoNumber.ToString());
            AddBodyCell("Inspector"); AddBodyCell(inspectorName);
            AddBodyCell("Replace Value"); AddBodyCell(replaceValue.ToString());
            AddBodyCell("Refurbish Value"); AddBodyCell(refurbishValue.ToString());
            AddBodyCell("Location"); AddBodyCell(location);
            AddBodyCell("Date of Assessment"); AddBodyCell(DateTime.Now.ToString("yyyy-MM-dd"));

            document.Add(table);

            // Footer
            var footer = new Paragraph("© 2025 Codex-IT. All Rights Reserved.")
                .SetFont(bodyFont)
                .SetFontSize(10)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginTop(30);
            document.Add(footer);

            document.Close();
        }

        private void GenerateQuotePdf(
            string path,
            int locoNumber,
            string inspectorName,
            string walkAroundID,
            string frontLocoID,
            string shortNoseID,
            string cabLocoID,
            string electCabID,
            string batSwitchID,
            string leftMidDoorID,
            string cirBreakPanID,
            string topRightPanID,
            string midPanID,
            string botLeftPanID,
            string cenAirID,
            string engineDeckID,
            string comFanID,
            string endDeckID,
            string coupGearID,
            string roofID,
            decimal netBookValue,
            decimal walkAroundRefurbish,
            decimal walkAroundReplace,
            decimal frontLocoRefurbish,
            decimal frontLocoReplace,
            decimal shortNoseRefurbish,
            decimal shortNoseReplace,
            decimal cabLocoRefurbish,
            decimal cabLocoReplace,
            decimal electCabRefurbish,
            decimal electCabReplace,
            decimal batSwitchRefurbish,
            decimal batSwitchReplace,
            decimal leftMidRefurbish,
            decimal leftMidReplace,
            decimal cirBreakRefurbish,
            decimal cirBreakReplace,
            decimal topRightRefurbish,
            decimal topRightReplace,
            decimal midPanRefurbish,
            decimal midPanReplace,
            decimal botLeftRefurbish,
            decimal botLeftReplace,
            decimal cenAirRefurbish,
            decimal cenAirReplace,
            decimal engineRefurbish,
            decimal engineReplace,
            decimal comFanRefurbish,
            decimal comFanReplace,
            decimal endRefurbish,
            decimal endReplace,
            decimal coupGearRefurbish,
            decimal coupGearReplace,
            decimal roofRefurbish,
            decimal roofReplace,
            decimal subTotal,
            decimal finalTotal
        )
        {
            using var writer = new PdfWriter(path);
            using var pdf = new PdfDocument(writer);
            var document = new Document(pdf);

            // Fonts
            var headerFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var bodyFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            // Header
            var header = new Paragraph("AVI")
                .SetFont(headerFont)
                .SetFontSize(24)
                .SetFontColor(ColorConstants.BLUE)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(10);
            document.Add(header);

            var title = new Paragraph("Inspection Quote")
                .SetFont(headerFont)
                .SetFontSize(16)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginBottom(20);
            document.Add(title);

            // Styled Table
            var table = new Table(UnitValue.CreatePercentArray(new float[] { 1, 2 })).UseAllAvailableWidth();

            void AddHeaderCell(string text)
            {
                table.AddHeaderCell(new Cell()
                    .Add(new Paragraph(text).SetFont(headerFont).SetFontColor(ColorConstants.WHITE))
                    .SetBackgroundColor(ColorConstants.BLUE)
                    .SetTextAlignment(TextAlignment.LEFT)
                    .SetPadding(5));
            }

            void AddBodyCell(string text)
            {
                table.AddCell(new Cell()
                    .Add(new Paragraph(text).SetFont(bodyFont))
                    .SetTextAlignment(TextAlignment.LEFT)
                    .SetPadding(5));
            }

            // Add rows
            AddHeaderCell("Field"); AddHeaderCell("Value");

            AddBodyCell("Loco Number"); AddBodyCell(locoNumber.ToString());
            AddBodyCell("Inspector"); AddBodyCell(inspectorName);
            AddBodyCell("Date of Assessment"); AddBodyCell(DateTime.Now.ToString("yyyy-MM-dd"));
            AddBodyCell("Net Book Val"); AddBodyCell(netBookValue.ToString());
            AddBodyCell("Form ID"); AddBodyCell(walkAroundID);
            AddBodyCell("Refurbish Val"); AddBodyCell(walkAroundRefurbish.ToString());
            AddBodyCell("Replace Val"); AddBodyCell(walkAroundReplace.ToString());
            AddBodyCell("Form ID"); AddBodyCell(shortNoseID);
            AddBodyCell("Refurbish Val"); AddBodyCell(shortNoseRefurbish.ToString());
            AddBodyCell("Replace Val"); AddBodyCell(shortNoseReplace.ToString());
            AddBodyCell("Form ID"); AddBodyCell(frontLocoID);
            AddBodyCell("Refurbish Val"); AddBodyCell(frontLocoRefurbish.ToString());
            AddBodyCell("Replace Val"); AddBodyCell(frontLocoReplace.ToString());
            AddBodyCell("Form ID"); AddBodyCell(cabLocoID);
            AddBodyCell("Refurbish Val"); AddBodyCell(cabLocoRefurbish.ToString());
            AddBodyCell("Replace Val"); AddBodyCell(cabLocoReplace.ToString());
            AddBodyCell("Form ID"); AddBodyCell(electCabID);
            AddBodyCell("Refurbish Val"); AddBodyCell(electCabRefurbish.ToString());
            AddBodyCell("Replace Val"); AddBodyCell(electCabReplace.ToString());
            AddBodyCell("Form ID"); AddBodyCell(batSwitchID);
            AddBodyCell("Refurbish Val"); AddBodyCell(batSwitchRefurbish.ToString());
            AddBodyCell("Replace Val"); AddBodyCell(batSwitchReplace.ToString());
            AddBodyCell("Form ID"); AddBodyCell(leftMidDoorID);
            AddBodyCell("Refurbish Val"); AddBodyCell(leftMidRefurbish.ToString());
            AddBodyCell("Replace Val"); AddBodyCell(leftMidReplace.ToString());
            AddBodyCell("Form ID"); AddBodyCell(cirBreakPanID);
            AddBodyCell("Refurbish Val"); AddBodyCell(cirBreakRefurbish.ToString());
            AddBodyCell("Replace Val"); AddBodyCell(cirBreakReplace.ToString());
            AddBodyCell("Form ID"); AddBodyCell(topRightPanID);
            AddBodyCell("Refurbish Val"); AddBodyCell(topRightRefurbish.ToString());
            AddBodyCell("Replace Val"); AddBodyCell(topRightReplace.ToString());
            AddBodyCell("Form ID"); AddBodyCell(midPanID);
            AddBodyCell("Refurbish Val"); AddBodyCell(midPanRefurbish.ToString());
            AddBodyCell("Replace Val"); AddBodyCell(midPanReplace.ToString());
            AddBodyCell("Form ID"); AddBodyCell(botLeftPanID);
            AddBodyCell("Refurbish Val"); AddBodyCell(botLeftRefurbish.ToString());
            AddBodyCell("Replace Val"); AddBodyCell(botLeftReplace.ToString());
            AddBodyCell("Form ID"); AddBodyCell(cenAirID);
            AddBodyCell("Refurbish Val"); AddBodyCell(cenAirRefurbish.ToString());
            AddBodyCell("Replace Val"); AddBodyCell(cenAirReplace.ToString());
            AddBodyCell("Form ID"); AddBodyCell(engineDeckID);
            AddBodyCell("Refurbish Val"); AddBodyCell(engineRefurbish.ToString());
            AddBodyCell("Replace Val"); AddBodyCell(engineReplace.ToString());
            AddBodyCell("Form ID"); AddBodyCell(comFanID);
            AddBodyCell("Refurbish Val"); AddBodyCell(comFanRefurbish.ToString());
            AddBodyCell("Replace Val"); AddBodyCell(comFanReplace.ToString());
            AddBodyCell("Form ID"); AddBodyCell(endDeckID);
            AddBodyCell("Refurbish Val"); AddBodyCell(endRefurbish.ToString());
            AddBodyCell("Replace Val"); AddBodyCell(endReplace.ToString());
            AddBodyCell("Form ID"); AddBodyCell(coupGearID);
            AddBodyCell("Refurbish Val"); AddBodyCell(coupGearRefurbish.ToString());
            AddBodyCell("Replace Val"); AddBodyCell(coupGearReplace.ToString());
            AddBodyCell("Form ID"); AddBodyCell(roofID);
            AddBodyCell("Refurbish Val"); AddBodyCell(roofRefurbish.ToString());
            AddBodyCell("Replace Val"); AddBodyCell(roofReplace.ToString());
            AddBodyCell("Sub Total"); AddBodyCell(subTotal.ToString());
            AddBodyCell("Final Total"); AddBodyCell(finalTotal.ToString());

            document.Add(table);

            // Footer
            var footer = new Paragraph("© 2025 Codex-IT. All Rights Reserved.")
                .SetFont(bodyFont)
                .SetFontSize(10)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginTop(30);
            document.Add(footer);

            document.Close();
        }
    }
}
