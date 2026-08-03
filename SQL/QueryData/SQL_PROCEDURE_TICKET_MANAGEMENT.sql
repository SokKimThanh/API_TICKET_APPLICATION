USE TicketManagementDB;
GO

IF OBJECT_ID('dbo.GetSeatsStatus', 'P') IS NOT NULL
    DROP PROCEDURE dbo.GetSeatsStatus;
GO

CREATE PROCEDURE dbo.GetSeatsStatus
    @ShowtimeId INT
AS
BEGIN
    SELECT s.Id AS SeatId,
           s.Row,
           s.Number,
           s.SeatType,
           CASE 
               WHEN t.Id IS NULL OR b.Id IS NULL THEN 'Available'
               ELSE 'Booked'
           END AS SeatStatus
    FROM Seats s
    JOIN CinemaHalls ch ON s.CinemaHallId = ch.Id
    JOIN Showtimes st ON st.CinemaHallId = ch.Id
    LEFT JOIN Tickets t ON t.SeatId = s.Id
    LEFT JOIN Bookings b ON b.Id = t.BookingId 
                         AND b.ShowtimeId = @ShowtimeId 
                         AND b.Status <> 'Cancelled'
    WHERE st.Id = @ShowtimeId;
END;
GO

-- EXEC dbo.GetSeatsStatus @ShowtimeId = 1;

IF OBJECT_ID('dbo.BookSeat', 'P') IS NOT NULL
    DROP PROCEDURE dbo.BookSeat;
GO

CREATE PROCEDURE dbo.BookSeat
    @BookingId INT,
    @SeatId INT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        -- Kiểm tra BookingId hợp lệ
        IF NOT EXISTS (SELECT 1 FROM Bookings WHERE Id = @BookingId)
        BEGIN
            PRINT N'Booking không tồn tại.';
            RETURN;
        END

        -- Kiểm tra SeatId hợp lệ
        IF NOT EXISTS (SELECT 1 FROM Seats WHERE Id = @SeatId)
        BEGIN
            PRINT N'Ghế không tồn tại.';
            RETURN;
        END

        -- Lấy ShowtimeId từ Booking
        DECLARE @ShowtimeId INT;
        SELECT @ShowtimeId = ShowtimeId FROM Bookings WHERE Id = @BookingId;

        -- Kiểm tra ghế đã được đặt cho suất chiếu này chưa
        IF EXISTS (
            SELECT 1
            FROM Tickets t
            JOIN Bookings b ON t.BookingId = b.Id
            WHERE t.SeatId = @SeatId
              AND b.ShowtimeId = @ShowtimeId
              AND b.Status = 'Paid'
        )
        BEGIN
            PRINT N'Ghế này đã được đặt cho suất chiếu này, vui lòng chọn ghế khác.';
            RETURN;
        END

        -- Nếu hợp lệ thì thêm vé mới
        INSERT INTO Tickets (BookingId, SeatId)
        VALUES (@BookingId, @SeatId);

        PRINT N'Đặt ghế thành công!';
    END TRY
    BEGIN CATCH
        PRINT N'Có lỗi xảy ra: ' + ERROR_MESSAGE();
    END CATCH
END;
go

-- EXEC dbo.BookSeat @BookingId = 2, @SeatId = 55;

USE TicketManagementDB;
GO

IF OBJECT_ID('dbo.ReportRevenueCapacity', 'P') IS NOT NULL
    DROP PROCEDURE dbo.ReportRevenueCapacity;
GO

IF OBJECT_ID('dbo.ReportRevenueCapacity', 'P') IS NOT NULL
    DROP PROCEDURE dbo.ReportRevenueCapacity;
GO

CREATE PROCEDURE dbo.ReportRevenueCapacity
    @FromDate DATETIME = NULL,
    @ToDate DATETIME = NULL
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRY
        -- Nếu không truyền ngày thì mặc định lấy toàn bộ dữ liệu
        IF @FromDate IS NULL SET @FromDate = '1900-01-01';
        IF @ToDate IS NULL SET @ToDate = '2100-01-01';

        SELECT 
            m.Title AS MovieTitle,
            ISNULL(SUM(CASE WHEN b.Status = 'Paid' THEN b.TotalPrice END),0) AS TotalRevenue,
            st.Id AS ShowtimeId,
            st.StartTime,
            COUNT(CASE WHEN b.Status = 'Paid' THEN t.Id END) AS TicketsSold,
            ch.TotalSeats,
            CASE 
                WHEN ch.TotalSeats > 0 
                THEN CAST(COUNT(CASE WHEN b.Status = 'Paid' THEN t.Id END) * 100.0 / ch.TotalSeats AS DECIMAL(5,2)) 
                ELSE 0 
            END AS CapacityPercent
        FROM Showtimes st
        JOIN Movies m ON st.MovieId = m.Id
        JOIN CinemaHalls ch ON st.CinemaHallId = ch.Id
        LEFT JOIN Bookings b ON b.ShowtimeId = st.Id
        LEFT JOIN Tickets t ON t.BookingId = b.Id
        WHERE st.StartTime BETWEEN @FromDate AND @ToDate
        GROUP BY m.Title, st.Id, st.StartTime, ch.TotalSeats
        ORDER BY m.Title, st.StartTime;
    END TRY
    BEGIN CATCH
        PRINT N'Có lỗi xảy ra trong quá trình tạo báo cáo: ' + ERROR_MESSAGE();
    END CATCH
END;
GO

-- EXEC dbo.ReportRevenueCapacity @FromDate = '2026-06-01', @ToDate = '2026-06-30';

