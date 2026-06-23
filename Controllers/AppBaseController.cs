using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using API_TICKET_APPLICATION.Models;

namespace API_TICKET_APPLICATION.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public abstract class AppBaseController : ControllerBase
    {
        // KẾT NỐI DATABASE DÙNG CHUNG CHO MỌI CONTROLLER CON
        protected readonly AppDbContext _context;

        protected AppBaseController(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // =========================================================================
        // BLUEPRINT: KHUÔN MẪU CHUẨN HÓA DỮ LIỆU TRẢ VỀ JSON CHO FRONT-END
        // =========================================================================

        // 1. Trả về thành công (HTTP 200 OK)
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

        protected OkObjectResult OkResponse(string message = "Thành công")
        {
            return OkResponse(data: null, message: message);
        }

        // 2. Trả về khi tạo mới thành công (HTTP 201 Created)
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

        // 3. Khuôn mẫu chung cho các lỗi (HTTP 4xx, 5xx)
        protected ObjectResult ErrorResponse(string errorMessage, int statusCode = StatusCodes.Status400BadRequest, string? errorCode = null)
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

        // Các hàm tiện ích gọi lỗi nhanh
        protected ObjectResult NotFoundError(string message = "Không tìm thấy")
            => ErrorResponse(message, StatusCodes.Status404NotFound, "NOT_FOUND");

        protected ObjectResult BadRequestError(string message = "Yêu cầu không hợp lệ")
            => ErrorResponse(message, StatusCodes.Status400BadRequest, "BAD_REQUEST");

        protected ObjectResult UnauthorizedError(string message = "Không được phép")
            => ErrorResponse(message, StatusCodes.Status401Unauthorized, "UNAUTHORIZED");

        protected ObjectResult ForbiddenError(string message = "Bị cấm truy cập")
            => ErrorResponse(message, StatusCodes.Status403Forbidden, "FORBIDDEN");

        // =========================================================================
        // TIỆN ÍCH XỬ LÝ TOKEN (JWT)
        // =========================================================================

        protected int? GetUserId()
        {
            var userIdClaim = User.FindFirst("UserId") ?? User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId)) return userId;
            return null;
        }

        private static string GetErrorCodeFromStatus(int statusCode)
        {
            return statusCode switch
            {
                StatusCodes.Status400BadRequest => "BAD_REQUEST",
                StatusCodes.Status401Unauthorized => "UNAUTHORIZED",
                StatusCodes.Status404NotFound => "NOT_FOUND",
                StatusCodes.Status500InternalServerError => "INTERNAL_SERVER_ERROR",
                _ => $"ERROR_{statusCode}"
            };
        }
    }
}