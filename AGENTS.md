# Quy ước mã nguồn JobPortal

- Các hàm do dự án tự viết dùng tên **tiếng Việt không dấu**, thể hiện rõ hành động.
  C# dùng PascalCase, ví dụ `LayHoSoAsync`, `CapNhatTrangThai`; JavaScript dùng
  camelCase, ví dụ `guiTinNhan`, `hienThiThongBao`. Hàm bất đồng bộ C# giữ hậu tố
  `Async`. Áp dụng cả cho hàm hỗ trợ và kiểm thử được viết thêm.
- Giữ đúng tên bắt buộc khi override/implement API của framework hoặc thư viện,
  như `OnModelCreating`, `OnGetAsync`, `OnPostAsync`, `ExecuteAsync`, `Validate`,
  `DisposeAsync`, và các hàm EF migration được sinh tự động.
- Khi đổi tên hàm, cập nhật đầy đủ chỗ gọi trong C#, Razor, JavaScript và kiểm thử.
  Duy trì các URL và giao thức SignalR hiện có bằng `ActionName`/`HubMethodName`;
  không đổi chuỗi tên endpoint chỉ để khớp với tên hàm mới trong C#.
- Không đổi tên bảng, cột, property dữ liệu hoặc thêm migration chỉ vì đổi tên hàm.
- Dự án chỉ phát triển website, ưu tiên máy tính. AI phỏng vấn làm sau phần CV.
- Cấu hình chứa khóa API và dữ liệu riêng tư không đưa vào Git hoặc log công khai.
- Khóa và model Gemini chỉ đọc từ `appsettings.json` tại thư mục gốc ứng dụng qua
  `CauHinhGeminiCucBo`; không chuyển khóa sang biến môi trường, User Secrets,
  `appsettings.Development.json`, JavaScript hoặc file mẫu.
