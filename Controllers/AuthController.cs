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
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : AppBaseController
    {
        private readonly IConfiguration _configuration;

        public AuthController(AppDbContext context, IConfiguration configuration) : base(context)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Đăng ký tài khoản mới
        /// </summary>
        [HttpPost("register")]
        [ProducesResponseType(typeof(ResponseModel<UserResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var defaultRole = string.IsNullOrWhiteSpace(request.Role) ? "Customer" : request.Role.Trim();

                var user = new User
                {
                    FullName = request.FullName,
                    Email = request.Email,
                    Role = defaultRole
                };

                var validation = UserValidator.ValidateRegistration(user, request.Password);
                if (!validation.IsValid)
                    return BadRequestError(validation.ErrorMessage ?? "Dữ liệu không hợp lệ");

                if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                    return BadRequestError("Email đã tồn tại trên hệ thống");

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
                user.CreatedAt = DateTime.UtcNow;
                user.IsDeleted = false;

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                return OkResponse(new UserResponseDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FullName = user.FullName,
                    Role = user.Role
                }, "Đăng ký tài khoản thành công");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return ErrorResponse("Đã có lỗi hệ thống xảy ra", StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Đăng nhập và nhận JWT token
        /// </summary>
        [HttpPost("login")]
        [ProducesResponseType(typeof(ResponseModel<LoginResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var validation = UserValidator.ValidateLogin(request.Email, request.Password);
                if (!validation.IsValid)
                    return BadRequestError(validation.ErrorMessage ?? "Dữ liệu không hợp lệ");

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email && u.IsDeleted == false);
                if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                    return ErrorResponse("Email hoặc mật khẩu không chính xác", StatusCodes.Status401Unauthorized);

                var token = GenerateJwtToken(user);

                return OkResponse(new LoginResponseDto
                {
                    Token = token,
                    User = new UserResponseDto
                    {
                        Id = user.Id,
                        Email = user.Email,
                        FullName = user.FullName,
                        Role = user.Role
                    }
                }, "Đăng nhập thành công");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return ErrorResponse("Đã có lỗi hệ thống xảy ra", StatusCodes.Status500InternalServerError);
            }
        }

        private string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["ExpiryInMinutes"]!)),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    public class RegisterRequest
    {
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string? Role { get; set; }
    }

    public class LoginRequest
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
    }
}
