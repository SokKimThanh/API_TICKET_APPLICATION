# API_TICKET_APPLICATION

API_TICKET_APPLICATION là một Web API cho hệ thống Đặt Vé Xem Phim (Ticket Management) được xây dựng bằng ASP.NET Core (.NET 10). Dự án bao gồm các thành phần chính: API endpoints (Controllers), middleware tùy chỉnh, Entity Framework Core DbContext, và các stored procedures cho thao tác CRUD trên bảng Movies.

---

## Tổng quan
- Framework: .NET 10
- Ngôn ngữ: C# 14
- Server: Kestrel
- HTTP port: 5924 (redirects) 
- HTTPS port: 5925
- Database: SQL Server (LocalDB) - `TicketManagementDB`

Mục tiêu: minh họa kiến trúc API, middleware security, Response wrapper pattern và tích hợp DbContext + Stored Procedures.

---

## Yêu cầu
- .NET 10 SDK
- SQL Server (LocalDB hoặc SQL Server Express)
- Visual Studio 2022/2026 hoặc VS Code
- (Tùy chọn) certificate dev: `dotnet dev-certs https --trust`

---

## Cấu trúc chính của project
- Program.cs — cấu hình Kestrel, middleware pipeline, DI
- Controllers/
  - AppBaseController.cs — Base Controller pattern (Response wrapper, utilities)
  - MoviesController.cs — CRUD endpoint cho Movies (sử dụng AppDbContext)
  - TestController.cs — endpoint test nội bộ
  - request_movies.http, request.http — tập các test request
- Models/
  - AppDbContext.cs — EF Core DbContext (sản phẩm scaffolding)
  - Movie.cs, Showtime.cs, ... — các entity model
- SQL/
  - movie-stored-procedures.sql — script tạo stored procedures cho Movies
- Docs/
  - STORED_PROCEDURES_GUIDE_VI.md
  - CRUD_WITH_STORED_PROCEDURES.md
  - BASE_CONTROLLER_PATTERN_GUIDE.md

---

## Cài đặt & chạy (Development)
1. Clone repository (nếu chưa có):
   ```bash
   git clone https://github.com/SokKimThanh/API_TICKET_APPLICATION.git
   ```

2. Mở project trong Visual Studio (F:\Design Web\ASP.NET PROJECTS\TICKETMANAGEMENT\API_TICKET_APPLICATION) hoặc dùng terminal:
   ```bash
   dotnet restore
   ```

3. Thiết lập certificate (nếu cần HTTPS local):
   ```bash
   dotnet dev-certs https --trust
   ```

4. Kiểm tra connection string trong `Models/AppDbContext.cs` hoặc chuyển vào `appsettings.json`:
   ```
   Server=(localdb)\\MSSQLLocalDB;Database=TicketManagementDB;Trusted_Connection=True;TrustServerCertificate=True;
   ```

5. Nếu bạn chưa tạo database / tables, dùng SQL Server Management Studio (SSMS) để tạo database `TicketManagementDB` và chạy script `SQL/movie-stored-procedures.sql` để tạo stored procedures và (nếu cần) tạo bảng `Movies`.

6. Chạy ứng dụng từ Visual Studio (F5) hoặc terminal:
   ```bash
   dotnet run --project API_TICKET_APPLICATION.csproj
   ```

Sau khi chạy, Kestrel sẽ lắng nghe:
- HTTP: http://localhost:5924 (sẽ redirect sang HTTPS)
- HTTPS: https://localhost:5925

---

## Middleware (Tóm tắt)
Ứng dụng đã cấu hình một số middleware tùy chỉnh (thứ tự quan trọng):
1. Exception Handling (DeveloperExceptionPage / ExceptionHandler + HSTS)
2. Circuit Breaker — buffer response, chuyển các 5xx thành 503 với message thân thiện
3. Input Validation — chặn XSS / SQLi theo pattern trên giá trị query đã decode
4. Secure Query Middleware — chỉ enforce khi param `secure` tồn tại; chặn khi giá trị khác `true`
5. Delay Request — mô phỏng delay (200ms)
6. Security Log events — log request/response
7. Elapsed time logging — stopwatch
8. UseHttpsRedirection (cấu hình HttpsPort = 5925)
9. Routing -> Authorization -> MapControllers

---

## Endpoints chính
- `GET /` → Hello World (MapGet)
- `GET /api/test` → endpoint test (TestController)
- MoviesController (CRUD): base URL: `/api/movies`
  - `GET /api/movies?pageNumber=&pageSize=` → Lấy danh sách (pagination)
  - `GET /api/movies/{id}` → Lấy phim theo ID
  - `POST /api/movies` → Tạo phim
  - `PUT /api/movies/{id}` → Cập nhật toàn bộ phim
  - `PATCH /api/movies/{id}` → Cập nhật một phần phim
  - `DELETE /api/movies/{id}` → Xóa phim

Responses đều được chuẩn hóa theo schema:
```json
{
  "success": true|false,
  "message": "...",
  "data": ... ,
  "timestamp": "ISO8601"
}
```

---

## Database & Stored Procedures
- DbContext: `Models/AppDbContext.cs` (đã scaffold)
- Stored procedures cho Movies có sẵn tại: `SQL/movie-stored-procedures.sql`
  - sp_GetAllMovies, sp_GetMovieById, sp_GetMovieByTitle
  - sp_CreateMovie, sp_UpdateMovie, sp_UpdateMoviePartial, sp_DeleteMovie

Hãy mở `movie-stored-procedures.sql` trong SSMS và chạy để tạo SPs. File này có comments bằng tiếng Việt và ví dụ test.

Lưu ý bảo mật và performance có đề xuất trong file: pagination cho bảng lớn, index trên Title, tránh hard-delete nếu liên quan FK, sử dụng QUOTENAME + sp_executesql cho dynamic SQL.

---

## Testing
- REST client files: `Controllers/request_movies.http`, `Controllers/request.http` — mở trong Visual Studio và chạy từng request.
- Postman: import file `request_movies.http` (hoặc tự tạo collection) để test các case CRUD, validation, secure flag, performance.
- Một số client có thể không follow redirect từ HTTP → HTTPS. Kiểm tra header `Location` nếu cần.

---

## Base Controller Pattern
- `Controllers/AppBaseController.cs` là abstract base controller dùng cho toàn bộ controllers:
  - quản lý `_context` tập trung
  - chuẩn hóa response: `OkResponse`, `ErrorResponse`, `CreatedResponse` và shortcut helpers
  - helper cho JWT claims: `GetUserId()`, `GetUserEmail()`, `IsUserInRole()`

Sử dụng pattern này giúp giảm code lặp, thống nhất response và dễ mở rộng.

---

## Debugging & Troubleshooting
- Lỗi "No connection could be made because the target machine actively refused it (localhost:5924)": đảm bảo ứng dụng đang chạy và Kestrel bind cổng 5924.
- Nếu UseHttpsRedirection không redirect đúng port, kiểm tra `builder.Services.AddHttpsRedirection(options => options.HttpsPort = 5925)` đã được cấu hình.
- Nếu middleware không chặn payload đã URL-encode: middleware hiện sử dụng `Request.Query` (đã decode) để kiểm tra.

---

## Contributing
- Fork & PR theo quy trình GitHub. Viết unit tests nếu thay đổi logic business.
- Code style: theo C# conventions, dùng async/await cho IO, tránh buffering lớn trên middleware cho responses lớn.

---

## License
- Mặc định: MIT (nếu muốn thay đổi, cập nhật file LICENSE)

---

## Liên hệ
- Repository: https://github.com/SokKimThanh/API_TICKET_APPLICATION
- Author: Project repository owner

---

Cần mình cập nhật README thêm phần cụ thể (ví dụ hướng dẫn migrate DB, script seed dữ liệu, hoặc postman collection) không?