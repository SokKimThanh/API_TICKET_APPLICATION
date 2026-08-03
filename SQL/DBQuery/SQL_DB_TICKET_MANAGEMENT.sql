USE master;
DROP DATABASE IF EXISTS TicketManagementDB;

CREATE DATABASE TicketManagementDB;
GO
USE TicketManagementDB;

-- ============================================================================
-- 1. Bảng Users (Quản lý người dùng)
-- ============================================================================
CREATE TABLE Users (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    FullName NVARCHAR(255) NOT NULL,
    Email NVARCHAR(255) UNIQUE NOT NULL,
    PasswordHash NVARCHAR(255) NOT NULL,
    Role NVARCHAR(50) NOT NULL CHECK (Role IN ('Admin', 'Customer')),

    -- Audit & Soft Delete Columns
    IsDeleted BIT DEFAULT 0 NOT NULL,
    DeletedAt DATETIME NULL,
    CreatedAt DATETIME DEFAULT GETDATE() NOT NULL,
    UpdatedAt DATETIME NULL,
    CreatedBy INT NULL FOREIGN KEY REFERENCES Users(Id),
    UpdatedBy INT NULL FOREIGN KEY REFERENCES Users(Id)
);

-- ============================================================================
-- 2. Bảng Movies (Quản lý phim)
-- ============================================================================
CREATE TABLE Movies (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Title NVARCHAR(255) NOT NULL,
    Description NVARCHAR(MAX) NULL,
    Genre NVARCHAR(100) NOT NULL,
    DurationInMinutes INT NOT NULL,
    PosterUrl NVARCHAR(255) NULL,
    ReleaseDate DATE NOT NULL,

    -- Audit & Soft Delete Columns
    IsDeleted BIT DEFAULT 0 NOT NULL,
    DeletedAt DATETIME NULL,
    CreatedAt DATETIME DEFAULT GETDATE() NOT NULL,
    UpdatedAt DATETIME NULL,
    CreatedBy INT NULL FOREIGN KEY REFERENCES Users(Id),
    UpdatedBy INT NULL FOREIGN KEY REFERENCES Users(Id)
);

-- ============================================================================
-- 3. Bảng CinemaHalls (Phòng chiếu)
-- ============================================================================
CREATE TABLE CinemaHalls (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(255) NOT NULL,
    TotalSeats INT NOT NULL CHECK (TotalSeats > 0),

    -- Audit & Soft Delete Columns
    IsDeleted BIT DEFAULT 0 NOT NULL,
    DeletedAt DATETIME NULL,
    CreatedAt DATETIME DEFAULT GETDATE() NOT NULL,
    UpdatedAt DATETIME NULL,
    CreatedBy INT NULL FOREIGN KEY REFERENCES Users(Id),
    UpdatedBy INT NULL FOREIGN KEY REFERENCES Users(Id)
);

-- ============================================================================
-- 4. Bảng Seats (Ghế ngồi cố định)
-- ============================================================================
CREATE TABLE Seats (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CinemaHallId INT NOT NULL,
    Row NVARCHAR(10) NOT NULL,
    Number INT NOT NULL,
    SeatType NVARCHAR(50) NOT NULL CHECK (SeatType IN ('Standard', 'VIP', 'Sweetbox')),

    -- Audit & Soft Delete Columns
    IsDeleted BIT DEFAULT 0 NOT NULL,
    DeletedAt DATETIME NULL,
    CreatedAt DATETIME DEFAULT GETDATE() NOT NULL,
    UpdatedAt DATETIME NULL,
    CreatedBy INT NULL FOREIGN KEY REFERENCES Users(Id),
    UpdatedBy INT NULL FOREIGN KEY REFERENCES Users(Id)
);

-- ============================================================================
-- 5. Bảng Showtimes (Suất chiếu lịch diễn)
-- ============================================================================
CREATE TABLE Showtimes (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    MovieId INT NOT NULL,
    CinemaHallId INT NOT NULL,
    StartTime DATETIME NOT NULL,
    EndTime DATETIME NOT NULL,
    BasePrice DECIMAL(18,2) NOT NULL CHECK (BasePrice >= 0),

    -- Audit & Soft Delete Columns
    IsDeleted BIT DEFAULT 0 NOT NULL,
    DeletedAt DATETIME NULL,
    CreatedAt DATETIME DEFAULT GETDATE() NOT NULL,
    UpdatedAt DATETIME NULL,
    CreatedBy INT NULL FOREIGN KEY REFERENCES Users(Id),
    UpdatedBy INT NULL FOREIGN KEY REFERENCES Users(Id)
);

-- ============================================================================
-- 6. Bảng Bookings (Đơn đặt vé tổng thể)
-- ============================================================================
CREATE TABLE Bookings (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    ShowtimeId INT NOT NULL,
    TotalPrice DECIMAL(18,2) NOT NULL CHECK (TotalPrice >= 0),
    Status NVARCHAR(20) NOT NULL CHECK (Status IN ('Pending', 'Paid', 'Cancelled')),
    BookingTime DATETIME DEFAULT GETDATE(),

    -- Audit & Soft Delete Columns
    IsDeleted BIT DEFAULT 0 NOT NULL,
    DeletedAt DATETIME NULL,
    UpdatedAt DATETIME NULL,
    CreatedBy INT NULL FOREIGN KEY REFERENCES Users(Id),
    UpdatedBy INT NULL FOREIGN KEY REFERENCES Users(Id)
);

-- ============================================================================
-- 7. Bảng Tickets (Vé chi tiết cho từng ghế)
-- ============================================================================
CREATE TABLE Tickets (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    BookingId INT NOT NULL,
    SeatId INT NOT NULL,

    -- Audit & Soft Delete Columns
    IsDeleted BIT DEFAULT 0 NOT NULL,
    DeletedAt DATETIME NULL,
    CreatedAt DATETIME DEFAULT GETDATE() NOT NULL,
    UpdatedAt DATETIME NULL,
    CreatedBy INT NULL FOREIGN KEY REFERENCES Users(Id),
    UpdatedBy INT NULL FOREIGN KEY REFERENCES Users(Id)
);

-- ============================================================================
-- FOREIGN KEY CONSTRAINTS
-- ============================================================================

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


-- ============================================================================
-- PERFORMANCE & FILTERED INDEXES
-- ============================================================================

-- Tối ưu hóa tìm kiếm người dùng theo Email (Xác thực/Đăng ký)
CREATE UNIQUE INDEX IX_Users_Email_Active ON Users(Email) WHERE IsDeleted = 0;
CREATE INDEX IX_Users_Role_Active ON Users(Role) WHERE IsDeleted = 0;

-- Tối ưu hóa tìm kiếm phim theo Thể loại và Ngày phát hành
CREATE INDEX IX_Movies_Genre_Active ON Movies(Genre) WHERE IsDeleted = 0;
CREATE INDEX IX_Movies_ReleaseDate_Active ON Movies(ReleaseDate) WHERE IsDeleted = 0;

-- Tối ưu hóa JOIN lấy ghế theo phòng chiếu
CREATE INDEX IX_Seats_CinemaHall_Active ON Seats(CinemaHallId) WHERE IsDeleted = 0;

-- Tối ưu hóa kiểm tra chồng lặp lịch chiếu
CREATE INDEX IX_Showtimes_StartTime_Hall_Active ON Showtimes(StartTime, CinemaHallId) WHERE IsDeleted = 0;
CREATE INDEX IX_Showtimes_Movie_Active ON Showtimes(MovieId) WHERE IsDeleted = 0;

-- Tối ưu hóa lọc đơn đặt hàng theo User và Trạng thái
CREATE INDEX IX_Bookings_User_Active ON Bookings(UserId) WHERE IsDeleted = 0;
CREATE INDEX IX_Bookings_Status_Active ON Bookings(Status) WHERE IsDeleted = 0;
CREATE INDEX IX_Bookings_Showtime_Active ON Bookings(ShowtimeId) WHERE IsDeleted = 0;

-- Tối ưu hóa truy xuất vé
CREATE INDEX IX_Tickets_Booking ON Tickets(BookingId);
CREATE INDEX IX_Tickets_Seat ON Tickets(SeatId);


-- ============================================================================
-- SEED TEST DATA
-- ============================================================================

-- Xóa dữ liệu cũ nếu có
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
(N'Nguyen Van A', 'a@example.com', 'hash123', 'Customer', GETDATE()),
(N'Tran Thi B', 'b@example.com', 'hash456', 'Customer', GETDATE());

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
(1, N'A', 1, 'Standard'),
(1, N'A', 2, 'Standard'),
(1, N'A', 3, 'VIP'),
(1, N'B', 1, 'Standard'),
(1, N'B', 2, 'Sweetbox');

-- Chèn ghế ngồi mẫu cho Hall 2 (Id = 2)
INSERT INTO Seats (CinemaHallId, Row, Number, SeatType)
VALUES
(2, N'A', 1, 'Standard'),
(2, N'A', 2, 'Standard'),
(2, N'B', 1, 'Standard'),
(2, N'B', 2, 'VIP');

-- Chèn lịch chiếu mẫu
INSERT INTO Showtimes (MovieId, CinemaHallId, StartTime, EndTime, BasePrice)
VALUES
(1, 1, '2026-06-05T19:00:00', '2026-06-05T21:28:00', 80000),
(2, 2, '2026-06-06T20:00:00', '2026-06-06T23:15:00', 90000);

-- Chèn đơn đặt vé mẫu
INSERT INTO Bookings (UserId, ShowtimeId, TotalPrice, Status, BookingTime)
VALUES
(1, 1, 160000, 'Paid', GETDATE()),
(2, 2, 90000, 'Paid', GETDATE());

-- Chèn vé chi tiết tương ứng với đơn đặt hàng
INSERT INTO Tickets (BookingId, SeatId)
VALUES
(1, 1), -- Ghế 1
(1, 2), -- Ghế 2
(2, 6); -- Ghế 6 (Ghế A1 của Hall 2)
