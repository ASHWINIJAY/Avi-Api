using AviAppFinal.Server.Controllers;
using AviFinal.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace AviFinal.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AviDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(AviDbContext context, IConfiguration configuration,ILogger<AuthController> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }
        public class ForgotPasswordRequest
        {
            public string Username { get; set; }
        }

        public class VerifyOtpRequest
        {
            public string Username { get; set; }
            public string Otp { get; set; }
        }

        public class ResetPasswordRequest
        {
            public string Username { get; set; }
            public string Otp { get; set; }
            public string NewPassword { get; set; }
        }
        public class CreateUserRequest
        {
            public string Username { get; set; }
            public string? Email { get; set; }
            public string? UserEmail { get; set; }
            public string Name { get; set; }
            public string? Password { get; set; }
            public string? UserPassword { get; set; }
            public string UserRole { get; set; }
            public string? CreatedBy { get; set; }
            public int? Active { get; set; }

            public int? IsDelete { get; set; }
        }
        public class ChangePasswordRequest
        {
            public string Username { get; set; }
            public string CurrentPassword { get; set; }
            public string NewPassword { get; set; }
        }
        private async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var smtpServer = _configuration["EmailSettings:SmtpServer"];
                var port = _configuration["EmailSettings:Port"];
                var username = _configuration["EmailSettings:Username"];
                var password = _configuration["EmailSettings:Password"];
                var fromEmail = _configuration["EmailSettings:From"];

                using (var client = new System.Net.Mail.SmtpClient(smtpServer, int.Parse(port)))
                {
                    client.EnableSsl = true;
                    client.Credentials = new System.Net.NetworkCredential(username, password);


                    var message = new System.Net.Mail.MailMessage(fromEmail, toEmail, subject, body);
                    await client.SendMailAsync(message);
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
                return false;
            }
        }

        private string GenerateOtp()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }
[AllowAnonymous]
[HttpPost("forgot-password")]
public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username))
            {
                return BadRequest("Username is required.");
            }

            var user = await _context.LeaseCoUsers
                .FirstOrDefaultAsync(u => u.UserName == request.Username && u.IsDelete != 1);

            if (user == null)
            {
                return NotFound("Username not found.");
            }

            if (string.IsNullOrEmpty(user.UserEmail))
            {
                return BadRequest("No email address found for this user. Please contact support.");
            }

            // Generate 6-digit OTP
            var otp = GenerateOtp();
            var expiresAt = DateTime.Now.AddMinutes(5);

            // Delete any existing OTPs for this user
            var existingOtps = _context.PasswordResetOtps
                .Where(o => o.UserName == request.Username && !o.IsUsed)
                .ToList();
            _context.PasswordResetOtps.RemoveRange(existingOtps);

            // Create new OTP record
            var otpRecord = new PasswordResetOtp
            {
                UserName = request.Username,
                OtpCode = otp,
                Email = user.UserEmail,
                CreatedAt = DateTime.Now,
                ExpiresAt = expiresAt,
                IsUsed = false,
                IsVerified = false
            };

            _context.PasswordResetOtps.Add(otpRecord);
            await _context.SaveChangesAsync();

            // Send email
            var emailBody = $@"
Dear {user.Name ?? request.Username},

Your password reset OTP is: {otp}

This code will expire in 5 minutes.

If you did not request a password reset, please ignore this email.

Best regards,
AVI System
";

            var emailSent = await SendEmailAsync(user.UserEmail, "Password Reset OTP - AVI System", emailBody);

            if (!emailSent)
            {
                return StatusCode(500, "Failed to send OTP email. Please try again later.");
            }

            // Return partial email for display
            var partialEmail = MaskEmail(user.UserEmail);
            return Ok(new
            {
                message = $"OTP sent to your registered email ({partialEmail})",
                email = partialEmail
            });
        }
[AllowAnonymous]
[HttpPost("verify-otp")]
public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Otp))
            {
                return BadRequest("Username and OTP are required.");
            }

            var otpRecord = await _context.PasswordResetOtps
                .Where(o => o.UserName == request.Username &&
                            o.OtpCode == request.Otp &&
                            !o.IsUsed &&
                            !o.IsVerified &&
                            o.ExpiresAt > DateTime.Now)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (otpRecord == null)
            {
                return BadRequest("Invalid or expired OTP.");
            }

            // Mark as verified
            otpRecord.IsVerified = true;
            await _context.SaveChangesAsync();

            return Ok(new { valid = true, message = "OTP verified successfully." });
        }
[AllowAnonymous]
[HttpPost("reset-password")]
public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.Otp) ||
                string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return BadRequest("All fields are required.");
            }

            if (request.NewPassword.Length < 8)
            {
                return BadRequest("Password must be at least 8 characters long.");
            }

            var otpRecord = await _context.PasswordResetOtps
                .Where(o => o.UserName == request.Username &&
                            o.OtpCode == request.Otp &&
                            !o.IsUsed &&
                            o.IsVerified &&
                            o.ExpiresAt > DateTime.Now)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (otpRecord == null)
            {
                return BadRequest("Invalid or expired OTP. Please verify your OTP first.");
            }

            var user = await _context.LeaseCoUsers
                .FirstOrDefaultAsync(u => u.UserName == request.Username);

            if (user == null)
            {
                return BadRequest("User not found.");
            }

            // Hash new password
            string newHash = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: request.NewPassword,
                salt: Encoding.UTF8.GetBytes("static_salt_here"),
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 10000,
                numBytesRequested: 256 / 8));

            user.UserPassword = newHash;
            user.UpdatedDate = DateTime.Now;
            user.UpdatedBy = request.Username;

            // Mark OTP as used
            otpRecord.IsUsed = true;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Password reset successfully. Please login with your new password." });
        }
private string MaskEmail(string email)
        {
            if (string.IsNullOrEmpty(email)) return "";

            var parts = email.Split('@');
            if (parts.Length != 2) return email;

            var localPart = parts[0];
            var domain = parts[1];

            if (localPart.Length <= 2)
            {
                return "***@" + domain;
            }

            return localPart.Substring(0, 2) + new string('*', Math.Min(localPart.Length - 2, 5)) + "@" + domain;
        }
        [HttpPost("changepassword")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.CurrentPassword) ||
                string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return BadRequest("All fields are required.");
            }

            var user = await _context.LeaseCoUsers
                .FirstOrDefaultAsync(u => u.UserName == request.Username);

            if (user == null)
                return BadRequest("Invalid username");

            // 🔹 Hash entered current password
            string currentHash = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: request.CurrentPassword,
                salt: Encoding.UTF8.GetBytes("static_salt_here"),
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 10000,
                numBytesRequested: 256 / 8));

            // 🔹 Compare with stored hash
            if (user.UserPassword != currentHash)
                return BadRequest("Existing password is incorrect");

            // 🔹 Hash new password
            string newHash = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: request.NewPassword,
                salt: Encoding.UTF8.GetBytes("static_salt_here"),
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 10000,
                numBytesRequested: 256 / 8));

            user.UserPassword = newHash;
            user.UpdatedDate = DateTime.Now;
            user.UpdatedBy = request.Username;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Password changed successfully" });
        }
        [HttpGet("list")]
        public IActionResult List()
        {
            var users = _context.LeaseCoUsers.Where(c => c.IsDelete !=1).ToList();
            return Ok(users);
        }
        [HttpPost("update")]
        public async Task<IActionResult> UpdateUser([FromBody] CreateUserRequest request)
        {
          var user =  _context.LeaseCoUsers.Where(u => u.UserName == request.Username).FirstOrDefault();
            var userId = User.FindFirst("UserId")?.Value;

            user.UserName = request.Username;
            user.UserEmail = request.UserEmail;
            user.UserRole= request.UserRole;
            user.Name = request.Name;
            user.Active = request.Active;
            user.UpdatedDate=DateTime.Now;
            user.UpdatedBy = request.CreatedBy;
            // 🔒 Handle password reset (only if new password is provided)
            if (!string.IsNullOrEmpty(request.UserPassword))
            {
                string hashedPassword = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                 password: request.UserPassword,
                 salt: Encoding.UTF8.GetBytes("static_salt_here"), // optional static salt
                 prf: KeyDerivationPrf.HMACSHA256,
                 iterationCount: 10000,
                 numBytesRequested: 256 / 8
             ));
                user.UserPassword = hashedPassword;

            }
            await _context.SaveChangesAsync();

            return Ok(new { message = "User updated successfully", userId = user.UserId });
        }
        [HttpPost("delete")]
        public async Task<IActionResult> DeleteUser([FromBody] string username)
        {
            var user = _context.LeaseCoUsers.Where(u => u.UserName == username).FirstOrDefault();
            var userId = User.FindFirst("UserId")?.Value;

            
            user.IsDelete = 1;
            user.UpdatedDate = DateTime.Now;
            user.UpdatedBy = userId;
            
            await _context.SaveChangesAsync();

            return Ok(new { message = "User Deleted successfully", userId = user.UserId });
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password) || string.IsNullOrEmpty(request.UserRole))
                return BadRequest("All fields are required.");

            if (_context.LeaseCoUsers.Any(u => u.UserName == request.Username))
                return BadRequest(new { message = "Username already exists." });

            // Hash password WITHOUT salt
            string hashedPassword = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: request.Password,
                salt: Encoding.UTF8.GetBytes("static_salt_here"), // optional static salt
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 10000,
                numBytesRequested: 256 / 8
            ));
            var userId = User.FindFirst("UserId")?.Value;
            var user = new LeaseCoUser
            {
                UserName = request.Username,
                Name = request.Name,
                UserEmail = request.Email,
                UserPassword = hashedPassword,
                UserRole = request.UserRole,
                CreatedBy = request.CreatedBy,
                CreatedDate = DateTime.Now,
                Active=request.Active,
                IsDelete=0

            };

            _context.LeaseCoUsers.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "User created successfully", userId = user.UserId });
        }

        public class LoginRequest
        {
            public string Username { get; set; }
            public string Password { get; set; }
        }
        private string GenerateJwtToken(LeaseCoUser user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
        new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
        new Claim(ClaimTypes.Name, user.UserName),
      new Claim("UserId", user.UserId.ToString()),
                    new Claim("UserRole", user.UserRole)
    };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(Convert.ToDouble(_configuration["Jwt:ExpireMinutes"])),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        [AllowAnonymous]
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
            {
                _logger.LogInformation("Login attempt with missing username or password: " + request.Username);
                return BadRequest("Username and password are required.");
            }

            // 1️⃣ Check if username exists first
            var user = _context.LeaseCoUsers
                        .FirstOrDefault(u => u.UserName == request.Username && u.IsDelete !=1);

            if (user == null)
            {
                return Unauthorized("Invalid username");
            }
            if (user.Active == 0)
            {
                return Unauthorized("User is not active");
            }

            // 2️⃣ Hash incoming password
            string hashedPassword = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: request.Password,
                salt: Encoding.UTF8.GetBytes("static_salt_here"), // must match creation
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 10000,
                numBytesRequested: 256 / 8
            ));

            // 3️⃣ Check password
            if (user.UserPassword != hashedPassword)
            {
                return Unauthorized("Invalid password");
            }

            // 4️⃣ Success
            var token1 = GenerateJwtToken(user);

            _logger.LogInformation("User logged in: " + request.Username);

            return Ok(new
            {
                token = token1,
                userId = user.UserId,
                userRole = user.UserRole,
                name = user.Name ?? string.Empty
            });
        }
    }
}