using AviFinal.Api.Models;
using IronPdf;
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

        public QuotePdfController(AviDbContext context)
        {
            _context = context;
        }

        [HttpPost("GenerateQuotePdf")]
        public async Task<IActionResult> GenerateQuotePdf([FromBody] int wagonNumber)
        {
            // Fetch parts data from the database
            var allParts = (await _context.AirBrakePartsInspects
                    .Where(p => p.WagonNumber == wagonNumber).ToListAsync())
                .Cast<dynamic>()
                .Concat(await _context.BottomDischargeInspects.Where(p => p.WagonNumber == wagonNumber).ToListAsync())
                .Concat(await _context.DoorsInspects.Where(p => p.WagonNumber == wagonNumber).ToListAsync())
                .Concat(await _context.FloorInspects.Where(p => p.WagonNumber == wagonNumber).ToListAsync())
                .Concat(await _context.StanchionsInspects.Where(p => p.WagonNumber == wagonNumber).ToListAsync())
                .Concat(await _context.TankersInspects.Where(p => p.WagonNumber == wagonNumber).ToListAsync())
                .Concat(await _context.TwistlocksInspects.Where(p => p.WagonNumber == wagonNumber).ToListAsync())
                .Concat(await _context.VacBrakePartsInspects.Where(p => p.WagonNumber == wagonNumber).ToListAsync())
                .Concat(await _context.WagonPartsInspects.Where(p => p.WagonNumber == wagonNumber).ToListAsync())
                .ToList();

            if (!allParts.Any())
                return NotFound("No parts found for this wagon number.");

            var partsWithNumbers = allParts.Select(p =>
            {
                string refurbish = p.RefurbishValue?.ToString() ?? "0.00";
                string missing = p.MissingValue?.ToString() ?? "0.00";
                string replace = p.ReplaceValue?.ToString() ?? "0.00";

                return new
                {
                    p.FormID,
                    p.PartDescr,
                    RefurbishValue = refurbish,
                    MissingValue = missing,
                    ReplaceValue = replace
                };
            }).ToList();

            // Totals calculation
            decimal totalRefurbish = partsWithNumbers.Sum(p => decimal.TryParse(p.RefurbishValue, out decimal refurbish) ? refurbish : 0);
            decimal totalMissing = partsWithNumbers.Sum(p => decimal.TryParse(p.MissingValue, out decimal missing) ? missing : 0);
            decimal totalReplace = partsWithNumbers.Sum(p => decimal.TryParse(p.ReplaceValue, out decimal replace) ? replace : 0);
            decimal grandTotal = totalRefurbish + totalMissing + totalReplace;

            // HTML template for PDF generation
            string html = $@"
<html>
<head>
    <style>
        body {{
            font-family: Arial, sans-serif;
            margin: 0;
            padding: 0;
        }}
        .con-1 {{
            height: auto;
            width: 100%;
            background-color: lightgray;
            padding-top: 20px;
            margin: 0;
            display: flex;
            align-items: center;
            justify-content: center;
            padding-bottom: 20px;
        }}
        .pdf-con {{
            height: auto;
            width: 700px;
            background-color: white;
            padding-top: 40px;
            display: grid;
            grid-template-rows: 1fr, 3fr, 1fr;
        }}
        .top-sec {{
            height: 290px;
            width: 100%;
            display: grid;
            padding-left: 35px;
            padding-right: 35px;
            grid-template-columns: repeat(2, 1fr);
            border-bottom: solid black 2px;
            align-content: start;
        }}
        .logo-top {{
            width: 100%;
            height: auto;
            display: grid;
            grid-template-rows: repeat(2, 1fr);
            margin-bottom: 10px;
        }}
        .logo-con1 {{
            width: 100%;
            height: 100%;
            display: flex;
            justify-content: left;
            align-items: center;
        }}
        .logo-con2 {{
            width: 100%;
            height: 100%;
            display: flex;
            justify-content: left;
            align-items: center;
        }}
        .info-top {{
            width: 100%;
            height: auto;
            display: grid;
            grid-template-rows: repeat(13, 18px);
            justify-content: end;
            font-family: 'Poppins', sans-serif;
            margin-bottom: 10px;
        }}
        .info-bold {{
            text-align: right;
            font-size: 14px;
            font-weight: 600;
        }}
        .info-reg {{
            text-align: right;
            font-size: 13px;
            font-weight: 400;
        }}
        .mid-sec {{
            height: auto;
            width: 100%;
            display: grid;
            padding-left: 35px;
            padding-right: 35px;
            grid-template-rows: 1fr, 1fr, 4fr;
            padding-top: 20px;
        }}
        .asset-head {{
            width: 100%;
            height: 40px;
            font-size: 21px;
            text-align: center;
            margin-bottom: 0 !important;
            font-family: 'Poppins', sans-serif;
        }}
        .process-head {{
            width: 100%;
            height: 30px;
            font-size: 17px;
            font-family: 'Poppins', sans-serif;
            text-align: left;
            margin-bottom: 0 !important;
        }}
        .table-block {{
            width: 100%;
            overflow-x: auto;
            margin-bottom: 30px;
        }}
        .pdf-table {{
            width: 100%;
            border-collapse: collapse;
            margin-top: 15px;
            font-family: Arial, sans-serif;
            font-size: 13px;
        }}
        .pdf-table th {{
            background-color: #f5f5f5;
            border: 1px solid #ccc;
            padding: 8px;
            text-align: left;
            font-weight: bold;
        }}
        .pdf-table td {{
            border: 1px solid #ccc;
            padding: 8px;
            text-align: left;
        }}
        .pdf-table tr:nth-child(even) {{
            background-color: #fafafa;
        }}
        .no-data {{
            text-align: center;
            padding: 12px;
            color: #777;
        }}
        .subtotal-row, .grandtotal-row {{
            font-weight: bold;
        }}
        .subtotal-row {{
            background-color: #f8f8f8;
        }}
        .grandtotal-row {{
            background-color: #e6e6e6;
            border-top: 2px solid #333;
            font-size: 1.1em;
        }}
        .subtotal-label {{
            text-align: right;
            padding-right: 10px;
        }}
        .subtotal-value {{
            text-align: right;
        }}
        .foot-sec {{
            width: 100%;
            height: 210px;
        }}
        .foot-sec img {{
            width: 100%;
            height: 100%;
        }}
    </style>
</head>
<body>
    <div class='con-1'>
        <div class='pdf-con'>
            <div class='top-sec'>
                <div class='logo-top'>
                    <div class='logo-con1'>
                        <img src='file://{Path.Combine(Directory.GetCurrentDirectory(), "src", "pdf", "images", "Logo1.png")}' alt='Logo 1' />
                    </div>
                    <div class='logo-con2'>
                        <img src='file://{Path.Combine(Directory.GetCurrentDirectory(), "src", "pdf", "images", "Logo2.png")}' alt='Logo 2' />
                    </div>
                </div>
                <div class='info-top'>
                    <p class='info-bold'>Worldwide Rail and Mining Solutions SA (Pty) Ltd</p>
                    <p class='info-reg'>52 8th Avenue, Edenvale Gauteng 1610</p>
                    <p class='info-reg'>Email: adminsa@wwms.co.za</p>
                    <p class='info-reg'>T: +27 11 453 2170</p>
                    <p class='info-reg'>Website: www.worldwideminingsolutions.co.za</p>
                    <p class='info-reg'>Reg. No.: 2019/544337/07</p>
                    <p class='info-bold'>Msomi Valuation Services (Pty) Ltd</p>
                    <p class='info-reg'>4 Sheffield Road, Ferryvale, Nigel, 1491</p>
                    <p class='info-reg'>T: 011 814 2047</p>
                    <p class='info-reg'>Website: Not Given</p>
                    <p class='info-reg'>Reg. No.: 2016/384934/07</p>
                    <p class='info-reg'>VAT: 4400277721</p>
                </div>
            </div>
            <div class='mid-sec'>
                <h1 class='asset-head'>Quote - Wagon {wagonNumber}</h1>
                <h3 class='process-head'>Inspection Model: Loco Model</h3>
                <div class='table-block'>
                    <table class='pdf-table'>
                        <thead>
                            <tr>
                                <th>No.</th>
                                <th>Form ID</th>
                                <th>Part Description</th>
                                <th>Refurbish Value</th>
                                <th>Missing Value</th>
                                <th>Replace Value</th>
                            </tr>
                        </thead>
                        <tbody>";

            int index = 1;
            foreach (var p in partsWithNumbers)
            {
                html += $"<tr>" +
                        $"<td>{index++}</td>" +
                        $"<td>{p.FormID}</td>" +
                        $"<td>{p.PartDescr}</td>" +
                        $"<td>{(p.RefurbishValue != "0.00" ? "R" + p.RefurbishValue : "-")}</td>" +
                        $"<td>{(p.MissingValue != "0.00" ? "R" + p.MissingValue : "-")}</td>" +
                        $"<td>{(p.ReplaceValue != "0.00" ? "R" + p.ReplaceValue : "-")}</td>" +
                        $"</tr>";
            }

            html += $@"
                        <tr class='subtotal-row'>
                            <td colspan='3' style='text-align:right;'>Totals</td>
                            <td>R{totalRefurbish}</td>
                            <td>R{totalMissing}</td>
                            <td>R{totalReplace}</td>
                        </tr>
                        <tr class='grandtotal-row'>
                            <td colspan='5' style='text-align:right;'>Grand Total</td>
                            <td>R{grandTotal}</td>
                        </tr>
                    </tbody>
                </table>
            </div>
        </div>
        <div class='foot-sec'>
            <img src='file://{Path.Combine(Directory.GetCurrentDirectory(), "src", "pdf", "images", "Footer1.png")}' alt='Footer' />
        </div>
    </div>
</body>
</html>";

            // Generate PDF
            var Renderer = new ChromePdfRenderer();
            var pdf = Renderer.RenderHtmlAsPdf(html);

            // Save to disk
            string outputDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "InspectionPdf", "Wagons", "QuotePdf");
            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            string filePath = Path.Combine(outputDir, $"{wagonNumber}_Quote_{System.DateTime.Now:yyyy-MM-dd_HH-mm-ss}.pdf");
            pdf.SaveAs(filePath);

            return Ok(new { message = "PDF generated successfully.", path = filePath });
        }
    }
}
