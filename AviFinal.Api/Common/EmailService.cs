using System.Net;
using System.Net.Mail;

namespace AviFinal.Api.Common
{
    public class EmailService : IEmailService
    {
        public async Task SendErrorEmailAsync(Exception ex, HttpContext context)
        {
            try
            {
                var errorDetails = GetExceptionDetails(ex);

                var mail = new MailMessage();
                mail.From = new MailAddress("codexitza@gmail.com");
                mail.To.Add("aswini@codex-it.co.za");

                mail.Subject = "🚨 API Exception Alert";

                var request = context.Request;

                mail.Body = $@"
{errorDetails}

API: {context.Request.Path}
Method: {context.Request.Method}
Time: {DateTime.Now}
";

                var smtp = new SmtpClient("smtp.gmail.com", 587)
                {
                    Credentials = new NetworkCredential("codexitza@gmail.com", "evudoqowdmaqqhzr"),
                    EnableSsl = true
                };

                await smtp.SendMailAsync(mail);
            }
            catch
            {
                // Avoid crash if email fails
            }
        }
        private string GetExceptionDetails(Exception ex)
        {
            var stackTrace = new System.Diagnostics.StackTrace(ex, true);
            var frame = stackTrace.GetFrames()?.FirstOrDefault(f => f.GetFileLineNumber() > 0);

            var lineNumber = frame?.GetFileLineNumber();
            var fileName = frame?.GetFileName();
            var methodName = frame?.GetMethod()?.Name;

            return $@"
Error: {ex.Message}

Method: {methodName}
File: {fileName}
Line: {lineNumber}

StackTrace:
{ex.StackTrace}

Inner Exception:
{ex.InnerException?.Message}
";
        }
    }
}
