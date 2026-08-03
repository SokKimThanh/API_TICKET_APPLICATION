-- ============================================================================
-- MODULE: TEST INDEX & REPORT PERFORMANCE
-- Mô tả: File kịch bản SQL thực hiện bật SET STATISTICS IO/TIME, chạy các
--        truy vấn kiểm tra hiệu năng của các chỉ mục (Indexes) đã thiết lập.
--        Mục tiêu là phân tích Execution Plan để xác nhận các chỉ mục báo cáo
--        và chỉ mục doanh thu được sử dụng tối ưu.
-- ============================================================================

-- 1. ĐẢM BẢO MÔI TRƯỜNG KIỂM THỬ CÓ SẴN DỮ LIỆU
-- Nếu cơ sở dữ liệu của bạn trống, vui lòng thực thi kịch bản gieo dữ liệu trước:
-- SQLCMD -S (localdb)\MSSQLLocalDB -d TicketManagementDB -i SQL/DBQuery/SQL_SEED_DATA_TICKET_MANAGEMENT.sql
-- Hoặc chạy trực tiếp file SQL_SEED_DATA_TICKET_MANAGEMENT.sql trong SSMS.

-- BẬT ĐO ĐẠC THỜI GIAN VÀ THÔNG TIN ĐỌC ĐĨA (STATISTICS)
SET STATISTICS IO ON;
SET STATISTICS TIME ON;
GO

PRINT '============================================================================';
PRINT 'KỊCH BẢN KIỂM THỬ HIỆU NĂNG CHỈ MỤC (INDEX PERFORMANCE TEST)';
PRINT '============================================================================';
GO

-- ----------------------------------------------------------------------------
-- TRUY VẤN 1: LỌC THEO THỜI GIAN BẮT ĐẦU (StartTime)
-- ----------------------------------------------------------------------------
-- Mục tiêu: Kiểm tra xem SQL Server có sử dụng tiền tố (Prefix) StartTime của
--           chỉ mục composite IX_Showtimes_StartTime_Hall_Active hay không.
-- Kỳ vọng: Thực hiện Index Seek trên IX_Showtimes_StartTime_Hall_Active.
--          Không cần phải quét toàn bộ bảng (Table/Clustered Index Scan).
-- ----------------------------------------------------------------------------
PRINT '--- TRUY VẤN 1: Lọc theo StartTime ---';
SELECT
    Id,
    MovieId,
    CinemaHallId,
    StartTime,
    EndTime,
    BasePrice
FROM dbo.Showtimes
WHERE StartTime >= '2026-06-01 00:00:00'
  AND StartTime < '2026-07-01 00:00:00'
  AND IsDeleted = 0;
GO


-- ----------------------------------------------------------------------------
-- TRUY VẤN 2: LỌC THEO THỜI GIAN BẮT ĐẦU + PHÒNG CHIẾU (StartTime + CinemaHallId)
-- ----------------------------------------------------------------------------
-- Mục tiêu: Kiểm tra độ khớp hoàn hảo (Perfect Match) của cả hai trường trong khóa
--           của chỉ mục composite IX_Showtimes_StartTime_Hall_Active.
-- Kỳ vọng: Thực hiện Index Seek trực tiếp với cả 2 điều kiện tìm kiếm (Seek Predicates)
--          trên chỉ mục IX_Showtimes_StartTime_Hall_Active cực nhanh.
-- ----------------------------------------------------------------------------
PRINT '--- TRUY VẤN 2: Lọc theo StartTime + CinemaHallId ---';
SELECT
    Id,
    MovieId,
    CinemaHallId,
    StartTime,
    EndTime,
    BasePrice
FROM dbo.Showtimes
WHERE StartTime >= '2026-06-01 00:00:00'
  AND StartTime < '2026-07-01 00:00:00'
  AND CinemaHallId = 1
  AND IsDeleted = 0;
GO


-- ----------------------------------------------------------------------------
-- TRUY VẤN 3: TỔNG HỢP DOANH THU THEO NGÀY (BookingTime)
-- ----------------------------------------------------------------------------
-- Mục tiêu: Kiểm tra hiệu năng của chỉ mục báo cáo doanh số theo ngày giao dịch.
-- Kỳ vọng: Sử dụng chỉ mục IX_Bookings_BookingTime_Active để tìm kiếm phạm vi ngày nhanh chóng,
--          giảm chi phí CPU khi gom nhóm theo ngày.
-- ----------------------------------------------------------------------------
PRINT '--- TRUY VẤN 3: Tổng hợp doanh thu theo ngày giao dịch (Booking Date) ---';
SELECT
    CAST(BookingTime AS DATE) AS RevenueDate,
    SUM(TotalPrice) AS DailyRevenue,
    COUNT(Id) AS TotalBookings
FROM dbo.Bookings
WHERE BookingTime >= '2026-01-01 00:00:00'
  AND BookingTime < '2026-12-31 23:59:59'
  AND Status = N'Paid'
  AND IsDeleted = 0
GROUP BY CAST(BookingTime AS DATE)
ORDER BY RevenueDate;
GO


-- ----------------------------------------------------------------------------
-- TRUY VẤN 4: TỔNG HỢP DOANH THU THEO PHIM (MovieId)
-- ----------------------------------------------------------------------------
-- Mục tiêu: Kiểm tra xem Covering Index IX_Bookings_Showtime_Status_Include có phát huy tác dụng.
-- Kỳ vọng: Sử dụng IX_Bookings_Showtime_Status_Include để tính tổng doanh thu từ Bookings
--          trực tiếp từ cây chỉ mục mà không cần thực hiện Key Lookup (tra cứu dòng gốc).
--          Kết hợp sử dụng IX_Showtimes_Movie_Active và bảng Movies.
-- ----------------------------------------------------------------------------
PRINT '--- TRUY VẤN 4: Tổng hợp doanh thu theo phim ---';
SELECT
    m.Id AS MovieId,
    m.Title,
    SUM(b.TotalPrice) AS TotalRevenue,
    COUNT(b.Id) AS TotalBookings
FROM dbo.Bookings b
INNER JOIN dbo.Showtimes s ON b.ShowtimeId = s.Id
INNER JOIN dbo.Movies m ON s.MovieId = m.Id
WHERE b.Status = N'Paid'
  AND b.IsDeleted = 0
  AND s.IsDeleted = 0
  AND m.IsDeleted = 0
GROUP BY m.Id, m.Title
ORDER BY TotalRevenue DESC;
GO


-- ----------------------------------------------------------------------------
-- TRUY VẤN 5: TỔNG HỢP DOANH THU THEO CẢ NGÀY VÀ PHIM (KẾT HỢP)
-- ----------------------------------------------------------------------------
-- Mục tiêu: Kiểm tra hiệu năng truy vấn báo cáo đa chiều phức tạp khi kết hợp cả thời gian
--           giao dịch và thông tin phim.
-- Kỳ vọng: Tận dụng đồng thời IX_Bookings_BookingTime_Active hoặc IX_Bookings_Showtime_Status_Include,
--          phối hợp với các chỉ mục của Showtimes và Movies để đạt được Execution Plan tối ưu nhất.
-- ----------------------------------------------------------------------------
PRINT '--- TRUY VẤN 5: Tổng hợp doanh thu kết hợp theo ngày giao dịch và phim ---';
SELECT
    CAST(b.BookingTime AS DATE) AS RevenueDate,
    m.Id AS MovieId,
    m.Title,
    SUM(b.TotalPrice) AS TotalRevenue,
    COUNT(b.Id) AS TotalBookings
FROM dbo.Bookings b
INNER JOIN dbo.Showtimes s ON b.ShowtimeId = s.Id
INNER JOIN dbo.Movies m ON s.MovieId = m.Id
WHERE b.Status = N'Paid'
  AND b.IsDeleted = 0
  AND s.IsDeleted = 0
  AND m.IsDeleted = 0
GROUP BY CAST(b.BookingTime AS DATE), m.Id, m.Title
ORDER BY RevenueDate, TotalRevenue DESC;
GO

-- TẮT THÔNG TIN ĐO ĐẠC SAU KHI HOÀN THÀNH KIỂM THỬ
SET STATISTICS IO OFF;
SET STATISTICS TIME OFF;
GO

PRINT '============================================================================';
PRINT 'KỊCH BẢN KIỂM THỬ HOÀN TẤT. VUI LÒNG KIỂM TRA PHẦN "MESSAGES" ĐỂ XEM THÔNG SỐ IO/TIME';
PRINT 'VÀ KHẢO SÁT CHỨC NĂNG "DISPLAY ACTUAL EXECUTION PLAN" TRONG SSMS/AZURE DATA STUDIO.';
PRINT '============================================================================';
GO
