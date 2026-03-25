namespace AviFinal.Api.DTO
{
    public class ErrorLogDto
    {
        public string Message { get; set; }
        public string Stack { get; set; }
        public string ApiUrl { get; set; }
        public object RequestData { get; set; }
        public object User { get; set; }
        public string Browser { get; set; }
        public DateTime Timestamp { get; set; }
        public string Screenshot { get; set; } // base64 image
        public string ScreenName { get; set; }
    }
}
