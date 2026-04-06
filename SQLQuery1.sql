SELECT Id, Name, Category, Thumbnail FROM CVTemplates;
SELECT Id, Name, TemplateViewPath FROM CVTemplates;

UPDATE CVTemplates
SET TemplateViewPath = 'Templates/T1' -- Chỉ để tên thư mục và tên file
WHERE Id = 1;

-- Làm tương tự cho các Id khác hoặc chạy lệnh dưới đây cho tất cả
UPDATE CVTemplates SET TemplateViewPath = 'Templates/T' + CAST(Id AS NVARCHAR);