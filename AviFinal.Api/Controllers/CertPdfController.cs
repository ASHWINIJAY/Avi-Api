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
using iText.StyledXmlParser.Jsoup.Helper;
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
    public class CertPdfController : ControllerBase
    {
        private readonly AviDbContext _context;
        private readonly IWebHostEnvironment _env;

        public CertPdfController(AviDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpPost("GenerateAndSaveCertPdf")]
        public async Task<IActionResult> GenerateAndSaveCertPdf([FromBody] int wagonNumber)
        {
            _context.Database.SetCommandTimeout(180);

            var model = await _context.WagonInfoCaptures
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.WagonNumber == wagonNumber);

            // --- Ensure folder exists ---
            string folderPath = Path.Combine(_env.WebRootPath, "InspectionPdf", "Wagons", "CertPdf");
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            string fileName = $"{wagonNumber}_{model?.WagonGroup}_Cert_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            string filePath = Path.Combine(folderPath, fileName);

            using (var writer = new PdfWriter(filePath))
            using (var pdf = new iText.Kernel.Pdf.PdfDocument(writer))
            using (var document = new Document(pdf))
            {
                var bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                var regular = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

                string headerPath = Path.Combine(_env.WebRootPath, "Images", "CertLogo1.png");
                if (System.IO.File.Exists(headerPath))
                {
                    var pageSize = pdf.GetDefaultPageSize();
                    float pageWidth = pageSize.GetWidth();
                    float pageHeight = pageSize.GetHeight();

                    var headerImg = new Image(ImageDataFactory.Create(headerPath));

                    // Scale image to page width while maintaining aspect ratio
                    headerImg.ScaleToFit(pageWidth, pageHeight / 6f); // adjust height as needed

                    // Set absolute position: x = 0, y = pageHeight - imageHeight (top of page)
                    headerImg.SetFixedPosition(0, pageHeight - headerImg.GetImageScaledHeight());

                    document.Add(headerImg);
                }

                if (!string.IsNullOrWhiteSpace(model?.WagonPhoto))
                {
                    string fullImagePath = Path.Combine(_env.WebRootPath, model.WagonPhoto.TrimStart('/').TrimStart('\\'));

                    if (System.IO.File.Exists(fullImagePath))
                    {
                        var wagonImg = new Image(ImageDataFactory.Create(fullImagePath));

                        wagonImg
                            .SetHorizontalAlignment(iText.Layout.Properties.HorizontalAlignment.CENTER)
                            .SetMarginBottom(15)
                            .SetAutoScale(true)
                            .SetWidth(350f)
                            .SetHeight(300f);  

                        document.Add(wagonImg);
                    }
                }

                document.Add(new Paragraph("DESCRIPTION OF ASSETS").SetFont(bold).SetFontSize(25).SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER)).SetBottomMargin(10);

                var assetTable = new Table(UnitValue.CreatePercentArray(2)).UseAllAvailableWidth().SetMarginBottom(15);

                assetTable.AddCell(new Cell().Add(new Paragraph("Wagon Type").SetFont(bold).SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER))).SetBackgroundColor(ColorConstants.BLUE);
                assetTable.AddCell(new Cell().Add(new Paragraph(model?.WagonType ?? "N/A").SetFont(regular).SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER))).SetBackgroundColor(ColorConstants.WHITE);

                assetTable.AddCell(new Cell().Add(new Paragraph("Wagon Group").SetFont(bold).SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER))).SetBackgroundColor(ColorConstants.BLUE);
                assetTable.AddCell(new Cell().Add(new Paragraph(model?.WagonGroup ?? "N/A").SetFont(regular).SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER))).SetBackgroundColor(ColorConstants.WHITE);

                assetTable.AddCell(new Cell().Add(new Paragraph("Wagon Number").SetFont(bold).SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER))).SetBackgroundColor(ColorConstants.BLUE);
                assetTable.AddCell(new Cell().Add(new Paragraph($"{wagonNumber}").SetFont(regular).SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER))).SetBackgroundColor(ColorConstants.WHITE);

                assetTable.AddCell(new Cell().Add(new Paragraph("GPS Latitude").SetFont(bold).SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER))).SetBackgroundColor(ColorConstants.BLUE);
                assetTable.AddCell(new Cell().Add(new Paragraph(model?.GpsLatitude ?? "Not Captured").SetFont(regular).SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER))).SetBackgroundColor(ColorConstants.WHITE);

                assetTable.AddCell(new Cell().Add(new Paragraph("GPS Longitude").SetFont(bold).SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER))).SetBackgroundColor(ColorConstants.BLUE);
                assetTable.AddCell(new Cell().Add(new Paragraph(model?.GpsLongitude ?? "Not Captured").SetFont(regular).SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER))).SetBackgroundColor(ColorConstants.WHITE);

                assetTable.AddCell(new Cell().Add(new Paragraph("Country").SetFont(bold).SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER))).SetBackgroundColor(ColorConstants.BLUE);
                assetTable.AddCell(new Cell().Add(new Paragraph("South Africa").SetFont(regular).SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER))).SetBackgroundColor(ColorConstants.WHITE);

                assetTable.AddCell(new Cell().Add(new Paragraph("Net Book Value").SetFont(bold).SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER))).SetBackgroundColor(ColorConstants.BLUE);
                assetTable.AddCell(new Cell().Add(new Paragraph(model?.NetBookValue != "#N/A" ? $"R{model?.NetBookValue:F2}" : "#N/A").SetFont(regular).SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER))).SetBackgroundColor(ColorConstants.WHITE);

                assetTable.AddCell(new Cell().Add(new Paragraph("Inspection Date").SetFont(bold).SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER))).SetBackgroundColor(ColorConstants.BLUE);
                assetTable.AddCell(new Cell().Add(new Paragraph(DateTime.Now.ToString("yyyy-MM-dd")).SetFont(regular).SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER))).SetBackgroundColor(ColorConstants.WHITE);

                document.Add(assetTable);

                string currentDate = DateTime.Now.ToString("yyyy-MM-dd");

                // First paragraph
                document.Add(new Paragraph()
                    .Add("This Valuation Certificate declares that the professional team - which is fully defined in the\n" +
                         "accompanying Quote document - has inspected Wagon: " + wagonNumber + "; we have verified the\n" +
                         "particulars set out in this valuation, and we value the herein described item for the purposes of this\n" +
                         "valuation to the best of our knowledge and skill on " + currentDate + " at a market value of:\n")
                    .SetFont(regular)
                    .SetFontSize(13)
                    .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER)
                    .SetMarginBottom(15)
                );

                // Valuation title
                document.Add(new Paragraph("VALUATION ESTIMATE")
                    .SetFont(bold)
                    .SetFontSize(25)
                    .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER)
                    .SetMarginBottom(10)
                );

                // Second paragraph
                document.Add(new Paragraph()
                    .Add("This certificate forms part of and must be read in conjunction with the Quote document. Please refer\n" +
                         "to the accompanying Quote document.\n")
                    .SetFont(regular)
                    .SetFontSize(13)
                    .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER)
                    .SetMarginBottom(15)
                );

                // Third paragraph
                document.Add(new Paragraph()
                    .Add("Date: " + currentDate + "\n" +
                         "Technical Team: Worldwide Rail and Mining Solutions (Pty) Ltd\n" +
                         "Professional Valuation Company: Msomi Valuation Services (Pty) Ltd\n" +
                         "Registered Professional Valuer (South Africa) – J.D.S. Oberholzer\n" +
                         "SACPVP Reg. No. (Has not been given)\n" +
                         "Member of the South African Institute of Valuers\n")
                    .SetFont(regular)
                    .SetFontSize(15)
                    .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER)
                );

                string footerPath = Path.Combine(_env.WebRootPath, "Images", "CertLogo2.png");
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

                var dashboard = await _context.WagonDashboards
                .FirstOrDefaultAsync(d => d.WagonNumber == wagonNumber);

                if (dashboard != null)
                {
                    string relativePath = Path.Combine("InspectionPdf", "Wagons", "CertPdf", fileName);
                    dashboard.AssessmentCert = relativePath;
                    _context.WagonDashboards.Update(dashboard);
                    await _context.SaveChangesAsync();
                }

                return Ok(new { message = "PDF generated successfully", path = filePath });
            }
        }
        [HttpPost("GenerateAndSaveCertPdfForAllWagon")]
        public async Task<IActionResult> GenerateAndSaveCertPdfForAllWagon()
        {
            var existingDashboard = await _context.WagonDashboards
                .Where(d => d.AssessmentCert == "Not Ready").ToListAsync();
            foreach (var dashboard in existingDashboard)
            {                 await GenerateAndSaveCertPdf(dashboard.WagonNumber);
            }
            return Ok(new { message = "PDFs generated successfully for all wagons." });
        }

        [HttpPost("GenerateAndSaveLocoCertPdf")]
        public async Task<IActionResult> GenerateAndSaveLocoCertPdf([FromBody] int locoNumber)
        {
            _context.Database.SetCommandTimeout(180);

            var model = await _context.LocoInfoCaptures
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.LocoNumber == locoNumber);

            // --- Ensure folder exists ---
            string folderPath = Path.Combine(_env.WebRootPath, "InspectionPdf", "Locos", "CertPdf");
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            string fileName = $"{locoNumber}_{model?.LocoModel}_Cert_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            string filePath = Path.Combine(folderPath, fileName);

            using (var writer = new PdfWriter(filePath))
            using (var pdf = new iText.Kernel.Pdf.PdfDocument(writer))
            using (var document = new Document(pdf))
            {
                var bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                var regular = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

                string headerPath = Path.Combine(_env.WebRootPath, "Images", "CertLogo1.png");
                if (System.IO.File.Exists(headerPath))
                {
                    var pageSize = pdf.GetDefaultPageSize();
                    float pageWidth = pageSize.GetWidth();
                    float pageHeight = pageSize.GetHeight();

                    var headerImg = new Image(ImageDataFactory.Create(headerPath));

                    // Scale image to page width while maintaining aspect ratio
                    headerImg.ScaleToFit(pageWidth, pageHeight / 6f); // adjust height as needed

                    // Set absolute position: x = 0, y = pageHeight - imageHeight (top of page)
                    headerImg.SetFixedPosition(0, pageHeight - headerImg.GetImageScaledHeight());

                    document.Add(headerImg);
                }

                if (!string.IsNullOrWhiteSpace(model?.LocoPhoto))
                {
                    string fullImagePath = Path.Combine(_env.WebRootPath, model.LocoPhoto.TrimStart('/').TrimStart('\\'));

                    if (System.IO.File.Exists(fullImagePath))
                    {
                        var wagonImg = new Image(ImageDataFactory.Create(fullImagePath));

                        wagonImg
                            .SetHorizontalAlignment(iText.Layout.Properties.HorizontalAlignment.CENTER)
                            .SetMarginBottom(15)
                            .SetAutoScale(true)
                            .SetWidth(350f)
                            .SetHeight(300f);

                        document.Add(wagonImg);
                    }
                }

                document.Add(new Paragraph("DESCRIPTION OF ASSETS").SetFont(bold).SetFontSize(25).SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER)).SetBottomMargin(10);

                var assetTable = new Table(UnitValue.CreatePercentArray(2)).UseAllAvailableWidth().SetMarginBottom(15);

                assetTable.AddCell(new Cell().Add(new Paragraph("Loco Type").SetFont(bold).SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER))).SetBackgroundColor(ColorConstants.BLUE);
                assetTable.AddCell(new Cell().Add(new Paragraph(model?.LocoClass ?? "N/A").SetFont(regular).SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER))).SetBackgroundColor(ColorConstants.WHITE);

                assetTable.AddCell(new Cell().Add(new Paragraph("Loco Model").SetFont(bold).SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER))).SetBackgroundColor(ColorConstants.BLUE);
                assetTable.AddCell(new Cell().Add(new Paragraph(model?.LocoModel ?? "N/A").SetFont(regular).SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER))).SetBackgroundColor(ColorConstants.WHITE);

                assetTable.AddCell(new Cell().Add(new Paragraph("Loco Number").SetFont(bold).SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER))).SetBackgroundColor(ColorConstants.BLUE);
                assetTable.AddCell(new Cell().Add(new Paragraph($"{locoNumber}").SetFont(regular).SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER))).SetBackgroundColor(ColorConstants.WHITE);

                assetTable.AddCell(new Cell().Add(new Paragraph("GPS Latitude").SetFont(bold).SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER))).SetBackgroundColor(ColorConstants.BLUE);
                assetTable.AddCell(new Cell().Add(new Paragraph(model?.GpsLatitude ?? "Not Captured").SetFont(regular).SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER))).SetBackgroundColor(ColorConstants.WHITE);

                assetTable.AddCell(new Cell().Add(new Paragraph("GPS Longitude").SetFont(bold).SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER))).SetBackgroundColor(ColorConstants.BLUE);
                assetTable.AddCell(new Cell().Add(new Paragraph(model?.GpsLongitude ?? "Not Captured").SetFont(regular).SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER))).SetBackgroundColor(ColorConstants.WHITE);

                assetTable.AddCell(new Cell().Add(new Paragraph("Country").SetFont(bold).SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER))).SetBackgroundColor(ColorConstants.BLUE);
                assetTable.AddCell(new Cell().Add(new Paragraph("South Africa").SetFont(regular).SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER))).SetBackgroundColor(ColorConstants.WHITE);

                assetTable.AddCell(new Cell().Add(new Paragraph("Net Book Value").SetFont(bold).SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER))).SetBackgroundColor(ColorConstants.BLUE);
                assetTable.AddCell(new Cell().Add(new Paragraph(model?.NetBookValue != "#N/A" ? $"R{model?.NetBookValue:F2}" : "#N/A").SetFont(regular).SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER))).SetBackgroundColor(ColorConstants.WHITE);

                assetTable.AddCell(new Cell().Add(new Paragraph("Inspection Date").SetFont(bold).SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER))).SetBackgroundColor(ColorConstants.BLUE);
                assetTable.AddCell(new Cell().Add(new Paragraph(DateTime.Now.ToString("yyyy-MM-dd")).SetFont(regular).SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER))).SetBackgroundColor(ColorConstants.WHITE);

                document.Add(assetTable);

                string currentDate = DateTime.Now.ToString("yyyy-MM-dd");

                // First paragraph
                document.Add(new Paragraph()
                    .Add("This Valuation Certificate declares that the professional team - which is fully defined in the\n" +
                         "accompanying Quote document - has inspected Loco: " + locoNumber + "; we have verified the\n" +
                         "particulars set out in this valuation, and we value the herein described item for the purposes of this\n" +
                         "valuation to the best of our knowledge and skill on " + currentDate + " at a market value of:\n")
                    .SetFont(regular)
                    .SetFontSize(13)
                    .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER)
                    .SetMarginBottom(15)
                );

                // Valuation title
                document.Add(new Paragraph("VALUATION ESTIMATE")
                    .SetFont(bold)
                    .SetFontSize(25)
                    .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER)
                    .SetMarginBottom(10)
                );

                // Second paragraph
                document.Add(new Paragraph()
                    .Add("This certificate forms part of and must be read in conjunction with the Quote document. Please refer\n" +
                         "to the accompanying Quote document.\n")
                    .SetFont(regular)
                    .SetFontSize(13)
                    .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER)
                    .SetMarginBottom(15)
                );

                // Third paragraph
                document.Add(new Paragraph()
                    .Add("Date: " + currentDate + "\n" +
                         "Technical Team: Worldwide Rail and Mining Solutions (Pty) Ltd\n" +
                         "Professional Valuation Company: Msomi Valuation Services (Pty) Ltd\n" +
                         "Registered Professional Valuer (South Africa) – J.D.S. Oberholzer\n" +
                         "SACPVP Reg. No. (Has not been given)\n" +
                         "Member of the South African Institute of Valuers\n")
                    .SetFont(regular)
                    .SetFontSize(15)
                    .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER)
                );

                string footerPath = Path.Combine(_env.WebRootPath, "Images", "CertLogo2.png");
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

                var dashboard = await _context.LocoDashboards
                .FirstOrDefaultAsync(d => d.LocoNumber == locoNumber);

                if (dashboard != null)
                {
                    string relativePath = Path.Combine("InspectionPdf", "Locos", "CertPdf", fileName);
                    dashboard.AssessmentCert = relativePath;
                    _context.LocoDashboards.Update(dashboard);
                    await _context.SaveChangesAsync();
                }

                return Ok(new { message = "PDF generated successfully", path = filePath });
            }
        }

        [HttpPost("GenerateAndSaveCertPdfForAllLoco")]
        public async Task<IActionResult> GenerateAndSaveCertPdfForAllLoco()
        {
            var existingDashboard = await _context.LocoDashboards
                .Where(d => d.AssessmentCert == "Not Ready").ToListAsync();
            foreach (var dashboard in existingDashboard)
            {
                await GenerateAndSaveLocoCertPdf((int)dashboard.LocoNumber);
            }
            return Ok(new { message = "PDFs generated successfully for all Locos." });
        }

    }
}
