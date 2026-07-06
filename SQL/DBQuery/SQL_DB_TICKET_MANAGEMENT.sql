 
USE master;
DROP DATABASE IF EXISTS TicketManagementDB;

CREATE DATABASE TicketManagementDB;
GO
USE TicketManagementDB;


-- 1. Bảng Users (Quản lý người dùng)
-- Lưu trữ thông tin tài khoản, mật khẩu và vai trò để phân quyền.
CREATE TABLE Users (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    FullName VARCHAR(255) NOT NULL,
    Email VARCHAR(255) UNIQUE NOT NULL,
    PasswordHash VARCHAR(255) NOT NULL,
    Role VARCHAR(50) NOT NULL CHECK (Role IN ('Admin', 'Customer')),
    CreatedAt DATETIME DEFAULT GETDATE()
);
-- 2. Bảng Movies (Quản lý phim)

CREATE TABLE Movies (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Title VARCHAR(255) NOT NULL,
    Description TEXT NULL,
    Genre VARCHAR(100) NOT NULL,
    DurationInMinutes INT NOT NULL,
    PosterUrl VARCHAR(255) NULL,
    ReleaseDate DATE NOT NULL
);

-- 3. Bảng CinemaHalls (Phòng chiếu)
CREATE TABLE CinemaHalls (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name VARCHAR(255) NOT NULL,
    TotalSeats INT NOT NULL CHECK (TotalSeats > 0)
);
 -- 4. Bảng Seats (Ghế ngồi cố định)
CREATE TABLE Seats (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CinemaHallId INT NOT NULL,
    Row VARCHAR(10) NOT NULL,
    Number INT NOT NULL,
    SeatType VARCHAR(50) NOT NULL CHECK (SeatType IN ('Standard', 'VIP', 'Sweetbox'))
);

-- 5. Bảng Showtimes (Suất chiếu lịch diễn)
CREATE TABLE Showtimes (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    MovieId INT NOT NULL,
    CinemaHallId INT NOT NULL,
    StartTime DATETIME NOT NULL,
    EndTime DATETIME NOT NULL,
    BasePrice DECIMAL(18,2) NOT NULL CHECK (BasePrice >= 0)
);

-- 6. Bảng Bookings (Đơn đặt vé tổng thể)
CREATE TABLE Bookings (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    ShowtimeId INT NOT NULL,
    TotalPrice DECIMAL(18,2) NOT NULL CHECK (TotalPrice >= 0),
    Status VARCHAR(20) NOT NULL CHECK (Status IN ('Pending', 'Paid', 'Cancelled')),
    BookingTime DATETIME DEFAULT GETDATE()
);
-- 7. Bảng Tickets (Vé chi tiết cho từng ghế)
CREATE TABLE Tickets (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    BookingId INT NOT NULL,
    SeatId INT NOT NULL
);

-- FOREIGN KEY
-- Seats tham chiếu CinemaHalls
ALTER TABLE Seats
ADD CONSTRAINT FK_Seats_CinemaHalls FOREIGN KEY (CinemaHallId) REFERENCES CinemaHalls(Id);

-- Showtimes tham chiếu Movies và CinemaHalls
ALTER TABLE Showtimes
ADD CONSTRAINT FK_Showtimes_Movies FOREIGN KEY (MovieId) REFERENCES Movies(Id);

ALTER TABLE Showtimes
ADD CONSTRAINT FK_Showtimes_CinemaHalls FOREIGN KEY (CinemaHallId) REFERENCES CinemaHalls(Id);

-- Bookings tham chiếu Users và Showtimes
ALTER TABLE Bookings
ADD CONSTRAINT FK_Bookings_Users FOREIGN KEY (UserId) REFERENCES Users(Id);

ALTER TABLE Bookings
ADD CONSTRAINT FK_Bookings_Showtimes FOREIGN KEY (ShowtimeId) REFERENCES Showtimes(Id);

-- Tickets tham chiếu Bookings và Seats
ALTER TABLE Tickets
ADD CONSTRAINT FK_Tickets_Bookings FOREIGN KEY (BookingId) REFERENCES Bookings(Id);

ALTER TABLE Tickets
ADD CONSTRAINT FK_Tickets_Seats FOREIGN KEY (SeatId) REFERENCES Seats(Id);

-- Ràng buộc chống trùng ghế trong cùng một booking
ALTER TABLE Tickets
ADD CONSTRAINT UQ_Tickets UNIQUE (BookingId, SeatId);


--KHU VỰC CHECK DỮ LIỆU DEFAULT






--KHU VUC DU LIỆU TEST CÁC CHỨC NĂNG
-- 1. Users
DELETE FROM Tickets;
DELETE FROM Bookings;
DELETE FROM Showtimes;
DELETE FROM Seats;
DELETE FROM CinemaHalls;
DELETE FROM Movies;
DELETE FROM Users;
SELECT * FROM Seats;

-- Users
INSERT INTO Users (FullName, Email, PasswordHash, Role, CreatedAt)
VALUES 
(N'Nguyen Van A', 'a@example.com', 'hash123', 'Customer', GETDATE()),
(N'Tran Thi B', 'b@example.com', 'hash456', 'Customer', GETDATE());

-- Movies
INSERT INTO Movies (Title, Description, Genre, DurationInMinutes, PosterUrl, ReleaseDate)
VALUES
(N'Inception', N'Một bộ phim khoa học viễn tưởng về giấc mơ', N'Sci-Fi', 148, NULL, '2010-07-16'),
(N'Titanic', N'Câu chuyện tình yêu trên con tàu Titanic', N'Drama', 195, NULL, '1997-12-19');

-- CinemaHalls
INSERT INTO CinemaHalls (Name, TotalSeats)
VALUES
(N'Hall 1', 50),
(N'Hall 2', 50);

-- Seats (dùng đúng Id của CinemaHalls vừa tạo: Hall 1 = 1, Hall 2 = 2)
-- Ghế Hall 1 (Id = 1)
INSERT INTO Seats (CinemaHallId, Row, Number, SeatType)
VALUES
(1, 'A', 1, 'Standard'),
(1, 'A', 2, 'Standard'),
(1, 'A', 3, 'VIP'),
(1, 'B', 1, 'Standard'),
(1, 'B', 2, 'Sweetbox');

-- Ghế Hall 2 (Id = 2)
INSERT INTO Seats (CinemaHallId, Row, Number, SeatType)
VALUES
(2, 'A', 1, 'Standard'),
(2, 'A', 2, 'Standard'),
(2, 'B', 1, 'Standard'),
(2, 'B', 2, 'VIP');


-- Showtimes (MovieId = 1,2; CinemaHallId = 1,2)
INSERT INTO Showtimes (MovieId, CinemaHallId, StartTime, EndTime, BasePrice)
VALUES
(1, 1, '2026-06-05T19:00:00', '2026-06-05T21:28:00', 80000),
(2, 2, '2026-06-06T20:00:00', '2026-06-06T23:15:00', 90000);

-- Bookings (UserId = 1,2; ShowtimeId = 1,2)
INSERT INTO Bookings (UserId, ShowtimeId, TotalPrice, Status, BookingTime)
VALUES
(1, 1, 160000, 'Paid', GETDATE()),
(2, 2, 90000, 'Paid', GETDATE());

-- Tickets (BookingId = 1,2; SeatId phải khớp với Seats đã tạo)
INSERT INTO Tickets (BookingId, SeatId)
VALUES
(1, 1), -- A1 Inception
(1, 2), -- A2 Inception
(2, 1); -- A2 Titanic (SeatId 1)

