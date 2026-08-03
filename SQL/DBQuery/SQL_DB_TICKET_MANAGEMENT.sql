-- ============================================================================
-- MASTER SCRIPT: DATABASE TICKET MANAGEMENT
-- Mô tả: File tổng hợp (Master) quản lý việc thiết lập và khởi tạo cơ sở dữ liệu.
--        Dự án đã được mô-đun hóa thành 4 file thành phần riêng biệt để dễ dàng
--        bảo trì, mở rộng và phát triển theo mô hình chuyên nghiệp.
--
-- Hướng dẫn chạy (SQLCMD Mode):
-- 1. Bật chế độ "SQLCMD Mode" trong SQL Server Management Studio (SSMS) (Query -> SQLCMD Mode).
-- 2. Đảm bảo 4 file thành phần nằm trong cùng thư mục với file này.
-- 3. Chạy file này để thực thi tuần tự việc khởi tạo.
-- ============================================================================

USE master;
DROP DATABASE IF EXISTS TicketManagementDB;

CREATE DATABASE TicketManagementDB;
GO
USE TicketManagementDB;
GO

-- 1. Khởi tạo cấu trúc các bảng (Schema)
PRINT 'Executing SQL_SCHEMA_TICKET_MANAGEMENT.sql...';
:r "F:\Design Web\ASP.NET PROJECTS\TICKETMANAGEMENT\API_TICKET_APPLICATION\SQL\DBQuery\SQL_SCHEMA_TICKET_MANAGEMENT.sql"
GO

-- 2. Thiết lập ràng buộc (Constraints)
PRINT 'Executing SQL_CONSTRAINTS_TICKET_MANAGEMENT.sql...';
:r "F:\Design Web\ASP.NET PROJECTS\TICKETMANAGEMENT\API_TICKET_APPLICATION\SQL\DBQuery\SQL_CONSTRAINTS_TICKET_MANAGEMENT.sql"
GO

-- 3. Thiết lập chỉ mục (Indexes)
PRINT 'Executing SQL_INDEXES_TICKET_MANAGEMENT.sql...';
:r "F:\Design Web\ASP.NET PROJECTS\TICKETMANAGEMENT\API_TICKET_APPLICATION\SQL\DBQuery\SQL_INDEXES_TICKET_MANAGEMENT.sql"
GO

-- 4. Chèn dữ liệu mẫu thử nghiệm (Seed Data)
PRINT 'Executing SQL_SEED_DATA_TICKET_MANAGEMENT.sql...';
:r "F:\Design Web\ASP.NET PROJECTS\TICKETMANAGEMENT\API_TICKET_APPLICATION\SQL\DBQuery\SQL_SEED_DATA_TICKET_MANAGEMENT.sql"
GO

PRINT '✔ TẤT CẢ MODULE ĐÃ ĐƯỢC THỰC THI THÀNH CÔNG VÀ ĐỒNG BỘ!';
