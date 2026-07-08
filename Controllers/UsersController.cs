using Microsoft.AspNetCore.Mvc;
using API_TICKET_APPLICATION.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;

namespace API_TICKET_APPLICATION.Controllers
{
    public class UsersController : AppBaseController
    {
        private readonly IConfiguration _configuration;

        public UsersController(AppDbContext context, IConfiguration configuration) : base(context)
        {
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var user = new User { Email = request.Email, FullName = request.FullName, Role = "User" };
                var validation = UserValidator.ValidateRegistration(user, request.Password);

                if (!validation.IsValid)
                    return BadRequestError(validation.ErrorMessage ?? "Dữ liệu không hợp lệ");

                // Check if email exists
                if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                    return BadRequestError("Email này đã được sử dụng");

                // Hash password
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
                user.CreatedAt = DateTime.UtcNow;
                user.IsDeleted = false;

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                return CreatedResponse(new { user.Id, user.Email, user.FullName }, "Đăng ký tài khoản thành công");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return ErrorResponse("Lỗi hệ thống khi đăng ký", StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var validation = UserValidator.ValidateLogin(request.Email, request.Password);
                if (!validation.IsValid)
                    return BadRequestError(validation.ErrorMessage ?? "Dữ liệu không hợp lệ");

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email && u.IsDeleted == false);

                if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                    return BadRequestError("Email hoặc mật khẩu không chính xác");

                var token = GenerateJwtToken(user);

                return OkResponse(new
                {
                    token,
                    user = new { user.Id, user.Email, user.FullName, user.Role }
                }, "Đăng nhập thành công");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return ErrorResponse("Lỗi hệ thống khi đăng nhập", StatusCodes.Status500InternalServerError);
            }
        }

        private string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var key = Encoding.ASCII.GetBytes(jwtSettings["SecretKey"]!);

            var claims = new List<Claim>
            {
                new Claim("UserId", user.Id.ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["ExpiryInMinutes"]!)),
                Issuer = jwtSettings["Issuer"],
                Audience = jwtSettings["Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }

    public class RegisterRequest
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string FullName { get; set; } = null!;
    }

    public class LoginRequest
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
    }
}
