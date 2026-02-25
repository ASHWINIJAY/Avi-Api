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
        public async Task<IActionResult> GetInfo(int wagonNumber)
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

            if (exists)
            {

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
                catch (Exception ex)
                {
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

                        if (loco.ScrapValue == input.ScrapValue)
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

        // ADJUSTED ↓
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

        // ADJUSTED ↓
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
                .AnyAsync(e => e.AssetType == assetSet.AssetType);

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

                        if (assetSet.OperatingCostsEscalation == asset.OperatingCostsEscalation)
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

        //[HttpGet("generateDcfWagon/{wagonNumber}")]
        //public async Task<IActionResult> GenerateDcfWagon(int wagonNumber)
        //{
        //    var input = await _context.WagonInputs
        //        .FirstOrDefaultAsync(i => i.WagonNumber == wagonNumber);

        //    if (input == null)
        //        return BadRequest("Wagon does not exist.");

        //    double scrapCost = Convert.ToDouble(ParseDecimalSafe(input.ScrappingCost));
        //    double scrapValue = Convert.ToDouble(ParseDecimalSafe(input.ScrapValue));
        //    double refurbishCost = Convert.ToDouble(ParseDecimalSafe(input.TotalCost));
        //    double corporateTax = Convert.ToDouble(ParseDecimalSafe(input.CorporateTaxRate)) / 100;
        //    int leaseTerm = Convert.ToInt32(input.LeaseTerm);
        //    double leaseIncome = Convert.ToDouble(ParseDecimalSafe(input.LeaseIncome));
        //    double escalationRate = Convert.ToDouble(ParseDecimalSafe(input.EscalationRate)) / 100;
        //    int wearTear = Convert.ToInt32(input.WearTearPeriod);
        //    double operatingCosts = Convert.ToDouble(ParseDecimalSafe(input.OperatingCosts));
        //    double operatingEscalation = Convert.ToDouble(ParseDecimalSafe(input.OperatingCostsEscalation)) / 100;
        //    double residualValue = Convert.ToDouble(ParseDecimalSafe(input.ResidualValue));
        //    double waccPre = Convert.ToDouble(ParseDecimalSafe(input.PreTax)) / 100;
        //    double waccPost = Convert.ToDouble(ParseDecimalSafe(input.PostTax)) / 100;
        //    double netBook = Convert.ToDouble(ParseDecimalSafe(input.NetBookValue));

        //    //1ST ROW

        //    //C2
        //    double totalScrapValue = scrapValue + scrapCost;

        //    //J2
        //    double J2 = (totalScrapValue + refurbishCost) * -1;

        //    //N2
        //    double N2 = (totalScrapValue + refurbishCost) * -1;

        //    //2ND ROW

        //    //B3
        //    double B3 = (1 <= leaseTerm)
        //        ? leaseIncome * Math.Pow(1 + escalationRate, 1 - 1)
        //        : 0;

        //    //D3
        //    int minTerm = Math.Min(leaseTerm, wearTear);

        //    double D3 = (1 <= minTerm)
        //        ? refurbishCost / minTerm
        //        : 0;

        //    //E3
        //    double E3 = (1 <= leaseTerm)
        //        ? operatingCosts
        //        : 0;

        //    //G3
        //    double G3 = (1 == leaseTerm)
        //        ? residualValue
        //        : 0;

        //    //H3
        //    double H3 = B3 - D3 - E3 + G3;

        //    //I3
        //    double I3 = 1 / Math.Pow(1 + waccPre, 1);

        //    //J3
        //    double J3 = H3 * I3;

        //    //K3
        //    double K3 = H3 * corporateTax;

        //    //L3
        //    double L3 = H3 - K3;

        //    //M3
        //    double M3 = 1 / Math.Pow(1 + waccPost, 1);

        //    //N3
        //    double N3 = L3 * M3;

        //    //3RD ROW

        //    //B4
        //    double B4 = (2 <= leaseTerm)
        //        ? leaseIncome * Math.Pow(1 + escalationRate, 2 - 1)
        //        : 0;

        //    //D4
        //    int minTerm2 = Math.Min(leaseTerm, wearTear);

        //    double D4 = (2 <= minTerm2)
        //        ? refurbishCost / minTerm2
        //        : 0;

        //    //E4
        //    double E4 = (2 <= leaseTerm)
        //        ? operatingCosts * Math.Pow(1 + operatingEscalation, 2 - 1)
        //        : 0;

        //    //G4
        //    double G4 = (2 == leaseTerm)
        //        ? residualValue
        //        : 0;

        //    //H4
        //    double H4 = B4 - D4 - E4 + G4;

        //    //I4
        //    double I4 = 1 / Math.Pow(1 + waccPre, 2);

        //    //J4
        //    double J4 = H4 * I4;

        //    //K4
        //    double K4 = H4 * corporateTax;

        //    //L4
        //    double L4 = H4 - K4;

        //    //M4
        //    double M4 = 1 / Math.Pow(1 + waccPost, 2);

        //    //N4
        //    double N4 = H4 * M4;

        //    //4TH ROW

        //    //B5
        //    double B5 = (3 <= leaseTerm)
        //        ? leaseIncome * Math.Pow(1 + escalationRate, 3 - 1)
        //        : 0;

        //    //D5
        //    int minTerm3 = Math.Min(leaseTerm, wearTear);

        //    double D5 = (3 <= minTerm3)
        //        ? refurbishCost / minTerm3
        //        : 0;

        //    //E5
        //    double E5 = (3 <= leaseTerm)
        //        ? operatingCosts * Math.Pow(1 + operatingEscalation, 3 - 1)
        //        : 0;

        //    //G5
        //    double G5 = (3 == leaseTerm)
        //        ? residualValue
        //        : 0;

        //    //H5
        //    double H5 = B5 - D5 - E5 + G5;

        //    //I5
        //    double I5 = 1 / Math.Pow(1 + waccPre, 3);

        //    //J5
        //    double J5 = H5 * I5;

        //    //K5
        //    double K5 = H5 * corporateTax;

        //    //L5
        //    double L5 = H5 - K5;

        //    //M5
        //    double M5 = 1 / Math.Pow(1 + waccPost, 3);

        //    //N5
        //    double N5 = H5 * M5;

        //    //5TH ROW

        //    //B6
        //    double B6 = (4 <= leaseTerm)
        //        ? leaseIncome * Math.Pow(1 + escalationRate, 4 - 1)
        //        : 0;

        //    //D6
        //    int minTerm4 = Math.Min(leaseTerm, wearTear);

        //    double D6 = (4 <= minTerm4)
        //        ? refurbishCost / minTerm4
        //        : 0;

        //    //E6
        //    double E6 = (4 <= leaseTerm)
        //        ? operatingCosts * Math.Pow(1 + operatingEscalation, 4 - 1)
        //        : 0;

        //    //G6
        //    double G6 = (4 == leaseTerm)
        //        ? residualValue
        //        : 0;

        //    //H6
        //    double H6 = B6 - D6 - E6 + G6;

        //    //I6
        //    double I6 = 1 / Math.Pow(1 + waccPre, 4);

        //    //J6
        //    double J6 = H6 * I6;

        //    //K6
        //    double K6 = H6 * corporateTax;

        //    //L6
        //    double L6 = H6 - K6;

        //    //M6
        //    double M6 = 1 / Math.Pow(1 + waccPost, 4);

        //    //N6
        //    double N6 = H6 * M6;

        //    //SIXTH ROW
        //    //B7
        //    double B7 = (5 <= leaseTerm)
        //        ? leaseIncome * Math.Pow(1 + escalationRate, 5 - 1)
        //        : 0;

        //    //D7
        //    int minTerm5 = Math.Min(leaseTerm, wearTear);

        //    double D7 = (5 <= minTerm5)
        //        ? refurbishCost / minTerm5
        //        : 0;

        //    //E7
        //    double E7 = (5 <= leaseTerm)
        //        ? operatingCosts * Math.Pow(1 + operatingEscalation, 5 - 1)
        //        : 0;

        //    //G7
        //    double G7 = (5 == leaseTerm)
        //        ? residualValue
        //        : 0;

        //    //H7
        //    double H7 = B7 - D7 - E7 + G7;

        //    //I7
        //    double I7 = 1 / Math.Pow(1 + waccPre, 5);

        //    //J7
        //    double J7 = H7 * I7;

        //    //K7
        //    double K7 = H7 * corporateTax;

        //    //L7
        //    double L7 = H7 - K7;

        //    //M7
        //    double M7 = 1 / Math.Pow(1 + waccPost, 5);

        //    //N7
        //    double N7 = H7 * M7;

        //    //SEVENTH ROW
        //    //B8
        //    double B8 = (6 <= leaseTerm)
        //        ? leaseIncome * Math.Pow(1 + escalationRate, 6 - 1)
        //        : 0;

        //    //D8
        //    int minTerm6 = Math.Min(leaseTerm, wearTear);

        //    double D8 = (6 <= minTerm6)
        //        ? refurbishCost / minTerm6
        //        : 0;

        //    //E8
        //    double E8 = (6 <= leaseTerm)
        //        ? operatingCosts * Math.Pow(1 + operatingEscalation, 6 - 1)
        //        : 0;

        //    //G8
        //    double G8 = (6 == leaseTerm)
        //        ? residualValue
        //        : 0;

        //    //H8
        //    double H8 = B8 - D8 - E8 + G8;

        //    //I8
        //    double I8 = 1 / Math.Pow(1 + waccPre, 6);

        //    //J8
        //    double J8 = H8 * I8;

        //    //K8
        //    double K8 = H8 * corporateTax;

        //    //L8
        //    double L8 = H8 - K8;

        //    //M8
        //    double M8 = 1 / Math.Pow(1 + waccPost, 6);

        //    //N8
        //    double N8 = H8 * M8;

        //    //EIGHTH ROW
        //    //B9
        //    double B9 = (7 <= leaseTerm)
        //        ? leaseIncome * Math.Pow(1 + escalationRate, 7 - 1)
        //        : 0;

        //    //D9
        //    int minTerm7 = Math.Min(leaseTerm, wearTear);

        //    double D9 = (7 <= minTerm7)
        //        ? refurbishCost / minTerm7
        //        : 0;

        //    //E9
        //    double E9 = (7 <= leaseTerm)
        //        ? operatingCosts * Math.Pow(1 + operatingEscalation, 7 - 1)
        //        : 0;

        //    //G9
        //    double G9 = (7 == leaseTerm)
        //        ? residualValue
        //        : 0;

        //    //H9
        //    double H9 = B9 - D9 - E9 + G9;

        //    //I9
        //    double I9 = 1 / Math.Pow(1 + waccPre, 7);

        //    //J9
        //    double J9 = H9 * I9;

        //    //K9
        //    double K9 = H9 * corporateTax;

        //    //L9
        //    double L9 = H9 - K9;

        //    //M9
        //    double M9 = 1 / Math.Pow(1 + waccPost, 7);

        //    //N9
        //    double N9 = H9 * M9;

        //    //NINETH ROW
        //    //B10
        //    double B10 = (8 <= leaseTerm)
        //        ? leaseIncome * Math.Pow(1 + escalationRate, 8 - 1)
        //        : 0;

        //    //D10
        //    int minTerm8 = Math.Min(leaseTerm, wearTear);

        //    double D10 = (8 <= minTerm8)
        //        ? refurbishCost / minTerm8
        //        : 0;

        //    //E10
        //    double E10 = (8 <= leaseTerm)
        //        ? operatingCosts * Math.Pow(1 + operatingEscalation, 8 - 1)
        //        : 0;

        //    //G10
        //    double G10 = (8 == leaseTerm)
        //        ? residualValue
        //        : 0;

        //    //H10
        //    double H10 = B10 - D10 - E10 + G10;

        //    //I10
        //    double I10 = 1 / Math.Pow(1 + waccPre, 8);

        //    //J10
        //    double J10 = H10 * I10;

        //    //K10
        //    double K10 = H10 * corporateTax;

        //    //L10
        //    double L10 = H10 - K10;

        //    //M10
        //    double M10 = 1 / Math.Pow(1 + waccPost, 8);

        //    //N10
        //    double N10 = H10 * M10;

        //    //TENTH ROW
        //    //B11
        //    double B11 = (9 <= leaseTerm)
        //        ? leaseIncome * Math.Pow(1 + escalationRate, 9 - 1)
        //        : 0;

        //    //D11
        //    int minTerm9 = Math.Min(leaseTerm, wearTear);

        //    double D11 = (9 <= minTerm9)
        //        ? refurbishCost / minTerm9
        //        : 0;

        //    //E11
        //    double E11 = (9 <= leaseTerm)
        //        ? operatingCosts * Math.Pow(1 + operatingEscalation, 9 - 1)
        //        : 0;

        //    //G11
        //    double G11 = (9 == leaseTerm)
        //        ? residualValue
        //        : 0;

        //    //H11
        //    double H11 = B11 - D11 - E11 + G11;

        //    //I11
        //    double I11 = 1 / Math.Pow(1 + waccPre, 9);

        //    //J11
        //    double J11 = H11 * I11;

        //    //K11
        //    double K11 = H11 * corporateTax;

        //    //L11
        //    double L11 = H11 - K11;

        //    //M11
        //    double M11 = 1 / Math.Pow(1 + waccPost, 9);

        //    //N11
        //    double N11 = H11 * M11;

        //    //11TH ROW
        //    //B12
        //    double B12 = (10 <= leaseTerm)
        //        ? leaseIncome * Math.Pow(1 + escalationRate, 10 - 1)
        //        : 0;

        //    //D12
        //    int minTerm10 = Math.Min(leaseTerm, wearTear);

        //    double D12 = (10 <= minTerm10)
        //        ? refurbishCost / minTerm10
        //        : 0;

        //    //E12
        //    double E12 = (10 <= leaseTerm)
        //        ? operatingCosts * Math.Pow(1 + operatingEscalation, 10 - 1)
        //        : 0;

        //    //G12
        //    double G12 = (10 == leaseTerm)
        //        ? residualValue
        //        : 0;

        //    //H12
        //    double H12 = B12 - D12 - E12 + G12;

        //    //I12
        //    double I12 = 1 / Math.Pow(1 + waccPre, 10);

        //    //J12
        //    double J12 = H12 * I12;

        //    //K12
        //    double K12 = H12 * corporateTax;

        //    //L12
        //    double L12 = H12 - K12;

        //    //M12
        //    double M12 = 1 / Math.Pow(1 + waccPost, 10);

        //    //N12
        //    double N12 = H12 * M12;

        //    //12TH ROW
        //    //B13
        //    double B13 = (11 <= leaseTerm)
        //        ? leaseIncome * Math.Pow(1 + escalationRate, 11 - 1)
        //        : 0;

        //    //D13
        //    int minTerm11 = Math.Min(leaseTerm, wearTear);

        //    double D13 = (11 <= minTerm11)
        //        ? refurbishCost / minTerm11
        //        : 0;

        //    //E13
        //    double E13 = (11 <= leaseTerm)
        //        ? operatingCosts * Math.Pow(1 + operatingEscalation, 11 - 1)
        //        : 0;

        //    //G13
        //    double G13 = (11 == leaseTerm)
        //        ? residualValue
        //        : 0;

        //    //H13
        //    double H13 = B13 - D13 - E13 + G13;

        //    //I13
        //    double I13 = 1 / Math.Pow(1 + waccPre, 11);

        //    //J13
        //    double J13 = H13 * I13;

        //    //K13
        //    double K13 = H13 * corporateTax;

        //    //L13
        //    double L13 = H13 - K13;

        //    //M13
        //    double M13 = 1 / Math.Pow(1 + waccPost, 11);

        //    //N13
        //    double N13 = H13 * M13;

        //    //13TH ROW
        //    //B14
        //    double B14 = (12 <= leaseTerm)
        //        ? leaseIncome * Math.Pow(1 + escalationRate, 12 - 1)
        //        : 0;

        //    //D14
        //    int minTerm12 = Math.Min(leaseTerm, wearTear);

        //    double D14 = (12 <= minTerm12)
        //        ? refurbishCost / minTerm12
        //        : 0;

        //    //E14
        //    double E14 = (12 <= leaseTerm)
        //        ? operatingCosts * Math.Pow(1 + operatingEscalation, 12 - 1)
        //        : 0;

        //    //G14
        //    double G14 = (12 == leaseTerm)
        //        ? residualValue
        //        : 0;

        //    //H14
        //    double H14 = B14 - D14 - E14 + G14;

        //    //I14
        //    double I14 = 1 / Math.Pow(1 + waccPre, 12);

        //    //J14
        //    double J14 = H14 * I14;

        //    //K14
        //    double K14 = H14 * corporateTax;

        //    //L14
        //    double L14 = H14 - K14;

        //    //M14
        //    double M14 = 1 / Math.Pow(1 + waccPost, 12);

        //    //N14
        //    double N14 = H14 * M14;

        //    //14TH ROW
        //    //B15
        //    double B15 = (13 <= leaseTerm)
        //        ? leaseIncome * Math.Pow(1 + escalationRate, 13 - 1)
        //        : 0;

        //    //D15
        //    int minTerm13 = Math.Min(leaseTerm, wearTear);

        //    double D15 = (13 <= minTerm13)
        //        ? refurbishCost / minTerm13
        //        : 0;

        //    //E15
        //    double E15 = (13 <= leaseTerm)
        //        ? operatingCosts * Math.Pow(1 + operatingEscalation, 13 - 1)
        //        : 0;

        //    //G15
        //    double G15 = (13 == leaseTerm)
        //        ? residualValue
        //        : 0;

        //    //H15
        //    double H15 = B15 - D15 - E15 + G15;

        //    //I15
        //    double I15 = 1 / Math.Pow(1 + waccPre, 13);

        //    //J15
        //    double J15 = H15 * I15;

        //    //K15
        //    double K15 = H15 * corporateTax;

        //    //L15
        //    double L15 = H15 - K15;

        //    //M15
        //    double M15 = 1 / Math.Pow(1 + waccPost, 13);

        //    //N15
        //    double N15 = H15 * M15;

        //    //15TH ROW
        //    //B16
        //    double B16 = (14 <= leaseTerm)
        //        ? leaseIncome * Math.Pow(1 + escalationRate, 14 - 1)
        //        : 0;

        //    //D16
        //    int minTerm14 = Math.Min(leaseTerm, wearTear);

        //    double D16 = (14 <= minTerm14)
        //        ? refurbishCost / minTerm14
        //        : 0;

        //    //E16
        //    double E16 = (14 <= leaseTerm)
        //        ? operatingCosts * Math.Pow(1 + operatingEscalation, 14 - 1)
        //        : 0;

        //    //G16
        //    double G16 = (14 == leaseTerm)
        //        ? residualValue
        //        : 0;

        //    //H16
        //    double H16 = B16 - D16 - E16 + G16;

        //    //I16
        //    double I16 = 1 / Math.Pow(1 + waccPre, 14);

        //    //J16
        //    double J16 = H16 * I16;

        //    //K16
        //    double K16 = H16 * corporateTax;

        //    //L16
        //    double L16 = H16 - K16;

        //    //M16
        //    double M16 = 1 / Math.Pow(1 + waccPost, 14);

        //    //N16
        //    double N16 = H16 * M16;

        //    //16TH ROW
        //    //B17
        //    double B17 = (15 <= leaseTerm)
        //        ? leaseIncome * Math.Pow(1 + escalationRate, 15 - 1)
        //        : 0;

        //    //D17
        //    int minTerm15 = Math.Min(leaseTerm, wearTear);

        //    double D17 = (15 <= minTerm15)
        //        ? refurbishCost / minTerm15
        //        : 0;

        //    //E17
        //    double E17 = (15 <= leaseTerm)
        //        ? operatingCosts * Math.Pow(1 + operatingEscalation, 15 - 1)
        //        : 0;

        //    //G17
        //    double G17 = (15 == leaseTerm)
        //        ? residualValue
        //        : 0;

        //    //H17
        //    double H17 = B17 - D17 - E17 + G17;

        //    //I17
        //    double I17 = 1 / Math.Pow(1 + waccPre, 15);

        //    //J17
        //    double J17 = H17 * I17;

        //    //K17
        //    double K17 = H17 * corporateTax;

        //    //L17
        //    double L17 = H17 - K17;

        //    //M17
        //    double M17 = 1 / Math.Pow(1 + waccPost, 15);

        //    //N17
        //    double N17 = H17 * M17;

        //    //17TH ROW
        //    //B18
        //    double B18 = (16 <= leaseTerm)
        //        ? leaseIncome * Math.Pow(1 + escalationRate, 16 - 1)
        //        : 0;

        //    //D18
        //    int minTerm16 = Math.Min(leaseTerm, wearTear);

        //    double D18 = (16 <= minTerm16)
        //        ? refurbishCost / minTerm16
        //        : 0;

        //    //E18
        //    double E18 = (16 <= leaseTerm)
        //        ? operatingCosts * Math.Pow(1 + operatingEscalation, 16 - 1)
        //        : 0;

        //    //G18
        //    double G18 = (16 == leaseTerm)
        //        ? residualValue
        //        : 0;

        //    //H18
        //    double H18 = B18 - D18 - E18 + G18;

        //    //I18
        //    double I18 = 1 / Math.Pow(1 + waccPre, 16);

        //    //J18
        //    double J18 = H18 * I18;

        //    //K18
        //    double K18 = H18 * corporateTax;

        //    //L18
        //    double L18 = H18 - K18;

        //    //M18
        //    double M18 = 1 / Math.Pow(1 + waccPost, 16);

        //    //N18
        //    double N18 = H18 * M18;

        //    //18TH ROW
        //    //B19
        //    double B19 = (17 <= leaseTerm)
        //        ? leaseIncome * Math.Pow(1 + escalationRate, 17 - 1)
        //        : 0;

        //    //D19
        //    int minTerm17 = Math.Min(leaseTerm, wearTear);

        //    double D19 = (17 <= minTerm17)
        //        ? refurbishCost / minTerm17
        //        : 0;

        //    //E19
        //    double E19 = (17 <= leaseTerm)
        //        ? operatingCosts * Math.Pow(1 + operatingEscalation, 17 - 1)
        //        : 0;

        //    //G19
        //    double G19 = (17 == leaseTerm)
        //        ? residualValue
        //        : 0;

        //    //H19
        //    double H19 = B19 - D19 - E19 + G19;

        //    //I19
        //    double I19 = 1 / Math.Pow(1 + waccPre, 17);

        //    //J19
        //    double J19 = H19 * I19;

        //    //K19
        //    double K19 = H19 * corporateTax;

        //    //L19
        //    double L19 = H19 - K19;

        //    //M19
        //    double M19 = 1 / Math.Pow(1 + waccPost, 17);

        //    //N19
        //    double N19 = H19 * M19;

        //    //19TH ROW
        //    //B20
        //    double B20 = (18 <= leaseTerm)
        //        ? leaseIncome * Math.Pow(1 + escalationRate, 18 - 1)
        //        : 0;

        //    //D20
        //    int minTerm18 = Math.Min(leaseTerm, wearTear);

        //    double D20 = (18 <= minTerm18)
        //        ? refurbishCost / minTerm18
        //        : 0;

        //    //E20
        //    double E20 = (18 <= leaseTerm)
        //        ? operatingCosts * Math.Pow(1 + operatingEscalation, 18 - 1)
        //        : 0;

        //    //G20
        //    double G20 = (18 == leaseTerm)
        //        ? residualValue
        //        : 0;

        //    //H20
        //    double H20 = B20 - D20 - E20 + G20;

        //    //I20
        //    double I20 = 1 / Math.Pow(1 + waccPre, 18);

        //    //J20
        //    double J20 = H20 * I20;

        //    //K20
        //    double K20 = H20 * corporateTax;

        //    //L20
        //    double L20 = H20 - K20;

        //    //M20
        //    double M20 = 1 / Math.Pow(1 + waccPost, 18);

        //    //N20
        //    double N20 = H20 * M20;

        //    //20TH ROW
        //    //B21
        //    double B21 = (19 <= leaseTerm)
        //        ? leaseIncome * Math.Pow(1 + escalationRate, 19 - 1)
        //        : 0;

        //    //D21
        //    int minTerm19 = Math.Min(leaseTerm, wearTear);

        //    double D21 = (19 <= minTerm19)
        //        ? refurbishCost / minTerm19
        //        : 0;

        //    //E21
        //    double E21 = (19 <= leaseTerm)
        //        ? operatingCosts * Math.Pow(1 + operatingEscalation, 19 - 1)
        //        : 0;

        //    //G21
        //    double G21 = (19 == leaseTerm)
        //        ? residualValue
        //        : 0;

        //    //H21
        //    double H21 = B21 - D21 - E21 + G21;

        //    //I21
        //    double I21 = 1 / Math.Pow(1 + waccPre, 19);

        //    //J21
        //    double J21 = H21 * I21;

        //    //K21
        //    double K21 = H21 * corporateTax;

        //    //L21
        //    double L21 = H21 - K21;

        //    //M21
        //    double M21 = 1 / Math.Pow(1 + waccPost, 19);

        //    //N21
        //    double N21 = H21 * M21;

        //    //21ST ROW
        //    //B22
        //    double B22 = (20 <= leaseTerm)
        //        ? leaseIncome * Math.Pow(1 + escalationRate, 20 - 1)
        //        : 0;

        //    //D22
        //    int minTerm20 = Math.Min(leaseTerm, wearTear);

        //    double D22 = (20 <= minTerm20)
        //        ? refurbishCost / minTerm20
        //        : 0;

        //    //E22
        //    double E22 = (20 <= leaseTerm)
        //        ? operatingCosts * Math.Pow(1 + operatingEscalation, 20 - 1)
        //        : 0;

        //    //G22
        //    double G22 = (20 == leaseTerm)
        //        ? residualValue
        //        : 0;

        //    //H22
        //    double H22 = B22 - D22 - E22 + G22;

        //    //I22
        //    double I22 = 1 / Math.Pow(1 + waccPre, 20);

        //    //J22
        //    double J22 = H22 * I22;

        //    //K22
        //    double K22 = H22 * corporateTax;

        //    //L22
        //    double L22 = H22 - K22;

        //    //M22
        //    double M22 = 1 / Math.Pow(1 + waccPost, 20);

        //    //N22
        //    double N22 = H22 * M22;

        //    //22ND ROW
        //    double J23 = J2 + J3 + J4 + J5 + J6 + J7 + J8 + J9 + J10 + J11 + J12 + J13 + J14 + J15 + J16 + J17 + J18 + J19 + J20 + +J21 + J22;

        //    double N23 = N2 + N3 + N4 + N5 + N6 + N7 + N8 + N9 + N10 + N11 + N12 + N13 + N14 + N15 + N16 + N17 + N18 + N19 + N20 + +N21 + N22;

        //    double O23 = (J23 >= 0)
        //        ? netBook + refurbishCost
        //        : 0;

        //    double P23 = (N23 >= 0)
        //        ? netBook + refurbishCost
        //        : 0;

        //    //23RD ROW
        //    string stat1;
        //    string stat2;

        //    if (J23 >= 0)
        //    {
        //        stat1 = "REFURBISH";
        //    }
        //    else
        //    {
        //        stat1 = "SCRAP";
        //    }

        //    if (N23 >= 0)
        //    {
        //        stat2 = "REFURBISH";
        //    }
        //    else
        //    {
        //        stat2 = "SCRAP";
        //    }

        //    using (var workbook = new XLWorkbook())
        //    {
        //        var ws = workbook.Worksheets.Add("DCF_Wagon");

        //        //HEADER ROW
        //        ws.Cell("A1").Value = "Year";
        //        ws.Cell("B1").Value = "Lease Revenue";
        //        ws.Cell("C1").Value = "Scrap Value";
        //        ws.Cell("D1").Value = "Wear & Tear";
        //        ws.Cell("E1").Value = "Operating Costs";
        //        ws.Cell("F1").Value = "Refurbishment Cost";
        //        ws.Cell("G1").Value = "Residual Value";
        //        ws.Cell("H1").Value = "Net Cash Flow";
        //        ws.Cell("I1").Value = "WACC (Pre-Tax)";
        //        ws.Cell("J1").Value = "Present Value (Pre-Tax)";
        //        ws.Cell("K1").Value = "Tax";
        //        ws.Cell("L1").Value = "EBITDA";
        //        ws.Cell("M1").Value = "WACC (Leveraged)";
        //        ws.Cell("N1").Value = "Present Value (Post-Tax)";
        //        ws.Cell("O1").Value = "Transfer Value (Pre-Tax)";
        //        ws.Cell("P1").Value = "Transfer Value (Post-Tax)";
        //        ws.Range("A1:P1").Style.Font.Bold = true;
        //        ws.Range("A1:P1").Style.Fill.BackgroundColor = XLColor.LightGray;

        //        //FIRST ROW
        //        ws.Cell("A2").Value = 0;
        //        ws.Cell("A2").Style.NumberFormat.Format = "0";
        //        ws.Cell("B2").Value = "-";
        //        ws.Cell("C2").Value = totalScrapValue;
        //        ws.Cell("C2").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("D2").Value = "-";
        //        ws.Cell("E2").Value = "-";
        //        ws.Cell("F2").Value = refurbishCost;
        //        ws.Cell("F2").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("G2").Value = "-";
        //        ws.Cell("H2").Value = "-";
        //        ws.Cell("I2").Value = -1;
        //        ws.Cell("I2").Style.NumberFormat.Format = "0";
        //        ws.Cell("J2").Value = J2;
        //        ws.Cell("J2").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("K2").Value = "-";
        //        ws.Cell("L2").Value = "-";
        //        ws.Cell("M2").Value = -1;
        //        ws.Cell("M2").Style.NumberFormat.Format = "0";
        //        ws.Cell("N2").Value = N2;
        //        ws.Cell("N2").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("O2").Value = "-";
        //        ws.Cell("P2").Value = "-";

        //        //SECOND ROW
        //        ws.Cell("A3").Value = 1;
        //        ws.Cell("A3").Style.NumberFormat.Format = "0";
        //        //= IF(A3 <= Inputs!$B$10; Inputs!$B$11 * (1 + Inputs!$B$12)^(A3 - 1); 0)
        //        ws.Cell("B3").Value = B3;
        //        ws.Cell("B3").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("C3").Value = "-";
        //        //= IF(A3 <= MIN(Inputs!$B$10; Inputs!$B$17); Inputs!$B$9 / MIN(Inputs!$B$10; Inputs!$B$17); 0)
        //        ws.Cell("D3").Value = D3;
        //        ws.Cell("D3").Style.NumberFormat.Format = "#,##0.00";
        //        //= IF(A3 <= Inputs!$B$10; Inputs!$B$18; 0)
        //        ws.Cell("E3").Value = E3;
        //        ws.Cell("E3").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("F3").Value = "-";
        //        //= IF(A3 = Inputs!$B$10; Inputs!$B$14; 0)
        //        ws.Cell("G3").Value = G3;
        //        ws.Cell("G3").Style.NumberFormat.Format = "#,##0.00";
        //        //= B3 - D3 - E3 + G3
        //        ws.Cell("H3").Value = H3;
        //        ws.Cell("H3").Style.NumberFormat.Format = "#,##0.00";
        //        //= 1 / (1 + Inputs!$B$21)^A3
        //        ws.Cell("I3").Value = I3;
        //        //= H3 * J3
        //        ws.Cell("J3").Value = J3;
        //        ws.Cell("J3").Style.NumberFormat.Format = "#,##0.00";
        //        //= H3 * Inputs!$B$20
        //        ws.Cell("K3").Value = K3;
        //        ws.Cell("K3").Style.NumberFormat.Format = "#,##0.00";
        //        //= H3 - M3
        //        ws.Cell("L3").Value = L3;
        //        ws.Cell("L3").Style.NumberFormat.Format = "#,##0.00";
        //        //= 1 / (1 + Inputs!$B$16)^A3
        //        ws.Cell("M3").Value = M3;
        //        //= N3 * O3
        //        ws.Cell("N3").Value = N3;
        //        ws.Cell("N3").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("O3").Value = "-";
        //        ws.Cell("P3").Value = "-";

        //        //THIRD ROW
        //        ws.Cell("A4").Value = 2;
        //        ws.Cell("A4").Style.NumberFormat.Format = "0";
        //        ////=IF(A4<=Inputs!$B$10;Inputs!$B$11*(1+Inputs!$B$12)^(A4-1);0)
        //        ws.Cell("B4").Value = B4;
        //        ws.Cell("B4").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("C4").Value = "-";
        //        ////= IF(A4 <= MIN(Inputs!$B$10; Inputs!$B$17); Inputs!$B$9 / MIN(Inputs!$B$10; Inputs!$B$17); 0)
        //        ws.Cell("D4").Value = D4;
        //        ws.Cell("D4").Style.NumberFormat.Format = "#,##0.00";
        //        ////=IF(A4<=Inputs!$B$10;Inputs!$B$18;0)*(1+Inputs!$B$19)^(A4-1)
        //        ws.Cell("E4").Value = E4;
        //        ws.Cell("E4").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("F4").Value = "-";
        //        ////= IF(A4 = Inputs!$B$10; Inputs!$B$14; 0)
        //        ws.Cell("G4").Value = G4;
        //        ws.Cell("G4").Style.NumberFormat.Format = "#,##0.00";
        //        ////= B4 - D4 - E4 + G4
        //        ws.Cell("H4").Value = H4;
        //        ws.Cell("H4").Style.NumberFormat.Format = "#,##0.00";
        //        ////= 1 / (1 + Inputs!$B$21)^A4
        //        ws.Cell("I4").Value = I4;
        //        ////= H4 * J4
        //        ws.Cell("J4").Value = J4;
        //        ws.Cell("J4").Style.NumberFormat.Format = "#,##0.00";
        //        ////= H4 * Inputs!$B$20
        //        ws.Cell("K4").Value = K4;
        //        ws.Cell("K4").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("L4").Value = L4;
        //        ws.Cell("L4").Style.NumberFormat.Format = "#,##0.00";
        //        ////= 1 / (1 + Inputs!$B$16)^A4
        //        ws.Cell("M4").Value = M4;
        //        ////= H4 * O4
        //        ws.Cell("N4").Value = N4;
        //        ws.Cell("N4").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("O4").Value = "-";
        //        ws.Cell("P4").Value = "-";

        //        //FOURTH ROW
        //        ws.Cell("A5").Value = 3;
        //        ws.Cell("A5").Style.NumberFormat.Format = "0";
        //        ////=IF(A5<=Inputs!$B$10;Inputs!$B$11*(1+Inputs!$B$12)^(A5-1);0)
        //        ws.Cell("B5").Value = B5;
        //        ws.Cell("B5").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("C5").Value = "-";
        //        ////= IF(A5 <= MIN(Inputs!$B$10; Inputs!$B$17); Inputs!$B$9 / MIN(Inputs!$B$10; Inputs!$B$17); 0)
        //        ws.Cell("D5").Value = D5;
        //        ws.Cell("D5").Style.NumberFormat.Format = "#,##0.00";
        //        ////=IF(A5<=Inputs!$B$10;Inputs!$B$18;0)*(1+Inputs!$B$19)^(A5-1)
        //        ws.Cell("E5").Value = E5;
        //        ws.Cell("E5").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("F5").Value = "-";
        //        ////= IF(A5 = Inputs!$B$10; Inputs!$B$15; 0)
        //        ws.Cell("G5").Value = G5;
        //        ws.Cell("G5").Style.NumberFormat.Format = "#,##0.00";
        //        ////= B5 - D5 - E5 + G5
        //        ws.Cell("H5").Value = H5;
        //        ws.Cell("H5").Style.NumberFormat.Format = "#,##0.00";
        //        ////= 1 / (1 + Inputs!$B$21)^A5
        //        ws.Cell("I5").Value = I5;
        //        ////= H5 * J5
        //        ws.Cell("J5").Value = J5;
        //        ws.Cell("J5").Style.NumberFormat.Format = "#,##0.00";
        //        ////= H5 * Inputs!$B$20
        //        ws.Cell("K5").Value = K5;
        //        ws.Cell("K5").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("L5").Value = L5;
        //        ws.Cell("L5").Style.NumberFormat.Format = "#,##0.00";
        //        ////= 1 / (1 + Inputs!$B$16)^A5
        //        ws.Cell("M5").Value = M5;
        //        ////= H5 * O5
        //        ws.Cell("N5").Value = N5;
        //        ws.Cell("N5").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("O5").Value = "-";
        //        ws.Cell("P5").Value = "-";

        //        //FIFTH ROW
        //        ws.Cell("A6").Value = 4;
        //        ws.Cell("A6").Style.NumberFormat.Format = "0";
        //        ////=IF(A6<=Inputs!$B$10;Inputs!$B$11*(1+Inputs!$B$12)^(A6-1);0)
        //        ws.Cell("B6").Value = B6;
        //        ws.Cell("B6").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("C6").Value = "-";
        //        ////= IF(A6 <= MIN(Inputs!$B$10; Inputs!$B$17); Inputs!$B$9 / MIN(Inputs!$B$10; Inputs!$B$17); 0)
        //        ws.Cell("D6").Value = D6;
        //        ws.Cell("D6").Style.NumberFormat.Format = "#,##0.00";
        //        ////=IF(A6<=Inputs!$B$10;Inputs!$B$18;0)*(1+Inputs!$B$19)^(A6-1)
        //        ws.Cell("E6").Value = E6;
        //        ws.Cell("E6").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("F6").Value = "-";
        //        ////= IF(A6 = Inputs!$B$10; Inputs!$B$16; 0)
        //        ws.Cell("G6").Value = G6;
        //        ws.Cell("G6").Style.NumberFormat.Format = "#,##0.00";
        //        ////= B6 - D6 - E6 + G6
        //        ws.Cell("H6").Value = H6;
        //        ws.Cell("H6").Style.NumberFormat.Format = "#,##0.00";
        //        ////= 1 / (1 + Inputs!$B$21)^A6
        //        ws.Cell("I6").Value = I6;
        //        ////= H6 * J6
        //        ws.Cell("J6").Value = J6;
        //        ws.Cell("J6").Style.NumberFormat.Format = "#,##0.00";
        //        ////= H6 * Inputs!$B$20
        //        ws.Cell("K6").Value = K6;
        //        ws.Cell("K6").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("L6").Value = L6;
        //        ws.Cell("L6").Style.NumberFormat.Format = "#,##0.00";
        //        ////= 1 / (1 + Inputs!$B$16)^A6
        //        ws.Cell("M6").Value = M6;
        //        ////= H6 * O6
        //        ws.Cell("N6").Value = N6;
        //        ws.Cell("N6").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("O6").Value = "-";
        //        ws.Cell("P6").Value = "-";

        //        //SIXTH ROW
        //        ws.Cell("A7").Value = 5;
        //        ws.Cell("A7").Style.NumberFormat.Format = "0";
        //        ////=IF(A7<=Inputs!$B$10;Inputs!$B$11*(1+Inputs!$B$12)^(A7-1);0)
        //        ws.Cell("B7").Value = B7;
        //        ws.Cell("B7").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("C7").Value = "-";
        //        ////= IF(A7 <= MIN(Inputs!$B$10; Inputs!$B$17); Inputs!$B$9 / MIN(Inputs!$B$10; Inputs!$B$17); 0)
        //        ws.Cell("D7").Value = D7;
        //        ws.Cell("D7").Style.NumberFormat.Format = "#,##0.00";
        //        ////=IF(A7<=Inputs!$B$10;Inputs!$B$18;0)*(1+Inputs!$B$19)^(A7-1)
        //        ws.Cell("E7").Value = E7;
        //        ws.Cell("E7").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("F7").Value = "-";
        //        ////= IF(A7 = Inputs!$B$10; Inputs!$B$17; 0)
        //        ws.Cell("G7").Value = G7;
        //        ws.Cell("G7").Style.NumberFormat.Format = "#,##0.00";
        //        ////= B7 - D7 - E7 + G7
        //        ws.Cell("H7").Value = H7;
        //        ws.Cell("H7").Style.NumberFormat.Format = "#,##0.00";
        //        ////= 1 / (1 + Inputs!$B$21)^A7
        //        ws.Cell("I7").Value = I7;
        //        ////= H7 * J7
        //        ws.Cell("J7").Value = J7;
        //        ws.Cell("J7").Style.NumberFormat.Format = "#,##0.00";
        //        ////= H7 * Inputs!$B$20
        //        ws.Cell("K7").Value = K7;
        //        ws.Cell("K7").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("L7").Value = L7;
        //        ws.Cell("L7").Style.NumberFormat.Format = "#,##0.00";
        //        ////= 1 / (1 + Inputs!$B$17)^A7
        //        ws.Cell("M7").Value = M7;
        //        ////= H7 * O7
        //        ws.Cell("N7").Value = N7;
        //        ws.Cell("N7").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("O7").Value = "-";
        //        ws.Cell("P7").Value = "-";

        //        //SEVENTH ROW
        //        ws.Cell("A8").Value = 6;
        //        ws.Cell("A8").Style.NumberFormat.Format = "0";
        //        ////=IF(A8<=Inputs!$B$10;Inputs!$B$11*(1+Inputs!$B$12)^(A8-1);0)
        //        ws.Cell("B8").Value = B8;
        //        ws.Cell("B8").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("C8").Value = "-";
        //        ////= IF(A8 <= MIN(Inputs!$B$10; Inputs!$B$17); Inputs!$B$9 / MIN(Inputs!$B$10; Inputs!$B$17); 0)
        //        ws.Cell("D8").Value = D8;
        //        ws.Cell("D8").Style.NumberFormat.Format = "#,##0.00";
        //        ////=IF(A8<=Inputs!$B$10;Inputs!$B$18;0)*(1+Inputs!$B$19)^(A8-1)
        //        ws.Cell("E8").Value = E8;
        //        ws.Cell("E8").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("F8").Value = "-";
        //        ////= IF(A8 = Inputs!$B$10; Inputs!$B$18; 0)
        //        ws.Cell("G8").Value = G8;
        //        ws.Cell("G8").Style.NumberFormat.Format = "#,##0.00";
        //        ////= B8 - D8 - E8 + G8
        //        ws.Cell("H8").Value = H8;
        //        ws.Cell("H8").Style.NumberFormat.Format = "#,##0.00";
        //        ////= 1 / (1 + Inputs!$B$21)^A8
        //        ws.Cell("I8").Value = I8;
        //        ////= H8 * J8
        //        ws.Cell("J8").Value = J8;
        //        ws.Cell("J8").Style.NumberFormat.Format = "#,##0.00";
        //        ////= H8 * Inputs!$B$20
        //        ws.Cell("K8").Value = K8;
        //        ws.Cell("K8").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("L8").Value = L8;
        //        ws.Cell("L8").Style.NumberFormat.Format = "#,##0.00";
        //        ////= 1 / (1 + Inputs!$B$18)^A8
        //        ws.Cell("M8").Value = M8;
        //        ////= H8 * O8
        //        ws.Cell("N8").Value = N8;
        //        ws.Cell("N8").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("O8").Value = "-";
        //        ws.Cell("P8").Value = "-";

        //        //EIGHTH ROW
        //        ws.Cell("A9").Value = 7;
        //        ws.Cell("A9").Style.NumberFormat.Format = "0";
        //        ////=IF(A9<=Inputs!$B$10;Inputs!$B$11*(1+Inputs!$B$12)^(A9-1);0)
        //        ws.Cell("B9").Value = B9;
        //        ws.Cell("B9").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("C9").Value = "-";
        //        ////= IF(A9 <= MIN(Inputs!$B$10; Inputs!$B$17); Inputs!$B$9 / MIN(Inputs!$B$10; Inputs!$B$17); 0)
        //        ws.Cell("D9").Value = D9;
        //        ws.Cell("D9").Style.NumberFormat.Format = "#,##0.00";
        //        ////=IF(A9<=Inputs!$B$10;Inputs!$B$19;0)*(1+Inputs!$B$19)^(A9-1)
        //        ws.Cell("E9").Value = E9;
        //        ws.Cell("E9").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("F9").Value = "-";
        //        ////= IF(A9 = Inputs!$B$10; Inputs!$B$19; 0)
        //        ws.Cell("G9").Value = G9;
        //        ws.Cell("G9").Style.NumberFormat.Format = "#,##0.00";
        //        ////= B9 - D9 - E9 + G9
        //        ws.Cell("H9").Value = H9;
        //        ws.Cell("H9").Style.NumberFormat.Format = "#,##0.00";
        //        ////= 1 / (1 + Inputs!$B$21)^A9
        //        ws.Cell("I9").Value = I9;
        //        ////= H9 * J9
        //        ws.Cell("J9").Value = J9;
        //        ws.Cell("J9").Style.NumberFormat.Format = "#,##0.00";
        //        ////= H9 * Inputs!$B$20
        //        ws.Cell("K9").Value = K9;
        //        ws.Cell("K9").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("L9").Value = L9;
        //        ws.Cell("L9").Style.NumberFormat.Format = "#,##0.00";
        //        ////= 1 / (1 + Inputs!$B$19)^A9
        //        ws.Cell("M9").Value = M9;
        //        ////= H9 * O9
        //        ws.Cell("N9").Value = N9;
        //        ws.Cell("N9").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("O9").Value = "-";
        //        ws.Cell("P9").Value = "-";

        //        //NINETH ROW
        //        ws.Cell("A10").Value = 8;
        //        ws.Cell("A10").Style.NumberFormat.Format = "0";
        //        ////=IF(A10<=Inputs!$B$10;Inputs!$B$11*(1+Inputs!$B$12)^(A10-1);0)
        //        ws.Cell("B10").Value = B10;
        //        ws.Cell("B10").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("C10").Value = "-";
        //        ////= IF(A10 <= MIN(Inputs!$B$10; Inputs!$B$17); Inputs!$B$9 / MIN(Inputs!$B$10; Inputs!$B$17); 0)
        //        ws.Cell("D10").Value = D10;
        //        ws.Cell("D10").Style.NumberFormat.Format = "#,##0.00";
        //        ////=IF(A10<=Inputs!$B$10;Inputs!$B$110;0)*(1+Inputs!$B$19)^(A10-1)
        //        ws.Cell("E10").Value = E10;
        //        ws.Cell("E10").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("F10").Value = "-";
        //        ////= IF(A10 = Inputs!$B$10; Inputs!$B$110; 0)
        //        ws.Cell("G10").Value = G10;
        //        ws.Cell("G10").Style.NumberFormat.Format = "#,##0.00";
        //        ////= B10 - D10 - E10 + G10
        //        ws.Cell("H10").Value = H10;
        //        ws.Cell("H10").Style.NumberFormat.Format = "#,##0.00";
        //        ////= 1 / (1 + Inputs!$B$21)^A10
        //        ws.Cell("I10").Value = I10;
        //        ////= H10 * J10
        //        ws.Cell("J10").Value = J10;
        //        ws.Cell("J10").Style.NumberFormat.Format = "#,##0.00";
        //        ////= H10 * Inputs!$B$20
        //        ws.Cell("K10").Value = K10;
        //        ws.Cell("K10").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("L10").Value = L10;
        //        ws.Cell("L10").Style.NumberFormat.Format = "#,##0.00";
        //        ////= 1 / (1 + Inputs!$B$110)^A10
        //        ws.Cell("M10").Value = M10;
        //        ////= H10 * O10
        //        ws.Cell("N10").Value = N10;
        //        ws.Cell("N10").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("O10").Value = "-";
        //        ws.Cell("P10").Value = "-";

        //        //TENTH ROW
        //        ws.Cell("A11").Value = 9;
        //        ws.Cell("A11").Style.NumberFormat.Format = "0";
        //        ////=IF(A11<=Inputs!$B$10;Inputs!$B$11*(1+Inputs!$B$12)^(A11-1);0)
        //        ws.Cell("B11").Value = B11;
        //        ws.Cell("B11").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("C11").Value = "-";
        //        ////= IF(A11 <= MIN(Inputs!$B$10; Inputs!$B$17); Inputs!$B$9 / MIN(Inputs!$B$10; Inputs!$B$17); 0)
        //        ws.Cell("D11").Value = D11;
        //        ws.Cell("D11").Style.NumberFormat.Format = "#,##0.00";
        //        ////=IF(A11<=Inputs!$B$10;Inputs!$B$111;0)*(1+Inputs!$B$19)^(A11-1)
        //        ws.Cell("E11").Value = E11;
        //        ws.Cell("E11").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("F11").Value = "-";
        //        ////= IF(A11 = Inputs!$B$10; Inputs!$B$111; 0)
        //        ws.Cell("G11").Value = G11;
        //        ws.Cell("G11").Style.NumberFormat.Format = "#,##0.00";
        //        ////= B11 - D11 - E11 + G11
        //        ws.Cell("H11").Value = H11;
        //        ws.Cell("H11").Style.NumberFormat.Format = "#,##0.00";
        //        ////= 1 / (1 + Inputs!$B$21)^A11
        //        ws.Cell("I11").Value = I11;
        //        ////= H11 * J11
        //        ws.Cell("J11").Value = J11;
        //        ws.Cell("J11").Style.NumberFormat.Format = "#,##0.00";
        //        ////= H11 * Inputs!$B$20
        //        ws.Cell("K11").Value = K11;
        //        ws.Cell("K11").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("L11").Value = L11;
        //        ws.Cell("L11").Style.NumberFormat.Format = "#,##0.00";
        //        ////= 1 / (1 + Inputs!$B$111)^A11
        //        ws.Cell("M11").Value = M11;
        //        ////= H11 * O11
        //        ws.Cell("N11").Value = N11;
        //        ws.Cell("N11").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("O11").Value = "-";
        //        ws.Cell("P11").Value = "-";

        //        //11TH ROW
        //        ws.Cell("A12").Value = 10;
        //        ws.Cell("A12").Style.NumberFormat.Format = "0";
        //        ////=IF(A12<=Inputs!$B$10;Inputs!$B$11*(1+Inputs!$B$12)^(A12-1);0)
        //        ws.Cell("B12").Value = B12;
        //        ws.Cell("B12").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("C12").Value = "-";
        //        ////= IF(A12 <= MIN(Inputs!$B$10; Inputs!$B$17); Inputs!$B$9 / MIN(Inputs!$B$10; Inputs!$B$17); 0)
        //        ws.Cell("D12").Value = D12;
        //        ws.Cell("D12").Style.NumberFormat.Format = "#,##0.00";
        //        ////=IF(A12<=Inputs!$B$10;Inputs!$B$112;0)*(1+Inputs!$B$19)^(A12-1)
        //        ws.Cell("E12").Value = E12;
        //        ws.Cell("E12").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("F12").Value = "-";
        //        ////= IF(A12 = Inputs!$B$10; Inputs!$B$112; 0)
        //        ws.Cell("G12").Value = G12;
        //        ws.Cell("G12").Style.NumberFormat.Format = "#,##0.00";
        //        ////= B12 - D12 - E12 + G12
        //        ws.Cell("H12").Value = H12;
        //        ws.Cell("H12").Style.NumberFormat.Format = "#,##0.00";
        //        ////= 1 / (1 + Inputs!$B$21)^A12
        //        ws.Cell("I12").Value = I12;
        //        ////= H12 * J12
        //        ws.Cell("J12").Value = J12;
        //        ws.Cell("J12").Style.NumberFormat.Format = "#,##0.00";
        //        ////= H12 * Inputs!$B$20
        //        ws.Cell("K12").Value = K12;
        //        ws.Cell("K12").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("L12").Value = L12;
        //        ws.Cell("L12").Style.NumberFormat.Format = "#,##0.00";
        //        ////= 1 / (1 + Inputs!$B$112)^A12
        //        ws.Cell("M12").Value = M12;
        //        ////= H12 * O12
        //        ws.Cell("N12").Value = N12;
        //        ws.Cell("N12").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("O12").Value = "-";
        //        ws.Cell("P12").Value = "-";

        //        //12TH ROW
        //        ws.Cell("A13").Value = 11;
        //        ws.Cell("A13").Style.NumberFormat.Format = "0";
        //        ////=IF(A13<=Inputs!$B$10;Inputs!$B$11*(1+Inputs!$B$12)^(A13-1);0)
        //        ws.Cell("B13").Value = B13;
        //        ws.Cell("B13").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("C13").Value = "-";
        //        ////= IF(A13 <= MIN(Inputs!$B$10; Inputs!$B$17); Inputs!$B$9 / MIN(Inputs!$B$10; Inputs!$B$17); 0)
        //        ws.Cell("D13").Value = D13;
        //        ws.Cell("D13").Style.NumberFormat.Format = "#,##0.00";
        //        ////=IF(A13<=Inputs!$B$10;Inputs!$B$113;0)*(1+Inputs!$B$19)^(A13-1)
        //        ws.Cell("E13").Value = E13;
        //        ws.Cell("E13").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("F13").Value = "-";
        //        ////= IF(A13 = Inputs!$B$10; Inputs!$B$113; 0)
        //        ws.Cell("G13").Value = G13;
        //        ws.Cell("G13").Style.NumberFormat.Format = "#,##0.00";
        //        ////= B13 - D13 - E13 + G13
        //        ws.Cell("H13").Value = H13;
        //        ws.Cell("H13").Style.NumberFormat.Format = "#,##0.00";
        //        ////= 1 / (1 + Inputs!$B$21)^A13
        //        ws.Cell("I13").Value = I13;
        //        ////= H13 * J13
        //        ws.Cell("J13").Value = J13;
        //        ws.Cell("J13").Style.NumberFormat.Format = "#,##0.00";
        //        ////= H13 * Inputs!$B$20
        //        ws.Cell("K13").Value = K13;
        //        ws.Cell("K13").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("L13").Value = L13;
        //        ws.Cell("L13").Style.NumberFormat.Format = "#,##0.00";
        //        ////= 1 / (1 + Inputs!$B$113)^A13
        //        ws.Cell("M13").Value = M13;
        //        ////= H13 * O13
        //        ws.Cell("N13").Value = N13;
        //        ws.Cell("N13").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("O13").Value = "-";
        //        ws.Cell("P13").Value = "-";

        //        //13TH ROW
        //        ws.Cell("A14").Value = 12;
        //        ws.Cell("A14").Style.NumberFormat.Format = "0";
        //        ////=IF(A14<=Inputs!$B$10;Inputs!$B$11*(1+Inputs!$B$12)^(A14-1);0)
        //        ws.Cell("B14").Value = B14;
        //        ws.Cell("B14").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("C14").Value = "-";
        //        ////= IF(A14 <= MIN(Inputs!$B$10; Inputs!$B$17); Inputs!$B$9 / MIN(Inputs!$B$10; Inputs!$B$17); 0)
        //        ws.Cell("D14").Value = D14;
        //        ws.Cell("D14").Style.NumberFormat.Format = "#,##0.00";
        //        ////=IF(A14<=Inputs!$B$10;Inputs!$B$114;0)*(1+Inputs!$B$19)^(A14-1)
        //        ws.Cell("E14").Value = E14;
        //        ws.Cell("E14").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("F14").Value = "-";
        //        ////= IF(A14 = Inputs!$B$10; Inputs!$B$114; 0)
        //        ws.Cell("G14").Value = G14;
        //        ws.Cell("G14").Style.NumberFormat.Format = "#,##0.00";
        //        ////= B14 - D14 - E14 + G14
        //        ws.Cell("H14").Value = H14;
        //        ws.Cell("H14").Style.NumberFormat.Format = "#,##0.00";
        //        ////= 1 / (1 + Inputs!$B$21)^A14
        //        ws.Cell("I14").Value = I14;
        //        ////= H14 * J14
        //        ws.Cell("J14").Value = J14;
        //        ws.Cell("J14").Style.NumberFormat.Format = "#,##0.00";
        //        ////= H14 * Inputs!$B$20
        //        ws.Cell("K14").Value = K14;
        //        ws.Cell("K14").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("L14").Value = L14;
        //        ws.Cell("L14").Style.NumberFormat.Format = "#,##0.00";
        //        ////= 1 / (1 + Inputs!$B$114)^A14
        //        ws.Cell("M14").Value = M14;
        //        ////= H14 * O14
        //        ws.Cell("N14").Value = N14;
        //        ws.Cell("N14").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("O14").Value = "-";
        //        ws.Cell("P14").Value = "-";

        //        //14TH ROW
        //        ws.Cell("A15").Value = 13;
        //        ws.Cell("A15").Style.NumberFormat.Format = "0";
        //        ////=IF(A15<=Inputs!$B$10;Inputs!$B$11*(1+Inputs!$B$12)^(A15-1);0)
        //        ws.Cell("B15").Value = B15;
        //        ws.Cell("B15").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("C15").Value = "-";
        //        ////= IF(A15 <= MIN(Inputs!$B$10; Inputs!$B$17); Inputs!$B$9 / MIN(Inputs!$B$10; Inputs!$B$17); 0)
        //        ws.Cell("D15").Value = D15;
        //        ws.Cell("D15").Style.NumberFormat.Format = "#,##0.00";
        //        ////=IF(A15<=Inputs!$B$10;Inputs!$B$115;0)*(1+Inputs!$B$19)^(A15-1)
        //        ws.Cell("E15").Value = E15;
        //        ws.Cell("E15").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("F15").Value = "-";
        //        ////= IF(A15 = Inputs!$B$10; Inputs!$B$115; 0)
        //        ws.Cell("G15").Value = G15;
        //        ws.Cell("G15").Style.NumberFormat.Format = "#,##0.00";
        //        ////= B15 - D15 - E15 + G15
        //        ws.Cell("H15").Value = H15;
        //        ws.Cell("H15").Style.NumberFormat.Format = "#,##0.00";
        //        ////= 1 / (1 + Inputs!$B$21)^A15
        //        ws.Cell("I15").Value = I15;
        //        ////= H15 * J15
        //        ws.Cell("J15").Value = J15;
        //        ws.Cell("J15").Style.NumberFormat.Format = "#,##0.00";
        //        ////= H15 * Inputs!$B$20
        //        ws.Cell("K15").Value = K15;
        //        ws.Cell("K15").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("L15").Value = L15;
        //        ws.Cell("L15").Style.NumberFormat.Format = "#,##0.00";
        //        ////= 1 / (1 + Inputs!$B$115)^A15
        //        ws.Cell("M15").Value = M15;
        //        ////= H15 * O15
        //        ws.Cell("N15").Value = N15;
        //        ws.Cell("N15").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("O15").Value = "-";
        //        ws.Cell("P15").Value = "-";

        //        //15TH ROW
        //        ws.Cell("A16").Value = 14;
        //        ws.Cell("A16").Style.NumberFormat.Format = "0";
        //        ////=IF(A16<=Inputs!$B$10;Inputs!$B$11*(1+Inputs!$B$12)^(A16-1);0)
        //        ws.Cell("B16").Value = B16;
        //        ws.Cell("B16").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("C16").Value = "-";
        //        ////= IF(A16 <= MIN(Inputs!$B$10; Inputs!$B$17); Inputs!$B$9 / MIN(Inputs!$B$10; Inputs!$B$17); 0)
        //        ws.Cell("D16").Value = D16;
        //        ws.Cell("D16").Style.NumberFormat.Format = "#,##0.00";
        //        ////=IF(A16<=Inputs!$B$10;Inputs!$B$116;0)*(1+Inputs!$B$19)^(A16-1)
        //        ws.Cell("E16").Value = E16;
        //        ws.Cell("E16").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("F16").Value = "-";
        //        ////= IF(A16 = Inputs!$B$10; Inputs!$B$116; 0)
        //        ws.Cell("G16").Value = G16;
        //        ws.Cell("G16").Style.NumberFormat.Format = "#,##0.00";
        //        ////= B16 - D16 - E16 + G16
        //        ws.Cell("H16").Value = H16;
        //        ws.Cell("H16").Style.NumberFormat.Format = "#,##0.00";
        //        ////= 1 / (1 + Inputs!$B$21)^A16
        //        ws.Cell("I16").Value = I16;
        //        ////= H16 * J16
        //        ws.Cell("J16").Value = J16;
        //        ws.Cell("J16").Style.NumberFormat.Format = "#,##0.00";
        //        ////= H16 * Inputs!$B$20
        //        ws.Cell("K16").Value = K16;
        //        ws.Cell("K16").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("L16").Value = L16;
        //        ws.Cell("L16").Style.NumberFormat.Format = "#,##0.00";
        //        ////= 1 / (1 + Inputs!$B$116)^A16
        //        ws.Cell("M16").Value = M16;
        //        ////= H16 * O16
        //        ws.Cell("N16").Value = N16;
        //        ws.Cell("N16").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("O16").Value = "-";
        //        ws.Cell("P16").Value = "-";

        //        //16TH ROW
        //        ws.Cell("A17").Value = 15;
        //        ws.Cell("A17").Style.NumberFormat.Format = "0";
        //        ////=IF(A17<=Inputs!$B$10;Inputs!$B$11*(1+Inputs!$B$12)^(A17-1);0)
        //        ws.Cell("B17").Value = B17;
        //        ws.Cell("B17").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("C17").Value = "-";
        //        ////= IF(A17 <= MIN(Inputs!$B$10; Inputs!$B$17); Inputs!$B$9 / MIN(Inputs!$B$10; Inputs!$B$17); 0)
        //        ws.Cell("D17").Value = D17;
        //        ws.Cell("D17").Style.NumberFormat.Format = "#,##0.00";
        //        ////=IF(A17<=Inputs!$B$10;Inputs!$B$117;0)*(1+Inputs!$B$19)^(A17-1)
        //        ws.Cell("E17").Value = E17;
        //        ws.Cell("E17").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("F17").Value = "-";
        //        ////= IF(A17 = Inputs!$B$10; Inputs!$B$117; 0)
        //        ws.Cell("G17").Value = G17;
        //        ws.Cell("G17").Style.NumberFormat.Format = "#,##0.00";
        //        ////= B17 - D17 - E17 + G17
        //        ws.Cell("H17").Value = H17;
        //        ws.Cell("H17").Style.NumberFormat.Format = "#,##0.00";
        //        ////= 1 / (1 + Inputs!$B$21)^A17
        //        ws.Cell("I17").Value = I17;
        //        ////= H17 * J17
        //        ws.Cell("J17").Value = J17;
        //        ws.Cell("J17").Style.NumberFormat.Format = "#,##0.00";
        //        ////= H17 * Inputs!$B$20
        //        ws.Cell("K17").Value = K17;
        //        ws.Cell("K17").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("L17").Value = L17;
        //        ws.Cell("L17").Style.NumberFormat.Format = "#,##0.00";
        //        ////= 1 / (1 + Inputs!$B$117)^A17
        //        ws.Cell("M17").Value = M17;
        //        ////= H17 * O17
        //        ws.Cell("N17").Value = N17;
        //        ws.Cell("N17").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("O17").Value = "-";
        //        ws.Cell("P17").Value = "-";

        //        //17TH ROW
        //        ws.Cell("A18").Value = 16;
        //        ws.Cell("A18").Style.NumberFormat.Format = "0";
        //        ////=IF(A18<=Inputs!$B$10;Inputs!$B$11*(1+Inputs!$B$12)^(A18-1);0)
        //        ws.Cell("B18").Value = B18;
        //        ws.Cell("B18").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("C18").Value = "-";
        //        ////= IF(A18 <= MIN(Inputs!$B$10; Inputs!$B$17); Inputs!$B$9 / MIN(Inputs!$B$10; Inputs!$B$17); 0)
        //        ws.Cell("D18").Value = D18;
        //        ws.Cell("D18").Style.NumberFormat.Format = "#,##0.00";
        //        ////=IF(A18<=Inputs!$B$10;Inputs!$B$118;0)*(1+Inputs!$B$19)^(A18-1)
        //        ws.Cell("E18").Value = E18;
        //        ws.Cell("E18").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("F18").Value = "-";
        //        ////= IF(A18 = Inputs!$B$10; Inputs!$B$118; 0)
        //        ws.Cell("G18").Value = G18;
        //        ws.Cell("G18").Style.NumberFormat.Format = "#,##0.00";
        //        ////= B18 - D18 - E18 + G18
        //        ws.Cell("H18").Value = H18;
        //        ws.Cell("H18").Style.NumberFormat.Format = "#,##0.00";
        //        ////= 1 / (1 + Inputs!$B$21)^A18
        //        ws.Cell("I18").Value = I18;
        //        ////= H18 * J18
        //        ws.Cell("J18").Value = J18;
        //        ws.Cell("J18").Style.NumberFormat.Format = "#,##0.00";
        //        ////= H18 * Inputs!$B$20
        //        ws.Cell("K18").Value = K18;
        //        ws.Cell("K18").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("L18").Value = L18;
        //        ws.Cell("L18").Style.NumberFormat.Format = "#,##0.00";
        //        ////= 1 / (1 + Inputs!$B$118)^A18
        //        ws.Cell("M18").Value = M18;
        //        ////= H18 * O18
        //        ws.Cell("N18").Value = N18;
        //        ws.Cell("N18").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("O18").Value = "-";
        //        ws.Cell("P18").Value = "-";

        //        //18TH ROW
        //        ws.Cell("A19").Value = 17;
        //        ws.Cell("A19").Style.NumberFormat.Format = "0";
        //        ////=IF(A19<=Inputs!$B$10;Inputs!$B$11*(1+Inputs!$B$12)^(A19-1);0)
        //        ws.Cell("B19").Value = B19;
        //        ws.Cell("B19").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("C19").Value = "-";
        //        ////= IF(A19 <= MIN(Inputs!$B$10; Inputs!$B$17); Inputs!$B$9 / MIN(Inputs!$B$10; Inputs!$B$17); 0)
        //        ws.Cell("D19").Value = D19;
        //        ws.Cell("D19").Style.NumberFormat.Format = "#,##0.00";
        //        ////=IF(A19<=Inputs!$B$10;Inputs!$B$119;0)*(1+Inputs!$B$19)^(A19-1)
        //        ws.Cell("E19").Value = E19;
        //        ws.Cell("E19").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("F19").Value = "-";
        //        ////= IF(A19 = Inputs!$B$10; Inputs!$B$119; 0)
        //        ws.Cell("G19").Value = G19;
        //        ws.Cell("G19").Style.NumberFormat.Format = "#,##0.00";
        //        ////= B19 - D19 - E19 + G19
        //        ws.Cell("H19").Value = H19;
        //        ws.Cell("H19").Style.NumberFormat.Format = "#,##0.00";
        //        ////= 1 / (1 + Inputs!$B$21)^A19
        //        ws.Cell("I19").Value = I19;
        //        ////= H19 * J19
        //        ws.Cell("J19").Value = J19;
        //        ws.Cell("J19").Style.NumberFormat.Format = "#,##0.00";
        //        ////= H19 * Inputs!$B$20
        //        ws.Cell("K19").Value = K19;
        //        ws.Cell("K19").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("L19").Value = L19;
        //        ws.Cell("L19").Style.NumberFormat.Format = "#,##0.00";
        //        ////= 1 / (1 + Inputs!$B$119)^A19
        //        ws.Cell("M19").Value = M19;
        //        ////= H19 * O19
        //        ws.Cell("N19").Value = N19;
        //        ws.Cell("N19").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("O19").Value = "-";
        //        ws.Cell("P19").Value = "-";

        //        //19TH ROW
        //        ws.Cell("A20").Value = 18;
        //        ws.Cell("A20").Style.NumberFormat.Format = "0";
        //        ////=IF(A20<=Inputs!$B$10;Inputs!$B$11*(1+Inputs!$B$12)^(A20-1);0)
        //        ws.Cell("B20").Value = B20;
        //        ws.Cell("B20").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("C20").Value = "-";
        //        ////= IF(A20 <= MIN(Inputs!$B$10; Inputs!$B$17); Inputs!$B$9 / MIN(Inputs!$B$10; Inputs!$B$17); 0)
        //        ws.Cell("D20").Value = D20;
        //        ws.Cell("D20").Style.NumberFormat.Format = "#,##0.00";
        //        ////=IF(A20<=Inputs!$B$10;Inputs!$B$120;0)*(1+Inputs!$B$19)^(A20-1)
        //        ws.Cell("E20").Value = E20;
        //        ws.Cell("E20").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("F20").Value = "-";
        //        ////= IF(A20 = Inputs!$B$10; Inputs!$B$120; 0)
        //        ws.Cell("G20").Value = G20;
        //        ws.Cell("G20").Style.NumberFormat.Format = "#,##0.00";
        //        ////= B20 - D20 - E20 + G20
        //        ws.Cell("H20").Value = H20;
        //        ws.Cell("H20").Style.NumberFormat.Format = "#,##0.00";
        //        ////= 1 / (1 + Inputs!$B$21)^A20
        //        ws.Cell("I20").Value = I20;
        //        ////= H20 * J20
        //        ws.Cell("J20").Value = J20;
        //        ws.Cell("J20").Style.NumberFormat.Format = "#,##0.00";
        //        ////= H20 * Inputs!$B$20
        //        ws.Cell("K20").Value = K20;
        //        ws.Cell("K20").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("L20").Value = L20;
        //        ws.Cell("L20").Style.NumberFormat.Format = "#,##0.00";
        //        ////= 1 / (1 + Inputs!$B$120)^A20
        //        ws.Cell("M20").Value = M20;
        //        ////= H20 * O20
        //        ws.Cell("N20").Value = N20;
        //        ws.Cell("N20").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("O20").Value = "-";
        //        ws.Cell("P20").Value = "-";

        //        //20TH ROW
        //        ws.Cell("A21").Value = 19;
        //        ws.Cell("A21").Style.NumberFormat.Format = "0";
        //        ////=IF(A21<=Inputs!$B$10;Inputs!$B$11*(1+Inputs!$B$12)^(A21-1);0)
        //        ws.Cell("B21").Value = B21;
        //        ws.Cell("B21").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("C21").Value = "-";
        //        ////= IF(A21 <= MIN(Inputs!$B$10; Inputs!$B$17); Inputs!$B$9 / MIN(Inputs!$B$10; Inputs!$B$17); 0)
        //        ws.Cell("D21").Value = D21;
        //        ws.Cell("D21").Style.NumberFormat.Format = "#,##0.00";
        //        ////=IF(A21<=Inputs!$B$10;Inputs!$B$121;0)*(1+Inputs!$B$19)^(A21-1)
        //        ws.Cell("E21").Value = E21;
        //        ws.Cell("E21").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("F21").Value = "-";
        //        ////= IF(A21 = Inputs!$B$10; Inputs!$B$121; 0)
        //        ws.Cell("G21").Value = G21;
        //        ws.Cell("G21").Style.NumberFormat.Format = "#,##0.00";
        //        ////= B21 - D21 - E21 + G21
        //        ws.Cell("H21").Value = H21;
        //        ws.Cell("H21").Style.NumberFormat.Format = "#,##0.00";
        //        ////= 1 / (1 + Inputs!$B$21)^A21
        //        ws.Cell("I21").Value = I21;
        //        ////= H21 * J21
        //        ws.Cell("J21").Value = J21;
        //        ws.Cell("J21").Style.NumberFormat.Format = "#,##0.00";
        //        ////= H21 * Inputs!$B$20
        //        ws.Cell("K21").Value = K21;
        //        ws.Cell("K21").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("L21").Value = L21;
        //        ws.Cell("L21").Style.NumberFormat.Format = "#,##0.00";
        //        ////= 1 / (1 + Inputs!$B$121)^A21
        //        ws.Cell("M21").Value = M21;
        //        ////= H21 * O21
        //        ws.Cell("N21").Value = N21;
        //        ws.Cell("N21").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("O21").Value = "-";
        //        ws.Cell("P21").Value = "-";

        //        //21ST ROW
        //        ws.Cell("A22").Value = 20;
        //        ws.Cell("A22").Style.NumberFormat.Format = "0";
        //        ////=IF(A22<=Inputs!$B$10;Inputs!$B$11*(1+Inputs!$B$12)^(A22-1);0)
        //        ws.Cell("B22").Value = B22;
        //        ws.Cell("B22").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("C22").Value = "-";
        //        ////= IF(A22 <= MIN(Inputs!$B$10; Inputs!$B$17); Inputs!$B$9 / MIN(Inputs!$B$10; Inputs!$B$17); 0)
        //        ws.Cell("D22").Value = D22;
        //        ws.Cell("D22").Style.NumberFormat.Format = "#,##0.00";
        //        ////=IF(A22<=Inputs!$B$10;Inputs!$B$122;0)*(1+Inputs!$B$19)^(A22-1)
        //        ws.Cell("E22").Value = E22;
        //        ws.Cell("E22").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("F22").Value = "-";
        //        ////= IF(A22 = Inputs!$B$10; Inputs!$B$122; 0)
        //        ws.Cell("G22").Value = G22;
        //        ws.Cell("G22").Style.NumberFormat.Format = "#,##0.00";
        //        ////= B22 - D22 - E22 + G22
        //        ws.Cell("H22").Value = H22;
        //        ws.Cell("H22").Style.NumberFormat.Format = "#,##0.00";
        //        ////= 1 / (1 + Inputs!$B$21)^A22
        //        ws.Cell("I22").Value = I22;
        //        ////= H22 * J22
        //        ws.Cell("J22").Value = J22;
        //        ws.Cell("J22").Style.NumberFormat.Format = "#,##0.00";
        //        ////= H22 * Inputs!$B$20
        //        ws.Cell("K22").Value = K22;
        //        ws.Cell("K22").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("L22").Value = L22;
        //        ws.Cell("L22").Style.NumberFormat.Format = "#,##0.00";
        //        ////= 1 / (1 + Inputs!$B$122)^A22
        //        ws.Cell("M22").Value = M22;
        //        ////= H22 * O22
        //        ws.Cell("N22").Value = N22;
        //        ws.Cell("N22").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("O22").Value = "-";
        //        ws.Cell("P22").Value = "-";

        //        //22ND ROW
        //        ws.Cell("A23").Value = "-";
        //        ws.Cell("B23").Value = "-";
        //        ws.Cell("C23").Value = "-";
        //        ws.Cell("D23").Value = "-";
        //        ws.Cell("E23").Value = "-";
        //        ws.Cell("F23").Value = "-";
        //        ws.Cell("G23").Value = "-";
        //        ws.Cell("H23").Value = "-";
        //        ws.Cell("I23").Value = "-";
        //        ws.Cell("J23").Value = J23;
        //        ws.Cell("J23").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("J23").Style.Font.Bold = true;
        //        ws.Cell("J23").Style.Fill.BackgroundColor = XLColor.LightGray;
        //        ws.Cell("K23").Value = "-";
        //        ws.Cell("L23").Value = "-";
        //        ws.Cell("M23").Value = "-";
        //        ws.Cell("N23").Value = N23;
        //        ws.Cell("N23").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("N23").Style.Font.Bold = true;
        //        ws.Cell("N23").Style.Fill.BackgroundColor = XLColor.LightGray;
        //        ws.Cell("O23").Value = O23;
        //        ws.Cell("O23").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("O23").Style.Font.Bold = true;
        //        ws.Cell("O23").Style.Fill.BackgroundColor = XLColor.LightGray;
        //        ws.Cell("P23").Value = P23;
        //        ws.Cell("P23").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("P23").Style.Font.Bold = true;
        //        ws.Cell("P23").Style.Fill.BackgroundColor = XLColor.LightGray;

        //        //23RD ROW
        //        ws.Cell("A24").Value = "-";
        //        ws.Cell("B24").Value = "-";
        //        ws.Cell("C24").Value = "-";
        //        ws.Cell("D24").Value = "-";
        //        ws.Cell("E24").Value = "-";
        //        ws.Cell("F24").Value = "-";
        //        ws.Cell("G24").Value = "-";
        //        ws.Cell("H24").Value = "-";
        //        ws.Cell("I24").Value = "-";
        //        ws.Cell("J24").Value = stat1;
        //        ws.Cell("J24").Style.Font.Bold = true;

        //        if (stat1 == "REFURBISH")
        //        {
        //            ws.Cell("J24").Style.Fill.BackgroundColor = XLColor.Green;
        //        }
        //        else
        //        {
        //            ws.Cell("J24").Style.Fill.BackgroundColor = XLColor.Red;
        //        }

        //        ws.Cell("K24").Value = "-";
        //        ws.Cell("L24").Value = "-";
        //        ws.Cell("M24").Value = "-";
        //        ws.Cell("N24").Value = stat2;
        //        ws.Cell("N24").Style.Font.Bold = true;

        //        if (stat2 == "REFURBISH")
        //        {
        //            ws.Cell("N24").Style.Fill.BackgroundColor = XLColor.Green;
        //        }
        //        else
        //        {
        //            ws.Cell("N24").Style.Fill.BackgroundColor = XLColor.Red;
        //        }

        //        ws.Cell("O24").Value = "-";
        //        ws.Cell("P24").Value = "-";

        //        ws.Columns().AdjustToContents();

        //        workbook.CalculateMode = XLCalculateMode.Auto;
        //        workbook.RecalculateAllFormulas();

        //        using (var stream = new MemoryStream())
        //        {
        //            workbook.SaveAs(stream);
        //            var content = stream.ToArray();

        //            var formulas = new Dictionary<string, string>();

        //            formulas["C2"] = "Scrap Value + Scrapping Cost";
        //            formulas["F2"] = "Return to Service Cost from Dashboard";
        //            formulas["J2"] = "(C2 + F2) * I2";
        //            formulas["N2"] = "(C2 + F2) * M2";
        //            formulas["E3"] = "IF(A3 <= Lease Term; Operating Costs; 0)";
        //            formulas["N3"] = "L3 * M3";
        //            formulas["J23"] = "SUM(J2 : J22)";
        //            formulas["N23"] = "SUM(N2 : N22)";
        //            formulas["O23"] = "IF(J23 >= 0; (Net Book Value + F2); 0)";
        //            formulas["P23"] = "IF(N23 >= 0; (Net Book Value + F2); 0)";
        //            formulas["J24"] = "IF(J23 >= 0; \"Refurbish\"; \"Scrap\")";
        //            formulas["N24"] = "IF(N23 >= 0; \"Refurbish\"; \"Scrap\")";

        //            for (int r = 3; r <= 22; r++)
        //            {
        //                formulas[$"B{r}"] = $"IF(A{r} <= Lease Term; Lease Income * (1 + Escalation Rate)^(A{r} - 1) ; 0)";
        //                formulas[$"D{r}"] = $"IF(A{r} <= MIN(Lease Term; Wear & Tear Period); F2 / MIN(Lease Term; Wear & Tear Period) ; 0)";
        //                formulas[$"G{r}"] = $"IF(A{r} = Lease Term; Residual Value; 0)";
        //                formulas[$"H{r}"] = $"B{r} - D{r} - E{r} + G{r}";
        //                formulas[$"I{r}"] = $"1 / (1 + WACC (Pre-Tax) from Input)^A{r}";
        //                formulas[$"J{r}"] = $"H{r} * I{r}";
        //                formulas[$"K{r}"] = $"H{r} * Corporate Tax Rate";
        //                formulas[$"L{r}"] = $"H{r} - K{r}";
        //                formulas[$"M{r}"] = $"1 / (1 + WACC (Post-Tax) from Input)^A{r}";
        //            }

        //            for (int r = 4; r <= 22; r++)
        //            {
        //                formulas[$"E{r}"] = $"IF(A{r} <= Lease Term; Operating Costs; 0) * (1 + Operating Costs Escalation)^(A{r} - 1)";
        //                formulas[$"N{r}"] = $"H{r} * M{r}";
        //            }

        //            return Ok(new
        //            {
        //                fileName = $"{wagonNumber}_DCF_Report.xlsx",
        //                fileBytes = Convert.ToBase64String(content),
        //                formulas = formulas
        //            });
        //        }
        //    }
        //}

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
            double benchmarkValue = Convert.ToDouble(ParseDecimalSafe(input.BenchmarkValue));

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

            double O23 = (J23 >= 0) ? benchmarkValue + refurbishCost : 0;
            double P23 = (N23 >= 0) ? benchmarkValue + refurbishCost : 0;

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
                ws.Cell("O1").Value = "Benchmark Value (Pre-Tax)";
                ws.Cell("P1").Value = "Benchmark Value (Post-Tax)";
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
                    formulas["O23"] = "IF(J23 >= 0; (Benchmark Value + F2); 0)";
                    formulas["P23"] = "IF(N23 >= 0; (Benchmark Value + F2); 0)";
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

            double benchmarkValue = Convert.ToDouble(ParseDecimalSafe(input.BenchmarkValue));

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

            double O23 = (J23 >= 0) ? benchmarkValue + refurbishCost : 0;
            double P23 = (N23 >= 0) ? benchmarkValue + refurbishCost : 0;

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
                ws.Cell("O1").Value = "Benchmark Value (Pre-Tax)";
                ws.Cell("P1").Value = "Benchmark Value (Post-Tax)";
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
                    formulas["O23"] = "IF(J23 >= 0; (Benchmark Value + F2); 0)";
                    formulas["P23"] = "IF(N23 >= 0; (Benchmark Value + F2); 0)";
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

        //[HttpGet("generateDcfLoco/{locoNumber}")]
        //public async Task<IActionResult> GenerateDcfLoco(int locoNumber)
        //{
        //    var input = await _context.LocoInputs
        //        .FirstOrDefaultAsync(i => i.LocoNumber == locoNumber);

        //    if (input == null)
        //        return BadRequest("Locomotive does not exist.");

        //    double scrapCost = Convert.ToDouble(ParseDecimalSafe(input.ScrappingCost));
        //    double scrapValue = Convert.ToDouble(ParseDecimalSafe(input.ScrapValue));
        //    double refurbishCost = Convert.ToDouble(ParseDecimalSafe(input.TotalCost));
        //    double corporateTax = Convert.ToDouble(ParseDecimalSafe(input.CorporateTaxRate)) / 100;

        //    int leaseTerm = ParseIntSafe(input.LeaseTerm);

        //    double leaseIncome = Convert.ToDouble(ParseDecimalSafe(input.LeaseIncome));
        //    double escalationRate = Convert.ToDouble(ParseDecimalSafe(input.EscalationRate)) / 100;

        //    int wearTear = ParseIntSafe(input.WearTearPeriod);

        //    double operatingCosts = Convert.ToDouble(ParseDecimalSafe(input.OperatingCosts));
        //    double operatingEscalation = Convert.ToDouble(ParseDecimalSafe(input.OperatingCostsEscalation)) / 100;

        //    double residualValue = Convert.ToDouble(ParseDecimalSafe(input.ResidualValue));

        //    double waccPre = Convert.ToDouble(ParseDecimalSafe(input.PreTax)) / 100;
        //    double waccPost = Convert.ToDouble(ParseDecimalSafe(input.PostTax)) / 100;

        //    double netBook = Convert.ToDouble(ParseDecimalSafe(input.NetBookValue));


        //    //C2
        //    double totalScrapValue = scrapValue + scrapCost;

        //    //J2
        //    double J2 = (totalScrapValue + refurbishCost) * -1;

        //    //N2
        //    double N2 = -totalScrapValue * 1 * (1 - corporateTax);

        //    //B3
        //    double B3 = (1 <= leaseTerm)
        //        ? leaseIncome * Math.Pow(1 + escalationRate, 1 - 1)
        //        : 0;

        //    //D3
        //    int minTerm = Math.Min(leaseTerm, wearTear);

        //    double D3 = (1 <= minTerm)
        //        ? refurbishCost / minTerm
        //        : 0;

        //    //E3
        //    double E3 = (1 <= leaseTerm)
        //        ? operatingCosts
        //        : 0;

        //    //G3
        //    double G3 = (1 == leaseTerm)
        //        ? residualValue
        //        : 0;

        //    //H3
        //    double H3 = B3 - D3 - E3 + G3;

        //    //I3
        //    double I3 = 1 / Math.Pow(1 + waccPre, 1);

        //    //J3
        //    double J3 = H3 * I3;

        //    //K3
        //    double K3 = H3 * corporateTax;

        //    //L3
        //    double L3 = H3 - K3;

        //    //M3
        //    double M3 = 1 / Math.Pow(1 + waccPost, 1);

        //    //N3
        //    double N3 = L3 * M3;

        //    //B4
        //    double B4 = (2 <= leaseTerm)
        //        ? leaseIncome * Math.Pow(1 + escalationRate, 2 - 1)
        //        : 0;

        //    //D4
        //    int minTerm2 = Math.Min(leaseTerm, wearTear);

        //    double D4 = (2 <= minTerm2)
        //        ? refurbishCost / minTerm2
        //        : 0;

        //    //E4
        //    double E4 = (2 <= leaseTerm)
        //        ? operatingCosts * Math.Pow(1 + operatingEscalation, 2 - 1)
        //        : 0;

        //    //G4
        //    double G4 = (2 == leaseTerm)
        //        ? residualValue
        //        : 0;

        //    //H4
        //    double H4 = B4 - D4 - E4 + G4;

        //    //I4
        //    double I4 = 1 / Math.Pow(1 + waccPre, 2);

        //    //J4
        //    double J4 = H4 * I4;

        //    //K4
        //    double K4 = H4 * corporateTax;

        //    //M4
        //    double M4 = 1 / Math.Pow(1 + waccPost, 2);

        //    //N4
        //    double N4 = H4 * M4;

        //    //FOURTH ROW
        //    //B5
        //    double B5 = (3 <= leaseTerm)
        //        ? leaseIncome * Math.Pow(1 + escalationRate, 3 - 1)
        //        : 0;

        //    //D5
        //    int minTerm3 = Math.Min(leaseTerm, wearTear);

        //    double D5 = (3 <= minTerm3)
        //        ? refurbishCost / minTerm3
        //        : 0;

        //    //E5
        //    double E5 = (3 <= leaseTerm)
        //        ? operatingCosts * Math.Pow(1 + operatingEscalation, 3 - 1)
        //        : 0;

        //    //G5
        //    double G5 = (3 == leaseTerm)
        //        ? residualValue
        //        : 0;

        //    //H5
        //    double H5 = B5 - D5 - E5 + G5;

        //    //I5
        //    double I5 = 1 / Math.Pow(1 + waccPre, 3);

        //    //J5
        //    double J5 = H5 * I5;

        //    //K5
        //    double K5 = H5 * corporateTax;

        //    //M5
        //    double M5 = 1 / Math.Pow(1 + waccPost, 3);

        //    //N5
        //    double N5 = H5 * M5;

        //    //FIFTH ROW
        //    //B6
        //    double B6 = (4 <= leaseTerm)
        //        ? leaseIncome * Math.Pow(1 + escalationRate, 4 - 1)
        //        : 0;

        //    //D6
        //    int minTerm4 = Math.Min(leaseTerm, wearTear);

        //    double D6 = (4 <= minTerm4)
        //        ? refurbishCost / minTerm4
        //        : 0;

        //    //E6
        //    double E6 = (4 <= leaseTerm)
        //        ? operatingCosts * Math.Pow(1 + operatingEscalation, 4 - 1)
        //        : 0;

        //    //G6
        //    double G6 = (4 == leaseTerm)
        //        ? residualValue
        //        : 0;

        //    //H6
        //    double H6 = B6 - D6 - E6 + G6;

        //    //I6
        //    double I6 = 1 / Math.Pow(1 + waccPre, 4);

        //    //J6
        //    double J6 = H6 * I6;

        //    //K6
        //    double K6 = H6 * corporateTax;

        //    //M6
        //    double M6 = 1 / Math.Pow(1 + waccPost, 4);

        //    //N6
        //    double N6 = H6 * M6;

        //    //SIXTH ROW
        //    //B7
        //    double B7 = (5 <= leaseTerm)
        //        ? leaseIncome * Math.Pow(1 + escalationRate, 5 - 1)
        //        : 0;

        //    //D7
        //    int minTerm5 = Math.Min(leaseTerm, wearTear);

        //    double D7 = (5 <= minTerm5)
        //        ? refurbishCost / minTerm5
        //        : 0;

        //    //E7
        //    double E7 = (5 <= leaseTerm)
        //        ? operatingCosts * Math.Pow(1 + operatingEscalation, 5 - 1)
        //        : 0;

        //    //G7
        //    double G7 = (5 == leaseTerm)
        //        ? residualValue
        //        : 0;

        //    //H7
        //    double H7 = B7 - D7 - E7 + G7;

        //    //I7
        //    double I7 = 1 / Math.Pow(1 + waccPre, 5);

        //    //J7
        //    double J7 = H7 * I7;

        //    //K7
        //    double K7 = H7 * corporateTax;

        //    //M7
        //    double M7 = 1 / Math.Pow(1 + waccPost, 5);

        //    //N7
        //    double N7 = H7 * M7;

        //    //SEVENTH ROW
        //    //B8
        //    double B8 = (6 <= leaseTerm)
        //        ? leaseIncome * Math.Pow(1 + escalationRate, 6 - 1)
        //        : 0;

        //    //D8
        //    int minTerm6 = Math.Min(leaseTerm, wearTear);

        //    double D8 = (6 <= minTerm6)
        //        ? refurbishCost / minTerm6
        //        : 0;

        //    //E8
        //    double E8 = (6 <= leaseTerm)
        //        ? operatingCosts * Math.Pow(1 + operatingEscalation, 6 - 1)
        //        : 0;

        //    //G8
        //    double G8 = (6 == leaseTerm)
        //        ? residualValue
        //        : 0;

        //    //H8
        //    double H8 = B8 - D8 - E8 + G8;

        //    //I8
        //    double I8 = 1 / Math.Pow(1 + waccPre, 6);

        //    //J8
        //    double J8 = H8 * I8;

        //    //K8
        //    double K8 = H8 * corporateTax;

        //    //M8
        //    double M8 = 1 / Math.Pow(1 + waccPost, 6);

        //    //N8
        //    double N8 = H8 * M8;

        //    //EIGHTH ROW
        //    //B9
        //    double B9 = (7 <= leaseTerm)
        //        ? leaseIncome * Math.Pow(1 + escalationRate, 7 - 1)
        //        : 0;

        //    //D9
        //    int minTerm7 = Math.Min(leaseTerm, wearTear);

        //    double D9 = (7 <= minTerm7)
        //        ? refurbishCost / minTerm7
        //        : 0;

        //    //E9
        //    double E9 = (7 <= leaseTerm)
        //        ? operatingCosts * Math.Pow(1 + operatingEscalation, 7 - 1)
        //        : 0;

        //    //G9
        //    double G9 = (7 == leaseTerm)
        //        ? residualValue
        //        : 0;

        //    //H9
        //    double H9 = B9 - D9 - E9 + G9;

        //    //I9
        //    double I9 = 1 / Math.Pow(1 + waccPre, 7);

        //    //J9
        //    double J9 = H9 * I9;

        //    //K9
        //    double K9 = H9 * corporateTax;

        //    //M9
        //    double M9 = 1 / Math.Pow(1 + waccPost, 7);

        //    //N9
        //    double N9 = H9 * M9;

        //    //NINETH ROW
        //    //B10
        //    double B10 = (8 <= leaseTerm)
        //        ? leaseIncome * Math.Pow(1 + escalationRate, 8 - 1)
        //        : 0;

        //    //D10
        //    int minTerm8 = Math.Min(leaseTerm, wearTear);

        //    double D10 = (8 <= minTerm8)
        //        ? refurbishCost / minTerm8
        //        : 0;

        //    //E10
        //    double E10 = (8 <= leaseTerm)
        //        ? operatingCosts * Math.Pow(1 + operatingEscalation, 8 - 1)
        //        : 0;

        //    //G10
        //    double G10 = (8 == leaseTerm)
        //        ? residualValue
        //        : 0;

        //    //H10
        //    double H10 = B10 - D10 - E10 + G10;

        //    //I10
        //    double I10 = 1 / Math.Pow(1 + waccPre, 8);

        //    //J10
        //    double J10 = H10 * I10;

        //    //K10
        //    double K10 = H10 * corporateTax;

        //    //M10
        //    double M10 = 1 / Math.Pow(1 + waccPost, 8);

        //    //N10
        //    double N10 = H10 * M10;

        //    //TENTH ROW
        //    //B11
        //    double B11 = (9 <= leaseTerm)
        //        ? leaseIncome * Math.Pow(1 + escalationRate, 9 - 1)
        //        : 0;

        //    //D11
        //    int minTerm9 = Math.Min(leaseTerm, wearTear);

        //    double D11 = (9 <= minTerm9)
        //        ? refurbishCost / minTerm9
        //        : 0;

        //    //E11
        //    double E11 = (9 <= leaseTerm)
        //        ? operatingCosts * Math.Pow(1 + operatingEscalation, 9 - 1)
        //        : 0;

        //    //G11
        //    double G11 = (9 == leaseTerm)
        //        ? residualValue
        //        : 0;

        //    //H11
        //    double H11 = B11 - D11 - E11 + G11;

        //    //I11
        //    double I11 = 1 / Math.Pow(1 + waccPre, 9);

        //    //J11
        //    double J11 = H11 * I11;

        //    //K11
        //    double K11 = H11 * corporateTax;

        //    //M11
        //    double M11 = 1 / Math.Pow(1 + waccPost, 9);

        //    //N11
        //    double N11 = H11 * M11;

        //    //11TH ROW
        //    //B12
        //    double B12 = (10 <= leaseTerm)
        //        ? leaseIncome * Math.Pow(1 + escalationRate, 10 - 1)
        //        : 0;

        //    //D12
        //    int minTerm10 = Math.Min(leaseTerm, wearTear);

        //    double D12 = (10 <= minTerm10)
        //        ? refurbishCost / minTerm10
        //        : 0;

        //    //E12
        //    double E12 = (10 <= leaseTerm)
        //        ? operatingCosts * Math.Pow(1 + operatingEscalation, 10 - 1)
        //        : 0;

        //    //G12
        //    double G12 = (10 == leaseTerm)
        //        ? residualValue
        //        : 0;

        //    //H12
        //    double H12 = B12 - D12 - E12 + G12;

        //    //I12
        //    double I12 = 1 / Math.Pow(1 + waccPre, 10);

        //    //J12
        //    double J12 = H12 * I12;

        //    //K12
        //    double K12 = H12 * corporateTax;

        //    //M12
        //    double M12 = 1 / Math.Pow(1 + waccPost, 10);

        //    //N12
        //    double N12 = H12 * M12;

        //    //12TH ROW
        //    //B13
        //    double B13 = (11 <= leaseTerm)
        //        ? leaseIncome * Math.Pow(1 + escalationRate, 11 - 1)
        //        : 0;

        //    //D13
        //    int minTerm11 = Math.Min(leaseTerm, wearTear);

        //    double D13 = (11 <= minTerm11)
        //        ? refurbishCost / minTerm11
        //        : 0;

        //    //E13
        //    double E13 = (11 <= leaseTerm)
        //        ? operatingCosts * Math.Pow(1 + operatingEscalation, 11 - 1)
        //        : 0;

        //    //G13
        //    double G13 = (11 == leaseTerm)
        //        ? residualValue
        //        : 0;

        //    //H13
        //    double H13 = B13 - D13 - E13 + G13;

        //    //I13
        //    double I13 = 1 / Math.Pow(1 + waccPre, 11);

        //    //J13
        //    double J13 = H13 * I13;

        //    //K13
        //    double K13 = H13 * corporateTax;

        //    //M13
        //    double M13 = 1 / Math.Pow(1 + waccPost, 11);

        //    //N13
        //    double N13 = H13 * M13;

        //    //13TH ROW
        //    //B14
        //    double B14 = (12 <= leaseTerm)
        //        ? leaseIncome * Math.Pow(1 + escalationRate, 12 - 1)
        //        : 0;

        //    //D14
        //    int minTerm12 = Math.Min(leaseTerm, wearTear);

        //    double D14 = (12 <= minTerm12)
        //        ? refurbishCost / minTerm12
        //        : 0;

        //    //E14
        //    double E14 = (12 <= leaseTerm)
        //        ? operatingCosts * Math.Pow(1 + operatingEscalation, 12 - 1)
        //        : 0;

        //    //G14
        //    double G14 = (12 == leaseTerm)
        //        ? residualValue
        //        : 0;

        //    //H14
        //    double H14 = B14 - D14 - E14 + G14;

        //    //I14
        //    double I14 = 1 / Math.Pow(1 + waccPre, 12);

        //    //J14
        //    double J14 = H14 * I14;

        //    //K14
        //    double K14 = H14 * corporateTax;

        //    //M14
        //    double M14 = 1 / Math.Pow(1 + waccPost, 12);

        //    //N14
        //    double N14 = H14 * M14;

        //    //14TH ROW
        //    //B15
        //    double B15 = (13 <= leaseTerm)
        //        ? leaseIncome * Math.Pow(1 + escalationRate, 13 - 1)
        //        : 0;

        //    //D15
        //    int minTerm13 = Math.Min(leaseTerm, wearTear);

        //    double D15 = (13 <= minTerm13)
        //        ? refurbishCost / minTerm13
        //        : 0;

        //    //E15
        //    double E15 = (13 <= leaseTerm)
        //        ? operatingCosts * Math.Pow(1 + operatingEscalation, 13 - 1)
        //        : 0;

        //    //G15
        //    double G15 = (13 == leaseTerm)
        //        ? residualValue
        //        : 0;

        //    //H15
        //    double H15 = B15 - D15 - E15 + G15;

        //    //I15
        //    double I15 = 1 / Math.Pow(1 + waccPre, 13);

        //    //J15
        //    double J15 = H15 * I15;

        //    //K15
        //    double K15 = H15 * corporateTax;

        //    //M15
        //    double M15 = 1 / Math.Pow(1 + waccPost, 13);

        //    //N15
        //    double N15 = H15 * M15;

        //    //15TH ROW
        //    //B16
        //    double B16 = (14 <= leaseTerm)
        //        ? leaseIncome * Math.Pow(1 + escalationRate, 14 - 1)
        //        : 0;

        //    //D16
        //    int minTerm14 = Math.Min(leaseTerm, wearTear);

        //    double D16 = (14 <= minTerm14)
        //        ? refurbishCost / minTerm14
        //        : 0;

        //    //E16
        //    double E16 = (14 <= leaseTerm)
        //        ? operatingCosts * Math.Pow(1 + operatingEscalation, 14 - 1)
        //        : 0;

        //    //G16
        //    double G16 = (14 == leaseTerm)
        //        ? residualValue
        //        : 0;

        //    //H16
        //    double H16 = B16 - D16 - E16 + G16;

        //    //I16
        //    double I16 = 1 / Math.Pow(1 + waccPre, 14);

        //    //J16
        //    double J16 = H16 * I16;

        //    //K16
        //    double K16 = H16 * corporateTax;

        //    //M16
        //    double M16 = 1 / Math.Pow(1 + waccPost, 14);

        //    //N16
        //    double N16 = H16 * M16;

        //    //16TH ROW
        //    //B17
        //    double B17 = (15 <= leaseTerm)
        //        ? leaseIncome * Math.Pow(1 + escalationRate, 15 - 1)
        //        : 0;

        //    //D17
        //    int minTerm15 = Math.Min(leaseTerm, wearTear);

        //    double D17 = (15 <= minTerm15)
        //        ? refurbishCost / minTerm15
        //        : 0;

        //    //E17
        //    double E17 = (15 <= leaseTerm)
        //        ? operatingCosts * Math.Pow(1 + operatingEscalation, 15 - 1)
        //        : 0;

        //    //G17
        //    double G17 = (15 == leaseTerm)
        //        ? residualValue
        //        : 0;

        //    //H17
        //    double H17 = B17 - D17 - E17 + G17;

        //    //I17
        //    double I17 = 1 / Math.Pow(1 + waccPre, 15);

        //    //J17
        //    double J17 = H17 * I17;

        //    //K17
        //    double K17 = H17 * corporateTax;

        //    //M17
        //    double M17 = 1 / Math.Pow(1 + waccPost, 15);

        //    //N17
        //    double N17 = H17 * M17;

        //    //17TH ROW
        //    //B18
        //    double B18 = (16 <= leaseTerm)
        //        ? leaseIncome * Math.Pow(1 + escalationRate, 16 - 1)
        //        : 0;

        //    //D18
        //    int minTerm16 = Math.Min(leaseTerm, wearTear);

        //    double D18 = (16 <= minTerm16)
        //        ? refurbishCost / minTerm16
        //        : 0;

        //    //E18
        //    double E18 = (16 <= leaseTerm)
        //        ? operatingCosts * Math.Pow(1 + operatingEscalation, 16 - 1)
        //        : 0;

        //    //G18
        //    double G18 = (16 == leaseTerm)
        //        ? residualValue
        //        : 0;

        //    //H18
        //    double H18 = B18 - D18 - E18 + G18;

        //    //I18
        //    double I18 = 1 / Math.Pow(1 + waccPre, 16);

        //    //J18
        //    double J18 = H18 * I18;

        //    //K18
        //    double K18 = H18 * corporateTax;

        //    //M18
        //    double M18 = 1 / Math.Pow(1 + waccPost, 16);

        //    //N18
        //    double N18 = H18 * M18;

        //    //18TH ROW
        //    //B19
        //    double B19 = (17 <= leaseTerm)
        //        ? leaseIncome * Math.Pow(1 + escalationRate, 17 - 1)
        //        : 0;

        //    //D19
        //    int minTerm17 = Math.Min(leaseTerm, wearTear);

        //    double D19 = (17 <= minTerm17)
        //        ? refurbishCost / minTerm17
        //        : 0;

        //    //E19
        //    double E19 = (17 <= leaseTerm)
        //        ? operatingCosts * Math.Pow(1 + operatingEscalation, 17 - 1)
        //        : 0;

        //    //G19
        //    double G19 = (17 == leaseTerm)
        //        ? residualValue
        //        : 0;

        //    //H19
        //    double H19 = B19 - D19 - E19 + G19;

        //    //I19
        //    double I19 = 1 / Math.Pow(1 + waccPre, 17);

        //    //J19
        //    double J19 = H19 * I19;

        //    //K19
        //    double K19 = H19 * corporateTax;

        //    //M19
        //    double M19 = 1 / Math.Pow(1 + waccPost, 17);

        //    //N19
        //    double N19 = H19 * M19;

        //    //19TH ROW
        //    //B20
        //    double B20 = (18 <= leaseTerm)
        //        ? leaseIncome * Math.Pow(1 + escalationRate, 18 - 1)
        //        : 0;

        //    //D20
        //    int minTerm18 = Math.Min(leaseTerm, wearTear);

        //    double D20 = (18 <= minTerm18)
        //        ? refurbishCost / minTerm18
        //        : 0;

        //    //E20
        //    double E20 = (18 <= leaseTerm)
        //        ? operatingCosts * Math.Pow(1 + operatingEscalation, 18 - 1)
        //        : 0;

        //    //G20
        //    double G20 = (18 == leaseTerm)
        //        ? residualValue
        //        : 0;

        //    //H20
        //    double H20 = B20 - D20 - E20 + G20;

        //    //I20
        //    double I20 = 1 / Math.Pow(1 + waccPre, 18);

        //    //J20
        //    double J20 = H20 * I20;

        //    //K20
        //    double K20 = H20 * corporateTax;

        //    //M20
        //    double M20 = 1 / Math.Pow(1 + waccPost, 18);

        //    //N20
        //    double N20 = H20 * M20;

        //    //20TH ROW
        //    //B21
        //    double B21 = (19 <= leaseTerm)
        //        ? leaseIncome * Math.Pow(1 + escalationRate, 19 - 1)
        //        : 0;

        //    //D21
        //    int minTerm19 = Math.Min(leaseTerm, wearTear);

        //    double D21 = (19 <= minTerm19)
        //        ? refurbishCost / minTerm19
        //        : 0;

        //    //E21
        //    double E21 = (19 <= leaseTerm)
        //        ? operatingCosts * Math.Pow(1 + operatingEscalation, 19 - 1)
        //        : 0;

        //    //G21
        //    double G21 = (19 == leaseTerm)
        //        ? residualValue
        //        : 0;

        //    //H21
        //    double H21 = B21 - D21 - E21 + G21;

        //    //I21
        //    double I21 = 1 / Math.Pow(1 + waccPre, 19);

        //    //J21
        //    double J21 = H21 * I21;

        //    //K21
        //    double K21 = H21 * corporateTax;

        //    //M21
        //    double M21 = 1 / Math.Pow(1 + waccPost, 19);

        //    //N21
        //    double N21 = H21 * M21;

        //    //21ST ROW
        //    //B22
        //    double B22 = (20 <= leaseTerm)
        //        ? leaseIncome * Math.Pow(1 + escalationRate, 20 - 1)
        //        : 0;

        //    //D22
        //    int minTerm20 = Math.Min(leaseTerm, wearTear);

        //    double D22 = (20 <= minTerm20)
        //        ? refurbishCost / minTerm20
        //        : 0;

        //    //E22
        //    double E22 = (20 <= leaseTerm)
        //        ? operatingCosts * Math.Pow(1 + operatingEscalation, 20 - 1)
        //        : 0;

        //    //G22
        //    double G22 = (20 == leaseTerm)
        //        ? residualValue
        //        : 0;

        //    //H22
        //    double H22 = B22 - D22 - E22 + G22;

        //    //I22
        //    double I22 = 1 / Math.Pow(1 + waccPre, 20);

        //    //J22
        //    double J22 = H22 * I22;

        //    //K22
        //    double K22 = H22 * corporateTax;

        //    //M22
        //    double M22 = 1 / Math.Pow(1 + waccPost, 20);

        //    //N22
        //    double N22 = H22 * M22;

        //    //22ND ROW
        //    double J23 = J2 + J3 + J4 + J5 + J6 + J7 + J8 + J9 + J10 + J11 + J12 + J13 + J14 + J15 + J16 + J17 + J18 + J19 + J20 + +J21 + J22;

        //    double N23 = N2 + N3 + N4 + N5 + N6 + N7 + N8 + N9 + N10 + N11 + N12 + N13 + N14 + N15 + N16 + N17 + N18 + N19 + N20 + +N21 + N22;

        //    double O23 = (J23 >= 0)
        //        ? netBook + refurbishCost
        //        : 0;

        //    double P23 = (N23 >= 0)
        //        ? netBook + refurbishCost
        //        : 0;

        //    //23RD ROW
        //    string stat1;
        //    string stat2;

        //    if (J23 >= 0)
        //    {
        //        stat1 = "REFURBISH";
        //    }
        //    else
        //    {
        //        stat1 = "SCRAP";
        //    }

        //    if (N23 >= 0)
        //    {
        //        stat2 = "REFURBISH";
        //    }
        //    else
        //    {
        //        stat2 = "SCRAP";
        //    }

        //    using (var workbook = new XLWorkbook())
        //    {
        //        var ws = workbook.Worksheets.Add("DCF_Loco");

        //        //HEADER ROW
        //        ws.Cell("A1").Value = "Year";
        //        ws.Cell("B1").Value = "Lease Revenue";
        //        ws.Cell("C1").Value = "Scrap Value";
        //        ws.Cell("D1").Value = "Wear & Tear";
        //        ws.Cell("E1").Value = "Operating Costs";
        //        ws.Cell("F1").Value = "Refurbishment Cost";
        //        ws.Cell("G1").Value = "Residual Value";
        //        ws.Cell("H1").Value = "Net Cash Flow";
        //        ws.Cell("I1").Value = "WACC (Pre-Tax)";
        //        ws.Cell("J1").Value = "Present Value (Pre-Tax)";
        //        ws.Cell("K1").Value = "Tax";
        //        ws.Cell("L1").Value = "EBITDA";
        //        ws.Cell("M1").Value = "WACC (Leveraged)";
        //        ws.Cell("N1").Value = "Present Value (Post-Tax)";
        //        ws.Cell("O1").Value = "Transfer Value (Pre-Tax)";
        //        ws.Cell("P1").Value = "Transfer Value (Post-Tax)";
        //        ws.Range("A1:P1").Style.Font.Bold = true;
        //        ws.Range("A1:P1").Style.Fill.BackgroundColor = XLColor.LightGray;

        //        //FIRST ROW
        //        ws.Cell("A2").Value = 0;
        //        ws.Cell("A2").Style.NumberFormat.Format = "0";
        //        ws.Cell("B2").Value = "-";
        //        ws.Cell("C2").Value = (Math.Round(totalScrapValue, MidpointRounding.AwayFromZero)); ;
        //        ws.Cell("C2").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("D2").Value = "-";
        //        ws.Cell("E2").Value = "-";
        //        ws.Cell("F2").Value = (Math.Round(refurbishCost, MidpointRounding.AwayFromZero)); ;
        //        ws.Cell("F2").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("G2").Value = "-";
        //        ws.Cell("H2").Value = "-";
        //        ws.Cell("I2").Value = -1;
        //        ws.Cell("I2").Style.NumberFormat.Format = "0";
        //        ws.Cell("J2").Value = (Math.Round(J2, MidpointRounding.AwayFromZero));
        //        ws.Cell("J2").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("K2").Value = "-";
        //        ws.Cell("L2").Value = "-";
        //        ws.Cell("M2").Value = 1;
        //        ws.Cell("M2").Style.NumberFormat.Format = "0";
        //        ws.Cell("N2").Value = (Math.Round(N2, MidpointRounding.AwayFromZero)); ;
        //        ws.Cell("N2").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("O2").Value = "-";
        //        ws.Cell("P2").Value = "-";

        //        //SECOND ROW
        //        ws.Cell("A3").Value = 1;
        //        ws.Cell("A3").Style.NumberFormat.Format = "0";
        //        //= IF(A3 <= Inputs!$B$10; Inputs!$B$11 * (1 + Inputs!$B$12)^(A3 - 1); 0)
        //        ws.Cell("B3").Value = (Math.Round(B3, MidpointRounding.AwayFromZero)); ;
        //        ws.Cell("B3").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("C3").Value = "-";
        //        //= IF(A3 <= MIN(Inputs!$B$10; Inputs!$B$17); Inputs!$B$9 / MIN(Inputs!$B$10; Inputs!$B$17); 0)
        //        ws.Cell("D3").Value = (Math.Round(D3, MidpointRounding.AwayFromZero)); ;
        //        ws.Cell("D3").Style.NumberFormat.Format = "#,##0.00";
        //        //= IF(A3 <= Inputs!$B$10; Inputs!$B$18; 0)
        //        ws.Cell("E3").Value = (Math.Round(E3, MidpointRounding.AwayFromZero)); ;
        //        ws.Cell("E3").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("F3").Value = "-";
        //        //= IF(A3 = Inputs!$B$10; Inputs!$B$14; 0)
        //        ws.Cell("G3").Value = (Math.Round(G3, MidpointRounding.AwayFromZero));
        //        ws.Cell("G3").Style.NumberFormat.Format = "#,##0.00";
        //        //= B3 - D3 - E3 + G3
        //        ws.Cell("H3").Value = (Math.Round(H3, MidpointRounding.AwayFromZero));
        //        ws.Cell("H3").Style.NumberFormat.Format = "#,##0.00";
        //        //= 1 / (1 + Inputs!$B$21)^A3
        //        ws.Cell("I3").Value = (Math.Round(I3, MidpointRounding.AwayFromZero));
        //        //= H3 * J3
        //        ws.Cell("J3").Value = (Math.Round(J3, MidpointRounding.AwayFromZero));
        //        ws.Cell("J3").Style.NumberFormat.Format = "#,##0.00";
        //        //= H3 * Inputs!$B$20
        //        ws.Cell("K3").Value = K3;
        //        ws.Cell("K3").Style.NumberFormat.Format = "#,##0.00";
        //        //= H3 - M3
        //        ws.Cell("L3").Value = L3;
        //        ws.Cell("L3").Style.NumberFormat.Format = "#,##0.00";
        //        //= 1 / (1 + Inputs!$B$16)^A3
        //        ws.Cell("M3").Value = M3;
        //        //= N3 * O3
        //        ws.Cell("N3").Value = N3;
        //        ws.Cell("N3").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("O3").Value = "-";
        //        ws.Cell("P3").Value = "-";

        //        //THIRD ROW
        //        ws.Cell("A4").Value = 2;
        //        ws.Cell("A4").Style.NumberFormat.Format = "0";
        //        ////=IF(A4<=Inputs!$B$10;Inputs!$B$11*(1+Inputs!$B$12)^(A4-1);0)
        //        ws.Cell("B4").Value = B4;
        //        ws.Cell("B4").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("C4").Value = "-";
        //        ////= IF(A4 <= MIN(Inputs!$B$10; Inputs!$B$17); Inputs!$B$9 / MIN(Inputs!$B$10; Inputs!$B$17); 0)
        //        ws.Cell("D4").Value = D4;
        //        ws.Cell("D4").Style.NumberFormat.Format = "#,##0.00";
        //        ////=IF(A4<=Inputs!$B$10;Inputs!$B$18;0)*(1+Inputs!$B$19)^(A4-1)
        //        ws.Cell("E4").Value = E4;
        //        ws.Cell("E4").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("F4").Value = "-";
        //        ////= IF(A4 = Inputs!$B$10; Inputs!$B$14; 0)
        //        ws.Cell("G4").Value = G4;
        //        ws.Cell("G4").Style.NumberFormat.Format = "#,##0.00";
        //        ////= B4 - D4 - E4 + G4
        //        ws.Cell("H4").Value = H4;
        //        ws.Cell("H4").Style.NumberFormat.Format = "#,##0.00";
        //        ////= 1 / (1 + Inputs!$B$21)^A4
        //        ws.Cell("I4").Value = I4;
        //        ////= H4 * J4
        //        ws.Cell("J4").Value = J4;
        //        ws.Cell("J4").Style.NumberFormat.Format = "#,##0.00";
        //        ////= H4 * Inputs!$B$20
        //        ws.Cell("K4").Value = K4;
        //        ws.Cell("K4").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("L4").Value = "-";
        //        ////= 1 / (1 + Inputs!$B$16)^A4
        //        ws.Cell("M4").Value = M4;
        //        ////= H4 * O4
        //        ws.Cell("N4").Value = N4;
        //        ws.Cell("N4").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("O4").Value = "-";
        //        ws.Cell("P4").Value = "-";

        //        //FOURTH ROW
        //        ws.Cell("A5").Value = 3;
        //        ws.Cell("A5").Style.NumberFormat.Format = "0";
        //        ////=IF(A5<=Inputs!$B$10;Inputs!$B$11*(1+Inputs!$B$12)^(A5-1);0)
        //        ws.Cell("B5").Value = B5;
        //        ws.Cell("B5").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("C5").Value = "-";
        //        ////= IF(A5 <= MIN(Inputs!$B$10; Inputs!$B$17); Inputs!$B$9 / MIN(Inputs!$B$10; Inputs!$B$17); 0)
        //        ws.Cell("D5").Value = D5;
        //        ws.Cell("D5").Style.NumberFormat.Format = "#,##0.00";
        //        ////=IF(A5<=Inputs!$B$10;Inputs!$B$18;0)*(1+Inputs!$B$19)^(A5-1)
        //        ws.Cell("E5").Value = E5;
        //        ws.Cell("E5").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("F5").Value = "-";
        //        ////= IF(A5 = Inputs!$B$10; Inputs!$B$15; 0)
        //        ws.Cell("G5").Value = G5;
        //        ws.Cell("G5").Style.NumberFormat.Format = "#,##0.00";
        //        ////= B5 - D5 - E5 + G5
        //        ws.Cell("H5").Value = H5;
        //        ws.Cell("H5").Style.NumberFormat.Format = "#,##0.00";
        //        ////= 1 / (1 + Inputs!$B$21)^A5
        //        ws.Cell("I5").Value = I5;
        //        ////= H5 * J5
        //        ws.Cell("J5").Value = J5;
        //        ws.Cell("J5").Style.NumberFormat.Format = "#,##0.00";
        //        ////= H5 * Inputs!$B$20
        //        ws.Cell("K5").Value = K5;
        //        ws.Cell("K5").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("L5").Value = "-";
        //        ////= 1 / (1 + Inputs!$B$16)^A5
        //        ws.Cell("M5").Value = M5;
        //        ////= H5 * O5
        //        ws.Cell("N5").Value = N5;
        //        ws.Cell("N5").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("O5").Value = "-";
        //        ws.Cell("P5").Value = "-";

        //        //FIFTH ROW
        //        ws.Cell("A6").Value = 4;
        //        ws.Cell("A6").Style.NumberFormat.Format = "0";
        //        ////=IF(A6<=Inputs!$B$10;Inputs!$B$11*(1+Inputs!$B$12)^(A6-1);0)
        //        ws.Cell("B6").Value = (Math.Round(B6, MidpointRounding.AwayFromZero)); ;
        //        ws.Cell("B6").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("C6").Value = "-";
        //        ////= IF(A6 <= MIN(Inputs!$B$10; Inputs!$B$17); Inputs!$B$9 / MIN(Inputs!$B$10; Inputs!$B$17); 0)
        //        ws.Cell("D6").Value = D6;
        //        ws.Cell("D6").Style.NumberFormat.Format = "#,##0.00";
        //        ////=IF(A6<=Inputs!$B$10;Inputs!$B$18;0)*(1+Inputs!$B$19)^(A6-1)
        //        ws.Cell("E6").Value = (Math.Round(E6, MidpointRounding.AwayFromZero)); ;
        //        ws.Cell("E6").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("F6").Value = "-";
        //        ////= IF(A6 = Inputs!$B$10; Inputs!$B$16; 0)
        //        ws.Cell("G6").Value = G6;
        //        ws.Cell("G6").Style.NumberFormat.Format = "#,##0.00";
        //        ////= B6 - D6 - E6 + G6
        //        ws.Cell("H6").Value = H6;
        //        ws.Cell("H6").Style.NumberFormat.Format = "#,##0.00";
        //        ////= 1 / (1 + Inputs!$B$21)^A6
        //        ws.Cell("I6").Value = I6;
        //        ////= H6 * J6
        //        ws.Cell("J6").Value = J6;
        //        ws.Cell("J6").Style.NumberFormat.Format = "#,##0.00";
        //        ////= H6 * Inputs!$B$20
        //        ws.Cell("K6").Value = K6;
        //        ws.Cell("K6").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("L6").Value = "-";
        //        ////= 1 / (1 + Inputs!$B$16)^A6
        //        ws.Cell("M6").Value = M6;
        //        ////= H6 * O6
        //        ws.Cell("N6").Value = N6;
        //        ws.Cell("N6").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("O6").Value = "-";
        //        ws.Cell("P6").Value = "-";

        //        //SIXTH ROW
        //        ws.Cell("A7").Value = 5;
        //        ws.Cell("A7").Style.NumberFormat.Format = "0";
        //        ////=IF(A7<=Inputs!$B$10;Inputs!$B$11*(1+Inputs!$B$12)^(A7-1);0)
        //        ws.Cell("B7").Value = (Math.Round(B7, MidpointRounding.AwayFromZero));
        //        ws.Cell("B7").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("C7").Value = "-";
        //        ////= IF(A7 <= MIN(Inputs!$B$10; Inputs!$B$17); Inputs!$B$9 / MIN(Inputs!$B$10; Inputs!$B$17); 0)
        //        ws.Cell("D7").Value = D7;
        //        ws.Cell("D7").Style.NumberFormat.Format = "#,##0.00";
        //        ////=IF(A7<=Inputs!$B$10;Inputs!$B$18;0)*(1+Inputs!$B$19)^(A7-1)
        //        ws.Cell("E7").Value = E7;
        //        ws.Cell("E7").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("F7").Value = "-";
        //        ////= IF(A7 = Inputs!$B$10; Inputs!$B$17; 0)
        //        ws.Cell("G7").Value = G7;
        //        ws.Cell("G7").Style.NumberFormat.Format = "#,##0.00";
        //        ////= B7 - D7 - E7 + G7
        //        ws.Cell("H7").Value = H7;
        //        ws.Cell("H7").Style.NumberFormat.Format = "#,##0.00";
        //        ////= 1 / (1 + Inputs!$B$21)^A7
        //        ws.Cell("I7").Value = I7;
        //        ////= H7 * J7
        //        ws.Cell("J7").Value = J7;
        //        ws.Cell("J7").Style.NumberFormat.Format = "#,##0.00";
        //        ////= H7 * Inputs!$B$20
        //        ws.Cell("K7").Value = K7;
        //        ws.Cell("K7").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("L7").Value = "-";
        //        ////= 1 / (1 + Inputs!$B$17)^A7
        //        ws.Cell("M7").Value = M7;
        //        ////= H7 * O7
        //        ws.Cell("N7").Value = N7;
        //        ws.Cell("N7").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("O7").Value = "-";
        //        ws.Cell("P7").Value = "-";

        //        //SEVENTH ROW
        //        ws.Cell("A8").Value = 6;
        //        ws.Cell("A8").Style.NumberFormat.Format = "0";
        //        ////=IF(A8<=Inputs!$B$10;Inputs!$B$11*(1+Inputs!$B$12)^(A8-1);0)
        //        ws.Cell("B8").Value = B8;
        //        ws.Cell("B8").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("C8").Value = "-";
        //        ////= IF(A8 <= MIN(Inputs!$B$10; Inputs!$B$17); Inputs!$B$9 / MIN(Inputs!$B$10; Inputs!$B$17); 0)
        //        ws.Cell("D8").Value = D8;
        //        ws.Cell("D8").Style.NumberFormat.Format = "#,##0.00";
        //        ////=IF(A8<=Inputs!$B$10;Inputs!$B$18;0)*(1+Inputs!$B$19)^(A8-1)
        //        ws.Cell("E8").Value = E8;
        //        ws.Cell("E8").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("F8").Value = "-";
        //        ////= IF(A8 = Inputs!$B$10; Inputs!$B$18; 0)
        //        ws.Cell("G8").Value = G8;
        //        ws.Cell("G8").Style.NumberFormat.Format = "#,##0.00";
        //        ////= B8 - D8 - E8 + G8
        //        ws.Cell("H8").Value = H8;
        //        ws.Cell("H8").Style.NumberFormat.Format = "#,##0.00";
        //        ////= 1 / (1 + Inputs!$B$21)^A8
        //        ws.Cell("I8").Value = I8;
        //        ////= H8 * J8
        //        ws.Cell("J8").Value = J8;
        //        ws.Cell("J8").Style.NumberFormat.Format = "#,##0.00";
        //        ////= H8 * Inputs!$B$20
        //        ws.Cell("K8").Value = K8;
        //        ws.Cell("K8").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("L8").Value = "-";
        //        ////= 1 / (1 + Inputs!$B$18)^A8
        //        ws.Cell("M8").Value = M8;
        //        ////= H8 * O8
        //        ws.Cell("N8").Value = N8;
        //        ws.Cell("N8").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("O8").Value = "-";
        //        ws.Cell("P8").Value = "-";

        //        //EIGHTH ROW
        //        ws.Cell("A9").Value = 7;
        //        ws.Cell("A9").Style.NumberFormat.Format = "0";
        //        ////=IF(A9<=Inputs!$B$10;Inputs!$B$11*(1+Inputs!$B$12)^(A9-1);0)
        //        ws.Cell("B9").Value = B9;
        //        ws.Cell("B9").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("C9").Value = "-";
        //        ////= IF(A9 <= MIN(Inputs!$B$10; Inputs!$B$17); Inputs!$B$9 / MIN(Inputs!$B$10; Inputs!$B$17); 0)
        //        ws.Cell("D9").Value = D9;
        //        ws.Cell("D9").Style.NumberFormat.Format = "#,##0.00";
        //        ////=IF(A9<=Inputs!$B$10;Inputs!$B$19;0)*(1+Inputs!$B$19)^(A9-1)
        //        ws.Cell("E9").Value = E9;
        //        ws.Cell("E9").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("F9").Value = "-";
        //        ////= IF(A9 = Inputs!$B$10; Inputs!$B$19; 0)
        //        ws.Cell("G9").Value = G9;
        //        ws.Cell("G9").Style.NumberFormat.Format = "#,##0.00";
        //        ////= B9 - D9 - E9 + G9
        //        ws.Cell("H9").Value = H9;
        //        ws.Cell("H9").Style.NumberFormat.Format = "#,##0.00";
        //        ////= 1 / (1 + Inputs!$B$21)^A9
        //        ws.Cell("I9").Value = I9;
        //        ////= H9 * J9
        //        ws.Cell("J9").Value = J9;
        //        ws.Cell("J9").Style.NumberFormat.Format = "#,##0.00";
        //        ////= H9 * Inputs!$B$20
        //        ws.Cell("K9").Value = K9;
        //        ws.Cell("K9").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("L9").Value = "-";
        //        ////= 1 / (1 + Inputs!$B$19)^A9
        //        ws.Cell("M9").Value = M9;
        //        ////= H9 * O9
        //        ws.Cell("N9").Value = N9;
        //        ws.Cell("N9").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("O9").Value = "-";
        //        ws.Cell("P9").Value = "-";

        //        //NINETH ROW
        //        ws.Cell("A10").Value = 8;
        //        ws.Cell("A10").Style.NumberFormat.Format = "0";
        //        ////=IF(A10<=Inputs!$B$10;Inputs!$B$11*(1+Inputs!$B$12)^(A10-1);0)
        //        ws.Cell("B10").Value = B10;
        //        ws.Cell("B10").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("C10").Value = "-";
        //        ////= IF(A10 <= MIN(Inputs!$B$10; Inputs!$B$17); Inputs!$B$9 / MIN(Inputs!$B$10; Inputs!$B$17); 0)
        //        ws.Cell("D10").Value = D10;
        //        ws.Cell("D10").Style.NumberFormat.Format = "#,##0.00";
        //        ////=IF(A10<=Inputs!$B$10;Inputs!$B$110;0)*(1+Inputs!$B$19)^(A10-1)
        //        ws.Cell("E10").Value = E10;
        //        ws.Cell("E10").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("F10").Value = "-";
        //        ////= IF(A10 = Inputs!$B$10; Inputs!$B$110; 0)
        //        ws.Cell("G10").Value = G10;
        //        ws.Cell("G10").Style.NumberFormat.Format = "#,##0.00";
        //        ////= B10 - D10 - E10 + G10
        //        ws.Cell("H10").Value = H10;
        //        ws.Cell("H10").Style.NumberFormat.Format = "#,##0.00";
        //        ////= 1 / (1 + Inputs!$B$21)^A10
        //        ws.Cell("I10").Value = I10;
        //        ////= H10 * J10
        //        ws.Cell("J10").Value = J10;
        //        ws.Cell("J10").Style.NumberFormat.Format = "#,##0.00";
        //        ////= H10 * Inputs!$B$20
        //        ws.Cell("K10").Value = K10;
        //        ws.Cell("K10").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("L10").Value = "-";
        //        ////= 1 / (1 + Inputs!$B$110)^A10
        //        ws.Cell("M10").Value = M10;
        //        ////= H10 * O10
        //        ws.Cell("N10").Value = N10;
        //        ws.Cell("N10").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("O10").Value = "-";
        //        ws.Cell("P10").Value = "-";

        //        //TENTH ROW
        //        ws.Cell("A11").Value = 9;
        //        ws.Cell("A11").Style.NumberFormat.Format = "0";
        //        ////=IF(A11<=Inputs!$B$10;Inputs!$B$11*(1+Inputs!$B$12)^(A11-1);0)
        //        ws.Cell("B11").Value = B11;
        //        ws.Cell("B11").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("C11").Value = "-";
        //        ////= IF(A11 <= MIN(Inputs!$B$10; Inputs!$B$17); Inputs!$B$9 / MIN(Inputs!$B$10; Inputs!$B$17); 0)
        //        ws.Cell("D11").Value = D11;
        //        ws.Cell("D11").Style.NumberFormat.Format = "#,##0.00";
        //        ////=IF(A11<=Inputs!$B$10;Inputs!$B$111;0)*(1+Inputs!$B$19)^(A11-1)
        //        ws.Cell("E11").Value = E11;
        //        ws.Cell("E11").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("F11").Value = "-";
        //        ////= IF(A11 = Inputs!$B$10; Inputs!$B$111; 0)
        //        ws.Cell("G11").Value = G11;
        //        ws.Cell("G11").Style.NumberFormat.Format = "#,##0.00";
        //        ////= B11 - D11 - E11 + G11
        //        ws.Cell("H11").Value = H11;
        //        ws.Cell("H11").Style.NumberFormat.Format = "#,##0.00";
        //        ////= 1 / (1 + Inputs!$B$21)^A11
        //        ws.Cell("I11").Value = I11;
        //        ////= H11 * J11
        //        ws.Cell("J11").Value = J11;
        //        ws.Cell("J11").Style.NumberFormat.Format = "#,##0.00";
        //        ////= H11 * Inputs!$B$20
        //        ws.Cell("K11").Value = K11;
        //        ws.Cell("K11").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("L11").Value = "-";
        //        ////= 1 / (1 + Inputs!$B$111)^A11
        //        ws.Cell("M11").Value = M11;
        //        ////= H11 * O11
        //        ws.Cell("N11").Value = N11;
        //        ws.Cell("N11").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("O11").Value = "-";
        //        ws.Cell("P11").Value = "-";

        //        //11TH ROW
        //        ws.Cell("A12").Value = 10;
        //        ws.Cell("A12").Style.NumberFormat.Format = "0";
        //        ////=IF(A12<=Inputs!$B$10;Inputs!$B$11*(1+Inputs!$B$12)^(A12-1);0)
        //        ws.Cell("B12").Value = B12;
        //        ws.Cell("B12").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("C12").Value = "-";
        //        ////= IF(A12 <= MIN(Inputs!$B$10; Inputs!$B$17); Inputs!$B$9 / MIN(Inputs!$B$10; Inputs!$B$17); 0)
        //        ws.Cell("D12").Value = D12;
        //        ws.Cell("D12").Style.NumberFormat.Format = "#,##0.00";
        //        ////=IF(A12<=Inputs!$B$10;Inputs!$B$112;0)*(1+Inputs!$B$19)^(A12-1)
        //        ws.Cell("E12").Value = E12;
        //        ws.Cell("E12").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("F12").Value = "-";
        //        ////= IF(A12 = Inputs!$B$10; Inputs!$B$112; 0)
        //        ws.Cell("G12").Value = G12;
        //        ws.Cell("G12").Style.NumberFormat.Format = "#,##0.00";
        //        ////= B12 - D12 - E12 + G12
        //        ws.Cell("H12").Value = H12;
        //        ws.Cell("H12").Style.NumberFormat.Format = "#,##0.00";
        //        ////= 1 / (1 + Inputs!$B$21)^A12
        //        ws.Cell("I12").Value = I12;
        //        ////= H12 * J12
        //        ws.Cell("J12").Value = J12;
        //        ws.Cell("J12").Style.NumberFormat.Format = "#,##0.00";
        //        ////= H12 * Inputs!$B$20
        //        ws.Cell("K12").Value = K12;
        //        ws.Cell("K12").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("L12").Value = "-";
        //        ////= 1 / (1 + Inputs!$B$112)^A12
        //        ws.Cell("M12").Value = M12;
        //        ////= H12 * O12
        //        ws.Cell("N12").Value = N12;
        //        ws.Cell("N12").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("O12").Value = "-";
        //        ws.Cell("P12").Value = "-";

        //        //12TH ROW
        //        ws.Cell("A13").Value = 11;
        //        ws.Cell("A13").Style.NumberFormat.Format = "0";
        //        ////=IF(A13<=Inputs!$B$10;Inputs!$B$11*(1+Inputs!$B$12)^(A13-1);0)
        //        ws.Cell("B13").Value = B13;
        //        ws.Cell("B13").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("C13").Value = "-";
        //        ////= IF(A13 <= MIN(Inputs!$B$10; Inputs!$B$17); Inputs!$B$9 / MIN(Inputs!$B$10; Inputs!$B$17); 0)
        //        ws.Cell("D13").Value = D13;
        //        ws.Cell("D13").Style.NumberFormat.Format = "#,##0.00";
        //        ////=IF(A13<=Inputs!$B$10;Inputs!$B$113;0)*(1+Inputs!$B$19)^(A13-1)
        //        ws.Cell("E13").Value = E13;
        //        ws.Cell("E13").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("F13").Value = "-";
        //        ////= IF(A13 = Inputs!$B$10; Inputs!$B$113; 0)
        //        ws.Cell("G13").Value = G13;
        //        ws.Cell("G13").Style.NumberFormat.Format = "#,##0.00";
        //        ////= B13 - D13 - E13 + G13
        //        ws.Cell("H13").Value = H13;
        //        ws.Cell("H13").Style.NumberFormat.Format = "#,##0.00";
        //        ////= 1 / (1 + Inputs!$B$21)^A13
        //        ws.Cell("I13").Value = I13;
        //        ////= H13 * J13
        //        ws.Cell("J13").Value = J13;
        //        ws.Cell("J13").Style.NumberFormat.Format = "#,##0.00";
        //        ////= H13 * Inputs!$B$20
        //        ws.Cell("K13").Value = K13;
        //        ws.Cell("K13").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("L13").Value = "-";
        //        ////= 1 / (1 + Inputs!$B$113)^A13
        //        ws.Cell("M13").Value = M13;
        //        ////= H13 * O13
        //        ws.Cell("N13").Value = N13;
        //        ws.Cell("N13").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("O13").Value = "-";
        //        ws.Cell("P13").Value = "-";

        //        //13TH ROW
        //        ws.Cell("A14").Value = 12;
        //        ws.Cell("A14").Style.NumberFormat.Format = "0";
        //        ////=IF(A14<=Inputs!$B$10;Inputs!$B$11*(1+Inputs!$B$12)^(A14-1);0)
        //        ws.Cell("B14").Value = B14;
        //        ws.Cell("B14").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("C14").Value = "-";
        //        ////= IF(A14 <= MIN(Inputs!$B$10; Inputs!$B$17); Inputs!$B$9 / MIN(Inputs!$B$10; Inputs!$B$17); 0)
        //        ws.Cell("D14").Value = D14;
        //        ws.Cell("D14").Style.NumberFormat.Format = "#,##0.00";
        //        ////=IF(A14<=Inputs!$B$10;Inputs!$B$114;0)*(1+Inputs!$B$19)^(A14-1)
        //        ws.Cell("E14").Value = E14;
        //        ws.Cell("E14").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("F14").Value = "-";
        //        ////= IF(A14 = Inputs!$B$10; Inputs!$B$114; 0)
        //        ws.Cell("G14").Value = G14;
        //        ws.Cell("G14").Style.NumberFormat.Format = "#,##0.00";
        //        ////= B14 - D14 - E14 + G14
        //        ws.Cell("H14").Value = H14;
        //        ws.Cell("H14").Style.NumberFormat.Format = "#,##0.00";
        //        ////= 1 / (1 + Inputs!$B$21)^A14
        //        ws.Cell("I14").Value = I14;
        //        ////= H14 * J14
        //        ws.Cell("J14").Value = J14;
        //        ws.Cell("J14").Style.NumberFormat.Format = "#,##0.00";
        //        ////= H14 * Inputs!$B$20
        //        ws.Cell("K14").Value = K14;
        //        ws.Cell("K14").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("L14").Value = "-";
        //        ////= 1 / (1 + Inputs!$B$114)^A14
        //        ws.Cell("M14").Value = M14;
        //        ////= H14 * O14
        //        ws.Cell("N14").Value = N14;
        //        ws.Cell("N14").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("O14").Value = "-";
        //        ws.Cell("P14").Value = "-";

        //        //14TH ROW
        //        ws.Cell("A15").Value = 13;
        //        ws.Cell("A15").Style.NumberFormat.Format = "0";
        //        ////=IF(A15<=Inputs!$B$10;Inputs!$B$11*(1+Inputs!$B$12)^(A15-1);0)
        //        ws.Cell("B15").Value = B15;
        //        ws.Cell("B15").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("C15").Value = "-";
        //        ////= IF(A15 <= MIN(Inputs!$B$10; Inputs!$B$17); Inputs!$B$9 / MIN(Inputs!$B$10; Inputs!$B$17); 0)
        //        ws.Cell("D15").Value = D15;
        //        ws.Cell("D15").Style.NumberFormat.Format = "#,##0.00";
        //        ////=IF(A15<=Inputs!$B$10;Inputs!$B$115;0)*(1+Inputs!$B$19)^(A15-1)
        //        ws.Cell("E15").Value = E15;
        //        ws.Cell("E15").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("F15").Value = "-";
        //        ////= IF(A15 = Inputs!$B$10; Inputs!$B$115; 0)
        //        ws.Cell("G15").Value = G15;
        //        ws.Cell("G15").Style.NumberFormat.Format = "#,##0.00";
        //        ////= B15 - D15 - E15 + G15
        //        ws.Cell("H15").Value = H15;
        //        ws.Cell("H15").Style.NumberFormat.Format = "#,##0.00";
        //        ////= 1 / (1 + Inputs!$B$21)^A15
        //        ws.Cell("I15").Value = I15;
        //        ////= H15 * J15
        //        ws.Cell("J15").Value = J15;
        //        ws.Cell("J15").Style.NumberFormat.Format = "#,##0.00";
        //        ////= H15 * Inputs!$B$20
        //        ws.Cell("K15").Value = K15;
        //        ws.Cell("K15").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("L15").Value = "-";
        //        ////= 1 / (1 + Inputs!$B$115)^A15
        //        ws.Cell("M15").Value = M15;
        //        ////= H15 * O15
        //        ws.Cell("N15").Value = N15;
        //        ws.Cell("N15").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("O15").Value = "-";
        //        ws.Cell("P15").Value = "-";

        //        //15TH ROW
        //        ws.Cell("A16").Value = 14;
        //        ws.Cell("A16").Style.NumberFormat.Format = "0";
        //        ////=IF(A16<=Inputs!$B$10;Inputs!$B$11*(1+Inputs!$B$12)^(A16-1);0)
        //        ws.Cell("B16").Value = B16;
        //        ws.Cell("B16").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("C16").Value = "-";
        //        ////= IF(A16 <= MIN(Inputs!$B$10; Inputs!$B$17); Inputs!$B$9 / MIN(Inputs!$B$10; Inputs!$B$17); 0)
        //        ws.Cell("D16").Value = D16;
        //        ws.Cell("D16").Style.NumberFormat.Format = "#,##0.00";
        //        ////=IF(A16<=Inputs!$B$10;Inputs!$B$116;0)*(1+Inputs!$B$19)^(A16-1)
        //        ws.Cell("E16").Value = E16;
        //        ws.Cell("E16").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("F16").Value = "-";
        //        ////= IF(A16 = Inputs!$B$10; Inputs!$B$116; 0)
        //        ws.Cell("G16").Value = G16;
        //        ws.Cell("G16").Style.NumberFormat.Format = "#,##0.00";
        //        ////= B16 - D16 - E16 + G16
        //        ws.Cell("H16").Value = H16;
        //        ws.Cell("H16").Style.NumberFormat.Format = "#,##0.00";
        //        ////= 1 / (1 + Inputs!$B$21)^A16
        //        ws.Cell("I16").Value = I16;
        //        ////= H16 * J16
        //        ws.Cell("J16").Value = J16;
        //        ws.Cell("J16").Style.NumberFormat.Format = "#,##0.00";
        //        ////= H16 * Inputs!$B$20
        //        ws.Cell("K16").Value = K16;
        //        ws.Cell("K16").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("L16").Value = "-";
        //        ////= 1 / (1 + Inputs!$B$116)^A16
        //        ws.Cell("M16").Value = M16;
        //        ////= H16 * O16
        //        ws.Cell("N16").Value = N16;
        //        ws.Cell("N16").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("O16").Value = "-";
        //        ws.Cell("P16").Value = "-";

        //        //16TH ROW
        //        ws.Cell("A17").Value = 15;
        //        ws.Cell("A17").Style.NumberFormat.Format = "0";
        //        ////=IF(A17<=Inputs!$B$10;Inputs!$B$11*(1+Inputs!$B$12)^(A17-1);0)
        //        ws.Cell("B17").Value = B17;
        //        ws.Cell("B17").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("C17").Value = "-";
        //        ////= IF(A17 <= MIN(Inputs!$B$10; Inputs!$B$17); Inputs!$B$9 / MIN(Inputs!$B$10; Inputs!$B$17); 0)
        //        ws.Cell("D17").Value = D17;
        //        ws.Cell("D17").Style.NumberFormat.Format = "#,##0.00";
        //        ////=IF(A17<=Inputs!$B$10;Inputs!$B$117;0)*(1+Inputs!$B$19)^(A17-1)
        //        ws.Cell("E17").Value = E17;
        //        ws.Cell("E17").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("F17").Value = "-";
        //        ////= IF(A17 = Inputs!$B$10; Inputs!$B$117; 0)
        //        ws.Cell("G17").Value = G17;
        //        ws.Cell("G17").Style.NumberFormat.Format = "#,##0.00";
        //        ////= B17 - D17 - E17 + G17
        //        ws.Cell("H17").Value = H17;
        //        ws.Cell("H17").Style.NumberFormat.Format = "#,##0.00";
        //        ////= 1 / (1 + Inputs!$B$21)^A17
        //        ws.Cell("I17").Value = I17;
        //        ////= H17 * J17
        //        ws.Cell("J17").Value = J17;
        //        ws.Cell("J17").Style.NumberFormat.Format = "#,##0.00";
        //        ////= H17 * Inputs!$B$20
        //        ws.Cell("K17").Value = K17;
        //        ws.Cell("K17").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("L17").Value = "-";
        //        ////= 1 / (1 + Inputs!$B$117)^A17
        //        ws.Cell("M17").Value = M17;
        //        ////= H17 * O17
        //        ws.Cell("N17").Value = N17;
        //        ws.Cell("N17").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("O17").Value = "-";
        //        ws.Cell("P17").Value = "-";

        //        //17TH ROW
        //        ws.Cell("A18").Value = 16;
        //        ws.Cell("A18").Style.NumberFormat.Format = "0";
        //        ////=IF(A18<=Inputs!$B$10;Inputs!$B$11*(1+Inputs!$B$12)^(A18-1);0)
        //        ws.Cell("B18").Value = B18;
        //        ws.Cell("B18").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("C18").Value = "-";
        //        ////= IF(A18 <= MIN(Inputs!$B$10; Inputs!$B$17); Inputs!$B$9 / MIN(Inputs!$B$10; Inputs!$B$17); 0)
        //        ws.Cell("D18").Value = D18;
        //        ws.Cell("D18").Style.NumberFormat.Format = "#,##0.00";
        //        ////=IF(A18<=Inputs!$B$10;Inputs!$B$118;0)*(1+Inputs!$B$19)^(A18-1)
        //        ws.Cell("E18").Value = E18;
        //        ws.Cell("E18").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("F18").Value = "-";
        //        ////= IF(A18 = Inputs!$B$10; Inputs!$B$118; 0)
        //        ws.Cell("G18").Value = G18;
        //        ws.Cell("G18").Style.NumberFormat.Format = "#,##0.00";
        //        ////= B18 - D18 - E18 + G18
        //        ws.Cell("H18").Value = H18;
        //        ws.Cell("H18").Style.NumberFormat.Format = "#,##0.00";
        //        ////= 1 / (1 + Inputs!$B$21)^A18
        //        ws.Cell("I18").Value = I18;
        //        ////= H18 * J18
        //        ws.Cell("J18").Value = J18;
        //        ws.Cell("J18").Style.NumberFormat.Format = "#,##0.00";
        //        ////= H18 * Inputs!$B$20
        //        ws.Cell("K18").Value = K18;
        //        ws.Cell("K18").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("L18").Value = "-";
        //        ////= 1 / (1 + Inputs!$B$118)^A18
        //        ws.Cell("M18").Value = M18;
        //        ////= H18 * O18
        //        ws.Cell("N18").Value = N18;
        //        ws.Cell("N18").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("O18").Value = "-";
        //        ws.Cell("P18").Value = "-";

        //        //18TH ROW
        //        ws.Cell("A19").Value = 17;
        //        ws.Cell("A19").Style.NumberFormat.Format = "0";
        //        ////=IF(A19<=Inputs!$B$10;Inputs!$B$11*(1+Inputs!$B$12)^(A19-1);0)
        //        ws.Cell("B19").Value = B19;
        //        ws.Cell("B19").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("C19").Value = "-";
        //        ////= IF(A19 <= MIN(Inputs!$B$10; Inputs!$B$17); Inputs!$B$9 / MIN(Inputs!$B$10; Inputs!$B$17); 0)
        //        ws.Cell("D19").Value = D19;
        //        ws.Cell("D19").Style.NumberFormat.Format = "#,##0.00";
        //        ////=IF(A19<=Inputs!$B$10;Inputs!$B$119;0)*(1+Inputs!$B$19)^(A19-1)
        //        ws.Cell("E19").Value = E19;
        //        ws.Cell("E19").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("F19").Value = "-";
        //        ////= IF(A19 = Inputs!$B$10; Inputs!$B$119; 0)
        //        ws.Cell("G19").Value = G19;
        //        ws.Cell("G19").Style.NumberFormat.Format = "#,##0.00";
        //        ////= B19 - D19 - E19 + G19
        //        ws.Cell("H19").Value = H19;
        //        ws.Cell("H19").Style.NumberFormat.Format = "#,##0.00";
        //        ////= 1 / (1 + Inputs!$B$21)^A19
        //        ws.Cell("I19").Value = I19;
        //        ////= H19 * J19
        //        ws.Cell("J19").Value = J19;
        //        ws.Cell("J19").Style.NumberFormat.Format = "#,##0.00";
        //        ////= H19 * Inputs!$B$20
        //        ws.Cell("K19").Value = K19;
        //        ws.Cell("K19").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("L19").Value = "-";
        //        ////= 1 / (1 + Inputs!$B$119)^A19
        //        ws.Cell("M19").Value = M19;
        //        ////= H19 * O19
        //        ws.Cell("N19").Value = N19;
        //        ws.Cell("N19").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("O19").Value = "-";
        //        ws.Cell("P19").Value = "-";

        //        //19TH ROW
        //        ws.Cell("A20").Value = 18;
        //        ws.Cell("A20").Style.NumberFormat.Format = "0";
        //        ////=IF(A20<=Inputs!$B$10;Inputs!$B$11*(1+Inputs!$B$12)^(A20-1);0)
        //        ws.Cell("B20").Value = B20;
        //        ws.Cell("B20").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("C20").Value = "-";
        //        ////= IF(A20 <= MIN(Inputs!$B$10; Inputs!$B$17); Inputs!$B$9 / MIN(Inputs!$B$10; Inputs!$B$17); 0)
        //        ws.Cell("D20").Value = D20;
        //        ws.Cell("D20").Style.NumberFormat.Format = "#,##0.00";
        //        ////=IF(A20<=Inputs!$B$10;Inputs!$B$120;0)*(1+Inputs!$B$19)^(A20-1)
        //        ws.Cell("E20").Value = E20;
        //        ws.Cell("E20").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("F20").Value = "-";
        //        ////= IF(A20 = Inputs!$B$10; Inputs!$B$120; 0)
        //        ws.Cell("G20").Value = G20;
        //        ws.Cell("G20").Style.NumberFormat.Format = "#,##0.00";
        //        ////= B20 - D20 - E20 + G20
        //        ws.Cell("H20").Value = H20;
        //        ws.Cell("H20").Style.NumberFormat.Format = "#,##0.00";
        //        ////= 1 / (1 + Inputs!$B$21)^A20
        //        ws.Cell("I20").Value = I20;
        //        ////= H20 * J20
        //        ws.Cell("J20").Value = J20;
        //        ws.Cell("J20").Style.NumberFormat.Format = "#,##0.00";
        //        ////= H20 * Inputs!$B$20
        //        ws.Cell("K20").Value = K20;
        //        ws.Cell("K20").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("L20").Value = "-";
        //        ////= 1 / (1 + Inputs!$B$120)^A20
        //        ws.Cell("M20").Value = M20;
        //        ////= H20 * O20
        //        ws.Cell("N20").Value = N20;
        //        ws.Cell("N20").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("O20").Value = "-";
        //        ws.Cell("P20").Value = "-";

        //        //20TH ROW
        //        ws.Cell("A21").Value = 19;
        //        ws.Cell("A21").Style.NumberFormat.Format = "0";
        //        ////=IF(A21<=Inputs!$B$10;Inputs!$B$11*(1+Inputs!$B$12)^(A21-1);0)
        //        ws.Cell("B21").Value = B21;
        //        ws.Cell("B21").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("C21").Value = "-";
        //        ////= IF(A21 <= MIN(Inputs!$B$10; Inputs!$B$17); Inputs!$B$9 / MIN(Inputs!$B$10; Inputs!$B$17); 0)
        //        ws.Cell("D21").Value = D21;
        //        ws.Cell("D21").Style.NumberFormat.Format = "#,##0.00";
        //        ////=IF(A21<=Inputs!$B$10;Inputs!$B$121;0)*(1+Inputs!$B$19)^(A21-1)
        //        ws.Cell("E21").Value = E21;
        //        ws.Cell("E21").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("F21").Value = "-";
        //        ////= IF(A21 = Inputs!$B$10; Inputs!$B$121; 0)
        //        ws.Cell("G21").Value = G21;
        //        ws.Cell("G21").Style.NumberFormat.Format = "#,##0.00";
        //        ////= B21 - D21 - E21 + G21
        //        ws.Cell("H21").Value = H21;
        //        ws.Cell("H21").Style.NumberFormat.Format = "#,##0.00";
        //        ////= 1 / (1 + Inputs!$B$21)^A21
        //        ws.Cell("I21").Value = I21;
        //        ////= H21 * J21
        //        ws.Cell("J21").Value = J21;
        //        ws.Cell("J21").Style.NumberFormat.Format = "#,##0.00";
        //        ////= H21 * Inputs!$B$20
        //        ws.Cell("K21").Value = K21;
        //        ws.Cell("K21").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("L21").Value = "-";
        //        ////= 1 / (1 + Inputs!$B$121)^A21
        //        ws.Cell("M21").Value = M21;
        //        ////= H21 * O21
        //        ws.Cell("N21").Value = N21;
        //        ws.Cell("N21").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("O21").Value = "-";
        //        ws.Cell("P21").Value = "-";

        //        //21ST ROW
        //        ws.Cell("A22").Value = 20;
        //        ws.Cell("A22").Style.NumberFormat.Format = "0";
        //        ////=IF(A22<=Inputs!$B$10;Inputs!$B$11*(1+Inputs!$B$12)^(A22-1);0)
        //        ws.Cell("B22").Value = B22;
        //        ws.Cell("B22").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("C22").Value = "-";
        //        ////= IF(A22 <= MIN(Inputs!$B$10; Inputs!$B$17); Inputs!$B$9 / MIN(Inputs!$B$10; Inputs!$B$17); 0)
        //        ws.Cell("D22").Value = D22;
        //        ws.Cell("D22").Style.NumberFormat.Format = "#,##0.00";
        //        ////=IF(A22<=Inputs!$B$10;Inputs!$B$122;0)*(1+Inputs!$B$19)^(A22-1)
        //        ws.Cell("E22").Value = E22;
        //        ws.Cell("E22").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("F22").Value = "-";
        //        ////= IF(A22 = Inputs!$B$10; Inputs!$B$122; 0)
        //        ws.Cell("G22").Value = G22;
        //        ws.Cell("G22").Style.NumberFormat.Format = "#,##0.00";
        //        ////= B22 - D22 - E22 + G22
        //        ws.Cell("H22").Value = H22;
        //        ws.Cell("H22").Style.NumberFormat.Format = "#,##0.00";
        //        ////= 1 / (1 + Inputs!$B$21)^A22
        //        ws.Cell("I22").Value = I22;
        //        ////= H22 * J22
        //        ws.Cell("J22").Value = J22;
        //        ws.Cell("J22").Style.NumberFormat.Format = "#,##0.00";
        //        ////= H22 * Inputs!$B$20
        //        ws.Cell("K22").Value = K22;
        //        ws.Cell("K22").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("L22").Value = "-";
        //        ////= 1 / (1 + Inputs!$B$122)^A22
        //        ws.Cell("M22").Value = M22;
        //        ////= H22 * O22
        //        ws.Cell("N22").Value = N22;
        //        ws.Cell("N22").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("O22").Value = "-";
        //        ws.Cell("P22").Value = "-";

        //        //22ND ROW
        //        ws.Cell("A23").Value = "-";
        //        ws.Cell("B23").Value = "-";
        //        ws.Cell("C23").Value = "-";
        //        ws.Cell("D23").Value = "-";
        //        ws.Cell("E23").Value = "-";
        //        ws.Cell("F23").Value = "-";
        //        ws.Cell("G23").Value = "-";
        //        ws.Cell("H23").Value = "-";
        //        ws.Cell("I23").Value = "-";
        //        ws.Cell("J23").Value = J23;
        //        ws.Cell("J23").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("J23").Style.Font.Bold = true;
        //        ws.Cell("J23").Style.Fill.BackgroundColor = XLColor.LightGray;
        //        ws.Cell("K23").Value = "-";
        //        ws.Cell("L23").Value = "-";
        //        ws.Cell("M23").Value = "-";
        //        ws.Cell("N23").Value = N23;
        //        ws.Cell("N23").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("N23").Style.Font.Bold = true;
        //        ws.Cell("N23").Style.Fill.BackgroundColor = XLColor.LightGray;
        //        ws.Cell("O23").Value = O23;
        //        ws.Cell("O23").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("O23").Style.Font.Bold = true;
        //        ws.Cell("O23").Style.Fill.BackgroundColor = XLColor.LightGray;
        //        ws.Cell("P23").Value = P23;
        //        ws.Cell("P23").Style.NumberFormat.Format = "#,##0.00";
        //        ws.Cell("P23").Style.Font.Bold = true;
        //        ws.Cell("P23").Style.Fill.BackgroundColor = XLColor.LightGray;

        //        //PLEASE ADD (NEW)
        //        //23RD ROW
        //        ws.Cell("A24").Value = "-";
        //        ws.Cell("B24").Value = "-";
        //        ws.Cell("C24").Value = "-";
        //        ws.Cell("D24").Value = "-";
        //        ws.Cell("E24").Value = "-";
        //        ws.Cell("F24").Value = "-";
        //        ws.Cell("G24").Value = "-";
        //        ws.Cell("H24").Value = "-";
        //        ws.Cell("I24").Value = "-";
        //        ws.Cell("J24").Value = stat1;
        //        ws.Cell("J24").Style.Font.Bold = true;

        //        if (stat1 == "REFURBISH")
        //        {
        //            ws.Cell("J24").Style.Fill.BackgroundColor = XLColor.Green;
        //        }
        //        else
        //        {
        //            ws.Cell("J24").Style.Fill.BackgroundColor = XLColor.Red;
        //        }

        //        ws.Cell("K24").Value = "-";
        //        ws.Cell("L24").Value = "-";
        //        ws.Cell("M24").Value = "-";
        //        ws.Cell("N24").Value = stat2;
        //        ws.Cell("N24").Style.Font.Bold = true;

        //        if (stat2 == "REFURBISH")
        //        {
        //            ws.Cell("N24").Style.Fill.BackgroundColor = XLColor.Green;
        //        }
        //        else
        //        {
        //            ws.Cell("N24").Style.Fill.BackgroundColor = XLColor.Red;
        //        }

        //        ws.Cell("O24").Value = "-";
        //        ws.Cell("P24").Value = "-";

        //        ws.Columns().AdjustToContents();

        //        workbook.CalculateMode = XLCalculateMode.Auto;
        //        workbook.RecalculateAllFormulas();

        //        using (var stream = new MemoryStream())
        //        {
        //            workbook.SaveAs(stream);
        //            var content = stream.ToArray();

        //            var formulas = new Dictionary<string, string>();

        //            formulas["C2"] = "Scrap Value + Scrapping Cost";
        //            formulas["F2"] = "Return to Service Cost from Dashboard";
        //            formulas["J2"] = "(C2 + F2) * I2";
        //            formulas["N2"] = "(C2 + F2) * M2";
        //            formulas["E3"] = "IF(A3 <= Lease Term; Operating Costs; 0)";
        //            formulas["N3"] = "L3 * M3";
        //            formulas["J23"] = "SUM(J2 : J22)";
        //            formulas["N23"] = "SUM(N2 : N22)";
        //            formulas["O23"] = "IF(J23 >= 0; (Net Book Value + F2); 0)";
        //            formulas["P23"] = "IF(N23 >= 0; (Net Book Value + F2); 0)";
        //            formulas["J24"] = "IF(J23 >= 0; \"Refurbish\"; \"Scrap\")";
        //            formulas["N24"] = "IF(N23 >= 0; \"Refurbish\"; \"Scrap\")";

        //            for (int r = 3; r <= 22; r++)
        //            {
        //                formulas[$"B{r}"] = $"IF(A{r} <= Lease Term; Lease Income * (1 + Escalation Rate)^(A{r} - 1) ; 0)";
        //                formulas[$"D{r}"] = $"IF(A{r} <= MIN(Lease Term; Wear & Tear Period); F2 / MIN(Lease Term; Wear & Tear Period) ; 0)";
        //                formulas[$"G{r}"] = $"IF(A{r} = Lease Term; Residual Value; 0)";
        //                formulas[$"H{r}"] = $"B{r} - D{r} - E{r} + G{r}";
        //                formulas[$"I{r}"] = $"1 / (1 + WACC (Pre-Tax) from Input)^A{r}";
        //                formulas[$"J{r}"] = $"H{r} * I{r}";
        //                formulas[$"K{r}"] = $"H{r} * Corporate Tax Rate";
        //                formulas[$"L{r}"] = $"H{r} - K{r}";
        //                formulas[$"M{r}"] = $"1 / (1 + WACC (Post-Tax) from Input)^A{r}";
        //            }

        //            for (int r = 4; r <= 22; r++)
        //            {
        //                formulas[$"E{r}"] = $"IF(A{r} <= Lease Term; Operating Costs; 0) * (1 + Operating Costs Escalation)^(A{r} - 1)";
        //                formulas[$"N{r}"] = $"H{r} * M{r}";
        //            }

        //            return Ok(new
        //            {
        //                fileName = $"{locoNumber}_DCF_Report.xlsx",
        //                fileBytes = Convert.ToBase64String(content),
        //                formulas = formulas
        //            });
        //        }
        //    }
        //}

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

        private static double ParseDoubleSafe(object value)
        {
            if (value == null) return 0;

            return double.Parse(
                value.ToString(),
                NumberStyles.Any,
                CultureInfo.InvariantCulture
            );
        }

        private static int ParseIntSafe(object value)
        {
            if (value == null) return 0;

            return Convert.ToInt32(value);
        }

        // ADD ENTIRE METHOD ↓
        (dynamic scrap, dynamic refurb, dynamic preTax, dynamic postTax, dynamic transferPre, dynamic transferPost)
        CalculateTotalsWagonSingle(dynamic w)
        {
            double grandScrap = 0;
            double grandRefurb = 0;
            double grandPre = 0;
            double grandPost = 0;
            double transferPre = 0;
            double transferPost = 0;

            var preTaxFlows = new List<double>();
            var postTaxFlows = new List<double>();

            double scrapCost = Convert.ToDouble(ParseDecimalSafe(w.ScrappingCost));
            double scrapValue = Convert.ToDouble(ParseDecimalSafe(w.ScrapValue));
            double refurbishCost = Convert.ToDouble(ParseDecimalSafe(w.TotalCost));
            double corporateTax = Convert.ToDouble(ParseDecimalSafe(w.CorporateTaxRate)) / 100;
            int leaseTerm = Convert.ToInt32(w.LeaseTerm);
            double leaseIncome = Convert.ToDouble(ParseDecimalSafe(w.LeaseIncome));
            double escalationRate = Convert.ToDouble(ParseDecimalSafe(w.EscalationRate)) / 100;
            int wearTear = Convert.ToInt32(w.WearTearPeriod);
            double operatingCosts = Convert.ToDouble(ParseDecimalSafe(w.OperatingCosts));
            double operatingEscalation = Convert.ToDouble(ParseDecimalSafe(w.OperatingCostsEscalation)) / 100;
            double residualValue = Convert.ToDouble(ParseDecimalSafe(w.ResidualValue));
            double waccPre = Convert.ToDouble(ParseDecimalSafe(w.PreTax)) / 100;
            double waccPost = Convert.ToDouble(ParseDecimalSafe(w.PostTax)) / 100;
            double netBook = Convert.ToDouble(ParseDecimalSafe(w.NetBookValue));

            int maxYears = 20;
            int minTerm = Math.Min(leaseTerm, wearTear);

            double totalScrapValue = scrapValue + scrapCost;
            double initialOutflow = (totalScrapValue + refurbishCost) * -1;

            preTaxFlows.Add(initialOutflow);
            postTaxFlows.Add(initialOutflow);

            double leaseY1 = 1 <= leaseTerm ? leaseIncome * Math.Pow(1 + escalationRate, 1 - 1) : 0;
            double refurbY1 = 1 <= minTerm ? refurbishCost / minTerm : 0;
            double opexY1 = 1 <= leaseTerm ? operatingCosts * Math.Pow(1 + operatingEscalation, 1 - 1) : 0;
            double residualY1 = 1 == leaseTerm ? residualValue : 0;
            double cashFlowY1 = leaseY1 - refurbY1 - opexY1 + residualY1;
            double discountPreY1 = 1 / Math.Pow(1 + waccPre, 1);
            double discountPostY1 = 1 / Math.Pow(1 + waccPost, 1);
            double preTaxPVY1 = cashFlowY1 * discountPreY1;
            double taxY1 = cashFlowY1 * corporateTax;
            double ebitY1 = cashFlowY1 - taxY1;
            double postTaxPVY1 = ebitY1 * discountPostY1;

            preTaxFlows.Add(preTaxPVY1);
            postTaxFlows.Add(postTaxPVY1);

            for (int year = 2; year <= maxYears; year++)
            {
                double lease = year <= leaseTerm ? leaseIncome * Math.Pow(1 + escalationRate, year - 1) : 0;
                double refurb = year <= minTerm ? refurbishCost / minTerm : 0;
                double opex = year <= leaseTerm ? operatingCosts * Math.Pow(1 + operatingEscalation, year - 1) : 0;
                double residual = year == leaseTerm ? residualValue : 0;

                double cashFlow = lease - refurb - opex + residual;

                double discountPre = 1 / Math.Pow(1 + waccPre, year);
                double discountPost = 1 / Math.Pow(1 + waccPost, year);

                double preTaxPV = cashFlow * discountPre;
                double postTaxPV = cashFlow * discountPost;

                preTaxFlows.Add(preTaxPV);
                postTaxFlows.Add(postTaxPV);
            }

            double npvPre = preTaxFlows.Sum();
            double npvPost = postTaxFlows.Sum();

            grandPre += npvPre;
            grandPost += npvPost;
            grandScrap += totalScrapValue;
            grandRefurb += refurbishCost;

            if (npvPre >= 0) transferPre += netBook + refurbishCost;
            if (npvPost >= 0) transferPost += netBook + refurbishCost;

            return (grandScrap, grandRefurb, grandPre, grandPost, transferPre, transferPost);
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

        public string? PreTax { get; set; }

        public string? UserId { get; set; }
    }

    public class InputWagon
    {
        public int WagonNumber { get; set; }
        public string? WagonType { get; set; }
        public string? NetBookValue { get; set; }
        public string? ScrapValue { get; set; }
        public string? ScrappingCost { get; set; }
        public string? NewScrapValue { get; set; }
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
        public string? NewScrapValue { get; set; }
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

        public int LeaseTerm { get; set; }

        public string EscalationRate { get; set; } = null!;

        public int UseAfterRefurbish { get; set; }

        public int WearTearPeriod { get; set; }

        public string OperatingCosts { get; set; } = null!;

        public string OperatingCostsEscalation { get; set; } = null!;

        public string CorporateTaxRate { get; set; } = null!;

        public string UserId { get; set; } = null!;
    }

    public class ScrapCalRequest
    {
        public string? ScrapValue { get; set; }

        public string? ScrappingCost { get; set; }
    }
}
