using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using API_TICKET_APPLICATION.Models;

namespace API_TICKET_APPLICATION.Controllers
{
    /// <summary>
    /// AppBaseController - Lớp quản gia cơ sở cho tất cả Controllers
    /// 
    /// Mục đích:
    ///   - Tập trung quản lý DbContext thông qua Dependency Injection
    ///   - Chuẩn hóa Response trả về (Response Wrapper)
    ///   - Cung cấp utility methods (GetUserId, GetCurrentUser, v.v.)
    ///   - Giảm code lặp lại (DRY - Don't Repeat Yourself)
    ///   - Tạo base layer cho architectural patterns
    /// 
    /// Lợi ích:
    ///   ✅ Code sạch, dễ bảo trì
    ///   ✅ Response format thống nhất cho toàn API
    ///   ✅ Logging, error handling tập trung
    ///   ✅ Security (JWT claims) quản lý ở một chỗ
    ///   ✅ Dễ mở rộng (authentication, authorization filter, v.v.)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public abstract class AppBaseController : ControllerBase
    {
        // ========== PROTECTED FIELDS ==========
        /// <summary>
        /// Hứng instance của AppDbContext thông qua Dependency Injection
        /// Protected để các lớp con có thể truy cập (ví dụ: _context.Movies)
        /// </summary>
        protected readonly AppDbContext _context;

        // ========== CONSTRUCTOR ==========
        /// <summary>
        /// Constructor của Base Controller
        /// 
        /// Tham số:
        ///   - context: DbContext instance được inject bởi ASP.NET Core DI container
        /// 
        /// Cách sử dụng trong lớp con:
        ///   public class MoviesController : AppBaseController
        ///   {
        ///       public MoviesController(AppDbContext context) : base(context)
        ///       {
        ///       }
        ///   }
        /// </summary>
        protected AppBaseController(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // ========== RESPONSE WRAPPER METHODS ==========

        /// <summary>
        /// OkResponse - Trả về kết quả thành công (HTTP 200)
        /// 
        /// Cấu trúc JSON trả về:
        /// {
        ///   "success": true,
        ///   "message": "Thành công",
        ///   "data": { ... }
        /// }
        /// 
        /// Ví dụ sử dụng:
        ///   var movie = await _context.Movies.FindAsync(id);
        ///   return OkResponse(movie, "Lấy phim thành công");
        /// </summary>
        /// <param name="data">Dữ liệu trả về (object)</param>
        /// <param name="message">Thông báo tùy chỉnh (default: "Thành công")</param>
        /// <returns>OkObjectResult (HTTP 200)</returns>
        protected OkObjectResult OkResponse(object? data, string message = "Thành công")
        {
            var response = new
            {
                success = true,
                message = message,
                data = data,
                timestamp = DateTime.UtcNow
            };

            return Ok(response);
        }

        /// <summary>
        /// OkResponse (không có dữ liệu) - Trả về kết quả thành công mà không cần data
        /// 
        /// Cấu trúc JSON trả về:
        /// {
        ///   "success": true,
        ///   "message": "Thành công",
        ///   "data": null
        /// }
        /// 
        /// Ví dụ sử dụng:
        ///   return OkResponse("Xóa phim thành công");
        /// </summary>
        /// <param name="message">Thông báo</param>
        /// <returns>OkObjectResult (HTTP 200)</returns>
        protected OkObjectResult OkResponse(string message = "Thành công")
        {
            return OkResponse(data: null, message: message);
        }

        /// <summary>
        /// CreatedResponse - Trả về kết quả tạo mới (HTTP 201 Created)
        /// 
        /// Cấu trúc JSON trả về:
        /// {
        ///   "success": true,
        ///   "message": "Tạo mới thành công",
        ///   "data": { id: 3, ... }
        /// }
        /// 
        /// Ví dụ sử dụng:
        ///   var newMovie = new Movie { Title = "...", ... };
        ///   await _context.Movies.AddAsync(newMovie);
        ///   await _context.SaveChangesAsync();
        ///   return CreatedResponse(newMovie, "Tạo phim thành công", $"/api/movies/{newMovie.Id}");
        /// </summary>
        /// <param name="data">Dữ liệu vừa tạo</param>
        /// <param name="message">Thông báo</param>
        /// <param name="location">URI của resource vừa tạo (header Location)</param>
        /// <returns>CreatedResult (HTTP 201)</returns>
        protected CreatedResult CreatedResponse(object data, string message = "Tạo mới thành công", string? location = null)
        {
            var response = new
            {
                success = true,
                message = message,
                data = data,
                timestamp = DateTime.UtcNow
            };

            return Created(location ?? string.Empty, response);
        }

        /// <summary>
        /// ErrorResponse - Trả về lỗi với HTTP Status tùy chỉnh
        /// 
        /// Cấu trúc JSON trả về:
        /// {
        ///   "success": false,
        ///   "message": "Không tìm thấy phim",
        ///   "data": null,
        ///   "errorCode": "NOT_FOUND"
        /// }
        /// 
        /// Ví dụ sử dụng:
        ///   var movie = await _context.Movies.FindAsync(id);
        ///   if (movie == null)
        ///       return ErrorResponse("Không tìm thấy phim", StatusCodes.Status404NotFound);
        /// 
        ///   // Hoặc tắt:
        ///   if (movie == null)
        ///       return NotFoundError("Không tìm thấy phim");
        /// </summary>
        /// <param name="errorMessage">Thông báo lỗi</param>
        /// <param name="statusCode">HTTP Status Code (default: 400)</param>
        /// <param name="errorCode">Error code tùy chỉnh (VD: "NOT_FOUND", "VALIDATION_ERROR")</param>
        /// <returns>ObjectResult với status tương ứng</returns>
        protected ObjectResult ErrorResponse(
            string errorMessage,
            int statusCode = StatusCodes.Status400BadRequest,
            string? errorCode = null)
        {
            var response = new
            {
                success = false,
                message = errorMessage,
                data = (object?)null,
                errorCode = errorCode ?? GetErrorCodeFromStatus(statusCode),
                timestamp = DateTime.UtcNow
            };

            return StatusCode(statusCode, response);
        }

        /// <summary>
        /// NotFoundError - Shortcut cho ErrorResponse 404
        /// 
        /// Ví dụ:
        ///   return NotFoundError("Phim không tồn tại");
        /// </summary>
        protected ObjectResult NotFoundError(string message = "Không tìm thấy")
        {
            return ErrorResponse(message, StatusCodes.Status404NotFound, "NOT_FOUND");
        }

        /// <summary>
        /// BadRequestError - Shortcut cho ErrorResponse 400
        /// 
        /// Ví dụ:
        ///   return BadRequestError("Dữ liệu không hợp lệ");
        /// </summary>
        protected ObjectResult BadRequestError(string message = "Yêu cầu không hợp lệ")
        {
            return ErrorResponse(message, StatusCodes.Status400BadRequest, "BAD_REQUEST");
        }

        /// <summary>
        /// UnauthorizedError - Shortcut cho ErrorResponse 401
        /// 
        /// Ví dụ:
        ///   return UnauthorizedError("Bạn cần đăng nhập");
        /// </summary>
        protected ObjectResult UnauthorizedError(string message = "Không được phép")
        {
            return ErrorResponse(message, StatusCodes.Status401Unauthorized, "UNAUTHORIZED");
        }

        /// <summary>
        /// ForbiddenError - Shortcut cho ErrorResponse 403
        /// 
        /// Ví dụ:
        ///   return ForbiddenError("Bạn không có quyền");
        /// </summary>
        protected ObjectResult ForbiddenError(string message = "Bị cấm truy cập")
        {
            return ErrorResponse(message, StatusCodes.Status403Forbidden, "FORBIDDEN");
        }

        // ========== UTILITY METHODS ==========

        /// <summary>
        /// GetUserId - Lấy ID người dùng từ JWT Token (Claim)
        /// 
        /// Giả định: JWT Token chứa Claim tên "UserId" hoặc "sub"
        /// 
        /// Ví dụ JWT Claim:
        /// {
        ///   "iss": "YourIssuer",
        ///   "aud": "YourAudience",
        ///   "UserId": "123",
        ///   "email": "user@example.com"
        /// }
        /// 
        /// Cách sử dụng:
        ///   var userId = GetUserId();
        ///   if (!userId.HasValue)
        ///       return UnauthorizedError("Bạn cần đăng nhập");
        /// 
        /// Trả về:
        ///   - Nếu tìm thấy Claim: ID của user (int)
        ///   - Nếu không: null
        /// </summary>
        protected int? GetUserId()
        {
            // Lấy Claim "UserId" từ HttpContext.User
            var userIdClaim = User.FindFirst("UserId") ?? User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
            {
                return userId;
            }

            return null;
        }

        /// <summary>
        /// GetUserEmail - Lấy email người dùng từ JWT Token
        /// 
        /// Cách sử dụng:
        ///   var email = GetUserEmail();
        /// </summary>
        protected string? GetUserEmail()
        {
            return User.FindFirst(ClaimTypes.Email)?.Value;
        }

        /// <summary>
        /// GetUserName - Lấy username người dùng từ JWT Token
        /// 
        /// Cách sử dụng:
        ///   var username = GetUserName();
        /// </summary>
        protected string? GetUserName()
        {
            return User.FindFirst(ClaimTypes.Name)?.Value;
        }

        /// <summary>
        /// IsUserAuthenticated - Kiểm tra xem người dùng đã đăng nhập hay chưa
        /// 
        /// Cách sử dụng:
        ///   if (!IsUserAuthenticated())
        ///       return UnauthorizedError();
        /// </summary>
        protected bool IsUserAuthenticated()
        {
            return User?.Identity?.IsAuthenticated ?? false;
        }

        /// <summary>
        /// IsUserInRole - Kiểm tra xem người dùng có role cụ thể hay không
        /// 
        /// Cách sử dụng:
        ///   if (!IsUserInRole("Admin"))
        ///       return ForbiddenError("Chỉ Admin mới được truy cập");
        /// </summary>
        protected bool IsUserInRole(string role)
        {
            return User.IsInRole(role);
        }

        // ========== HELPER METHODS (PRIVATE) ==========

        /// <summary>
        /// GetErrorCodeFromStatus - Chuyển HTTP Status Code thành Error Code string
        /// 
        /// Ví dụ:
        ///   400 → "BAD_REQUEST"
        ///   404 → "NOT_FOUND"
        ///   500 → "INTERNAL_SERVER_ERROR"
        /// </summary>
        private static string GetErrorCodeFromStatus(int statusCode)
        {
            return statusCode switch
            {
                StatusCodes.Status400BadRequest => "BAD_REQUEST",
                StatusCodes.Status401Unauthorized => "UNAUTHORIZED",
                StatusCodes.Status403Forbidden => "FORBIDDEN",
                StatusCodes.Status404NotFound => "NOT_FOUND",
                StatusCodes.Status409Conflict => "CONFLICT",
                StatusCodes.Status422UnprocessableEntity => "VALIDATION_ERROR",
                StatusCodes.Status500InternalServerError => "INTERNAL_SERVER_ERROR",
                _ => $"ERROR_{statusCode}"
            };
        }
    }
}
