namespace AviFinal.Api.DTO
{
    public class InspectionStatusDto
    {
        public string InspectionType { get; set; }
        public int Total { get; set; }
        public int Inspected { get; set; }
        public int Pending { get; set; }
        public decimal CompletionPercent { get; set; }
        public int Uploaded { get; set; }
        public int PendingUploaded { get; set; }
        public decimal CompletionUploadedPercent { get; set; }
    }

}
