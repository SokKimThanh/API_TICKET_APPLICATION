using Microsoft.AspNetCore.Mvc;
using API_TICKET_APPLICATION.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace API_TICKET_APPLICATION.Controllers
{
    public class CinemaHallsController : AppBaseController
    {
        public CinemaHallsController(AppDbContext context) : base(context)
        {
        }

        // ========== GET ENDPOINTS ==========

        /// <summary>
        /// Lấy danh sách phòng chiếu (Có phân trang, bỏ qua phòng đã xóa)
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ResponseModel<PagedData<CinemaHall>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                // LOGIC SOFT DELETE: Chỉ lấy những phòng chiếu chưa bị xóa
                // OPTIMIZATION: .AsNoTracking() reduces overhead for read-only operations
                var query = _context.CinemaHalls.AsNoTracking().Where(ch => ch.IsDeleted == false);

                var halls = await query
                    .OrderBy(ch => ch.Name)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return OkResponse(new PagedData<CinemaHall>
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = await query.CountAsync(),
                    Data = halls
                }, $"Lấy {halls.Count} phòng chiếu thành công");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Lỗi xảy ra trong hệ thống");
                return ErrorResponse("Đã có lỗi hệ thống xảy ra. Vui lòng liên hệ quản trị viên.", StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Lấy thông tin chi tiết một phòng chiếu theo ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ResponseModel<CinemaHall>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                if (id <= 0) return BadRequestError("ID phòng chiếu không hợp lệ");

                // LOGIC SOFT DELETE: Tìm ID và phải đảm bảo phòng chiếu chưa bị xóa
                // OPTIMIZATION: .AsNoTracking() reduces overhead for read-only operations
                var hall = await _context.CinemaHalls.AsNoTracking().FirstOrDefaultAsync(ch => ch.Id == id && ch.IsDeleted == false);

                if (hall == null)
                    return NotFoundError($"Không tìm thấy phòng chiếu với ID: {id}");

                return OkResponse(hall, $"Lấy phòng chiếu '{hall.Name}' thành công");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Lỗi xảy ra trong hệ thống");
                return ErrorResponse("Đã có lỗi hệ thống xảy ra. Vui lòng liên hệ quản trị viên.", StatusCodes.Status500InternalServerError);
            }
        }

        // ========== POST ENDPOINT ==========

        /// <summary>
        /// Thêm mới một phòng chiếu vào hệ thống (Chỉ Admin)
        /// Áp dụng CinemaHallValidator để kiểm tra dữ liệu đầu vào
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ResponseModel<CinemaHall>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Create([FromBody] CinemaHall hall)
        {
            try
            {
                // VALIDATION: Sử dụng CinemaHallValidator
                var validation = CinemaHallValidator.Validate(hall);
                if (!validation.IsValid)
                    return BadRequestError(validation.ErrorMessage ?? "Dữ liệu phòng chiếu không hợp lệ");

                // ========== GÁN GIÁ TRỊ MẶC ĐỊNH CHO AUDIT & SOFT DELETE ==========
                hall.IsDeleted = false;
                hall.CreatedAt = DateTime.UtcNow;
                hall.CreatedBy = GetUserId();

                // ========== LƯU VÀO DATABASE ==========
                _context.CinemaHalls.Add(hall);
                await _context.SaveChangesAsync();

                Logger.LogInformation("[CRUD] POST /api/cinemahalls - Created: {Name} (ID: {Id})", hall.Name, hall.Id);

                return CreatedResponse(hall, $"Tạo phòng chiếu '{hall.Name}' thành công", $"/api/cinemahalls/{hall.Id}");
            }
            catch (DbUpdateException dbEx)
            {
                Logger.LogError(dbEx, "[DATABASE ERROR] Create CinemaHall: {Message}", dbEx.Message);
                return ErrorResponse("Lỗi khi lưu dữ liệu vào database. Vui lòng kiểm tra dữ liệu đầu vào.", StatusCodes.Status500InternalServerError);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[SYSTEM ERROR] Create CinemaHall: {Message}", ex.Message);
                return ErrorResponse("Đã có lỗi hệ thống xảy ra. Vui lòng liên hệ quản trị viên.", StatusCodes.Status500InternalServerError);
            }
        }

        // ========== PUT ENDPOINT ==========

        /// <summary>
        /// Cập nhật toàn bộ thông tin phòng chiếu (Chỉ Admin)
        /// Áp dụng CinemaHallValidator để kiểm tra dữ liệu đầu vào
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ResponseModel<CinemaHall>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ResponseModel<object>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Update(int id, [FromBody] CinemaHall hall)
        {
            try
            {
                // ========== KIỂM TRA ID HỢP LỆ ==========
                if (id <= 0)
                    return BadRequestError("ID phòng chiếu không hợp lệ");

                if (hall == null)
                    return BadRequestError("Dữ liệu trống");

                // ========== KIỂM TRA VALIDATION BẰNG STATIC VALIDATOR ==========
                var validation = CinemaHallValidator.Validate(hall);

                if (!validation.IsValid)
                {
                    Logger.LogWarning("[VALIDATION ERROR] Update CinemaHall {Id}: {ErrorMessage}", id, validation.ErrorMessage);
                    return BadRequestError(validation.ErrorMessage ?? "Dữ liệu phòng chiếu không hợp lệ");
                }

                // ========== TÌM PHÒNG CHIẾU HIỆN CÓ ==========
                var existingHall = await _context.CinemaHalls.FirstOrDefaultAsync(ch => ch.Id == id);
                if (existingHall == null)
                    return NotFoundError($"Không tìm thấy phòng chiếu với ID: {id}");

                // ========== CẬP NHẬT TẤT CẢ CÁC TRƯỜNG ==========
                existingHall.Name = hall.Name;
                existingHall.TotalSeats = hall.TotalSeats;
                existingHall.UpdatedAt = DateTime.UtcNow;
                existingHall.UpdatedBy = GetUserId();

                await _context.SaveChangesAsync();

                Logger.LogInformation("[CRUD] PUT /api/cinemahalls/{Id} - Updated: {Name}", id, existingHall.Name);

                return OkResponse(existingHall, $"Cập nhật phòng chiếu '{existingHall.Name}' thành công");
            }
            catch (DbUpdateException dbEx)
            {
                Logger.LogError(dbEx, "[DATABASE ERROR] Update CinemaHall {Id}: {Message}", id, dbEx.Message);
                return ErrorResponse("Lỗi khi cập nhật dữ liệu. Vui lòng thử lại.", StatusCodes.Status500InternalServerError);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[SYSTEM ERROR] Update CinemaHall {Id}: {Message}", id, ex.Message);
                return ErrorResponse("Đã có lỗi hệ thống xảy ra. Vui lòng liên hệ quản trị viên.", StatusCodes.Status500InternalServerError);
            }
        }

        // ========== PATCH ENDPOINT ==========

        /// <summary>
        /// PATCH /api/cinemahalls/{id}
        /// Cập nhật một số trường của phòng chiếu (Chỉ Admin)
        /// </summary>
        [HttpPatch("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ResponseModel<CinemaHall>), StatusCodes.Status200OK)]
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
                    return BadRequestError("ID phòng chiếu không hợp lệ");

                if (updates == null || updates.Count == 0)
                    return BadRequestError("Phải cung cấp ít nhất một trường để cập nhật");

                // Tìm phòng chiếu
                var hall = await _context.CinemaHalls.FirstOrDefaultAsync(ch => ch.Id == id);
                if (hall == null)
                {
                    Logger.LogInformation("[CRUD] PATCH /api/cinemahalls/{Id} - Not found (404)", id);
                    return NotFoundError($"Không tìm thấy phòng chiếu với ID: {id}");
                }

                // Cập nhật từng trường
                foreach (var update in updates)
                {
                    // VALIDATION: Kiểm tra từng trường trước khi gán
                    var validation = CinemaHallValidator.ValidateField(update.Key, update.Value);
                    if (!validation.IsValid)
                        return BadRequestError(validation.ErrorMessage ?? "Dữ liệu không hợp lệ");

                    switch (update.Key.ToLower())
                    {
                        case "name":
                            hall.Name = update.Value?.ToString() ?? hall.Name;
                            break;
                        case "totalseats":
                            if (int.TryParse(update.Value?.ToString(), out var totalSeats))
                                hall.TotalSeats = totalSeats;
                            break;
                    }
                }

                hall.UpdatedAt = DateTime.UtcNow;
                hall.UpdatedBy = GetUserId();

                // OPTIMIZATION: Removed redundant _context.CinemaHalls.Update(hall).
                await _context.SaveChangesAsync();

                Logger.LogInformation("[CRUD] PATCH /api/cinemahalls/{Id} - Partially updated: {Name}", id, hall.Name);

                return OkResponse(hall, $"Cập nhật một phần phòng chiếu '{hall.Name}' thành công");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Lỗi xảy ra trong hệ thống");
                return ErrorResponse("Đã có lỗi hệ thống xảy ra. Vui lòng liên hệ quản trị viên.", StatusCodes.Status500InternalServerError);
            }
        }

        // ========== DELETE ENDPOINT ==========

        /// <summary>
        /// Xóa phòng chiếu (Xóa mềm - Soft Delete) - Chỉ Admin
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
                if (id <= 0) return BadRequestError("ID phòng chiếu không hợp lệ");

                // Tìm phòng chiếu (Chỉ tìm phòng chưa xóa)
                var hall = await _context.CinemaHalls.FirstOrDefaultAsync(ch => ch.Id == id && ch.IsDeleted == false);

                if (hall == null)
                    return NotFoundError($"Không tìm thấy phòng chiếu hoặc phòng chiếu đã bị xóa trước đó (ID: {id})");

                // ✅ THỰC HIỆN SOFT DELETE: Chỉ bật cờ IsDeleted
                hall.IsDeleted = true;
                hall.DeletedAt = DateTime.UtcNow;
                hall.UpdatedBy = GetUserId();

                await _context.SaveChangesAsync();

                return OkResponse($"Đã gỡ bỏ phòng chiếu '{hall.Name}' khỏi hệ thống (Xóa mềm) thành công");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Lỗi xảy ra trong hệ thống");
                return ErrorResponse("Đã có lỗi hệ thống xảy ra. Vui lòng liên hệ quản trị viên.", StatusCodes.Status500InternalServerError);
            }
        }
    }
}
