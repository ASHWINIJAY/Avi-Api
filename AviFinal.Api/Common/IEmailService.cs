namespace AviFinal.Api.Common
{
    public interface IEmailService
    {
        Task SendErrorEmailAsync(Exception ex, HttpContext context);
    }
}
