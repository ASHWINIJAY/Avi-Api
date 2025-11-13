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

        //PLEASE ADD AND ADJUST WHATEVER IS DIFFERENT FROM THE PREIVIOUS VERSION
        [HttpPost("GenerateAndSaveQuotePdf")]
        public async Task<IActionResult> GenerateAndSaveQuotePdf([FromBody] int wagonNumber)
        {
            _context.Database.SetCommandTimeout(180);

            var model = await _context.WagonInfoCaptures
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.WagonNumber == wagonNumber);

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

            // --- Ensure folder exists ---
            string folderPath = Path.Combine(_env.WebRootPath, "InspectionPdf", "Wagons", "QuotePdf");
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            string fileName = $"{wagonNumber}_{model?.WagonGroup}_Quote_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
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
                document.Add(new Paragraph($"Quote - Asset Code: {wagonNumber}").SetFont(bold).SetFontSize(14).SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER));
                document.Add(new Paragraph($"Asset Model/Group: {model?.WagonGroup ?? "N/A"}").SetFont(regular).SetFontSize(12).SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER).SetMarginBottom(10));

                decimal grandTotalRefurbish = 0, grandTotalMissing = 0, grandTotalReplace = 0;

                //PLEASE ADD
                // --- Lift & Barrel Costs Section ---
                decimal liftCost = 0;
                decimal barrelCost = 0;

                if (model?.LiftLapsed == "Yes")
                    liftCost = 420982;
                else if (model?.LiftLapsed == "No")
                    liftCost = 0;

                if (model?.BarrelLapsed == "Yes")
                    barrelCost = 351893;
                else if (model?.BarrelLapsed == "No" || model?.BarrelLapsed == "N/A")
                    barrelCost = 0;

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
                liftBarrelTable.AddCell(new Cell().Add(new Paragraph(liftCost != 0 ? $"R{liftCost:F2}" : "-").SetFont(regular)));

                liftBarrelTable.AddCell(new Cell().Add(new Paragraph("Barrel Inspection").SetFont(regular)));
                liftBarrelTable.AddCell(new Cell().Add(new Paragraph(model?.BarrelLapsed ?? "N/A").SetFont(regular)));
                liftBarrelTable.AddCell(new Cell().Add(new Paragraph(barrelCost != 0 ? $"R{barrelCost:F2}" : "-").SetFont(regular)));

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
                        decimal refVal = 0, missVal = 0, replVal = 0;

                        if (p.RefurbishValue != null && !string.IsNullOrWhiteSpace(p.RefurbishValue.ToString()))
                            decimal.TryParse(p.RefurbishValue.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out refVal);

                        if (p.MissingValue != null && !string.IsNullOrWhiteSpace(p.MissingValue.ToString()))
                            decimal.TryParse(p.MissingValue.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out missVal);

                        if (p.ReplaceValue != null && !string.IsNullOrWhiteSpace(p.ReplaceValue.ToString()))
                            decimal.TryParse(p.ReplaceValue.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out replVal);

                        return new
                        {
                            p.FormId,
                            p.PartDescr,
                            RefurbishValue = refVal,
                            MissingValue = missVal,
                            ReplaceValue = replVal
                        };
                    }).ToList();

                    decimal totalRefurbish = partsWithNumbers.Sum(p => p.RefurbishValue);
                    decimal totalMissing = partsWithNumbers.Sum(p => p.MissingValue);
                    decimal totalReplace = partsWithNumbers.Sum(p => p.ReplaceValue);

                    grandTotalRefurbish += totalRefurbish;
                    grandTotalMissing += totalMissing;
                    grandTotalReplace += totalReplace;

                    document.Add(new Paragraph(group.Key)
                        .SetFont(bold)
                        .SetFontSize(13)
                        .SetMarginTop(15)
                        .SetBackgroundColor(ColorConstants.LIGHT_GRAY)
                        .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER));

                    var table = new Table(UnitValue.CreatePercentArray(6)).UseAllAvailableWidth();
                    string[] headers = { "No.", "Form ID", "Part Description", "Refurbish Value", "Missing Value", "Replace Value" };
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
                        index++;
                    }

                    table.AddCell(new Cell(1, 3)
                        .Add(new Paragraph("Subtotal").SetFont(bold))
                        .SetTextAlignment(iText.Layout.Properties.TextAlignment.RIGHT)
                        .SetBackgroundColor(ColorConstants.LIGHT_GRAY));

                    table.AddCell(new Cell().Add(new Paragraph($"R{totalRefurbish:F2}").SetFont(bold)));
                    table.AddCell(new Cell().Add(new Paragraph($"R{totalMissing:F2}").SetFont(bold)));
                    table.AddCell(new Cell().Add(new Paragraph($"R{totalReplace:F2}").SetFont(bold)));

                    document.Add(table);
                }

                // --- Final Grand Totals ---
                decimal grandTotal = grandTotalRefurbish + grandTotalMissing + grandTotalReplace + liftBarrelTotal;

                document.Add(new Paragraph("\nGrand Totals").SetFont(bold).SetFontSize(13).SetTextAlignment(iText.Layout.Properties.TextAlignment.RIGHT));

                var totalsTable = new Table(UnitValue.CreatePercentArray(5)).UseAllAvailableWidth();
                totalsTable.AddHeaderCell(new Cell().Add(new Paragraph("Refurbish Total").SetFont(bold)));
                totalsTable.AddHeaderCell(new Cell().Add(new Paragraph("Missing Total").SetFont(bold)));
                totalsTable.AddHeaderCell(new Cell().Add(new Paragraph("Replace Total").SetFont(bold)));
                totalsTable.AddHeaderCell(new Cell().Add(new Paragraph("Lift & Barrel Total").SetFont(bold)));
                totalsTable.AddHeaderCell(new Cell().Add(new Paragraph("Overall Total").SetFont(bold)));

                totalsTable.AddCell(new Cell().Add(new Paragraph($"R{grandTotalRefurbish:F2}").SetFont(regular)));
                totalsTable.AddCell(new Cell().Add(new Paragraph($"R{grandTotalMissing:F2}").SetFont(regular)));
                totalsTable.AddCell(new Cell().Add(new Paragraph($"R{grandTotalReplace:F2}").SetFont(regular)));
                totalsTable.AddCell(new Cell().Add(new Paragraph($"R{liftBarrelTotal:F2}").SetFont(regular)));
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

            var dashboard = await _context.WagonDashboards
                .FirstOrDefaultAsync(d => d.WagonNumber == wagonNumber);

            if (dashboard != null)
            {
                string relativePath = Path.Combine("InspectionPdf", "Wagons", "QuotePdf", fileName);
                dashboard.AssessmentQuote = relativePath;
                _context.WagonDashboards.Update(dashboard);
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "PDF generated successfully", path = filePath });
        }
        [HttpPost("GenerateAndSaveQuotePdfForAllWagons")]
        public async Task<IActionResult> GenerateAndSaveQuotePdfForAllWagons()
        {
            var vagonNumbers = await _context.WagonInfoCaptures
                .AsNoTracking()
                .Select(w => w.WagonNumber)
                .ToListAsync();
            foreach (var number in vagonNumbers)
            {
                await GenerateAndSaveQuotePdf(number);
            }
                

            

            return Ok(new { message = "PDF generated successfully"});
        }

        [HttpPost("GenerateAndSaveQuotePdfForLocos")]
        public async Task<IActionResult> GenerateAndSaveQuotePdfForLocos([FromBody] int wagonNumber)
        {
            _context.Database.SetCommandTimeout(180);

            var model = await _context.LocoInfoCaptures 
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.LocoNumber == wagonNumber);
            var LocoNumber= wagonNumber;
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

            if (inspectionSources.All(s => !s.Value.Any()))
                return NotFound("No parts found for this wagon number.");

            // --- Ensure folder exists ---
            string folderPath = Path.Combine(_env.WebRootPath, "InspectionPdf", "Locos", "QuotePdf");
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            string fileName = $"{wagonNumber}_{model?.LocoModel}_Quote_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
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
                document.Add(new Paragraph($"Quote - Asset Code: {wagonNumber}").SetFont(bold).SetFontSize(14).SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER));
                document.Add(new Paragraph($"Asset Model/Group: {model?.LocoModel ?? "N/A"}").SetFont(regular).SetFontSize(12).SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER).SetMarginBottom(10));

                decimal grandTotalRefurbish = 0, grandTotalMissing = 0, grandTotalReplace = 0;

                //PLEASE ADD
                // --- Lift & Barrel Costs Section ---
                

                // --- Inspection Tables ---
                foreach (var group in inspectionSources)
                {
                    var parts = group.Value;
                    if (!parts.Any()) continue;

                    var partsWithNumbers = parts.Select(p =>
                    {
                        decimal refVal = 0, missVal = 0, replVal = 0;

                        if (p.RefurbishValue != null && !string.IsNullOrWhiteSpace(p.RefurbishValue.ToString()))
                            decimal.TryParse(p.RefurbishValue.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out refVal);

                        if (p.MissingValue != null && !string.IsNullOrWhiteSpace(p.MissingValue.ToString()))
                            decimal.TryParse(p.MissingValue.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out missVal);

                        if (p.ReplaceValue != null && !string.IsNullOrWhiteSpace(p.ReplaceValue.ToString()))
                            decimal.TryParse(p.ReplaceValue.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out replVal);

                        return new
                        {
                            p.FormId,
                            p.PartDescr,
                            RefurbishValue = refVal,
                            MissingValue = missVal,
                            ReplaceValue = replVal
                        };
                    }).ToList();

                    decimal totalRefurbish = partsWithNumbers.Sum(p => p.RefurbishValue);
                    decimal totalMissing = partsWithNumbers.Sum(p => p.MissingValue);
                    decimal totalReplace = partsWithNumbers.Sum(p => p.ReplaceValue);

                    grandTotalRefurbish += totalRefurbish;
                    grandTotalMissing += totalMissing;
                    grandTotalReplace += totalReplace;

                    document.Add(new Paragraph(group.Key)
                        .SetFont(bold)
                        .SetFontSize(13)
                        .SetMarginTop(15)
                        .SetBackgroundColor(ColorConstants.LIGHT_GRAY)
                        .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER));

                    var table = new Table(UnitValue.CreatePercentArray(6)).UseAllAvailableWidth();
                    string[] headers = { "No.", "Form ID", "Part Description", "Refurbish Value", "Missing Value", "Replace Value" };
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
                        index++;
                    }

                    table.AddCell(new Cell(1, 3)
                        .Add(new Paragraph("Subtotal").SetFont(bold))
                        .SetTextAlignment(iText.Layout.Properties.TextAlignment.RIGHT)
                        .SetBackgroundColor(ColorConstants.LIGHT_GRAY));

                    table.AddCell(new Cell().Add(new Paragraph($"R{totalRefurbish:F2}").SetFont(bold)));
                    table.AddCell(new Cell().Add(new Paragraph($"R{totalMissing:F2}").SetFont(bold)));
                    table.AddCell(new Cell().Add(new Paragraph($"R{totalReplace:F2}").SetFont(bold)));

                    document.Add(table);
                }

                // --- Final Grand Totals ---
                decimal grandTotal = grandTotalRefurbish + grandTotalMissing + grandTotalReplace;

                document.Add(new Paragraph("\nGrand Totals").SetFont(bold).SetFontSize(13).SetTextAlignment(iText.Layout.Properties.TextAlignment.RIGHT));

                var totalsTable = new Table(UnitValue.CreatePercentArray(4)).UseAllAvailableWidth();
                totalsTable.AddHeaderCell(new Cell().Add(new Paragraph("Refurbish Total").SetFont(bold)));
                totalsTable.AddHeaderCell(new Cell().Add(new Paragraph("Missing Total").SetFont(bold)));
                totalsTable.AddHeaderCell(new Cell().Add(new Paragraph("Replace Total").SetFont(bold)));
                totalsTable.AddHeaderCell(new Cell().Add(new Paragraph("Overall Total").SetFont(bold)));

                totalsTable.AddCell(new Cell().Add(new Paragraph($"R{grandTotalRefurbish:F2}").SetFont(regular)));
                totalsTable.AddCell(new Cell().Add(new Paragraph($"R{grandTotalMissing:F2}").SetFont(regular)));
                totalsTable.AddCell(new Cell().Add(new Paragraph($"R{grandTotalReplace:F2}").SetFont(regular)));
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
            .FirstOrDefaultAsync(d => d.LocoNumber == wagonNumber);

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
            var vagonNumbers = await _context.LocoInfoCaptures.Where(predicate=> predicate.LocoModel == "E18" || predicate.LocoModel == "GE34")  
                .AsNoTracking()
                .Select(w => w.LocoNumber)
                .ToListAsync();
            foreach (var number in vagonNumbers)
            {
                await GenerateAndSaveQuotePdfForLocos(number);
            }




            return Ok(new { message = "PDF generated successfully" });
        }
        //PLEASE ADD
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
}
