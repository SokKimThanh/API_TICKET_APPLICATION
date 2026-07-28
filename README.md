# API Ticket Application

Một hệ thống **Database-First Movie Booking API** chuyên nghiệp được xây dựng trên nền tảng **ASP.NET Core (.NET 10)**, tích hợp cơ chế phân quyền bảo mật (role-based authorization), quy trình đặt vé an toàn qua database transaction, và tài liệu hướng dẫn chuẩn OpenAPI/Swagger tương tác trực quan.

**Phiên bản hiện tại:** `v1.2.0-security-openapi` (Gia cố bảo mật & Tối ưu hóa Swagger Schema) 🛡️⚡

---

## Tổng quan dự án (Project Overview)

**API Ticket Application** là hệ thống quản lý đặt vé xem phim ở backend, đảm bảo kiểm soát chặt chẽ thông tin Người dùng, Phim, Lịch chiếu, Ghế ngồi, Đơn đặt vé và Vé xem phim với độ an toàn dữ liệu mức doanh nghiệp. Hệ thống ngăn chặn việc đặt trùng ghế thông qua cơ chế Transaction của EF Core và áp dụng phân quyền nghiêm ngặt giữa Quản trị viên (Admin) và Người dùng thường (User) thông qua cơ chế kiểm tra quyền sở hữu bản ghi.

### Đặc điểm kỹ thuật nổi bật
- **Framework:** .NET 10 với C# 14
- **Database:** SQL Server (LocalDB / Express) sử dụng Entity Framework Core (Database-First)
- **Architecture:** Kiến trúc REST API chuẩn hóa định dạng phản hồi (Standardized Response Models)
- **Security:** Xác thực bằng JWT, phân quyền theo vai trò (Role-based), bảo toàn giao dịch dữ liệu (Database Transactions)
- **API Documentation:** OpenAPI gốc tích hợp Swagger UI thân thiện
- **Testing:** Bộ kịch bản `.http` phân tách rõ ràng để kiểm thử tự động trực tiếp trên IDE

---

## Tiến độ phát triển (Phase 1 - MVP Completed) ✅

### Các tính năng đã hoàn thành

#### 1. **Chuẩn hóa phản hồi & Định danh Schema tự động**
- ✅ `ResponseModel<T>` — Lớp bọc hợp nhất mọi dữ liệu phản hồi từ API.
- ✅ `PagedData<T>` — Cấu trúc dữ liệu phân trang chuẩn hóa cho mọi Controller.
- ✅ **Định danh Schema tự động cho Generic Model (Đã phẳng hóa - Flattened):**
  Hệ thống cấu hình bộ sinh ID Schema tự động (`options.AddSchemaTransformer` trong `Program.cs` thông qua `OpenApiSchemaHelper`) nhằm phẳng hóa và chuẩn hóa các kiểu dữ liệu generic phức tạp. Điều này giúp Swagger UI hiển thị giao diện cực kỳ sạch sẽ, loại bỏ các tầng lồng nhau không cần thiết, giúp các công cụ sinh code ở phía Front-end dễ dàng sinh mã nguồn (TypeScript client, v.v.).

  **Cơ chế phẳng hóa và đặt tên Schema cụ thể:**
  - `ResponseModel<PagedData<T>>` ➔ **`PagedResponseOf{T}`** (Ví dụ: `PagedResponseOfMovie`)
  - `ResponseModel<T>` ➔ **`ResponseOf{T}`** (Ví dụ: `ResponseOfMovie`, `ResponseOfUserResponseDto`, `ResponseOfLoginResponseDto`, `ResponseOfBooking`, `ResponseOfTicket`)
  - `PagedData<T>` ➔ **`PagedDataOf{T}`** (Ví dụ: `PagedDataOfMovie`)
  - Các lỗi đầu vào được trả về theo dạng: **`ResponseOfObject`**

- ✅ **Mô tả Schema động tiếng Việt (Dynamic Schema Descriptions):**
  Khi chạy trong môi trường **Development**, hệ thống sẽ tự động sinh mô tả tiếng Việt (`schema.Description`) chi tiết cho từng Schema generic dựa trên kiểu dữ liệu gốc lồng nhau bên trong. Ví dụ:
  - `ResponseModel<PagedData<Movie>>` sẽ nhận được mô tả: *"Mô hình phản hồi API chuẩn chứa dữ liệu phân trang phục vụ cho model Movie (chỉ dùng cho môi trường Development)"*.
  - Giúp lập trình viên phía Front-end nắm bắt rõ ràng cấu trúc dữ liệu mong đợi mà không cần lục tìm mã nguồn Backend.

- ✅ Các mã trạng thái HTTP chuẩn hóa: `200 OK`, `201 Created`, `400 Bad Request`, `401 Unauthorized`, `403 Forbidden`, `404 Not Found`, `500 Internal Server Error`.

#### 2. **Giao dịch Database & Quy trình đặt vé an toàn**
- ✅ Hỗ trợ Transaction toàn vẹn khi tạo đơn đặt vé (`Bookings`) và phát sinh vé tương ứng (`Tickets`).
- ✅ Ngăn chặn tuyệt đối hiện tượng đặt trùng ghế (double-booking) thông qua cơ chế kiểm tra trạng thái khả dụng của ghế trong cùng một phiên giao dịch khép kín.

#### 3. **Cơ chế phân quyền bảo mật cao**
- ✅ Phân định rõ quyền truy cập Admin và User trên từng endpoint nhạy cảm.
- ✅ Kiểm tra quyền sở hữu bản ghi chặt chẽ (Người dùng chỉ được xem/hủy đơn hàng của chính mình).
- ✅ Tích hợp JWT Bearer Token đi kèm với các claim về quyền hạn cụ thể.

#### 4. **Tài liệu Swagger/OpenAPI**
- ✅ Khởi tạo tài liệu đầy đủ các Endpoint kèm theo Request/Response model trực quan.
- ⚠️ **Ràng buộc HTTPS:** Swagger UI (`/swagger`) và OpenAPI Document JSON (`/openapi/v1.json`) **chỉ hoạt động và truy cập được qua giao thức bảo mật HTTPS** (`https://localhost:5925`). Hệ thống không hỗ trợ chạy và kiểm tra Swagger qua giao thức HTTP thường.
- ✅ Nút xác thực trực quan (Authorize padlock button) được tích hợp trực tiếp vào Swagger UI thông qua OpenAPI Document Transformers để đính kèm Token JWT dễ dàng.

#### 5. **Hạ tầng kiểm thử tự động hóa**
- ✅ Hệ thống các file kịch bản `.http` lưu tại thư mục `RequestHTTP/`, phân loại theo từng nghiệp vụ (Auth, Movies, Showtimes, Bookings, Tickets).
- ✅ Cấu hình sẵn cơ chế lưu biến Token tự động sau khi đăng nhập thành công để tái sử dụng cho các API tiếp theo.

---

## Cấu hình Giao thức & CORS (CORS & Protocol Constraints)

### 1. Ràng buộc về Giao thức HTTPS
- **Cổng kết nối:** Kestrel được cấu hình lắng nghe trên hai cổng:
  - **HTTP:** `http://localhost:5924` (Tự động chuyển hướng về HTTPS)
  - **HTTPS:** `https://localhost:5925` (Cổng giao tiếp chính, có chứng chỉ SSL/TLS)
- **Swagger/OpenAPI:** Do tính chất bảo mật của hệ thống JWT, Swagger UI **chỉ hoạt động qua HTTPS**. Mọi cố gắng truy cập Swagger qua HTTP sẽ bị chặn hoặc chuyển hướng lỗi.

### 2. Hiện trạng Cấu hình CORS (Cross-Origin Resource Sharing)
- ⚠️ **Hiện trạng:** **CORS chưa được cấu hình (mặc định tắt)** trong `Program.cs`.
- **Tác động:** Trình duyệt web sẽ chặn các cuộc gọi API trực tiếp từ ứng dụng frontend (chạy trên các origin khác như React, Angular hoặc Vue tại `http://localhost:3000`).
- **Cách kích hoạt CORS (khi cần tích hợp Frontend):**
  Để cho phép trình duyệt truy cập API từ ứng dụng bên ngoài, bạn cần thêm đoạn mã sau vào `Program.cs`:
  ```csharp
  // 1. Khai báo dịch vụ CORS (Thêm vào phần Đăng ký Dịch vụ trước builder.Build())
  builder.Services.AddCors(options =>
  {
      options.AddPolicy("AllowFrontend", policy =>
      {
          policy.WithOrigins("http://localhost:3000") // Thay đổi theo origin frontend của bạn
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
      });
  });

  // 2. Kích hoạt Middleware (Thêm vào phần Cấu hình Pipeline, sau app.UseRouting() và trước app.UseAuthorization())
  app.UseCors("AllowFrontend");
  ```

---

## Kiến trúc Cơ sở Dữ liệu hiện tại

Hệ thống quản lý **6 thực thể cốt lõi** có quan hệ chặt chẽ:

| Thực thể | Mục đích | Các trường khóa/thông tin chính |
|--------|---------|-----------|
| **Users** | Quản lý tài khoản (Admin, Customer) | UserId, Email, PasswordHash, Role, CreatedAt |
| **Movies** | Danh mục phim | MovieId, Title, Description, Duration, ReleaseDate |
| **Showtimes** | Lịch chiếu phim tại các rạp | ShowtimeId, MovieId, StartTime, EndTime, TotalSeats |
| **Seats** | Trạng thái ghế ngồi của từng lịch chiếu | SeatId, ShowtimeId, SeatNumber, IsAvailable |
| **Bookings** | Bản ghi đơn đặt vé của khách hàng | BookingId, UserId, ShowtimeId, BookingDate, TotalPrice |
| **Tickets** | Vé chi tiết tương ứng với từng ghế trong đơn | TicketId, BookingId, SeatId, IsUsed |

### Quan hệ giữa các bảng
- `Users` (1) ➔ (Nhiều) `Bookings`
- `Movies` (1) ➔ (Nhiều) `Showtimes`
- `Showtimes` (1) ➔ (Nhiều) `Seats` & (Nhiều) `Bookings`
- `Bookings` (1) ➔ (Nhiều) `Tickets`
- `Seats` (1) ➔ (Nhiều) `Tickets`

---

## Danh sách các Endpoint chính (Phase 1)

### Xác thực tài khoản (Authentication)
- `POST /api/auth/register` — Đăng ký tài khoản người dùng mới:
  - Hỗ trợ truyền thuộc tính `Role` tùy chọn (Mặc định vai trò là `"Customer"`, chuẩn hóa chữ hoa/thường để khớp check constraint).
  - Tích hợp cơ chế **chống nâng quyền trái phép (Privilege Escalation Prevention)**: Chỉ cho phép đăng ký tài khoản với vai trò `"Admin"` nếu hệ thống chưa có bất kỳ Admin nào hoạt động (Bootstrap Admin) HOẶC người gửi yêu cầu đã xác thực thành công và có quyền `"Admin"`.
  - Được kiểm chứng kỹ càng qua bộ kiểm thử tích hợp `request_auth.http`.
- `POST /api/auth/login` — Đăng nhập, nhận Token JWT phục vụ cho các yêu cầu tiếp theo.

### Quản lý Phim (Movies)
- `GET /api/movies?pageNumber={page}&pageSize={size}` — Lấy danh sách phim có phân trang (Tất cả mọi người).
- `GET /api/movies/{id}` — Lấy chi tiết phim theo ID (Tất cả mọi người).
- `POST /api/movies` — Thêm mới phim (Yêu cầu quyền **Admin**).
- `PUT /api/movies/{id}` — Cập nhật thông tin phim (Yêu cầu quyền **Admin**).
- `PATCH /api/movies/{id}` — Cập nhật một phần thông tin phim (Yêu cầu quyền **Admin**).
- `DELETE /api/movies/{id}` — Xóa phim (Yêu cầu quyền **Admin**).

### Quản lý Lịch chiếu (Showtimes)
- `GET /api/showtimes?movieId={id}&pageNumber={page}` — Lấy danh sách lịch chiếu theo phim.
- `GET /api/showtimes/{id}` — Xem chi tiết lịch chiếu cụ thể.
- `POST /api/showtimes` — Tạo lịch chiếu mới và tự động khởi tạo danh sách ghế trống tương ứng (Yêu cầu quyền **Admin**).
- `PUT /api/showtimes/{id}` — Cập nhật thông tin lịch chiếu (Yêu cầu quyền **Admin**).
- `DELETE /api/showtimes/{id}` — Xóa lịch chiếu (Yêu cầu quyền **Admin**).

### Quản lý Ghế ngồi (Seats)
- `GET /api/seats?showtimeId={id}` — Lấy toàn bộ danh sách ghế (kèm trạng thái trống/đã đặt) của một lịch chiếu.
- `GET /api/seats/{id}` — Xem chi tiết thông tin một ghế cụ thể.

### Quản lý Đặt vé & Vé (Bookings & Tickets)
- `POST /api/bookings` — Thực hiện đặt vé (Chạy transaction, tự động khóa ghế và tạo vé tương ứng).
- `GET /api/bookings/{id}` — Xem thông tin chi tiết đơn đặt vé (Chỉ cho phép Admin hoặc chính chủ đơn đặt vé).
- `GET /api/bookings/user/{userId}` — Xem lịch sử đặt vé của người dùng (Yêu cầu Admin hoặc chính người dùng đó).
- `GET /api/tickets/{bookingId}` — Xem danh sách vé đi kèm của một đơn hàng (Chỉ cho phép Admin hoặc chính chủ vé).

### Định dạng dữ liệu phản hồi chung (JSON Response Format)
Mọi phản hồi từ hệ thống đều được đồng bộ theo cấu trúc:
```json
{
  "success": true,
  "message": "Operation completed successfully",
  "data": { /* Dữ liệu chi tiết trả về */ },
  "timestamp": "2024-12-15T10:30:45Z",
  "statusCode": 200
}
```

---

## Hướng dẫn Cài đặt & Khởi chạy

### Điều kiện tiên quyết
- **.NET 10 SDK** — [Tải về tại đây](https://dotnet.microsoft.com/download)
- **SQL Server** — LocalDB hoặc Express Edition
- **Visual Studio 2026** hoặc **VS Code**

### Các bước khởi chạy nhanh

1. **Tải mã nguồn về máy:**
   ```bash
   git clone https://github.com/SokKimThanh/API_TICKET_APPLICATION.git
   cd API_TICKET_APPLICATION
   ```

2. **Khôi phục các thư viện phụ thuộc (Restore packages):**
   ```bash
   dotnet restore
   ```

3. **Cấu hình chuỗi kết nối Database:**
   - Mở file `appsettings.json` và cấu hình lại mục Connection String phù hợp với máy của bạn:
     ```json
     "ConnectionStrings": {
       "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=TicketManagementDB;Trusted_Connection=True;TrustServerCertificate=True;"
     }
     ```

4. **Tạo Database và khởi tạo dữ liệu mẫu:**
   ```bash
   dotnet ef database update
   ```

5. **Kích hoạt chứng chỉ HTTPS nội bộ (Nếu chưa cài đặt):**
   ```bash
   dotnet dev-certs https --trust
   ```

6. **Khởi chạy ứng dụng:**
   ```bash
   dotnet run
   ```

   Hệ thống sẽ chạy và lắng nghe tại các địa chỉ:
   - **HTTP (Redirect):** `http://localhost:5924`
   - **HTTPS (Main URL):** `https://localhost:5925`
   - **Swagger UI (HTTPS Only):** `https://localhost:5925/swagger/index.html`

---

## Hướng dẫn Kiểm thử API (API Testing)

### 1. Sử dụng công cụ REST Client của IDE (File .http)
Thư mục `RequestHTTP/` chứa toàn bộ các kịch bản test có sẵn. Bạn chỉ cần mở các file này trong Visual Studio hoặc VS Code (đã cài extension REST Client) và bấm **Send Request**:

```http
### Lấy danh sách phim có phân trang
GET https://localhost:5925/api/movies?pageNumber=1&pageSize=10
Authorization: Bearer {token_lay_tu_auth}
```

### 2. Sử dụng Postman
1. Import các file `.http` từ thư mục `RequestHTTP/` vào Postman dưới dạng collection.
2. Gán biến Token vào Header `Authorization: Bearer <your_token>` để chạy các API yêu cầu xác thực.

### 3. Sử dụng cURL
```bash
# Gọi lấy danh sách phim (Bỏ qua kiểm tra chứng chỉ SSL cục bộ bằng tham số -k)
curl -k -X GET "https://localhost:5925/api/movies?pageNumber=1&pageSize=10" \
  -H "Authorization: Bearer <YOUR_JWT_TOKEN>"
```

---

## Cấu trúc thư mục dự án

```
API_TICKET_APPLICATION/
├── Controllers/              # Điều hướng API & Xử lý nghiệp vụ chính
│   ├── AppBaseController.cs  # Controller cơ sở, chứa cấu trúc ResponseModel thống nhất
│   ├── AuthController.cs     # API Đăng ký, đăng nhập và cấp phát JWT
│   ├── MoviesController.cs   # API CRUD Phim có phân trang và kiểm tra dữ liệu đầu vào
│   ├── ShowtimesController.cs # API Lịch chiếu phim & Tự động tạo cấu trúc ghế trống
│   ├── SeatsController.cs    # API Truy xuất ghế ngồi trống/bận của lịch chiếu
│   ├── BookingsController.cs # API Đặt vé tích hợp transactional đảm bảo an toàn dữ liệu
│   └── TicketsController.cs  # API Tra cứu và sử dụng vé của khách hàng
├── Models/                   # Thực thể dữ liệu & Kết nối DB
│   ├── AppDbContext.cs       # Cấu hình Entity Framework Core DbContext
│   ├── User.cs, Movie.cs, Showtime.cs, Seat.cs, Booking.cs, Ticket.cs
│   └── ResponseModel.cs, PagedData.cs  # Các DTO chuẩn hóa cấu trúc dữ liệu phản hồi
├── Services/                 # Các dịch vụ dùng chung
│   ├── BookingService.cs     # Nghiệp vụ đặt vé có transaction đảm bảo atomic
│   └── AuthService.cs        # Logic băm mật khẩu, so khớp và tạo token JWT
├── RequestHTTP/              # Chứa kịch bản test API phân tách theo nghiệp vụ
│   ├── movies.http
│   ├── showtimes.http
│   ├── bookings.http
│   ├── tickets.http
│   └── auth.http
├── Migrations/               # Lịch sử cập nhật Database của EF Core
├── Program.cs                # Điểm khởi chạy ứng dụng, cấu hình Kestrel, JWT, và OpenAPI
├── appsettings.json          # Tệp cấu hình môi trường
└── README.md                 # Tài liệu này
```

---

## Kế hoạch Phát triển Tiếp theo (Phase 2 - Multi-Theater Expansion)

Trong Phase 2, hệ thống sẽ được mở rộng để hỗ trợ mô hình **chuỗi nhiều cụm rạp** với các cải tiến quan trọng:

#### 1. Các thực thể mới bổ sung
- **Cinemas** — Quản lý thông tin cụm rạp (Tên rạp, vị trí địa lý, thành phố, liên hệ).
- **CinemaHalls** — Quản lý phòng chiếu chi tiết trong từng cụm rạp (Tên phòng, sức chứa, cấu hình sơ đồ ghế ngồi).

#### 2. Cập nhật luồng nghiệp vụ
- **Showtimes** sẽ được liên kết trực tiếp với phòng chiếu (`CinemaHalls`) thay vì phòng chiếu chung chung.
- Hệ thống sơ đồ **Seats** sẽ tự động sinh theo cấu hình của từng phòng chiếu cụ thể.
- Hỗ trợ tìm kiếm lịch chiếu tiện lợi theo cụm rạp, thành phố hoặc khu vực địa lý.

---

## Khắc phục sự cố thường gặp (Troubleshooting)

| Sự cố | Nguyên nhân phổ biến | Giải pháp khắc phục |
|-------|----------|-------------------|
| Lỗi từ chối kết nối trên `localhost:5924` | Ứng dụng chưa được chạy hoặc Kestrel bị xung đột cổng | Chạy lệnh `dotnet run` và kiểm tra log console để xem cổng đang lắng nghe |
| Lỗi Chứng chỉ SSL/TLS khi gọi HTTPS | Máy khách chưa tin tưởng chứng chỉ dev cục bộ của .NET | Chạy lệnh `dotnet dev-certs https --trust` để kích hoạt chứng chỉ |
| Lỗi Database không tìm thấy | Chuỗi kết nối SQL Server bị sai hoặc chưa khởi chạy DB | Kiểm tra lại mục Connection String trong `appsettings.json` và chạy lệnh `dotnet ef database update` |
| Phản hồi 401 Unauthorized | Chưa truyền hoặc truyền sai định dạng Token JWT trong Header | Đảm bảo truyền header đúng dạng: `Authorization: Bearer <token_cua_ban>` |
| Phản hồi 403 Forbidden | Tài khoản hiện tại không có quyền truy cập | Tài khoản cần có claim role là `Admin` để thực hiện hành động này |
| Không thể tải trang Swagger UI | Truy cập qua HTTP thông thường | Đảm bảo sử dụng đường dẫn HTTPS: `https://localhost:5925/swagger` |

---

## Lịch sử thay đổi (Changelog)

### [2026-07-28] — v1.2.0-security-openapi (Security Hardening & Swagger Schema Optimization) 🛡️⚡
- **Gia cố bảo mật Endpoint Register (Chống nâng quyền trái phép):**
  - Bổ sung trường tùy chọn `Role` vào `RegisterRequest` kèm theo validation chặt chẽ trong `UserValidator`.
  - Chặn đứng lỗ hổng leo thang đặc quyền (Privilege Escalation) bằng cách chỉ cho phép tạo tài khoản `Admin` trong giai đoạn Bootstrap (chưa có Admin nào hoạt động trong DB) hoặc khi yêu cầu được gửi từ một `Admin` đã đăng nhập.
  - Chuẩn hóa đầu vào của Role thành `Customer` hoặc `Admin` để tránh lỗi Check Constraint của cơ sở dữ liệu.
  - Cập nhật file kiểm thử tích hợp `request_auth.http` chứng minh các kịch bản chặn nâng quyền và đăng ký thành công.
- **Tối ưu hóa OpenAPI/Swagger Schema Naming (Flattened Naming):**
  - Tích hợp `OpenApiSchemaHelper` định danh lại Schema cho các kiểu Generic lồng nhau: phẳng hóa từ `ResponseModel<PagedData<Movie>>` thành `PagedResponseOfMovie`, hay `ResponseModel<Movie>` thành `ResponseOfMovie`.
  - Sinh mô tả chi tiết bằng tiếng Việt sống động cho từng schema generic dựa trên các tham số kiểu (Generic Arguments) thực tế khi chạy trong môi trường `Development`.
- **Đồng bộ hóa kiểm thử & Sửa lỗi:**
  - Toàn bộ 5 kịch bản kiểm thử `.http` đã được cập nhật đồng bộ và xác nhận chạy thành công trên môi trường cục bộ.

### [2024-12-16] — Cập nhật bảo mật & OpenAPI nâng cao (Phiên bản v1.1.0-mvp)
- **Cải tiến tài liệu OpenAPI:**
  - Tích hợp bộ sinh ID Schema tự động cho generic model (`AddSchemaTransformer`), hiển thị giao diện schema trực quan chuẩn dạng `ResponseModelOfPagedDataOfMovie`, v.v.
  - Tích hợp Authorize padlock trực tiếp vào Swagger UI của .NET 10 qua `IOpenApiDocumentTransformer`.
- **Ràng buộc an toàn:**
  - Quy định rõ ràng Swagger/OpenAPI chỉ khả dụng qua kết nối HTTPS bảo mật (`https://localhost:5925`).
  - Làm rõ trạng thái cấu hình CORS (Mặc định tắt và cung cấp hướng dẫn cấu hình khi tích hợp Frontend).
- **Ngăn ngừa vòng lặp vô hạn (Infinite Reference Loop):**
  - Tích hợp bộ lọc Contract Modifier `IgnoreVirtualPropertiesModifier` loại bỏ đệ quy các thuộc tính virtual của EF Core trong quá trình tuần tự hóa JSON và sinh OpenAPI Schema.

### [2024-12-15] — Phát hành Phase 1 MVP
- Hoàn thiện cấu trúc phản hồi chuẩn `ResponseModel<T>` và `PagedData<T>` trên toàn bộ Controller.
- Triển khai quy trình đăng ký, đăng nhập và phân quyền JWT.
- Thiết lập quy trình đặt vé an toàn qua Database Transaction, ngăn chặn đặt trùng ghế.
- Cung cấp bộ kịch bản test `.http` hoàn chỉnh trong thư mục `RequestHTTP/`.

---

## Bản quyền & Đóng góp (License & Contributing)

Dự án này được phân phối dưới giấy phép **MIT License**. Mọi đóng góp, báo lỗi xin vui lòng tạo Issue hoặc gửi Pull Request thông qua GitHub Repository của dự án.

---

**Được phát triển với ❤️ dành cho các ứng dụng RESTful API hiện đại**
