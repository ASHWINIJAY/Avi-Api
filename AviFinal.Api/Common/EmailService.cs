using System.Net;
using System.Net.Mail;

namespace AviFinal.Api.Common
{
    public class EmailService : IEmailService
    {
        private void WriteErrorToFile(Exception ex, HttpContext context)
        {
            try
            {
                var logFolder = Path.Combine(Directory.GetCurrentDirectory(), "Logs");

                if (!Directory.Exists(logFolder))
                    Directory.CreateDirectory(logFolder);

                var filePath = Path.Combine(logFolder, $"ErrorLog_{DateTime.Now:yyyyMMdd}.txt");

                var errorDetails = GetExceptionDetails(ex);

                var log = $@"
===============================
Time: {DateTime.Now}
API: {context.Request.Path}
Method: {context.Request.Method}

{errorDetails}
===============================

";

                File.AppendAllText(filePath, log);
            }
            catch
            {
                // Don't crash if logging fails
            }
        }
        public async Task SendErrorEmailAsync(Exception ex, HttpContext context)
        {
            try
            {
                var errorDetails = GetExceptionDetails(ex);
                WriteErrorToFile(ex, context);
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
