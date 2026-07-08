# 🔍 CODE AUDIT & ARCHITECTURE ANALYSIS
## API_TICKET_APPLICATION - Đánh giá kiến trúc & 3 Câu hỏi chính

**Ngày kiểm toán:** Tháng 1, 2025
**Phiên bản:** .NET 10
**Đối tượng:** MoviesController, AppBaseController, Program.cs

---

## 📊 TÓM TẮT KIỂM TOÁN

### ✅ Những gì dự án ĐÃ CÓ:

| Thành phần | Trạng thái | Chi tiết |
|-----------|-----------|---------|
| **Base Controller Pattern** | ✅ CÓ | AppBaseController với response methods chuẩn |
| **CRUD Operations** | ✅ CÓ | Get, Post, Put, Patch, Delete đầy đủ |
| **Soft Delete Logic** | ✅ CÓ | Implemented trong Movies controller |
| **Pagination** | ✅ CÓ | GetAll endpoint có pageNumber & pageSize |
| **Exception Handling** | ✅ CÓ | Try-catch cơ bản trong mỗi endpoint |
| **Error Response Format** | ✅ CÓ | Standardized JSON response từ AppBaseController |
| **API Documentation** | ✅ CÓ | OpenAPI/Swagger cấu hình sẵn |
| **Middleware Stack** | ✅ CÓ | Exception, Input Validation, Security logging |

### ❌ Những gì dự án CHƯA CÓ:

| Thành phần | Trạng thái | Chi tiết |
|-----------|-----------|---------|
| **Service Layer** | ❌ CHƯA | Không có thư mục Services/ hoặc interface IService |
| **Repository Pattern** | ❌ CHƯA | Không có thư mục Repositories/ hoặc interface IRepository |
| **Dependency Injection Container** | ⚠️ Bộ phận | Chỉ DI DbContext, chưa có DI Service/Repository |
| **Authentication (JWT)** | ❌ CHƯA | Không có token authentication, chỉ có skeleton GetUserId() |
| **Authorization (Roles)** | ❌ CHƯA | Không có [Authorize], [AllowAnonymous] attributes |
| **Unit Tests** | ❌ CHƯA | Không có .Tests project |
| **Comprehensive Logging** | ⚠️ Bộ phận | Chỉ dùng Console.WriteLine(), không có Serilog/Logger structured |
| **Input Validation Rules** | ⚠️ Bộ phận | Chỉ check null/whitespace, không có FluentValidation |
| **DTOs (Data Transfer Objects)** | ❌ CHƯA | Trả về Entity trực tiếp, chưa có DTO layer |

---

## 🎯 3 CÂU HỎI CHÍNH VÀ PHÂN TÍCH CHI TIẾT

---

## ❓ **CÂUH 1: Bằng chứ thị giác (Visual Proof)**

### **Câu hỏi:**
> Bạn dựa vào những chi tiết cụ thể nào (tên file, tên thư mục, tab đang mở) trong ảnh chụp màn hình thiết lập Solution Explorer để khẳng định tôi chưa có "Service Layer" và "Repository Pattern"?

---

### **📁 Bằng chứ cấu trúc thư mục HIỆN TẠI:**

Từ kết quả `get_files_in_project`, cấu trúc workspace thực tế là:

```
API_TICKET_APPLICATION/
│
├── Controllers/                    ✅ CÓ
│   ├── AppBaseController.cs
│   ├── MoviesController.cs
│   ├── TestController.cs
│
├── Models/                         ✅ CÓ
│   ├── AppDbContext.cs
│   ├── Movie.cs
│   ├── Booking.cs
│   ├── CinemaHall.cs
│   ├── Seat.cs
│   ├── Showtime.cs
│   ├── Ticket.cs
│   ├── User.cs
│
├── Properties/                     ✅ CÓ
│   └── launchSettings.json
│
├── RequestHTTP/                    ✅ CÓ
│   ├── request.http
│   └── request_movies.http
│
├── SQL/                            ✅ CÓ
│   └── DBQuery/
│
├── Documents/                      ✅ CÓ
│   ├── BASE_CONTROLLER_PATTERN_GUIDE.md
│   ├── BASE_CONTROLLER_SUMMARY.md
│   └── (các docs khác)
│
├── Program.cs
├── appsettings.json
├── appsettings.Development.json
└── .gitignore
```

### **❌ Những thư mục KHÔNG tồn tại:**

```
Services/                          ❌ KHÔNG CÓ
Repositories/                      ❌ KHÔNG CÓ
Interfaces/                        ❌ KHÔNG CÓ
DTOs/                             ❌ KHÔNG CÓ
```

---

### **🔍 Bằng chứ từ nội dung CODE:**

#### **Trong `MoviesController.cs` - Dòng 30-35:**
```csharp
public async Task<IActionResult> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
{
	try
	{
		// ❌ TRỰC TIẾP truy cập _context (từ AppBaseController)
		var query = _context.Movies.AsNoTracking().Where(m => m.IsDeleted == false);

		// ❌ KHÔNG gọi: await _movieService.GetAllAsync(pageNumber, pageSize);
		// ❌ KHÔNG gọi: await _movieRepository.GetAllAsync(pageNumber, pageSize);
```

**Chứng minh:**
- Nếu có **Service Layer**, code sẽ là: `var movies = await _movieService.GetAllAsync(pageNumber, pageSize);`
- Nếu có **Repository Pattern**, code sẽ là: `var movies = await _movieRepository.GetAllAsync(pageNumber, pageSize);`
- Hiện tại: **Trực tiếp dùng `_context`** → không có Service/Repository

#### **Trong `AppBaseController.cs` - Dòng 12-13:**
```csharp
public abstract class AppBaseController : ControllerBase
{
	// Controller KHÔNG nên biết DbContext
	protected readonly AppDbContext _context;  // ❌ RED FLAG

	protected AppBaseController(AppDbContext context)
	{
		_context = context ?? throw new ArgumentNullException(nameof(context));
	}
```

**Chứng minh:**
- Nếu có **Repository Pattern**, `AppBaseController` sẽ KHÔNG có `_context`
- Các controller sẽ inject `IMovieRepository`, `IBookingRepository` thay vì `AppDbContext`
- Hiện tại: **AppBaseController pass DbContext trực tiếp** → Violate separation of concerns

---

### **📝 Kết luận Câu 1:**

**Tôi không đoán mò.** Bằng chứ cụ thể là:

1. **File-system check**: `get_files_in_project` trả về danh sách file - **KHÔNG có file nào trong Services/, Repositories/, DTOs/**
2. **Code pattern analysis**: MoviesController gọi `_context` trực tiếp, không gọi service/repository
3. **Dependency injection**: Program.cs chỉ đăng ký `DbContext`, không đăng ký `IMovieService`, `IMovieRepository`

**Nhưng tôi phải thừa nhận:** Tôi không có ảnh chụp màn hình Solution Explorer từ Visual Studio của bạn. Có khả năng:
- Bạn đã tạo Services/Repositories ở project khác
- Hoặc bạn đã tạo chúng nhưng tôi chưa tìm thấy
- Hoặc chúng được đặt ở vị trí khác

**Nếu bạn đã có Services/Repositories rồi, vui lòng chỉ tôi đường dẫn, tôi sẽ cập nhật phân tích.**

---

---

## ❓ **CÂUH 2: Đánh giá độ ưu tiên**

### **Câu hỏi:**
> Tại sao bạn lại xếp "Service Layer" và "Repository Pattern" vào nhóm Ngay lập tức (High Priority), trong khi dự án hiện tại của tôi đã có thể chạy và trả về kết quả 200 OK với dữ liệu phim rồi? Việc chạy được không quan trọng bằng cấu trúc sao?

---

### **❌ TÔI ĐÃ NHẦM - CẦN CHỈNH SỬA**

Tôi công nhân **sai lầm trong đánh giá độ ưu tiên ban đầu.**

#### **Lý do tôi nhầm:**
Tôi áp dụng **quy tắc kiến trúc lý tưởng** (Best Practice) mà không xem xét **giai đoạn phát triển hiện tại** của dự án bạn.

### **📊 So sánh: Tiêu chí ưu tiên**

| Tiêu chí | Mô tả | Mức độ quan trọng |
|---------|-------|-------------------|
| **Functionality** | API chạy được, CRUD hoạt động | ⭐⭐⭐⭐⭐ **NGAY LẬP TỨC** |
| **Correctness** | Trả về đúng data, xử lý lỗi | ⭐⭐⭐⭐⭐ **NGAY LẬP TỨC** |
| **Security** | Authentication, Authorization | ⭐⭐⭐⭐⭐ **NGAY LẬP TỨC** |
| **Validation** | Kiểm tra input toàn diện | ⭐⭐⭐⭐ **CẦN SỚM** |
| **Logging** | Log structured, debug dễ | ⭐⭐⭐ **TỐI THIỂU** |
| **Architecture** | Service Layer, Repository | ⭐⭐ **CÓ THỂ CHỜ** |
| **Testing** | Unit/Integration tests | ⭐⭐ **CÓ THỂ CHỜ** |

---

### **🔄 Giai đoạn phát triển & Ưu tiên thay đổi:**

#### **GIAI ĐOẠN 1: MVP (Ngay bây giờ - Đó là nơi bạn đang ở)**

```
✅ FOCUS: Hoàn thành Functionality
  - Tất cả entity (Movie, User, Booking, Seat, Showtime, Ticket) có CRUD
  - Tất cả endpoint trả về đúng data format
  - Test qua request.http thành công

✅ FOCUS: Bảo mật cơ bản
  - JWT Authentication (ai được phép call API?)
  - Role-based Authorization (user vs admin)
  - Input validation (SQL injection, XSS)

⏳ DEFER: Architecture
  - Service Layer (chỉ 1-2 controller, chưa cần)
  - Repository Pattern (logic chưa phức tạp, chưa cần)
  - Unit Tests (focus on manual testing trước)

ƯỚI TIÊN NGAY:
  [1] Hoàn thành CRUD cho User, Booking, Seat, Showtime, Ticket
  [2] Thêm JWT Authentication
  [3] Thêm Authorization (Roles)
  [4] Validate input toàn diện
```

#### **GIAI ĐOẠN 2: Scaling (2-4 tuần nữa)**

```
⏰ LÚC ĐÓ, bạn sẽ nhận ra:
  - "Tôi có 6 controllers, logic lặp lại"
  - "Khó maintain khi mỗi controller có 200 dòng logic"
  - "Muốn viết unit test nhưng Controller quá nặng"

✅ LÚCĐÓ mới cần:
  [1] Tách Service Layer (để tái sử dụng logic)
  [2] Tách Repository (để thay đổi DB mà không sửa controller)
  [3] Viết Unit Tests (test service layer)
```

---

### **🎯 Câu trả lời thành thật:**

**Bạn đúng 100%.**

> **"Việc chạy được KHÔNG quan trọng bằng cấu trúc"**
>
> → **NHƯNG CHẮC LẠI:** Hiện tại, dự án bạn:
> - ✅ Chạy được
> - ✅ Có CRUD
> - ✅ Trả về data đúng
> - ❌ CHƯA có Auth
> - ❌ CHƯA có Validation toàn diện
>
> **Nên ưu tiên Auth + Validation trước, chứ không phải refactor architecture ngay.**

---

### **✅ Đánh giá lại độ ưu tiên CHÍNH XÁC:**

```
🔴 HIGH PRIORITY (Làm trong tuần này):
  [1] Hoàn thành CRUD cho 5 entity còn lại (User, Booking, Seat, Showtime, Ticket)
	  └─ Sử dụng AppBaseController pattern, tương tự Movies

  [2] Thêm JWT Authentication
	  └─ UserController.Login endpoint
	  └─ Generate JWT token
	  └─ Validate token trong middleware

  [3] Thêm Role-based Authorization
	  └─ [Authorize(Roles = "Admin")]
	  └─ [AllowAnonymous]

  [4] Implement FluentValidation
	  └─ Validate CreateMovieDto, UpdateMovieDto, etc.
	  └─ Replace inline checks

🟡 MEDIUM PRIORITY (2-4 tuần nữa):
  [5] Implement Service Layer
	  └─ IMovieService, MovieService
	  └─ Tách business logic khỏi controller

  [6] Implement Repository Pattern
	  └─ IMovieRepository, MovieRepository
	  └─ Centralize data access

  [7] Thêm Unit Tests
	  └─ Test Service layer
	  └─ Test Repository layer

  [8] Upgrade Logging
	  └─ Thay Console.WriteLine → Serilog
	  └─ Structured logging

🟢 LOW PRIORITY (Tháng 2-3):
  [9] Chi tiết Swagger Documentation
	  └─ [ProducesResponseType] chi tiết
	  └─ Example requests/responses

  [10] Performance optimization
	  └─ Caching
	  └─ Query optimization
```

---

### **📌 Kết luận Câu 2:**

**Tôi xin lỗi vì đánh giá sai.**

**Độ ưu tiên ĐÚNG phải là:**

1. **NGAY LẬP TỨC (This week):**
   - ✅ Hoàn thành CRUD cho 5 entity còn lại
   - ✅ JWT Authentication
   - ✅ Role-based Authorization
   - ✅ Input Validation (FluentValidation)

2. **CÓ THỂ CHỜ (2-4 weeks):**
   - Service Layer
   - Repository Pattern
   - Unit Tests

3. **CÓ THỂ ĐẾN CUỐI (Month 2-3):**
   - Documentation details
   - Performance optimization

**Lý do chính:** Bạn đúng - **"Việc chạy được VÀ an toàn" mới là ưu tiên, chứ không phải "kiến trúc đẹp".**

---

---

## ❓ **CÂUH 3: Lời khuyên kiến trúc - Clean Architecture**

### **Câu hỏi:**
> Giả sử bây giờ tôi muốn bắt đầu tách dự án này theo mô hình Clean Architecture (hoặc 3-Layer):
> - Tôi nên tạo thêm những thư mục/thư viện (Project) cụ thể nào trong Solution này?
> - File "AppBaseController.cs" và "MoviesController.cs" hiện tại sẽ cần thay đổi như thế nào (về mặt vai trò)?

---

### **📐 MÔ HÌNH 3-LAYER ARCHITECTURE CHO DỰ ÁN BẠN**

#### **CẤP ĐỘ 1: Single Project (Recommended cho dự án này)**

Nếu dự án chỉ là 1 project, tạo cấu trúc thư mục:

```
API_TICKET_APPLICATION/ (Single project)
│
├── Controllers/                          🎯 PRESENTATION LAYER
│   ├── AppBaseController.cs              (Sửa: Chỉ response methods)
│   ├── MoviesController.cs               (Sửa: Inject IMovieService)
│   ├── BookingController.cs
│   ├── UserController.cs
│   └── (các controller khác)
│
├── Services/                             🎯 BUSINESS LOGIC LAYER
│   ├── Interfaces/
│   │   ├── IMovieService.cs
│   │   ├── IBookingService.cs
│   │   ├── IUserService.cs
│   │   └── (các interface khác)
│   │
│   └── Implementations/
│       ├── MovieService.cs
│       ├── BookingService.cs
│       ├── UserService.cs
│       └── (các implementation khác)
│
├── Repositories/                         🎯 DATA ACCESS LAYER
│   ├── Interfaces/
│   │   ├── IMovieRepository.cs
│   │   ├── IBookingRepository.cs
│   │   ├── IUserRepository.cs
│   │   └── (các interface khác)
│   │
│   ├── Implementations/
│   │   ├── MovieRepository.cs
│   │   ├── BookingRepository.cs
│   │   ├── UserRepository.cs
│   │   └── (các implementation khác)
│   │
│   └── GenericRepository.cs              (Optional: Base Repository)
│
├── Models/
│   ├── AppDbContext.cs
│   ├── Movie.cs
│   ├── User.cs
│   ├── Booking.cs
│   ├── Seat.cs
│   ├── Showtime.cs
│   ├── Ticket.cs
│   ├── CinemaHall.cs
│   │
│   └── DTOs/                             🆕 NEW LAYER
│       ├── Movies/
│       │   ├── CreateMovieDto.cs
│       │   ├── UpdateMovieDto.cs
│       │   └── MovieResponseDto.cs
│       │
│       ├── Users/
│       │   ├── CreateUserDto.cs
│       │   ├── UserResponseDto.cs
│       │   └── LoginDto.cs
│       │
│       └── (các DTO khác)
│
├── Exceptions/                           🆕 NEW (Optional)
│   ├── NotFoundException.cs
│   ├── BadRequestException.cs
│   └── (các exception khác)
│
├── Validators/                           🆕 NEW (Optional)
│   ├── CreateMovieDtoValidator.cs
│   ├── UpdateMovieDtoValidator.cs
│   └── (các validator khác)
│
├── Middleware/
│   └── ExceptionHandlingMiddleware.cs   (Extract từ Program.cs)
│
├── Controllers/                          🎯 PRESENTATION
├── Program.cs                            (Update DI container)
├── appsettings.json
└── (các file khác)
```

---

#### **CẤP ĐỘ 2: Multiple Projects (Enterprise - Tương lai)**

Nếu muốn tách hoàn toàn:

```
Solution: API_TICKET_APPLICATION
│
├── API_TICKET_APPLICATION              (API/WebHost project)
│   ├── Controllers/
│   ├── Program.cs
│   ├── appsettings.json
│   └── (Presentation layer)
│
├── API_TICKET_APPLICATION.Core         (Business Logic project)
│   ├── Services/
│   ├── Interfaces/
│   ├── Exceptions/
│   └── Validators/
│
├── API_TICKET_APPLICATION.Data         (Data Access project)
│   ├── Repositories/
│   ├── Models/
│   └── AppDbContext.cs
│
├── API_TICKET_APPLICATION.Tests        (Unit Tests project)
│   ├── Services/
│   ├── Repositories/
│   └── Controllers/
│
└── API_TICKET_APPLICATION.Shared       (Shared/Common project)
	├── DTOs/
	├── Enums/
	└── (Shared classes)
```

---

### **🔄 THAY ĐỔI TRONG CÁC FILE:**

#### **1. AppBaseController.cs - Thay đổi vai trò**

**TRƯỚC (hiện tại):**
```csharp
public abstract class AppBaseController : ControllerBase
{
	// ❌ CHỨA: Database context - Không đúng chỗ cho controller
	protected readonly AppDbContext _context;

	protected AppBaseController(AppDbContext context)
	{
		_context = context ?? throw new ArgumentNullException(nameof(context));
	}

	// Các method response...
	protected OkObjectResult OkResponse(...) { ... }
	protected CreatedResult CreatedResponse(...) { ... }
	protected ObjectResult ErrorResponse(...) { ... }
}
```

**SAU (3-Layer):**
```csharp
public abstract class AppBaseController : ControllerBase
{
	// ✅ BỎ: Không còn _context
	// ✅ GIỮ LẠI: Chỉ response methods

	// Các method response (GIỮ NGUYÊN)
	protected OkObjectResult OkResponse(object? data, string message = "Thành công")
	{
		var response = new
		{
			success = true,
			message = message,
			data = data,
			timestamp = DateTime.UtcNow
		};
		return Ok(response);
	}

	protected CreatedResult CreatedResponse(object data, string message = "Tạo mới thành công", string? location = null)
	{
		var response = new
		{
			success = true,
			message = message,
			data = data,
			timestamp = DateTime.UtcNow
		};
		return Created(location ?? string.Empty, response);
	}

	protected ObjectResult ErrorResponse(string errorMessage, int statusCode = StatusCodes.Status400BadRequest, string? errorCode = null)
	{
		var response = new
		{
			success = false,
			message = errorMessage,
			data = (object?)null,
			errorCode = errorCode ?? GetErrorCodeFromStatus(statusCode),
			timestamp = DateTime.UtcNow
		};
		return StatusCode(statusCode, response);
	}

	// Các hàm helper
	protected ObjectResult NotFoundError(string message = "Không tìm thấy")
		=> ErrorResponse(message, StatusCodes.Status404NotFound, "NOT_FOUND");

	protected ObjectResult BadRequestError(string message = "Yêu cầu không hợp lệ")
		=> ErrorResponse(message, StatusCodes.Status400BadRequest, "BAD_REQUEST");

	protected ObjectResult UnauthorizedError(string message = "Không được phép")
		=> ErrorResponse(message, StatusCodes.Status401Unauthorized, "UNAUTHORIZED");

	protected ObjectResult ForbiddenError(string message = "Bị cấm truy cập")
		=> ErrorResponse(message, StatusCodes.Status403Forbidden, "FORBIDDEN");

	protected int? GetUserId()
	{
		var userIdClaim = User.FindFirst("UserId") ?? User.FindFirst(ClaimTypes.NameIdentifier);
		if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId)) return userId;
		return null;
	}

	private static string GetErrorCodeFromStatus(int statusCode)
	{
		return statusCode switch
		{
			StatusCodes.Status400BadRequest => "BAD_REQUEST",
			StatusCodes.Status401Unauthorized => "UNAUTHORIZED",
			StatusCodes.Status404NotFound => "NOT_FOUND",
			StatusCodes.Status500InternalServerError => "INTERNAL_SERVER_ERROR",
			_ => $"ERROR_{statusCode}"
		};
	}
}
```

---

#### **2. MoviesController.cs - Thay đổi vai trò**

**TRƯỚC (hiện tại):**
```csharp
public class MoviesController : AppBaseController
{
	public MoviesController(AppDbContext context) : base(context) { }

	[HttpGet]
	public async Task<IActionResult> GetAll(...)
	{
		try
		{
			// ❌ TRỰC TIẾP query Database
			var query = _context.Movies.AsNoTracking()
				.Where(m => m.IsDeleted == false);

			var movies = await query.OrderBy(m => m.Title)
				.Skip((pageNumber - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

			return OkResponse(new { pageNumber, pageSize, totalCount = ..., data = movies });
		}
		catch (Exception ex)
		{
			return ErrorResponse(...);
		}
	}
}
```

**SAU (3-Layer - Sử dụng Service):**
```csharp
public class MoviesController : AppBaseController
{
	private readonly IMovieService _movieService;  // ✅ NEW: Inject Service

	public MoviesController(IMovieService movieService)
	{
		_movieService = movieService ?? throw new ArgumentNullException(nameof(movieService));
	}

	[HttpGet]
	public async Task<IActionResult> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
	{
		try
		{
			// ✅ GỌI Service (Business logic ở đây)
			var result = await _movieService.GetAllAsync(pageNumber, pageSize);

			return OkResponse(result);
		}
		catch (Exception ex)
		{
			return ErrorResponse("Đã có lỗi hệ thống xảy ra", StatusCodes.Status500InternalServerError);
		}
	}

	[HttpGet("{id}")]
	public async Task<IActionResult> GetById(int id)
	{
		try
		{
			// ✅ GỌI Service
			var movie = await _movieService.GetByIdAsync(id);

			if (movie == null)
				return NotFoundError($"Không tìm thấy phim với ID: {id}");

			return OkResponse(movie);
		}
		catch (Exception ex)
		{
			return ErrorResponse("Đã có lỗi hệ thống", StatusCodes.Status500InternalServerError);
		}
	}

	[HttpPost]
	public async Task<IActionResult> Create([FromBody] CreateMovieDto dto)
	{
		try
		{
			// ✅ GỌI Service
			var movie = await _movieService.CreateAsync(dto);

			return CreatedResponse(movie, $"Tạo phim '{movie.Title}' thành công", $"/api/movies/{movie.Id}");
		}
		catch (Exception ex)
		{
			return ErrorResponse("Đã có lỗi hệ thống", StatusCodes.Status500InternalServerError);
		}
	}

	// ... các endpoint khác (Put, Patch, Delete)
}
```

---

#### **3. Tạo IMovieService (Interface)**

**Tệp: Services/Interfaces/IMovieService.cs**

```csharp
using API_TICKET_APPLICATION.Models.DTOs.Movies;
using API_TICKET_APPLICATION.Models;

namespace API_TICKET_APPLICATION.Services.Interfaces
{
	public interface IMovieService
	{
		// GET: Lấy tất cả phim (có phân trang)
		Task<PaginatedResponse<MovieResponseDto>> GetAllAsync(int pageNumber, int pageSize);

		// GET: Lấy phim theo ID
		Task<MovieResponseDto?> GetByIdAsync(int id);

		// POST: Tạo mới phim
		Task<MovieResponseDto> CreateAsync(CreateMovieDto dto);

		// PUT: Cập nhật toàn bộ phim
		Task<MovieResponseDto?> UpdateAsync(int id, UpdateMovieDto dto);

		// PATCH: Cập nhật một phần phim
		Task<MovieResponseDto?> PartialUpdateAsync(int id, Dictionary<string, object> updates);

		// DELETE: Xóa phim (Soft delete)
		Task<bool> DeleteAsync(int id);
	}

	// DTO Response cho phân trang
	public class PaginatedResponse<T>
	{
		public int PageNumber { get; set; }
		public int PageSize { get; set; }
		public int TotalCount { get; set; }
		public List<T> Data { get; set; } = new();
	}
}
```

---

#### **4. Tạo MovieService (Implementation)**

**Tệp: Services/Implementations/MovieService.cs**

```csharp
using API_TICKET_APPLICATION.Services.Interfaces;
using API_TICKET_APPLICATION.Models.DTOs.Movies;
using API_TICKET_APPLICATION.Models;

namespace API_TICKET_APPLICATION.Services.Implementations
{
	public class MovieService : IMovieService
	{
		private readonly IMovieRepository _repository;  // ✅ Inject Repository

		public MovieService(IMovieRepository repository)
		{
			_repository = repository ?? throw new ArgumentNullException(nameof(repository));
		}

		public async Task<PaginatedResponse<MovieResponseDto>> GetAllAsync(int pageNumber, int pageSize)
		{
			// ✅ Validation logic (Business Rules)
			if (pageNumber < 1) pageNumber = 1;
			if (pageSize < 1 || pageSize > 100) pageSize = 10;

			// ✅ Call Repository để lấy data
			var (movies, totalCount) = await _repository.GetAllAsync(pageNumber, pageSize);

			// ✅ Map Entity → DTO
			var dtos = movies.Select(m => new MovieResponseDto
			{
				Id = m.Id,
				Title = m.Title,
				Description = m.Description,
				Genre = m.Genre,
				DurationInMinutes = m.DurationInMinutes,
				PosterUrl = m.PosterUrl,
				ReleaseDate = m.ReleaseDate
			}).ToList();

			return new PaginatedResponse<MovieResponseDto>
			{
				PageNumber = pageNumber,
				PageSize = pageSize,
				TotalCount = totalCount,
				Data = dtos
			};
		}

		public async Task<MovieResponseDto?> GetByIdAsync(int id)
		{
			if (id <= 0)
				throw new ArgumentException("ID phải lớn hơn 0");

			var movie = await _repository.GetByIdAsync(id);

			if (movie == null)
				return null;

			return new MovieResponseDto
			{
				Id = movie.Id,
				Title = movie.Title,
				Description = movie.Description,
				Genre = movie.Genre,
				DurationInMinutes = movie.DurationInMinutes,
				PosterUrl = movie.PosterUrl,
				ReleaseDate = movie.ReleaseDate
			};
		}

		public async Task<MovieResponseDto> CreateAsync(CreateMovieDto dto)
		{
			// ✅ Validation (có thể gọi Validator)
			if (string.IsNullOrWhiteSpace(dto.Title))
				throw new ArgumentException("Tên phim không được để trống");

			// ✅ Create Entity từ DTO
			var movie = new Movie
			{
				Title = dto.Title,
				Description = dto.Description,
				Genre = dto.Genre,
				DurationInMinutes = dto.DurationInMinutes,
				PosterUrl = dto.PosterUrl,
				ReleaseDate = dto.ReleaseDate,
				IsDeleted = false
			};

			// ✅ Call Repository để save
			await _repository.AddAsync(movie);

			// ✅ Return DTO
			return new MovieResponseDto
			{
				Id = movie.Id,
				Title = movie.Title,
				Description = movie.Description,
				Genre = movie.Genre,
				DurationInMinutes = movie.DurationInMinutes,
				PosterUrl = movie.PosterUrl,
				ReleaseDate = movie.ReleaseDate
			};
		}

		// ... các method khác (Update, PartialUpdate, Delete)
	}
}
```

---

#### **5. Tạo IMovieRepository (Interface)**

**Tệp: Repositories/Interfaces/IMovieRepository.cs**

```csharp
using API_TICKET_APPLICATION.Models;

namespace API_TICKET_APPLICATION.Repositories.Interfaces
{
	public interface IMovieRepository
	{
		// Lấy tất cả phim
		Task<(List<Movie> movies, int totalCount)> GetAllAsync(int pageNumber, int pageSize);

		// Lấy phim theo ID
		Task<Movie?> GetByIdAsync(int id);

		// Thêm phim
		Task AddAsync(Movie movie);

		// Cập nhật phim
		Task UpdateAsync(Movie movie);

		// Xóa phim (Soft delete)
		Task DeleteAsync(Movie movie);

		// Kiểm tra tồn tại
		Task<bool> ExistsAsync(int id);

		// Lưu thay đổi
		Task SaveChangesAsync();
	}
}
```

---

#### **6. Tạo MovieRepository (Implementation)**

**Tệp: Repositories/Implementations/MovieRepository.cs**

```csharp
using API_TICKET_APPLICATION.Models;
using API_TICKET_APPLICATION.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API_TICKET_APPLICATION.Repositories.Implementations
{
	public class MovieRepository : IMovieRepository
	{
		private readonly AppDbContext _context;  // ✅ Chỉ Repository biết DbContext

		public MovieRepository(AppDbContext context)
		{
			_context = context ?? throw new ArgumentNullException(nameof(context));
		}

		public async Task<(List<Movie> movies, int totalCount)> GetAllAsync(int pageNumber, int pageSize)
		{
			// ✅ Pure Data Access Logic
			var query = _context.Movies.AsNoTracking()
				.Where(m => m.IsDeleted == false);

			var movies = await query
				.OrderBy(m => m.Title)
				.Skip((pageNumber - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

			var totalCount = await query.CountAsync();

			return (movies, totalCount);
		}

		public async Task<Movie?> GetByIdAsync(int id)
		{
			return await _context.Movies.AsNoTracking()
				.FirstOrDefaultAsync(m => m.Id == id && m.IsDeleted == false);
		}

		public async Task AddAsync(Movie movie)
		{
			await _context.Movies.AddAsync(movie);
			await SaveChangesAsync();
		}

		public async Task UpdateAsync(Movie movie)
		{
			_context.Movies.Update(movie);
			await SaveChangesAsync();
		}

		public async Task DeleteAsync(Movie movie)
		{
			movie.IsDeleted = true;
			_context.Movies.Update(movie);
			await SaveChangesAsync();
		}

		public async Task<bool> ExistsAsync(int id)
		{
			return await _context.Movies
				.AnyAsync(m => m.Id == id && m.IsDeleted == false);
		}

		public async Task SaveChangesAsync()
		{
			await _context.SaveChangesAsync();
		}
	}
}
```

---

#### **7. Tạo DTOs**

**Tệp: Models/DTOs/Movies/CreateMovieDto.cs**

```csharp
namespace API_TICKET_APPLICATION.Models.DTOs.Movies
{
	public class CreateMovieDto
	{
		public string Title { get; set; } = null!;
		public string? Description { get; set; }
		public string? Genre { get; set; }
		public int DurationInMinutes { get; set; }
		public string? PosterUrl { get; set; }
		public DateOnly ReleaseDate { get; set; }
	}
}
```

**Tệ: Models/DTOs/Movies/UpdateMovieDto.cs**

```csharp
namespace API_TICKET_APPLICATION.Models.DTOs.Movies
{
	public class UpdateMovieDto
	{
		public string Title { get; set; } = null!;
		public string? Description { get; set; }
		public string? Genre { get; set; }
		public int DurationInMinutes { get; set; }
		public string? PosterUrl { get; set; }
		public DateOnly ReleaseDate { get; set; }
	}
}
```

**Tệp: Models/DTOs/Movies/MovieResponseDto.cs**

```csharp
namespace API_TICKET_APPLICATION.Models.DTOs.Movies
{
	public class MovieResponseDto
	{
		public int Id { get; set; }
		public string Title { get; set; } = null!;
		public string? Description { get; set; }
		public string? Genre { get; set; }
		public int DurationInMinutes { get; set; }
		public string? PosterUrl { get; set; }
		public DateOnly ReleaseDate { get; set; }
	}
}
```

---

#### **8. Cập nhật Program.cs - Dependency Injection**

**Trước:**
```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
	options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();
builder.Services.AddOpenApi();
```

**Sau:**
```csharp
// ✅ Database
builder.Services.AddDbContext<AppDbContext>(options =>
	options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ✅ Repository Layer (Data Access)
builder.Services.AddScoped<IMovieRepository, MovieRepository>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ISeatRepository, SeatRepository>();
builder.Services.AddScoped<IShowtimeRepository, ShowtimeRepository>();
builder.Services.AddScoped<ITicketRepository, TicketRepository>();

// ✅ Service Layer (Business Logic)
builder.Services.AddScoped<IMovieService, MovieService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IUserService, UserService>();

// ✅ Validators (Optional)
builder.Services.AddScoped<IValidator<CreateMovieDto>, CreateMovieDtoValidator>();
builder.Services.AddScoped<IValidator<UpdateMovieDto>, UpdateMovieDtoValidator>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();
```

---

### **🔄 Luồng dữ liệu trước và sau:**

#### **TRƯỚC (Hiện tại - Monolithic):**
```
HTTP Request → MoviesController.GetAll()
					↓
			  _context.Movies.Where().ToListAsync()
					↓
			  HTTP Response (Movie entities)
```

**Vấn đề:**
- Controller biết DbContext
- Khó test (không thể mock DbContext)
- Khó reuse logic (lặp lại trong nhiều controller)

#### **SAU (3-Layer):**
```
HTTP Request
	↓
MoviesController.GetAll()
	└─ Inject: IMovieService
	↓
IMovieService.GetAllAsync()
	└─ Inject: IMovieRepository
	├─ Business Logic: Validation, Mapping
	↓
IMovieRepository.GetAllAsync()
	└─ Inject: AppDbContext
	├─ Data Access: Query, Save
	↓
Database Query Result
	↓
Return DTO → HTTP Response
```

**Lợi ích:**
- ✅ Tách biệt trách nhiệm (Separation of Concerns)
- ✅ Dễ test (Mock Service, Repository)
- ✅ Dễ reuse (Service dùng bởi nhiều controller)
- ✅ Dễ maintain (Thay đổi DB chỉ ảnh hưởng Repository)
- ✅ Dễ extend (Thêm caching, logging ở Service)

---

### **📊 So sánh vai trò từng file:**

| File | Trước (Monolithic) | Sau (3-Layer) |
|------|------------------|---------------|
| **AppBaseController** | DbContext + Response | Chỉ Response methods |
| **MoviesController** | DbContext + Logic + Response | Inject Service + Response |
| **MovieService** | ❌ KHÔNG CÓ | Business Logic + Validation + Mapping |
| **MovieRepository** | ❌ KHÔNG CÓ | Data Access + Query + Save |
| **DTOs** | ❌ KHÔNG CÓ | Request/Response models |

---

### **⏱️ Thời gian thực hiện:**

Để áp dụng 3-Layer cho toàn bộ dự án:

```
Entities: Movie, User, Booking, Seat, Showtime, Ticket

Công việc:
- [x] Tạo 6 IService interfaces + 6 implementations        → 6-8 giờ
- [x] Tạo 6 IRepository interfaces + 6 implementations      → 4-6 giờ
- [x] Tạo DTOs cho mỗi entity                              → 3-4 giờ
- [x] Cập nhật 6 Controllers (gọi Service thay vì _context) → 2-3 giờ
- [x] Cập nhật Program.cs (DI)                             → 1 giờ
- [x] Test tất cả endpoints                                 → 3-4 giờ

TỔNG CỘNG: 20-30 giờ (khoảng 3-4 ngày làm việc toàn thời gian)
```

---

### **✅ Kết luận Câu 3:**

**Tạo thêm những thư mục cụ thể:**

1. ✅ `Services/Interfaces/` - Interface cho business logic
2. ✅ `Services/Implementations/` - Implementation
3. ✅ `Repositories/Interfaces/` - Interface cho data access
4. ✅ `Repositories/Implementations/` - Implementation
5. ✅ `Models/DTOs/` - Data transfer objects
6. ✅ (Optional) `Models/Exceptions/` - Custom exceptions
7. ✅ (Optional) `Validators/` - Input validation rules

**Thay đổi vai trò:**

- **AppBaseController**: Chỉ chứa response methods, **BỎ `_context`**
- **MoviesController**: Inject `IMovieService`, **BỎ `_context`**, gọi service thay vì query trực tiếp
- **Program.cs**: Thêm DI cho Service + Repository

---

---

## 🎓 BẢNG TÓM TẮT TOÀN BỘ PHÂN TÍCH

| Câu hỏi | Kết luận |
|--------|---------|
| **1. Bằng chứ Visual** | ✅ Không có Services/, Repositories/, DTOs/ - Chứng minh từ file-system + code analysis |
| **2. Độ ưu tiên** | ❌ TÔI NHẦM - Nên là: Auth > Validation > Architecture (không phải High Priority ngay) |
| **3. Clean Architecture** | ✅ Tạo 5-7 thư mục, thay đổi vai trò AppBaseController + MoviesController + Program.cs |

---

## 🚀 HÀNH ĐỘNG TIẾP THEO

### **Tuần này (HIGH PRIORITY):**

- [ ] Hoàn thành CRUD cho 5 entity còn lại (User, Booking, Seat, Showtime, Ticket)
- [ ] Implement JWT Authentication
- [ ] Implement Role-based Authorization
- [ ] Thêm FluentValidation

### **Tuần sau (MEDIUM PRIORITY):**

- [ ] Tách Service Layer
- [ ] Tách Repository Pattern
- [ ] Thêm Unit Tests
- [ ] Upgrade Logging → Serilog

### **Tháng 2-3 (LOW PRIORITY):**

- [ ] Chi tiết Swagger docs
- [ ] Performance optimization

---

## 📌 GÁCHÚ

- Tôi không có ảnh chụp màn hình Solution Explorer từ VS của bạn, nên nếu bạn đã có Services/Repositories rồi, vui lòng cho tôi biết
- Phân tích này dựa trên **code audit chính xác**, không phải đoán mò
- Kiến trúc 3-Layer được khuyến nghị **không phải ngay bây giờ**, mà là **2-4 tuần nữa** khi dự án phức tạp hơn

---

**Document được tạo bằng Code Audit Tool**
**Date:** January 2025
**Project:** API_TICKET_APPLICATION
**Status:** Ready for Review
