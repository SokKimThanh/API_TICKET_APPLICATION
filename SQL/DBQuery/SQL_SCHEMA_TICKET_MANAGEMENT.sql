-- ============================================================================
-- MODULE: SCHEMA TICKET MANAGEMENT
-- Mô tả: Khởi tạo các bảng cơ sở dữ liệu chính với kiểu dữ liệu tối ưu,
--        hỗ trợ tiếng Việt Unicode và tích hợp sẵn trường Audit/Soft Delete.
-- ============================================================================

-- 1. Bảng Users (Quản lý người dùng)
CREATE TABLE Users (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    FullName NVARCHAR(255) NOT NULL,
    Email NVARCHAR(255) UNIQUE NOT NULL,
    PasswordHash NVARCHAR(255) NOT NULL,
    Role NVARCHAR(50) NOT NULL,

    -- Audit & Soft Delete Columns
    IsDeleted BIT DEFAULT 0 NOT NULL,
    DeletedAt DATETIME NULL,
    CreatedAt DATETIME DEFAULT GETDATE() NOT NULL,
    UpdatedAt DATETIME NULL,
    CreatedBy INT NULL,
    UpdatedBy INT NULL
);

-- 2. Bảng Movies (Quản lý phim)
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
    CreatedBy INT NULL,
    UpdatedBy INT NULL
);

-- 3. Bảng CinemaHalls (Phòng chiếu)
CREATE TABLE CinemaHalls (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(255) NOT NULL,
    TotalSeats INT NOT NULL,

    -- Audit & Soft Delete Columns
    IsDeleted BIT DEFAULT 0 NOT NULL,
    DeletedAt DATETIME NULL,
    CreatedAt DATETIME DEFAULT GETDATE() NOT NULL,
    UpdatedAt DATETIME NULL,
    CreatedBy INT NULL,
    UpdatedBy INT NULL
);

-- 4. Bảng Seats (Ghế ngồi cố định)
CREATE TABLE Seats (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CinemaHallId INT NOT NULL,
    Row NVARCHAR(10) NOT NULL,
    Number INT NOT NULL,
    SeatType NVARCHAR(50) NOT NULL,

    -- Audit & Soft Delete Columns
    IsDeleted BIT DEFAULT 0 NOT NULL,
    DeletedAt DATETIME NULL,
    CreatedAt DATETIME DEFAULT GETDATE() NOT NULL,
    UpdatedAt DATETIME NULL,
    CreatedBy INT NULL,
    UpdatedBy INT NULL
);

-- 5. Bảng Showtimes (Suất chiếu lịch diễn)
CREATE TABLE Showtimes (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    MovieId INT NOT NULL,
    CinemaHallId INT NOT NULL,
    StartTime DATETIME NOT NULL,
    EndTime DATETIME NOT NULL,
    BasePrice DECIMAL(18,2) NOT NULL,

    -- Audit & Soft Delete Columns
    IsDeleted BIT DEFAULT 0 NOT NULL,
    DeletedAt DATETIME NULL,
    CreatedAt DATETIME DEFAULT GETDATE() NOT NULL,
    UpdatedAt DATETIME NULL,
    CreatedBy INT NULL,
    UpdatedBy INT NULL
);

-- 6. Bảng Bookings (Đơn đặt vé tổng thể)
CREATE TABLE Bookings (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    ShowtimeId INT NOT NULL,
    TotalPrice DECIMAL(18,2) NOT NULL,
    Status NVARCHAR(20) NOT NULL,
    BookingTime DATETIME DEFAULT GETDATE(),
    BookingDate AS CAST(BookingTime AS DATE) PERSISTED,

    -- Audit & Soft Delete Columns
    IsDeleted BIT DEFAULT 0 NOT NULL,
    DeletedAt DATETIME NULL,
    UpdatedAt DATETIME NULL,
    CreatedBy INT NULL,
    UpdatedBy INT NULL
);

-- 7. Bảng Tickets (Vé chi tiết cho từng ghế)
CREATE TABLE Tickets (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    BookingId INT NOT NULL,
    SeatId INT NOT NULL,
    ShowtimeId INT NOT NULL,

    -- Audit & Soft Delete Columns
    IsDeleted BIT DEFAULT 0 NOT NULL,
    DeletedAt DATETIME NULL,
    CreatedAt DATETIME DEFAULT GETDATE() NOT NULL,
    UpdatedAt DATETIME NULL,
    CreatedBy INT NULL,
    UpdatedBy INT NULL
);
