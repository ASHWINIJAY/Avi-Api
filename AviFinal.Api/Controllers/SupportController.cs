using AviFinal.Api.DTO;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Mail;

namespace AviFinal.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SupportController : ControllerBase
    {
        private readonly IConfiguration _config;

        public SupportController(IConfiguration config)
        {
            _config = config;
        }

        [HttpPost("log-error")]
        public async Task<IActionResult> LogError([FromBody] ErrorLogDto model)
        {
            try
            {
                // 🔹 Convert Screenshot Base64 → Image
                byte[] imageBytes = null;

                if (!string.IsNullOrEmpty(model.Screenshot))
                {
                    var base64Data = model.Screenshot.Contains(",")
                        ? model.Screenshot.Split(',')[1]
                        : model.Screenshot;

                    imageBytes = Convert.FromBase64String(base64Data);
                }

                // 🔹 Build Email Body (Readable format)
                var body = $@"
🚨 ERROR OCCURRED

Message:
{model.Message}

-----------------------------------
STACK TRACE:
{model.Stack}

-----------------------------------
API:
{model.ApiUrl}

-----------------------------------
USER DETAILS:
{System.Text.Json.JsonSerializer.Serialize(model.User, new System.Text.Json.JsonSerializerOptions { WriteIndented = true })}

-----------------------------------
REQUEST DATA:
{System.Text.Json.JsonSerializer.Serialize(model.RequestData, new System.Text.Json.JsonSerializerOptions { WriteIndented = true })}

-----------------------------------
BROWSER:
{model.Browser}

-----------------------------------
TIME:
{model.Timestamp}
";

                // 🔹 Create Mail
                var mail = new MailMessage();
                mail.From = new MailAddress(_config["EmailSettings:From"]);
                mail.To.Add(_config["EmailSettings:To"]);
                mail.Subject = "🚨 Application Error - Loco Inspection";
                mail.Body = body;

                // 🔹 Attach Screenshot
                if (imageBytes != null)
                {
                    var stream = new MemoryStream(imageBytes);
                    mail.Attachments.Add(new Attachment(stream, "screenshot.png", "image/png"));
                }

                // 🔹 SMTP Config
                var smtp = new SmtpClient(_config["EmailSettings:SmtpServer"], int.Parse(_config["EmailSettings:Port"]))
                {
                    Credentials = new NetworkCredential(
                        _config["EmailSettings:Username"],
                        _config["EmailSettings:Password"]
                    ),
                    EnableSsl = true
                };

                await smtp.SendMailAsync(mail);

                // 🔹 OPTIONAL: Save to DB
                // await SaveErrorToDatabase(model);

                return Ok(new { message = "Error logged and email sent successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error while logging: {ex.Message}");
            }
        }
    }
}
