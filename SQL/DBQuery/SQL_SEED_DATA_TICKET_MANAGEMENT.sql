-- ============================================================================
-- MODULE: SEED DATA TICKET MANAGEMENT
-- Mô tả: Xóa sạch dữ liệu cũ và chèn dữ liệu mẫu thử nghiệm Unicode để phục vụ
--        cho việc phát triển và tích hợp hệ thống.
-- ============================================================================

-- Làm sạch dữ liệu cũ theo đúng trình tự khóa ngoại
DELETE FROM Tickets;
DELETE FROM Bookings;
DELETE FROM Showtimes;
DELETE FROM Seats;
DELETE FROM CinemaHalls;
DELETE FROM Movies;
DELETE FROM Users;

-- Chèn người dùng mẫu
INSERT INTO Users (FullName, Email, PasswordHash, Role, CreatedAt)
VALUES
(N'Nguyen Van A', 'a@example.com', 'hash123', N'Customer', GETDATE()),
(N'Tran Thi B', 'b@example.com', 'hash456', N'Customer', GETDATE());

-- Chèn phim mẫu
INSERT INTO Movies (Title, Description, Genre, DurationInMinutes, PosterUrl, ReleaseDate)
VALUES
(N'Inception', N'Một bộ phim khoa học viễn tưởng về giấc mơ', N'Sci-Fi', 148, NULL, '2010-07-16'),
(N'Titanic', N'Câu chuyện tình yêu trên con tàu Titanic', N'Drama', 195, NULL, '1997-12-19');

-- Chèn phòng chiếu mẫu
INSERT INTO CinemaHalls (Name, TotalSeats)
VALUES
(N'Hall 1', 50),
(N'Hall 2', 50);

-- Chèn ghế ngồi mẫu cho Hall 1 (Id = 1)
INSERT INTO Seats (CinemaHallId, Row, Number, SeatType)
VALUES
(1, N'A', 1, N'Standard'),
(1, N'A', 2, N'Standard'),
(1, N'A', 3, N'VIP'),
(1, N'B', 1, N'Standard'),
(1, N'B', 2, N'Sweetbox');

-- Chèn ghế ngồi mẫu cho Hall 2 (Id = 2)
INSERT INTO Seats (CinemaHallId, Row, Number, SeatType)
VALUES
(2, N'A', 1, N'Standard'),
(2, N'A', 2, N'Standard'),
(2, N'B', 1, N'Standard'),
(2, N'B', 2, N'VIP');

-- Chèn lịch chiếu mẫu
INSERT INTO Showtimes (MovieId, CinemaHallId, StartTime, EndTime, BasePrice)
VALUES
(1, 1, '2026-06-05T19:00:00', '2026-06-05T21:28:00', 80000),
(2, 2, '2026-06-06T20:00:00', '2026-06-06T23:15:00', 90000);

-- Chèn đơn đặt vé mẫu
INSERT INTO Bookings (UserId, ShowtimeId, TotalPrice, Status, BookingTime)
VALUES
(1, 1, 160000, N'Paid', GETDATE()),
(2, 2, 90000, N'Paid', GETDATE());

-- Chèn vé chi tiết tương ứng với đơn đặt hàng
INSERT INTO Tickets (BookingId, SeatId)
VALUES
(1, 1), -- Ghế 1
(1, 2), -- Ghế 2
(2, 6); -- Ghế 6 (Ghế A1 của Hall 2)
