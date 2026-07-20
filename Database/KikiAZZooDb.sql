-- Script tạo database KikiAzoo cho SQL Server
-- Chạy script này nếu bạn muốn tạo database thủ công trước khi chạy ứng dụng

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'KikiAZZooDb')
BEGIN
    CREATE DATABASE KikiAZZooDb;
END
GO

USE KikiAZZooDb;
GO

-- Lưu ý: Bảng sẽ được tự động tạo bởi Entity Framework Migrations khi chạy ứng dụng.
-- Chỉ cần đảm bảo connection string trong appsettings.json trỏ đúng Server và Database.

-- Ví dụ connection string:
-- Server=(localdb)\mssqllocaldb;Database=KikiAZZooDb;Trusted_Connection=True;TrustServerCertificate=True
-- Hoặc: Server=YOUR_SERVER_NAME;Database=KikiAZZooDb;Trusted_Connection=True;TrustServerCertificate=True

-- Tài khoản Admin mặc định (tự tạo khi chạy app lần đầu):
-- Email: admin@kikiazoo.com
-- Password: Admin@123
