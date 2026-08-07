using Microsoft.AspNetCore.Mvc;
using API_TICKET_APPLICATION.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace API_TICKET_APPLICATION.Controllers
{
    // KẾ THỪA TỪ QUẢN GIA: Không cần khai báo lại _context
    public class MoviesController : AppBaseController
    {
        // Đẩy context xuống lớp cha (AppBaseController) xử lý
        public MoviesController(AppDbContext context) : base(context)
        {
        }

        // ========== GET ENDPOINTS ==========

        /// <summary>
        /// Lấy danh sách phim (Có phân trang, bỏ qua phim đã xóa)
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ResponseModel<PagedData<Movie>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                // LOGIC SOFT DELETE: Chỉ lấy những phim chưa bị xóa
                // OPTIMIZATION: .AsNoTracking() reduces overhead for read-only operations
                var query = _context.Movies.AsNoTracking().Where(m => m.IsDeleted == false);

                var movies = await query
                    .OrderBy(m => m.Title)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return OkResponse(new PagedData<Movie>
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = await query.CountAsync(),
                    Data = movies
                }, $"Lấy {movies.Count} phim thành công");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Lỗi xảy ra trong hệ thống");
                return ErrorResponse("Đã có lỗi hệ thống xảy ra. Vui lòng liên hệ quản trị viên.", StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Lấy thông tin chi tiết một bộ phim theo ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ResponseModel<Movie>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                if (id <= 0) return BadRequestError("ID phim không hợp lệ");

                // LOGIC SOFT DELETE: Tìm ID và phải đảm bảo phim chưa bị xóa
                // OPTIMIZATION: .AsNoTracking() reduces overhead for read-only operations
                var movie = await _context.Movies.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id && m.IsDeleted == false);

                if (movie == null)
                    return NotFoundError($"Không tìm thấy phim đang chiếu với ID: {id}");

                return OkResponse(movie, $"Lấy phim '{movie.Title}' thành công");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Lỗi xảy ra trong hệ thống");
                return ErrorResponse("Đã có lỗi hệ thống xảy ra. Vui lòng liên hệ quản trị viên.", StatusCodes.Status500InternalServerError);
            }
        }

        // ========== POST ENDPOINT ==========

        /// <summary>
        /// Thêm mới một bộ phim vào hệ thống
        /// Áp dụng MovieValidator để kiểm tra dữ liệu đầu vào
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ResponseModel<Movie>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Create([FromBody] Movie movie)
        {
            try
            {
                // VALIDATION: Sử dụng MovieValidator
                var validation = MovieValidator.Validate(movie);
                if (!validation.IsValid)
                    return BadRequestError(validation.ErrorMessage ?? "Dữ liệu phim không hợp lệ");

                // ========== GÁN GIÁ TRỊ MẶC ĐỊNH CHO AUDIT & SOFT DELETE ==========
                movie.IsDeleted = false;
                movie.CreatedAt = DateTime.UtcNow;

                // ========== LƯU VÀO DATABASE ==========
                _context.Movies.Add(movie);
                await _context.SaveChangesAsync();

                Logger.LogInformation("[CRUD] POST /api/movies - Created: {Title} (ID: {Id})", movie.Title, movie.Id);

                return CreatedResponse(movie, $"Tạo phim '{movie.Title}' thành công", $"/api/movies/{movie.Id}");
            }
            catch (DbUpdateException dbEx)
            {
                Logger.LogError(dbEx, "[DATABASE ERROR] Create Movie: {Message}", dbEx.Message);
                return ErrorResponse("Lỗi khi lưu dữ liệu vào database. Vui lòng kiểm tra dữ liệu đầu vào.", StatusCodes.Status500InternalServerError);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[SYSTEM ERROR] Create Movie: {Message}", ex.Message);
                return ErrorResponse("Đã có lỗi hệ thống xảy ra. Vui lòng liên hệ quản trị viên.", StatusCodes.Status500InternalServerError);
            }
        }

        // ========== PUT ENDPOINT ==========

        /// <summary>
        /// Cập nhật toàn bộ thông tin phim
        /// Áp dụng MovieValidator để kiểm tra dữ liệu đầu vào
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ResponseModel<Movie>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Update(int id, [FromBody] Movie movie)
        {
            try
            {
                // ========== KIỂM TRA ID HỢP LỆ ==========
                if (id <= 0)
                    return BadRequestError("ID phim không hợp lệ");

                if (movie == null)
                    return BadRequestError("Dữ liệu trống");

                // ========== KIỂM TRA VALIDATION BẰNG STATIC VALIDATOR ==========
                var validation = MovieValidator.Validate(movie);

                if (!validation.IsValid)
                {
                    Logger.LogWarning("[VALIDATION ERROR] Update Movie {Id}: {ErrorMessage}", id, validation.ErrorMessage);
                    return BadRequestError(validation.ErrorMessage ?? "Dữ liệu phim không hợp lệ");
                }

                // ========== TÌM PHIM HIỆN CÓ ==========
                var existingMovie = await _context.Movies.FirstOrDefaultAsync(m => m.Id == id);
                if (existingMovie == null)
                    return NotFoundError($"Không tìm thấy phim với ID: {id}");

                // ========== CẬP NHẬT TẤT CẢ CÁC TRƯỜNG ==========
                existingMovie.Title = movie.Title;
                existingMovie.Description = movie.Description;
                existingMovie.Genre = movie.Genre;
                existingMovie.DurationInMinutes = movie.DurationInMinutes;
                existingMovie.PosterUrl = movie.PosterUrl;
                existingMovie.ReleaseDate = movie.ReleaseDate;
                existingMovie.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                Logger.LogInformation("[CRUD] PUT /api/movies/{Id} - Updated: {Title}", id, existingMovie.Title);

                return OkResponse(existingMovie, $"Cập nhật phim '{existingMovie.Title}' thành công");
            }
            catch (DbUpdateException dbEx)
            {
                Logger.LogError(dbEx, "[DATABASE ERROR] Update Movie {Id}: {Message}", id, dbEx.Message);
                return ErrorResponse("Lỗi khi cập nhật dữ liệu. Vui lòng thử lại.", StatusCodes.Status500InternalServerError);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[SYSTEM ERROR] Update Movie {Id}: {Message}", id, ex.Message);
                return ErrorResponse("Đã có lỗi hệ thống xảy ra. Vui lòng liên hệ quản trị viên.", StatusCodes.Status500InternalServerError);
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
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ResponseModel<Movie>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status500InternalServerError)]
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
                    Logger.LogInformation("[CRUD] PATCH /api/movies/{Id} - Not found (404)", id);
                    return NotFoundError($"Không tìm thấy phim với ID: {id}");
                }

                // Cập nhật từng trường
                foreach (var update in updates)
                {
                    // VALIDATION: Kiểm tra từng trường trước khi gán
                    var validation = MovieValidator.ValidateField(update.Key, update.Value);
                    if (!validation.IsValid)
                        return BadRequestError(validation.ErrorMessage ?? "Dữ liệu không hợp lệ");

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

                // OPTIMIZATION: Removed redundant _context.Movies.Update(movie).
                // EF Core change tracker automatically detects changed properties and only updates those columns in SQL.
                await _context.SaveChangesAsync();

                Logger.LogInformation("[CRUD] PATCH /api/movies/{Id} - Partially updated: {Title}", id, movie.Title);

                return OkResponse(movie, $"Cập nhật một phần phim '{movie.Title}' thành công");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Lỗi xảy ra trong hệ thống");
                return ErrorResponse("Đã có lỗi hệ thống xảy ra. Vui lòng liên hệ quản trị viên.", StatusCodes.Status500InternalServerError);
            }
        }
        // ========== DELETE ENDPOINT ==========

        /// <summary>
        /// Xóa bộ phim khỏi hệ thống (Xóa mềm - Soft Delete)
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
                if (id <= 0) return BadRequestError("ID phim không hợp lệ");

                // Tìm phim (Chỉ tìm phim chưa xóa)
                var movie = await _context.Movies.FirstOrDefaultAsync(m => m.Id == id && m.IsDeleted == false);

                if (movie == null)
                    return NotFoundError($"Không tìm thấy phim hoặc phim đã bị xóa trước đó (ID: {id})");

                // ✅ THỰC HIỆN SOFT DELETE: Chỉ bật cờ IsDeleted
                movie.IsDeleted = true;

                // OPTIMIZATION: Removed redundant _context.Movies.Update(movie).
                // EF Core change tracker will only update the IsDeleted column.
                await _context.SaveChangesAsync();

                return OkResponse($"Đã gỡ bỏ phim '{movie.Title}' khỏi hệ thống (Xóa mềm) thành công");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Lỗi xảy ra trong hệ thống");
                return ErrorResponse("Đã có lỗi hệ thống xảy ra. Vui lòng liên hệ quản trị viên.", StatusCodes.Status500InternalServerError);
            }
        }
    }
}