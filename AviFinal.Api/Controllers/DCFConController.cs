
using AviFinal.Api.Models;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.RegularExpressions;

namespace AviAppFinal.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DCFConController : ControllerBase
    {
        private readonly AviDbContext _context;

        public DCFConController(AviDbContext context)
        {
            _context = context;
        }

        [HttpGet("getAssetType")]
        public async Task<IActionResult> GetInputWagons()
        {
            var assetType = await _context.AssetTypeSetups
                .Select(w => new
                {
                    w.AssetType
                })
                .ToListAsync();

            return Ok(assetType);
        }

        // ADJUSTED ↓
        [HttpGet("generateSingle/{assetType}")]
        public async Task<IActionResult> GenerateSingle(string assetType)
        {

            _context.Database.SetCommandTimeout(200);

            var inputWagon = await _context.WagonInputs
                .Where(w => w.WagonType == assetType)
                .AsNoTracking()
                .ToListAsync();

            var inputLoco = await _context.LocoInputs
                .Where(w => w.LocoType == assetType)
                .AsNoTracking()
                .ToListAsync();

            try
            {
                using (var workbook = new XLWorkbook())
                {

                    var ws = workbook.Worksheets.Add($"DCF_{assetType}");

                    // HEADER ROW
                    ws.Cell("A1").Value = "Asset Type";
                    ws.Cell("B1").Value = "Asset Number";
                    ws.Cell("C1").Value = "Market Value";
                    ws.Cell("D1").Value = "Return to Service Cost";
                    ws.Cell("E1").Value = "Present Value (Pre-Tax)";
                    ws.Cell("F1").Value = "Present Value (Post-Tax)";
                    ws.Cell("G1").Value = "Benchmark Value (Pre-Tax)";
                    ws.Cell("H1").Value = "Benchmark Value (Post-Tax)";
                    ws.Range("A1:H1").Style.Font.Bold = true;
                    ws.Range("A1:H1").Style.Fill.BackgroundColor = XLColor.LightGray;

                    var allMarket = new List<double>();
                    var allRefurbish = new List<double>();
                    var allPreTax = new List<double>();
                    var allPostTax = new List<double>();
                    var allTransPre = new List<double>();
                    var allTransPost = new List<double>();

                    int row = 2;
                    int endRow = 0;

                    if (inputWagon.Count != 0)
                    {
                        foreach (var wagon in inputWagon)
                        {
                            string wagonNum = wagon.WagonNumber.ToString();

                            var t = CalculateTotalsWagonSingle(wagon);
                            WriteAssetBlockSingle(ws, ref row, assetType, wagonNum,
                                    t.market, t.refurb, t.preTax, t.postTax, t.transferPre, t.transferPost);

                            allMarket.Add(t.market);
                            allRefurbish.Add(t.refurb);
                            allPreTax.Add(t.preTax);
                            allPostTax.Add(t.postTax);
                            allTransPre.Add(t.transferPre);
                            allTransPost.Add(t.transferPost);
                        }

                        endRow = inputWagon.Count + inputWagon.Count + 1;
                    }
                    else if (inputLoco.Count != 0)
                    {
                        foreach (var loco in inputLoco)
                        {
                            string locoNum = loco.LocoNumber.ToString();

                            var t = CalculateTotalsLocoSingle(loco);
                            WriteAssetBlockSingle(ws, ref row, assetType, locoNum,
                                    t.market, t.refurb, t.preTax, t.postTax, t.transferPre, t.transferPost);

                            allMarket.Add(t.market);
                            allRefurbish.Add(t.refurb);
                            allPreTax.Add(t.preTax);
                            allPostTax.Add(t.postTax);
                            allTransPre.Add(t.transferPre);
                            allTransPost.Add(t.transferPost);
                        }

                        endRow = inputLoco.Count + inputLoco.Count + 1;
                    }

                    ws.Cell($"A{endRow + 1}").Value = "Asset Type";
                    ws.Cell($"B{endRow + 1}").Value = "-";
                    ws.Cell($"C{endRow + 1}").Value = "Total Market Value";
                    ws.Cell($"D{endRow + 1}").Value = "Total Return to Service Cost";
                    ws.Cell($"E{endRow + 1}").Value = "Total PV (Pre-Tax)";
                    ws.Cell($"F{endRow + 1}").Value = "Total PV (Post-Tax)";
                    ws.Cell($"G{endRow + 1}").Value = "Total BV (Pre-Tax)";
                    ws.Cell($"H{endRow + 1}").Value = "Total BV (Post-Tax)";

                    double marketTotal = allMarket.Sum();
                    double refurbTotal = allRefurbish.Sum();
                    double preTaxTotal = allPreTax.Sum();
                    double postTaxTotal = allPostTax.Sum();
                    double transPreTotal = allTransPre.Sum();
                    double transPostTotal = allTransPost.Sum();

                    ws.Cell($"A{endRow + 2}").Value = assetType;
                    ws.Cell($"B{endRow + 2}").Value = "-";
                    ws.Cell($"C{endRow + 2}").Value = marketTotal;
                    ws.Cell($"C{endRow + 2}").Style.NumberFormat.Format = "#,##0.00";
                    ws.Cell($"D{endRow + 2}").Value = refurbTotal;
                    ws.Cell($"D{endRow + 2}").Style.NumberFormat.Format = "#,##0.00";
                    ws.Cell($"E{endRow + 2}").Value = preTaxTotal;
                    ws.Cell($"E{endRow + 2}").Style.NumberFormat.Format = "#,##0.00";
                    ws.Cell($"F{endRow + 2}").Value = postTaxTotal;
                    ws.Cell($"F{endRow + 2}").Style.NumberFormat.Format = "#,##0.00";
                    ws.Cell($"G{endRow + 2}").Value = transPreTotal;
                    ws.Cell($"G{endRow + 2}").Style.NumberFormat.Format = "#,##0.00";
                    ws.Cell($"H{endRow + 2}").Value = transPostTotal;
                    ws.Cell($"H{endRow + 2}").Style.NumberFormat.Format = "#,##0.00";

                    string statPre = preTaxTotal >= 0 ? "REFURBISH" : "SCRAP";
                    string statPost = postTaxTotal >= 0 ? "REFURBISH" : "SCRAP";

                    ws.Cell($"A{endRow + 3}").Value = "-";
                    ws.Cell($"B{endRow + 3}").Value = "-";
                    ws.Cell($"C{endRow + 3}").Value = "-";
                    ws.Cell($"D{endRow + 3}").Value = "-";
                    ws.Cell($"E{endRow + 3}").Value = statPre;
                    ws.Cell($"E{endRow + 3}").Style.Font.Bold = true;
                    ws.Cell($"E{endRow + 3}").Style.Fill.BackgroundColor =
                        statPre == "REFURBISH" ? XLColor.Green : XLColor.Red;
                    ws.Cell($"F{endRow + 3}").Value = statPost;
                    ws.Cell($"F{endRow + 3}").Style.Font.Bold = true;
                    ws.Cell($"F{endRow + 3}").Style.Fill.BackgroundColor =
                        statPost == "REFURBISH" ? XLColor.Green : XLColor.Red;
                    ws.Cell($"G{endRow + 3}").Value = "-";
                    ws.Cell($"H{endRow + 3}").Value = "-";

                    ws.Columns().AdjustToContents();

                    workbook.CalculateMode = XLCalculateMode.Auto;
                    workbook.RecalculateAllFormulas();

                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        var content = stream.ToArray();

                        var formulas = new Dictionary<string, string>();

                        int numAssets = 0;

                        if (inputWagon.Count != 0)
                        {
                            numAssets = inputWagon.Count;
                        }
                        else if (inputLoco.Count != 0)
                        {
                            numAssets = inputLoco.Count;
                        }

                        formulas["C2"] = "Market Value from the Dashboard for this Asset";
                        formulas["D2"] = "Return to Service Cost from the Dashboard for this Asset";
                        formulas["E2"] = "Sum of all Present Values (Pre-Tax) for this Asset";
                        formulas["F2"] = "Sum of all Present Values (Post-Tax) for this Asset";
                        formulas["G2"] = "Benchmark Value (Pre-Tax) for this Asset";
                        formulas["H2"] = "Benchmark Value (Post-Tax) for this Asset";
                        formulas["E3"] = "IF(E2 >= 0; \"Refurbish\"; \"Scrap\")";
                        formulas["F3"] = "IF(F2 >= 0; \"Refurbish\"; \"Scrap\")";

                        for (int r = 2; r <= numAssets; r++)
                        {
                            formulas[$"C{r * 2}"] = "Market Value from the Dashboard for this Asset";
                            formulas[$"D{r * 2}"] = "Return to Service Cost from the Dashboard for this Asset";
                            formulas[$"E{r * 2}"] = "Sum of all Present Values (Pre-Tax) for this Asset";
                            formulas[$"F{r * 2}"] = "Sum of all Present Values (Post-Tax) for this Asset";
                            formulas[$"G{r * 2}"] = "Benchmark Value (Pre-Tax) for this Asset";
                            formulas[$"H{r * 2}"] = "Benchmark Value (Post-Tax) for this Asset";
                            formulas[$"E{(r * 2) + 1}"] = $"IF(E{r * 2} >= 0; \"Refurbish\"; \"Scrap\")";
                            formulas[$"F{(r * 2) + 1}"] = $"IF(F{r * 2} >= 0; \"Refurbish\"; \"Scrap\")";
                        }

                        return Ok(new
                        {
                            fileName = $"{assetType}_DCF_Report.xlsx",
                            fileBytes = Convert.ToBase64String(content),
                            formulas = formulas
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // ADJUSTED ↓
        [HttpGet("generateAll")]
        public async Task<IActionResult> GenerateAll()
        {
            _context.Database.SetCommandTimeout(300);

            var assetType = await _context.AssetTypeSetups
                .AsNoTracking()
                .ToListAsync();

            var inputWagon = await _context.WagonInputs
                .AsNoTracking()
                .ToListAsync();

            var inputLoco = await _context.LocoInputs
                .AsNoTracking()
                .ToListAsync();

            try
            {
                if (assetType.Count != 0 && inputWagon.Count != 0 && inputLoco.Count != 0)
                {
                    using (var workbook = new XLWorkbook())
                    {
                        var ws = workbook.Worksheets.Add($"DCF_AllAssetTypes");

                        // HEADER
                        ws.Cell("A1").Value = "Asset Type";
                        ws.Cell("B1").Value = "Type of Assets";
                        ws.Cell("C1").Value = "Market Value";
                        ws.Cell("D1").Value = "Return to Service Cost";
                        ws.Cell("E1").Value = "Present Value (Pre-Tax)";
                        ws.Cell("F1").Value = "Present Value (Post-Tax)";
                        ws.Cell("G1").Value = "Benchmark Value (Pre-Tax)";
                        ws.Cell("H1").Value = "Benchmark Value (Post-Tax)";
                        ws.Range("A1:H1").Style.Font.Bold = true;
                        ws.Range("A1:H1").Style.Fill.BackgroundColor = XLColor.LightGray;

                        var wagonLookup = inputWagon
                            .GroupBy(w => w.WagonType)
                            .ToDictionary(g => g.Key, g => g.ToList());

                        var locoLookup = inputLoco
                            .GroupBy(l => l.LocoType)
                            .ToDictionary(g => g.Key, g => g.ToList());

                        int row = 2;

                        foreach (var a in assetType)
                        {
                            if (wagonLookup.TryGetValue(a.AssetType, out var wagons))
                            {
                                var t = CalculateTotalsWagon(wagons);
                                WriteAssetBlock(ws, ref row, a.AssetType, "Wagon",
                                    t.market, t.refurb, t.preTax, t.postTax, t.transferPre, t.transferPost);
                            }
                            else if (locoLookup.TryGetValue(a.AssetType, out var locos))
                            {
                                var t = CalculateTotalsLoco(locos);
                                WriteAssetBlock(ws, ref row, a.AssetType, "Locomotive",
                                    t.market, t.refurb, t.preTax, t.postTax, t.transferPre, t.transferPost);
                            }
                        }

                        ws.Columns().AdjustToContents();

                        workbook.CalculateMode = XLCalculateMode.Auto;
                        workbook.RecalculateAllFormulas();

                        using (var stream = new MemoryStream())
                        {
                            workbook.SaveAs(stream);
                            var content = stream.ToArray();

                            var formulas = new Dictionary<string, string>();

                            int numAssetTypes = assetType.Count;

                            formulas["C2"] = "Sum of each Market Value per Asset within this Asset Type";
                            formulas["D2"] = "Sum of each Return to Service Cost per Asset within this Asset Type";
                            formulas["E2"] = "Sum of all Present Values (Pre-Tax) per Asset within this Asset Type";
                            formulas["F2"] = "Sum of all Present Values (Post-Tax) per Asset within this Asset Type";
                            formulas["G2"] = "Sum of each Benchmark Value (Pre-Tax) per Asset within this Asset Type";
                            formulas["H2"] = "Sum of each Benchmark Value (Post-Tax) per Asset within this Asset Type";
                            formulas["E3"] = "IF(E2 >= 0; \"Refurbish\"; \"Scrap\")";
                            formulas["F3"] = "IF(F2 >= 0; \"Refurbish\"; \"Scrap\")";

                            for (int r = 2; r <= numAssetTypes; r++)
                            {
                                formulas[$"C{r * 2}"] = "Sum of each Market Value per Asset within this Asset Type";
                                formulas[$"D{r * 2}"] = "Sum of each Return to Service Cost per Asset within this Asset Type";
                                formulas[$"E{r * 2}"] = "Sum of all Present Values (Pre-Tax) per Asset within this Asset Type";
                                formulas[$"F{r * 2}"] = "Sum of all Present Values (Post-Tax) per Asset within this Asset Type";
                                formulas[$"G{r * 2}"] = "Sum of each Benchmark Value (Pre-Tax) per Asset within this Asset Type";
                                formulas[$"H{r * 2}"] = "Sum of each Benchmark Value (Post-Tax) per Asset within this Asset Type";
                                formulas[$"E{(r * 2) + 1}"] = $"IF(E{r * 2} >= 0; \"Refurbish\"; \"Scrap\")";
                                formulas[$"F{(r * 2) + 1}"] = $"IF(F{r * 2} >= 0; \"Refurbish\"; \"Scrap\")";
                            }

                            return Ok(new
                            {
                                fileName = "AllAssetTypes_DCF_Report.xlsx",
                                fileBytes = Convert.ToBase64String(content),
                                formulas = formulas
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }

            return BadRequest("No data found for asset type");
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

        // ADJUSTED ↓
        (dynamic market, dynamic refurb, dynamic preTax, dynamic postTax, dynamic transferPre, dynamic transferPost)
        CalculateTotalsWagon<T>(List<T> items) where T : class
        {
            double grandMarket = 0;
            double grandRefurb = 0;
            double grandPre = 0;
            double grandPost = 0;
            double transferPre = 0;
            double transferPost = 0;

            foreach (dynamic w in items)
            {
                var preTaxFlows = new List<double>();
                var postTaxFlows = new List<double>();

                //double scrapCost = Convert.ToDouble(ParseDecimalSafe(w.ScrappingCost));
                double marketValue = Convert.ToDouble(ParseDecimalSafe(w.MarketValue));
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
                double benchmarkValue = Convert.ToDouble(ParseDecimalSafe(w.BenchmarkValue));

                int maxYears = 20;
                int minTerm = Math.Min(leaseTerm, wearTear);

                double totalMarketValue = marketValue;
                double initialOutflow = (totalMarketValue + refurbishCost) * -1;

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
                grandMarket += totalMarketValue;
                grandRefurb += refurbishCost;

                if (npvPre >= 0) transferPre += benchmarkValue + refurbishCost;
                if (npvPost >= 0) transferPost += benchmarkValue + refurbishCost;
            }

            return (grandMarket, grandRefurb, grandPre, grandPost, transferPre, transferPost);
        }

        // ADJUSTED ↓
        (dynamic market, dynamic refurb, dynamic preTax, dynamic postTax, dynamic transferPre, dynamic transferPost)
        CalculateTotalsLoco<T>(List<T> items) where T : class
        {
            double grandMarket = 0;
            double grandRefurb = 0;
            double grandPre = 0;
            double grandPost = 0;
            double transferPre = 0;
            double transferPost = 0;

            foreach (dynamic w in items)
            {
                var preTaxFlows = new List<double>();
                var postTaxFlows = new List<double>();

                //double scrapCost = Convert.ToDouble(ParseDecimalSafe(w.ScrappingCost));
                double marketValue = Convert.ToDouble(ParseDecimalSafe(w.MarketValue));
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
                double benchmarkValue = Convert.ToDouble(ParseDecimalSafe(w.BenchmarkValue));

                int maxYears = 20;
                int minTerm = Math.Min(leaseTerm, wearTear);

                double totalMarketValue = marketValue;
                double initialOutflowPre = (totalMarketValue + refurbishCost) * -1;
                double initialOutflowPost = -totalMarketValue * 1 * (1 - corporateTax); // ← ADJUST

                preTaxFlows.Add(initialOutflowPre);
                postTaxFlows.Add(initialOutflowPost);

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
                grandMarket += totalMarketValue;
                grandRefurb += refurbishCost;

                if (npvPre >= 0) transferPre += benchmarkValue + refurbishCost;
                if (npvPost >= 0) transferPost += benchmarkValue + refurbishCost;
            }

            return (grandMarket, grandRefurb, grandPre, grandPost, transferPre, transferPost);
        }

        // ADJUSTED ↓
        (dynamic market, dynamic refurb, dynamic preTax, dynamic postTax, dynamic transferPre, dynamic transferPost)
        CalculateTotalsWagonSingle(dynamic w)
        {
            double grandMarket = 0;
            double grandRefurb = 0;
            double grandPre = 0;
            double grandPost = 0;
            double transferPre = 0;
            double transferPost = 0;

            var preTaxFlows = new List<double>();
            var postTaxFlows = new List<double>();

            //double scrapCost = Convert.ToDouble(ParseDecimalSafe(w.ScrappingCost));
            double marketValue = Convert.ToDouble(ParseDecimalSafe(w.MarketValue));
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
            double benchmarkValue = Convert.ToDouble(ParseDecimalSafe(w.BenchmarkValue));

            int maxYears = 20;
            int minTerm = Math.Min(leaseTerm, wearTear);

            double totalMarketValue = marketValue;
            double initialOutflow = (totalMarketValue + refurbishCost) * -1;

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
            grandMarket += totalMarketValue;
            grandRefurb += refurbishCost;

            if (npvPre >= 0) transferPre += benchmarkValue + refurbishCost;
            if (npvPost >= 0) transferPost += benchmarkValue + refurbishCost;

            return (grandMarket, grandRefurb, grandPre, grandPost, transferPre, transferPost);
        }

        // ADJUSTED ↓
        (dynamic market, dynamic refurb, dynamic preTax, dynamic postTax, dynamic transferPre, dynamic transferPost)
        CalculateTotalsLocoSingle(dynamic w)
        {
            double grandMarket = 0;
            double grandRefurb = 0;
            double grandPre = 0;
            double grandPost = 0;
            double transferPre = 0;
            double transferPost = 0;

            var preTaxFlows = new List<double>();
            var postTaxFlows = new List<double>();

            //double scrapCost = Convert.ToDouble(ParseDecimalSafe(w.ScrappingCost));
            double marketValue = Convert.ToDouble(ParseDecimalSafe(w.MarketValue));
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
            double benchmarkValue = Convert.ToDouble(ParseDecimalSafe(w.BenchmarkValue));

            int maxYears = 20;
            int minTerm = Math.Min(leaseTerm, wearTear);

            double totalMarketValue = marketValue;
            double initialOutflowPre = (totalMarketValue + refurbishCost) * -1;
            double initialOutflowPost = -totalMarketValue * 1 * (1 - corporateTax); // ← ADJUST

            preTaxFlows.Add(initialOutflowPre);
            postTaxFlows.Add(initialOutflowPost);

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
            grandMarket += totalMarketValue;
            grandRefurb += refurbishCost;

            if (npvPre >= 0) transferPre += benchmarkValue + refurbishCost;
            if (npvPost >= 0) transferPost += benchmarkValue + refurbishCost;

            return (grandMarket, grandRefurb, grandPre, grandPost, transferPre, transferPost);
        }

        // ADJUSTED ↓
        void WriteAssetBlock(IXLWorksheet ws, ref int row, string assetType, string assetCategory,
            double market, double refurb, double preTax, double postTax, double transferPre, double transferPost)
        {
            string statPre = preTax >= 0 ? "REFURBISH" : "SCRAP";
            string statPost = postTax >= 0 ? "REFURBISH" : "SCRAP";

            // VALUES ROW
            ws.Cell(row, 1).Value = assetType;
            ws.Cell(row, 2).Value = assetCategory;
            ws.Cell(row, 3).Value = (Math.Round(market, MidpointRounding.AwayFromZero));
            ws.Cell(row, 4).Value = (Math.Round(refurb, MidpointRounding.AwayFromZero));
            ws.Cell(row, 5).Value = (Math.Round(preTax, MidpointRounding.AwayFromZero));
            ws.Cell(row, 6).Value = (Math.Round(postTax, MidpointRounding.AwayFromZero));
            ws.Cell(row, 7).Value = (Math.Round(transferPre, MidpointRounding.AwayFromZero));
            ws.Cell(row, 8).Value = (Math.Round(transferPost, MidpointRounding.AwayFromZero));

            ws.Range(row, 3, row, 8).Style.NumberFormat.Format = "#,##0.00";

            // DECISION ROW
            ws.Cell(row + 1, 1).Value = "-";
            ws.Cell(row + 1, 2).Value = "-";
            ws.Cell(row + 1, 3).Value = "-";
            ws.Cell(row + 1, 4).Value = "-";

            StyleDecisionCell(ws.Cell(row + 1, 5), statPre);
            StyleDecisionCell(ws.Cell(row + 1, 6), statPost);

            ws.Cell(row + 1, 7).Value = "-";
            ws.Cell(row + 1, 8).Value = "-";

            row += 2;
        }

        // ADJUSTED ↓
        void WriteAssetBlockSingle(IXLWorksheet ws, ref int row, string assetType, string assetNumber,
            double market, double refurb, double preTax, double postTax, double transferPre, double transferPost)
        {
            string statPre = preTax >= 0 ? "REFURBISH" : "SCRAP";
            string statPost = postTax >= 0 ? "REFURBISH" : "SCRAP";

            // VALUES ROW
            ws.Cell(row, 1).Value = assetType;
            ws.Cell(row, 2).Value = assetNumber;
            ws.Cell(row, 3).Value = (Math.Round(market, MidpointRounding.AwayFromZero));
            ws.Cell(row, 4).Value = (Math.Round(refurb, MidpointRounding.AwayFromZero));
            ws.Cell(row, 5).Value = (Math.Round(preTax, MidpointRounding.AwayFromZero));
            ws.Cell(row, 6).Value = (Math.Round(postTax, MidpointRounding.AwayFromZero));
            ws.Cell(row, 7).Value = (Math.Round(transferPre, MidpointRounding.AwayFromZero));
            ws.Cell(row, 8).Value = (Math.Round(transferPost, MidpointRounding.AwayFromZero));

            ws.Range(row, 3, row, 8).Style.NumberFormat.Format = "#,##0.00";

            // DECISION ROW
            ws.Cell(row + 1, 1).Value = "-";
            ws.Cell(row + 1, 2).Value = "-";
            ws.Cell(row + 1, 3).Value = "-";
            ws.Cell(row + 1, 4).Value = "-";

            StyleDecisionCell(ws.Cell(row + 1, 5), statPre);
            StyleDecisionCell(ws.Cell(row + 1, 6), statPost);

            ws.Cell(row + 1, 7).Value = "-";
            ws.Cell(row + 1, 8).Value = "-";

            row += 2;
        }


        void StyleDecisionCell(IXLCell cell, string value)
        {
            cell.Value = value;
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor =
                value == "REFURBISH" ? XLColor.Green : XLColor.Red;
        }
    }
}
