using Microsoft.AspNetCore.Mvc;
using API_TICKET_APPLICATION.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace API_TICKET_APPLICATION.Controllers
{
    /// <summary>
    /// MoviesController - API Controller quản lý phim
    /// 
    /// Kế thừa từ AppBaseController để:
    ///   - Sử dụng _context từ base class
    ///   - Sử dụng OkResponse, ErrorResponse cho response thống nhất
    ///   - Sử dụng GetUserId(), GetUserEmail(), v.v. cho JWT
    ///   - Giảm code lặp lại
    /// </summary>
    public class MoviesController : AppBaseController
    {
        // ========== CONSTRUCTOR ==========
        /// <summary>
        /// Constructor của MoviesController
        /// 
        /// Inject AppDbContext và truyền lên Base Class thông qua base(context)
        /// </summary>
        public MoviesController(AppDbContext context) : base(context)
        {
        }

        // ========== GET ENDPOINTS ==========

        /// <summary>
        /// GET /api/movies
        /// Lấy tất cả phim
        /// 
        /// Query Parameters (Optional):
        ///   - pageNumber: Số trang (default: 1)
        ///   - pageSize: Số phim trên một trang (default: 10)
        /// 
        /// Ví dụ:
        ///   GET /api/movies
        ///   GET /api/movies?pageNumber=2&pageSize=20
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<Movie>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                // Validation
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                // Lấy dữ liệu từ Stored Procedure (hoặc LINQ)
                var movies = await _context.Movies
                    .OrderBy(m => m.Title)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                Console.WriteLine($"[CRUD] GET /api/movies (Page {pageNumber}, Size {pageSize}) - Retrieved {movies.Count} movies");

                return OkResponse(new
                {
                    pageNumber,
                    pageSize,
                    totalCount = await _context.Movies.CountAsync(),
                    data = movies
                }, $"Lấy {movies.Count} phim thành công");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CRUD] GET /api/movies - Error: {ex.Message}");
                return ErrorResponse($"Lỗi khi lấy dữ liệu phim: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// GET /api/movies/{id}
        /// Lấy phim theo ID
        /// 
        /// Ví dụ:
        ///   GET /api/movies/1
        /// 
        /// Trả về:
        ///   200 OK: { success: true, data: { id: 1, title: "...", ... } }
        ///   404 Not Found: { success: false, message: "Không tìm thấy phim" }
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Movie), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                // Validation
                if (id <= 0)
                    return BadRequestError("ID phim không hợp lệ");

                // Lấy phim từ database
                var movie = await _context.Movies.FirstOrDefaultAsync(m => m.Id == id);

                // Nếu không tìm thấy → trả 404 với OkResponse wrapper
                if (movie == null)
                {
                    Console.WriteLine($"[CRUD] GET /api/movies/{id} - Not found (404)");
                    return NotFoundError($"Không tìm thấy phim với ID: {id}");
                }

                Console.WriteLine($"[CRUD] GET /api/movies/{id} - Retrieved: {movie.Title}");

                // Trả về 200 với dữ liệu
                return OkResponse(movie, $"Lấy phim '{movie.Title}' thành công");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CRUD] GET /api/movies/{id} - Error: {ex.Message}");
                return ErrorResponse($"Lỗi khi lấy phim: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        // ========== POST ENDPOINT ==========

        /// <summary>
        /// POST /api/movies
        /// Tạo phim mới
        /// 
        /// Request Body:
        /// {
        ///   "title": "Inception",
        ///   "description": "A thief who steals corporate secrets...",
        ///   "genre": "Sci-Fi",
        ///   "durationInMinutes": 148,
        ///   "posterUrl": "https://example.com/inception.jpg",
        ///   "releaseDate": "2010-07-16"
        /// }
        /// 
        /// Trả về:
        ///   201 Created: { success: true, data: { id: 3, title: "Inception", ... } }
        ///   400 Bad Request: { success: false, message: "Title không được để trống" }
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(Movie), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] Movie movie)
        {
            try
            {
                // Validation
                if (movie == null)
                    return BadRequestError("Dữ liệu phim không được để trống");

                if (string.IsNullOrWhiteSpace(movie.Title))
                    return BadRequestError("Tên phim (Title) không được để trống");

                if (movie.DurationInMinutes <= 0)
                    return BadRequestError("Thời lượng phim phải lớn hơn 0");

                // Thêm vào database
                _context.Movies.Add(movie);
                await _context.SaveChangesAsync();

                Console.WriteLine($"[CRUD] POST /api/movies - Created: {movie.Title} (ID: {movie.Id})");

                // Trả về 201 Created
                return CreatedResponse(movie, $"Tạo phim '{movie.Title}' thành công", $"/api/movies/{movie.Id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CRUD] POST /api/movies - Error: {ex.Message}");
                return ErrorResponse($"Lỗi khi tạo phim: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }

        // ========== PUT ENDPOINT ==========

        /// <summary>
        /// PUT /api/movies/{id}
        /// Cập nhật toàn bộ phim (PUT request)
        /// 
        /// Request Body:
        /// {
        ///   "title": "Updated Title",
        ///   "description": "Updated description",
        ///   "genre": "Drama",
        ///   "durationInMinutes": 150,
        ///   "posterUrl": "https://example.com/new-poster.jpg",
        ///   "releaseDate": "1994-09-23"
        /// }
        /// 
        /// Trả về:
        ///   200 OK: { success: true, data: { id: 1, title: "Updated Title", ... } }
        ///   404 Not Found: { success: false, message: "Không tìm thấy phim" }
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(Movie), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(int id, [FromBody] Movie movie)
        {
            try
            {
                // Validation
                if (movie == null)
                    return BadRequestError("Dữ liệu phim không được để trống");

                if (id <= 0)
                    return BadRequestError("ID phim không hợp lệ");

                // Tìm phim cũ
                var existingMovie = await _context.Movies.FirstOrDefaultAsync(m => m.Id == id);
                if (existingMovie == null)
                {
                    Console.WriteLine($"[CRUD] PUT /api/movies/{id} - Not found (404)");
                    return NotFoundError($"Không tìm thấy phim với ID: {id}");
                }

                // Cập nhật dữ liệu
                existingMovie.Title = movie.Title ?? existingMovie.Title;
                existingMovie.Description = movie.Description ?? existingMovie.Description;
                existingMovie.Genre = movie.Genre ?? existingMovie.Genre;
                existingMovie.DurationInMinutes = movie.DurationInMinutes > 0 ? movie.DurationInMinutes : existingMovie.DurationInMinutes;
                existingMovie.PosterUrl = movie.PosterUrl ?? existingMovie.PosterUrl;
                existingMovie.ReleaseDate = movie.ReleaseDate != default ? movie.ReleaseDate : existingMovie.ReleaseDate;

                _context.Movies.Update(existingMovie);
                await _context.SaveChangesAsync();

                Console.WriteLine($"[CRUD] PUT /api/movies/{id} - Updated: {existingMovie.Title}");

                return OkResponse(existingMovie, $"Cập nhật phim '{existingMovie.Title}' thành công");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CRUD] PUT /api/movies/{id} - Error: {ex.Message}");
                return ErrorResponse($"Lỗi khi cập nhật phim: {ex.Message}", StatusCodes.Status500InternalServerError);
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
        /// DELETE /api/movies/{id}
        /// Xóa phim
        /// 
        /// Ví dụ:
        ///   DELETE /api/movies/1
        /// 
        /// Trả về:
        ///   200 OK: { success: true, message: "Xóa phim 'The Shawshank Redemption' thành công" }
        ///   404 Not Found: { success: false, message: "Không tìm thấy phim" }
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                // Validation
                if (id <= 0)
                    return BadRequestError("ID phim không hợp lệ");

                // Tìm phim
                var movie = await _context.Movies.FirstOrDefaultAsync(m => m.Id == id);
                if (movie == null)
                {
                    Console.WriteLine($"[CRUD] DELETE /api/movies/{id} - Not found (404)");
                    return NotFoundError($"Không tìm thấy phim với ID: {id}");
                }

                var movieTitle = movie.Title;

                // Xóa phim
                _context.Movies.Remove(movie);
                await _context.SaveChangesAsync();

                Console.WriteLine($"[CRUD] DELETE /api/movies/{id} - Deleted: {movieTitle}");

                return OkResponse($"Xóa phim '{movieTitle}' thành công");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CRUD] DELETE /api/movies/{id} - Error: {ex.Message}");
                return ErrorResponse($"Lỗi khi xóa phim: {ex.Message}", StatusCodes.Status500InternalServerError);
            }
        }
    }
}
