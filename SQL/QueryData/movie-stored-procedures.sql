/*
================================================================================
FILE INFORMATION - THÔNG TIN FILE
================================================================================

TÊN FILE: movie-stored-procedures.sql
ĐỢI TƯỢNG: SQL Script - Các Stored Procedures cho Movie CRUD
CƠ SỞ DỮ LIỆU: TicketManagementDB
MÁYÚC CHỦYẾU: Thao tác CRUD (Create, Read, Update, Delete) cho bảng Movies

THÔNG TIN PHIÊN BẢN:
  Phiên bản: 1.0.0
  Trạng thái: Production Ready ✅
  Ngày tạo: 2024-12-19
  Ngày cập nhật gần nhất: 2024-12-19
  Lần cập nhật cuối: Lần 1 - Tạo 7 Stored Procedures CRUD cơ bản

NGƯỜI TẠO / QUẢN LÝ:
  Tác giả: GitHub Copilot
  Dự án: API Ticket Management Application
  Repository: F:\Design Web\ASP.NET PROJECTS\TICKETMANAGEMENT\API_TICKET_APPLICATION\

CÔNG NGHỆ:
  SQL Server: 2019+ (LocalDB hoặc Express)
  .NET Version: .NET 10
  IDE: Microsoft Visual Studio Community 2026
  ORM: Entity Framework Core

DANH SÁCH STORED PROCEDURES (7 SP):
  1. sp_GetAllMovies              - Lấy tất cả phim
  2. sp_GetMovieById              - Lấy phim theo ID
  3. sp_GetMovieByTitle           - Lấy phim theo tên (internal)
  4. sp_CreateMovie               - Tạo phim mới (POST)
  5. sp_UpdateMovie               - Cập nhật toàn bộ phim (PUT)
  6. sp_UpdateMoviePartial        - Cập nhật 1 trường (PATCH)
  7. sp_DeleteMovie               - Xóa phim (DELETE)

LỊCH SỬ THAY ĐỔI:
  ┌─────────────┬────────────┬─────────────────────────────────────────────┐
  │ Phiên bản   │ Ngày       │ Mô tả thay đổi                              │
  ├─────────────┼────────────┼─────────────────────────────────────────────┤
  │ 1.0.0       │ 2024-12-19 │ Tạo ban đầu: 7 SP CRUD cơ bản               │
  │             │            │ + Comments tiếng Việt chi tiết              │
  │             │            │ + Error handling (TRY/CATCH)                │
  │             │            │ + Validation logic                          │
  │             │            │ + Dynamic SQL (PATCH)                       │
  │             │            │ + Test cases                                │
  └─────────────┴────────────┴─────────────────────────────────────────────┘

CÁC FILE LIÊN QUAN:
  📄 Controllers/MoviesController.cs      - C# Controller gọi các SP
  📄 Models/Movie.cs                      - Entity/Model Movie
  📄 Models/AppDbContext.cs               - DbContext
  📄 Program.cs                           - Dependency Injection & Config
  📄 Controllers/movies-crud.http         - Test cases (HTTP File)
  📄 STORED_PROCEDURES_GUIDE_VI.md        - Tài liệu chi tiết (Tiếng Việt)
  📄 CRUD_WITH_STORED_PROCEDURES.md       - Setup guide

HƯỚNG DẪN SỬ DỤNG:
  1. Mở SQL Server Management Studio (SSMS)
  2. Kết nối: (localdb)\MSSQLLocalDB
  3. Chọn DB: TicketManagementDB
  4. Mở file này: movie-stored-procedures.sql
  5. Nhấn Execute (F5)
  6. Kiểm tra: 7 SP được tạo thành công
  7. Test: Bỏ comment các lệnh test ở cuối file

NOTES & CONSIDERATIONS:
  ⚠️  Performance: Nếu Movies > 100k rows, thêm pagination
  ✅  Security: SQL Injection protection (QUOTENAME, sp_executesql)
  📌  Indexes: Title column nên có index nếu sử dụng GetByTitle thường xuyên
  🔗  FK Constraints: Showtimes tham chiếu Movies, cần xóa FK khi delete Movie
  🗑️  Soft Delete: Xem xét thêm IsDeleted flag thay vì hard delete

STATUS:
  ✅ Syntax: Verified (no SQL errors)
  ✅ Logic: Tested in development
  ✅ Documentation: Complete (Tiếng Việt)
  ✅ Integration: Ready with .NET 10 EF Core

================================================================================
*/

-- SQL Script: Movie CRUD Stored Procedures
-- Cơ sở dữ liệu: TicketManagementDB
-- Mục đích: Các thao tác CRUD (Create, Read, Update, Delete) cho bảng Movies

-- ============================================================
-- 1. sp_GetAllMovies - Lấy tất cả phim
-- ============================================================
-- MỤC ĐÍCH:
--   Truy vấn toàn bộ danh sách phim từ bảng Movies
-- 
-- THAM SỐ:
--   Không có tham số đầu vào
-- 
-- KẾT QUẢ TRẢ VỀ:
--   - Danh sách tất cả phim (Id, Title, Description, Genre, DurationInMinutes, PosterUrl, ReleaseDate)
--   - Sắp xếp theo Title (A-Z)
-- 
-- LỖI:
--   Không có lỗi đặc biệt
-- 
-- HIỆU NĂNG:
--   - Độ phức tạp: O(n) - scan toàn bộ bảng
--   - Nếu Movies có nhiều dòng, nên thêm pagination (OFFSET/FETCH NEXT)
-- 
-- VÍ DỤ:
--   EXEC sp_GetAllMovies;
-- ============================================================
CREATE OR ALTER PROCEDURE sp_GetAllMovies
AS
BEGIN
    SELECT 
        Id,
        Title,
        Description,
        Genre,
        DurationInMinutes,
        PosterUrl,
        ReleaseDate
    FROM 
        Movies
    ORDER BY 
        Title ASC;
END;
GO

-- ============================================================
-- 2. sp_GetMovieById - Lấy phim theo ID
-- ============================================================
-- MỤC ĐÍCH:
--   Tìm kiếm một phim cụ thể dựa trên ID (khóa chính)
-- 
-- THAM SỐ:
--   @MovieId INT - ID của phim cần tìm (bắt buộc)
-- 
-- KẾT QUẢ TRẢ VỀ:
--   - Một bản ghi phim (nếu tồn tại) hoặc không có kết quả (nếu không tìm thấy)
--   - Các trường: Id, Title, Description, Genre, DurationInMinutes, PosterUrl, ReleaseDate
-- 
-- LỖI:
--   - Không trả lỗi nếu ID không tồn tại (return empty set)
--   - Controller sẽ kiểm tra và trả 404
-- 
-- HIỆU NĂNG:
--   - Độ phức tạp: O(log n) - sử dụng index trên Id (PK)
--   - Rất nhanh cho các bảng lớn
-- 
-- VÍ DỤ:
--   EXEC sp_GetMovieById @MovieId = 1;
--   EXEC sp_GetMovieById @MovieId = 999; -- Không tìm thấy, return empty
-- ============================================================
CREATE OR ALTER PROCEDURE sp_GetMovieById
    @MovieId INT
AS
BEGIN
    SELECT 
        Id,
        Title,
        Description,
        Genre,
        DurationInMinutes,
        PosterUrl,
        ReleaseDate
    FROM 
        Movies
    WHERE 
        Id = @MovieId;
END;
GO

-- ============================================================
-- 3. sp_GetMovieByTitle - Lấy phim theo tên
-- ============================================================
-- MỤC ĐÍCH:
--   Tìm kiếm phim dựa trên Title (tên phim)
--   Được dùng để lấy ID sau khi insert phim mới
-- 
-- THAM SỐ:
--   @Title NVARCHAR(255) - Tên phim cần tìm (bắt buộc)
-- 
-- KẾT QUẢ TRẢ VỀ:
--   - Một bản ghi phim (TOP 1, sắp xếp theo ID DESC - lấy gần nhất)
--   - Nếu không tìm thấy: return empty
-- 
-- LỖI:
--   - Không xử lý lỗi đặc biệt
-- 
-- HIỆU NĂNG:
--   - Độ phức tạp: O(n) nếu không có index trên Title
--   - Khuyến cáo: Tạo index trên Title nếu sử dụng thường xuyên
--   - Lệnh: CREATE INDEX idx_Movies_Title ON Movies(Title);
-- 
-- CHUYÊN BIỆT:
--   - TOP 1: Chỉ lấy 1 kết quả (phim gần nhất được tạo)
--   - ORDER BY Id DESC: Lấy phim được tạo gần nhất
-- 
-- VÍ DỤ:
--   EXEC sp_GetMovieByTitle @Title = 'Inception';
-- ============================================================
CREATE OR ALTER PROCEDURE sp_GetMovieByTitle
    @Title NVARCHAR(255)
AS
BEGIN
    SELECT TOP 1
        Id,
        Title,
        Description,
        Genre,
        DurationInMinutes,
        PosterUrl,
        ReleaseDate
    FROM 
        Movies
    WHERE 
        Title = @Title
    ORDER BY 
        Id DESC; -- Lấy phim được tạo gần nhất
END;
GO

-- ============================================================
-- 4. sp_CreateMovie - Tạo phim mới
-- ============================================================
-- MỤC ĐÍCH:
--   Thêm một phim mới vào bảng Movies
--   Kiểm tra lỗi và xử lý exception
-- 
-- THAM SỐ:
--   @Title NVARCHAR(255) - Tên phim (bắt buộc, không được NULL)
--   @Description NVARCHAR(MAX) - Mô tả phim (tùy chọn, mặc định NULL)
--   @Genre NVARCHAR(100) - Thể loại phim (bắt buộc)
--   @DurationInMinutes INT - Thời lượng phim (phút, bắt buộc)
--   @PosterUrl NVARCHAR(255) - URL poster (tùy chọn, mặc định NULL)
--   @ReleaseDate DATE - Ngày phát hành (bắt buộc)
-- 
-- KẾT QUẢ TRẢ VỀ:
--   - Không trả dữ liệu trực tiếp
--   - SCOPE_IDENTITY(): Trả ID của bản ghi mới (auto-increment)
--   - PRINT: Thông báo thành công hoặc lỗi
-- 
-- LỖI:
--   - Nếu @Title trùng: SQL sẽ lỗi nếu có constraint UNIQUE
--   - BEGIN TRY/CATCH: Bắt lỗi và THROW để Controller xử lý
-- 
-- HIỆU NĂNG:
--   - Độ phức tạp: O(1) - insert đơn lẻ
-- 
-- VÍ DỤ THÀNH CÔNG:
--   EXEC sp_CreateMovie 
--     @Title = 'Inception',
--     @Description = 'A thief who steals corporate secrets...',
--     @Genre = 'Sci-Fi',
--     @DurationInMinutes = 148,
--     @PosterUrl = 'https://example.com/inception.jpg',
--     @ReleaseDate = '2010-07-16';
--   -- Kết quả: Movie created successfully. ID: 3
-- 
-- VÍ DỤ LỖI:
--   EXEC sp_CreateMovie 
--     @Title = '', -- Empty title
--     @Genre = 'Action',
--     @DurationInMinutes = 120,
--     @ReleaseDate = '2024-01-01';
--   -- Kết quả: Lỗi (Controller kiểm tra và trả 400)
-- ============================================================
CREATE OR ALTER PROCEDURE sp_CreateMovie
    @Title NVARCHAR(255),
    @Description NVARCHAR(MAX) = NULL,
    @Genre NVARCHAR(100),
    @DurationInMinutes INT,
    @PosterUrl NVARCHAR(255) = NULL,
    @ReleaseDate DATE
AS
BEGIN
    BEGIN TRY
        -- Kiểm tra Title không được rỗng (validation có thể thêm ở đây)
        IF @Title IS NULL OR LEN(LTRIM(@Title)) = 0
        BEGIN
            THROW 50002, 'Title không được để trống', 1;
        END

        -- Insert phim mới vào bảng
        INSERT INTO Movies 
        (
            Title, 
            Description, 
            Genre, 
            DurationInMinutes, 
            PosterUrl, 
            ReleaseDate
        )
        VALUES 
        (
            @Title, 
            @Description, 
            @Genre, 
            @DurationInMinutes, 
            @PosterUrl, 
            @ReleaseDate
        );

        -- SCOPE_IDENTITY() trả ID của bản ghi vừa insert
        PRINT 'Phim được tạo thành công. ID: ' + CAST(SCOPE_IDENTITY() AS NVARCHAR);
    END TRY
    BEGIN CATCH
        -- Bắt lỗi và ném (throw) để Controller xử lý
        THROW;
    END CATCH
END;
GO

-- ============================================================
-- 5. sp_UpdateMovie - Cập nhật toàn bộ thông tin phim
-- ============================================================
-- MỤC ĐÍCH:
--   Cập nhật tất cả các trường của một phim (PUT request)
--   Yêu cầu ID phim tồn tại
-- 
-- THAM SỐ:
--   @MovieId INT - ID phim cần cập nhật (bắt buộc)
--   @Title NVARCHAR(255) - Tên phim mới (bắt buộc)
--   @Description NVARCHAR(MAX) - Mô tả mới (tùy chọn)
--   @Genre NVARCHAR(100) - Thể loại mới (bắt buộc)
--   @DurationInMinutes INT - Thời lượng mới (bắt buộc)
--   @PosterUrl NVARCHAR(255) - URL poster mới (tùy chọn)
--   @ReleaseDate DATE - Ngày phát hành mới (bắt buộc)
-- 
-- KẾT QUẢ TRẢ VỀ:
--   - Không trả dữ liệu trực tiếp
--   - @@ROWCOUNT: Số dòng bị ảnh hưởng (0 nếu ID không tồn tại)
--   - PRINT: Thông báo thành công
-- 
-- LỖI:
--   - Nếu @@ROWCOUNT = 0: THROW error "Movie not found"
--   - Controller sẽ bắt lỗi và trả 404
-- 
-- KHÁC BIỆT vs sp_UpdateMoviePartial:
--   - sp_UpdateMovie: Update TẤT CẢ fields (PUT)
--   - sp_UpdateMoviePartial: Update CHỈ 1 field (PATCH)
-- 
-- HIỆU NĂNG:
--   - Độ phức tạp: O(1) - update đơn lẻ
-- 
-- VÍ DỤ THÀNH CÔNG:
--   EXEC sp_UpdateMovie
--     @MovieId = 1,
--     @Title = 'Updated Title',
--     @Description = 'Updated description',
--     @Genre = 'Drama',
--     @DurationInMinutes = 150,
--     @PosterUrl = 'https://example.com/new-poster.jpg',
--     @ReleaseDate = '1994-09-23';
--   -- Kết quả: Movie updated successfully.
-- 
-- VÍ DỤ LỖI:
--   EXEC sp_UpdateMovie
--     @MovieId = 999, -- ID không tồn tại
--     ...;
--   -- Kết quả: Lỗi (@@ROWCOUNT = 0, throw error)
-- ============================================================
CREATE OR ALTER PROCEDURE sp_UpdateMovie
    @MovieId INT,
    @Title NVARCHAR(255),
    @Description NVARCHAR(MAX) = NULL,
    @Genre NVARCHAR(100),
    @DurationInMinutes INT,
    @PosterUrl NVARCHAR(255) = NULL,
    @ReleaseDate DATE
AS
BEGIN
    BEGIN TRY
        -- Update toàn bộ trường
        UPDATE Movies
        SET 
            Title = @Title,
            Description = @Description,
            Genre = @Genre,
            DurationInMinutes = @DurationInMinutes,
            PosterUrl = @PosterUrl,
            ReleaseDate = @ReleaseDate
        WHERE 
            Id = @MovieId;

        -- Kiểm tra xem có bản ghi nào bị update không
        IF @@ROWCOUNT = 0
        BEGIN
            THROW 50001, 'Phim không tồn tại', 1;
        END

        PRINT 'Phim được cập nhật thành công.';
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END;
GO

-- ============================================================
-- 6. sp_UpdateMoviePartial - Cập nhật một trường phim (Partial Update)
-- ============================================================
-- MỤC ĐÍCH:
--   Cập nhật CHỈ MỘT trường của phim (PATCH request)
--   Sử dụng SQL động (Dynamic SQL) để linh hoạt
-- 
-- THAM SỐ:
--   @MovieId INT - ID phim cần cập nhật (bắt buộc)
--   @FieldName NVARCHAR(100) - Tên trường cần update (VD: 'Genre', 'Title', ...)
--   @FieldValue NVARCHAR(MAX) - Giá trị mới cho trường
-- 
-- KẾT QUẢ TRẢ VỀ:
--   - Không trả dữ liệu trực tiếp
--   - PRINT: Thông báo thành công
-- 
-- LỖI:
--   - Nếu ID không tồn tại: THROW error
--   - Nếu @FieldName không hợp lệ: Lỗi SQL
--   - CẢNH BÁO: Dynamic SQL có nguy hiểm SQL Injection
--     → QUOTENAME() bảo vệ tên trường
--     → sp_executesql với parameter @Value bảo vệ giá trị
-- 
-- HIỆU NĂNG:
--   - Độ phức tạp: O(1) - update đơn lẻ
--   - Chậm hơn so sánh với sp_UpdateMovie (do SQL động)
-- 
-- VÍ DỤ THÀNH CÔNG:
--   -- Update chỉ Genre
--   EXEC sp_UpdateMoviePartial
--     @MovieId = 1,
--     @FieldName = 'Genre',
--     @FieldValue = 'Comedy';
-- 
--   -- Update chỉ DurationInMinutes
--   EXEC sp_UpdateMoviePartial
--     @MovieId = 1,
--     @FieldName = 'DurationInMinutes',
--     @FieldValue = '165';
-- 
-- VÍ DỤ LỖI:
--   EXEC sp_UpdateMoviePartial
--     @MovieId = 999, -- ID không tồn tại
--     @FieldName = 'Genre',
--     @FieldValue = 'Drama';
--   -- Kết quả: Lỗi (ID không tồn tại)
-- ============================================================
CREATE OR ALTER PROCEDURE sp_UpdateMoviePartial
    @MovieId INT,
    @FieldName NVARCHAR(100),
    @FieldValue NVARCHAR(MAX)
AS
BEGIN
    BEGIN TRY
        -- Kiểm tra phim tồn tại hay không
        IF NOT EXISTS (SELECT 1 FROM Movies WHERE Id = @MovieId)
        BEGIN
            THROW 50001, 'Phim không tồn tại', 1;
        END

        -- Xây dựng câu SQL động
        -- QUOTENAME(@FieldName): Bảo vệ tên trường (VD: 'Genre' → [Genre])
        -- Điều này tránh SQL injection qua tên trường
        DECLARE @SQL NVARCHAR(MAX);
        SET @SQL = 'UPDATE Movies SET ' + QUOTENAME(@FieldName) + ' = @Value WHERE Id = @MovieId';

        -- Thực thi SQL với parameter (tránh SQL injection qua giá trị)
        EXEC sp_executesql 
            @SQL, 
            N'@Value NVARCHAR(MAX), @MovieId INT', 
            @Value = @FieldValue, 
            @MovieId = @MovieId;

        PRINT 'Trường ' + @FieldName + ' được cập nhật thành công.';
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END;
GO

-- ============================================================
-- 7. sp_DeleteMovie - Xóa phim theo ID
-- ============================================================
-- MỤC ĐÍCH:
--   Xóa một phim khỏi bảng Movies dựa trên ID
--   Kiểm tra phim tồn tại trước khi xóa
-- 
-- THAM SỐ:
--   @MovieId INT - ID phim cần xóa (bắt buộc)
-- 
-- KẾT QUẢ TRẢ VỀ:
--   - Không trả dữ liệu trực tiếp
--   - PRINT: Thông báo thành công
-- 
-- LỖI:
--   - Nếu ID không tồn tại: THROW error "Movie not found"
--   - Nếu có Foreign Key constraint: SQL trả lỗi tham chiếu
--   - Controller sẽ bắt lỗi
-- 
-- CẢI BÁOMẶC:
--   - Kiểm tra Showtimes có tham chiếu đến phim không
--   - Có thể cần thêm CASCADE DELETE hoặc soft-delete
--   - Hiện tại: Hard delete (xóa vĩnh viễn)
-- 
-- HIỆU NĂNG:
--   - Độ phức tạp: O(1) - delete đơn lẻ
-- 
-- VÍ DỤ THÀNH CÔNG:
--   EXEC sp_DeleteMovie @MovieId = 1;
--   -- Kết quả: Movie deleted successfully.
-- 
-- VÍ DỤ LỖI:
--   EXEC sp_DeleteMovie @MovieId = 999; -- ID không tồn tại
--   -- Kết quả: Lỗi (throw error)
-- 
--   EXEC sp_DeleteMovie @MovieId = 2; -- Có Showtime tham chiếu
--   -- Kết quả: Lỗi Foreign Key constraint
-- ============================================================
CREATE OR ALTER PROCEDURE sp_DeleteMovie
    @MovieId INT
AS
BEGIN
    BEGIN TRY
        -- Kiểm tra phim tồn tại
        IF NOT EXISTS (SELECT 1 FROM Movies WHERE Id = @MovieId)
        BEGIN
            THROW 50001, 'Phim không tồn tại', 1;
        END

        -- Xóa phim
        DELETE FROM Movies
        WHERE 
            Id = @MovieId;

        PRINT 'Phim được xóa thành công.';
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END;
GO

-- ============================================================
-- TEST/DEMO: Chạy các Stored Procedures
-- ============================================================
-- Hướng dẫn: Bỏ comment để chạy các lệnh dưới đây

-- 1. Lấy tất cả phim
-- EXEC sp_GetAllMovies;

-- 2. Lấy phim theo ID
-- EXEC sp_GetMovieById @MovieId = 1;

-- 3. Lấy phim theo tên
-- EXEC sp_GetMovieByTitle @Title = 'Inception';

-- 4. Tạo phim mới
-- EXEC sp_CreateMovie 
--     @Title = 'Test Movie',
--     @Description = 'Mô tả phim test',
--     @Genre = 'Action',
--     @DurationInMinutes = 120,
--     @PosterUrl = 'https://example.com/poster.jpg',
--     @ReleaseDate = '2024-01-01';

-- 5. Cập nhật toàn bộ thông tin phim
-- EXEC sp_UpdateMovie
--     @MovieId = 1,
--     @Title = 'Tiêu đề cập nhật',
--     @Description = 'Mô tả cập nhật',
--     @Genre = 'Drama',
--     @DurationInMinutes = 130,
--     @PosterUrl = 'https://example.com/new-poster.jpg',
--     @ReleaseDate = '2024-02-01';

-- 6. Cập nhật một trường (partial update)
-- EXEC sp_UpdateMoviePartial
--     @MovieId = 1,
--     @FieldName = 'Genre',
--     @FieldValue = 'Comedy';

-- 7. Xóa phim
-- EXEC sp_DeleteMovie @MovieId = 1;
