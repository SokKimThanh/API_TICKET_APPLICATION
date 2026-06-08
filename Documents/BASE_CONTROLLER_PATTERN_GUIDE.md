# Base Controller Pattern - Hướng Dẫn Chi Tiết

> **Mô hình kiến trúc: Base Controller Pattern** (Người quản gia)
> 
> Giúp tập trung quản lý DbContext, chuẩn hóa Response, và giảm code lặp lại

---

## 📋 Mục Lục

1. [Tổng Quan](#tổng-quan)
2. [AppBaseController - Lớp Quản Gia](#appbasecontroller---lớp-quản-gia)
3. [Response Wrapper Pattern](#response-wrapper-pattern)
4. [Sửa Đổi MoviesController](#sửa-đổi-moviescontroller)
5. [Ví Dụ Thực Tế](#ví-dụ-thực-tế)
6. [Best Practices](#best-practices)

---

## 🏗️ Tổng Quan

### Vấn Đề Cũ (Không Tốt)
```csharp
// ❌ Code lặp lại trong mỗi Controller
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
            return NotFound(new { message = "Không tìm thấy" });

        return Ok(new { success = true, data = movie }); // Response format không thống nhất
    }
}

public class ShowtimesController : ControllerBase
{
    private readonly AppDbContext _context;

    public ShowtimesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var showtime = await _context.Showtimes.FindAsync(id);
        if (showtime == null)
            return NotFound(new { message = "Không tìm thấy" });

        return Ok(new { success = true, data = showtime }); // Lặp lại code!
    }
}
```

**Vấn đề:**
- ❌ DbContext được inject lặp lại ở mỗi Controller
- ❌ Response format không thống nhất (mỗi developer viết khác nhau)
- ❌ Error handling không nhất quán
- ❌ Khó bảo trì, mở rộng

---

## ✅ Giải Pháp: Base Controller Pattern

```csharp
// ✅ Lớp quản gia tập trung
public abstract class AppBaseController : ControllerBase
{
    protected readonly AppDbContext _context;

    protected AppBaseController(AppDbContext context)
    {
        _context = context;
    }

    // Response wrapper methods
    protected OkObjectResult OkResponse(object? data, string message = "Thành công")
    {
        var response = new { success = true, message, data };
        return Ok(response);
    }

    protected ObjectResult ErrorResponse(string message, int statusCode = 400)
    {
        var response = new { success = false, message };
        return StatusCode(statusCode, response);
    }
}

// ✅ Các Controller chỉ kế thừa Base
public class MoviesController : AppBaseController
{
    public MoviesController(AppDbContext context) : base(context) { }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var movie = await _context.Movies.FindAsync(id);
        return movie == null 
            ? NotFoundError("Không tìm thấy phim")
            : OkResponse(movie);
    }
}

public class ShowtimesController : AppBaseController
{
    public ShowtimesController(AppDbContext context) : base(context) { }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var showtime = await _context.Showtimes.FindAsync(id);
        return showtime == null 
            ? NotFoundError("Không tìm thấy suất chiếu")
            : OkResponse(showtime);
    }
}
```

**Lợi ích:**
- ✅ DbContext quản lý ở 1 chỗ (AppBaseController)
- ✅ Response format thống nhất
- ✅ Code sạch, dễ mở rộng
- ✅ Dễ bảo trì

---

## 🎯 AppBaseController - Lớp Quản Gia

### Kiến Trúc

```
┌─────────────────────────────────────────────────┐
│           AppBaseController (Abstract)           │
├─────────────────────────────────────────────────┤
│ Protected Fields:                               │
│   - _context: AppDbContext                      │
│                                                  │
│ Public Methods:                                 │
│   - OkResponse()        (HTTP 200)              │
│   - CreatedResponse()   (HTTP 201)              │
│   - ErrorResponse()     (HTTP 4xx/5xx)          │
│   - NotFoundError()     (HTTP 404)              │
│   - BadRequestError()   (HTTP 400)              │
│   - UnauthorizedError() (HTTP 401)              │
│   - ForbiddenError()    (HTTP 403)              │
│                                                  │
│ Utility Methods:                                │
│   - GetUserId()         (From JWT Token)        │
│   - GetUserEmail()      (From JWT Token)        │
│   - IsUserAuthenticated()                       │
│   - IsUserInRole()                              │
└─────────────────────────────────────────────────┘
        ▲           ▲           ▲
        │           │           │
        │           │           │
  ┌─────┴───┐ ┌──────┴──┐ ┌────┴────┐
  │ Movies  │ │Showtimes│ │  Users  │
  │Ctrl     │ │  Ctrl   │ │  Ctrl   │
  └─────────┘ └─────────┘ └─────────┘
```

### Các Methods Chính

#### 1. **OkResponse(data, message)** - Trả về HTTP 200

```csharp
// Cấu trúc JSON
{
  "success": true,
  "message": "Lấy phim thành công",
  "data": { "id": 1, "title": "Inception", ... },
  "timestamp": "2024-12-19T10:30:00Z"
}
```

**Ví dụ sử dụng:**
```csharp
[HttpGet("{id}")]
public async Task<IActionResult> GetById(int id)
{
    var movie = await _context.Movies.FindAsync(id);
    if (movie == null)
        return NotFoundError("Không tìm thấy");

    // Trả về 200 OK với response wrapper
    return OkResponse(movie, "Lấy phim thành công");
}
```

#### 2. **ErrorResponse(message, statusCode)** - Trả về lỗi (4xx/5xx)

```csharp
// Cấu trúc JSON
{
  "success": false,
  "message": "Không tìm thấy phim",
  "data": null,
  "errorCode": "NOT_FOUND",
  "timestamp": "2024-12-19T10:30:00Z"
}
```

**Ví dụ sử dụng:**
```csharp
[HttpDelete("{id}")]
public async Task<IActionResult> Delete(int id)
{
    var movie = await _context.Movies.FindAsync(id);
    if (movie == null)
        return ErrorResponse("Không tìm thấy phim", StatusCodes.Status404NotFound);

    _context.Movies.Remove(movie);
    await _context.SaveChangesAsync();

    return OkResponse("Xóa phim thành công");
}
```

#### 3. **Shortcut Methods** - NotFoundError, BadRequestError, v.v.

```csharp
// Thay vì:
return ErrorResponse("Không hợp lệ", StatusCodes.Status400BadRequest, "BAD_REQUEST");

// Dùng shortcut:
return BadRequestError("Không hợp lệ");

// Các shortcut khác:
return NotFoundError("Không tìm thấy");
return UnauthorizedError("Cần đăng nhập");
return ForbiddenError("Không có quyền");
```

#### 4. **GetUserId()** - Lấy ID từ JWT Token

```csharp
[HttpPost]
[Authorize] // Yêu cầu đăng nhập
public async Task<IActionResult> Create([FromBody] Movie movie)
{
    // Lấy ID người dùng từ JWT Token
    var userId = GetUserId();

    if (!userId.HasValue)
        return UnauthorizedError("Bạn cần đăng nhập");

    // Ghi log ai tạo
    Console.WriteLine($"User {userId} created movie: {movie.Title}");

    _context.Movies.Add(movie);
    await _context.SaveChangesAsync();

    return CreatedResponse(movie);
}
```

---

## 📦 Response Wrapper Pattern

### Cấu Trúc Chung

```typescript
// Thành công (200, 201)
{
  success: true,
  message: "Thành công",
  data: { ... },
  timestamp: "2024-12-19T10:30:00Z"
}

// Lỗi (400, 404, 500)
{
  success: false,
  message: "Lỗi mô tả",
  data: null,
  errorCode: "ERROR_TYPE",
  timestamp: "2024-12-19T10:30:00Z"
}
```

### Lợi Ích

✅ **Thống nhất** - Front-end luôn biết format response  
✅ **Dễ parse** - Luôn có `success` field để check  
✅ **Rõ ràng** - `message` giải thích rõ what/why  
✅ **Tracking** - `timestamp` để debug  
✅ **Errors** - `errorCode` để client xử lý lỗi cụ thể

---

## 🔄 Sửa Đổi MoviesController

### Bước 1: Thay Đổi Inheritance

**Trước:**
```csharp
public class MoviesController : ControllerBase
{
    private readonly AppDbContext _context;

    public MoviesController(AppDbContext context)
    {
        _context = context;
    }
}
```

**Sau:**
```csharp
public class MoviesController : AppBaseController
{
    public MoviesController(AppDbContext context) : base(context)
    {
        // Không cần khai báo _context nữa, kế thừa từ base
    }
}
```

### Bước 2: Thay Thế Response Methods

**Trước:**
```csharp
[HttpGet("{id}")]
public async Task<IActionResult> GetById(int id)
{
    var movie = await _context.Movies.FindAsync(id);

    if (movie == null)
        return NotFound(new { message = "Không tìm thấy" });

    return Ok(movie);
}
```

**Sau:**
```csharp
[HttpGet("{id}")]
public async Task<IActionResult> GetById(int id)
{
    var movie = await _context.Movies.FindAsync(id);

    if (movie == null)
        return NotFoundError("Không tìm thấy phim");

    return OkResponse(movie, "Lấy phim thành công");
}
```

### Bước 3: Chuẩn Hóa Error Handling

**Trước:**
```csharp
[HttpPost]
public async Task<IActionResult> Create([FromBody] Movie movie)
{
    try
    {
        if (movie == null)
            return BadRequest("Movie không được null");

        _context.Movies.Add(movie);
        await _context.SaveChangesAsync();

        return Ok(movie);
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { error = ex.Message });
    }
}
```

**Sau:**
```csharp
[HttpPost]
public async Task<IActionResult> Create([FromBody] Movie movie)
{
    try
    {
        if (movie == null)
            return BadRequestError("Dữ liệu phim không được để trống");

        if (string.IsNullOrWhiteSpace(movie.Title))
            return BadRequestError("Tên phim không được để trống");

        _context.Movies.Add(movie);
        await _context.SaveChangesAsync();

        return CreatedResponse(movie, "Tạo phim thành công", $"/api/movies/{movie.Id}");
    }
    catch (Exception ex)
    {
        return ErrorResponse($"Lỗi khi tạo phim: {ex.Message}", StatusCodes.Status500InternalServerError);
    }
}
```

---

## 💡 Ví Dụ Thực Tế

### Ví Dụ 1: GET by ID

```csharp
[HttpGet("{id}")]
public async Task<IActionResult> GetById(int id)
{
    // Validation
    if (id <= 0)
        return BadRequestError("ID không hợp lệ");

    // Lấy dữ liệu
    var movie = await _context.Movies.FirstOrDefaultAsync(m => m.Id == id);

    // Check not found
    if (movie == null)
        return NotFoundError($"Không tìm thấy phim với ID: {id}");

    // Success response
    return OkResponse(movie, "Lấy phim thành công");
}

// Response 200 OK:
{
  "success": true,
  "message": "Lấy phim thành công",
  "data": {
    "id": 1,
    "title": "The Shawshank Redemption",
    "genre": "Drama",
    ...
  },
  "timestamp": "2024-12-19T10:30:00Z"
}

// Response 404 Not Found:
{
  "success": false,
  "message": "Không tìm thấy phim với ID: 999",
  "data": null,
  "errorCode": "NOT_FOUND",
  "timestamp": "2024-12-19T10:30:00Z"
}
```

### Ví Dụ 2: POST Create với Validation

```csharp
[HttpPost]
[Authorize] // Yêu cầu đăng nhập
public async Task<IActionResult> Create([FromBody] Movie movie)
{
    try
    {
        // Validation
        if (movie == null)
            return BadRequestError("Dữ liệu phim không được để trống");

        if (string.IsNullOrWhiteSpace(movie.Title))
            return BadRequestError("Tên phim không được để trống");

        if (movie.DurationInMinutes <= 0)
            return BadRequestError("Thời lượng phim phải lớn hơn 0");

        // Get current user from JWT
        var userId = GetUserId();
        if (!userId.HasValue)
            return UnauthorizedError("Bạn cần đăng nhập");

        Console.WriteLine($"User {userId} creating movie: {movie.Title}");

        // Add to DB
        _context.Movies.Add(movie);
        await _context.SaveChangesAsync();

        // Return 201 Created
        return CreatedResponse(movie, "Tạo phim thành công", $"/api/movies/{movie.Id}");
    }
    catch (Exception ex)
    {
        return ErrorResponse($"Lỗi khi tạo phim: {ex.Message}", StatusCodes.Status500InternalServerError);
    }
}

// Response 201 Created:
{
  "success": true,
  "message": "Tạo phim thành công",
  "data": {
    "id": 3,
    "title": "Inception",
    ...
  },
  "timestamp": "2024-12-19T10:30:00Z"
}
```

### Ví Dụ 3: DELETE với Authorization Check

```csharp
[HttpDelete("{id}")]
[Authorize(Roles = "Admin")] // Chỉ Admin được xóa
public async Task<IActionResult> Delete(int id)
{
    try
    {
        // Validation
        if (id <= 0)
            return BadRequestError("ID không hợp lệ");

        // Check authorization (role-based)
        if (!IsUserInRole("Admin"))
            return ForbiddenError("Chỉ Admin mới được xóa phim");

        // Find movie
        var movie = await _context.Movies.FindAsync(id);
        if (movie == null)
            return NotFoundError($"Không tìm thấy phim với ID: {id}");

        // Delete
        _context.Movies.Remove(movie);
        await _context.SaveChangesAsync();

        Console.WriteLine($"Admin {GetUserEmail()} deleted movie: {movie.Title}");

        // Success response (200 OK, không cần data)
        return OkResponse($"Xóa phim '{movie.Title}' thành công");
    }
    catch (Exception ex)
    {
        return ErrorResponse($"Lỗi khi xóa phim: {ex.Message}", StatusCodes.Status500InternalServerError);
    }
}
```

---

## 🎓 Best Practices

### ✅ DO

1. **Luôn kế thừa AppBaseController**
   ```csharp
   public class ShowtimesController : AppBaseController
   {
       public ShowtimesController(AppDbContext context) : base(context) { }
   }
   ```

2. **Sử dụng OkResponse/ErrorResponse cho response thống nhất**
   ```csharp
   return OkResponse(data, "Thành công");
   return NotFoundError("Không tìm thấy");
   ```

3. **Kiểm tra JWT claims khi cần**
   ```csharp
   var userId = GetUserId();
   var email = GetUserEmail();
   ```

4. **Validation trước khi xử lý**
   ```csharp
   if (id <= 0)
       return BadRequestError("ID không hợp lệ");
   ```

5. **Có error handling (try-catch)**
   ```csharp
   try { ... }
   catch (Exception ex)
   {
       return ErrorResponse($"Lỗi: {ex.Message}", StatusCodes.Status500InternalServerError);
   }
   ```

### ❌ DON'T

1. **Không inject AppDbContext riêng trong con**
   ```csharp
   // ❌ Sai
   public class MoviesController : ControllerBase
   {
       private readonly AppDbContext _context;
       public MoviesController(AppDbContext context) { _context = context; }
   }
   ```

2. **Không trả về Ok() trực tiếp**
   ```csharp
   // ❌ Sai
   return Ok(movie);

   // ✅ Đúng
   return OkResponse(movie, "Thành công");
   ```

3. **Không mix response formats**
   ```csharp
   // ❌ Sai
   return Ok(movie);
   return NotFound("Không tìm thấy");
   return StatusCode(500, new { error = ex.Message });

   // ✅ Đúng
   return OkResponse(movie);
   return NotFoundError("Không tìm thấy");
   return ErrorResponse(ex.Message, StatusCodes.Status500InternalServerError);
   ```

4. **Không quên try-catch cho async operations**
   ```csharp
   // ❌ Sai - Có thể crash server nếu SaveChanges() lỗi
   _context.Movies.Add(movie);
   await _context.SaveChangesAsync();

   // ✅ Đúng
   try
   {
       _context.Movies.Add(movie);
       await _context.SaveChangesAsync();
   }
   catch (Exception ex)
   {
       return ErrorResponse($"Lỗi: {ex.Message}", StatusCodes.Status500InternalServerError);
   }
   ```

---

## 📊 Bảng So Sánh

| Điều | Trước | Sau |
|-----|------|-----|
| Inheritance | ControllerBase | AppBaseController |
| _context declaration | ✅ Có (trong mỗi con) | ❌ Không (kế thừa từ base) |
| Response format | ❌ Không thống nhất | ✅ Thống nhất |
| Error handling | ❌ Khác nhau | ✅ Một kiểu |
| JWT support | ❌ Không | ✅ Có (GetUserId, etc.) |
| Code length | ❌ Dài | ✅ Ngắn, sạch |
| Maintainability | ❌ Khó | ✅ Dễ |

---

## 🚀 Mở Rộng Base Controller

### Thêm Methods Cho Business Logic

```csharp
public abstract class AppBaseController : ControllerBase
{
    // ... existing code ...

    /// <summary>
    /// LogActivity - Ghi log hoạt động
    /// </summary>
    protected void LogActivity(string action, object? data = null)
    {
        var userId = GetUserId();
        var timestamp = DateTime.UtcNow;
        Console.WriteLine($"[{timestamp}] User {userId}: {action} | Data: {data}");
    }

    /// <summary>
    /// PaginationResponse - Helper cho pagination
    /// </summary>
    protected OkObjectResult PaginationResponse<T>(
        List<T> items,
        int pageNumber,
        int pageSize,
        int totalCount)
    {
        return OkResponse(new
        {
            pageNumber,
            pageSize,
            totalCount,
            totalPages = (totalCount + pageSize - 1) / pageSize,
            data = items
        });
    }
}

// Sử dụng:
[HttpGet]
public async Task<IActionResult> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
{
    var movies = await _context.Movies
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    var totalCount = await _context.Movies.CountAsync();

    return PaginationResponse(movies, pageNumber, pageSize, totalCount);
}
```

---

**Trạng thái**: ✅ Production Ready  
**Phiên bản**: 1.0.0  
**Cập nhật lần cuối**: 2024-12-19
