# Base Controller Pattern - Tóm Tắt Nhanh

> **Giải pháp:** Giảm code lặp lại, chuẩn hóa Response, quản lý DbContext tập trung

---

## 📌 Nhanh Gọn

### Trước (❌ Code Lặp Lại)
```csharp
public class MoviesController : ControllerBase
{
    private readonly AppDbContext _context;

    public MoviesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var movie = await _context.Movies.FindAsync(id);
        if (movie == null)
            return NotFound();

        return Ok(movie);
    }
}

// Lặp lại ở ShowtimesController, UsersController, ... 😫
```

### Sau (✅ Base Controller Pattern)
```csharp
// 1. Tạo base class (1 lần)
public abstract class AppBaseController : ControllerBase
{
    protected readonly AppDbContext _context;

    protected AppBaseController(AppDbContext context) => _context = context;

    // Response wrapper
    protected OkObjectResult OkResponse(object? data, string message = "Thành công")
    {
        return Ok(new { success = true, message, data });
    }

    protected ObjectResult NotFoundError(string message = "Không tìm thấy")
    {
        return StatusCode(404, new { success = false, message });
    }

    // JWT utility
    protected int? GetUserId()
    {
        var claim = User.FindFirst("UserId") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        return int.TryParse(claim?.Value, out var id) ? id : null;
    }
}

// 2. Các Controller chỉ kế thừa (reusable!)
public class MoviesController : AppBaseController
{
    public MoviesController(AppDbContext context) : base(context) { }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var movie = await _context.Movies.FindAsync(id);
        return movie == null ? NotFoundError() : OkResponse(movie);
    }
}

public class ShowtimesController : AppBaseController
{
    public ShowtimesController(AppDbContext context) : base(context) { }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var showtime = await _context.Showtimes.FindAsync(id);
        return showtime == null ? NotFoundError() : OkResponse(showtime);
    }
}
```

---

## 🎯 Files Được Tạo

| File | Mục Đích |
|------|---------|
| `Controllers/AppBaseController.cs` | Lớp quản gia cơ sở |
| `Controllers/MoviesController.cs` | Controller ví dụ (cập nhật) |
| `BASE_CONTROLLER_PATTERN_GUIDE.md` | Hướng dẫn chi tiết |

---

## 📚 Các Methods Chính

### Response Wrapper
```csharp
OkResponse(data, "Thành công")              // HTTP 200
CreatedResponse(data, "Tạo mới")            // HTTP 201
ErrorResponse(message, statusCode)          // HTTP 4xx/5xx
NotFoundError(message)                      // HTTP 404
BadRequestError(message)                    // HTTP 400
UnauthorizedError(message)                  // HTTP 401
ForbiddenError(message)                     // HTTP 403
```

### JWT Utilities
```csharp
GetUserId()              // Lấy ID từ Token
GetUserEmail()           // Lấy email từ Token
GetUserName()            // Lấy username từ Token
IsUserAuthenticated()    // Check login
IsUserInRole(role)       // Check role
```

---

## 💡 Cách Sử Dụng

### Bước 1: Thay Đổi Inheritance
```csharp
// Trước:
public class MoviesController : ControllerBase { }

// Sau:
public class MoviesController : AppBaseController { }
```

### Bước 2: Update Constructor
```csharp
public MoviesController(AppDbContext context) : base(context) { }
```

### Bước 3: Thay Response Methods
```csharp
// Trước:
if (movie == null) return NotFound();
return Ok(movie);

// Sau:
if (movie == null) return NotFoundError("Phim không tồn tại");
return OkResponse(movie, "Lấy phim thành công");
```

---

## 📊 Response Format Thống Nhất

### Success Response (200, 201)
```json
{
  "success": true,
  "message": "Lấy phim thành công",
  "data": { "id": 1, "title": "Inception", ... },
  "timestamp": "2024-12-19T10:30:00Z"
}
```

### Error Response (4xx, 5xx)
```json
{
  "success": false,
  "message": "Không tìm thấy phim",
  "data": null,
  "errorCode": "NOT_FOUND",
  "timestamp": "2024-12-19T10:30:00Z"
}
```

---

## 🔐 JWT Token Integration

### Ví Dụ: Lấy User ID từ Token
```csharp
[HttpPost]
[Authorize]
public async Task<IActionResult> Create([FromBody] Movie movie)
{
    var userId = GetUserId();

    if (!userId.HasValue)
        return UnauthorizedError("Bạn cần đăng nhập");

    Console.WriteLine($"User {userId} creating movie: {movie.Title}");

    // ... rest of logic
}
```

### JWT Token Format (Example)
```json
{
  "iss": "YourApp",
  "aud": "YourAppUsers",
  "UserId": "123",
  "email": "user@example.com",
  "name": "John Doe"
}
```

---

## 🎓 Best Practices

✅ **DO:**
- Kế thừa AppBaseController từ tất cả Controllers
- Sử dụng OkResponse/ErrorResponse cho response thống nhất
- Validation trước khi xử lý
- Try-catch cho async operations
- Dùng GetUserId() cho JWT operations

❌ **DON'T:**
- Inject AppDbContext riêng trong con
- Mix response formats (Ok, NotFound, StatusCode)
- Quên try-catch
- Return Ok() trực tiếp (dùng OkResponse)
- Tự handle error response (dùng ErrorResponse shortcuts)

---

## 🚀 Lợi Ích

| Lợi Ích | Chi Tiết |
|--------|---------|
| **DRY** | Giảm code lặp 80% |
| **Consistency** | Response format thống nhất |
| **Maintainability** | Dễ update toàn cục |
| **Security** | JWT handling tập trung |
| **Scalability** | Dễ add features cho tất cả controllers |
| **Documentation** | XML comments rõ ràng |

---

## 📚 Tài Liệu Chi Tiết

📖 Xem: `BASE_CONTROLLER_PATTERN_GUIDE.md` để:
- Giải thích kiến trúc chi tiết
- Ví dụ thực tế từng method
- So sánh trước/sau
- Best practices
- Mở rộng Base Controller

---

**Status**: ✅ Production Ready  
**Build**: ✅ Successful  
**Phiên bản**: 1.0.0  
**Ngày**: 2024-12-19
