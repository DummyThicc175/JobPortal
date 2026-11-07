Hai file `ConsolidateCv.Before.sql` và `ConsolidateCv.After.sql` là tài nguyên nội bộ
của migration `ConsolidateCvAndIntegrity`. EF Core thực hiện chúng trong cùng giao
dịch với thay đổi schema; không chạy riêng trong SSMS.

Dùng `dotnet ef database update --project JobPortal.csproj` từ thư mục gốc.
Script `SQLQuery1.sql` cũ đã được bỏ vì xóa bảng mẫu CV và chèn các mẫu chưa có
giao diện thực tế. Hai mẫu hoạt động được seed bằng EF Core.
