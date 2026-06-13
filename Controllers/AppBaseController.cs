using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using API_TICKET_APPLICATION.Models;

namespace API_TICKET_APPLICATION.Controllers
{ 
    [ApiController]
    [Route("api/[controller]")]
    public abstract class AppBaseController : ControllerBase
    {
        
        protected readonly AppDbContext _context;
         
        protected AppBaseController(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }
         
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

       
        protected ObjectResult NotFoundError(string message = "Không tìm thấy")
        {
            return ErrorResponse(message, StatusCodes.Status404NotFound, "NOT_FOUND");
        }
         
        protected ObjectResult BadRequestError(string message = "Yêu cầu không hợp lệ")
        {
            return ErrorResponse(message, StatusCodes.Status400BadRequest, "BAD_REQUEST");
        }
         
        protected ObjectResult UnauthorizedError(string message = "Không được phép")
        {
            return ErrorResponse(message, StatusCodes.Status401Unauthorized, "UNAUTHORIZED");
        }
         
        protected ObjectResult ForbiddenError(string message = "Bị cấm truy cập")
        {
            return ErrorResponse(message, StatusCodes.Status403Forbidden, "FORBIDDEN");
        }
         
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
         
        protected string? GetUserEmail()
        {
            return User.FindFirst(ClaimTypes.Email)?.Value;
        }
         
        protected string? GetUserName()
        {
            return User.FindFirst(ClaimTypes.Name)?.Value;
        }
         
        protected bool IsUserAuthenticated()
        {
            return User?.Identity?.IsAuthenticated ?? false;
        }
         
        protected bool IsUserInRole(string role)
        {
            return User.IsInRole(role);
        }
         
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
