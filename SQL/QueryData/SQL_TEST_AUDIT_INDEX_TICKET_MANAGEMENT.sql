-- 1. Đảm bảo có dữ liệu phòng chiếu Id = 1 và phim Id = 1 trước (Nếu chưa có)
SET IDENTITY_INSERT dbo.CinemaHalls ON;
IF NOT EXISTS (SELECT 1 FROM dbo.CinemaHalls WHERE Id = 5)
    INSERT INTO dbo.CinemaHalls (Id, Name, TotalSeats, IsDeleted) 
    VALUES (5, N'Phòng Chiếu Số 5', 120, 0);
SET IDENTITY_INSERT dbo.CinemaHalls OFF;

SET IDENTITY_INSERT dbo.Movies ON;
IF NOT EXISTS (SELECT 1 FROM dbo.Movies WHERE Id = 5)
    INSERT INTO dbo.Movies (Id, Title, Genre, DurationInMinutes,PosterUrl, ReleaseDate, IsDeleted) 
    VALUES (5, N'Phim Thử Nghiệm Hiệu Năng', N'Hành Động',120, 'c:/phim.png','2026-01-6',  0);
SET IDENTITY_INSERT dbo.Movies OFF;

select * from dbo.cinemahalls
select * from dbo.movies

-- 2. Chèn dữ liệu kiểm thử vào bảng Showtimes
-- Bao gồm cả dữ liệu THỎA MÃN và KHÔNG THỎA MÃN điều kiện để test tính chính xác của Index
INSERT INTO dbo.Showtimes (MovieId, CinemaHallId, StartTime, EndTime, BasePrice, IsDeleted, CreatedAt)
VALUES 
-- Khớp điều kiện: Sau ngày 01/05/2026, Phòng 1, Chưa xóa
(1, 5, '2026-05-01 10:00:00', '2026-05-01 12:00:00', 90000, 0, GETDATE()),
(1, 5, '2026-05-02 14:30:00', '2026-05-02 16:30:00', 90000, 0, GETDATE()),
(1, 5, '2026-06-15 19:00:00', '2026-06-15 21:00:00', 120000, 0, GETDATE()),

-- KHÔNG khớp điều kiện (Để test xem Index có loại trừ được không):
(1, 5, '2026-04-15 18:00:00', '2026-04-15 20:00:00', 90000, 0, GETDATE()),  -- Sai: Trước ngày 01/05/2026
(1, 2, '2026-05-05 20:00:00', '2026-05-05 22:00:00', 90000, 0, GETDATE()),  -- Sai: Phòng chiếu số 2
(1, 5, '2026-05-10 09:00:00', '2026-05-10 11:00:00', 90000, 1, GETDATE());  -- Sai: Đã bị Xóa mềm (IsDeleted = 1)
GO

-- Bật tính năng đo thời gian và tài nguyên CPU/RAM tiêu tốn (Hiển thị ở tab Messages)
SET STATISTICS IO ON;
SET STATISTICS TIME ON;
GO

-- Chạy câu lệnh truy vấn
SELECT Id, MovieId, CinemaHallId, StartTime, IsDeleted 
FROM dbo.Showtimes 
WHERE StartTime >= '2026-05-01' AND CinemaHallId = 5 AND IsDeleted = 0;
GO

-- Tắt tính năng đo
SET STATISTICS IO OFF;
SET STATISTICS TIME OFF;
GO