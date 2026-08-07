-- ============================================================================
-- MODULE: INDEXES TICKET MANAGEMENT
-- Mô tả: Khởi tạo tất cả các chỉ mục Non-Clustered Indexes và Filtered Indexes
--        nhằm tối ưu hóa tốc độ truy vấn giao dịch (OLTP) và báo cáo phân tích (OLAP).
-- ============================================================================

-- ----------------------------------------------------------------------------
-- 1. TỐI ƯU HÓA TRUY VẤN GIAO DỊCH (OLTP INDEXES)
-- ----------------------------------------------------------------------------

-- Tối ưu hóa tìm kiếm người dùng theo Email (Xác thực/Đăng ký)
CREATE UNIQUE INDEX IX_Users_Email_Active ON Users(Email) WHERE IsDeleted = 0;
CREATE INDEX IX_Users_Role_Active ON Users(Role) WHERE IsDeleted = 0;

-- Tối ưu hóa tìm kiếm phim theo Thể loại và Ngày phát hành
CREATE INDEX IX_Movies_Genre_Active ON Movies(Genre) WHERE IsDeleted = 0;
CREATE INDEX IX_Movies_ReleaseDate_Active ON Movies(ReleaseDate) WHERE IsDeleted = 0;

-- Tối ưu hóa JOIN lấy ghế theo phòng chiếu (CinemaHallId)
CREATE INDEX IX_Seats_CinemaHall_Active ON Seats(CinemaHallId) WHERE IsDeleted = 0;

-- Tối ưu hóa kiểm tra chồng lặp lịch chiếu và tìm kiếm ngày chiếu đơn lẻ (ShowDate)
-- Ghi chú tối ưu: Index này có StartTime ở đầu, do đó nó hoạt động cực kỳ hiệu quả
-- cho cả việc lọc theo (StartTime, CinemaHallId) lẫn lọc ngày chiếu đơn lẻ (StartTime).
-- Việc chèn thêm index đơn trên StartTime là không cần thiết, tránh dư thừa.
CREATE INDEX IX_Showtimes_StartTime_Hall_Active ON Showtimes(StartTime, CinemaHallId) WHERE IsDeleted = 0;
CREATE INDEX IX_Showtimes_Movie_Active ON Showtimes(MovieId) WHERE IsDeleted = 0;

-- Tối ưu hóa lọc đơn đặt hàng theo Người dùng
CREATE INDEX IX_Bookings_User_Active ON Bookings(UserId) WHERE IsDeleted = 0;
CREATE INDEX IX_Bookings_Status_Active ON Bookings(Status) WHERE IsDeleted = 0;

-- Tối ưu hóa truy xuất vé chi tiết
CREATE INDEX IX_Tickets_Booking ON Tickets(BookingId);
CREATE INDEX IX_Tickets_Seat ON Tickets(SeatId);

-- Chống đặt trùng ghế cùng lịch chiếu (Double Booking Prevention)
CREATE UNIQUE INDEX UX_Tickets_Showtime_Seat ON Tickets(ShowtimeId, SeatId) WHERE IsDeleted = 0;


-- ----------------------------------------------------------------------------
-- 2. TỐI ƯU HÓA TRUY VẤN BÁO CÁO PHÂN TÍCH (OLAP / REPORTING INDEXES)
-- ----------------------------------------------------------------------------

-- Báo cáo 1: Doanh thu theo phim (MovieId)
-- Tối ưu hóa: Sử dụng Composite Covering Index trên Bookings(ShowtimeId, Status)
-- và INCLUDE cột TotalPrice để SQL Server lấy số tiền ngay từ Index mà không cần Key Lookup.
-- Ghi chú tối ưu Phần 1: Do chỉ mục này bắt đầu bằng ShowtimeId, nó hoàn toàn thay thế và
-- bao phủ chỉ mục cũ IX_Bookings_Showtime_Active. Chỉ mục cũ đã được gỡ bỏ để tránh ghi trùng lặp dữ liệu (DML).
CREATE INDEX IX_Bookings_Showtime_Status_Include
ON Bookings(ShowtimeId, Status)
INCLUDE (TotalPrice)
WHERE IsDeleted = 0;

-- Báo cáo 2: Vé bán theo ngày (ShowDate/BookingDate)
-- Tối ưu hóa: Thêm chỉ mục cho ngày thực hiện giao dịch (BookingTime) để báo cáo doanh số theo ngày chạy tức thời.
CREate INDEX IX_Bookings_BookingTime_Active
ON Bookings(BookingTime)
--INCLUDE (TotalPrice)
WHERE IsDeleted = 0 --and Status = 'Paid';

-- Tối ưu hóa: Thêm chỉ mục cho cột tính toán BookingDate để hỗ trợ lọc/nhóm theo ngày không cần CAST() runtime.
CREATE INDEX IX_Bookings_BookingDate_Active
ON Bookings(BookingDate)
WHERE IsDeleted = 0;


-- Báo cáo 3: Suất chiếu theo phòng (RoomId / CinemaHallId)
-- Tối ưu hóa: Thêm chỉ mục độc lập cho CinemaHallId trên Showtimes để hỗ trợ lọc phòng chiếu cực nhanh
-- khi không kết hợp điều kiện thời gian.
CREATE INDEX IX_Showtimes_CinemaHall_Active ON Showtimes(CinemaHallId) WHERE IsDeleted = 0;

SELECT 
    i.name AS IndexName,
    i.index_id,
    i.type_desc AS IndexType,
    c.name AS ColumnName,
    ic.is_included_column,
    i.is_unique,
    i.is_primary_key,
    i.is_unique_constraint,
    i.filter_definition
FROM sys.indexes i
JOIN sys.index_columns ic 
    ON i.object_id = ic.object_id AND i.index_id = ic.index_id
JOIN sys.columns c 
    ON ic.object_id = c.object_id AND ic.column_id = c.column_id
WHERE i.object_id = OBJECT_ID('dbo.Bookings')
ORDER BY i.name, ic.key_ordinal;
