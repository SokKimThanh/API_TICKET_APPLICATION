using Microsoft.AspNetCore.Mvc;
using API_TICKET_APPLICATION.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace API_TICKET_APPLICATION.Controllers
{
    [Authorize]
    public class BookingsController : AppBaseController
    {
        public BookingsController(AppDbContext context) : base(context)
        {
        }

        /// <summary>
        /// Lấy danh sách đặt vé (User thấy của mình, Admin thấy tất cả)
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ResponseModel<PagedData<Booking>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var userId = GetUserId();
                var isAdmin = User.IsInRole("Admin");

                // OPTIMIZATION: Separate base filtering query from Includes.
                // This prevents SQL Server from joining tables (Showtimes, Movies, Tickets) when executing CountAsync().
                var baseQuery = _context.Bookings.AsNoTracking()
                    .Where(b => b.IsDeleted == false);

                if (!isAdmin)
                {
                    baseQuery = baseQuery.Where(b => b.UserId == userId);
                }

                var totalCount = await baseQuery.CountAsync();

                var bookings = await baseQuery
                    .Include(b => b.Showtime)
                        .ThenInclude(s => s.Movie)
                    .Include(b => b.Tickets)
                    .OrderByDescending(b => b.BookingTime)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return OkResponse(new PagedData<Booking>
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    Data = bookings
                }, "Lấy danh sách đặt vé thành công");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return ErrorResponse("Lỗi hệ thống khi lấy danh sách đặt vé", StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Lấy chi tiết một đơn đặt vé
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ResponseModel<Booking>), StatusCodes.Status200OK)]
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

                var booking = await _context.Bookings.AsNoTracking()
                    .Include(b => b.Showtime)
                        .ThenInclude(s => s.Movie)
                    .Include(b => b.Tickets)
                        .ThenInclude(t => t.Seat)
                    .FirstOrDefaultAsync(b => b.Id == id && b.IsDeleted == false);

                if (booking == null) return NotFoundError("Không tìm thấy đơn đặt vé");

                if (!isAdmin && booking.UserId != userId)
                    return ForbiddenError("Bạn không có quyền xem đơn đặt vé này");

                return OkResponse(booking);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return ErrorResponse("Lỗi hệ thống", StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Đặt vé mới
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ResponseModel<Booking>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Create([FromBody] BookingCreateRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var userId = GetUserId();
                if (userId == null) return UnauthorizedError();

                if (request.SeatIds == null || !request.SeatIds.Any())
                    return BadRequestError("Phải chọn ít nhất một ghế");

                // OPTIMIZATION: Use .AsNoTracking() for read-only queries during validation to avoid change tracker overhead.
                var showtime = await _context.Showtimes.AsNoTracking().FirstOrDefaultAsync(s => s.Id == request.ShowtimeId && s.IsDeleted == false);
                if (showtime == null) return BadRequestError("Lịch chiếu không tồn tại");

                if (showtime.StartTime < DateTime.UtcNow)
                    return BadRequestError("Lịch chiếu này đã bắt đầu hoặc đã kết thúc");

                // Kiểm tra ghế có tồn tại trong rạp của lịch chiếu không
                // OPTIMIZATION: Use .AsNoTracking() here as well since the returned Seat list is only used for read-only count checks.
                var seats = await _context.Seats.AsNoTracking().Where(s => request.SeatIds.Contains(s.Id) && s.CinemaHallId == showtime.CinemaHallId && s.IsDeleted == false).ToListAsync();
                if (seats.Count != request.SeatIds.Count)
                    return BadRequestError("Một số ghế không tồn tại hoặc không thuộc phòng chiếu này");

                // Kiểm tra ghế đã được đặt chưa (Double booking check)
                // OPTIMIZATION: Rewrite query to use inner join via navigation property (`t.Booking`) and apply .AsNoTracking().
                // This produces a cleaner SQL query and avoids the expensive correlated subquery while reducing memory tracking overhead.
                var bookedSeatIds = await _context.Tickets
                    .AsNoTracking()
                    .Where(t => t.IsDeleted == false &&
                                t.Booking.ShowtimeId == request.ShowtimeId &&
                                t.Booking.IsDeleted == false &&
                                t.Booking.Status != "Cancelled")
                    .Select(t => t.SeatId)
                    .ToListAsync();

                var overlappingSeats = request.SeatIds.Intersect(bookedSeatIds).ToList();
                if (overlappingSeats.Any())
                    return BadRequestError($"Ghế ID {string.Join(", ", overlappingSeats)} đã được đặt cho suất chiếu này");

                // Tạo Booking
                var booking = new Booking
                {
                    UserId = userId.Value,
                    ShowtimeId = request.ShowtimeId,
                    TotalPrice = showtime.BasePrice * request.SeatIds.Count,
                    Status = "Pending",
                    BookingTime = DateTime.UtcNow,
                    IsDeleted = false
                };

                var validation = BookingValidator.Validate(booking);
                if (!validation.IsValid) return BadRequestError(validation.ErrorMessage!);

                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();

                // Tạo Tickets
                foreach (var seatId in request.SeatIds)
                {
                    var ticket = new Ticket
                    {
                        BookingId = booking.Id,
                        SeatId = seatId,
                        CreatedAt = DateTime.UtcNow,
                        IsDeleted = false
                    };
                    _context.Tickets.Add(ticket);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return CreatedResponse(booking, "Đặt vé thành công", $"/api/bookings/{booking.Id}");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine(ex.ToString());
                return ErrorResponse("Lỗi hệ thống khi đặt vé", StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Cập nhật trạng thái đơn đặt vé (Chỉ Admin)
        /// </summary>
        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ResponseModel<Booking>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] string status)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(status)) return BadRequestError("Trạng thái không được để trống");

                var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == id && b.IsDeleted == false);
                if (booking == null) return NotFoundError("Không tìm thấy đơn đặt vé");

                booking.Status = status;
                booking.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return OkResponse(booking, "Cập nhật trạng thái thành công");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return ErrorResponse("Lỗi hệ thống", StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Hủy đơn đặt vé
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Cancel(int id)
        {
            try
            {
                var userId = GetUserId();
                var isAdmin = User.IsInRole("Admin");

                var booking = await _context.Bookings.Include(b => b.Tickets).FirstOrDefaultAsync(b => b.Id == id && b.IsDeleted == false);
                if (booking == null) return NotFoundError("Không tìm thấy đơn đặt vé");

                if (!isAdmin && booking.UserId != userId)
                    return ForbiddenError("Bạn không có quyền hủy đơn này");

                booking.Status = "Cancelled";
                booking.IsDeleted = true;
                booking.DeletedAt = DateTime.UtcNow;

                foreach (var ticket in booking.Tickets)
                {
                    ticket.IsDeleted = true;
                    ticket.DeletedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                return OkResponse("Hủy đơn đặt vé thành công");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return ErrorResponse("Lỗi hệ thống", StatusCodes.Status500InternalServerError);
            }
        }
    }

    public class BookingCreateRequest
    {
        public int ShowtimeId { get; set; }
        public List<int> SeatIds { get; set; } = new List<int>();
    }
}
