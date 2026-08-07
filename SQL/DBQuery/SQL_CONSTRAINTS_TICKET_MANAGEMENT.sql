-- ============================================================================
-- MODULE: CONSTRAINTS TICKET MANAGEMENT
-- Mô tả: Cấu hình tất cả các ràng buộc ngoại khóa (Foreign Keys), ràng buộc kiểm tra
--        (Check Constraints) và các ràng buộc duy nhất (Unique) để bảo vệ tính toàn vẹn dữ liệu.
-- ============================================================================

-- ----------------------------------------------------------------------------
-- 1. FOREIGN KEY CONSTRAINTS (MỐI QUAN HỆ CHÍNH GIỮA CÁC BẢNG)
-- ----------------------------------------------------------------------------

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

-- Tickets tham chiếu Showtimes
ALTER TABLE Tickets
ADD CONSTRAINT FK_Tickets_Showtimes FOREIGN KEY (ShowtimeId) REFERENCES Showtimes(Id);

-- Ràng buộc chống trùng ghế trong cùng một đơn đặt vé
ALTER TABLE Tickets
ADD CONSTRAINT UQ_Tickets UNIQUE (BookingId, SeatId);


-- ----------------------------------------------------------------------------
-- 2. AUDIT FOREIGN KEYS (RÀNG BUỘC CHO TRƯỜNG NGƯỜI TẠO / NGƯỜI CẬP NHẬT)
-- ----------------------------------------------------------------------------

-- Users Self-Referencing Audits
ALTER TABLE Users ADD CONSTRAINT FK_Users_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES Users(Id);
ALTER TABLE Users ADD CONSTRAINT FK_Users_UpdatedBy FOREIGN KEY (UpdatedBy) REFERENCES Users(Id);

-- Movies Audits
ALTER TABLE Movies ADD CONSTRAINT FK_Movies_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES Users(Id);
ALTER TABLE Movies ADD CONSTRAINT FK_Movies_UpdatedBy FOREIGN KEY (UpdatedBy) REFERENCES Users(Id);

-- CinemaHalls Audits
ALTER TABLE CinemaHalls ADD CONSTRAINT FK_CinemaHalls_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES Users(Id);
ALTER TABLE CinemaHalls ADD CONSTRAINT FK_CinemaHalls_UpdatedBy FOREIGN KEY (UpdatedBy) REFERENCES Users(Id);

-- Seats Audits
ALTER TABLE Seats ADD CONSTRAINT FK_Seats_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES Users(Id);
ALTER TABLE Seats ADD CONSTRAINT FK_Seats_UpdatedBy FOREIGN KEY (UpdatedBy) REFERENCES Users(Id);

-- Showtimes Audits
ALTER TABLE Showtimes ADD CONSTRAINT FK_Showtimes_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES Users(Id);
ALTER TABLE Showtimes ADD CONSTRAINT FK_Showtimes_UpdatedBy FOREIGN KEY (UpdatedBy) REFERENCES Users(Id);

-- Bookings Audits
ALTER TABLE Bookings ADD CONSTRAINT FK_Bookings_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES Users(Id);
ALTER TABLE Bookings ADD CONSTRAINT FK_Bookings_UpdatedBy FOREIGN KEY (UpdatedBy) REFERENCES Users(Id);

-- Tickets Audits
ALTER TABLE Tickets ADD CONSTRAINT FK_Tickets_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES Users(Id);
ALTER TABLE Tickets ADD CONSTRAINT FK_Tickets_UpdatedBy FOREIGN KEY (UpdatedBy) REFERENCES Users(Id);


-- ----------------------------------------------------------------------------
-- 3. CHECK CONSTRAINTS (RÀNG BUỘC MIỀN GIÁ TRỊ)
-- ----------------------------------------------------------------------------

-- Users: Giới hạn các phân quyền vai trò được chấp nhận
ALTER TABLE Users
ADD CONSTRAINT CK_Users_Role CHECK (Role IN (N'Admin', N'Customer'));

-- CinemaHalls: Sức chứa phòng chiếu phải lớn hơn 0
ALTER TABLE CinemaHalls
ADD CONSTRAINT CK_CinemaHalls_TotalSeats CHECK (TotalSeats > 0);

-- Seats: Giới hạn loại ghế
ALTER TABLE Seats
ADD CONSTRAINT CK_Seats_SeatType CHECK (SeatType IN (N'Standard', N'VIP', N'Sweetbox'));

-- Showtimes: Giá vé gốc không được âm
ALTER TABLE Showtimes
ADD CONSTRAINT CK_Showtimes_BasePrice CHECK (BasePrice >= 0);

-- Bookings: Tổng tiền không được âm và ràng buộc trạng thái đơn hàng
ALTER TABLE Bookings
ADD CONSTRAINT CK_Bookings_TotalPrice CHECK (TotalPrice >= 0);

ALTER TABLE Bookings
ADD CONSTRAINT CK_Bookings_Status CHECK (Status IN (N'Pending', N'Paid', N'Cancelled'));
