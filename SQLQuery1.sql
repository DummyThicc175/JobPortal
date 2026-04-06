-- 1. Làm sạch bảng để nhập mới (Cẩn thận: Lệnh này sẽ xóa dữ liệu cũ trong bảng CVTemplates)
TRUNCATE TABLE CVTemplates;

-- 2. Nhập 10 mẫu CV với phân loại và đường dẫn tương ứng
INSERT INTO CVTemplates (Name, Thumbnail, Category, TemplateViewPath)
VALUES 
-- Nhóm Đơn giản (1, 2, 3)
(N'Mẫu Đơn Giản Classic', 'images/templates/cv1.jpg', N'Đơn giản', 'Templates/T1'),
(N'Mẫu Đơn Giản Tối Ưu', 'images/templates/cv2.jpg', N'Đơn giản', 'Templates/T2'),
(N'Mẫu Đơn Giản Thanh Lịch', 'images/templates/cv3.jpg', N'Đơn giản', 'Templates/T3'),

-- Nhóm Hiện đại (4, 5, 6)
(N'Mẫu Hiện Đại Basic', 'images/templates/cv4.jpg', N'Hiện đại', 'Templates/T4'),
(N'Mẫu Hiện Đại Sáng Tạo', 'images/templates/cv5.jpg', N'Hiện đại', 'Templates/T5'),
(N'Mẫu Hiện Đại GenZ', 'images/templates/cv6.jpg', N'Hiện đại', 'Templates/T6'),

-- Nhóm Chuyên nghiệp (7, 8, 9, 10)
(N'Mẫu Chuyên Nghiệp Leader', 'images/templates/cv7.jpg', N'Chuyên nghiệp', 'Templates/T7'),
(N'Mẫu Chuyên Nghiệp HR', 'images/templates/cv8.jpg', N'Chuyên nghiệp', 'Templates/T8'),
(N'Mẫu Chuyên Nghiệp Global', 'images/templates/cv9.jpg', N'Chuyên nghiệp', 'Templates/T9'),
(N'Mẫu Chuyên Nghiệp IT', 'images/templates/cv10.jpg', N'Chuyên nghiệp', 'Templates/T10');

-- 3. Kiểm tra lại dữ liệu sau khi nhập
SELECT * FROM CVTemplates;

UPDATE CVTemplates SET Thumbnail = 'images/templates/t1.jpg' WHERE Id = 1;
UPDATE CVTemplates SET Thumbnail = 'images/templates/t2.jpg' WHERE Id = 2;
UPDATE CVTemplates SET Thumbnail = 'images/templates/t3.jpg' WHERE Id = 3;
UPDATE CVTemplates SET Thumbnail = 'images/templates/t4.jpg' WHERE Id = 4;
UPDATE CVTemplates SET Thumbnail = 'images/templates/t5.jpg' WHERE Id = 5;
UPDATE CVTemplates SET Thumbnail = 'images/templates/t6.jpg' WHERE Id = 6;
UPDATE CVTemplates SET Thumbnail = 'images/templates/t7.jpg' WHERE Id = 7;
UPDATE CVTemplates SET Thumbnail = 'images/templates/t8.jpg' WHERE Id = 8;
UPDATE CVTemplates SET Thumbnail = 'images/templates/t9.jpg' WHERE Id = 9;
UPDATE CVTemplates SET Thumbnail = 'images/templates/t10.jpg' WHERE Id = 10;