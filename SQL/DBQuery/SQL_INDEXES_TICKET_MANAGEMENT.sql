-- ============================================================================
-- MODULE: INDEXES TICKET MANAGEMENT
-- Mô tả: Khởi tạo tất cả các chỉ mục Non-Clustered Indexes và Filtered Indexes
--        nhằm tối ưu hóa tốc độ truy vấn, tăng cường hiệu năng cho các API.
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
