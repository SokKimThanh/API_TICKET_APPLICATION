using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using API_TICKET_APPLICATION.Models;

namespace API_TICKET_APPLICATION.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)] // Ẩn khỏi tài liệu Swagger để giao diện sạch sẽ
    public class ErrorController : AppBaseController
    {
        // Đẩy context xuống lớp cha (AppBaseController) xử lý
        public ErrorController(AppDbContext context) : base(context)
        {
        }

        /// <summary>
        /// Endpoint xử lý lỗi toàn cục từ Exception Handler Middleware
        /// </summary>
        [Route("/Error")]
        public IActionResult HandleError()
        {
            var context = HttpContext.Features.Get<IExceptionHandlerFeature>();
            if (context != null)
            {
                var exception = context.Error;
                // Ghi log chi tiết lỗi phía server để debug
                Console.WriteLine($"[GLOBAL EXCEPTION] {DateTime.UtcNow}: {exception}");
            }

            // Trả về định dạng JSON lỗi chuẩn hóa bảo mật, tránh rò rỉ Stack Trace / Thông tin nội bộ (Information Leakage)
            return ErrorResponse("Đã xảy ra lỗi hệ thống, vui lòng thử lại sau.", StatusCodes.Status500InternalServerError);
        }
    }
}
