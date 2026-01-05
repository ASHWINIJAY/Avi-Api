using AviFinal.Api.Models;
using iText.IO.Font.Constants;
using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Crypto;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Canvas.Draw;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.StyledXmlParser.Jsoup.Helper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Path = System.IO.Path;
using TextAlignment = iText.Layout.Properties.TextAlignment;

namespace AviAppFinal.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CertPdfController : ControllerBase
    {
        private readonly AviDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;

        public CertPdfController(AviDbContext context, IWebHostEnvironment env, IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _env = env;
            _config = config;
            _httpClientFactory = httpClientFactory;
        }


        [HttpPost("GenerateAndSaveCertPdf")]
        public async Task<IActionResult> GenerateAndSaveCertPdf([FromBody] CertPdfRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.WagonNumber))
                return BadRequest("Invalid request: WagonNumber is required.");

            _context.Database.SetCommandTimeout(180);

            if (!int.TryParse(request.WagonNumber, out int wagonNumber))
                return BadRequest("Invalid WagonNumber format.");

            string userId = request.UserId ?? "";

            // Fetch related records (non-tracking reads where appropriate)
            var assessor = string.IsNullOrWhiteSpace(userId) ? null :
                await _context.LeaseCoUsers.AsNoTracking().FirstOrDefaultAsync(a => a.UserId == userId);

            var master = await _context.MasterWagons.AsNoTracking().FirstOrDefaultAsync(m => m.WagonNumber == wagonNumber);
            var model = await _context.WagonInfoCaptures.AsNoTracking().FirstOrDefaultAsync(p => p.WagonNumber == wagonNumber);

            var dash = await _context.WagonDashboards.AsNoTracking().FirstOrDefaultAsync(p => p.WagonNumber == wagonNumber); //PLEASE ADD (NEW)

            int? score = dash?.ConditionScore;

            var condition = await _context.ConditionRatings.AsNoTracking().FirstOrDefaultAsync(c => c.Score == score);

            var input = await _context.WagonInputs.AsNoTracking().FirstOrDefaultAsync(i => i.WagonNumber == wagonNumber);

            // Validate basic presence
            if (model == null)
                return BadRequest($"No WagonInfoCaptures record found for wagon {wagonNumber}.");

            if (input == null)
                return BadRequest($"Inputs for wagon {wagonNumber} was not found. Please contact administrator for assistence.");

            // Ensure directory exists
            string folderPath = Path.Combine(_env.WebRootPath, "InspectionPdf", "Wagons", "CertPdf");
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            // Create sanitized filename (timestamp + guid to avoid collisions)
            string safeGroup = SanitizeFileName(model?.WagonGroup ?? "Group");
            string fileName = $"{wagonNumber}_{safeGroup}_Cert_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N").Substring(0, 8)}.pdf";
            string filePath = Path.Combine(folderPath, fileName);

            try
            {
                using (var writer = new PdfWriter(filePath))
                using (var pdf = new iText.Kernel.Pdf.PdfDocument(writer))
                using (var document = new Document(pdf, PageSize.A4))
                {
                    var bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                    var regular = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
                    float pageWidth = pdf.GetDefaultPageSize().GetWidth();
                    float pageHeight = pdf.GetDefaultPageSize().GetHeight();

                    // -------------------- HEADER --------------------
                    string headerPath = Path.Combine(_env.WebRootPath, "Images", "CertLogo1.png");
                    float headerHeight = 0f;
                    if (System.IO.File.Exists(headerPath))
                    {
                        var headerImg = new Image(ImageDataFactory.Create(headerPath));
                        headerImg.ScaleToFit(pageWidth, headerImg.GetImageHeight());
                        headerHeight = headerImg.GetImageScaledHeight();
                        headerImg.SetFixedPosition(0, pageHeight - headerHeight); // flush top
                        document.Add(headerImg);
                    }

                    // -------------------- WAGON PHOTO --------------------
                    if (!string.IsNullOrWhiteSpace(model?.WagonPhoto))
                    {
                        string fullImagePath = Path.Combine(_env.WebRootPath, model.WagonPhoto.TrimStart('/', '\\'));
                        if (System.IO.File.Exists(fullImagePath))
                        {
                            Image wagonImg = new Image(ImageDataFactory.Create(fullImagePath));
                            wagonImg.ScaleToFit(500f, 250f);
                            wagonImg.SetHorizontalAlignment(HorizontalAlignment.CENTER);

                            Div photoDiv = new Div()
                                .Add(wagonImg)
                                .SetMarginTop(headerHeight + 15f) // leave some space below header
                                .SetMarginBottom(15f)
                                .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER);

                            document.Add(photoDiv);
                        }
                    }

                    // -------------------- DESCRIPTION HEADING --------------------
                    document.Add(new Paragraph("DESCRIPTION OF ASSETS")
                        .SetFont(bold)
                        .SetFontSize(25)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetMarginTop(10f)
                        .SetMarginBottom(15f));

                    // -------------------- ASSET TABLE --------------------
                    var table = new Table(new float[] { 1.5f, 2f })
                        .UseAllAvailableWidth()
                        .SetMarginBottom(20f)
                        .SetBorder(Border.NO_BORDER);

                    var headerBg = new DeviceRgb(30, 60, 110);
                    var oddRowBg = new DeviceRgb(245, 245, 245);
                    var evenRowBg = ColorConstants.WHITE;

                    Style headerStyle = new Style()
                        .SetFont(bold).SetFontSize(12)
                        .SetFontColor(ColorConstants.WHITE)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetPadding(8);

                    Style labelStyle = new Style()
                        .SetFont(bold).SetFontSize(11)
                        .SetFontColor(ColorConstants.BLACK)
                        .SetTextAlignment(TextAlignment.LEFT)
                        .SetPadding(6);

                    Style valueStyle = new Style()
                        .SetFont(regular).SetFontSize(11)
                        .SetFontColor(ColorConstants.BLACK)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetPadding(6);

                    table.AddCell(new Cell().Add(new Paragraph("ASSET FIELD").AddStyle(headerStyle)).SetBackgroundColor(headerBg).SetBorder(Border.NO_BORDER));
                    table.AddCell(new Cell().Add(new Paragraph("VALUE").AddStyle(headerStyle)).SetBackgroundColor(headerBg).SetBorder(Border.NO_BORDER));

                    int rowIndex = 0;
                    void AddRow(string label, string value)
                    {
                        var bg = (rowIndex % 2 == 0) ? oddRowBg : evenRowBg;
                        table.AddCell(new Cell().Add(new Paragraph(label).AddStyle(labelStyle)).SetBackgroundColor(bg).SetBorder(Border.NO_BORDER));
                        table.AddCell(new Cell().Add(new Paragraph(value).AddStyle(valueStyle)).SetBackgroundColor(bg).SetBorder(Border.NO_BORDER));
                        rowIndex++;
                    }

                    //PLEASE ADD
                    // --- compute lift/barrel costs robustly ---
                    decimal liftCost = 0m;
                    decimal barrelCost = 0m;

                    // Use case-insensitive checks and handle a few possible string values
                    if (!string.IsNullOrWhiteSpace(model?.LiftLapsed))
                    {
                        var lift = model.LiftLapsed.Trim().ToLowerInvariant();
                        if (lift == "Yes" || lift == "y" || lift == "true") liftCost = 420982m;
                    }

                    if (!string.IsNullOrWhiteSpace(model?.BarrelLapsed))
                    {
                        var barrel = model.BarrelLapsed.Trim().ToLowerInvariant();
                        if (barrel == "Yes" || barrel == "y" || barrel == "true") barrelCost = 351893m;
                    }

                    decimal liftBarrelTotal = liftCost + barrelCost;

                    //PLEASE ADD
                    // --- parse dashboard numeric values safely (strip currency and commas) ---
                    decimal refVal = ParseDecimalSafe(dash?.RefurbishValue);
                    decimal misVal = ParseDecimalSafe(dash?.MissingValue);
                    decimal repVal = ParseDecimalSafe(dash?.ReplaceValue);
                    decimal labVal = ParseDecimalSafe(dash?.TotalLaborValue);

                    decimal asset = ParseDecimalSafe(dash?.AssetValue); //PLEASE ADD (NEW)

                    // FIXED: previously you added refVal twice — use repVal here
                    decimal repairValue = refVal + misVal + repVal + labVal + liftBarrelTotal;
                    string totalRepair = "R" + repairValue.ToString("N2", new CultureInfo("en-ZA"));

                    //PLEASE ADD
                    // market value (from master)
                    decimal marketValue = ParseDecimalSafe(master?.MarketValue);

                    string assetValue = "R" + asset.ToString("N2", new CultureInfo("en-ZA"));

                    //PLEASE ADD
                    // net book value: keep original format if not parseable
                    string netBookValue = "#N/A";
                    if (!string.IsNullOrWhiteSpace(model?.NetBookValue) && model.NetBookValue != "#N/A")
                    {
                        // try to parse replacing non-numeric chars, but fallback to original
                        var sanitized = model.NetBookValue.Replace("R", "").Replace(" ", "").Replace(".", "").Replace(",", ".");
                        if (decimal.TryParse(sanitized, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal nb))
                            netBookValue = "R" + nb.ToString("N2", new CultureInfo("en-ZA"));
                        else
                            netBookValue = model.NetBookValue;
                    }

                    //PLEASE ADD
                    // City via reverse geocode (best-effort)
                    string city = "Not Captured";
                    if (double.TryParse(model?.GpsLatitude, NumberStyles.Any, CultureInfo.InvariantCulture, out double latitude)
                        && double.TryParse(model?.GpsLongitude, NumberStyles.Any, CultureInfo.InvariantCulture, out double longitude))
                    {
                        var resolved = await GetCityFromCoordinatesAsync(latitude, longitude);
                        if (!string.IsNullOrWhiteSpace(resolved) && !resolved.StartsWith("Error", StringComparison.InvariantCultureIgnoreCase))
                            city = resolved;
                    }

                    double scrapCost = Convert.ToDouble(input.ScrappingCost);
                    double scrapValue = Convert.ToDouble(input.ScrapValue);
                    double refurbishCost = Convert.ToDouble(input.RefurbishmentCost);
                    double corporateTax = Convert.ToDouble(input.CorporateTaxRate) / 100;
                    int leaseTerm = Convert.ToInt32(input.LeaseTerm);
                    double leaseIncome = Convert.ToDouble(input.LeaseIncome);
                    double escalationRate = Convert.ToDouble(input.EscalationRate) / 100;
                    int wearTear = Convert.ToInt32(input.WearTearPeriod);
                    double operatingCosts = Convert.ToDouble(input.OperatingCosts);
                    double operatingEscalation = Convert.ToDouble(input.OperatingCostsEscalation) / 100;
                    double residualValue = Convert.ToDouble(input.ResidualValue);
                    double waccPre = Convert.ToDouble(ParseDecimalSafe(input.PreTax)) / 100;
                    double waccPost = Convert.ToDouble(ParseDecimalSafe(input.PostTax)) / 100;
                    double netBook = Convert.ToDouble(input.NetBookValue);

                    int maxPeriods = 20;
                    int minTerm = Math.Min(leaseTerm, wearTear);

                    double[] J = new double[maxPeriods + 1];
                    double[] N = new double[maxPeriods + 1];

                    double totalScrapValue = scrapValue + scrapCost;

                    double J2 = (totalScrapValue + refurbishCost) * -1;
                    double N2 = -totalScrapValue * (1 - corporateTax);

                    J[0] = J2;
                    N[0] = N2;

                    for (int t = 1; t <= maxPeriods; t++)
                    {
                        // B
                        double lease = (t <= leaseTerm)
                            ? leaseIncome * Math.Pow(1 + escalationRate, t - 1)
                            : 0;

                        // D
                        double refurbish = (t <= minTerm)
                            ? refurbishCost / minTerm
                            : 0;

                        // E
                        double operating = (t <= leaseTerm)
                            ? operatingCosts * Math.Pow(1 + operatingEscalation, t - 1)
                            : 0;

                        // G
                        double residual = (t == leaseTerm)
                            ? residualValue
                            : 0;

                        // H
                        double cashFlow = lease - refurbish - operating + residual;

                        // I / M
                        double discountPre = 1 / Math.Pow(1 + waccPre, t);
                        double discountPost = 1 / Math.Pow(1 + waccPost, t);

                        // J
                        J[t] = cashFlow * discountPre;

                        // N
                        double tax = cashFlow * corporateTax;
                        double postTaxCash = cashFlow - tax;
                        N[t] = postTaxCash * discountPost;
                    }

                    double J23 = J.Sum();
                    double N23 = N.Sum();

                    double O23 = (J23 >= 0)
                        ? netBook + refurbishCost
                        : 0;

                    double P23 = (N23 >= 0)
                        ? netBook + refurbishCost
                        : 0;

                    decimal scrapPre = ParseDecimalSafe(J23);
                    decimal refurPre = ParseDecimalSafe(N23);
                    decimal transPre = ParseDecimalSafe(O23);

                    string preScrap = "R" + scrapPre.ToString("N2", new CultureInfo("en-ZA"));
                    string preRefur = "R" + refurPre.ToString("N2", new CultureInfo("en-ZA"));
                    string preTrans = "R" + transPre.ToString("N2", new CultureInfo("en-ZA"));

                    AddRow("Wagon Type", model?.WagonType ?? "N/A");
                    AddRow("Wagon Group", model?.WagonGroup ?? "N/A");
                    AddRow("Wagon Number", wagonNumber.ToString());
                    AddRow("Inspector", dash?.InspectorName ?? "Not Captured");
                    AddRow("Evaluator", assessor?.UserName ?? "Not Captured");
                    AddRow("GPS Latitude", model?.GpsLatitude ?? "Not Captured");
                    AddRow("GPS Longitude", model?.GpsLongitude ?? "Not Captured");
                    AddRow("City", city);
                    AddRow("Country", "South Africa");
                    AddRow("Net Book Value", netBookValue);
                    AddRow("Return To Service Cost", totalRepair);
                    AddRow("Asset Value", assetValue);
                    AddRow("Score", score.ToString() ?? "0");
                    AddRow("Condition", condition?.Condition ?? "N/A");
                    AddRow("Operational Status", condition?.OperationalStatus ?? "N/A");
                    AddRow("Inspection Date", DateTime.Now.ToString("yyyy-MM-dd"));
                    AddRow("SCRAP: Present Value (Pre-Tax)", preScrap ?? "0.00");
                    AddRow("REFURBISH: Present Value (Pre-Tax)", preRefur ?? "0.00");
                    AddRow("Transfer Value (Pre-Tax)", preTrans ?? "0.00");


                    document.Add(table);

                    // -------------------- VALUATION PARAGRAPHS --------------------
                    string currentDate = DateTime.Now.ToString("yyyy-MM-dd");

                    document.Add(new Paragraph($"This Valuation Certificate declares that the professional team - which is fully defined in the accompanying Quote document - has inspected Wagon: {wagonNumber}; we have verified the particulars set out in this valuation, and we value the herein described item for the purposes of this valuation to the best of our knowledge and skill on {currentDate} at a market value of:\n")
                        .SetFont(regular).SetFontSize(13)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetMarginBottom(10));

                    document.Add(new Paragraph("VALUATION ESTIMATE")
                        .SetFont(bold).SetFontSize(25)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetMarginBottom(10));

                    document.Add(new Paragraph("This certificate forms part of and must be read in conjunction with the Quote document. Please refer to the accompanying Quote document.\n")
                        .SetFont(regular).SetFontSize(13)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetMarginBottom(15));

                    // -------------------- FOOTER --------------------
                    string footerPath = Path.Combine(_env.WebRootPath, "Images", "CertLogo2.png");
                    float footerHeight = 0f;
                    if (System.IO.File.Exists(footerPath))
                    {
                        var footerImg = new Image(ImageDataFactory.Create(footerPath));
                        footerImg.ScaleToFit(pageWidth, 100f);
                        footerHeight = footerImg.GetImageScaledHeight();
                        int lastPageNumber = pdf.GetNumberOfPages();
                        footerImg.SetFixedPosition(lastPageNumber, 0, 0); // flush bottom
                        document.Add(footerImg);
                    }

                    // -------------------- THIRD PARAGRAPH (STICK ABOVE FOOTER) --------------------
                    Paragraph thirdParagraph = new Paragraph()
                        .Add($"Date: {currentDate}\n")
                        .Add("Technical Team: Worldwide Rail and Mining Solutions (Pty) Ltd\n")
                        .Add("Professional Valuation Company: Msomi Valuation Services (Pty) Ltd\n")
                        .Add("Registered Professional Valuer (South Africa) – J.D.S. Oberholzer\n")
                        .Add("SACPVP Reg. No. (Has not been given)\n")
                        .Add("Member of the South African Institute of Valuers\n")
                        .SetFont(regular).SetFontSize(15)
                        .SetTextAlignment(TextAlignment.CENTER);

                    var lastPage = pdf.GetLastPage();
                    var canvas = new Canvas(new PdfCanvas(lastPage), lastPage.GetPageSize());
                    float yPosition = footerHeight + 10f; // 10pt above footer
                    canvas.ShowTextAligned(thirdParagraph, pageWidth / 2f, yPosition, TextAlignment.CENTER);
                    canvas.Close();

                    document.Close();
                    pdf.Close();
                }

                //PLEASE ADJUST
                var dashboard = await _context.WagonDashboards.FirstOrDefaultAsync(d => d.WagonNumber == wagonNumber);
                string relativePath = Path.Combine("InspectionPdf", "Wagons", "CertPdf", fileName).Replace("\\", "/");

                if (dashboard != null)
                {
                    dashboard.AssessmentCert = relativePath;
                    _context.WagonDashboards.Update(dashboard);
                }
                else
                {
                    return BadRequest("Dashboard row missing for wagon.");
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = "PDF generated successfully", path = filePath });
            }
            catch (Exception ex)
            {
                // Remove the partial file if it was created
                try
                {
                    if (System.IO.File.Exists(filePath))
                        System.IO.File.Delete(filePath);
                }
                catch { /* ignore cleanup errors */ }

                return StatusCode(500, new { error = "PDF generation failed", detail = ex.Message });
            }
        }

        [HttpPost("RegenerateAndSaveCertPdf")]
        public async Task<IActionResult> RegenerateAndSaveCertPdf([FromBody] CertPdfRequestUpload request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.WagonNumber))
                return BadRequest("Invalid request: WagonNumber is required.");

            _context.Database.SetCommandTimeout(180);

            if (!int.TryParse(request.WagonNumber, out int wagonNumber))
                return BadRequest("Invalid WagonNumber format.");

            string userId = request.UserId ?? "";

            // Fetch related records (non-tracking reads where appropriate)
            var assessor = string.IsNullOrWhiteSpace(userId) ? null :
                await _context.LeaseCoUsers.AsNoTracking().FirstOrDefaultAsync(a => a.UserId == userId);

            var master = await _context.MasterWagons.AsNoTracking().FirstOrDefaultAsync(m => m.WagonNumber == wagonNumber);
            var model = await _context.WagonInfoCaptures.AsNoTracking().FirstOrDefaultAsync(p => p.WagonNumber == wagonNumber);

            var dash = await _context.WagonDashboardUploadeds.AsNoTracking().FirstOrDefaultAsync(p => p.WagonNumber == wagonNumber);

            int? score = dash?.ConditionScore;

            var condition = await _context.ConditionRatings.AsNoTracking().FirstOrDefaultAsync(c => c.Score == score);

            var input = await _context.WagonInputs.AsNoTracking().FirstOrDefaultAsync(i => i.WagonNumber == wagonNumber);

            // Validate basic presence
            if (model == null)
                return BadRequest($"No WagonInfoCaptures record found for wagon {wagonNumber}.");

            if (input == null)
                return BadRequest($"Inputs for wagon {wagonNumber} was not found. Please contact administrator for assistence.");

            // Ensure directory exists
            string folderPath = Path.Combine(_env.WebRootPath, "InspectionPdf", "Wagons", "CertPdf");
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            // Create sanitized filename (timestamp + guid to avoid collisions)
            string safeGroup = SanitizeFileName(model?.WagonGroup ?? "Group");
            string fileName = $"{wagonNumber}_{safeGroup}_Cert_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N").Substring(0, 8)}.pdf";
            string filePath = Path.Combine(folderPath, fileName);

            try
            {
                using (var writer = new PdfWriter(filePath))
                using (var pdf = new iText.Kernel.Pdf.PdfDocument(writer))
                using (var document = new Document(pdf, PageSize.A4))
                {
                    var bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                    var regular = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
                    float pageWidth = pdf.GetDefaultPageSize().GetWidth();
                    float pageHeight = pdf.GetDefaultPageSize().GetHeight();

                    // -------------------- HEADER --------------------
                    string headerPath = Path.Combine(_env.WebRootPath, "Images", "CertLogo1.png");
                    float headerHeight = 0f;
                    if (System.IO.File.Exists(headerPath))
                    {
                        var headerImg = new Image(ImageDataFactory.Create(headerPath));
                        headerImg.ScaleToFit(pageWidth, headerImg.GetImageHeight());
                        headerHeight = headerImg.GetImageScaledHeight();
                        headerImg.SetFixedPosition(0, pageHeight - headerHeight); // flush top
                        document.Add(headerImg);
                    }

                    // -------------------- WAGON PHOTO --------------------
                    if (!string.IsNullOrWhiteSpace(model?.WagonPhoto))
                    {
                        string fullImagePath = Path.Combine(_env.WebRootPath, model.WagonPhoto.TrimStart('/', '\\'));
                        if (System.IO.File.Exists(fullImagePath))
                        {
                            Image wagonImg = new Image(ImageDataFactory.Create(fullImagePath));
                            wagonImg.ScaleToFit(500f, 250f);
                            wagonImg.SetHorizontalAlignment(HorizontalAlignment.CENTER);

                            Div photoDiv = new Div()
                                .Add(wagonImg)
                                .SetMarginTop(headerHeight + 15f) // leave some space below header
                                .SetMarginBottom(15f)
                                .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER);

                            document.Add(photoDiv);
                        }
                    }

                    // -------------------- DESCRIPTION HEADING --------------------
                    document.Add(new Paragraph("DESCRIPTION OF ASSETS")
                        .SetFont(bold)
                        .SetFontSize(25)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetMarginTop(10f)
                        .SetMarginBottom(15f));

                    // -------------------- ASSET TABLE --------------------
                    var table = new Table(new float[] { 1.5f, 2f })
                        .UseAllAvailableWidth()
                        .SetMarginBottom(20f)
                        .SetBorder(Border.NO_BORDER);

                    var headerBg = new DeviceRgb(30, 60, 110);
                    var oddRowBg = new DeviceRgb(245, 245, 245);
                    var evenRowBg = ColorConstants.WHITE;

                    Style headerStyle = new Style()
                        .SetFont(bold).SetFontSize(12)
                        .SetFontColor(ColorConstants.WHITE)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetPadding(8);

                    Style labelStyle = new Style()
                        .SetFont(bold).SetFontSize(11)
                        .SetFontColor(ColorConstants.BLACK)
                        .SetTextAlignment(TextAlignment.LEFT)
                        .SetPadding(6);

                    Style valueStyle = new Style()
                        .SetFont(regular).SetFontSize(11)
                        .SetFontColor(ColorConstants.BLACK)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetPadding(6);

                    table.AddCell(new Cell().Add(new Paragraph("ASSET FIELD").AddStyle(headerStyle)).SetBackgroundColor(headerBg).SetBorder(Border.NO_BORDER));
                    table.AddCell(new Cell().Add(new Paragraph("VALUE").AddStyle(headerStyle)).SetBackgroundColor(headerBg).SetBorder(Border.NO_BORDER));

                    int rowIndex = 0;
                    void AddRow(string label, string value)
                    {
                        var bg = (rowIndex % 2 == 0) ? oddRowBg : evenRowBg;
                        table.AddCell(new Cell().Add(new Paragraph(label).AddStyle(labelStyle)).SetBackgroundColor(bg).SetBorder(Border.NO_BORDER));
                        table.AddCell(new Cell().Add(new Paragraph(value).AddStyle(valueStyle)).SetBackgroundColor(bg).SetBorder(Border.NO_BORDER));
                        rowIndex++;
                    }

                    //PLEASE ADD
                    // --- compute lift/barrel costs robustly ---
                    decimal liftCost = 0m;
                    decimal barrelCost = 0m;

                    // Use case-insensitive checks and handle a few possible string values
                    if (!string.IsNullOrWhiteSpace(model?.LiftLapsed))
                    {
                        var lift = model.LiftLapsed.Trim().ToLowerInvariant();
                        if (lift == "Yes" || lift == "y" || lift == "true") liftCost = 420982m;
                    }

                    if (!string.IsNullOrWhiteSpace(model?.BarrelLapsed))
                    {
                        var barrel = model.BarrelLapsed.Trim().ToLowerInvariant();
                        if (barrel == "Yes" || barrel == "y" || barrel == "true") barrelCost = 351893m;
                    }

                    decimal liftBarrelTotal = liftCost + barrelCost;

                    //PLEASE ADD
                    // --- parse dashboard numeric values safely (strip currency and commas) ---
                    decimal refVal = ParseDecimalSafe(dash?.RefurbishValue);
                    decimal misVal = ParseDecimalSafe(dash?.MissingValue);
                    decimal repVal = ParseDecimalSafe(dash?.ReplaceValue);
                    decimal labVal = ParseDecimalSafe(dash?.TotalLaborValue);

                    decimal asset = ParseDecimalSafe(dash?.AssetValue); //PLEASE ADD (NEW)

                    // FIXED: previously you added refVal twice — use repVal here
                    decimal repairValue = refVal + misVal + repVal + labVal + liftBarrelTotal;
                    string totalRepair = "R" + repairValue.ToString("N2", new CultureInfo("en-ZA"));

                    //PLEASE ADD
                    // market value (from master)
                    decimal marketValue = ParseDecimalSafe(master?.MarketValue);

                    string assetValue = "R" + asset.ToString("N2", new CultureInfo("en-ZA"));

                    //PLEASE ADD
                    // net book value: keep original format if not parseable
                    string netBookValue = "#N/A";
                    if (!string.IsNullOrWhiteSpace(model?.NetBookValue) && model.NetBookValue != "#N/A")
                    {
                        // try to parse replacing non-numeric chars, but fallback to original
                        var sanitized = model.NetBookValue.Replace("R", "").Replace(" ", "").Replace(".", "").Replace(",", ".");
                        if (decimal.TryParse(sanitized, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal nb))
                            netBookValue = "R" + nb.ToString("N2", new CultureInfo("en-ZA"));
                        else
                            netBookValue = model.NetBookValue;
                    }

                    //PLEASE ADD
                    // City via reverse geocode (best-effort)
                    string city = "Not Captured";
                    if (double.TryParse(model?.GpsLatitude, NumberStyles.Any, CultureInfo.InvariantCulture, out double latitude)
                        && double.TryParse(model?.GpsLongitude, NumberStyles.Any, CultureInfo.InvariantCulture, out double longitude))
                    {
                        var resolved = await GetCityFromCoordinatesAsync(latitude, longitude);
                        if (!string.IsNullOrWhiteSpace(resolved) && !resolved.StartsWith("Error", StringComparison.InvariantCultureIgnoreCase))
                            city = resolved;
                    }

                    double scrapCost = Convert.ToDouble(input.ScrappingCost);
                    double scrapValue = Convert.ToDouble(input.ScrapValue);
                    double refurbishCost = Convert.ToDouble(input.RefurbishmentCost);
                    double corporateTax = Convert.ToDouble(input.CorporateTaxRate) / 100;
                    int leaseTerm = Convert.ToInt32(input.LeaseTerm);
                    double leaseIncome = Convert.ToDouble(input.LeaseIncome);
                    double escalationRate = Convert.ToDouble(input.EscalationRate) / 100;
                    int wearTear = Convert.ToInt32(input.WearTearPeriod);
                    double operatingCosts = Convert.ToDouble(input.OperatingCosts);
                    double operatingEscalation = Convert.ToDouble(input.OperatingCostsEscalation) / 100;
                    double residualValue = Convert.ToDouble(input.ResidualValue);
                    double waccPre = Convert.ToDouble(ParseDecimalSafe(input.PreTax)) / 100;
                    double waccPost = Convert.ToDouble(ParseDecimalSafe(input.PostTax)) / 100;
                    double netBook = Convert.ToDouble(input.NetBookValue);

                    int maxPeriods = 20;
                    int minTerm = Math.Min(leaseTerm, wearTear);

                    double[] J = new double[maxPeriods + 1];
                    double[] N = new double[maxPeriods + 1];

                    double totalScrapValue = scrapValue + scrapCost;

                    double J2 = (totalScrapValue + refurbishCost) * -1;
                    double N2 = -totalScrapValue * (1 - corporateTax);

                    J[0] = J2;
                    N[0] = N2;

                    for (int t = 1; t <= maxPeriods; t++)
                    {
                        // B
                        double lease = (t <= leaseTerm)
                            ? leaseIncome * Math.Pow(1 + escalationRate, t - 1)
                            : 0;

                        // D
                        double refurbish = (t <= minTerm)
                            ? refurbishCost / minTerm
                            : 0;

                        // E
                        double operating = (t <= leaseTerm)
                            ? operatingCosts * Math.Pow(1 + operatingEscalation, t - 1)
                            : 0;

                        // G
                        double residual = (t == leaseTerm)
                            ? residualValue
                            : 0;

                        // H
                        double cashFlow = lease - refurbish - operating + residual;

                        // I / M
                        double discountPre = 1 / Math.Pow(1 + waccPre, t);
                        double discountPost = 1 / Math.Pow(1 + waccPost, t);

                        // J
                        J[t] = cashFlow * discountPre;

                        // N
                        double tax = cashFlow * corporateTax;
                        double postTaxCash = cashFlow - tax;
                        N[t] = postTaxCash * discountPost;
                    }

                    double J23 = J.Sum();
                    double N23 = N.Sum();

                    double O23 = (J23 >= 0)
                        ? netBook + refurbishCost
                        : 0;

                    double P23 = (N23 >= 0)
                        ? netBook + refurbishCost
                        : 0;

                    decimal scrapPre = ParseDecimalSafe(J23);
                    decimal refurPre = ParseDecimalSafe(N23);
                    decimal transPre = ParseDecimalSafe(O23);

                    string preScrap = "R" + scrapPre.ToString("N2", new CultureInfo("en-ZA"));
                    string preRefur = "R" + refurPre.ToString("N2", new CultureInfo("en-ZA"));
                    string preTrans = "R" + transPre.ToString("N2", new CultureInfo("en-ZA"));

                    AddRow("Wagon Type", model?.WagonType ?? "N/A");
                    AddRow("Wagon Group", model?.WagonGroup ?? "N/A");
                    AddRow("Wagon Number", wagonNumber.ToString());
                    AddRow("Inspector", dash?.InspectorName ?? "Not Captured");
                    AddRow("Evaluator", assessor?.UserName ?? "Not Captured");
                    AddRow("GPS Latitude", model?.GpsLatitude ?? "Not Captured");
                    AddRow("GPS Longitude", model?.GpsLongitude ?? "Not Captured");
                    AddRow("City", city);
                    AddRow("Country", "South Africa");
                    AddRow("Net Book Value", netBookValue);
                    AddRow("Return To Service Cost", totalRepair);
                    AddRow("Asset Value", assetValue);
                    AddRow("Score", score.ToString() ?? "0");
                    AddRow("Condition", condition?.Condition ?? "N/A");
                    AddRow("Operational Status", condition?.OperationalStatus ?? "N/A");
                    AddRow("Inspection Date", DateTime.Now.ToString("yyyy-MM-dd"));
                    AddRow("SCRAP: Present Value (Pre-Tax)", preScrap ?? "0.00");
                    AddRow("REFURBISH: Present Value (Pre-Tax)", preRefur ?? "0.00");
                    AddRow("Transfer Value (Pre-Tax)", preTrans ?? "0.00");

                    document.Add(table);

                    // -------------------- VALUATION PARAGRAPHS --------------------
                    string currentDate = DateTime.Now.ToString("yyyy-MM-dd");

                    document.Add(new Paragraph($"This Valuation Certificate declares that the professional team - which is fully defined in the accompanying Quote document - has inspected Wagon: {wagonNumber}; we have verified the particulars set out in this valuation, and we value the herein described item for the purposes of this valuation to the best of our knowledge and skill on {currentDate} at a market value of:\n")
                        .SetFont(regular).SetFontSize(13)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetMarginBottom(10));

                    document.Add(new Paragraph("VALUATION ESTIMATE")
                        .SetFont(bold).SetFontSize(25)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetMarginBottom(10));

                    document.Add(new Paragraph("This certificate forms part of and must be read in conjunction with the Quote document. Please refer to the accompanying Quote document.\n")
                        .SetFont(regular).SetFontSize(13)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetMarginBottom(15));

                    // -------------------- FOOTER --------------------
                    string footerPath = Path.Combine(_env.WebRootPath, "Images", "CertLogo2.png");
                    float footerHeight = 0f;
                    if (System.IO.File.Exists(footerPath))
                    {
                        var footerImg = new Image(ImageDataFactory.Create(footerPath));
                        footerImg.ScaleToFit(pageWidth, 100f);
                        footerHeight = footerImg.GetImageScaledHeight();
                        int lastPageNumber = pdf.GetNumberOfPages();
                        footerImg.SetFixedPosition(lastPageNumber, 0, 0); // flush bottom
                        document.Add(footerImg);
                    }

                    // -------------------- THIRD PARAGRAPH (STICK ABOVE FOOTER) --------------------
                    Paragraph thirdParagraph = new Paragraph()
                        .Add($"Date: {currentDate}\n")
                        .Add("Technical Team: Worldwide Rail and Mining Solutions (Pty) Ltd\n")
                        .Add("Professional Valuation Company: Msomi Valuation Services (Pty) Ltd\n")
                        .Add("Registered Professional Valuer (South Africa) – J.D.S. Oberholzer\n")
                        .Add("SACPVP Reg. No. (Has not been given)\n")
                        .Add("Member of the South African Institute of Valuers\n")
                        .SetFont(regular).SetFontSize(15)
                        .SetTextAlignment(TextAlignment.CENTER);

                    var lastPage = pdf.GetLastPage();
                    var canvas = new Canvas(new PdfCanvas(lastPage), lastPage.GetPageSize());
                    float yPosition = footerHeight + 10f; // 10pt above footer
                    canvas.ShowTextAligned(thirdParagraph, pageWidth / 2f, yPosition, TextAlignment.CENTER);
                    canvas.Close();

                    document.Close();
                    pdf.Close();
                }

                //PLEASE ADJUST
                var dashboard = await _context.WagonDashboardUploadeds.FirstOrDefaultAsync(d => d.WagonNumber == wagonNumber);
                string relativePath = Path.Combine("InspectionPdf", "Wagons", "CertPdf", fileName).Replace("\\", "/");

                if (dashboard != null)
                {
                    dashboard.AssessmentCert = relativePath;
                    _context.WagonDashboardUploadeds.Update(dashboard);
                }
                else
                {
                    return BadRequest("Dashboard row missing for wagon.");
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = "PDF generated successfully", path = filePath });
            }
            catch (Exception ex)
            {
                // Remove the partial file if it was created
                try
                {
                    if (System.IO.File.Exists(filePath))
                        System.IO.File.Delete(filePath);
                }
                catch { /* ignore cleanup errors */ }

                return StatusCode(500, new { error = "PDF generation failed", detail = ex.Message });
            }
        }
        [HttpPost("GenerateAndSaveCertPdfForAllWagon")]
        public async Task<IActionResult> GenerateAndSaveCertPdfForAllWagon()
        {
            var existingDashboard = await _context.WagonDashboards
                .Where(d => d.AssessmentCert == "Not Ready").ToListAsync();
            foreach (var dashboard in existingDashboard)
            { //await GenerateAndSaveCertPdf(dashboard.WagonNumber);
            }
            return Ok(new { message = "PDFs generated successfully for all wagons." });
        }

        [HttpPost("GenerateAndSaveLocoCertPdf")]
        public async Task<IActionResult> GenerateAndSaveLocoCertPdf([FromBody] LocoCertPdfRequest request)
        {
            if(request == null || string.IsNullOrWhiteSpace(request.LocoNumber))
                return BadRequest("Invalid request: LocoNumber is required.");

            _context.Database.SetCommandTimeout(180);

            if (!int.TryParse(request.LocoNumber, out int locoNumber))
                return BadRequest("Invalid LocoNumber format.");

            string userId = request.UserId ?? "";

            // Fetch related records (non-tracking reads where appropriate)
            var assessor = string.IsNullOrWhiteSpace(userId) ? null :
                await _context.LeaseCoUsers.AsNoTracking().FirstOrDefaultAsync(a => a.UserId == userId);

            var master = await _context.MasterLocos.AsNoTracking().FirstOrDefaultAsync(m => m.LocoNumber == locoNumber);
            var model = await _context.LocoInfoCaptures.AsNoTracking().FirstOrDefaultAsync(p => p.LocoNumber == locoNumber);
            var dash = await _context.LocoDashboards.AsNoTracking().FirstOrDefaultAsync(p => locoNumber == locoNumber);
            int? score = dash?.ConditionScore;

            var condition = await _context.ConditionRatings.AsNoTracking().FirstOrDefaultAsync(c => c.Score == score);
            // Validate basic presence
            if (model == null)
                return BadRequest($"No LocoInfoCaptures record found for loco {locoNumber}.");
            var LocoNumber = locoNumber;
            var input = await _context.LocoInputs.AsNoTracking().FirstOrDefaultAsync(i => i.LocoNumber == locoNumber);
            // Ensure directory exists
            string folderPath = Path.Combine(_env.WebRootPath, "InspectionPdf", "Locos", "CertPdf");
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            // Create sanitized filename (timestamp + guid to avoid collisions)
            string safeGroup = SanitizeFileName(model?.LocoModel ?? "Group");
            string fileName = $"{LocoNumber}_{safeGroup}_Cert_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N").Substring(0, 8)}.pdf";
            string filePath = Path.Combine(folderPath, fileName);

            try
            {
                using (var writer = new PdfWriter(filePath))
                using (var pdf = new iText.Kernel.Pdf.PdfDocument(writer))
                using (var document = new Document(pdf, PageSize.A4))
                {
                    var bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                    var regular = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
                    float pageWidth = pdf.GetDefaultPageSize().GetWidth();
                    float pageHeight = pdf.GetDefaultPageSize().GetHeight();

                    // -------------------- HEADER --------------------
                    string headerPath = Path.Combine(_env.WebRootPath, "Images", "CertLogo1.png");
                    float headerHeight = 0f;
                    if (System.IO.File.Exists(headerPath))
                    {
                        var headerImg = new Image(ImageDataFactory.Create(headerPath));
                        headerImg.ScaleToFit(pageWidth, headerImg.GetImageHeight());
                        headerHeight = headerImg.GetImageScaledHeight();
                        headerImg.SetFixedPosition(0, pageHeight - headerHeight); // flush top
                        document.Add(headerImg);
                    }

                    // -------------------- WAGON PHOTO --------------------
                    if (!string.IsNullOrWhiteSpace(model?.LocoPhoto))
                    {
                        string fullImagePath = Path.Combine(_env.WebRootPath, model.LocoPhoto.TrimStart('/', '\\'));
                        if (System.IO.File.Exists(fullImagePath))
                        {
                            Image wagonImg = new Image(ImageDataFactory.Create(fullImagePath));
                            wagonImg.ScaleToFit(500f, 250f);
                            wagonImg.SetHorizontalAlignment(HorizontalAlignment.CENTER);

                            Div photoDiv = new Div()
                                .Add(wagonImg)
                                .SetMarginTop(headerHeight + 15f) // leave some space below header
                                .SetMarginBottom(15f)
                                .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER);

                            document.Add(photoDiv);
                        }
                    }

                    // -------------------- DESCRIPTION HEADING --------------------
                    document.Add(new Paragraph("DESCRIPTION OF ASSETS")
                        .SetFont(bold)
                        .SetFontSize(25)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetMarginTop(10f)
                        .SetMarginBottom(15f));

                    // -------------------- ASSET TABLE --------------------
                    var table = new Table(new float[] { 1.5f, 2f })
                        .UseAllAvailableWidth()
                        .SetMarginBottom(20f)
                        .SetBorder(Border.NO_BORDER);

                    var headerBg = new DeviceRgb(30, 60, 110);
                    var oddRowBg = new DeviceRgb(245, 245, 245);
                    var evenRowBg = ColorConstants.WHITE;

                    Style headerStyle = new Style()
                        .SetFont(bold).SetFontSize(12)
                        .SetFontColor(ColorConstants.WHITE)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetPadding(8);

                    Style labelStyle = new Style()
                        .SetFont(bold).SetFontSize(11)
                        .SetFontColor(ColorConstants.BLACK)
                        .SetTextAlignment(TextAlignment.LEFT)
                        .SetPadding(6);

                    Style valueStyle = new Style()
                        .SetFont(regular).SetFontSize(11)
                        .SetFontColor(ColorConstants.BLACK)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetPadding(6);

                    table.AddCell(new Cell().Add(new Paragraph("ASSET FIELD").AddStyle(headerStyle)).SetBackgroundColor(headerBg).SetBorder(Border.NO_BORDER));
                    table.AddCell(new Cell().Add(new Paragraph("VALUE").AddStyle(headerStyle)).SetBackgroundColor(headerBg).SetBorder(Border.NO_BORDER));

                    int rowIndex = 0;
                    void AddRow(string label, string value)
                    {
                        var bg = (rowIndex % 2 == 0) ? oddRowBg : evenRowBg;
                        table.AddCell(new Cell().Add(new Paragraph(label).AddStyle(labelStyle)).SetBackgroundColor(bg).SetBorder(Border.NO_BORDER));
                        table.AddCell(new Cell().Add(new Paragraph(value).AddStyle(valueStyle)).SetBackgroundColor(bg).SetBorder(Border.NO_BORDER));
                        rowIndex++;
                    }

                    //PLEASE ADD
                    // --- compute lift/barrel costs robustly ---


                    // Use case-insensitive checks and handle a few possible string values
                   

                    //PLEASE ADD
                    // --- parse dashboard numeric values safely (strip currency and commas) ---
                    decimal refVal = ParseDecimalSafe(dash?.RefurbishValue);
                    decimal misVal = ParseDecimalSafe(dash?.MissingValue);
                    decimal repVal = ParseDecimalSafe(dash?.ReplaceValue);
                    decimal labVal = ParseDecimalSafe(dash?.TotalLaborValue);

                    // FIXED: previously you added refVal twice — use repVal here
                    decimal repairValue = refVal + misVal + repVal + labVal;
                    string totalRepair = "R" + repairValue.ToString("N2", new CultureInfo("en-ZA"));

                    //PLEASE ADD
                    // market value (from master)
                    decimal marketValue = ParseDecimalSafe(master?.MarketValue);
                    decimal asset = ParseDecimalSafe(dash?.AssetValue);
                    string assetValue = "R" + asset.ToString("N2", new CultureInfo("en-ZA"));

                    //PLEASE ADD
                    // net book value: keep original format if not parseable
                    string netBookValue = "#N/A";
                    if (!string.IsNullOrWhiteSpace(model?.NetBookValue) && model.NetBookValue != "#N/A")
                    {
                        // try to parse replacing non-numeric chars, but fallback to original
                        var sanitized = model.NetBookValue.Replace("R", "").Replace(" ", "").Replace(".", "").Replace(",", ".");
                        if (decimal.TryParse(sanitized, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal nb))
                            netBookValue = "R" + nb.ToString("N2", new CultureInfo("en-ZA"));
                        else
                            netBookValue = model.NetBookValue;
                    }

                    //PLEASE ADD
                    // City via reverse geocode (best-effort)
                    string city = "Not Captured";
                    if (double.TryParse(model?.GpsLatitude, NumberStyles.Any, CultureInfo.InvariantCulture, out double latitude)
                        && double.TryParse(model?.GpsLongitude, NumberStyles.Any, CultureInfo.InvariantCulture, out double longitude))
                    {
                        var resolved = await GetCityFromCoordinatesAsync(latitude, longitude);
                        if (!string.IsNullOrWhiteSpace(resolved) && !resolved.StartsWith("Error", StringComparison.InvariantCultureIgnoreCase))
                            city = resolved;
                    }
                    double scrapCost = Convert.ToDouble(input.ScrappingCost);
                    double scrapValue = Convert.ToDouble(input.ScrapValue);
                    double refurbishCost = Convert.ToDouble(input.RefurbishmentCost);
                    double corporateTax = Convert.ToDouble(input.CorporateTaxRate) / 100;
                    int leaseTerm = Convert.ToInt32(input.LeaseTerm);
                    double leaseIncome = Convert.ToDouble(input.LeaseIncome);
                    double escalationRate = Convert.ToDouble(input.EscalationRate) / 100;
                    int wearTear = Convert.ToInt32(input.WearTearPeriod);
                    double operatingCosts = Convert.ToDouble(input.OperatingCosts);
                    double operatingEscalation = Convert.ToDouble(input.OperatingCostsEscalation) / 100;
                    double residualValue = Convert.ToDouble(input.ResidualValue);
                    double waccPre = Convert.ToDouble(ParseDecimalSafe(input.PreTax)) / 100;
                    double waccPost = Convert.ToDouble(ParseDecimalSafe(input.PostTax)) / 100;
                    double netBook = Convert.ToDouble(input.NetBookValue);

                    int maxPeriods = 20;
                    int minTerm = Math.Min(leaseTerm, wearTear);

                    double[] J = new double[maxPeriods + 1];
                    double[] N = new double[maxPeriods + 1];

                    double totalScrapValue = scrapValue + scrapCost;

                    double J2 = (totalScrapValue + refurbishCost) * -1;
                    double N2 = -totalScrapValue * (1 - corporateTax);

                    J[0] = J2;
                    N[0] = N2;

                    for (int t = 1; t <= maxPeriods; t++)
                    {
                        // B
                        double lease = (t <= leaseTerm)
                            ? leaseIncome * Math.Pow(1 + escalationRate, t - 1)
                            : 0;

                        // D
                        double refurbish = (t <= minTerm)
                            ? refurbishCost / minTerm
                            : 0;

                        // E
                        double operating = (t <= leaseTerm)
                            ? operatingCosts * Math.Pow(1 + operatingEscalation, t - 1)
                            : 0;

                        // G
                        double residual = (t == leaseTerm)
                            ? residualValue
                            : 0;

                        // H
                        double cashFlow = lease - refurbish - operating + residual;

                        // I / M
                        double discountPre = 1 / Math.Pow(1 + waccPre, t);
                        double discountPost = 1 / Math.Pow(1 + waccPost, t);

                        // J
                        J[t] = cashFlow * discountPre;

                        // N
                        double tax = cashFlow * corporateTax;
                        double postTaxCash = cashFlow - tax;
                        N[t] = postTaxCash * discountPost;
                    }

                    double J23 = J.Sum();
                    double N23 = N.Sum();

                    double O23 = (J23 >= 0)
                        ? netBook + refurbishCost
                        : 0;

                    double P23 = (N23 >= 0)
                        ? netBook + refurbishCost
                        : 0;

                    decimal scrapPre = ParseDecimalSafe(J23);
                    decimal refurPre = ParseDecimalSafe(N23);
                    decimal transPre = ParseDecimalSafe(O23);

                    string preScrap = "R" + scrapPre.ToString("N2", new CultureInfo("en-ZA"));
                    string preRefur = "R" + refurPre.ToString("N2", new CultureInfo("en-ZA"));
                    string preTrans = "R" + transPre.ToString("N2", new CultureInfo("en-ZA"));

                    AddRow("Loco Model", model?.LocoModel ?? "N/A");
                    AddRow("Loco Class", model?.LocoClass ?? "N/A");
                    AddRow("Loco Number", locoNumber.ToString());
                    AddRow("Inspector", dash?.InspectorName ?? "Not Captured");
                    AddRow("Evaluator", assessor?.UserName ?? "Not Captured");
                    AddRow("GPS Latitude", model?.GpsLatitude ?? "Not Captured");
                    AddRow("GPS Longitude", model?.GpsLongitude ?? "Not Captured");
                    AddRow("City", city);
                    AddRow("Country", "South Africa");
                    AddRow("Net Book Value", netBookValue); //PLEASE ADJUST
                    AddRow("Return To Service Cost", totalRepair); //PLEASE ADJUST
                    AddRow("Asset Value", assetValue);
                    AddRow("Score", score.ToString() ?? "0");
                    AddRow("Condition", condition?.Condition ?? "N/A");
                    AddRow("Operational Status", condition?.OperationalStatus ?? "N/A");
                    AddRow("Inspection Date", DateTime.Now.ToString("yyyy-MM-dd"));
                    AddRow("SCRAP: Present Value (Pre-Tax)", preScrap ?? "0.00");
                    AddRow("REFURBISH: Present Value (Pre-Tax)", preRefur ?? "0.00");
                    AddRow("Transfer Value (Pre-Tax)", preTrans ?? "0.00");
                    document.Add(table);

                    // -------------------- VALUATION PARAGRAPHS --------------------
                    string currentDate = DateTime.Now.ToString("yyyy-MM-dd");

                    document.Add(new Paragraph($"This Valuation Certificate declares that the professional team - which is fully defined in the accompanying Quote document - has inspected Loco: {locoNumber}; we have verified the particulars set out in this valuation, and we value the herein described item for the purposes of this valuation to the best of our knowledge and skill on {currentDate} at a market value of:\n")
                        .SetFont(regular).SetFontSize(13)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetMarginBottom(10));

                    document.Add(new Paragraph("VALUATION ESTIMATE")
                        .SetFont(bold).SetFontSize(25)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetMarginBottom(10));

                    document.Add(new Paragraph("This certificate forms part of and must be read in conjunction with the Quote document. Please refer to the accompanying Quote document.\n")
                        .SetFont(regular).SetFontSize(13)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetMarginBottom(15));

                    // -------------------- FOOTER --------------------
                    string footerPath = Path.Combine(_env.WebRootPath, "Images", "CertLogo2.png");
                    float footerHeight = 0f;
                    if (System.IO.File.Exists(footerPath))
                    {
                        var footerImg = new Image(ImageDataFactory.Create(footerPath));
                        footerImg.ScaleToFit(pageWidth, 100f);
                        footerHeight = footerImg.GetImageScaledHeight();
                        int lastPageNumber = pdf.GetNumberOfPages();
                        footerImg.SetFixedPosition(lastPageNumber, 0, 0); // flush bottom
                        document.Add(footerImg);
                    }

                    // -------------------- THIRD PARAGRAPH (STICK ABOVE FOOTER) --------------------
                    Paragraph thirdParagraph = new Paragraph()
                        .Add($"Date: {currentDate}\n")
                        .Add("Technical Team: Worldwide Rail and Mining Solutions (Pty) Ltd\n")
                        .Add("Professional Valuation Company: Msomi Valuation Services (Pty) Ltd\n")
                        .Add("Registered Professional Valuer (South Africa) – J.D.S. Oberholzer\n")
                        .Add("SACPVP Reg. No. (Has not been given)\n")
                        .Add("Member of the South African Institute of Valuers\n")
                        .SetFont(regular).SetFontSize(15)
                        .SetTextAlignment(TextAlignment.CENTER);

                    var lastPage = pdf.GetLastPage();
                    var canvas = new Canvas(new PdfCanvas(lastPage), lastPage.GetPageSize());
                    float yPosition = footerHeight + 10f; // 10pt above footer
                    canvas.ShowTextAligned(thirdParagraph, pageWidth / 2f, yPosition, TextAlignment.CENTER);
                    canvas.Close();

                    document.Close();
                    pdf.Close();
                }

                //PLEASE ADJUST
                var dashboard = await _context.LocoDashboards.FirstOrDefaultAsync(d => d.LocoNumber == locoNumber);
                string relativePath = Path.Combine("InspectionPdf", "Locos", "CertPdf", fileName).Replace("\\", "/");

                if (dashboard != null)
                {
                    dashboard.AssessmentCert = relativePath;
                    _context.LocoDashboards.Update(dashboard);
                }
                else
                {
                    return BadRequest("Dashboard row missing for loco.");
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = "PDF generated successfully", path = filePath });
            }
            catch (Exception ex)
            {
                // Remove the partial file if it was created
                try
                {
                    if (System.IO.File.Exists(filePath))
                        System.IO.File.Delete(filePath);
                }
                catch { /* ignore cleanup errors */ }

                return StatusCode(500, new { error = "PDF generation failed", detail = ex.Message });
            }
        }

        [HttpPost("GenerateAndSaveCertPdfForAllLoco")]
        public async Task<IActionResult> GenerateAndSaveCertPdfForAllLoco()
        {
            var existingDashboard = await _context.LocoDashboards
                .Where(d => d.AssessmentCert == "Not Ready").ToListAsync();
            foreach (var dashboard in existingDashboard)
            {
                //await GenerateAndSaveLocoCertPdf((int)dashboard.LocoNumber);
            }
            return Ok(new { message = "PDFs generated successfully for all Locos." });
        }

    
    
        //PLEASE ADD
        private static decimal ParseDecimalSafe(object? obj)
        {
            if (obj == null) return 0m;
            try
            {
                // If already numeric types
                if (obj is decimal d) return d;
                if (obj is double db) return Convert.ToDecimal(db);
                if (obj is int i) return i;

                string s = obj.ToString() ?? "";
                if (string.IsNullOrWhiteSpace(s)) return 0m;

                // remove currency symbols and spaces; replace comma thousands separators
                s = s.Replace("R", "", StringComparison.InvariantCultureIgnoreCase)
                     .Replace(" ", "")
                     .Trim();

                // If the value contains both . and , assume comma is thousands separator => remove commas
                // Otherwise let . be decimal point. Use invariant parsing.
                s = s.Replace(",", "");

                if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result))
                    return result;
            }
            catch
            {
                // ignored - fallback 0
            }
            return 0m;
        }

        //PLEASE ADD
        // Simple filename sanitizer to remove bad characters
        private static string SanitizeFileName(string input)
        {
            string s = input ?? "";
            // remove path separators and invalid chars
            s = Regex.Replace(s, @"[\/\\\:\*\?\""<>\|]", "_");
            s = Regex.Replace(s, @"\s+", "_");
            if (s.Length > 40) s = s.Substring(0, 40);
            return string.IsNullOrWhiteSpace(s) ? "Group" : s;
        }

        //PLEASE ADD
        private async Task<string> GetCityFromCoordinatesAsync(double latitude, double longitude)
        {
            try
            {
                string? apiKey = _config["LocationIQ:ApiKey"];
                if (string.IsNullOrWhiteSpace(apiKey))
                    return "Not Captured";

                var client = _httpClientFactory.CreateClient();
                string url = $"https://us1.locationiq.com/v1/reverse.php?key={apiKey}&lat={latitude.ToString(CultureInfo.InvariantCulture)}&lon={longitude.ToString(CultureInfo.InvariantCulture)}&format=json";

                using (var resp = await client.GetAsync(url))
                {
                    if (!resp.IsSuccessStatusCode) return "Not Captured";
                    var json = await resp.Content.ReadAsStringAsync();
                    var obj = JObject.Parse(json);
                    string? city = obj["address"]?["city"]?.ToString()
                               ?? obj["address"]?["town"]?.ToString()
                               ?? obj["address"]?["village"]?.ToString()
                               ?? obj["address"]?["county"]?.ToString();
                    return string.IsNullOrWhiteSpace(city) ? "Not Captured" : city;
                }
            }
            catch
            {
                return "Not Captured";
            }
        }


        //PLEASE ADD
    }
    public class CertPdfRequest
{
    public string WagonNumber { get; set; } = string.Empty;
    public string UserId { get; set; } = "";
}
    public class CertPdfRequestUpload
    {
        public string WagonNumber { get; set; } = string.Empty;
        public string UserId { get; set; } = "";
    }
    public class LocoCertPdfRequest
    {
        public string LocoNumber { get; set; } = string.Empty;
        public string UserId { get; set; } = "";
    }
}
