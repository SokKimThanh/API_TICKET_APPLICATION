using Microsoft.AspNetCore.Mvc;
using API_TICKET_APPLICATION.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace API_TICKET_APPLICATION.Controllers
{
    public class SeatsController : AppBaseController
    {
        public SeatsController(AppDbContext context) : base(context)
        {
        }

        /// <summary>
        /// Lấy danh sách ghế (có phân trang, bộ lọc theo rạp)
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ResponseModel<PagedData<Seat>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAll([FromQuery] int? cinemaHallId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                var query = _context.Seats.AsNoTracking().Where(s => s.IsDeleted == false);

                if (cinemaHallId.HasValue)
                    query = query.Where(s => s.CinemaHallId == cinemaHallId.Value);

                var seats = await query
                    .OrderBy(s => s.CinemaHallId).ThenBy(s => s.Row).ThenBy(s => s.Number)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return OkResponse(new PagedData<Seat>
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = await query.CountAsync(),
                    Data = seats
                }, "Lấy danh sách ghế thành công");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Lỗi xảy ra trong hệ thống");
                return ErrorResponse("Lỗi hệ thống khi lấy danh sách ghế", StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Lấy thông tin chi tiết một ghế theo ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ResponseModel<Seat>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var seat = await _context.Seats.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id && s.IsDeleted == false);
                if (seat == null) return NotFoundError($"Không tìm thấy ghế với ID: {id}");
                return OkResponse(seat);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Lỗi xảy ra trong hệ thống");
                return ErrorResponse("Lỗi hệ thống", StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Tạo ghế mới (Chỉ Admin)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ResponseModel<Seat>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Create([FromBody] Seat seat)
        {
            try
            {
                var validation = SeatValidator.Validate(seat);
                if (!validation.IsValid) return BadRequestError(validation.ErrorMessage!);

                if (!await _context.CinemaHalls.AnyAsync(ch => ch.Id == seat.CinemaHallId))
                    return BadRequestError("Phòng chiếu không tồn tại");

                seat.IsDeleted = false;
                seat.CreatedAt = DateTime.UtcNow;

                _context.Seats.Add(seat);
                await _context.SaveChangesAsync();

                return CreatedResponse(seat, "Tạo ghế thành công", $"/api/seats/{seat.Id}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Lỗi xảy ra trong hệ thống");
                return ErrorResponse("Lỗi hệ thống khi tạo ghế", StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Cập nhật toàn bộ thông tin ghế (Chỉ Admin)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ResponseModel<Seat>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Update(int id, [FromBody] Seat seat)
        {
            try
            {
                var validation = SeatValidator.Validate(seat);
                if (!validation.IsValid) return BadRequestError(validation.ErrorMessage!);

                var existingSeat = await _context.Seats.FirstOrDefaultAsync(s => s.Id == id);
                if (existingSeat == null) return NotFoundError("Không tìm thấy ghế");

                existingSeat.CinemaHallId = seat.CinemaHallId;
                existingSeat.Row = seat.Row;
                existingSeat.Number = seat.Number;
                existingSeat.SeatType = seat.SeatType;
                existingSeat.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return OkResponse(existingSeat, "Cập nhật ghế thành công");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Lỗi xảy ra trong hệ thống");
                return ErrorResponse("Lỗi hệ thống khi cập nhật ghế", StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Cập nhật một phần thông tin ghế (PATCH) - Chỉ Admin
        /// </summary>
        [HttpPatch("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ResponseModel<Seat>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PartialUpdate(int id, [FromBody] Dictionary<string, object> updates)
        {
            try
            {
                var seat = await _context.Seats.FirstOrDefaultAsync(s => s.Id == id);
                if (seat == null) return NotFoundError("Không tìm thấy ghế");

                foreach (var update in updates)
                {
                    var validation = SeatValidator.ValidateField(update.Key, update.Value);
                    if (!validation.IsValid) return BadRequestError(validation.ErrorMessage!);

                    switch (update.Key.ToLower())
                    {
                        case "cinemahallid":
                            seat.CinemaHallId = int.Parse(update.Value.ToString()!);
                            break;
                        case "row":
                            seat.Row = update.Value.ToString()!;
                            break;
                        case "number":
                            seat.Number = int.Parse(update.Value.ToString()!);
                            break;
                        case "seattype":
                            seat.SeatType = update.Value.ToString()!;
                            break;
                    }
                }

                seat.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return OkResponse(seat, "Cập nhật một phần ghế thành công");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Lỗi xảy ra trong hệ thống");
                return ErrorResponse("Lỗi hệ thống", StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Xóa ghế (Xóa mềm - Soft Delete) - Chỉ Admin
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var seat = await _context.Seats.FirstOrDefaultAsync(s => s.Id == id && s.IsDeleted == false);
                if (seat == null) return NotFoundError("Ghế không tồn tại hoặc đã xóa");

                seat.IsDeleted = true;
                seat.DeletedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return OkResponse("Xóa ghế thành công (Xóa mềm)");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Lỗi xảy ra trong hệ thống");
                return ErrorResponse("Lỗi hệ thống khi xóa ghế", StatusCodes.Status500InternalServerError);
            }
        }
    }
}
