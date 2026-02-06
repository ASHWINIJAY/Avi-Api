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

        public class CreateUserRequest
        {
            public string Username { get; set; }
            public string? Email { get; set; }
            public string? UserEmail { get; set; }
            public string Name { get; set; }
            public string? Password { get; set; }
            public string? UserPassword { get; set; }
            public string UserRole { get; set; }
        }
        [HttpGet("list")]
        public IActionResult List()
        {
            var users = _context.LeaseCoUsers.ToList();
            return Ok(users);
        }
        [HttpPost("update")]
        public async Task<IActionResult> UpdateUser([FromBody] CreateUserRequest request)
        {
          var user =  _context.LeaseCoUsers.Where(u => u.UserName == request.Username).FirstOrDefault();


          user.UserName = request.Username;
            user.UserEmail = request.UserEmail;
            user.UserRole= request.UserRole;
            user.Name = request.Name;
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

            var user = new LeaseCoUser
            {
                UserName = request.Username,
                Name = request.Name,
                UserEmail = request.Email,
                UserPassword = hashedPassword,
                UserRole = request.UserRole
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

            // Hash the incoming password the same way
            string hashedPassword = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: request.Password,
                salt: Encoding.UTF8.GetBytes("static_salt_here"), // must match the one used in creation
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 10000,
                numBytesRequested: 256 / 8
            ));

            var user = _context.LeaseCoUsers
                        .FirstOrDefault(u => u.UserName == request.Username
                                        );

            if (user == null)
                return Unauthorized("Invalid credentials");

            // Generate JWT
            var token1 = GenerateJwtToken(user);
            _logger.LogInformation("User logged in: " + request.Username);

            return Ok(new { token = token1, userId = user.UserId, userRole = user.UserRole,name = user.Name??string.Empty });
        }
    }
}