using AviFinal.Api.Models;
using iText.IO.Font.Constants;
using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Crypto;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Draw;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AviAppFinal.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QuotePdfController : ControllerBase
    {
        private readonly AviDbContext _context;
        private readonly IWebHostEnvironment _env;

        public QuotePdfController(AviDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpPost("GenerateAndSaveQuotePdf")]
        public async Task<IActionResult> GenerateAndSaveQuotePdf([FromBody] QuotePdfRequest request)
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
            var dash = await _context.WagonDashboards.AsNoTracking().FirstOrDefaultAsync(p => p.WagonNumber == wagonNumber);

            // Validate basic presence
            if (model == null)
                return BadRequest($"No WagonInfoCaptures record found for wagon {wagonNumber}.");

            // Define each inspection table source and label
            var inspectionSources = new Dictionary<string, IEnumerable<dynamic>>
            {
                { "Air Brake Inspection", await _context.AirBrakePartsInspects.Where(p => p.WagonNumber == wagonNumber).ToListAsync() },
                { "Bottom Discharge Inspection", await _context.BottomDischargeInspects.Where(p => p.WagonNumber == wagonNumber).ToListAsync() },
                { "Doors Inspection", await _context.DoorsInspects.Where(p => p.WagonNumber == wagonNumber).ToListAsync() },
                { "Floor Inspection", await _context.FloorInspects.Where(p => p.WagonNumber == wagonNumber).ToListAsync() },
                { "Stanchions Inspection", await _context.StanchionsInspects.Where(p => p.WagonNumber == wagonNumber).ToListAsync() },
                { "Tankers Inspection", await _context.TankersInspects.Where(p => p.WagonNumber == wagonNumber).ToListAsync() },
                { "Twistlocks Inspection", await _context.TwistlocksInspects.Where(p => p.WagonNumber == wagonNumber).ToListAsync() },
                { "Vacuum Brake Inspection", await _context.VacBrakePartsInspects.Where(p => p.WagonNumber == wagonNumber).ToListAsync() },
                { "Wagon Parts Inspection", await _context.WagonPartsInspects.Where(p => p.WagonNumber == wagonNumber).ToListAsync() }
            };

            if (inspectionSources.All(s => !s.Value.Any()))
                return NotFound("No parts found for this wagon number.");

            string folderPath = Path.Combine(_env.WebRootPath, "InspectionPdf", "Wagons", "QuotePdf");
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            // Create sanitized filename (timestamp + guid to avoid collisions)
            string safeGroup = SanitizeFileName(model?.WagonGroup ?? "Group");
            string fileName = $"{wagonNumber}_{safeGroup}_Quote_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N").Substring(0, 8)}.pdf";
            string filePath = Path.Combine(folderPath, fileName);

            try
            {
                // --- Generate PDF ---
                using (var writer = new PdfWriter(filePath))
                using (var pdf = new iText.Kernel.Pdf.PdfDocument(writer))
                using (var document = new Document(pdf))
                {
                    var bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                    var regular = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

                    // --- Header section ---
                    float[] columnWidths = { 150f, 350f };
                    var topTable = new Table(UnitValue.CreatePointArray(columnWidths)).SetWidth(UnitValue.CreatePercentValue(100));

                    var logoCell = new Cell().SetBorder(Border.NO_BORDER).SetVerticalAlignment(VerticalAlignment.TOP);
                    string logo1Path = Path.Combine(_env.WebRootPath, "Images", "Logo1.png");
                    string logo2Path = Path.Combine(_env.WebRootPath, "Images", "Logo2.png");

                    if (System.IO.File.Exists(logo1Path))
                    {
                        var logo1Img = new Image(ImageDataFactory.Create(logo1Path)).ScaleToFit(100, 50);
                        logoCell.Add(logo1Img);
                    }
                    if (System.IO.File.Exists(logo2Path))
                    {
                        logoCell.Add(new Paragraph("\n"));
                        var logo2Img = new Image(ImageDataFactory.Create(logo2Path)).ScaleToFit(100, 50);
                        logoCell.Add(logo2Img);
                    }
                    topTable.AddCell(logoCell);

                    var infoCell = new Cell().SetBorder(Border.NO_BORDER).SetVerticalAlignment(VerticalAlignment.TOP)
                        .SetHorizontalAlignment(HorizontalAlignment.RIGHT)
                        .SetTextAlignment(iText.Layout.Properties.TextAlignment.RIGHT);

                    infoCell.Add(new Paragraph()
                        .Add(new Text("Worldwide Rail and Mining Solutions SA (Pty) Ltd\n").SetFont(bold))
                        .Add(new Text("52 8th Avenue, Edenvale Gauteng 1610\n"))
                        .Add(new Text("Email: adminsa@wwms.co.za\n"))
                        .Add(new Text("T: +27 11 453 2170\n"))
                        .Add(new Text("Website: www.worldwideminingsolutions.co.za\n"))
                        .Add(new Text("Reg. No.: 2019/544337/07\n"))
                        .Add(new Text("Msomi Valuation Services (Pty) Ltd\n").SetFont(bold))
                        .Add(new Text("4 Sheffield Road, Ferryvale, Nigel, 1491\n"))
                        .Add(new Text("T: 011 814 2047\n"))
                        .Add(new Text("VAT: 4400277721\n")));

                    topTable.AddCell(infoCell);
                    document.Add(topTable);

                    document.Add(new LineSeparator(new SolidLine(1f)).SetMarginTop(10).SetMarginBottom(15));
                    document.Add(new Paragraph($"Quote - Asset Code: {wagonNumber}").SetFont(bold).SetFontSize(14).SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER));
                    document.Add(new Paragraph($"Asset Model/Group: {model?.WagonGroup ?? "N/A"}").SetFont(regular).SetFontSize(12).SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER).SetMarginBottom(10));
                    document.Add(new Paragraph($"Assessor: {assessor?.UserName ?? "N/A"}").SetFont(regular).SetFontSize(12).SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER).SetMarginBottom(10));

                    decimal grandTotalRefurbish = 0, grandTotalMissing = 0, grandTotalReplace = 0, grandTotalLabor = 0;

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

                    document.Add(new Paragraph("Lift & Barrel Costs")
                        .SetFont(bold)
                        .SetFontSize(13)
                        .SetMarginTop(15)
                        .SetBackgroundColor(ColorConstants.LIGHT_GRAY)
                        .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER));

                    var liftBarrelTable = new Table(UnitValue.CreatePercentArray(3)).UseAllAvailableWidth();

                    string[] liftBarrelHeaders = { "Description", "Lapsed", "Value (ZAR)" };
                    foreach (var header in liftBarrelHeaders)
                        liftBarrelTable.AddHeaderCell(new Cell().Add(new Paragraph(header).SetFont(bold).SetBackgroundColor(ColorConstants.LIGHT_GRAY)));

                    liftBarrelTable.AddCell(new Cell().Add(new Paragraph("Lift Inspection").SetFont(regular)));
                    liftBarrelTable.AddCell(new Cell().Add(new Paragraph(model?.LiftLapsed ?? "N/A").SetFont(regular)));
                    liftBarrelTable.AddCell(new Cell().Add(new Paragraph(liftCost != 0m ? $"R{liftCost:F2}" : "-").SetFont(regular)));

                    liftBarrelTable.AddCell(new Cell().Add(new Paragraph("Barrel Inspection").SetFont(regular)));
                    liftBarrelTable.AddCell(new Cell().Add(new Paragraph(model?.BarrelLapsed ?? "N/A").SetFont(regular)));
                    liftBarrelTable.AddCell(new Cell().Add(new Paragraph(barrelCost != 0m ? $"R{barrelCost:F2}" : "-").SetFont(regular)));

                    liftBarrelTable.AddCell(new Cell(1, 2)
                        .Add(new Paragraph("Subtotal").SetFont(bold))
                        .SetTextAlignment(iText.Layout.Properties.TextAlignment.RIGHT)
                        .SetBackgroundColor(ColorConstants.LIGHT_GRAY));

                    liftBarrelTable.AddCell(new Cell().Add(new Paragraph($"R{liftBarrelTotal:F2}").SetFont(bold)));

                    document.Add(liftBarrelTable);

                    // --- Inspection Tables ---
                    foreach (var group in inspectionSources)
                    {
                        var parts = group.Value;
                        if (!parts.Any()) continue;

                        var partsWithNumbers = parts.Select(p =>
                        {
                            decimal refVal = 0, missVal = 0, replVal = 0, labVal = 0;

                            if (p.RefurbishValue != null && !string.IsNullOrWhiteSpace(p.RefurbishValue.ToString()))
                                decimal.TryParse(p.RefurbishValue.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out refVal);

                            if (p.MissingValue != null && !string.IsNullOrWhiteSpace(p.MissingValue.ToString()))
                                decimal.TryParse(p.MissingValue.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out missVal);

                            if (p.ReplaceValue != null && !string.IsNullOrWhiteSpace(p.ReplaceValue.ToString()))
                                decimal.TryParse(p.ReplaceValue.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out replVal);

                            if (p.LaborValue != null && !string.IsNullOrWhiteSpace(p.LaborValue.ToString()))
                                decimal.TryParse(p.LaborValue.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out labVal);

                            return new
                            {
                                p.FormId,
                                p.PartDescr,
                                RefurbishValue = refVal,
                                MissingValue = missVal,
                                ReplaceValue = replVal,
                                LaborValue = labVal
                            };
                        }).ToList();

                        decimal totalRefurbish = partsWithNumbers.Sum(p => p.RefurbishValue);
                        decimal totalMissing = partsWithNumbers.Sum(p => p.MissingValue);
                        decimal totalReplace = partsWithNumbers.Sum(p => p.ReplaceValue);
                        decimal totalLabor = partsWithNumbers.Sum(p => p.LaborValue);

                        grandTotalRefurbish += totalRefurbish;
                        grandTotalMissing += totalMissing;
                        grandTotalReplace += totalReplace;
                        grandTotalLabor += totalLabor;

                        document.Add(new Paragraph(group.Key)
                            .SetFont(bold)
                            .SetFontSize(13)
                            .SetMarginTop(15)
                            .SetBackgroundColor(ColorConstants.LIGHT_GRAY)
                            .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER));

                        var table = new Table(UnitValue.CreatePercentArray(7)).UseAllAvailableWidth();
                        string[] headers = { "No.", "Form ID", "Part Description", "Refurbish Value", "Missing Value", "Replace Value", "Labor Value" };
                        foreach (var header in headers)
                            table.AddHeaderCell(new Cell().Add(new Paragraph(header).SetFont(bold).SetBackgroundColor(ColorConstants.LIGHT_GRAY)));

                        int index = 1;
                        foreach (var p in partsWithNumbers)
                        {
                            table.AddCell(new Cell().Add(new Paragraph(index.ToString()).SetFont(regular)));
                            table.AddCell(new Cell().Add(new Paragraph(p.FormId.ToString()).SetFont(regular)));
                            table.AddCell(new Cell().Add(new Paragraph(p.PartDescr).SetFont(regular)));
                            table.AddCell(new Cell().Add(new Paragraph(p.RefurbishValue != 0 ? $"R{p.RefurbishValue:F2}" : "-").SetFont(regular)));
                            table.AddCell(new Cell().Add(new Paragraph(p.MissingValue != 0 ? $"R{p.MissingValue:F2}" : "-").SetFont(regular)));
                            table.AddCell(new Cell().Add(new Paragraph(p.ReplaceValue != 0 ? $"R{p.ReplaceValue:F2}" : "-").SetFont(regular)));
                            table.AddCell(new Cell().Add(new Paragraph(p.LaborValue != 0 ? $"R{p.LaborValue:F2}" : "-").SetFont(regular)));
                            index++;
                        }

                        table.AddCell(new Cell(1, 3)
                            .Add(new Paragraph("Subtotal").SetFont(bold))
                            .SetTextAlignment(iText.Layout.Properties.TextAlignment.RIGHT)
                            .SetBackgroundColor(ColorConstants.LIGHT_GRAY));

                        table.AddCell(new Cell().Add(new Paragraph($"R{totalRefurbish:F2}").SetFont(bold)));
                        table.AddCell(new Cell().Add(new Paragraph($"R{totalMissing:F2}").SetFont(bold)));
                        table.AddCell(new Cell().Add(new Paragraph($"R{totalReplace:F2}").SetFont(bold)));
                        table.AddCell(new Cell().Add(new Paragraph($"R{totalLabor:F2}").SetFont(bold)));

                        document.Add(table);
                    }

                    decimal marketValue = ParseDecimalSafe(master?.MarketValue);
                    decimal rts = grandTotalRefurbish + grandTotalMissing + grandTotalReplace + grandTotalLabor + liftBarrelTotal;
                    decimal assetValue = marketValue - rts;

                    // --- Final Grand Totals ---
                    decimal grandTotal = grandTotalRefurbish + grandTotalMissing + grandTotalReplace + liftBarrelTotal + grandTotalLabor + assetValue;

                    document.Add(new Paragraph("\nGrand Totals").SetFont(bold).SetFontSize(13).SetTextAlignment(iText.Layout.Properties.TextAlignment.RIGHT));

                    var totalsTable = new Table(UnitValue.CreatePercentArray(7)).UseAllAvailableWidth();
                    totalsTable.AddHeaderCell(new Cell().Add(new Paragraph("Refurbish Total").SetFont(bold)));
                    totalsTable.AddHeaderCell(new Cell().Add(new Paragraph("Missing Total").SetFont(bold)));
                    totalsTable.AddHeaderCell(new Cell().Add(new Paragraph("Replace Total").SetFont(bold)));
                    totalsTable.AddHeaderCell(new Cell().Add(new Paragraph("Lift & Barrel Total").SetFont(bold)));
                    totalsTable.AddHeaderCell(new Cell().Add(new Paragraph("Labor Total").SetFont(bold)));
                    totalsTable.AddHeaderCell(new Cell().Add(new Paragraph("Market Value").SetFont(bold)));
                    totalsTable.AddHeaderCell(new Cell().Add(new Paragraph("Return to Service Cost").SetFont(bold)));

                    totalsTable.AddCell(new Cell().Add(new Paragraph($"R{grandTotalRefurbish:F2}").SetFont(regular)));
                    totalsTable.AddCell(new Cell().Add(new Paragraph($"R{grandTotalMissing:F2}").SetFont(regular)));
                    totalsTable.AddCell(new Cell().Add(new Paragraph($"R{grandTotalReplace:F2}").SetFont(regular)));
                    totalsTable.AddCell(new Cell().Add(new Paragraph($"R{liftBarrelTotal:F2}").SetFont(regular)));
                    totalsTable.AddCell(new Cell().Add(new Paragraph($"R{grandTotalLabor:F2}").SetFont(regular)));
                    totalsTable.AddCell(new Cell().Add(new Paragraph($"R{assetValue:F2}").SetFont(regular)));
                    totalsTable.AddCell(new Cell().Add(new Paragraph($"R{grandTotal:F2}").SetFont(bold)));

                    document.Add(totalsTable);

                    // --- Footer Image ---
                    string footerPath = Path.Combine(_env.WebRootPath, "Images", "Footer1.png");
                    if (System.IO.File.Exists(footerPath))
                    {
                        var pageSize = pdf.GetDefaultPageSize();
                        float pageWidth = pageSize.GetWidth();

                        var footerImg = new Image(ImageDataFactory.Create(footerPath));
                        float scaleX = pageWidth / footerImg.GetImageWidth();
                        footerImg.ScaleToFit(pageWidth, footerImg.GetImageHeight() * scaleX);
                        footerImg.SetFixedPosition(0, 0);
                        document.Add(footerImg);
                    }

                    document.Close();
                    pdf.Close();
                }

                //PLEASE ADJUST
                var dashboard = await _context.WagonDashboards.FirstOrDefaultAsync(d => d.WagonNumber == wagonNumber);
                string relativePath = Path.Combine("InspectionPdf", "Wagons", "QuotePdf", fileName).Replace("\\", "/");

                if (dashboard != null)
                {
                    dashboard.AssessmentQuote = relativePath;
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

        [HttpPost("RegenerateAndSaveQuotePdf")]
        public async Task<IActionResult> RegenerateAndSaveQuotePdf([FromBody] QuotePdfRequestUpload request)
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

            // Validate basic presence
            if (model == null)
                return BadRequest($"No WagonInfoCaptures record found for wagon {wagonNumber}.");

            // Define each inspection table source and label
            var inspectionSources = new Dictionary<string, IEnumerable<dynamic>>
            {
                { "Air Brake Inspection", await _context.AirBrakePartsInspects.Where(p => p.WagonNumber == wagonNumber).ToListAsync() },
                { "Bottom Discharge Inspection", await _context.BottomDischargeInspects.Where(p => p.WagonNumber == wagonNumber).ToListAsync() },
                { "Doors Inspection", await _context.DoorsInspects.Where(p => p.WagonNumber == wagonNumber).ToListAsync() },
                { "Floor Inspection", await _context.FloorInspects.Where(p => p.WagonNumber == wagonNumber).ToListAsync() },
                { "Stanchions Inspection", await _context.StanchionsInspects.Where(p => p.WagonNumber == wagonNumber).ToListAsync() },
                { "Tankers Inspection", await _context.TankersInspects.Where(p => p.WagonNumber == wagonNumber).ToListAsync() },
                { "Twistlocks Inspection", await _context.TwistlocksInspects.Where(p => p.WagonNumber == wagonNumber).ToListAsync() },
                { "Vacuum Brake Inspection", await _context.VacBrakePartsInspects.Where(p => p.WagonNumber == wagonNumber).ToListAsync() },
                { "Wagon Parts Inspection", await _context.WagonPartsInspects.Where(p => p.WagonNumber == wagonNumber).ToListAsync() }
            };

            if (inspectionSources.All(s => !s.Value.Any()))
                return NotFound("No parts found for this wagon number.");

            string folderPath = Path.Combine(_env.WebRootPath, "InspectionPdf", "Wagons", "QuotePdf");
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            // Create sanitized filename (timestamp + guid to avoid collisions)
            string safeGroup = SanitizeFileName(model?.WagonGroup ?? "Group");
            string fileName = $"{wagonNumber}_{safeGroup}_Quote_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N").Substring(0, 8)}.pdf";
            string filePath = Path.Combine(folderPath, fileName);

            try
            {
                // --- Generate PDF ---
                using (var writer = new PdfWriter(filePath))
                using (var pdf = new iText.Kernel.Pdf.PdfDocument(writer))
                using (var document = new Document(pdf))
                {
                    var bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                    var regular = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

                    // --- Header section ---
                    float[] columnWidths = { 150f, 350f };
                    var topTable = new Table(UnitValue.CreatePointArray(columnWidths)).SetWidth(UnitValue.CreatePercentValue(100));

                    var logoCell = new Cell().SetBorder(Border.NO_BORDER).SetVerticalAlignment(VerticalAlignment.TOP);
                    string logo1Path = Path.Combine(_env.WebRootPath, "Images", "Logo1.png");
                    string logo2Path = Path.Combine(_env.WebRootPath, "Images", "Logo2.png");

                    if (System.IO.File.Exists(logo1Path))
                    {
                        var logo1Img = new Image(ImageDataFactory.Create(logo1Path)).ScaleToFit(100, 50);
                        logoCell.Add(logo1Img);
                    }
                    if (System.IO.File.Exists(logo2Path))
                    {
                        logoCell.Add(new Paragraph("\n"));
                        var logo2Img = new Image(ImageDataFactory.Create(logo2Path)).ScaleToFit(100, 50);
                        logoCell.Add(logo2Img);
                    }
                    topTable.AddCell(logoCell);

                    var infoCell = new Cell().SetBorder(Border.NO_BORDER).SetVerticalAlignment(VerticalAlignment.TOP)
                        .SetHorizontalAlignment(HorizontalAlignment.RIGHT)
                        .SetTextAlignment(iText.Layout.Properties.TextAlignment.RIGHT);

                    infoCell.Add(new Paragraph()
                        .Add(new Text("Worldwide Rail and Mining Solutions SA (Pty) Ltd\n").SetFont(bold))
                        .Add(new Text("52 8th Avenue, Edenvale Gauteng 1610\n"))
                        .Add(new Text("Email: adminsa@wwms.co.za\n"))
                        .Add(new Text("T: +27 11 453 2170\n"))
                        .Add(new Text("Website: www.worldwideminingsolutions.co.za\n"))
                        .Add(new Text("Reg. No.: 2019/544337/07\n"))
                        .Add(new Text("Msomi Valuation Services (Pty) Ltd\n").SetFont(bold))
                        .Add(new Text("4 Sheffield Road, Ferryvale, Nigel, 1491\n"))
                        .Add(new Text("T: 011 814 2047\n"))
                        .Add(new Text("VAT: 4400277721\n")));

                    topTable.AddCell(infoCell);
                    document.Add(topTable);

                    document.Add(new LineSeparator(new SolidLine(1f)).SetMarginTop(10).SetMarginBottom(15));
                    document.Add(new Paragraph($"Quote - Asset Code: {wagonNumber}").SetFont(bold).SetFontSize(14).SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER));
                    document.Add(new Paragraph($"Asset Model/Group: {model?.WagonGroup ?? "N/A"}").SetFont(regular).SetFontSize(12).SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER).SetMarginBottom(10));
                    document.Add(new Paragraph($"Assessor: {assessor?.UserName ?? "N/A"}").SetFont(regular).SetFontSize(12).SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER).SetMarginBottom(10));

                    decimal grandTotalRefurbish = 0, grandTotalMissing = 0, grandTotalReplace = 0, grandTotalLabor = 0;

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

                    document.Add(new Paragraph("Lift & Barrel Costs")
                        .SetFont(bold)
                        .SetFontSize(13)
                        .SetMarginTop(15)
                        .SetBackgroundColor(ColorConstants.LIGHT_GRAY)
                        .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER));

                    var liftBarrelTable = new Table(UnitValue.CreatePercentArray(3)).UseAllAvailableWidth();

                    string[] liftBarrelHeaders = { "Description", "Lapsed", "Value (ZAR)" };
                    foreach (var header in liftBarrelHeaders)
                        liftBarrelTable.AddHeaderCell(new Cell().Add(new Paragraph(header).SetFont(bold).SetBackgroundColor(ColorConstants.LIGHT_GRAY)));

                    liftBarrelTable.AddCell(new Cell().Add(new Paragraph("Lift Inspection").SetFont(regular)));
                    liftBarrelTable.AddCell(new Cell().Add(new Paragraph(model?.LiftLapsed ?? "N/A").SetFont(regular)));
                    liftBarrelTable.AddCell(new Cell().Add(new Paragraph(liftCost != 0m ? $"R{liftCost:F2}" : "-").SetFont(regular)));

                    liftBarrelTable.AddCell(new Cell().Add(new Paragraph("Barrel Inspection").SetFont(regular)));
                    liftBarrelTable.AddCell(new Cell().Add(new Paragraph(model?.BarrelLapsed ?? "N/A").SetFont(regular)));
                    liftBarrelTable.AddCell(new Cell().Add(new Paragraph(barrelCost != 0m ? $"R{barrelCost:F2}" : "-").SetFont(regular)));

                    liftBarrelTable.AddCell(new Cell(1, 2)
                        .Add(new Paragraph("Subtotal").SetFont(bold))
                        .SetTextAlignment(iText.Layout.Properties.TextAlignment.RIGHT)
                        .SetBackgroundColor(ColorConstants.LIGHT_GRAY));

                    liftBarrelTable.AddCell(new Cell().Add(new Paragraph($"R{liftBarrelTotal:F2}").SetFont(bold)));

                    document.Add(liftBarrelTable);

                    // --- Inspection Tables ---
                    foreach (var group in inspectionSources)
                    {
                        var parts = group.Value;
                        if (!parts.Any()) continue;

                        var partsWithNumbers = parts.Select(p =>
                        {
                            decimal refVal = 0, missVal = 0, replVal = 0, labVal = 0;

                            if (p.RefurbishValue != null && !string.IsNullOrWhiteSpace(p.RefurbishValue.ToString()))
                                decimal.TryParse(p.RefurbishValue.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out refVal);

                            if (p.MissingValue != null && !string.IsNullOrWhiteSpace(p.MissingValue.ToString()))
                                decimal.TryParse(p.MissingValue.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out missVal);

                            if (p.ReplaceValue != null && !string.IsNullOrWhiteSpace(p.ReplaceValue.ToString()))
                                decimal.TryParse(p.ReplaceValue.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out replVal);

                            if (p.LaborValue != null && !string.IsNullOrWhiteSpace(p.LaborValue.ToString()))
                                decimal.TryParse(p.LaborValue.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out labVal);

                            return new
                            {
                                p.FormID,
                                p.PartDescr,
                                RefurbishValue = refVal,
                                MissingValue = missVal,
                                ReplaceValue = replVal,
                                LaborValue = labVal
                            };
                        }).ToList();

                        decimal totalRefurbish = partsWithNumbers.Sum(p => p.RefurbishValue);
                        decimal totalMissing = partsWithNumbers.Sum(p => p.MissingValue);
                        decimal totalReplace = partsWithNumbers.Sum(p => p.ReplaceValue);
                        decimal totalLabor = partsWithNumbers.Sum(p => p.LaborValue);

                        grandTotalRefurbish += totalRefurbish;
                        grandTotalMissing += totalMissing;
                        grandTotalReplace += totalReplace;
                        grandTotalLabor += totalLabor;

                        document.Add(new Paragraph(group.Key)
                            .SetFont(bold)
                            .SetFontSize(13)
                            .SetMarginTop(15)
                            .SetBackgroundColor(ColorConstants.LIGHT_GRAY)
                            .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER));

                        var table = new Table(UnitValue.CreatePercentArray(7)).UseAllAvailableWidth();
                        string[] headers = { "No.", "Form ID", "Part Description", "Refurbish Value", "Missing Value", "Replace Value", "Labor Value" };
                        foreach (var header in headers)
                            table.AddHeaderCell(new Cell().Add(new Paragraph(header).SetFont(bold).SetBackgroundColor(ColorConstants.LIGHT_GRAY)));

                        int index = 1;
                        foreach (var p in partsWithNumbers)
                        {
                            table.AddCell(new Cell().Add(new Paragraph(index.ToString()).SetFont(regular)));
                            table.AddCell(new Cell().Add(new Paragraph(p.FormID.ToString()).SetFont(regular)));
                            table.AddCell(new Cell().Add(new Paragraph(p.PartDescr).SetFont(regular)));
                            table.AddCell(new Cell().Add(new Paragraph(p.RefurbishValue != 0 ? $"R{p.RefurbishValue:F2}" : "-").SetFont(regular)));
                            table.AddCell(new Cell().Add(new Paragraph(p.MissingValue != 0 ? $"R{p.MissingValue:F2}" : "-").SetFont(regular)));
                            table.AddCell(new Cell().Add(new Paragraph(p.ReplaceValue != 0 ? $"R{p.ReplaceValue:F2}" : "-").SetFont(regular)));
                            table.AddCell(new Cell().Add(new Paragraph(p.LaborValue != 0 ? $"R{p.LaborValue:F2}" : "-").SetFont(regular)));
                            index++;
                        }

                        table.AddCell(new Cell(1, 3)
                            .Add(new Paragraph("Subtotal").SetFont(bold))
                            .SetTextAlignment(iText.Layout.Properties.TextAlignment.RIGHT)
                            .SetBackgroundColor(ColorConstants.LIGHT_GRAY));

                        table.AddCell(new Cell().Add(new Paragraph($"R{totalRefurbish:F2}").SetFont(bold)));
                        table.AddCell(new Cell().Add(new Paragraph($"R{totalMissing:F2}").SetFont(bold)));
                        table.AddCell(new Cell().Add(new Paragraph($"R{totalReplace:F2}").SetFont(bold)));
                        table.AddCell(new Cell().Add(new Paragraph($"R{totalLabor:F2}").SetFont(bold)));

                        document.Add(table);
                    }

                    decimal marketValue = ParseDecimalSafe(master?.MarketValue);
                    decimal rts = grandTotalRefurbish + grandTotalMissing + grandTotalReplace + grandTotalLabor + liftBarrelTotal;
                    decimal assetValue = marketValue - rts;

                    decimal grandTotal = grandTotalRefurbish + grandTotalMissing + grandTotalReplace + liftBarrelTotal + grandTotalLabor + assetValue;

                    // --- Final Grand Totals ---
                    document.Add(new Paragraph("\nGrand Totals").SetFont(bold).SetFontSize(13).SetTextAlignment(iText.Layout.Properties.TextAlignment.RIGHT));

                    var totalsTable = new Table(UnitValue.CreatePercentArray(7)).UseAllAvailableWidth();
                    totalsTable.AddHeaderCell(new Cell().Add(new Paragraph("Refurbish Total").SetFont(bold)));
                    totalsTable.AddHeaderCell(new Cell().Add(new Paragraph("Missing Total").SetFont(bold)));
                    totalsTable.AddHeaderCell(new Cell().Add(new Paragraph("Replace Total").SetFont(bold)));
                    totalsTable.AddHeaderCell(new Cell().Add(new Paragraph("Lift & Barrel Total").SetFont(bold)));
                    totalsTable.AddHeaderCell(new Cell().Add(new Paragraph("Labor Total").SetFont(bold)));
                    totalsTable.AddHeaderCell(new Cell().Add(new Paragraph("Market Value").SetFont(bold)));
                    totalsTable.AddHeaderCell(new Cell().Add(new Paragraph("Return to Service Cost").SetFont(bold)));

                    totalsTable.AddCell(new Cell().Add(new Paragraph($"R{grandTotalRefurbish:F2}").SetFont(regular)));
                    totalsTable.AddCell(new Cell().Add(new Paragraph($"R{grandTotalMissing:F2}").SetFont(regular)));
                    totalsTable.AddCell(new Cell().Add(new Paragraph($"R{grandTotalReplace:F2}").SetFont(regular)));
                    totalsTable.AddCell(new Cell().Add(new Paragraph($"R{liftBarrelTotal:F2}").SetFont(regular)));
                    totalsTable.AddCell(new Cell().Add(new Paragraph($"R{grandTotalLabor:F2}").SetFont(regular)));
                    totalsTable.AddCell(new Cell().Add(new Paragraph($"R{assetValue:F2}").SetFont(regular)));
                    totalsTable.AddCell(new Cell().Add(new Paragraph($"R{grandTotal:F2}").SetFont(bold)));

                    document.Add(totalsTable);

                    // --- Footer Image ---
                    string footerPath = Path.Combine(_env.WebRootPath, "Images", "Footer1.png");
                    if (System.IO.File.Exists(footerPath))
                    {
                        var pageSize = pdf.GetDefaultPageSize();
                        float pageWidth = pageSize.GetWidth();

                        var footerImg = new Image(ImageDataFactory.Create(footerPath));
                        float scaleX = pageWidth / footerImg.GetImageWidth();
                        footerImg.ScaleToFit(pageWidth, footerImg.GetImageHeight() * scaleX);
                        footerImg.SetFixedPosition(0, 0);
                        document.Add(footerImg);
                    }

                    document.Close();
                    pdf.Close();
                }

                var dashboard = await _context.WagonDashboardUploadeds.FirstOrDefaultAsync(d => d.WagonNumber == wagonNumber);
                string relativePath = Path.Combine("InspectionPdf", "Wagons", "QuotePdf", fileName).Replace("\\", "/");

                if (dashboard != null)
                {
                    dashboard.AssessmentQuote = relativePath;
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
        private void AddLogoCell(Table table, string imagePath)
        {
            var cell = new Cell().SetBorder(Border.NO_BORDER);
            if (System.IO.File.Exists(imagePath))
            {
                var img = new Image(ImageDataFactory.Create(imagePath)).ScaleToFit(100, 50);
                cell.Add(img);
            }
            table.AddCell(cell);
        }

        [HttpPost("GenerateAndSaveSowPdf")]
        public async Task<IActionResult> GenerateAndSaveSowPdf([FromBody] QuotePdfRequest request)
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

            var model = await _context.WagonInfoCaptures.AsNoTracking().FirstOrDefaultAsync(p => p.WagonNumber == wagonNumber);
            var dash = await _context.WagonDashboards.AsNoTracking().FirstOrDefaultAsync(p => p.WagonNumber == wagonNumber);

            // Validate basic presence
            if (model == null)
                return BadRequest($"No WagonInfoCaptures record found for wagon {wagonNumber}.");

            // Define each inspection table source and label
            var inspectionSources = new Dictionary<string, IEnumerable<dynamic>>
            {
                { "Air Brake Inspection", await _context.AirBrakePartsInspects.Where(p => p.WagonNumber == wagonNumber).ToListAsync() },
                { "Bottom Discharge Inspection", await _context.BottomDischargeInspects.Where(p => p.WagonNumber == wagonNumber).ToListAsync() },
                { "Doors Inspection", await _context.DoorsInspects.Where(p => p.WagonNumber == wagonNumber).ToListAsync() },
                { "Floor Inspection", await _context.FloorInspects.Where(p => p.WagonNumber == wagonNumber).ToListAsync() },
                { "Stanchions Inspection", await _context.StanchionsInspects.Where(p => p.WagonNumber == wagonNumber).ToListAsync() },
                { "Tankers Inspection", await _context.TankersInspects.Where(p => p.WagonNumber == wagonNumber).ToListAsync() },
                { "Twistlocks Inspection", await _context.TwistlocksInspects.Where(p => p.WagonNumber == wagonNumber).ToListAsync() },
                { "Vacuum Brake Inspection", await _context.VacBrakePartsInspects.Where(p => p.WagonNumber == wagonNumber).ToListAsync() },
                { "Wagon Parts Inspection", await _context.WagonPartsInspects.Where(p => p.WagonNumber == wagonNumber).ToListAsync() }
            };

            if (inspectionSources.All(s => !s.Value.Any()))
                return NotFound("No parts found for this wagon number.");

            string folderPath = Path.Combine(_env.WebRootPath, "InspectionPdf", "Wagons", "SowPdf");
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            // Create sanitized filename (timestamp + guid to avoid collisions)
            string safeGroup = SanitizeFileName(model?.WagonGroup ?? "Group");
            string fileName = $"{wagonNumber}_{safeGroup}_Sow_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N").Substring(0, 8)}.pdf";
            string filePath = Path.Combine(folderPath, fileName);

            try
            {
                // --- Generate PDF ---
                using (var writer = new PdfWriter(filePath))
                using (var pdf = new iText.Kernel.Pdf.PdfDocument(writer))
                using (var document = new Document(pdf))
                {
                    var bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                    var regular = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

                    // --- Header section ---
                    float[] columnWidths = { 150f, 350f };
                    var topTable = new Table(UnitValue.CreatePointArray(columnWidths)).SetWidth(UnitValue.CreatePercentValue(100));

                    var logoCell = new Cell().SetBorder(Border.NO_BORDER).SetVerticalAlignment(VerticalAlignment.TOP);
                    string logo1Path = Path.Combine(_env.WebRootPath, "Images", "Logo1.png");
                    string logo2Path = Path.Combine(_env.WebRootPath, "Images", "Logo2.png");

                    if (System.IO.File.Exists(logo1Path))
                    {
                        var logo1Img = new Image(ImageDataFactory.Create(logo1Path)).ScaleToFit(100, 50);
                        logoCell.Add(logo1Img);
                    }
                    if (System.IO.File.Exists(logo2Path))
                    {
                        logoCell.Add(new Paragraph("\n"));
                        var logo2Img = new Image(ImageDataFactory.Create(logo2Path)).ScaleToFit(100, 50);
                        logoCell.Add(logo2Img);
                    }
                    topTable.AddCell(logoCell);

                    var infoCell = new Cell().SetBorder(Border.NO_BORDER).SetVerticalAlignment(VerticalAlignment.TOP)
                        .SetHorizontalAlignment(HorizontalAlignment.RIGHT)
                        .SetTextAlignment(iText.Layout.Properties.TextAlignment.RIGHT);

                    infoCell.Add(new Paragraph()
                        .Add(new Text("Worldwide Rail and Mining Solutions SA (Pty) Ltd\n").SetFont(bold))
                        .Add(new Text("52 8th Avenue, Edenvale Gauteng 1610\n"))
                        .Add(new Text("Email: adminsa@wwms.co.za\n"))
                        .Add(new Text("T: +27 11 453 2170\n"))
                        .Add(new Text("Website: www.worldwideminingsolutions.co.za\n"))
                        .Add(new Text("Reg. No.: 2019/544337/07\n"))
                        .Add(new Text("Msomi Valuation Services (Pty) Ltd\n").SetFont(bold))
                        .Add(new Text("4 Sheffield Road, Ferryvale, Nigel, 1491\n"))
                        .Add(new Text("T: 011 814 2047\n"))
                        .Add(new Text("VAT: 4400277721\n")));

                    topTable.AddCell(infoCell);
                    document.Add(topTable);

                    document.Add(new LineSeparator(new SolidLine(1f)).SetMarginTop(10).SetMarginBottom(15));
                    document.Add(new Paragraph($"Quote - Asset Code: {wagonNumber}").SetFont(bold).SetFontSize(14).SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER));
                    document.Add(new Paragraph($"Asset Model/Group: {model?.WagonGroup ?? "N/A"}").SetFont(regular).SetFontSize(12).SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER).SetMarginBottom(10));
                    document.Add(new Paragraph($"Assessor: {assessor?.UserName ?? "N/A"}").SetFont(regular).SetFontSize(12).SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER).SetMarginBottom(10));

                    document.Add(new Paragraph("Lift & Barrel")
                        .SetFont(bold)
                        .SetFontSize(13)
                        .SetMarginTop(15)
                        .SetBackgroundColor(ColorConstants.LIGHT_GRAY)
                        .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER));

                    var liftBarrelTable = new Table(UnitValue.CreatePercentArray(2)).UseAllAvailableWidth();

                    string[] liftBarrelHeaders = { "Description", "Lapsed" };
                    foreach (var header in liftBarrelHeaders)
                        liftBarrelTable.AddHeaderCell(new Cell().Add(new Paragraph(header).SetFont(bold).SetBackgroundColor(ColorConstants.LIGHT_GRAY)));

                    liftBarrelTable.AddCell(new Cell().Add(new Paragraph("Lift Lapsed").SetFont(regular)));
                    liftBarrelTable.AddCell(new Cell().Add(new Paragraph(model?.LiftLapsed ?? "N/A").SetFont(regular)));

                    liftBarrelTable.AddCell(new Cell().Add(new Paragraph("Barrel Lasped").SetFont(regular)));
                    liftBarrelTable.AddCell(new Cell().Add(new Paragraph(model?.BarrelLapsed ?? "N/A").SetFont(regular))); ;

                    document.Add(liftBarrelTable);

                    // --- Inspection Tables ---
                    foreach (var group in inspectionSources)
                    {
                        var parts = group.Value;
                        if (!parts.Any()) continue;

                        //PLEASE ADD
                        string NormalizeCheck(object? obj)
                        {
                            if (obj == null) return "-";
                            string s = obj.ToString()?.Trim() ?? "";
                            if (string.IsNullOrWhiteSpace(s)) return "-";
                            if (s.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
                                s.Equals("y", StringComparison.OrdinalIgnoreCase) ||
                                s.Equals("true", StringComparison.OrdinalIgnoreCase))
                                return "Yes";
                            if (s.Equals("no", StringComparison.OrdinalIgnoreCase) ||
                                s.Equals("n", StringComparison.OrdinalIgnoreCase) ||
                                s.Equals("false", StringComparison.OrdinalIgnoreCase))
                                return "-";
                            // If DB contains something unexpected, show it trimmed (you can change to "-" if you prefer)
                            return s;
                        }

                        // map parts to trimmed + normalized fields
                        var partsWithNumbers = parts.Select(p => new
                        {
                            FormId = p.FormId,
                            PartDescr = p.PartDescr,
                            GoodCheck = NormalizeCheck(p.GoodCheck),
                            RefurbishCheck = NormalizeCheck(p.RefurbishCheck),
                            MissingCheck = NormalizeCheck(p.MissingCheck),
                            ReplaceCheck = NormalizeCheck(p.ReplaceCheck)
                        }).ToList();

                        document.Add(new Paragraph(group.Key)
                            .SetFont(bold)
                            .SetFontSize(13)
                            .SetMarginTop(15)
                            .SetBackgroundColor(ColorConstants.LIGHT_GRAY)
                            .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER));

                        var table = new Table(UnitValue.CreatePercentArray(7)).UseAllAvailableWidth();
                        string[] headers = { "No.", "Form ID", "Part Description", "Good", "Refurbish", "Missing", "Damage" }; //PLEASE ADJUST (NEW)
                        foreach (var header in headers)
                            table.AddHeaderCell(new Cell().Add(new Paragraph(header).SetFont(bold).SetBackgroundColor(ColorConstants.LIGHT_GRAY)));

                        int index = 1;
                        foreach (var p in partsWithNumbers)
                        {
                            table.AddCell(new Cell().Add(new Paragraph(index.ToString()).SetFont(regular)));
                            table.AddCell(new Cell().Add(new Paragraph(p.FormId?.ToString() ?? "").SetFont(regular)));
                            table.AddCell(new Cell().Add(new Paragraph(p.PartDescr ?? "").SetFont(regular)));
                            table.AddCell(new Cell().Add(new Paragraph(p.GoodCheck).SetFont(regular)));        // shows Yes or -
                            table.AddCell(new Cell().Add(new Paragraph(p.RefurbishCheck).SetFont(regular)));   // shows Yes or -
                            table.AddCell(new Cell().Add(new Paragraph(p.MissingCheck).SetFont(regular)));     // shows Yes or -
                            table.AddCell(new Cell().Add(new Paragraph(p.ReplaceCheck).SetFont(regular)));     // shows Yes or -
                            index++;
                        }

                        document.Add(table);
                    }

                    // --- Footer Image ---
                    string footerPath = Path.Combine(_env.WebRootPath, "Images", "Footer1.png");
                    if (System.IO.File.Exists(footerPath))
                    {
                        var pageSize = pdf.GetDefaultPageSize();
                        float pageWidth = pageSize.GetWidth();

                        var footerImg = new Image(ImageDataFactory.Create(footerPath));
                        float scaleX = pageWidth / footerImg.GetImageWidth();
                        footerImg.ScaleToFit(pageWidth, footerImg.GetImageHeight() * scaleX);
                        footerImg.SetFixedPosition(0, 0).SetMarginTop(15);
                        document.Add(footerImg);
                    }

                    document.Close();
                    pdf.Close();
                }

                //PLEASE ADJUST
                var dashboard = await _context.WagonDashboards.FirstOrDefaultAsync(d => d.WagonNumber == wagonNumber);
                string relativePath = Path.Combine("InspectionPdf", "Wagons", "SowPdf", fileName).Replace("\\", "/");

                if (dashboard != null)
                {
                    dashboard.AssessmentSow = relativePath;
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

        [HttpPost("RegenerateAndSaveSowPdf")]
        public async Task<IActionResult> RegenerateAndSaveSowPdf([FromBody] QuotePdfRequestUpload request)
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

            var model = await _context.WagonInfoCaptures.AsNoTracking().FirstOrDefaultAsync(p => p.WagonNumber == wagonNumber);
            var dash = await _context.WagonDashboardUploadeds.AsNoTracking().FirstOrDefaultAsync(p => p.WagonNumber == wagonNumber);

            // Validate basic presence
            if (model == null)
                return BadRequest($"No WagonInfoCaptures record found for wagon {wagonNumber}.");

            // Define each inspection table source and label
            var inspectionSources = new Dictionary<string, IEnumerable<dynamic>>
            {
                { "Air Brake Inspection", await _context.AirBrakePartsInspects.Where(p => p.WagonNumber == wagonNumber).ToListAsync() },
                { "Bottom Discharge Inspection", await _context.BottomDischargeInspects.Where(p => p.WagonNumber == wagonNumber).ToListAsync() },
                { "Doors Inspection", await _context.DoorsInspects.Where(p => p.WagonNumber == wagonNumber).ToListAsync() },
                { "Floor Inspection", await _context.FloorInspects.Where(p => p.WagonNumber == wagonNumber).ToListAsync() },
                { "Stanchions Inspection", await _context.StanchionsInspects.Where(p => p.WagonNumber == wagonNumber).ToListAsync() },
                { "Tankers Inspection", await _context.TankersInspects.Where(p => p.WagonNumber == wagonNumber).ToListAsync() },
                { "Twistlocks Inspection", await _context.TwistlocksInspects.Where(p => p.WagonNumber == wagonNumber).ToListAsync() },
                { "Vacuum Brake Inspection", await _context.VacBrakePartsInspects.Where(p => p.WagonNumber == wagonNumber).ToListAsync() },
                { "Wagon Parts Inspection", await _context.WagonPartsInspects.Where(p => p.WagonNumber == wagonNumber).ToListAsync() }
            };

            if (inspectionSources.All(s => !s.Value.Any()))
                return NotFound("No parts found for this wagon number.");

            string folderPath = Path.Combine(_env.WebRootPath, "InspectionPdf", "Wagons", "SowPdf");
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            // Create sanitized filename (timestamp + guid to avoid collisions)
            string safeGroup = SanitizeFileName(model?.WagonGroup ?? "Group");
            string fileName = $"{wagonNumber}_{safeGroup}_Sow_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N").Substring(0, 8)}.pdf";
            string filePath = Path.Combine(folderPath, fileName);

            try
            {
                // --- Generate PDF ---
                using (var writer = new PdfWriter(filePath))
                using (var pdf = new iText.Kernel.Pdf.PdfDocument(writer))
                using (var document = new Document(pdf))
                {
                    var bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                    var regular = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

                    // --- Header section ---
                    float[] columnWidths = { 150f, 350f };
                    var topTable = new Table(UnitValue.CreatePointArray(columnWidths)).SetWidth(UnitValue.CreatePercentValue(100));

                    var logoCell = new Cell().SetBorder(Border.NO_BORDER).SetVerticalAlignment(VerticalAlignment.TOP);
                    string logo1Path = Path.Combine(_env.WebRootPath, "Images", "Logo1.png");
                    string logo2Path = Path.Combine(_env.WebRootPath, "Images", "Logo2.png");

                    if (System.IO.File.Exists(logo1Path))
                    {
                        var logo1Img = new Image(ImageDataFactory.Create(logo1Path)).ScaleToFit(100, 50);
                        logoCell.Add(logo1Img);
                    }
                    if (System.IO.File.Exists(logo2Path))
                    {
                        logoCell.Add(new Paragraph("\n"));
                        var logo2Img = new Image(ImageDataFactory.Create(logo2Path)).ScaleToFit(100, 50);
                        logoCell.Add(logo2Img);
                    }
                    topTable.AddCell(logoCell);

                    var infoCell = new Cell().SetBorder(Border.NO_BORDER).SetVerticalAlignment(VerticalAlignment.TOP)
                        .SetHorizontalAlignment(HorizontalAlignment.RIGHT)
                        .SetTextAlignment(iText.Layout.Properties.TextAlignment.RIGHT);

                    infoCell.Add(new Paragraph()
                        .Add(new Text("Worldwide Rail and Mining Solutions SA (Pty) Ltd\n").SetFont(bold))
                        .Add(new Text("52 8th Avenue, Edenvale Gauteng 1610\n"))
                        .Add(new Text("Email: adminsa@wwms.co.za\n"))
                        .Add(new Text("T: +27 11 453 2170\n"))
                        .Add(new Text("Website: www.worldwideminingsolutions.co.za\n"))
                        .Add(new Text("Reg. No.: 2019/544337/07\n"))
                        .Add(new Text("Msomi Valuation Services (Pty) Ltd\n").SetFont(bold))
                        .Add(new Text("4 Sheffield Road, Ferryvale, Nigel, 1491\n"))
                        .Add(new Text("T: 011 814 2047\n"))
                        .Add(new Text("VAT: 4400277721\n")));

                    topTable.AddCell(infoCell);
                    document.Add(topTable);

                    document.Add(new LineSeparator(new SolidLine(1f)).SetMarginTop(10).SetMarginBottom(15));
                    document.Add(new Paragraph($"Quote - Asset Code: {wagonNumber}").SetFont(bold).SetFontSize(14).SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER));
                    document.Add(new Paragraph($"Asset Model/Group: {model?.WagonGroup ?? "N/A"}").SetFont(regular).SetFontSize(12).SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER).SetMarginBottom(10));
                    document.Add(new Paragraph($"Assessor: {assessor?.UserName ?? "N/A"}").SetFont(regular).SetFontSize(12).SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER).SetMarginBottom(10));

                    document.Add(new Paragraph("Lift & Barrel")
                        .SetFont(bold)
                        .SetFontSize(13)
                        .SetMarginTop(15)
                        .SetBackgroundColor(ColorConstants.LIGHT_GRAY)
                        .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER));

                    var liftBarrelTable = new Table(UnitValue.CreatePercentArray(2)).UseAllAvailableWidth();

                    string[] liftBarrelHeaders = { "Description", "Lapsed" };
                    foreach (var header in liftBarrelHeaders)
                        liftBarrelTable.AddHeaderCell(new Cell().Add(new Paragraph(header).SetFont(bold).SetBackgroundColor(ColorConstants.LIGHT_GRAY)));

                    liftBarrelTable.AddCell(new Cell().Add(new Paragraph("Lift Lapsed").SetFont(regular)));
                    liftBarrelTable.AddCell(new Cell().Add(new Paragraph(model?.LiftLapsed ?? "N/A").SetFont(regular)));

                    liftBarrelTable.AddCell(new Cell().Add(new Paragraph("Barrel Lasped").SetFont(regular)));
                    liftBarrelTable.AddCell(new Cell().Add(new Paragraph(model?.BarrelLapsed ?? "N/A").SetFont(regular))); ;

                    document.Add(liftBarrelTable);

                    // --- Inspection Tables ---
                    foreach (var group in inspectionSources)
                    {
                        var parts = group.Value;
                        if (!parts.Any()) continue;

                        //PLEASE ADD
                        string NormalizeCheck(object? obj)
                        {
                            if (obj == null) return "-";
                            string s = obj.ToString()?.Trim() ?? "";
                            if (string.IsNullOrWhiteSpace(s)) return "-";
                            if (s.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
                                s.Equals("y", StringComparison.OrdinalIgnoreCase) ||
                                s.Equals("true", StringComparison.OrdinalIgnoreCase))
                                return "Yes";
                            if (s.Equals("no", StringComparison.OrdinalIgnoreCase) ||
                                s.Equals("n", StringComparison.OrdinalIgnoreCase) ||
                                s.Equals("false", StringComparison.OrdinalIgnoreCase))
                                return "-";
                            // If DB contains something unexpected, show it trimmed (you can change to "-" if you prefer)
                            return s;
                        }

                        // map parts to trimmed + normalized fields
                        var partsWithNumbers = parts.Select(p => new
                        {
                            FormId = p.FormId,
                            PartDescr = p.PartDescr,
                            GoodCheck = NormalizeCheck(p.GoodCheck),
                            RefurbishCheck = NormalizeCheck(p.RefurbishCheck),
                            MissingCheck = NormalizeCheck(p.MissingCheck),
                            ReplaceCheck = NormalizeCheck(p.ReplaceCheck)
                        }).ToList();

                        document.Add(new Paragraph(group.Key)
                            .SetFont(bold)
                            .SetFontSize(13)
                            .SetMarginTop(15)
                            .SetBackgroundColor(ColorConstants.LIGHT_GRAY)
                            .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER));

                        var table = new Table(UnitValue.CreatePercentArray(7)).UseAllAvailableWidth();
                        string[] headers = { "No.", "Form ID", "Part Description", "Good", "Refurbish", "Missing", "Damage" }; //PLEASE ADJUST (NEW)
                        foreach (var header in headers)
                            table.AddHeaderCell(new Cell().Add(new Paragraph(header).SetFont(bold).SetBackgroundColor(ColorConstants.LIGHT_GRAY)));

                        int index = 1;
                        foreach (var p in partsWithNumbers)
                        {
                            table.AddCell(new Cell().Add(new Paragraph(index.ToString()).SetFont(regular)));
                            table.AddCell(new Cell().Add(new Paragraph(p.FormId?.ToString() ?? "").SetFont(regular)));
                            table.AddCell(new Cell().Add(new Paragraph(p.PartDescr ?? "").SetFont(regular)));
                            table.AddCell(new Cell().Add(new Paragraph(p.GoodCheck).SetFont(regular)));        // shows Yes or -
                            table.AddCell(new Cell().Add(new Paragraph(p.RefurbishCheck).SetFont(regular)));   // shows Yes or -
                            table.AddCell(new Cell().Add(new Paragraph(p.MissingCheck).SetFont(regular)));     // shows Yes or -
                            table.AddCell(new Cell().Add(new Paragraph(p.ReplaceCheck).SetFont(regular)));     // shows Yes or -
                            index++;
                        }

                        document.Add(table);
                    }

                    // --- Footer Image ---
                    string footerPath = Path.Combine(_env.WebRootPath, "Images", "Footer1.png");
                    if (System.IO.File.Exists(footerPath))
                    {
                        var pageSize = pdf.GetDefaultPageSize();
                        float pageWidth = pageSize.GetWidth();

                        var footerImg = new Image(ImageDataFactory.Create(footerPath));
                        float scaleX = pageWidth / footerImg.GetImageWidth();
                        footerImg.ScaleToFit(pageWidth, footerImg.GetImageHeight() * scaleX);
                        footerImg.SetFixedPosition(0, 0).SetMarginTop(15);
                        document.Add(footerImg);
                    }

                    document.Close();
                    pdf.Close();
                }

                //PLEASE ADJUST
                var dashboard = await _context.WagonDashboardUploadeds.FirstOrDefaultAsync(d => d.WagonNumber == wagonNumber);
                string relativePath = Path.Combine("InspectionPdf", "Wagons", "SowPdf", fileName).Replace("\\", "/");

                if (dashboard != null)
                {
                    dashboard.AssessmentSow = relativePath;
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
        [HttpPost("GenerateAndSaveSowPdfForOldLoco")]
        public async Task<IActionResult> GenerateAndSaveSowPdfForOldLoco()
        {
            var existingLocoDashboards = await _context.LocoDashboards
                .Where(ld =>  ld.AssessmentCert != "Not Ready")
                .ToListAsync();
            foreach(var item in existingLocoDashboards)
            {
                var locoNumber = item.LocoNumber;
                var request = new LocoQuotePdfRequest
                {
                    LocoNumber = locoNumber.ToString(),
                    UserId = item.InspectorId
                };
                await GenerateAndSaveSowPdfForLoco(request);
            }
            return Ok(new { message = "PDFs generated for existing locos." });
        }
        [HttpPost("GenerateAndSaveSowPdfForOldWagon")]
        public async Task<IActionResult> GenerateAndSaveSowPdfForOldWagon()
        {
            var existingLocoDashboards = await _context.WagonDashboards
                .Where(ld => ld.AssessmentCert != "Not Ready")
                .ToListAsync();
            foreach (var item in existingLocoDashboards)
            {
                var locoNumber = item.WagonNumber;
                var request = new QuotePdfRequest
                {
                    WagonNumber = locoNumber.ToString(),
                    UserId = item.InspectorId
                };
                await GenerateAndSaveSowPdf(request);
            }
            return Ok(new { message = "PDFs generated for existing wagons." });
        }
        [HttpPost("GenerateAndSaveSowPdfForLoco")]
        public async Task<IActionResult> GenerateAndSaveSowPdfForLoco([FromBody] LocoQuotePdfRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.LocoNumber))
                return BadRequest("Invalid request: WagonNumber is required.");

            _context.Database.SetCommandTimeout(180);

            if (!int.TryParse(request.LocoNumber, out int locoNumber))
                return BadRequest("Invalid WagonNumber format.");

            string userId = request.UserId ?? "";

            // Fetch related records (non-tracking reads where appropriate)
            var assessor = string.IsNullOrWhiteSpace(userId) ? null :
                await _context.LeaseCoUsers.AsNoTracking().FirstOrDefaultAsync(a => a.UserId == userId);

            var master = await _context.MasterLocos.AsNoTracking().FirstOrDefaultAsync(m => m.LocoNumber == locoNumber);
            var model = await _context.LocoInfoCaptures.AsNoTracking().FirstOrDefaultAsync(p => p.LocoNumber == locoNumber);
            var dash = await _context.LocoDashboards.AsNoTracking().FirstOrDefaultAsync(p => locoNumber == locoNumber);

            // Validate basic presence
            if (model == null)
                return BadRequest($"No LocoInfoCaptures record found for loco {locoNumber}.");
            var LocoNumber = locoNumber;
            var inspectionSources = new Dictionary<string, IEnumerable<dynamic>>();
            if (model == null)
            {
                return NotFound("Loco not found.");

            }
            if (model.LocoModel == "E18")
            {

                // Define each inspection table source and label
                inspectionSources = new Dictionary<string, IEnumerable<dynamic>>
            {
                { "BELOW DECK Walk Around Inspection", await _context.E18bdinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
                { "Front of Loco Inspection", await _context.E18flinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
                { "Back of Loco Inspection", await _context.E18beinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
                { "18E Cab Inspection", await _context.E18eeinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
                { "Low Voltage Compartment Inspection", await _context.E18lvinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
                { "Corridor Inspection", await _context.E18crinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
                { "HT High Voltage Compartment Inspection", await _context.E18hvinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
                { "Motor Alternator Set Inspection", await _context.E18mainspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
                { "Exhauster Inspection", await _context.E18ehinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
          { "Machine Brake Compartment Inspection", await _context.E18mbinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
                { "High Speed Circuit Breaker Compartment Inspection", await _context.E18hsinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
                { "Exciter Set 2 Inspection", await _context.E18esinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
                { "High Voltage Compartment No 1 Inspection", await _context.E18hcinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
                { "Compressor Compartment Inspection", await _context.E18ccinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
                { "Cab and Toilet No 1 End Inspection", await _context.E18ctinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
                { "Roof Top Inspection", await _context.E18rfinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() }

            };
            }

            if (model.LocoModel == "GE34")
            {

                inspectionSources = new Dictionary<string, IEnumerable<dynamic>>
{
    { "Walk Around / Below Deck Inspection", await _context.Ge34bdinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
    { "Front of Loco Inspection", await _context.Ge34flinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
    { "Short Nose Inspection", await _context.Ge34sninspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
    { "Cab Loco Inspection", await _context.Ge34clinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
    { "Electrical Cab Inspection", await _context.Ge34ecinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
    { "Battery Switch Inspection", await _context.Ge34bsinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
    { "Outside Driver’s Door Inspection", await _context.Ge34odinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
    { "Blower Compartment Inspection", await _context.Ge34bcinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
    { "Alternator Compartment Inspection", await _context.Ge34acinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
    { "Engine Deck Inspection", await _context.Ge34edinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
    { "Compressor Fan Inspection", await _context.Ge34cfinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
    { "End Deck Inspection", await _context.Ge34deinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
    { "Roof Top Inspection", await _context.Ge34rfinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() }
};

            }

            if (model.LocoModel == "GE35")
            {

                inspectionSources = new Dictionary<string, IEnumerable<dynamic>>
{
    { "Walk Around / Below Deck Inspection", await _context.Ge35bdinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
    { "Front of Loco Inspection", await _context.Ge35flinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
    { "Short Nose Inspection", await _context.Ge35sninspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
    { "Cab Loco Inspection", await _context.Ge35clinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
    { "Electrical Cab Inspection", await _context.Ge35ecinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
    { "Battery Switch Inspection", await _context.Ge35bsinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
    { "Outside Driver’s Door Inspection", await _context.Ge35odinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
    { "Blower Compartment Inspection", await _context.Ge35bcinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
    { "Main Gen Compartment Inspection", await _context.Ge35mginspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
    { "Engine Deck Inspection", await _context.Ge35edinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
    { "Compressor Fan Inspection", await _context.Ge35cfinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
    { "End Deck Inspection", await _context.Ge35deinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
    { "Roof Top Inspection", await _context.Ge35rfinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() }
};

            }
            if (model.LocoModel == "GE36")
            {
                inspectionSources = new Dictionary<string, IEnumerable<dynamic>>
    {
        { "Walk Around / Below Deck Inspect", await _context.Ge36bdinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Front Loco Inspect", await _context.Ge36flinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Short Nose Inspect", await _context.Ge36sninspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Cab Loco Inspect", await _context.Ge36clinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Elect Cab Inspect", await _context.Ge36ecinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Central Air Inspect", await _context.Ge36cainspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Main Gen Compartment Inspect", await _context.Ge36mginspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Engine Deck Inspect", await _context.Ge36edinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Compressor Fan Inspect", await _context.Ge36cfinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "End Deck Inspect", await _context.Ge36deinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Roof Top Inspect", await _context.Ge36rfinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() }
    };
            }
            if (model.LocoModel == "GM34")
            {
                inspectionSources = new Dictionary<string, IEnumerable<dynamic>>
    {
        { "Below Deck From No.1A to 1B", await _context.Gm34bdinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Front of Loco Above", await _context.Gm34flinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Short Nose", await _context.Gm34sninspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Cab of Loco Assistant Entrance", await _context.Gm34clinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Elect Cabinet Top Left", await _context.Gm34elinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Battery Knife Switch Compartment", await _context.Gm34bsinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Left Middle Door", await _context.Gm34lminspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Circuit Breaker Control Panel", await _context.Gm34cbinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Top Right Panel", await _context.Gm34trinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Middle Panel", await _context.Gm34mpinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Bottom Left Panel", await _context.Gm34blinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Central Air Compartment", await _context.Gm34cainspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Engine and Above Deck", await _context.Gm34edinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Compressor Fan Rad Compartment", await _context.Gm34cfinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "No.2 End above deck", await _context.Gm34deinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Roof Top Inspect", await _context.Gm34rfinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() }
    };
            }
            if (model.LocoModel == "GM35")
            {
                inspectionSources = new Dictionary<string, IEnumerable<dynamic>>
    {
        { "Below Deck From No.1A to 1B", await _context.Gm35wainspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Front of Loco Above", await _context.Gm35flinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Short Nose", await _context.Gm35sninspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Cab of Loco Assistant Entrance", await _context.Gm35clinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Elect Cabinet Top Left", await _context.Gm35elinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Battery Knife Switch Compartment", await _context.Gm35bsinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Left Middle Door", await _context.Gm35lminspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Circuit Breaker Control Panel", await _context.Gm35cbinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Top Right Panel", await _context.Gm35trinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Middle Panel", await _context.Gm35mpinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Bottom Left Panel", await _context.Gm35blinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Central Air Compartment", await _context.Gm35cainspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Engine and Above Deck", await _context.Gm35edinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Compressor Fan Rad Compartment", await _context.Gm35cfinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "No.2 End Above Deck", await _context.Gm35deinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Roof Top Inspect", await _context.Gm35rfinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() }
    };
            }

            if (model.LocoModel == "GM36")
            {
                inspectionSources = new Dictionary<string, IEnumerable<dynamic>>
    {
        { "Below Deck From No.1A to 1B", await _context.Gm36wainspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Front of Loco Above", await _context.Gm36flinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Short Nose", await _context.Gm36sninspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Brake Valve Compartment", await _context.Gm36bvinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Cab of Loco Assistant Entrance", await _context.Gm36clinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Elect Cabinet Top Left", await _context.Gm36ecinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Circuit Breaker Control Panel", await _context.Gm36cbinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Battery Knife Switch Compartment", await _context.Gm36bsinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Left Middle Door", await _context.Gm36lminspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Left Control Panel", await _context.Gm36lcinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Top Right Panel", await _context.Gm36trinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Bottom Panel", await _context.Gm36bpinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Central Air Compartment", await _context.Gm36cainspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Engine and Above Deck", await _context.Gm36edinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Compressor Fan Rad Compartment", await _context.Gm36cfinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "No.2 End Above Deck", await _context.Gm36deinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Roof Top Inspect", await _context.Gm36rfinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() }
    };
            }

            if (inspectionSources.All(s => !s.Value.Any()))
                return NotFound("No parts found for this wagon number.");

            string folderPath = Path.Combine(_env.WebRootPath, "InspectionPdf", "Locos", "SowPdf");
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            // Create sanitized filename (timestamp + guid to avoid collisions)
            string safeGroup = SanitizeFileName(model?.LocoModel ?? "Group");
            string fileName = $"{locoNumber}_{safeGroup}_Sow_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N").Substring(0, 8)}.pdf";
            string filePath = Path.Combine(folderPath, fileName);

            try
            {
                // --- Generate PDF ---
                using (var writer = new PdfWriter(filePath))
                using (var pdf = new iText.Kernel.Pdf.PdfDocument(writer))
                using (var document = new Document(pdf))
                {
                    var bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                    var regular = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

                    // --- Header section ---
                    float[] columnWidths = { 150f, 350f };
                    var topTable = new Table(UnitValue.CreatePointArray(columnWidths)).SetWidth(UnitValue.CreatePercentValue(100));

                    var logoCell = new Cell().SetBorder(Border.NO_BORDER).SetVerticalAlignment(VerticalAlignment.TOP);
                    string logo1Path = Path.Combine(_env.WebRootPath, "Images", "Logo1.png");
                    string logo2Path = Path.Combine(_env.WebRootPath, "Images", "Logo2.png");

                    if (System.IO.File.Exists(logo1Path))
                    {
                        var logo1Img = new Image(ImageDataFactory.Create(logo1Path)).ScaleToFit(100, 50);
                        logoCell.Add(logo1Img);
                    }
                    if (System.IO.File.Exists(logo2Path))
                    {
                        logoCell.Add(new Paragraph("\n"));
                        var logo2Img = new Image(ImageDataFactory.Create(logo2Path)).ScaleToFit(100, 50);
                        logoCell.Add(logo2Img);
                    }
                    topTable.AddCell(logoCell);

                    var infoCell = new Cell().SetBorder(Border.NO_BORDER).SetVerticalAlignment(VerticalAlignment.TOP)
                        .SetHorizontalAlignment(HorizontalAlignment.RIGHT)
                        .SetTextAlignment(iText.Layout.Properties.TextAlignment.RIGHT);

                    infoCell.Add(new Paragraph()
                        .Add(new Text("Worldwide Rail and Mining Solutions SA (Pty) Ltd\n").SetFont(bold))
                        .Add(new Text("52 8th Avenue, Edenvale Gauteng 1610\n"))
                        .Add(new Text("Email: adminsa@wwms.co.za\n"))
                        .Add(new Text("T: +27 11 453 2170\n"))
                        .Add(new Text("Website: www.worldwideminingsolutions.co.za\n"))
                        .Add(new Text("Reg. No.: 2019/544337/07\n"))
                        .Add(new Text("Msomi Valuation Services (Pty) Ltd\n").SetFont(bold))
                        .Add(new Text("4 Sheffield Road, Ferryvale, Nigel, 1491\n"))
                        .Add(new Text("T: 011 814 2047\n"))
                        .Add(new Text("VAT: 4400277721\n")));

                    topTable.AddCell(infoCell);
                    document.Add(topTable);

                    document.Add(new LineSeparator(new SolidLine(1f)).SetMarginTop(10).SetMarginBottom(15));
                    document.Add(new Paragraph($"Quote - Asset Code: {locoNumber}").SetFont(bold).SetFontSize(14).SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER));
                    document.Add(new Paragraph($"Asset Model/Group: {model?.LocoModel ?? "N/A"}").SetFont(regular).SetFontSize(12).SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER).SetMarginBottom(10));
                    document.Add(new Paragraph($"Assessor: {assessor?.UserName ?? "N/A"}").SetFont(regular).SetFontSize(12).SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER).SetMarginBottom(10));

                  

                    // --- Inspection Tables ---
                    foreach (var group in inspectionSources)
                    {
                        var parts = group.Value;
                        if (!parts.Any()) continue;
                        //PLEASE ADD
                        string NormalizeCheck(object? obj)
                        {
                            if (obj == null) return "-";
                            string s = obj.ToString()?.Trim() ?? "";
                            if (string.IsNullOrWhiteSpace(s)) return "-";
                            if (s.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
                                s.Equals("y", StringComparison.OrdinalIgnoreCase) ||
                                s.Equals("true", StringComparison.OrdinalIgnoreCase))
                                return "Yes";
                            if (s.Equals("no", StringComparison.OrdinalIgnoreCase) ||
                                s.Equals("n", StringComparison.OrdinalIgnoreCase) ||
                                s.Equals("false", StringComparison.OrdinalIgnoreCase))
                                return "-";
                            // If DB contains something unexpected, show it trimmed (you can change to "-" if you prefer)
                            return s;
                        }

                        // map parts to trimmed + normalized fields
                        var partsWithNumbers = parts.Select(p => new
                        {
                            FormId = p.FormId,
                            PartDescr = p.PartDescr,
                            GoodCheck = NormalizeCheck(p.GoodCheck),
                            RefurbishCheck = NormalizeCheck(p.RefurbishCheck),
                            MissingCheck = NormalizeCheck(p.MissingCheck),
                            ReplaceCheck = NormalizeCheck(p.ReplaceCheck)
                        }).ToList();
                        //PLEASE ADJSUT
                       
                        document.Add(new Paragraph(group.Key)
                            .SetFont(bold)
                            .SetFontSize(13)
                            .SetMarginTop(15)
                            .SetBackgroundColor(ColorConstants.LIGHT_GRAY)
                            .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER));

                        var table = new Table(UnitValue.CreatePercentArray(7)).UseAllAvailableWidth();
                        string[] headers = { "No.", "Form ID", "Part Description", "Good", "Refurbish", "Missing", "Damage" }; //PLEASE ADJUST (NEW)
                        foreach (var header in headers)
                            table.AddHeaderCell(new Cell().Add(new Paragraph(header).SetFont(bold).SetBackgroundColor(ColorConstants.LIGHT_GRAY)));

                        int index = 1;
                        foreach (var p in partsWithNumbers)
                        {
                            table.AddCell(new Cell().Add(new Paragraph(index.ToString()).SetFont(regular)));
                            table.AddCell(new Cell().Add(new Paragraph(p.FormId?.ToString() ?? "").SetFont(regular)));
                            table.AddCell(new Cell().Add(new Paragraph(p.PartDescr ?? "").SetFont(regular)));
                            table.AddCell(new Cell().Add(new Paragraph(p.GoodCheck).SetFont(regular)));        // shows Yes or -
                            table.AddCell(new Cell().Add(new Paragraph(p.RefurbishCheck).SetFont(regular)));   // shows Yes or -
                            table.AddCell(new Cell().Add(new Paragraph(p.MissingCheck).SetFont(regular)));     // shows Yes or -
                            table.AddCell(new Cell().Add(new Paragraph(p.ReplaceCheck).SetFont(regular)));     // shows Yes or -
                            index++;
                        }

                        document.Add(table);
                    }

                    // --- Footer Image ---
                    string footerPath = Path.Combine(_env.WebRootPath, "Images", "Footer1.png");
                    if (System.IO.File.Exists(footerPath))
                    {
                        var pageSize = pdf.GetDefaultPageSize();
                        float pageWidth = pageSize.GetWidth();

                        var footerImg = new Image(ImageDataFactory.Create(footerPath));
                        float scaleX = pageWidth / footerImg.GetImageWidth();
                        footerImg.ScaleToFit(pageWidth, footerImg.GetImageHeight() * scaleX);
                        footerImg.SetFixedPosition(0, 0).SetMarginTop(15);
                        document.Add(footerImg);
                    }

                    document.Close();
                    pdf.Close();
                }

                //PLEASE ADJUST
                var dashboard = await _context.LocoDashboards.FirstOrDefaultAsync(d => d.LocoNumber == locoNumber);
                string relativePath = Path.Combine("InspectionPdf", "Locos", "SowPdf", fileName).Replace("\\", "/");

                if (dashboard != null)
                {
                   dashboard.AssessmentSow = relativePath;
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
        [HttpPost("GenerateAndSaveQuotePdfForAllWagons")]
        public async Task<IActionResult> GenerateAndSaveQuotePdfForAllWagons()
        {
            var notReadyWagonNumbers = await _context.WagonDashboards
    .AsNoTracking()
    .Where(d => d.AssessmentQuote == "Not Ready")
    .Select(d => d.WagonNumber)
    .Distinct()
    .ToListAsync();

            // 2. Generate PDF for each one
            foreach (var number in notReadyWagonNumbers)
            {
              //  await GenerateAndSaveQuotePdf(number);
            }




            return Ok(new { message = "PDF generated successfully" });
        }

        [HttpPost("GenerateAndSaveQuotePdfForLocos")]
        public async Task<IActionResult> GenerateAndSaveQuotePdfForLocos([FromBody] LocoQuotePdfRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.LocoNumber))
                return BadRequest("Invalid request: WagonNumber is required.");

            _context.Database.SetCommandTimeout(180);

            if (!int.TryParse(request.LocoNumber, out int locoNumber))
                return BadRequest("Invalid WagonNumber format.");

            string userId = request.UserId ?? "";

            // Fetch related records (non-tracking reads where appropriate)
            var assessor = string.IsNullOrWhiteSpace(userId) ? null :
                await _context.LeaseCoUsers.AsNoTracking().FirstOrDefaultAsync(a => a.UserId == userId);

            var master = await _context.MasterLocos.AsNoTracking().FirstOrDefaultAsync(m => m.LocoNumber == locoNumber);
            var model = await _context.LocoInfoCaptures.AsNoTracking().FirstOrDefaultAsync(p => p.LocoNumber == locoNumber);
            var dash = await _context.LocoDashboards.AsNoTracking().FirstOrDefaultAsync(p => locoNumber == locoNumber);

            // Validate basic presence
            if (model == null)
                return BadRequest($"No LocoInfoCaptures record found for loco {locoNumber}.");
            var LocoNumber = locoNumber;
            var inspectionSources = new Dictionary<string, IEnumerable<dynamic>>();
            if (model == null)
            {
                return NotFound("Loco not found.");

            }
            if (model.LocoModel == "E18")
            {

                // Define each inspection table source and label
                inspectionSources = new Dictionary<string, IEnumerable<dynamic>>
            {
                { "BELOW DECK Walk Around Inspection", await _context.E18bdinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
                { "Front of Loco Inspection", await _context.E18flinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
                { "Back of Loco Inspection", await _context.E18beinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
                { "18E Cab Inspection", await _context.E18eeinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
                { "Low Voltage Compartment Inspection", await _context.E18lvinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
                { "Corridor Inspection", await _context.E18crinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
                { "HT High Voltage Compartment Inspection", await _context.E18hvinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
                { "Motor Alternator Set Inspection", await _context.E18mainspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
                { "Exhauster Inspection", await _context.E18ehinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
          { "Machine Brake Compartment Inspection", await _context.E18mbinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
                { "High Speed Circuit Breaker Compartment Inspection", await _context.E18hsinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
                { "Exciter Set 2 Inspection", await _context.E18esinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
                { "High Voltage Compartment No 1 Inspection", await _context.E18hcinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
                { "Compressor Compartment Inspection", await _context.E18ccinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
                { "Cab and Toilet No 1 End Inspection", await _context.E18ctinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
                { "Roof Top Inspection", await _context.E18rfinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() }

            };
            }

            if (model.LocoModel == "GE34")
            {

                inspectionSources = new Dictionary<string, IEnumerable<dynamic>>
{
    { "Walk Around / Below Deck Inspection", await _context.Ge34bdinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
    { "Front of Loco Inspection", await _context.Ge34flinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
    { "Short Nose Inspection", await _context.Ge34sninspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
    { "Cab Loco Inspection", await _context.Ge34clinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
    { "Electrical Cab Inspection", await _context.Ge34ecinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
    { "Battery Switch Inspection", await _context.Ge34bsinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
    { "Outside Driver’s Door Inspection", await _context.Ge34odinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
    { "Blower Compartment Inspection", await _context.Ge34bcinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
    { "Alternator Compartment Inspection", await _context.Ge34acinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
    { "Engine Deck Inspection", await _context.Ge34edinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
    { "Compressor Fan Inspection", await _context.Ge34cfinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
    { "End Deck Inspection", await _context.Ge34deinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
    { "Roof Top Inspection", await _context.Ge34rfinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() }
};

            }

            if (model.LocoModel == "GE35")
            {

                inspectionSources = new Dictionary<string, IEnumerable<dynamic>>
{
    { "Walk Around / Below Deck Inspection", await _context.Ge35bdinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
    { "Front of Loco Inspection", await _context.Ge35flinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
    { "Short Nose Inspection", await _context.Ge35sninspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
    { "Cab Loco Inspection", await _context.Ge35clinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
    { "Electrical Cab Inspection", await _context.Ge35ecinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
    { "Battery Switch Inspection", await _context.Ge35bsinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
    { "Outside Driver’s Door Inspection", await _context.Ge35odinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
    { "Blower Compartment Inspection", await _context.Ge35bcinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
    { "Main Gen Compartment Inspection", await _context.Ge35mginspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
    { "Engine Deck Inspection", await _context.Ge35edinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
    { "Compressor Fan Inspection", await _context.Ge35cfinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
    { "End Deck Inspection", await _context.Ge35deinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
    { "Roof Top Inspection", await _context.Ge35rfinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() }
};

            }
            if (model.LocoModel == "GE36")
            {
                inspectionSources = new Dictionary<string, IEnumerable<dynamic>>
    {
        { "Walk Around / Below Deck Inspect", await _context.Ge36bdinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Front Loco Inspect", await _context.Ge36flinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Short Nose Inspect", await _context.Ge36sninspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Cab Loco Inspect", await _context.Ge36clinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Elect Cab Inspect", await _context.Ge36ecinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Central Air Inspect", await _context.Ge36cainspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Main Gen Compartment Inspect", await _context.Ge36mginspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Engine Deck Inspect", await _context.Ge36edinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Compressor Fan Inspect", await _context.Ge36cfinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "End Deck Inspect", await _context.Ge36deinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Roof Top Inspect", await _context.Ge36rfinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() }
    };
            }
            if (model.LocoModel == "GM34")
            {
                inspectionSources = new Dictionary<string, IEnumerable<dynamic>>
    {
        { "Below Deck From No.1A to 1B", await _context.Gm34bdinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Front of Loco Above", await _context.Gm34flinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Short Nose", await _context.Gm34sninspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Cab of Loco Assistant Entrance", await _context.Gm34clinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Elect Cabinet Top Left", await _context.Gm34elinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Battery Knife Switch Compartment", await _context.Gm34bsinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Left Middle Door", await _context.Gm34lminspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Circuit Breaker Control Panel", await _context.Gm34cbinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Top Right Panel", await _context.Gm34trinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Middle Panel", await _context.Gm34mpinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Bottom Left Panel", await _context.Gm34blinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Central Air Compartment", await _context.Gm34cainspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Engine and Above Deck", await _context.Gm34edinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Compressor Fan Rad Compartment", await _context.Gm34cfinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "No.2 End above deck", await _context.Gm34deinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Roof Top Inspect", await _context.Gm34rfinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() }
    };
            }
            if (model.LocoModel == "GM35")
            {
                inspectionSources = new Dictionary<string, IEnumerable<dynamic>>
    {
        { "Below Deck From No.1A to 1B", await _context.Gm35wainspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Front of Loco Above", await _context.Gm35flinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Short Nose", await _context.Gm35sninspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Cab of Loco Assistant Entrance", await _context.Gm35clinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Elect Cabinet Top Left", await _context.Gm35elinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Battery Knife Switch Compartment", await _context.Gm35bsinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Left Middle Door", await _context.Gm35lminspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Circuit Breaker Control Panel", await _context.Gm35cbinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Top Right Panel", await _context.Gm35trinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Middle Panel", await _context.Gm35mpinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Bottom Left Panel", await _context.Gm35blinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Central Air Compartment", await _context.Gm35cainspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Engine and Above Deck", await _context.Gm35edinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Compressor Fan Rad Compartment", await _context.Gm35cfinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "No.2 End Above Deck", await _context.Gm35deinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Roof Top Inspect", await _context.Gm35rfinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() }
    };
            }

            if (model.LocoModel == "GM36")
            {
                inspectionSources = new Dictionary<string, IEnumerable<dynamic>>
    {
        { "Below Deck From No.1A to 1B", await _context.Gm36wainspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Front of Loco Above", await _context.Gm36flinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Short Nose", await _context.Gm36sninspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Brake Valve Compartment", await _context.Gm36bvinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Cab of Loco Assistant Entrance", await _context.Gm36clinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Elect Cabinet Top Left", await _context.Gm36ecinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Circuit Breaker Control Panel", await _context.Gm36cbinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Battery Knife Switch Compartment", await _context.Gm36bsinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Left Middle Door", await _context.Gm36lminspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Left Control Panel", await _context.Gm36lcinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Top Right Panel", await _context.Gm36trinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Bottom Panel", await _context.Gm36bpinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Central Air Compartment", await _context.Gm36cainspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Engine and Above Deck", await _context.Gm36edinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Compressor Fan Rad Compartment", await _context.Gm36cfinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "No.2 End Above Deck", await _context.Gm36deinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() },
        { "Roof Top Inspect", await _context.Gm36rfinspects.Where(p => p.LocoNumber == LocoNumber).ToListAsync() }
    };
            }

            if (inspectionSources.All(s => !s.Value.Any()))
                return NotFound("No parts found for this wagon number.");

            // --- Ensure folder exists ---
            string folderPath = Path.Combine(_env.WebRootPath, "InspectionPdf", "Locos", "QuotePdf");
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            string fileName = $"{locoNumber}_{model?.LocoModel}_Quote_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            string filePath = Path.Combine(folderPath, fileName);

            // --- Generate PDF ---
            using (var writer = new PdfWriter(filePath))
            using (var pdf = new iText.Kernel.Pdf.PdfDocument(writer))
            using (var document = new Document(pdf))
            {
                var bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                var regular = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

                // --- Header section ---
                float[] columnWidths = { 150f, 350f };
                var topTable = new Table(UnitValue.CreatePointArray(columnWidths)).SetWidth(UnitValue.CreatePercentValue(100));

                var logoCell = new Cell().SetBorder(Border.NO_BORDER).SetVerticalAlignment(VerticalAlignment.TOP);
                string logo1Path = Path.Combine(_env.WebRootPath, "Images", "Logo1.png");
                string logo2Path = Path.Combine(_env.WebRootPath, "Images", "Logo2.png");

                if (System.IO.File.Exists(logo1Path))
                {
                    var logo1Img = new Image(ImageDataFactory.Create(logo1Path)).ScaleToFit(100, 50);
                    logoCell.Add(logo1Img);
                }
                if (System.IO.File.Exists(logo2Path))
                {
                    logoCell.Add(new Paragraph("\n"));
                    var logo2Img = new Image(ImageDataFactory.Create(logo2Path)).ScaleToFit(100, 50);
                    logoCell.Add(logo2Img);
                }
                topTable.AddCell(logoCell);

                var infoCell = new Cell().SetBorder(Border.NO_BORDER).SetVerticalAlignment(VerticalAlignment.TOP)
                    .SetHorizontalAlignment(HorizontalAlignment.RIGHT)
                    .SetTextAlignment(iText.Layout.Properties.TextAlignment.RIGHT);

                infoCell.Add(new Paragraph()
                    .Add(new Text("Worldwide Rail and Mining Solutions SA (Pty) Ltd\n").SetFont(bold))
                    .Add(new Text("52 8th Avenue, Edenvale Gauteng 1610\n"))
                    .Add(new Text("Email: adminsa@wwms.co.za\n"))
                    .Add(new Text("T: +27 11 453 2170\n"))
                    .Add(new Text("Website: www.worldwideminingsolutions.co.za\n"))
                    .Add(new Text("Reg. No.: 2019/544337/07\n"))
                    .Add(new Text("Msomi Valuation Services (Pty) Ltd\n").SetFont(bold))
                    .Add(new Text("4 Sheffield Road, Ferryvale, Nigel, 1491\n"))
                    .Add(new Text("T: 011 814 2047\n"))
                    .Add(new Text("VAT: 4400277721\n")));

                topTable.AddCell(infoCell);
                document.Add(topTable);

                document.Add(new LineSeparator(new SolidLine(1f)).SetMarginTop(10).SetMarginBottom(15));
                document.Add(new Paragraph($"Quote - Asset Code: {locoNumber}").SetFont(bold).SetFontSize(14).SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER));
                document.Add(new Paragraph($"Asset Model/Group: {model?.LocoModel ?? "N/A"}").SetFont(regular).SetFontSize(12).SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER).SetMarginBottom(10));
                document.Add(new Paragraph($"Assessor: {assessor?.UserName ?? "N/A"}").SetFont(regular).SetFontSize(12).SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER).SetMarginBottom(10));

                decimal grandTotalRefurbish = 0, grandTotalMissing = 0, grandTotalReplace = 0, grandTotalLabor=0;

                //PLEASE ADD
                // --- Lift & Barrel Costs Section ---


                // --- Inspection Tables ---
                foreach (var group in inspectionSources)
                {
                    var parts = group.Value;
                    if (!parts.Any()) continue;

                    var partsWithNumbers = parts.Select(p =>
                    {
                        decimal refVal = 0, missVal = 0, replVal = 0, labVal = 0;

                        if (p.RefurbishValue != null && !string.IsNullOrWhiteSpace(p.RefurbishValue.ToString()))
                            decimal.TryParse(p.RefurbishValue.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out refVal);

                        if (p.MissingValue != null && !string.IsNullOrWhiteSpace(p.MissingValue.ToString()))
                            decimal.TryParse(p.MissingValue.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out missVal);

                        if (p.ReplaceValue != null && !string.IsNullOrWhiteSpace(p.ReplaceValue.ToString()))
                            decimal.TryParse(p.ReplaceValue.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out replVal);

                        if (p.LaborValue != null && !string.IsNullOrWhiteSpace(p.LaborValue.ToString()))
                            decimal.TryParse(p.LaborValue.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out labVal);

                        return new
                        {
                            p.FormId,
                            p.PartDescr,
                            RefurbishValue = refVal,
                            MissingValue = missVal,
                            ReplaceValue = replVal,
                            LaborValue = labVal
                        };
                    }).ToList();

                    decimal totalRefurbish = partsWithNumbers.Sum(p => p.RefurbishValue);
                    decimal totalMissing = partsWithNumbers.Sum(p => p.MissingValue);
                    decimal totalReplace = partsWithNumbers.Sum(p => p.ReplaceValue);
                    decimal totalLabor = partsWithNumbers.Sum(p => p.LaborValue);

                    grandTotalRefurbish += totalRefurbish;
                    grandTotalMissing += totalMissing;
                    grandTotalReplace += totalReplace;
                    grandTotalLabor += totalLabor;

                    document.Add(new Paragraph(group.Key)
                        .SetFont(bold)
                        .SetFontSize(13)
                        .SetMarginTop(15)
                        .SetBackgroundColor(ColorConstants.LIGHT_GRAY)
                        .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER));

                    var table = new Table(UnitValue.CreatePercentArray(7)).UseAllAvailableWidth();
                    string[] headers = { "No.", "Form ID", "Part Description", "Refurbish Value", "Missing Value", "Replace Value", "Labor Value" };
                    foreach (var header in headers)
                        table.AddHeaderCell(new Cell().Add(new Paragraph(header).SetFont(bold).SetBackgroundColor(ColorConstants.LIGHT_GRAY)));

                    int index = 1;
                    foreach (var p in partsWithNumbers)
                    {
                        table.AddCell(new Cell().Add(new Paragraph(index.ToString()).SetFont(regular)));
                        table.AddCell(new Cell().Add(new Paragraph(p.FormId.ToString()).SetFont(regular)));
                        table.AddCell(new Cell().Add(new Paragraph(p.PartDescr).SetFont(regular)));
                        table.AddCell(new Cell().Add(new Paragraph(p.RefurbishValue != 0 ? $"R{p.RefurbishValue:F2}" : "-").SetFont(regular)));
                        table.AddCell(new Cell().Add(new Paragraph(p.MissingValue != 0 ? $"R{p.MissingValue:F2}" : "-").SetFont(regular)));
                        table.AddCell(new Cell().Add(new Paragraph(p.ReplaceValue != 0 ? $"R{p.ReplaceValue:F2}" : "-").SetFont(regular)));
                        table.AddCell(new Cell().Add(new Paragraph(p.LaborValue != 0 ? $"R{p.LaborValue:F2}" : "-").SetFont(regular)));
                        index++;
                    }

                    table.AddCell(new Cell(1, 3)
                        .Add(new Paragraph("Subtotal").SetFont(bold))
                        .SetTextAlignment(iText.Layout.Properties.TextAlignment.RIGHT)
                        .SetBackgroundColor(ColorConstants.LIGHT_GRAY));

                    table.AddCell(new Cell().Add(new Paragraph($"R{totalRefurbish:F2}").SetFont(bold)));
                    table.AddCell(new Cell().Add(new Paragraph($"R{totalMissing:F2}").SetFont(bold)));
                    table.AddCell(new Cell().Add(new Paragraph($"R{totalReplace:F2}").SetFont(bold)));
                    table.AddCell(new Cell().Add(new Paragraph($"R{totalLabor:F2}").SetFont(bold)));

                    document.Add(table);
                }
                decimal marketValue = ParseDecimalSafe(master?.MarketValue);
                decimal rts = grandTotalRefurbish + grandTotalMissing + grandTotalReplace + grandTotalLabor ;
                decimal assetValue = marketValue - rts;
                // --- Final Grand Totals ---
                decimal grandTotal = grandTotalRefurbish + grandTotalMissing + grandTotalReplace + assetValue + +grandTotalLabor;

                document.Add(new Paragraph("\nGrand Totals").SetFont(bold).SetFontSize(13).SetTextAlignment(iText.Layout.Properties.TextAlignment.RIGHT));

                var totalsTable = new Table(UnitValue.CreatePercentArray(6)).UseAllAvailableWidth();
                totalsTable.AddHeaderCell(new Cell().Add(new Paragraph("Refurbish Total").SetFont(bold)));
                totalsTable.AddHeaderCell(new Cell().Add(new Paragraph("Missing Total").SetFont(bold)));
                totalsTable.AddHeaderCell(new Cell().Add(new Paragraph("Replace Total").SetFont(bold)));
                totalsTable.AddHeaderCell(new Cell().Add(new Paragraph("Labor Total").SetFont(bold)));
                totalsTable.AddHeaderCell(new Cell().Add(new Paragraph("Market Value").SetFont(bold)));
                totalsTable.AddHeaderCell(new Cell().Add(new Paragraph("Return to Service Cost").SetFont(bold)));

                totalsTable.AddCell(new Cell().Add(new Paragraph($"R{grandTotalRefurbish:F2}").SetFont(regular)));
                totalsTable.AddCell(new Cell().Add(new Paragraph($"R{grandTotalMissing:F2}").SetFont(regular)));
                totalsTable.AddCell(new Cell().Add(new Paragraph($"R{grandTotalReplace:F2}").SetFont(regular)));
                totalsTable.AddCell(new Cell().Add(new Paragraph($"R{grandTotalLabor:F2}").SetFont(regular)));
                totalsTable.AddCell(new Cell().Add(new Paragraph($"R{assetValue:F2}").SetFont(regular)));
                totalsTable.AddCell(new Cell().Add(new Paragraph($"R{grandTotal:F2}").SetFont(bold)));

                document.Add(totalsTable);

                // --- Footer Image ---
                string footerPath = Path.Combine(_env.WebRootPath, "Images", "Footer1.png");
                if (System.IO.File.Exists(footerPath))
                {
                    var pageSize = pdf.GetDefaultPageSize();
                    float pageWidth = pageSize.GetWidth();

                    var footerImg = new Image(ImageDataFactory.Create(footerPath));
                    float scaleX = pageWidth / footerImg.GetImageWidth();
                    footerImg.ScaleToFit(pageWidth, footerImg.GetImageHeight() * scaleX);
                    footerImg.SetFixedPosition(0, 0);
                    document.Add(footerImg);
                }
            }

            var dashboard = await _context.LocoDashboards
            .FirstOrDefaultAsync(d => d.LocoNumber == locoNumber);

            if (dashboard != null)
            {
                string relativePath = Path.Combine("InspectionPdf", "Locos", "QuotePdf", fileName);
                dashboard.AssessmentQuote = relativePath;
                _context.LocoDashboards.Update(dashboard);
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "PDF generated successfully", path = filePath });
        }
        [HttpPost("GenerateAndSaveQuotePdfForAllLocos")]
        public async Task<IActionResult> GenerateAndSaveQuotePdfForAllLocos()
        {
            var vagonNumbers = await _context.LocoDashboards.Where(predicate => predicate.AssessmentQuote == "Not Ready")
                .AsNoTracking()
                .Select(w => w.LocoNumber)
                .ToListAsync();
            foreach (var number in vagonNumbers)
            {
                //await GenerateAndSaveQuotePdfForLocos((int)number);
            }




            return Ok(new { message = "PDF generated successfully" });
        }
        //PLEASE ADD
       
        // --- Optional Upload Endpoint (if you want manual upload via FormData) ---
        [HttpPost("upload")]
        public async Task<IActionResult> UploadPdf([FromForm] IFormFile file, [FromForm] string wagonNumber, [FromForm] string wagonGroup)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            string dateTimeStr = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string fileName = $"{wagonNumber}_{wagonGroup}_Quote_{dateTimeStr}.pdf";
            string folderPath = Path.Combine(_env.WebRootPath, "InspectionPdf", "Wagons", "QuotePdf");
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            string filePath = Path.Combine(folderPath, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return Ok(new { message = "PDF saved successfully", path = filePath });
        }
    }
    public class QuotePdfRequest
    {
        public string WagonNumber { get; set; } = string.Empty;
        public string UserId { get; set; } = "";
    }
    public class QuotePdfRequestUpload
    {
        public string WagonNumber { get; set; } = string.Empty;
        public string UserId { get; set; } = "";
    }
    public class LocoQuotePdfRequest
    {
        public string LocoNumber { get; set; } = string.Empty;
        public string UserId { get; set; } = "";
    }
}
