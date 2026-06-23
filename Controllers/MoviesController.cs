using Microsoft.AspNetCore.Mvc;
using API_TICKET_APPLICATION.Models;
using Microsoft.EntityFrameworkCore;

namespace API_TICKET_APPLICATION.Controllers
{
    // KẾ THỪA TỪ QUẢN GIA: Không cần khai báo lại _context
    public class MoviesController : AppBaseController
    {
        // Đẩy context xuống lớp cha (AppBaseController) xử lý
        public MoviesController(AppDbContext context) : base(context) { }

        // ========== GET ENDPOINTS ==========

        /// <summary>
        /// Lấy danh sách phim (Có phân trang, bỏ qua phim đã xóa)
        /// </summary>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                // LOGIC SOFT DELETE: Chỉ lấy những phim chưa bị xóa
                var query = _context.Movies.Where(m => m.IsDeleted == false);

                var movies = await query
                    .OrderBy(m => m.Title)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return OkResponse(new
                {
                    pageNumber,
                    pageSize,
                    totalCount = await query.CountAsync(), // Đếm tổng số phim KHẢ DỤNG
                    data = movies
                }, $"Lấy {movies.Count} phim thành công");
            }
            catch (Exception ex)
            {
                return ErrorResponse($"Lỗi hệ thống: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Lấy thông tin chi tiết một bộ phim theo ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Movie), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                if (id <= 0) return BadRequestError("ID phim không hợp lệ");

                // LOGIC SOFT DELETE: Tìm ID và phải đảm bảo phim chưa bị xóa
                var movie = await _context.Movies.FirstOrDefaultAsync(m => m.Id == id && m.IsDeleted == false);

                if (movie == null)
                    return NotFoundError($"Không tìm thấy phim đang chiếu với ID: {id}");

                return OkResponse(movie, $"Lấy phim '{movie.Title}' thành công");
            }
            catch (Exception ex)
            {
                return ErrorResponse($"Lỗi hệ thống: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        // ========== POST ENDPOINT ==========

        /// <summary>
        /// Thêm mới một bộ phim vào hệ thống
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(Movie), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Create([FromBody] Movie movie)
        {
            try
            {
                if (movie == null) return BadRequestError("Dữ liệu trống");
                if (string.IsNullOrWhiteSpace(movie.Title)) return BadRequestError("Tên phim không được để trống");

                // Gán giá trị mặc định cho cột Audit & Soft Delete
                movie.IsDeleted = false;

                _context.Movies.Add(movie);
                await _context.SaveChangesAsync();

                return CreatedResponse(movie, $"Tạo phim '{movie.Title}' thành công", $"/api/movies/{movie.Id}");
            }
            catch (Exception ex)
            {
                return ErrorResponse($"Lỗi tạo phim: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        // ========== PATCH ENDPOINT ==========

        /// <summary>
        /// PATCH /api/movies/{id}
        /// Cập nhật một số trường của phim (PATCH request)
        /// 
        /// Request Body (chỉ cần gửi các trường cần update):
        /// {
        ///   "genre": "Comedy",
        ///   "durationInMinutes": 165
        /// }
        /// 
        /// Trả về:
        ///   200 OK: { success: true, data: { id: 1, title: "...", genre: "Comedy", ... } }
        ///   404 Not Found: { success: false, message: "Không tìm thấy phim" }
        /// </summary>
        [HttpPatch("{id}")]
        [ProducesResponseType(typeof(Movie), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> PartialUpdate(int id, [FromBody] Dictionary<string, object> updates)
        {
            try
            {
                // Validation
                if (id <= 0)
                    return BadRequestError("ID phim không hợp lệ");

                if (updates == null || updates.Count == 0)
                    return BadRequestError("Phải cung cấp ít nhất một trường để cập nhật");

                // Tìm phim
                var movie = await _context.Movies.FirstOrDefaultAsync(m => m.Id == id);
                if (movie == null)
                {
                    Console.WriteLine($"[CRUD] PATCH /api/movies/{id} - Not found (404)");
                    return NotFoundError($"Không tìm thấy phim với ID: {id}");
                }

                // Cập nhật từng trường
                foreach (var update in updates)
                {
                    switch (update.Key.ToLower())
                    {
                        case "title":
                            movie.Title = update.Value?.ToString() ?? movie.Title;
                            break;
                        case "description":
                            movie.Description = update.Value?.ToString();
                            break;
                        case "genre":
                            movie.Genre = update.Value?.ToString() ?? movie.Genre;
                            break;
                        case "durationinminutes":
                            if (int.TryParse(update.Value?.ToString(), out var duration))
                                movie.DurationInMinutes = duration;
                            break;
                        case "posterurl":
                            movie.PosterUrl = update.Value?.ToString();
                            break;
                        case "releasedate":
                            if (DateOnly.TryParse(update.Value?.ToString(), out var releaseDate))
                                movie.ReleaseDate = releaseDate;
                            break;
                    }
                }

                _context.Movies.Update(movie);
                await _context.SaveChangesAsync();

                Console.WriteLine($"[CRUD] PATCH /api/movies/{id} - Partially updated: {movie.Title}");

                return OkResponse(movie, $"Cập nhật một phần phim '{movie.Title}' thành công");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CRUD] PATCH /api/movies/{id} - Error: {ex.Message}");
                return ErrorResponse($"Lỗi khi cập nhật phim: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }
        // ========== DELETE ENDPOINT ==========

        /// <summary>
        /// Xóa bộ phim khỏi hệ thống (Xóa mềm - Soft Delete)
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                if (id <= 0) return BadRequestError("ID phim không hợp lệ");

                // Tìm phim (Chỉ tìm phim chưa xóa)
                var movie = await _context.Movies.FirstOrDefaultAsync(m => m.Id == id && m.IsDeleted == false);

                if (movie == null)
                    return NotFoundError($"Không tìm thấy phim hoặc phim đã bị xóa trước đó (ID: {id})");

                // ✅ THỰC HIỆN SOFT DELETE: Chỉ bật cờ IsDeleted
                movie.IsDeleted = true;

                _context.Movies.Update(movie);
                await _context.SaveChangesAsync();

                return OkResponse($"Đã gỡ bỏ phim '{movie.Title}' khỏi hệ thống (Xóa mềm) thành công");
            }
            catch (Exception ex)
            {
                return ErrorResponse($"Lỗi khi xóa phim: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }
    }
}