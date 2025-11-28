namespace AviFinal.Api.DTO
{
    public class InspectionStatusDto
    {
        public string InspectionType { get; set; }
        public int Total { get; set; }
        public int Inspected { get; set; }
        public int Pending { get; set; }
        public decimal CompletionPercent { get; set; }
    }

}
