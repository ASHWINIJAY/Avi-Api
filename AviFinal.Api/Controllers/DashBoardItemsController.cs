using AviFinal.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AviFinal.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashBoardItemsController : ControllerBase
    {
        private readonly AviDbContext _context;

        public DashBoardItemsController(AviDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboardItems()
        {
            // Step 1: Fetch data from DB (in EF-compatible way)
            var itemsFromDb = await _context.DashBoardItems
                .Select(d => new
                {
                    record = d.Record,
                    locoNumber = d.LocoNumber,
                    dateAssessed = d.DateAssessed.ToString("yyyy-MM-dd"),
                    timeAssessed = d.TimeAssessed.ToString(@"hh\:mm"),
                    inspectorName = d.InspectorName ?? "",
                    proMain = d.ProMain ?? "",
                    bodyDamage = d.BodyDamage ?? "",
                    bodyPhotos = d.BodyPhotos ?? "",
                    bodyRepairValue = string.IsNullOrEmpty(d.BodyRepairValue) ? "0" : d.BodyRepairValue,
                    replaceValue = string.IsNullOrEmpty(d.ReplaceValue) ? "0" : d.ReplaceValue,
                    refurbishValue = string.IsNullOrEmpty(d.RefurbishValue) ? "0" : d.RefurbishValue,
                    liftingRequired = d.LiftingRequired ?? "",
                    liftPhotos = d.LiftPhotos ?? "",
                    liftDate = d.LiftDate.HasValue ? d.LiftDate.Value.ToString("yyyy-MM-dd") : "",
                    assessmentResults = d.AssessmentResults ?? "Not Functional",
                    assessmentPhotosRaw = d.AssessmentPhotos ?? "",
                    assessmentQuote = d.AssessmentQuote ?? "Not Functional",
                    assessmentCert = d.AssessmentCert ?? "Not Functional",
                    uploadStatus = d.UploadStatus ?? "",
                    uploadDate = d.UploadDate.ToString("yyyy-MM-dd")
                })
                .ToListAsync();

            // Step 2: Process AssessmentPhotos in-memory
            var formattedItems = itemsFromDb.Select(d => new
            {
                d.record,
                d.locoNumber,
                d.dateAssessed,
                d.timeAssessed,
                d.inspectorName,
                d.proMain,
                d.bodyDamage,
                d.bodyPhotos,
                d.bodyRepairValue,
                d.replaceValue,
                d.refurbishValue,
                d.liftingRequired,
                d.liftPhotos,
                d.liftDate,
                d.assessmentResults,
                assessmentPhotos = d.assessmentPhotosRaw
                    .Split(';', StringSplitOptions.RemoveEmptyEntries),
                d.assessmentQuote,
                d.assessmentCert,
                d.uploadStatus,
                d.uploadDate
            });

            return Ok(formattedItems);
        }
    }
}
