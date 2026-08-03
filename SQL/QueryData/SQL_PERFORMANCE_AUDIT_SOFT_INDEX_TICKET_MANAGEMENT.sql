-- ============================================================================
-- SECTION 1: ADD AUDIT & SOFT DELETE FIELDS TO ALL TABLES
-- ============================================================================
-- Khai báo cấu trúc bảng tạm và các biến hệ thống
DECLARE @TableName NVARCHAR(100), @ColumnName NVARCHAR(100), @ColumnDef NVARCHAR(200), @Sql NVARCHAR(MAX);
DECLARE @Columns TABLE (TableName NVARCHAR(100), ColumnName NVARCHAR(100), ColumnDef NVARCHAR(200));

INSERT INTO @Columns VALUES
-- Users
('Users', 'CreatedAt', 'DATETIME DEFAULT GETDATE()'),
('Users', 'UpdatedAt', 'DATETIME NULL'),
('Users', 'CreatedBy', 'INT NULL REFERENCES dbo.Users(Id)'),
('Users', 'UpdatedBy', 'INT NULL REFERENCES dbo.Users(Id)'),
('Users', 'IsDeleted', 'BIT DEFAULT 0'),
('Users', 'DeletedAt', 'DATETIME NULL'),

-- Movies
('Movies', 'CreatedAt', 'DATETIME DEFAULT GETDATE()'),
('Movies', 'UpdatedAt', 'DATETIME NULL'),
('Movies', 'CreatedBy', 'INT REFERENCES dbo.Users(Id)'),
('Movies', 'UpdatedBy', 'INT NULL REFERENCES dbo.Users(Id)'),
('Movies', 'IsDeleted', 'BIT DEFAULT 0'),
('Movies', 'DeletedAt', 'DATETIME NULL'),

-- CinemaHalls
('CinemaHalls', 'CreatedAt', 'DATETIME DEFAULT GETDATE()'),
('CinemaHalls', 'UpdatedAt', 'DATETIME NULL'),
('CinemaHalls', 'CreatedBy', 'INT REFERENCES dbo.Users(Id)'),
('CinemaHalls', 'UpdatedBy', 'INT NULL REFERENCES dbo.Users(Id)'),
('CinemaHalls', 'IsDeleted', 'BIT DEFAULT 0'),
('CinemaHalls', 'DeletedAt', 'DATETIME NULL'),

-- Seats
('Seats', 'CreatedAt', 'DATETIME DEFAULT GETDATE()'),
('Seats', 'UpdatedAt', 'DATETIME NULL'),
('Seats', 'CreatedBy', 'INT REFERENCES dbo.Users(Id)'),
('Seats', 'UpdatedBy', 'INT NULL REFERENCES dbo.Users(Id)'),
('Seats', 'IsDeleted', 'BIT DEFAULT 0'),
('Seats', 'DeletedAt', 'DATETIME NULL'),

-- Showtimes
('Showtimes', 'CreatedAt', 'DATETIME DEFAULT GETDATE()'),
('Showtimes', 'UpdatedAt', 'DATETIME NULL'),
('Showtimes', 'CreatedBy', 'INT REFERENCES dbo.Users(Id)'),
('Showtimes', 'UpdatedBy', 'INT NULL REFERENCES dbo.Users(Id)'),
('Showtimes', 'IsDeleted', 'BIT DEFAULT 0'),
('Showtimes', 'DeletedAt', 'DATETIME NULL'),

-- Bookings
('Bookings', 'UpdatedAt', 'DATETIME NULL'),
('Bookings', 'CreatedBy', 'INT REFERENCES dbo.Users(Id)'),
('Bookings', 'UpdatedBy', 'INT NULL REFERENCES dbo.Users(Id)'),
('Bookings', 'IsDeleted', 'BIT DEFAULT 0'),
('Bookings', 'DeletedAt', 'DATETIME NULL'),

-- Tickets
('Tickets', 'CreatedAt', 'DATETIME DEFAULT GETDATE()'),
('Tickets', 'UpdatedAt', 'DATETIME NULL'),
('Tickets', 'CreatedBy', 'INT REFERENCES dbo.Users(Id)'),
('Tickets', 'UpdatedBy', 'INT NULL REFERENCES dbo.Users(Id)'),
('Tickets', 'IsDeleted', 'BIT DEFAULT 0'),
('Tickets', 'DeletedAt', 'DATETIME NULL');

-- Bắt đầu con trỏ hoặc vòng lặp duyệt qua từng dòng để thêm cột
WHILE EXISTS (SELECT 1 FROM @Columns)
BEGIN
    SELECT TOP 1 
        @TableName = TableName, 
        @ColumnName = ColumnName, 
        @ColumnDef = ColumnDef 
    FROM @Columns;

    -- Kiểm tra nếu cột chưa tồn tại thì mới chạy ALTER TABLE
    IF NOT EXISTS (
        SELECT 1 FROM sys.columns c 
        JOIN sys.tables t ON c.object_id = t.object_id 
        WHERE t.name = @TableName AND c.name = @ColumnName
    )
    BEGIN
        SET @Sql = 'ALTER TABLE dbo.' + QUOTENAME(@TableName) + ' ADD ' + QUOTENAME(@ColumnName) + ' ' + @ColumnDef;
        EXEC sp_executesql @Sql;
    END

    -- Xóa dòng vừa xử lý khỏi bảng tạm
    DELETE TOP (1) FROM @Columns;
END
GO -- CHỈ ĐẶT GO Ở ĐÂY SAU KHI KẾT THÚC HOÀN TOÀN VÒNG LẶP
 
 
-- ============================================================================
-- SECTION 2: CREATE PERFORMANCE INDEXES
-- ============================================================================
-- Optimize Users table queries by email and role filtering
CREATE INDEX IX_Users_Email ON dbo.Users(Email) WHERE IsDeleted = 0;
CREATE INDEX IX_Users_Role ON dbo.Users(Role) WHERE IsDeleted = 0;

-- Optimize Movies table queries by genre and release date filtering
CREATE INDEX IX_Movies_Genre ON dbo.Movies(Genre) WHERE IsDeleted = 0;
CREATE INDEX IX_Movies_ReleaseDate ON dbo.Movies(ReleaseDate) WHERE IsDeleted = 0;

-- Optimize Seats table queries by cinema hall (common join operation)
CREATE INDEX IX_Seats_CinemaHallId ON dbo.Seats(CinemaHallId) WHERE IsDeleted = 0;

-- Optimize Showtimes table queries by movie, cinema hall, and start time filtering
CREATE INDEX IX_Showtimes_MovieId ON dbo.Showtimes(MovieId) WHERE IsDeleted = 0;
CREATE INDEX IX_Showtimes_CinemaHallId ON dbo.Showtimes(CinemaHallId) WHERE IsDeleted = 0;
CREATE INDEX IX_Showtimes_StartTime ON dbo.Showtimes(StartTime) WHERE IsDeleted = 0;
CREATE INDEX IX_Showtimes_StartTime_CinemaHallId ON dbo.Showtimes(StartTime, CinemaHallId) WHERE IsDeleted = 0;

-- Optimize Bookings table queries by user, status, and booking date filtering
CREATE INDEX IX_Bookings_UserId ON dbo.Bookings(UserId) WHERE IsDeleted = 0;
CREATE INDEX IX_Bookings_Status ON dbo.Bookings(Status) WHERE IsDeleted = 0;
CREATE INDEX IX_Bookings_BookingTime ON dbo.Bookings(BookingTime) WHERE IsDeleted = 0;
CREATE INDEX IX_Bookings_ShowtimeId ON dbo.Bookings(ShowtimeId) WHERE IsDeleted = 0;

-- Optimize Tickets table queries by booking and seat lookup operations
CREATE INDEX IX_Tickets_BookingId ON dbo.Tickets(BookingId);
CREATE INDEX IX_Tickets_SeatId ON dbo.Tickets(SeatId);