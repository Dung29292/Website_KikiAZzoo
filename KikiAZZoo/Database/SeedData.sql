-- KikiAzoo: Dữ liệu mẫu được seed tự động qua DbInitializer khi chạy ứng dụng.
-- Nếu cần reset database hoàn toàn, chạy lệnh sau trong Package Manager Console:
--   Drop-Database
--   Update-Database
-- Hoặc xóa database KikiAZZooDb trong SSMS rồi chạy lại ứng dụng.

-- Các bảng lookup (lưu trong SQL thay vì hardcode):
--   CategoryGroups  : Thú cưng, Thức ăn, Đồ chơi, Phụ kiện, Quần áo, Chăm sóc
--   PetTypes        : Chó, Mèo, Chim, Hamster, Cá cảnh, Thỏ
--   PriceRanges     : Dưới 100K, 100K-300K, 300K-500K, 500K-1M, Trên 1M
--   Services        : Spa, Tắm rửa, Khám SK, Nhận nuôi, Huấn luyện, Khách sạn
--   ProductFeatures : Đặc điểm sản phẩm hiển thị trang chi tiết

SELECT 'CategoryGroups' AS [Table], COUNT(*) AS [Rows] FROM CategoryGroups
UNION ALL SELECT 'PetTypes', COUNT(*) FROM PetTypes
UNION ALL SELECT 'PriceRanges', COUNT(*) FROM PriceRanges
UNION ALL SELECT 'Services', COUNT(*) FROM Services
UNION ALL SELECT 'Categories', COUNT(*) FROM Categories
UNION ALL SELECT 'Products', COUNT(*) FROM Products
UNION ALL SELECT 'ProductFeatures', COUNT(*) FROM ProductFeatures;
