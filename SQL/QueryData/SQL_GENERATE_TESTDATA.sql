-- ============================================================================
-- MODULE: GENERATE BULK TEST DATA (SQL_GENERATE_TESTDATA.sql)
-- Mô tả: File kịch bản SQL tự động tạo ra một khối lượng dữ liệu cực lớn
--        (vài trăm nghìn dòng) cho các bảng Showtimes, Bookings, và Tickets.
--        Sử dụng kỹ thuật CROSS JOIN tối ưu hiệu năng để làm nguyên liệu benchmark,
--        kiểm tra execution plan của các chỉ mục báo cáo (StartTime, CinemaHallId)
--        và chỉ mục doanh thu (MovieId, Status, TotalPrice).
-- ============================================================================

SET NOCOUNT ON;
GO

PRINT '============================================================================';
PRINT 'BẮT ĐẦU QUY TRÌNH XÓA VÀ KHỞI TẠO LẠI DỮ LIỆU BULK (DATA SEEDING & GENERATION)';
PRINT '============================================================================';
GO

-- 1. LÀM SẠCH DỮ LIỆU CŨ THEO ĐÚNG TRÌNH TỰ KHÓA NGOẠI
PRINT '--- 1. Làm sạch dữ liệu cũ... ---';
DELETE FROM Tickets;
DELETE FROM Bookings;
DELETE FROM Showtimes;
DELETE FROM Seats;
DELETE FROM CinemaHalls;
DELETE FROM Movies;
DELETE FROM Users;
GO

-- RE-SEED IDENTITIES
DBCC CHECKIDENT ('Users', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('Movies', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('CinemaHalls', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('Seats', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('Showtimes', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('Bookings', RESEED, 0) WITH NO_INFOMSGS;
DBCC CHECKIDENT ('Tickets', RESEED, 0) WITH NO_INFOMSGS;
GO

-- 2. CHÈN 10 NGƯỜI DÙNG THAM CHIẾU (USERS)
PRINT '--- 2. Tạo dữ liệu người dùng mẫu (Users)... ---';
SET IDENTITY_INSERT Users ON;
INSERT INTO Users (Id, FullName, Email, PasswordHash, Role, IsDeleted, CreatedAt)
VALUES
(1, N'Nguyễn Văn A', 'customer1@example.com', '$2a$11$123456789012345678901u', 'Customer', 0, GETDATE()),
(2, N'Trần Thị B', 'customer2@example.com', '$2a$11$123456789012345678901u', 'Customer', 0, GETDATE()),
(3, N'Lê Hoàng C', 'customer3@example.com', '$2a$11$123456789012345678901u', 'Customer', 0, GETDATE()),
(4, N'Phạm Minh D', 'customer4@example.com', '$2a$11$123456789012345678901u', 'Customer', 0, GETDATE()),
(5, N'Hoàng Anh E', 'customer5@example.com', '$2a$11$123456789012345678901u', 'Customer', 0, GETDATE()),
(6, N'Vũ Quốc F', 'customer6@example.com', '$2a$11$123456789012345678901u', 'Customer', 0, GETDATE()),
(7, N'Đặng Thu G', 'customer7@example.com', '$2a$11$123456789012345678901u', 'Customer', 0, GETDATE()),
(8, N'Bùi Tiến H', 'customer8@example.com', '$2a$11$123456789012345678901u', 'Customer', 0, GETDATE()),
(9, N'Đỗ Thùy I', 'customer9@example.com', '$2a$11$123456789012345678901u', 'Customer', 0, GETDATE()),
(10, N'Admin Hệ Thống', 'admin@example.com', '$2a$11$123456789012345678901u', 'Admin', 0, GETDATE());
SET IDENTITY_INSERT Users OFF;
GO

-- 3. CHÈN 10 PHIM THAM CHIẾU (MOVIES)
PRINT '--- 3. Tạo danh sách phim mẫu (Movies)... ---';
SET IDENTITY_INSERT Movies ON;
INSERT INTO Movies (Id, Title, Description, Genre, DurationInMinutes, PosterUrl, ReleaseDate, IsDeleted)
VALUES
(1, N'Kẻ Kiến Tạo', N'Sci-Fi hành động tương lai', N'Sci-Fi', 133, NULL, '2023-09-29', 0),
(2, N'Oppenheimer', N'Phim tiểu sử về cha đẻ bom nguyên tử', N'Biography', 180, NULL, '2023-07-21', 0),
(3, N'Đất Rừng Phương Nam', N'Phim chính kịch lịch sử Việt Nam', N'Drama', 110, NULL, '2023-10-20', 0),
(4, N'Nhà Bà Nữ', N'Phim tâm lý gia đình hài hước', N'Drama', 102, NULL, '2023-01-22', 0),
(5, N'Bố Già', N'Tình cảm gia đình sâu sắc', N'Drama', 128, NULL, '2021-03-12', 0),
(6, N'Lật Mặt 6', N'Hành động giật gân, tình bạn tình anh em', N'Action', 112, NULL, '2023-04-28', 0),
(7, N'Avatar 2', N'Hành trình dòng nước kỳ vĩ', N'Sci-Fi', 192, NULL, '2022-12-16', 0),
(8, N'Người Nhện', N'Du hành vũ trụ nhện mới', N'Animation', 140, NULL, '2023-06-02', 0),
(9, N'Dune 2', N'Hành tinh cát phần 2 sử thi hoành tráng', N'Sci-Fi', 166, NULL, '2024-03-01', 0),
(10, N'Mai', N'Phim tâm lý tình cảm của Trấn Thành', N'Drama', 131, NULL, '2024-02-10', 0);
SET IDENTITY_INSERT Movies OFF;
GO

-- 4. CHÈN 5 PHÒNG CHIẾU (CINEMAHALLS)
PRINT '--- 4. Tạo phòng chiếu mẫu (CinemaHalls)... ---';
SET IDENTITY_INSERT CinemaHalls ON;
INSERT INTO CinemaHalls (Id, Name, TotalSeats, IsDeleted)
VALUES
(1, N'Phòng Chiếu Thượng Hạng 1', 50, 0),
(2, N'Phòng Chiếu Thượng Hạng 2', 50, 0),
(3, N'Phòng Chiếu IMAX 3', 50, 0),
(4, N'Phòng Chiếu 3D Cao Cấp 4', 50, 0),
(5, N'Phòng Chiếu Gold Class 5', 50, 0);
SET IDENTITY_INSERT CinemaHalls OFF;
GO

-- 5. SINH GHẾ TỰ ĐỘNG CHO CÁC PHÒNG CHIẾU (SEATS)
PRINT '--- 5. Tự động tạo 50 ghế/phòng cho 5 phòng chiếu (Seats) -> Tổng 250 Ghế... ---';
WITH RowLetter AS (
    SELECT N'A' AS RowLetter UNION ALL SELECT N'B' UNION ALL SELECT N'C' UNION ALL SELECT N'D' UNION ALL SELECT N'E'
),
SeatNum AS (
    SELECT 1 AS Num UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4 UNION ALL SELECT 5
    UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9 UNION ALL SELECT 10
)
INSERT INTO Seats (CinemaHallId, Row, Number, SeatType, IsDeleted)
SELECT
    h.Id AS CinemaHallId,
    r.RowLetter AS Row,
    s.Num AS Number,
    CASE WHEN s.Num IN (9, 10) THEN N'Sweetbox'
         WHEN s.Num IN (5, 6, 7, 8) THEN N'VIP'
         ELSE N'Standard'
    END AS SeatType,
    0 AS IsDeleted
FROM CinemaHalls h
CROSS JOIN RowLetter r
CROSS JOIN SeatNum s;
GO

-- 6. SINH SUẤT CHIẾU HÀNG LOẠT (SHOWTIMES) BẰNG KỸ THUẬT CROSS JOIN
-- Sinh 40,000 Suất chiếu phân bổ đều qua 200 ngày với 4 khung giờ chiếu khác nhau.
PRINT '--- 6. Sinh 40.000 suất chiếu (Showtimes) qua CROSS JOIN của 800 ngày/khung giờ... ---';
WITH L0 AS (SELECT 1 AS c UNION ALL SELECT 1), -- 2 rows
     L1 AS (SELECT 1 AS c FROM L0 AS a CROSS JOIN L0 AS b), -- 4 rows
     L2 AS (SELECT 1 AS c FROM L1 AS a CROSS JOIN L1 AS b), -- 16 rows
     L3 AS (SELECT 1 AS c FROM L2 AS a CROSS JOIN L2 AS b), -- 256 rows
     L4 AS (SELECT 1 AS c FROM L3 AS a CROSS JOIN L3 AS b), -- 65536 rows
     Nums AS (SELECT ROW_NUMBER() OVER(ORDER BY (SELECT NULL)) AS n FROM L4)
INSERT INTO Showtimes (MovieId, CinemaHallId, StartTime, EndTime, BasePrice, IsDeleted, CreatedAt)
SELECT
    m.Id AS MovieId,
    h.Id AS CinemaHallId,
    DATEADD(MINUTE, (num.n % 4) * 180, DATEADD(DAY, num.n / 4, '2026-01-01 08:00:00')) AS StartTime,
    DATEADD(MINUTE, (num.n % 4) * 180 + 150, DATEADD(DAY, num.n / 4, '2026-01-01 08:00:00')) AS EndTime,
    80000 + (num.n % 5) * 10000 AS BasePrice,
    0 AS IsDeleted,
    GETDATE() AS CreatedAt
FROM (SELECT TOP 800 n FROM Nums) num
CROSS JOIN Movies m
CROSS JOIN CinemaHalls h;
GO

-- 7. SINH ĐƠN ĐẶT VÉ HÀNG LOẠT (BOOKINGS) BẰNG KỸ THUẬT CROSS JOIN
-- Sinh 120.000 đơn đặt hàng (Bookings) bằng cách gán 3 đơn đặt hàng vào mỗi suất chiếu.
PRINT '--- 7. Sinh 120.000 đơn hàng (Bookings)... ---';
INSERT INTO Bookings (UserId, ShowtimeId, TotalPrice, Status, BookingTime, IsDeleted)
SELECT
    (s.Id % 9) + 1 AS UserId, -- Phân bổ đều cho các user 1 -> 9
    s.Id AS ShowtimeId,
    s.BasePrice * b_num.b AS TotalPrice,
    CASE WHEN (s.Id + b_num.b) % 3 = 0 THEN N'Paid'
         WHEN (s.Id + b_num.b) % 3 = 1 THEN N'Pending'
         ELSE N'Cancelled'
    END AS Status,
    DATEADD(HOUR, -24 + b_num.b, s.StartTime) AS BookingTime,
    0 AS IsDeleted
FROM Showtimes s
CROSS JOIN (SELECT 1 AS b UNION ALL SELECT 2 UNION ALL SELECT 3) b_num;
GO

-- 8. SINH VÉ CHI TIẾT HÀNG LOẠT (TICKETS) BẰNG KỸ THUẬT UNIONS & ARITHMETIC
-- Sinh khoảng 160.000 vé chi tiết (Tickets), đảm bảo các ghế đặt thuộc đúng phòng của suất chiếu đó.
PRINT '--- 8. Sinh khoảng 160.000 vé chi tiết (Tickets) hợp lệ... ---';
INSERT INTO Tickets (BookingId, SeatId, ShowtimeId, IsDeleted, CreatedAt)
SELECT
    b.Id AS BookingId,
    (s.CinemaHallId - 1) * 50 + 1 + (b.Id % 25) AS SeatId,
    s.Id AS ShowtimeId,
    0 AS IsDeleted,
    GETDATE() AS CreatedAt
FROM Bookings b
INNER JOIN Showtimes s ON b.ShowtimeId = s.Id
WHERE b.Status <> N'Cancelled'

UNION ALL

SELECT
    b.Id AS BookingId,
    (s.CinemaHallId - 1) * 50 + 26 + (b.Id % 25) AS SeatId,
    0 AS IsDeleted,
    GETDATE() AS CreatedAt
FROM Bookings b
INNER JOIN Showtimes s ON b.ShowtimeId = s.Id
WHERE b.Status <> N'Cancelled' AND (b.Id % 2) = 0; -- 50% đơn đặt vé sẽ mua 2 vé
GO

-- 9. TỔNG HỢP VÀ THỐNG KÊ DỮ LIỆU ĐÃ SINH
PRINT '============================================================================';
PRINT 'QUY TRÌNH SINH DỮ LIỆU HOÀN THÀNH. THỐNG KÊ CHI TIẾT CÁC BẢNG:';
PRINT '============================================================================';
SELECT 'Users' AS TableName, COUNT(*) AS [RowCount] FROM Users UNION ALL
SELECT 'Movies' AS TableName, COUNT(*) AS [RowCount] FROM Movies UNION ALL
SELECT 'CinemaHalls' AS TableName, COUNT(*) AS [RowCount] FROM CinemaHalls UNION ALL
SELECT 'Seats' AS TableName, COUNT(*) AS [RowCount] FROM Seats UNION ALL
SELECT 'Showtimes' AS TableName, COUNT(*) AS [RowCount] FROM Showtimes UNION ALL
SELECT 'Bookings' AS TableName, COUNT(*) AS [RowCount] FROM Bookings UNION ALL
SELECT 'Tickets' AS TableName, COUNT(*) AS [RowCount] FROM Tickets;
GO
