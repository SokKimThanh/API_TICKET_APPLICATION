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
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var defaultRole = string.IsNullOrWhiteSpace(request.Role) ? "Customer" : request.Role.Trim();

                // Chuẩn hóa chữ hoa chữ thường cho Role để khớp chính xác với database check constraint ('Customer', 'Admin')
                if (string.Equals(defaultRole, "Customer", StringComparison.OrdinalIgnoreCase))
                {
                    defaultRole = "Customer";
                }
                else if (string.Equals(defaultRole, "Admin", StringComparison.OrdinalIgnoreCase))
                {
                    defaultRole = "Admin";
                }

                // Ngăn chặn leo thang đặc quyền (Privilege Escalation):
                // Chỉ cho phép đăng ký tài khoản có vai trò 'Admin' khi:
                // 1. Hệ thống chưa có bất kỳ quản trị viên nào hoạt động (Bootstrap Admin).
                // 2. Hoặc Người đang thực hiện yêu cầu là một Admin hợp lệ đã được xác thực.
                if (defaultRole == "Admin")
                {
                    var anyAdminExists = await _context.Users.AnyAsync(u => u.Role == "Admin" && u.IsDeleted == false);
                    if (anyAdminExists)
                    {
                        var currentUserId = GetUserId();
                        var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

                        if (currentUserRole != "Admin")
                        {
                            return ErrorResponse("Bạn không có quyền đăng ký tài khoản với vai trò Quản trị viên (Admin).", StatusCodes.Status403Forbidden, "FORBIDDEN");
                        }
                    }
                }

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
                Logger.LogError(ex, "Lỗi xảy ra trong hệ thống");
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
                Logger.LogError(ex, "Lỗi xảy ra trong hệ thống");
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
