using AviAppFinal.Server.Models;
using AviFinal.Api.Models;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Compression;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AviAppFinal.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DCFController : ControllerBase
    {
        private readonly AviDbContext _context;

        public DCFController(AviDbContext context)
        {
            _context = context;
        }

        [HttpGet("currentSetup")]
        public async Task<IActionResult> CurrentSetup()
        {
            var wacc = await _context.WaccSetups
                .Select(w => new
                {
                    PostTax = w.PostTax ?? "0.00",
                    PreTax = w.PreTax ?? "0.00",
                })
                .FirstOrDefaultAsync();

            return Ok(wacc);
        }

        [HttpPost("updateSetup")]
        public async Task<IActionResult> UpdateSetup([FromForm] Setup setup)
        {
            try
            {
                var user = await _context.LeaseCoUsers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.UserId == setup.UserId);

                string? userName = user?.UserName;

                int id = 1;
                var wacc = await _context.WaccSetups
                    .FirstOrDefaultAsync(w => w.Id == id);

                if (wacc != null)
                {
                    decimal post = ParseDecimalSafe(setup.PostTax);
                    decimal pre = ParseDecimalSafe(setup.PreTax);

                    wacc.PostTax = post.ToString("N2", new CultureInfo("en-ZA"));
                    wacc.PreTax = pre.ToString("N2", new CultureInfo("en-ZA"));
                    wacc.UpdateDate = DateTime.Now.ToString("yyyy-MM-dd");
                    wacc.UpdateBy = userName ?? "N/A";
                    _context.WaccSetups.Update(wacc);

                    var wagonInput = await _context.WagonInputs
                        .ToListAsync();

                    var locoInput = await _context.LocoInputs
                        .ToListAsync();

                    if (wagonInput.Count != 0)
                    {
                        foreach (var wagon in wagonInput)
                        {
                            wagon.PostTax = post.ToString("N2", new CultureInfo("en-ZA"));
                            wagon.PreTax = pre.ToString("N2", new CultureInfo("en-ZA"));

                            _context.WagonInputs.Update(wagon);
                        }
                    }

                    if (locoInput.Count != 0)
                    {
                        foreach (var loco in locoInput)
                        {
                            loco.PostTax = post.ToString("N2", new CultureInfo("en-ZA"));
                            loco.PreTax = pre.ToString("N2", new CultureInfo("en-ZA"));

                            _context.LocoInputs.Update(loco);
                        }
                    } 
                }
                else
                {
                    return BadRequest("Current WACC Setup does not exist.");
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = "WACC updated successfully." });

            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Update failed.", detail = ex.Message });
            }
        }

        // ADJUSTED ↓
        [HttpGet("getWagons")]
        public async Task<IActionResult> GetWagons()
        {
            _context.Database.SetCommandTimeout(180);

            var wagon = await _context.WagonInputs
                .Select(w => new
                {
                    w.WagonNumber
                })
                .ToListAsync();

            return Ok(wagon);
        }

        [HttpGet("getInfo/{wagonNumber}")]
        public async Task<IActionResult> GetInfo (int wagonNumber)
        {
            bool exists = await _context.WagonInputs
                .AnyAsync(e => e.WagonNumber == wagonNumber);

            if (exists)
            {
                var input = await _context.WagonInputs
                    .AsNoTracking()
                    .FirstOrDefaultAsync(i => i.WagonNumber == wagonNumber);

                if (input == null)
                    return BadRequest("Wagon does not exist.");

                return Ok(new
                {
                    input.WagonType,
                    input.NetBookValue,
                    input.ScrapValue,
                    input.ScrappingCost,
                    input.NewScrapValue,
                    input.TotalCost,
                    input.LeaseTerm,
                    input.LeaseIncome,
                    input.EscalationRate,
                    input.UseAfterRefurbish,
                    input.ResidualValue,
                    input.PostTax,
                    input.WearTearPeriod,
                    input.OperatingCosts,
                    input.OperatingCostsEscalation,
                    input.CorporateTaxRate,
                    input.PreTax,
                });
            }

            int id = 1;

            var master = await _context.MasterWagons
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.WagonNumber == wagonNumber);

            var wacc = await _context.WaccSetups
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.Id == id);

            if (master == null)
                return BadRequest("Wagon does not exist.");

            string assetType = master.WagonType;

            var asset = await _context.AssetTypeSetups
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.AssetType == assetType);

            string netBook = "0.00";
            if (!string.IsNullOrWhiteSpace(master.NetBookValue))
            {
                var sanitized = master.NetBookValue.Replace("R", "").Replace(" ", "").Replace(".", "").Replace(",", ".");
                if (decimal.TryParse(sanitized, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal nb))
                    netBook = nb.ToString("N2", new CultureInfo("en-ZA"));
                else
                    netBook = master.NetBookValue;
            }

            string? scrapValue = string.Empty;

            decimal sv = ParseDecimalSafe(master.ScrapValue);
            scrapValue = sv.ToString("N2", new CultureInfo("en-ZA"));

            string leaseIncome = string.Empty;
            int leaseTerm = 0;
            string escalationRate = string.Empty;
            int useAfterRefurb = 0;
            int wearTearPeriod = 0;
            string operCosts = string.Empty;
            string operEscalation = string.Empty;
            string coporateTax = string.Empty;

            if (asset != null)
            {
                leaseIncome = asset.LeaseIncome;
                leaseTerm = asset.LeaseTerm;
                escalationRate = asset.EscalationRate;
                useAfterRefurb = asset.UseAfterRefurbish;
                wearTearPeriod = asset.WearTearPeriod;
                operCosts = asset.OperatingCosts;
                operEscalation = asset.OperatingCostsEscalation;
                coporateTax = asset.CorporateTaxRate;
            }
            else
            {
                leaseIncome = "";
                leaseTerm = 0;
                escalationRate = "";
                useAfterRefurb = 0;
                wearTearPeriod = 0;
                operCosts = "";
                operEscalation = "";
                coporateTax = "";
            }

            var wagon = await _context.WagonDashboards
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.WagonNumber == wagonNumber);

            string? totalCost = string.Empty;

            if (wagon != null)
            {
                decimal tc = ParseDecimalSafe(wagon.TotalValue);
                totalCost = tc.ToString("N2", new CultureInfo("en-ZA"));
            }
            else
            {
                decimal rev = ParseDecimalSafe(wagon?.RefurbishValue);
                decimal mv = ParseDecimalSafe(wagon?.MissingValue);
                decimal rpv = ParseDecimalSafe(wagon?.ReplaceValue);
                decimal lv = ParseDecimalSafe(wagon?.TotalLaborValue);
                decimal lfv = ParseDecimalSafe(wagon?.LiftValue);
                decimal bv = ParseDecimalSafe(wagon?.BarrelValue);

                decimal tv = rev + mv + rpv + lv + lfv + bv;

                totalCost = tv.ToString("N2", new CultureInfo("en-ZA"));
            }
                return Ok(new
                {
                    master.WagonType,
                    NetBookValue = netBook ?? "0.00",
                    ScrapValue = scrapValue ?? "0.00",
                    ScrappingCost = "",
                    NewScrapValue = "",
                    TotalCost = totalCost,
                    LeaseTerm = leaseTerm,
                    LeaseIncome = leaseIncome,
                    EscalationRate = escalationRate,
                    UseAfterRefurbish = useAfterRefurb,
                    ResidualValue = "",
                    PostTax = wacc?.PostTax ?? "0.00",
                    WearTearPeriod = wearTearPeriod,
                    OperatingCosts = operCosts,
                    OperatingCostsEscalation = operEscalation,
                    CorporateTaxRate = coporateTax,
                    PreTax = wacc?.PreTax ?? "0.00",
                });
        }

        [HttpPost("updateInsertWagon")]
        public async Task<IActionResult> UpdateInsertWagon([FromForm] InputWagon wagon)
        {
            bool exists = await _context.WagonInputs
                .AnyAsync(e => e.WagonNumber == wagon.WagonNumber);

            var user = await _context.LeaseCoUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == wagon.UserId);

            if (user == null)
                return BadRequest("User does not exist.");

            string userName = user.UserName;

            if (exists) {

                try
                {
                    var input = await _context.WagonInputs
                        .FirstOrDefaultAsync(i => i.WagonNumber == wagon.WagonNumber);

                    if (input != null)
                    {
                        input.WagonNumber = wagon.WagonNumber;
                        input.WagonType = wagon.WagonType ?? "N/A";

                        if (wagon.NetBookValue == input.NetBookValue)
                        {
                            input.NetBookValue = wagon.NetBookValue;
                        }
                        else
                        {
                            decimal nbv = ParseDecimalSafe(wagon.NetBookValue);
                            input.NetBookValue = nbv.ToString("N2", new CultureInfo("en-ZA"));
                        }

                        if (wagon.ScrapValue == input.ScrapValue)
                        {
                            input.ScrapValue = wagon.ScrapValue;
                        }
                        else
                        {
                            decimal sv = ParseDecimalSafe(wagon.ScrapValue);
                            input.ScrapValue = sv.ToString("N2", new CultureInfo("en-ZA"));
                        }

                        if (wagon.ScrappingCost == input.ScrappingCost)
                        {
                            input.ScrappingCost = wagon.ScrappingCost;
                        }
                        else
                        {
                            decimal scvc = ParseDecimalSafe(wagon.ScrappingCost);
                            input.ScrappingCost = scvc.ToString("N2", new CultureInfo("en-ZA"));
                        }

                        if (wagon.NewScrapValue == input.NewScrapValue)
                        {
                            input.NewScrapValue = wagon.NewScrapValue;
                        }
                        else
                        {
                            decimal nsv = ParseDecimalSafe(wagon.NewScrapValue);
                            input.NewScrapValue = nsv.ToString("N2", new CultureInfo("en-ZA"));
                        }

                        if (wagon.ResidualValue == input.ResidualValue)
                        {
                            input.ResidualValue = wagon.ResidualValue;
                        }
                        else
                        {
                            decimal rsv = ParseDecimalSafe(wagon.ResidualValue);
                            input.ResidualValue = rsv.ToString("N2", new CultureInfo("en-ZA"));
                        }

                        input.TotalCost = wagon.TotalCost ?? "0.00";
                        input.LeaseIncome = wagon.LeaseIncome ?? "0.00";
                        input.LeaseTerm = wagon.LeaseTerm;
                        input.EscalationRate = wagon.EscalationRate ?? "0.00";
                        input.UseAfterRefurbish = wagon.UseAfterRefurbish;
                        input.PostTax = wagon.PostTax ?? "0.00";
                        input.WearTearPeriod = wagon.WearTearPeriod;
                        input.OperatingCosts = wagon.OperatingCosts ?? "0.00";
                        input.OperatingCostsEscalation = wagon.OperatingCostsEscalation ?? "0.00";
                        input.CorporateTaxRate = wagon.CorporateTaxRate ?? "0.00";
                        input.PreTax = wagon.PreTax ?? "0.00";
                        input.DateSaved = DateTime.Now.ToString("yyyy-MM-dd");
                        input.SavedBy = userName ?? "N/A";

                        _context.WagonInputs.Update(input);

                        await _context.SaveChangesAsync();

                        return Ok(new { message = "Wagon input updated successfully." });
                    }
                    else
                    {
                        return BadRequest("Wagon does not exist.");
                    }
                }
                catch (Exception ex) {
                    return StatusCode(500, new { error = "Update failed.", detail = ex.Message });
                }
            }

            try
            {
                
                decimal nbv = ParseDecimalSafe(wagon.NetBookValue);
                decimal sv = ParseDecimalSafe(wagon.ScrapValue);
                decimal scvc = ParseDecimalSafe(wagon.ScrappingCost);
                decimal nsv = ParseDecimalSafe(wagon.NewScrapValue);
                decimal rsv = ParseDecimalSafe(wagon.ResidualValue);

                var inputEntry = new WagonInput
                {
                    WagonNumber = wagon.WagonNumber,
                    WagonType = wagon.WagonType ?? "N/A",
                    NetBookValue = nbv.ToString("N2", new CultureInfo("en-ZA")),
                    ScrapValue = sv.ToString("N2", new CultureInfo("en-ZA")),
                    ScrappingCost = scvc.ToString("N2", new CultureInfo("en-ZA")),
                    NewScrapValue = nsv.ToString("N2", new CultureInfo("en-ZA")),
                    TotalCost = wagon.TotalCost ?? "0.00", 
                    LeaseTerm = wagon.LeaseTerm,
                    LeaseIncome = wagon.LeaseIncome ?? "0.00", 
                    EscalationRate = wagon.EscalationRate ?? "0.00",
                    UseAfterRefurbish = wagon.UseAfterRefurbish,
                    ResidualValue = rsv.ToString("N2", new CultureInfo("en-ZA")),
                    PostTax = wagon.PostTax ?? "0.00",
                    WearTearPeriod = wagon.WearTearPeriod,
                    OperatingCosts = wagon.OperatingCosts ?? "0.00",
                    OperatingCostsEscalation = wagon.OperatingCostsEscalation ?? "0.00", 
                    CorporateTaxRate = wagon.CorporateTaxRate ?? "0.00",
                    PreTax = wagon.PreTax ?? "0.00",
                    DateSaved = DateTime.Now.ToString("yyyy-MM-dd"),
                    SavedBy = userName ?? "N/A",
                };

                _context.WagonInputs.Add(inputEntry);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Wagon input inserted successfully." });

            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Insert failed.", detail = ex.Message });
            }
        }

        // ADJUSTED ↓
        [HttpGet("getLocos")]
        public async Task<IActionResult> GetLocos()
        {
            _context.Database.SetCommandTimeout(180);

            var loco = await _context.LocoInputs
                .Select(w => new
                {
                    w.LocoNumber
                })
                .ToListAsync();

            return Ok(loco);
        }

        [HttpGet("getInfoLoco/{locoNumber}")]
        public async Task<IActionResult> GetInfoLoco(int locoNumber)
        {
            bool exists = await _context.LocoInputs
                .AnyAsync(e => e.LocoNumber == locoNumber);

            if (exists)
            {
                var input = await _context.LocoInputs
                    .AsNoTracking()
                    .FirstOrDefaultAsync(i => i.LocoNumber == locoNumber);

                if (input == null)
                    return BadRequest("Loco does not exist.");

                return Ok(new
                {
                    input.LocoType,
                    input.NetBookValue,
                    input.ScrapValue,
                    input.ScrappingCost,
                    input.NewScrapValue,
                    input.TotalCost,
                    input.LeaseTerm,
                    input.LeaseIncome,
                    input.EscalationRate,
                    input.UseAfterRefurbish,
                    input.ResidualValue,
                    input.PostTax,
                    input.WearTearPeriod,
                    input.OperatingCosts,
                    input.OperatingCostsEscalation,
                    input.CorporateTaxRate,
                    input.PreTax,
                });
            }

            int id = 1;

            var master = await _context.MasterLocos
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.LocoNumber == locoNumber);

            var wacc = await _context.WaccSetups
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.Id == id);

            if (master == null)
                return BadRequest("Loco does not exist.");

            string netBook = "#N/A";
            if (!string.IsNullOrWhiteSpace(master.NetBookValue) && master.NetBookValue != "#N/A")
            {
                var sanitized = master.NetBookValue.Replace("R", "").Replace(" ", "").Replace(".", "").Replace(",", ".");
                if (decimal.TryParse(sanitized, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal nb))
                    netBook = nb.ToString("N2", new CultureInfo("en-ZA"));
                else
                    netBook = master.NetBookValue;
            }

            string? scrapValue = string.Empty;

            decimal sv = ParseDecimalSafe(master.ScrapValue);
            scrapValue = sv.ToString("N2", new CultureInfo("en-ZA"));

            string assetType = master.LocoClass;

            var asset = await _context.AssetTypeSetups
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.AssetType == assetType);

            string leaseIncome = string.Empty;
            int leaseTerm = 0;
            string escalationRate = string.Empty;
            int useAfterRefurb = 0;
            int wearTearPeriod = 0;
            string operCosts = string.Empty;
            string operEscalation = string.Empty;
            string coporateTax = string.Empty;

            if (asset != null)
            {
                leaseIncome = asset.LeaseIncome;
                leaseTerm = asset.LeaseTerm;
                escalationRate = asset.EscalationRate;
                useAfterRefurb = asset.UseAfterRefurbish;
                wearTearPeriod = asset.WearTearPeriod;
                operCosts = asset.OperatingCosts;
                operEscalation = asset.OperatingCostsEscalation;
                coporateTax = asset.CorporateTaxRate;
            }
            else
            {
                leaseIncome = "";
                leaseTerm = 0;
                escalationRate = "";
                useAfterRefurb = 0;
                wearTearPeriod = 0;
                operCosts = "";
                operEscalation = "";
                coporateTax = "";
            }

            var loco = await _context.LocoDashboards
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.LocoNumber == locoNumber);

            string? totalCost = string.Empty;

            if (loco != null)
            {
                decimal tc = ParseDecimalSafe(loco.TotalValue);
                totalCost = tc.ToString("N2", new CultureInfo("en-ZA"));
            }
            else
            {
                decimal rev = ParseDecimalSafe(loco?.RefurbishValue);
                decimal mv = ParseDecimalSafe(loco?.MissingValue);
                decimal rpv = ParseDecimalSafe(loco?.ReplaceValue);
                decimal lv = ParseDecimalSafe(loco?.TotalLaborValue);

                decimal tv = rev + mv + rpv + lv;

                totalCost = tv.ToString("N2", new CultureInfo("en-ZA"));
            }

            return Ok(new
            {
                LocoType = master.LocoClass,
                NetBookValue = netBook ?? "0.00",
                ScrapValue = scrapValue ?? "0.00",
                ScrappingCost = "",
                NewScrapValue = "",
                TotalCost = totalCost,
                LeaseTerm = leaseTerm,
                LeaseIncome = leaseIncome,
                EscalationRate = escalationRate,
                UseAfterRefurbish = useAfterRefurb,
                ResidualValue = "",
                PostTax = wacc?.PostTax ?? "0.00",
                WearTearPeriod = wearTearPeriod,
                OperatingCosts = operCosts,
                OperatingCostsEscalation = operEscalation,
                CorporateTaxRate = coporateTax,
                PreTax = wacc?.PreTax ?? "0.00",
            });
        }

        [HttpPost("updateInsertLoco")]
        public async Task<IActionResult> UpdateInsertLoco([FromForm] InputLoco loco)
        {
            bool exists = await _context.LocoInputs
                .AnyAsync(e => e.LocoNumber == loco.LocoNumber);

            var user = await _context.LeaseCoUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == loco.UserId);

            if (user == null)
                return BadRequest("User does not exist.");

            string userName = user.UserName;

            if (exists)
            {

                try
                {
                    var input = await _context.LocoInputs
                        .FirstOrDefaultAsync(i => i.LocoNumber == loco.LocoNumber);

                    if (input != null)
                    {
                        input.LocoNumber = loco.LocoNumber;
                        input.LocoType = loco.LocoType ?? "N/A";

                        if (loco.NetBookValue == input.NetBookValue)
                        {
                            input.NetBookValue = loco.NetBookValue;
                        }
                        else
                        {
                            decimal nbv = ParseDecimalSafe(loco.NetBookValue);
                            input.NetBookValue = nbv.ToString("N2", new CultureInfo("en-ZA"));
                        }

                        if (loco.ScrapValue  == input.ScrapValue)
                        {
                            input.ScrapValue = loco.ScrapValue;
                        }
                        else
                        {
                            decimal sv = ParseDecimalSafe(loco.ScrapValue);
                            input.ScrapValue = sv.ToString("N2", new CultureInfo("en-ZA"));
                        }

                        if (loco.ScrappingCost == input.ScrappingCost)
                        {
                            input.ScrappingCost = loco.ScrappingCost;
                        }
                        else
                        {
                            decimal scvc = ParseDecimalSafe(loco.ScrappingCost);
                            input.ScrappingCost = scvc.ToString("N2", new CultureInfo("en-ZA"));
                        }

                        if (loco.NewScrapValue == input.NewScrapValue)
                        {
                            input.NewScrapValue = loco.NewScrapValue;
                        }
                        else
                        {
                            decimal nsv = ParseDecimalSafe(loco.NewScrapValue);
                            input.NewScrapValue = nsv.ToString("N2", new CultureInfo("en-ZA"));
                        }

                        input.TotalCost = loco.TotalCost ?? "0.00";
                        input.LeaseIncome = loco.LeaseIncome ?? "0.00"; 
                        input.LeaseTerm = loco.LeaseTerm;
                        input.EscalationRate = loco.EscalationRate ?? "0.00";
                        input.UseAfterRefurbish = loco.UseAfterRefurbish;

                        if (loco.ResidualValue == input.ResidualValue)
                        {
                            input.ResidualValue = loco.ResidualValue;
                        }
                        else
                        {
                            decimal rsv = ParseDecimalSafe(loco.ResidualValue);
                            input.ResidualValue = rsv.ToString("N2", new CultureInfo("en-ZA"));
                        }

                        input.PostTax = loco.PostTax ?? "0.00";
                        input.WearTearPeriod = loco.WearTearPeriod;
                        input.OperatingCosts = loco.OperatingCosts ?? "0.00";
                        input.OperatingCostsEscalation = loco.OperatingCostsEscalation ?? "0.00";
                        input.CorporateTaxRate = loco.CorporateTaxRate ?? "0.00";
                        input.PreTax = loco.PreTax ?? "0.00";
                        input.DateSaved = DateTime.Now.ToString("yyyy-MM-dd");
                        input.SavedBy = userName ?? "N/A";

                        _context.LocoInputs.Update(input);

                        await _context.SaveChangesAsync();

                        return Ok(new { message = "Wagon input updated successfully." });
                    }
                    else
                    {
                        return BadRequest("Wagon does not exist.");
                    }
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { error = "Update failed.", detail = ex.Message });
                }
            }

            try
            {
                decimal nbv = ParseDecimalSafe(loco.NetBookValue);
                decimal sv = ParseDecimalSafe(loco.ScrapValue);
                decimal scvc = ParseDecimalSafe(loco.ScrappingCost);
                decimal nsv = ParseDecimalSafe(loco.NewScrapValue);
                decimal rsv = ParseDecimalSafe(loco.ResidualValue);

                var inputEntry = new LocoInput
                {
                    LocoNumber = loco.LocoNumber,
                    LocoType = loco.LocoType ?? "N/A",
                    NetBookValue = nbv.ToString("N2", new CultureInfo("en-ZA")),
                    ScrapValue = sv.ToString("N2", new CultureInfo("en-ZA")),
                    ScrappingCost = scvc.ToString("N2", new CultureInfo("en-ZA")),
                    NewScrapValue = nsv.ToString("N2", new CultureInfo("en-ZA")),
                    TotalCost = loco.TotalCost ?? "0.00", 
                    LeaseTerm = loco.LeaseTerm,
                    LeaseIncome = loco.LeaseIncome ?? "0.00", 
                    EscalationRate = loco.EscalationRate ?? "0.00",
                    UseAfterRefurbish = loco.UseAfterRefurbish,
                    ResidualValue = rsv.ToString("N2", new CultureInfo("en-ZA")),
                    PostTax = loco.PostTax ?? "0.00",
                    WearTearPeriod = loco.WearTearPeriod,
                    OperatingCosts = loco.OperatingCosts ?? "0.00",
                    OperatingCostsEscalation = loco.OperatingCostsEscalation ?? "0.00",
                    CorporateTaxRate = loco.CorporateTaxRate ?? "0.00",
                    PreTax = loco.PreTax ?? "0.00",
                    DateSaved = DateTime.Now.ToString("yyyy-MM-dd"),
                    SavedBy = userName ?? "N/A",
                };

                _context.LocoInputs.Add(inputEntry);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Wagon input inserted successfully." });

            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Insert failed.", detail = ex.Message });
            }
        }

        [HttpPost("calNewScrapVal")]
        public IActionResult CalNewScrapVal([FromBody] ScrapCalRequest req)
        {
            decimal sv;
            decimal sc;
            decimal nsv;

            string newScrapValue;

            if (!string.IsNullOrWhiteSpace(req.ScrapValue) && !string.IsNullOrWhiteSpace(req.ScrappingCost))
            {
                sv = ParseDecimalSafe(req.ScrapValue);
                sc = ParseDecimalSafe(req.ScrappingCost);

                nsv = sv + sc;

                newScrapValue = nsv.ToString("N2", new CultureInfo("en-ZA"));
            }
            else
            {
                newScrapValue = "0.00";
            }

                return Ok(new { newScrapValue });
        }

        [HttpGet("getInputWagons")]
        public async Task<IActionResult> GetInputWagons()
        {
            _context.Database.SetCommandTimeout(180);

            var wagonInput = await _context.WagonInputs
                .Select(w => new
                {
                    w.WagonNumber
                })
                .ToListAsync();

            return Ok(wagonInput);
        }

        [HttpGet("getInputLocos")]
        public async Task<IActionResult> GetInputLocos()
        {
            _context.Database.SetCommandTimeout(180);

            var locoInput = await _context.LocoInputs
                .Select(w => new
                {
                    w.LocoNumber
                })
                .ToListAsync();

            return Ok(locoInput);
        }
         
        [HttpPost("insertUpdateAsset")]
        public async Task<IActionResult> InsertUpdateAsset([FromForm] AssetSet assetSet)
        {
            _context.Database.SetCommandTimeout(200);

            bool exists = await _context.AssetTypeSetups
                .AnyAsync(e => e.AssetType ==  assetSet.AssetType);

            var user = await _context.LeaseCoUsers
               .AsNoTracking()
               .FirstOrDefaultAsync(u => u.UserId == assetSet.UserId);

            if (user == null)
                return BadRequest("User does not exist.");

            string userName = user.UserName;

            if (exists)
            {
                try
                {
                    var asset = await _context.AssetTypeSetups
                        .FirstOrDefaultAsync(a => a.AssetType == assetSet.AssetType);

                    var wagonInput = await _context.WagonInputs
                        .Where(w => w.WagonType == assetSet.AssetType)
                        .ToListAsync();

                    var locoInput = await _context.LocoInputs
                        .Where(w => w.LocoType == assetSet.AssetType)
                        .ToListAsync();

                    if (asset != null)
                    {
                        asset.AssetType = assetSet.AssetType;

                        if (assetSet.LeaseIncome == asset.LeaseIncome)
                        {
                            asset.LeaseIncome = assetSet.LeaseIncome;

                            if (wagonInput.Count != 0)
                            {
                                foreach (var wagon in wagonInput)
                                {
                                    wagon.LeaseIncome = assetSet.LeaseIncome;
                                }
                            }
                            else if (locoInput.Count != 0)
                            {
                                foreach (var loco in locoInput)
                                {
                                    loco.LeaseIncome = assetSet.LeaseIncome;
                                }
                            }
                        }
                        else
                        {
                            decimal leaseIncome = ParseDecimalSafe(assetSet.LeaseIncome);
                            asset.LeaseIncome = leaseIncome.ToString("N2", new CultureInfo("en-ZA"));

                            if (wagonInput.Count != 0)
                            {
                                foreach (var wagon in wagonInput)
                                {
                                    wagon.LeaseIncome = leaseIncome.ToString("N2", new CultureInfo("en-ZA")); ;
                                }
                            }
                            else if (locoInput.Count != 0)
                            {
                                foreach (var loco in locoInput)
                                {
                                    loco.LeaseIncome = leaseIncome.ToString("N2", new CultureInfo("en-ZA")); ;
                                }
                            }
                        }

                        asset.DateSaved = DateTime.Now.ToString("yyyy-MM-dd");

                        asset.SavedBy = userName;

                        asset.LeaseTerm = assetSet.LeaseTerm;

                        if (assetSet.EscalationRate == asset.EscalationRate)
                        {
                            asset.EscalationRate = assetSet.EscalationRate;

                            if (wagonInput.Count != 0)
                            {
                                foreach (var wagon in wagonInput)
                                {
                                    wagon.EscalationRate = assetSet.EscalationRate;
                                }
                            }
                            else if (locoInput.Count != 0)
                            {
                                foreach (var loco in locoInput)
                                {
                                    loco.EscalationRate = assetSet.EscalationRate;
                                }
                            }
                        }
                        else
                        {
                            decimal er = ParseDecimalSafe(assetSet.EscalationRate);
                            asset.EscalationRate = er.ToString("N2", new CultureInfo("en-ZA"));

                            if (wagonInput.Count != 0)
                            {
                                foreach (var wagon in wagonInput)
                                {
                                    wagon.EscalationRate = er.ToString("N2", new CultureInfo("en-ZA")); ;
                                }
                            }
                            else if (locoInput.Count != 0)
                            {
                                foreach (var loco in locoInput)
                                {
                                    loco.EscalationRate = er.ToString("N2", new CultureInfo("en-ZA")); ;
                                }
                            }
                        }

                        asset.UseAfterRefurbish = assetSet.UseAfterRefurbish;
                        asset.WearTearPeriod = assetSet.WearTearPeriod;

                        if (assetSet.OperatingCosts == asset.OperatingCosts)
                        {
                            asset.OperatingCosts = assetSet.OperatingCosts;

                            if (wagonInput.Count != 0)
                            {
                                foreach (var wagon in wagonInput)
                                {
                                    wagon.OperatingCosts = assetSet.OperatingCosts;
                                }
                            }
                            else if (locoInput.Count != 0)
                            {
                                foreach (var loco in locoInput)
                                {
                                    loco.OperatingCosts = assetSet.OperatingCosts;
                                }
                            }
                        }
                        else
                        {
                            decimal oc = ParseDecimalSafe(assetSet.OperatingCosts);
                            asset.OperatingCosts = oc.ToString("N2", new CultureInfo("en-ZA"));

                            if (wagonInput.Count != 0)
                            {
                                foreach (var wagon in wagonInput)
                                {
                                    wagon.OperatingCosts = oc.ToString("N2", new CultureInfo("en-ZA")); ;
                                }
                            }
                            else if (locoInput.Count != 0)
                            {
                                foreach (var loco in locoInput)
                                {
                                    loco.OperatingCosts = oc.ToString("N2", new CultureInfo("en-ZA")); ;
                                }
                            }
                        }

                        if (assetSet.OperatingCostsEscalation ==  asset.OperatingCostsEscalation)
                        {
                            asset.OperatingCostsEscalation = assetSet.OperatingCostsEscalation;

                            if (wagonInput.Count != 0)
                            {
                                foreach (var wagon in wagonInput)
                                {
                                    wagon.OperatingCostsEscalation = assetSet.OperatingCostsEscalation;
                                }
                            }
                            else if (locoInput.Count != 0)
                            {
                                foreach (var loco in locoInput)
                                {
                                    loco.OperatingCostsEscalation = assetSet.OperatingCostsEscalation;
                                }
                            }
                        }
                        else
                        {
                            decimal oce = ParseDecimalSafe(assetSet.OperatingCostsEscalation);
                            asset.OperatingCostsEscalation = oce.ToString("N2", new CultureInfo("en-ZA"));

                            if (wagonInput.Count != 0)
                            {
                                foreach (var wagon in wagonInput)
                                {
                                    wagon.OperatingCostsEscalation = oce.ToString("N2", new CultureInfo("en-ZA")); ;
                                }
                            }
                            else if (locoInput.Count != 0)
                            {
                                foreach (var loco in locoInput)
                                {
                                    loco.OperatingCostsEscalation = oce.ToString("N2", new CultureInfo("en-ZA")); ;
                                }
                            }
                        }

                        if (assetSet.CorporateTaxRate == asset.CorporateTaxRate)
                        {
                            asset.CorporateTaxRate = assetSet.CorporateTaxRate;

                            if (wagonInput.Count != 0)
                            {
                                foreach (var wagon in wagonInput)
                                {
                                    wagon.CorporateTaxRate = assetSet.CorporateTaxRate;
                                }
                            }
                            else if (locoInput.Count != 0)
                            {
                                foreach (var loco in locoInput)
                                {
                                    loco.CorporateTaxRate = assetSet.CorporateTaxRate;
                                }
                            }
                        }
                        else
                        {
                            decimal ctr = ParseDecimalSafe(assetSet.CorporateTaxRate);
                            asset.CorporateTaxRate = ctr.ToString("N2", new CultureInfo("en-ZA"));

                            if (wagonInput.Count != 0)
                            {
                                foreach (var wagon in wagonInput)
                                {
                                    wagon.CorporateTaxRate = ctr.ToString("N2", new CultureInfo("en-ZA")); ;
                                }
                            }
                            else if (locoInput.Count != 0)
                            {
                                foreach (var loco in locoInput)
                                {
                                    loco.CorporateTaxRate = ctr.ToString("N2", new CultureInfo("en-ZA")); ;
                                }
                            }
                        }

                        _context.AssetTypeSetups.Update(asset);

                        if (wagonInput.Count != 0)
                        {
                            foreach (var wagon in wagonInput)
                            {
                                wagon.LeaseTerm = assetSet.LeaseTerm;
                                wagon.UseAfterRefurbish = assetSet.UseAfterRefurbish;
                                wagon.WearTearPeriod = assetSet.WearTearPeriod;

                                _context.WagonInputs.Update(wagon);
                            }
                        }
                        else if (locoInput.Count != 0)
                        {
                            foreach (var loco in locoInput)
                            {
                                loco.LeaseTerm = assetSet.LeaseTerm;
                                loco.UseAfterRefurbish = assetSet.UseAfterRefurbish;
                                loco.WearTearPeriod = assetSet.WearTearPeriod;

                                _context.LocoInputs.Update(loco);
                            }
                        }

                        await _context.SaveChangesAsync();

                        return Ok(new { message = "Asset setup updated successfully." });
                    }
                    else
                    {
                        return BadRequest("Asset setup does not exist.");
                    }
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { error = "Update failed.", detail = ex.Message });
                }
            }

            try
            {
                decimal leaseIncome = ParseDecimalSafe(assetSet.LeaseIncome);
                decimal er = ParseDecimalSafe(assetSet.EscalationRate);
                decimal oc = ParseDecimalSafe(assetSet.OperatingCosts);
                decimal oce = ParseDecimalSafe(assetSet.OperatingCostsEscalation);
                decimal ctr = ParseDecimalSafe(assetSet.CorporateTaxRate);

                var assetEntry = new AssetTypeSetup
                {
                    AssetType = assetSet.AssetType,
                    LeaseIncome = leaseIncome.ToString("N2", new CultureInfo("en-ZA")),
                    DateSaved = DateTime.Now.ToString("yyyy-MM-dd"),
                    SavedBy = userName,
                    LeaseTerm = assetSet.LeaseTerm,
                    EscalationRate = er.ToString("N2", new CultureInfo("en-ZA")),
                    UseAfterRefurbish = assetSet.UseAfterRefurbish,
                    WearTearPeriod = assetSet.WearTearPeriod,
                    OperatingCosts = oc.ToString("N2", new CultureInfo("en-ZA")),
                    OperatingCostsEscalation = oce.ToString("N2", new CultureInfo("en-ZA")),
                    CorporateTaxRate = ctr.ToString("N2", new CultureInfo("en-ZA"))
                };

                _context.AssetTypeSetups.Add(assetEntry);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Asset setup inserted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Insert failed.", detail = ex.Message });
            }
        }

        [HttpGet("getInfoAsset/{assetType}")]
        public async Task<IActionResult> GetInfoAsset(string assetType)
        {
            bool exists = await _context.AssetTypeSetups
                .AnyAsync(e => e.AssetType == assetType);

            if (exists)
            {
                var asset = await _context.AssetTypeSetups
                    .AsNoTracking()
                    .FirstOrDefaultAsync(i => i.AssetType == assetType);

                if (asset == null)
                    return BadRequest("Asset setup does not exist.");

                return Ok(new
                {
                    asset.LeaseIncome,
                    asset.LeaseTerm,
                    asset.EscalationRate,
                    asset.UseAfterRefurbish,
                    asset.WearTearPeriod,
                    asset.OperatingCosts,
                    asset.OperatingCostsEscalation,
                    asset.CorporateTaxRate
                });
            }
            else
            {
                return Ok(new
                {
                    LeaseIncome = "",
                    LeaseTerm = 0,
                    EscalationRate = "",
                    UseAfterRefurbish = 0,
                    WearTearPeriod = 0,
                    OperatingCosts = "",
                    OperatingCostsEscalation = "",
                    CorporateTaxRate = ""
                });
            }    
        }

        // ADJUSTED ↓
        [HttpGet("generateDcfWagon/{wagonNumber}")]
        public async Task<IActionResult> GenerateDcfWagon(int wagonNumber)
        {
            var input = await _context.WagonInputs
                .FirstOrDefaultAsync(i => i.WagonNumber == wagonNumber);

            if (input == null)
                return BadRequest("Wagon does not exist.");

            //double scrapCost = Convert.ToDouble(ParseDecimalSafe(input.ScrappingCost));
            double marketValue = Convert.ToDouble(ParseDecimalSafe(input.MarketValue));
            double refurbishCost = Convert.ToDouble(ParseDecimalSafe(input.TotalCost));
            double corporateTax = Convert.ToDouble(ParseDecimalSafe(input.CorporateTaxRate)) / 100;
            int leaseTerm = Convert.ToInt32(input.LeaseTerm);
            double leaseIncome = Convert.ToDouble(ParseDecimalSafe(input.LeaseIncome));
            double escalationRate = Convert.ToDouble(ParseDecimalSafe(input.EscalationRate)) / 100;
            int wearTear = Convert.ToInt32(input.WearTearPeriod);
            double operatingCosts = Convert.ToDouble(ParseDecimalSafe(input.OperatingCosts));
            double operatingEscalation = Convert.ToDouble(ParseDecimalSafe(input.OperatingCostsEscalation)) / 100;
            double residualValue = Convert.ToDouble(ParseDecimalSafe(input.ResidualValue));
            double waccPre = Convert.ToDouble(ParseDecimalSafe(input.PreTax)) / 100;
            double waccPost = Convert.ToDouble(ParseDecimalSafe(input.PostTax)) / 100;
            double netBook = Convert.ToDouble(ParseDecimalSafe(input.NetBookValue));

            double[] B = new double[21];
            double[] D = new double[21];
            double[] E = new double[21];
            double[] G = new double[21];
            double[] H = new double[21];
            double[] I = new double[21];
            double[] J = new double[21];
            double[] K = new double[21];
            double[] L = new double[21];
            double[] M = new double[21];
            double[] N = new double[21];

            // YEAR 0
            double totalMarketValue = marketValue;
            double J2 = (totalMarketValue + refurbishCost) * -1;
            double N2 = (totalMarketValue + refurbishCost) * -1;

            int maxPeriods = 20;
            int minTerm = Math.Min(leaseTerm, wearTear);

            // YEAR 1
            double B3 = (1 <= leaseTerm)
                ? leaseIncome * Math.Pow(1 + escalationRate, 1 - 1)
                : 0;
      
            double D3 = (1 <= minTerm)
                ? refurbishCost / minTerm
                : 0;

            double E3 = (1 <= leaseTerm)
                ? operatingCosts
                : 0;

            double G3 = (1 == leaseTerm)
                ? residualValue
                : 0;

            double H3 = B3 - D3 - E3 + G3;

            double I3 = 1 / Math.Pow(1 + waccPre, 1);

            double J3 = H3 * I3;

            double K3 = H3 * corporateTax;

            double L3 = H3 - K3;

            double M3 = 1 / Math.Pow(1 + waccPost, 1);

            double N3 = L3 * M3;

            double JTotal = 0;
            double NTotal = 0;

            // YEAR 2 - 20
            for (int t = 2; t <= maxPeriods; t++)
            {
                // B column (lease income)
                B[t] = (t <= leaseTerm)
                    ? leaseIncome * Math.Pow(1 + escalationRate, t - 1)
                    : 0;

                // D column (refurb allocation)
                D[t] = (t <= minTerm)
                    ? refurbishCost / minTerm
                    : 0;

                // E column (operating cost)
                E[t] = (t <= leaseTerm)
                    ? operatingCosts * Math.Pow(1 + operatingEscalation, t - 1)
                    : 0;

                // G column (residual value at end of lease only)
                G[t] = (t == leaseTerm)
                    ? residualValue
                    : 0;

                // H column (cashflow before tax)
                H[t] = B[t] - D[t] - E[t] + G[t];

                // PRE-TAX
                I[t] = 1 / Math.Pow(1 + waccPre, t);
                J[t] = H[t] * I[t];

                // POST-TAX
                K[t] = H[t] * corporateTax;
                L[t] = H[t] - K[t];

                M[t] = 1 / Math.Pow(1 + waccPost, t);
                N[t] = H[t] * M[t];

                JTotal += J[t];
                NTotal += N[t];
            }

            // 22ND ROW
            double J23 = J2 + J3 + JTotal;
            double N23 = N2 + N3 + NTotal;

            double O23 = (J23 >= 0) ? netBook + refurbishCost : 0;
            double P23 = (N23 >= 0) ? netBook + refurbishCost : 0;

            // 23RD ROW
            string stat1 = (J23 >= 0) ? "REFURBISH" : "SCRAP";
            string stat2 = (N23 >= 0) ? "REFURBISH" : "SCRAP";

            using (var workbook = new XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("DCF_Wagon");

                //HEADER ROW
                ws.Cell("A1").Value = "Year";
                ws.Cell("B1").Value = "Lease Revenue";
                ws.Cell("C1").Value = "Market Value";
                ws.Cell("D1").Value = "Wear & Tear";
                ws.Cell("E1").Value = "Inspection Cost";
                ws.Cell("F1").Value = "Return to Service Cost";
                ws.Cell("G1").Value = "Residual Value";
                ws.Cell("H1").Value = "Net Cash Flow";
                ws.Cell("I1").Value = "WACC (Pre-Tax)";
                ws.Cell("J1").Value = "Present Value (Pre-Tax)";
                ws.Cell("K1").Value = "Tax";
                ws.Cell("L1").Value = "EBITDA";
                ws.Cell("M1").Value = "WACC (Leveraged)";
                ws.Cell("N1").Value = "Present Value (Post-Tax)";
                ws.Cell("O1").Value = "Transnet Net Book Value (Pre-Tax)";
                ws.Cell("P1").Value = "Transnet Net Book Value (Post-Tax)";
                ws.Range("A1:P1").Style.Font.Bold = true;
                ws.Range("A1:P1").Style.Fill.BackgroundColor = XLColor.LightGray;

                // YEAR 0
                ws.Cell("A2").Value = 0;
                ws.Cell("A2").Style.NumberFormat.Format = "0";
                ws.Cell("B2").Value = "-";
                ws.Cell("C2").Value = totalMarketValue;
                ws.Cell("C2").Style.NumberFormat.Format = "#,##0.00";
                ws.Cell("D2").Value = "-";
                ws.Cell("E2").Value = "-";
                ws.Cell("F2").Value = refurbishCost;
                ws.Cell("F2").Style.NumberFormat.Format = "#,##0.00";
                ws.Cell("G2").Value = "-";
                ws.Cell("H2").Value = "-";
                ws.Cell("I2").Value = -1;
                ws.Cell("I2").Style.NumberFormat.Format = "0";
                ws.Cell("J2").Value = J2;
                ws.Cell("J2").Style.NumberFormat.Format = "#,##0.00";
                ws.Cell("K2").Value = "-";
                ws.Cell("L2").Value = "-";
                ws.Cell("M2").Value = -1;
                ws.Cell("M2").Style.NumberFormat.Format = "0";
                ws.Cell("N2").Value = N2;
                ws.Cell("N2").Style.NumberFormat.Format = "#,##0.00";
                ws.Cell("O2").Value = "-";
                ws.Cell("P2").Value = "-";

                // YEAR 1
                ws.Cell("A3").Value = 1;
                ws.Cell("A3").Style.NumberFormat.Format = "0";
                ws.Cell("B3").Value = B3;
                ws.Cell("B3").Style.NumberFormat.Format = "#,##0.00";
                ws.Cell("C3").Value = "-";
                ws.Cell("D3").Value = D3;
                ws.Cell("D3").Style.NumberFormat.Format = "#,##0.00";
                ws.Cell("E3").Value = E3;
                ws.Cell("E3").Style.NumberFormat.Format = "#,##0.00";
                ws.Cell("F3").Value = "-";
                ws.Cell("G3").Value = G3;
                ws.Cell("G3").Style.NumberFormat.Format = "#,##0.00";
                ws.Cell("H3").Value = H3;
                ws.Cell("H3").Style.NumberFormat.Format = "#,##0.00";
                ws.Cell("I3").Value = I3;
                ws.Cell("J3").Value = J3;
                ws.Cell("J3").Style.NumberFormat.Format = "#,##0.00";
                ws.Cell("K3").Value = K3;
                ws.Cell("K3").Style.NumberFormat.Format = "#,##0.00";
                ws.Cell("L3").Value = L3;
                ws.Cell("L3").Style.NumberFormat.Format = "#,##0.00";
                ws.Cell("M3").Value = M3;
                ws.Cell("N3").Value = N3;
                ws.Cell("N3").Style.NumberFormat.Format = "#,##0.00";
                ws.Cell("O3").Value = "-";
                ws.Cell("P3").Value = "-";

                int startRow = 3;
                int totalYears = 20;

                // YEAR 2 - 20
                for (int year = 2; year <= totalYears; year++)
                {
                    int row = startRow + (year - 1);

                    ws.Cell(row, 1).Value = year;
                    ws.Cell(row, 1).Style.NumberFormat.Format = "0";

                    ws.Cell(row, 2).Value = Math.Round(B[year], MidpointRounding.AwayFromZero);
                    ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00";

                    ws.Cell(row, 3).Value = "-";

                    ws.Cell(row, 4).Value = Math.Round(D[year], MidpointRounding.AwayFromZero);
                    ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";

                    ws.Cell(row, 5).Value = Math.Round(E[year], MidpointRounding.AwayFromZero);
                    ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00";

                    ws.Cell(row, 6).Value = "-";

                    ws.Cell(row, 7).Value = Math.Round(G[year], MidpointRounding.AwayFromZero);
                    ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00";

                    ws.Cell(row, 8).Value = Math.Round(H[year], MidpointRounding.AwayFromZero);
                    ws.Cell(row, 8).Style.NumberFormat.Format = "#,##0.00";

                    ws.Cell(row, 9).Value = I[year];

                    ws.Cell(row, 10).Value = Math.Round(J[year], MidpointRounding.AwayFromZero);
                    ws.Cell(row, 10).Style.NumberFormat.Format = "#,##0.00";

                    ws.Cell(row, 11).Value = Math.Round(K[year], MidpointRounding.AwayFromZero);
                    ws.Cell(row, 11).Style.NumberFormat.Format = "#,##0.00";

                    ws.Cell(row, 12).Value = Math.Round(L[year], MidpointRounding.AwayFromZero);

                    ws.Cell(row, 13).Value = M[year];

                    ws.Cell(row, 14).Value = Math.Round(N[year], MidpointRounding.AwayFromZero);
                    ws.Cell(row, 14).Style.NumberFormat.Format = "#,##0.00";

                    ws.Cell(row, 15).Value = "-";
                    ws.Cell(row, 16).Value = "-";
                }

                //22ND ROW
                ws.Cell("A23").Value = "-";
                ws.Cell("B23").Value = "-";
                ws.Cell("C23").Value = "-";
                ws.Cell("D23").Value = "-";
                ws.Cell("E23").Value = "-";
                ws.Cell("F23").Value = "-";
                ws.Cell("G23").Value = "-";
                ws.Cell("H23").Value = "-";
                ws.Cell("I23").Value = "-";
                ws.Cell("J23").Value = J23;
                ws.Cell("J23").Style.NumberFormat.Format = "#,##0.00";
                ws.Cell("J23").Style.Font.Bold = true;
                ws.Cell("J23").Style.Fill.BackgroundColor = XLColor.LightGray;
                ws.Cell("K23").Value = "-";
                ws.Cell("L23").Value = "-";
                ws.Cell("M23").Value = "-";
                ws.Cell("N23").Value = N23;
                ws.Cell("N23").Style.NumberFormat.Format = "#,##0.00";
                ws.Cell("N23").Style.Font.Bold = true;
                ws.Cell("N23").Style.Fill.BackgroundColor = XLColor.LightGray;
                ws.Cell("O23").Value = O23;
                ws.Cell("O23").Style.NumberFormat.Format = "#,##0.00";
                ws.Cell("O23").Style.Font.Bold = true;
                ws.Cell("O23").Style.Fill.BackgroundColor = XLColor.LightGray;
                ws.Cell("P23").Value = P23;
                ws.Cell("P23").Style.NumberFormat.Format = "#,##0.00";
                ws.Cell("P23").Style.Font.Bold = true;
                ws.Cell("P23").Style.Fill.BackgroundColor = XLColor.LightGray;

                //23RD ROW
                ws.Cell("A24").Value = "-";
                ws.Cell("B24").Value = "-";
                ws.Cell("C24").Value = "-";
                ws.Cell("D24").Value = "-";
                ws.Cell("E24").Value = "-";
                ws.Cell("F24").Value = "-";
                ws.Cell("G24").Value = "-";
                ws.Cell("H24").Value = "-";
                ws.Cell("I24").Value = "-";
                ws.Cell("J24").Value = stat1;
                ws.Cell("J24").Style.Font.Bold = true;

                if (stat1 == "REFURBISH")
                {
                    ws.Cell("J24").Style.Fill.BackgroundColor = XLColor.Green;
                }
                else
                {
                    ws.Cell("J24").Style.Fill.BackgroundColor = XLColor.Red;
                }

                ws.Cell("K24").Value = "-";
                ws.Cell("L24").Value = "-";
                ws.Cell("M24").Value = "-";
                ws.Cell("N24").Value = stat2;
                ws.Cell("N24").Style.Font.Bold = true;

                if (stat2 == "REFURBISH")
                {
                    ws.Cell("N24").Style.Fill.BackgroundColor = XLColor.Green;
                }
                else
                {
                    ws.Cell("N24").Style.Fill.BackgroundColor = XLColor.Red;
                }

                ws.Cell("O24").Value = "-";
                ws.Cell("P24").Value = "-";

                ws.Columns().AdjustToContents();

                workbook.CalculateMode = XLCalculateMode.Auto;
                workbook.RecalculateAllFormulas();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();

                    var formulas = new Dictionary<string, string>();

                formulas["C2"] = "Market Value from Dashboard";
                formulas["F2"] = "Return to Service Cost from Dashboard";
                formulas["J2"] = "(C2 + F2) * I2";
                formulas["N2"] = "(C2 + F2) * M2";
                formulas["E3"] = "IF(A3 <= Lease Term; Inspection Cost; 0)";
                formulas["N3"] = "L3 * M3";
                formulas["J23"] = "SUM(J2 : J22)";
                formulas["N23"] = "SUM(N2 : N22)";
                formulas["O23"] = "IF(J23 >= 0; (Transnet Net Book Value + F2); 0)";
                formulas["P23"] = "IF(N23 >= 0; (Transnet Net Book Value + F2); 0)";
                formulas["J24"] = "IF(J23 >= 0; \"Refurbish\"; \"Scrap\")";
                formulas["N24"] = "IF(N23 >= 0; \"Refurbish\"; \"Scrap\")";

                for (int r = 3; r <= 22; r++)
                {
                    formulas[$"B{r}"] = $"IF(A{r} <= Lease Term; Lease Income * (1 + Escalation Rate)^(A{r} - 1) ; 0)";
                    formulas[$"D{r}"] = $"IF(A{r} <= MIN(Lease Term; Wear & Tear Period); F2 / MIN(Lease Term; Wear & Tear Period) ; 0)";
                    formulas[$"G{r}"] = $"IF(A{r} = Lease Term; Residual Value; 0)";
                    formulas[$"H{r}"] = $"B{r} - D{r} - E{r} + G{r}";
                    formulas[$"I{r}"] = $"1 / (1 + WACC (Pre-Tax) from Input)^A{r}";
                    formulas[$"J{r}"] = $"H{r} * I{r}";
                    formulas[$"K{r}"] = $"H{r} * Corporate Tax Rate";
                    formulas[$"L{r}"] = $"H{r} - K{r}";
                    formulas[$"M{r}"] = $"1 / (1 + WACC (Post-Tax) from Input)^A{r}";
                }

                for (int r = 4; r <= 22; r++)
                {
                    formulas[$"E{r}"] = $"IF(A{r} <= Lease Term; Inspection Cost; 0) * (1 + Inspection Cost Escalation)^(A{r} - 1)";
                    formulas[$"N{r}"] = $"H{r} * M{r}";
                }

                return Ok(new
                    {
                        fileName = $"{wagonNumber}_DCF_Report.xlsx",
                        fileBytes = Convert.ToBase64String(content),
                        formulas = formulas
                    });
                }
            }
        }

        // ADJUSTED ↓
        [HttpGet("generateDcfLoco/{locoNumber}")]
        public async Task<IActionResult> GenerateDcfLoco(int locoNumber)
        {
            var input = await _context.LocoInputs
                .FirstOrDefaultAsync(i => i.LocoNumber == locoNumber);

            if (input == null)
                return BadRequest("Locomotive does not exist.");

            //double scrapCost = Convert.ToDouble(ParseDecimalSafe(input.ScrappingCost));
            double marketValue = Convert.ToDouble(ParseDecimalSafe(input.MarketValue));
            double refurbishCost = Convert.ToDouble(ParseDecimalSafe(input.TotalCost));
            double corporateTax = Convert.ToDouble(ParseDecimalSafe(input.CorporateTaxRate)) / 100;

            int leaseTerm = ParseIntSafe(input.LeaseTerm);

            double leaseIncome = Convert.ToDouble(ParseDecimalSafe(input.LeaseIncome));
            double escalationRate = Convert.ToDouble(ParseDecimalSafe(input.EscalationRate)) / 100;

            int wearTear = ParseIntSafe(input.WearTearPeriod);

            double operatingCosts = Convert.ToDouble(ParseDecimalSafe(input.OperatingCosts));
            double operatingEscalation = Convert.ToDouble(ParseDecimalSafe(input.OperatingCostsEscalation)) / 100;

            double residualValue = Convert.ToDouble(ParseDecimalSafe(input.ResidualValue));

            double waccPre = Convert.ToDouble(ParseDecimalSafe(input.PreTax)) / 100;
            double waccPost = Convert.ToDouble(ParseDecimalSafe(input.PostTax)) / 100;

            double netBook = Convert.ToDouble(ParseDecimalSafe(input.NetBookValue));

            double[] B = new double[21];
            double[] D = new double[21];
            double[] E = new double[21];
            double[] G = new double[21];
            double[] H = new double[21];
            double[] I = new double[21];
            double[] J = new double[21];
            double[] K = new double[21];
            double[] M = new double[21];
            double[] N = new double[21];


            // YEAR 0
            double totalMarketValue = marketValue;
            double J2 = (totalMarketValue + refurbishCost) * -1;
            double N2 = -totalMarketValue * 1 * (1 - corporateTax);

            int maxPeriods = 20;
            int minTerm = Math.Min(leaseTerm, wearTear);

            // YEAR 1
            double B3 = (1 <= leaseTerm)
                ? leaseIncome * Math.Pow(1 + escalationRate, 1 - 1)
                : 0;

            double D3 = (1 <= minTerm)
                ? refurbishCost / minTerm
                : 0;

            double E3 = (1 <= leaseTerm)
                ? operatingCosts
                : 0;

            double G3 = (1 == leaseTerm)
                ? residualValue
                : 0;

            double H3 = B3 - D3 - E3 + G3;

            double I3 = 1 / Math.Pow(1 + waccPre, 1);

            double J3 = H3 * I3;

            double K3 = H3 * corporateTax;

            double L3 = H3 - K3;

            double M3 = 1 / Math.Pow(1 + waccPost, 1);

            double N3 = L3 * M3;

            double JTotal = 0;
            double NTotal = 0;

            // YEAR 2 - 20
            for (int t = 2; t <= maxPeriods; t++)
            {
                // B column (lease income)
                B[t] = (t <= leaseTerm)
                    ? leaseIncome * Math.Pow(1 + escalationRate, t - 1)
                    : 0;

                // D column (refurb allocation)
                D[t] = (t <= minTerm)
                    ? refurbishCost / minTerm
                    : 0;

                // E column (operating cost)
                E[t] = (t <= leaseTerm)
                    ? operatingCosts * Math.Pow(1 + operatingEscalation, t - 1)
                    : 0;

                // G column (residual value at end of lease only)
                G[t] = (t == leaseTerm)
                    ? residualValue
                    : 0;

                // H column (cashflow before tax)
                H[t] = B[t] - D[t] - E[t] + G[t];

                // PRE-TAX
                I[t] = 1 / Math.Pow(1 + waccPre, t);
                J[t] = H[t] * I[t];

                // POST-TAX
                K[t] = H[t] * corporateTax;

                M[t] = 1 / Math.Pow(1 + waccPost, t);
                N[t] = H[t] * M[t];

                JTotal += J[t];
                NTotal += N[t];
            }

            // 22ND ROW
            double J23 = J2 + J3 + JTotal;
            double N23 = N2 + N3 + NTotal;

            double O23 = (J23 >= 0) ? netBook + refurbishCost : 0;
            double P23 = (N23 >= 0) ? netBook + refurbishCost : 0;

            // 23RD ROW
            string stat1 = (J23 >= 0) ? "REFURBISH" : "SCRAP";
            string stat2 = (N23 >= 0) ? "REFURBISH" : "SCRAP";

            using (var workbook = new XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("DCF_Loco");

                //HEADER ROW
                ws.Cell("A1").Value = "Year";
                ws.Cell("B1").Value = "Lease Revenue";
                ws.Cell("C1").Value = "Market Value";
                ws.Cell("D1").Value = "Wear & Tear";
                ws.Cell("E1").Value = "Inspection Cost";
                ws.Cell("F1").Value = "Return to Service Cost";
                ws.Cell("G1").Value = "Residual Value";
                ws.Cell("H1").Value = "Net Cash Flow";
                ws.Cell("I1").Value = "WACC (Pre-Tax)";
                ws.Cell("J1").Value = "Present Value (Pre-Tax)";
                ws.Cell("K1").Value = "Tax";
                ws.Cell("L1").Value = "EBITDA";
                ws.Cell("M1").Value = "WACC (Leveraged)";
                ws.Cell("N1").Value = "Present Value (Post-Tax)";
                ws.Cell("O1").Value = "Transnet Net Book Value (Pre-Tax)";
                ws.Cell("P1").Value = "Transnet Net Book Value (Post-Tax)";
                ws.Range("A1:P1").Style.Font.Bold = true;
                ws.Range("A1:P1").Style.Fill.BackgroundColor = XLColor.LightGray;

                //YEAR 0
                ws.Cell("A2").Value = 0;
                ws.Cell("A2").Style.NumberFormat.Format = "0";
                ws.Cell("B2").Value = "-";
                ws.Cell("C2").Value = (Math.Round(totalMarketValue, MidpointRounding.AwayFromZero)); 
                ws.Cell("C2").Style.NumberFormat.Format = "#,##0.00";
                ws.Cell("D2").Value = "-";
                ws.Cell("E2").Value = "-";
                ws.Cell("F2").Value = (Math.Round(refurbishCost, MidpointRounding.AwayFromZero)); 
                ws.Cell("F2").Style.NumberFormat.Format = "#,##0.00";
                ws.Cell("G2").Value = "-";
                ws.Cell("H2").Value = "-";
                ws.Cell("I2").Value = -1;
                ws.Cell("I2").Style.NumberFormat.Format = "0";
                ws.Cell("J2").Value = (Math.Round(J2, MidpointRounding.AwayFromZero)); 
                ws.Cell("J2").Style.NumberFormat.Format = "#,##0.00";
                ws.Cell("K2").Value = "-";
                ws.Cell("L2").Value = "-";
                ws.Cell("M2").Value = 1;
                ws.Cell("M2").Style.NumberFormat.Format = "0";
                ws.Cell("N2").Value = (Math.Round(N2, MidpointRounding.AwayFromZero)); 
                ws.Cell("N2").Style.NumberFormat.Format = "#,##0.00";
                ws.Cell("O2").Value = "-";
                ws.Cell("P2").Value = "-";

                //YEAR 1
                ws.Cell("A3").Value = 1;
                ws.Cell("A3").Style.NumberFormat.Format = "0";
                ws.Cell("B3").Value = (Math.Round(B3, MidpointRounding.AwayFromZero));
                ws.Cell("B3").Style.NumberFormat.Format = "#,##0.00";
                ws.Cell("C3").Value = "-";
                ws.Cell("D3").Value = (Math.Round(D3, MidpointRounding.AwayFromZero));
                ws.Cell("D3").Style.NumberFormat.Format = "#,##0.00";
                ws.Cell("E3").Value = (Math.Round(E3, MidpointRounding.AwayFromZero));
                ws.Cell("E3").Style.NumberFormat.Format = "#,##0.00";
                ws.Cell("F3").Value = "-";
                ws.Cell("G3").Value = (Math.Round(G3, MidpointRounding.AwayFromZero));
                ws.Cell("G3").Style.NumberFormat.Format = "#,##0.00";
                ws.Cell("H3").Value = (Math.Round(H3, MidpointRounding.AwayFromZero));
                ws.Cell("H3").Style.NumberFormat.Format = "#,##0.00";
                ws.Cell("I3").Value = I3;
                ws.Cell("J3").Value = (Math.Round(J3, MidpointRounding.AwayFromZero));
                ws.Cell("J3").Style.NumberFormat.Format = "#,##0.00";
                ws.Cell("K3").Value = (Math.Round(K3, MidpointRounding.AwayFromZero));
                ws.Cell("K3").Style.NumberFormat.Format = "#,##0.00";
                ws.Cell("L3").Value = (Math.Round(L3, MidpointRounding.AwayFromZero));
                ws.Cell("L3").Style.NumberFormat.Format = "#,##0.00";
                ws.Cell("M3").Value = M3;
                ws.Cell("N3").Value = (Math.Round(N3, MidpointRounding.AwayFromZero));
                ws.Cell("N3").Style.NumberFormat.Format = "#,##0.00";
                ws.Cell("O3").Value = "-";
                ws.Cell("P3").Value = "-";

                int startRow = 3;
                int totalYears = 20;

                // YEAR 2 - 20
                for (int year = 2; year <= totalYears; year++)
                {
                    int row = startRow + (year - 1);

                    ws.Cell(row, 1).Value = year;
                    ws.Cell(row, 1).Style.NumberFormat.Format = "0";

                    ws.Cell(row, 2).Value = Math.Round(B[year], MidpointRounding.AwayFromZero);
                    ws.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00";

                    ws.Cell(row, 3).Value = "-";

                    ws.Cell(row, 4).Value = Math.Round(D[year], MidpointRounding.AwayFromZero);
                    ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";

                    ws.Cell(row, 5).Value = Math.Round(E[year], MidpointRounding.AwayFromZero);
                    ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00";

                    ws.Cell(row, 6).Value = "-";

                    ws.Cell(row, 7).Value = Math.Round(G[year], MidpointRounding.AwayFromZero);
                    ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00";

                    ws.Cell(row, 8).Value = Math.Round(H[year], MidpointRounding.AwayFromZero);
                    ws.Cell(row, 8).Style.NumberFormat.Format = "#,##0.00";

                    ws.Cell(row, 9).Value = I[year];

                    ws.Cell(row, 10).Value = Math.Round(J[year], MidpointRounding.AwayFromZero);
                    ws.Cell(row, 10).Style.NumberFormat.Format = "#,##0.00";

                    ws.Cell(row, 11).Value = Math.Round(K[year], MidpointRounding.AwayFromZero);
                    ws.Cell(row, 11).Style.NumberFormat.Format = "#,##0.00";

                    ws.Cell(row, 12).Value = "-";

                    ws.Cell(row, 13).Value = M[year];

                    ws.Cell(row, 14).Value = Math.Round(N[year], MidpointRounding.AwayFromZero);
                    ws.Cell(row, 14).Style.NumberFormat.Format = "#,##0.00";

                    ws.Cell(row, 15).Value = "-";
                    ws.Cell(row, 16).Value = "-";
                }

                //22ND ROW
                ws.Cell("A23").Value = "-";
                ws.Cell("B23").Value = "-";
                ws.Cell("C23").Value = "-";
                ws.Cell("D23").Value = "-";
                ws.Cell("E23").Value = "-";
                ws.Cell("F23").Value = "-";
                ws.Cell("G23").Value = "-";
                ws.Cell("H23").Value = "-";
                ws.Cell("I23").Value = "-";
                ws.Cell("J23").Value = J23;
                ws.Cell("J23").Style.NumberFormat.Format = "#,##0.00";
                ws.Cell("J23").Style.Font.Bold = true;
                ws.Cell("J23").Style.Fill.BackgroundColor = XLColor.LightGray;
                ws.Cell("K23").Value = "-";
                ws.Cell("L23").Value = "-";
                ws.Cell("M23").Value = "-";
                ws.Cell("N23").Value = N23;
                ws.Cell("N23").Style.NumberFormat.Format = "#,##0.00";
                ws.Cell("N23").Style.Font.Bold = true;
                ws.Cell("N23").Style.Fill.BackgroundColor = XLColor.LightGray;
                ws.Cell("O23").Value = O23;
                ws.Cell("O23").Style.NumberFormat.Format = "#,##0.00";
                ws.Cell("O23").Style.Font.Bold = true;
                ws.Cell("O23").Style.Fill.BackgroundColor = XLColor.LightGray;
                ws.Cell("P23").Value = P23;
                ws.Cell("P23").Style.NumberFormat.Format = "#,##0.00";
                ws.Cell("P23").Style.Font.Bold = true;
                ws.Cell("P23").Style.Fill.BackgroundColor = XLColor.LightGray;

                //23RD ROW
                ws.Cell("A24").Value = "-";
                ws.Cell("B24").Value = "-";
                ws.Cell("C24").Value = "-";
                ws.Cell("D24").Value = "-";
                ws.Cell("E24").Value = "-";
                ws.Cell("F24").Value = "-";
                ws.Cell("G24").Value = "-";
                ws.Cell("H24").Value = "-";
                ws.Cell("I24").Value = "-";
                ws.Cell("J24").Value = stat1;
                ws.Cell("J24").Style.Font.Bold = true;

                if (stat1 == "REFURBISH")
                {
                    ws.Cell("J24").Style.Fill.BackgroundColor = XLColor.Green;
                }
                else
                {
                    ws.Cell("J24").Style.Fill.BackgroundColor = XLColor.Red;
                }

                ws.Cell("K24").Value = "-";
                ws.Cell("L24").Value = "-";
                ws.Cell("M24").Value = "-";
                ws.Cell("N24").Value = stat2;
                ws.Cell("N24").Style.Font.Bold = true;

                if (stat2 == "REFURBISH")
                {
                    ws.Cell("N24").Style.Fill.BackgroundColor = XLColor.Green;
                }
                else
                {
                    ws.Cell("N24").Style.Fill.BackgroundColor = XLColor.Red;
                }

                ws.Cell("O24").Value = "-";
                ws.Cell("P24").Value = "-";

                ws.Columns().AdjustToContents();

                workbook.CalculateMode = XLCalculateMode.Auto;
                workbook.RecalculateAllFormulas();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();

                    var formulas = new Dictionary<string, string>();

                    formulas["C2"] = "Market Value form Dashboard";
                    formulas["F2"] = "Return to Service Cost from Dashboard";
                    formulas["J2"] = "(C2 + F2) * I2";
                    formulas["N2"] = "-C2 * M2 * (1 - Corporate Tax)";
                    formulas["E3"] = "IF(A3 <= Lease Term; Inspection Cost; 0)";
                    formulas["L3"] = "H3 - K3";
                    formulas["N3"] = "L3 * M3";
                    formulas["J23"] = "SUM(J2 : J22)";
                    formulas["N23"] = "SUM(N2 : N22)";
                    formulas["O23"] = "IF(J23 >= 0; (Transnet Net Book Value + F2); 0)";
                    formulas["P23"] = "IF(N23 >= 0; (Transnet Net Book Value + F2); 0)";
                    formulas["J24"] = "IF(J23 >= 0; \"Refurbish\"; \"Scrap\")";
                    formulas["N24"] = "IF(N23 >= 0; \"Refurbish\"; \"Scrap\")";

                    for (int r = 3; r <= 22; r++)
                    {
                        formulas[$"B{r}"] = $"IF(A{r} <= Lease Term; Lease Income * (1 + Escalation Rate)^(A{r} - 1) ; 0)";
                        formulas[$"D{r}"] = $"IF(A{r} <= MIN(Lease Term; Wear & Tear Period); F2 / MIN(Lease Term; Wear & Tear Period) ; 0)";
                        formulas[$"G{r}"] = $"IF(A{r} = Lease Term; Residual Value; 0)";
                        formulas[$"H{r}"] = $"B{r} - D{r} - E{r} + G{r}";
                        formulas[$"I{r}"] = $"1 / (1 + WACC (Pre-Tax) from Input)^A{r}";
                        formulas[$"J{r}"] = $"H{r} * I{r}";
                        formulas[$"K{r}"] = $"H{r} * Corporate Tax Rate";
                        //formulas[$"L{r}"] = $"H{r} - K{r}";
                        formulas[$"M{r}"] = $"1 / (1 + WACC (Post-Tax) from Input)^A{r}";
                    }

                    for (int r = 4; r <= 22; r++)
                    {
                        formulas[$"E{r}"] = $"IF(A{r} <= Lease Term; Inspection Cost; 0) * (1 + Inspection Cost Escalation)^(A{r} - 1)";
                        formulas[$"N{r}"] = $"H{r} * M{r}";
                    }

                    return Ok(new
                    {
                        fileName = $"{locoNumber}_DCF_Report.xlsx",
                        fileBytes = Convert.ToBase64String(content),
                        formulas = formulas
                    });
                }
            }
        }

        private static decimal ParseDecimalSafe(object? obj)
        {
            if (obj == null) return 0m;

            // Already numeric
            if (obj is decimal d) return d;
            if (obj is int i) return i;
            if (obj is long l) return l;
            if (obj is double db) return Convert.ToDecimal(db);
            if (obj is float f) return Convert.ToDecimal(f);

            var s = obj.ToString();
            if (string.IsNullOrWhiteSpace(s)) return 0m;

            s = s.Trim();

            // Remove currency symbols and non-numeric noise except separators
            s = Regex.Replace(s, @"[^\d\.,\-]", "");

            // Case 1: Both comma and dot exist
            if (s.Contains(",") && s.Contains("."))
            {
                // Assume comma is thousands separator: 12,345.67
                s = s.Replace(",", "");
            }
            // Case 2: Only comma exists
            else if (s.Contains(",") && !s.Contains("."))
            {
                // Treat comma as decimal separator: 12345,67 → 12345.67
                s = s.Replace(",", ".");
            }

            // Final parse using invariant culture
            if (decimal.TryParse(s, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var result))
                return result;

            return 0m;
        }

        private static int ParseIntSafe(object value)
        {
            if (value == null) return 0;

            return Convert.ToInt32(value);
        }
    }

    // ADDED ↓
    public class DcfYear
    {
        public int Year { get; set; }
        public double LeaseRevenue { get; set; }
        public double WearTear { get; set; }
        public double OperatingCosts { get; set; }
        public double ResidualValue { get; set; }
        public double NetCashFlow { get; set; }
        public double DiscountPre { get; set; }
        public double PresentValuePre { get; set; }
        public double Tax { get; set; }
        public double EBITDA { get; set; }
        public double DiscountPost { get; set; }
        public double PresentValuePost { get; set; }
    }

    public class Setup
    {
        public string? CurrentPost { get; set; }

        public string? CurrentPre { get; set; }

        public string? PostTax { get; set; }

        public string? PreTax {  get; set; }

        public string? UserId { get; set; }
    }

    public class InputWagon
    {
        public int WagonNumber { get; set; }
        public string? WagonType { get; set; }
        public string? NetBookValue { get; set; }
        public string? ScrapValue { get; set; }
        public string? ScrappingCost { get; set; }

        // ADD ↓
        public string? NewScrapValue { get; set; }

        // ADJUST ↓
        public string? TotalCost { get; set; }
        public int LeaseTerm { get; set; }
        public string? LeaseIncome { get; set; }
        public string? EscalationRate { get; set; }
        public int UseAfterRefurbish { get; set; }
        public string? ResidualValue { get; set; }
        public string? PostTax { get; set; }
        public int WearTearPeriod { get; set; }
        public string? OperatingCosts { get; set; }
        public string? OperatingCostsEscalation { get; set; }
        public string? CorporateTaxRate { get; set; }
        public string? PreTax { get; set; }
        public string? UserId { get; set; }
    }

    public class InputLoco
    {
        public int LocoNumber { get; set; }
        public string? LocoType { get; set; }
        public string? NetBookValue { get; set; }
        public string? ScrapValue { get; set; }
        public string? ScrappingCost { get; set; }

        // ADD ↓
        public string? NewScrapValue { get; set; }

        // ADJUST ↓
        public string? TotalCost { get; set; }
        public int LeaseTerm { get; set; }
        public string? LeaseIncome { get; set; }
        public string? EscalationRate { get; set; }
        public int UseAfterRefurbish { get; set; }
        public string? ResidualValue { get; set; }
        public string? PostTax { get; set; }
        public int WearTearPeriod { get; set; }
        public string? OperatingCosts { get; set; }
        public string? OperatingCostsEscalation { get; set; }
        public string? CorporateTaxRate { get; set; }
        public string? PreTax { get; set; }
        public string? UserId { get; set; }
    }

    public class AssetSet
    {
        public string AssetType { get; set; } = null!;

        public string LeaseIncome { get; set; } = null!;

        // ADD ↓
        public int LeaseTerm { get; set; }

        public string EscalationRate { get; set; } = null!;

        public int UseAfterRefurbish { get; set; }

        public int WearTearPeriod { get; set; }

        public string OperatingCosts { get; set; } = null!;

        public string OperatingCostsEscalation { get; set; } = null!;

        public string CorporateTaxRate { get; set; } = null!;

        public string UserId { get; set; } = null!;
    }

    // ADD ENTIRE CLOSS
    public class ScrapCalRequest
    {
        public string? ScrapValue { get; set; }

        public string? ScrappingCost { get; set; } 
    }
}
