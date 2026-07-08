using Microsoft.AspNetCore.Mvc;
using API_TICKET_APPLICATION.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace API_TICKET_APPLICATION.Controllers
{
    [Authorize]
    public class TicketsController : AppBaseController
    {
        public TicketsController(AppDbContext context) : base(context)
        {
        }

        /// <summary>
        /// Lấy danh sách vé (User thấy vé của mình, Admin thấy tất cả)
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ResponseModel<PagedData<Ticket>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAll([FromQuery] int? bookingId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var userId = GetUserId();
                var isAdmin = User.IsInRole("Admin");

                var query = _context.Tickets.AsNoTracking()
                    .Include(t => t.Booking)
                    .Include(t => t.Seat)
                    .Where(t => t.IsDeleted == false);

                if (bookingId.HasValue)
                    query = query.Where(t => t.BookingId == bookingId.Value);

                if (!isAdmin)
                {
                    query = query.Where(t => t.Booking.UserId == userId);
                }

                var totalCount = await query.CountAsync();
                var tickets = await query
                    .OrderByDescending(t => t.CreatedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return OkResponse(new PagedData<Ticket>
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    Data = tickets
                }, "Lấy danh sách vé thành công");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return ErrorResponse("Lỗi hệ thống", StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Lấy chi tiết một vé
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ResponseModel<Ticket>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var userId = GetUserId();
                var isAdmin = User.IsInRole("Admin");

                var ticket = await _context.Tickets.AsNoTracking()
                    .Include(t => t.Booking)
                    .Include(t => t.Seat)
                    .FirstOrDefaultAsync(t => t.Id == id && t.IsDeleted == false);

                if (ticket == null) return NotFoundError("Không tìm thấy vé");

                if (!isAdmin && ticket.Booking.UserId != userId)
                    return ForbiddenError("Bạn không có quyền xem vé này");

                return OkResponse(ticket);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return ErrorResponse("Lỗi hệ thống", StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Xóa vé (Chỉ Admin) - Thường vé được xóa/hủy theo đơn đặt vé
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var ticket = await _context.Tickets.FirstOrDefaultAsync(t => t.Id == id && t.IsDeleted == false);
                if (ticket == null) return NotFoundError("Không tìm thấy vé");

                ticket.IsDeleted = true;
                ticket.DeletedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return OkResponse("Xóa vé thành công");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return ErrorResponse("Lỗi hệ thống", StatusCodes.Status500InternalServerError);
            }
        }
    }
}
