using Microsoft.AspNetCore.Mvc;
using API_TICKET_APPLICATION.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace API_TICKET_APPLICATION.Controllers
{
    public class ShowtimesController : AppBaseController
    {
        public ShowtimesController(AppDbContext context) : base(context)
        {
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int? movieId, [FromQuery] int? cinemaHallId, [FromQuery] DateTime? date, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var query = _context.Showtimes.AsNoTracking()
                    .Include(s => s.Movie)
                    .Include(s => s.CinemaHall)
                    .Where(s => s.IsDeleted == false);

                if (movieId.HasValue)
                    query = query.Where(s => s.MovieId == movieId.Value);

                if (cinemaHallId.HasValue)
                    query = query.Where(s => s.CinemaHallId == cinemaHallId.Value);

                if (date.HasValue)
                {
                    var startDate = date.Value.Date;
                    var endDate = startDate.AddDays(1);
                    query = query.Where(s => s.StartTime >= startDate && s.StartTime < endDate);
                }

                var showtimes = await query
                    .OrderBy(s => s.StartTime)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return OkResponse(new
                {
                    pageNumber,
                    pageSize,
                    totalCount = await query.CountAsync(),
                    data = showtimes
                }, "Lấy danh sách lịch chiếu thành công");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return ErrorResponse("Lỗi hệ thống khi lấy lịch chiếu", StatusCodes.Status500InternalServerError);
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var showtime = await _context.Showtimes.AsNoTracking()
                    .Include(s => s.Movie)
                    .Include(s => s.CinemaHall)
                    .FirstOrDefaultAsync(s => s.Id == id && s.IsDeleted == false);

                if (showtime == null) return NotFoundError($"Không tìm thấy lịch chiếu với ID: {id}");
                return OkResponse(showtime);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return ErrorResponse("Lỗi hệ thống", StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] Showtime showtime)
        {
            try
            {
                var validation = ShowtimeValidator.Validate(showtime);
                if (!validation.IsValid) return BadRequestError(validation.ErrorMessage!);

                // Kiểm tra sự tồn tại của Movie và CinemaHall
                if (!await _context.Movies.AnyAsync(m => m.Id == showtime.MovieId && m.IsDeleted == false))
                    return BadRequestError("Phim không tồn tại hoặc đã bị gỡ");

                if (!await _context.CinemaHalls.AnyAsync(ch => ch.Id == showtime.CinemaHallId && ch.IsDeleted == false))
                    return BadRequestError("Phòng chiếu không tồn tại");

                // Kiểm tra chồng chéo lịch chiếu (Overlap)
                var hasOverlap = await _context.Showtimes.AnyAsync(s =>
                    s.IsDeleted == false &&
                    s.CinemaHallId == showtime.CinemaHallId &&
                    ((showtime.StartTime >= s.StartTime && showtime.StartTime < s.EndTime) ||
                     (showtime.EndTime > s.StartTime && showtime.EndTime <= s.EndTime) ||
                     (showtime.StartTime <= s.StartTime && showtime.EndTime >= s.EndTime)));

                if (hasOverlap)
                    return BadRequestError("Lịch chiếu bị trùng lặp với một lịch chiếu khác trong cùng phòng");

                showtime.IsDeleted = false;
                showtime.CreatedAt = DateTime.UtcNow;

                _context.Showtimes.Add(showtime);
                await _context.SaveChangesAsync();

                return CreatedResponse(showtime, "Tạo lịch chiếu thành công", $"/api/showtimes/{showtime.Id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return ErrorResponse("Lỗi hệ thống khi tạo lịch chiếu", StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] Showtime showtime)
        {
            try
            {
                var validation = ShowtimeValidator.Validate(showtime);
                if (!validation.IsValid) return BadRequestError(validation.ErrorMessage!);

                // Kiểm tra sự tồn tại của Movie và CinemaHall
                if (!await _context.Movies.AnyAsync(m => m.Id == showtime.MovieId && m.IsDeleted == false))
                    return BadRequestError("Phim không tồn tại hoặc đã bị gỡ");

                if (!await _context.CinemaHalls.AnyAsync(ch => ch.Id == showtime.CinemaHallId && ch.IsDeleted == false))
                    return BadRequestError("Phòng chiếu không tồn tại");

                var existingShowtime = await _context.Showtimes.FirstOrDefaultAsync(s => s.Id == id);
                if (existingShowtime == null) return NotFoundError("Không tìm thấy lịch chiếu");

                // Kiểm tra chồng chéo (trừ chính nó)
                var hasOverlap = await _context.Showtimes.AnyAsync(s =>
                    s.Id != id &&
                    s.IsDeleted == false &&
                    s.CinemaHallId == showtime.CinemaHallId &&
                    ((showtime.StartTime >= s.StartTime && showtime.StartTime < s.EndTime) ||
                     (showtime.EndTime > s.StartTime && showtime.EndTime <= s.EndTime) ||
                     (showtime.StartTime <= s.StartTime && showtime.EndTime >= s.EndTime)));

                if (hasOverlap)
                    return BadRequestError("Cập nhật thất bại: Lịch chiếu bị trùng lặp với lịch khác");

                existingShowtime.MovieId = showtime.MovieId;
                existingShowtime.CinemaHallId = showtime.CinemaHallId;
                existingShowtime.StartTime = showtime.StartTime;
                existingShowtime.EndTime = showtime.EndTime;
                existingShowtime.BasePrice = showtime.BasePrice;
                existingShowtime.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return OkResponse(existingShowtime, "Cập nhật lịch chiếu thành công");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return ErrorResponse("Lỗi hệ thống khi cập nhật lịch chiếu", StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPatch("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PartialUpdate(int id, [FromBody] Dictionary<string, object> updates)
        {
            try
            {
                var showtime = await _context.Showtimes.FirstOrDefaultAsync(s => s.Id == id);
                if (showtime == null) return NotFoundError("Không tìm thấy lịch chiếu");

                foreach (var update in updates)
                {
                    var validation = ShowtimeValidator.ValidateField(update.Key, update.Value);
                    if (!validation.IsValid) return BadRequestError(validation.ErrorMessage!);

                    switch (update.Key.ToLower())
                    {
                        case "movieid":
                            showtime.MovieId = int.Parse(update.Value.ToString()!);
                            break;
                        case "cinemahallid":
                            showtime.CinemaHallId = int.Parse(update.Value.ToString()!);
                            break;
                        case "starttime":
                            showtime.StartTime = DateTime.Parse(update.Value.ToString()!);
                            break;
                        case "endtime":
                            showtime.EndTime = DateTime.Parse(update.Value.ToString()!);
                            break;
                        case "baseprice":
                            showtime.BasePrice = decimal.Parse(update.Value.ToString()!);
                            break;
                    }
                }

                // Sau khi PATCH, kiểm tra lại tính hợp lệ logic thời gian và chồng chéo
                if (showtime.StartTime >= showtime.EndTime)
                    return BadRequestError("Lỗi logic: Thời gian bắt đầu phải trước thời gian kết thúc");

                var hasOverlap = await _context.Showtimes.AnyAsync(s =>
                    s.Id != id &&
                    s.IsDeleted == false &&
                    s.CinemaHallId == showtime.CinemaHallId &&
                    ((showtime.StartTime >= s.StartTime && showtime.StartTime < s.EndTime) ||
                     (showtime.EndTime > s.StartTime && showtime.EndTime <= s.EndTime) ||
                     (showtime.StartTime <= s.StartTime && showtime.EndTime >= s.EndTime)));

                if (hasOverlap)
                    return BadRequestError("PATCH thất bại: Lịch chiếu bị trùng lặp");

                showtime.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return OkResponse(showtime, "Cập nhật một phần lịch chiếu thành công");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return ErrorResponse("Lỗi hệ thống", StatusCodes.Status500InternalServerError);
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var showtime = await _context.Showtimes.FirstOrDefaultAsync(s => s.Id == id && s.IsDeleted == false);
                if (showtime == null) return NotFoundError("Lịch chiếu không tồn tại hoặc đã xóa");

                showtime.IsDeleted = true;
                showtime.DeletedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return OkResponse("Xóa lịch chiếu thành công (Xóa mềm)");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return ErrorResponse("Lỗi hệ thống khi xóa lịch chiếu", StatusCodes.Status500InternalServerError);
            }
        }
    }
}
