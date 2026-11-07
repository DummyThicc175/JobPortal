# JobPortal

Website tuyển dụng dùng ASP.NET Core MVC (.NET 8), Razor, Entity Framework Core,
SQL Server, ASP.NET Core Identity và SignalR.

Lộ trình nâng cấp đồ án chuyên ngành nằm trong [ROADMAP.md](ROADMAP.md).
Đợt giao diện dùng hệ thống thiết kế chung cho cả Admin, Employer, Candidate,
trang công khai và tài khoản. Đã triển khai phỏng vấn video gắn với hồ sơ;
hướng dẫn sử dụng và cấu hình HTTPS/TURN trong [INTERVIEWS.md](INTERVIEWS.md).
Chưa triển khai AI phỏng vấn tự xây.
Phạm vi hiện tại chỉ phát triển website, ưu tiên máy tính; chưa phát triển app mobile.

## Cấu trúc mã nguồn

```text
JobPortal/
├── Backend/
│   ├── Program.cs                 # Khởi động và cấu hình ứng dụng
│   ├── Configuration/             # Routing tương thích với URL cũ
│   ├── Controllers/               # Trang chủ, tìm kiếm việc làm, API chat
│   ├── Areas/
│   │   ├── Admin/Controllers/     # Quản trị
│   │   ├── Candidate/Controllers/ # Các chức năng ứng viên
│   │   ├── Employer/Controllers/  # Các chức năng nhà tuyển dụng
│   │   └── Identity/Pages/Account/# PageModel C# cho các trang tài khoản
│   ├── Models/                    # Entity dữ liệu
│   ├── ViewModels/                # Dữ liệu phục vụ biểu mẫu và hiển thị
│   ├── Services/                  # CV, upload riêng tư, tìm kiếm, AI và quyền chat
│   ├── Storage/cvs/               # PDF riêng tư, bị Git bỏ qua
│   ├── Data/                      # ApplicationDbContext
│   │   └── Scripts/               # Script SQL có sẵn của dự án
│   ├── Migrations/                # Lịch sử migration EF Core
│   └── Hubs/                      # SignalR ChatHub
├── Frontend/
│   ├── Views/                     # Razor views dùng chung và trang công khai
│   ├── Areas/                     # Razor views theo vai trò, trang Identity
│   └── wwwroot/                   # CSS, JavaScript, thư viện, ảnh và uploads
├── Tests/JobPortal.SmokeTests/     # Kiểm tra tích hợp bằng CSDL tạm
├── Properties/launchSettings.json
├── appsettings.json
├── appsettings.Development.json
├── JobPortal.csproj
└── JobPortal.sln
```

Frontend và Backend vẫn thuộc cùng một ứng dụng ASP.NET Core MVC. Chạy một project
`JobPortal.csproj` như trước. Các file cấu hình môi trường và solution giữ ở thư mục
gốc để Visual Studio và `dotnet run` tiếp tục đọc đúng cấu hình.

## Controller theo chức năng

| Khu vực | Controller | Chức năng / đường dẫn chính |
|---|---|---|
| Công khai | `HomeController` | Trang chủ, trang thông tin, trang lỗi |
| Công khai | `JobController` | Danh sách, tìm kiếm, chi tiết việc làm: `/Job/Details/{id}` |
| Dùng chung | `ChatController` | API đối tác và lịch sử trò chuyện: `/Chat/GetChatPartners`, `/Chat/GetChatHistory` |
| Dùng chung | `InterviewController` | Phòng phỏng vấn riêng: `/Interview/Room/{id}`; tạo/kết thúc phiên cho đúng hồ sơ |
| Candidate | `ProfileController` | Xem, chỉnh sửa, lưu hồ sơ và tải ảnh/CV lên |
| Candidate | `ApplicationController` | Việc đã ứng tuyển: `/Candidate/Application`; nộp hồ sơ: `POST /Candidate/Application/Apply` |
| Candidate | `SavedJobController` | Việc đã lưu: `/Candidate/SavedJob`; lưu việc: `POST /Candidate/SavedJob/Save` |
| Candidate | `CVController` | Chọn mẫu và tạo CV: `/Candidate/CV/SelectTemplate`, `/Candidate/CV/Create?templateId=1` |
| Candidate | `CVReviewController` | Đánh giá CV bằng Gemini: `POST /Candidate/CVReview/Analyze` |
| Employer | `JobController` | Xem, đăng, sửa và xóa tin của nhà tuyển dụng |
| Employer | `ApplicantController` | Tất cả ứng viên: `/Employer/Applicant`; ứng viên theo tin: `/Employer/Applicant/ByJob?jobId=...` |
| Employer | `ChatController` | Giao diện nhắn tin: `/Employer/Chat` |
| Admin | `DashboardController` | Thống kê |
| Admin | `CategoriesController` | Quản lý ngành nghề |
| Admin | `JobManagerController` | Duyệt và quản lý tin tuyển dụng |
| Admin | `UserManagerController` | Quản lý tài khoản và quyền |

Đăng nhập, đăng ký, đăng xuất vẫn dùng Razor Pages của ASP.NET Core Identity.
Phần C# nằm trong Backend, các file `.cshtml` nằm trong Frontend.

Các URL cũ như `/Home/JobDetails/1`, `/Home/ApplyJob`, `/Home/SaveJob`,
`/Candidate/Profile/AppliedJobs`, `/Candidate/Profile/SavedJobs`,
`/Candidate/Profile/SelectTemplate`, `/Candidate/Profile/CreateCV`,
`/Candidate/Profile/AnalyzeCVByAi` và `/Employer/Job/Candidates1`,
`/Employer/Job/Candidates`, `/Employer/Job/Messages` vẫn được ánh xạ đến
controller tương ứng trong `Backend/Configuration/LegacyRouteExtensions.cs`.
Các URL cũ nhận POST trực tiếp, không chuyển hướng làm mất dữ liệu biểu mẫu.

## Chạy dự án

Cần .NET 8 SDK hoặc SDK mới hơn hỗ trợ target .NET 8, ASP.NET Core Runtime 8
và SQL Server có database của dự án. Kiểm tra `ConnectionStrings:DefaultConnection`
trong `appsettings.json` hoặc cấu hình môi trường. Cấu hình mặc định trỏ đến
SQL Server Express cục bộ (`Server=.\SQLEXPRESS`), database `JobPortalDb`, xác thực Windows.

Tại thư mục gốc:

```powershell
# Khi lấy source lần đầu và chưa có cấu hình cục bộ:
Copy-Item appsettings.example.json appsettings.json
dotnet tool restore
dotnet restore JobPortal.csproj
dotnet build JobPortal.csproj
dotnet run --project JobPortal.csproj
```

Chỉ sao chép file mẫu khi chưa có `appsettings.json`. Điền khóa vào `Gemini:ApiKey`
trong file cục bộ. Các file `appsettings*.json` được bỏ qua trong Git, ngoại trừ
`appsettings.example.json` không chứa khóa. Controller AI đọc khóa qua cấu hình.

Ứng dụng tiếp tục dùng các profile trong `Properties/launchSettings.json`.
Trong Visual Studio, mở `JobPortal.sln` và chọn project `JobPortal` để chạy.

`dotnet-ef` được cố định ở phiên bản 8.0.28 trong `dotnet-tools.json` của dự án.
Chạy `dotnet tool restore` sau khi lấy source để dùng lệnh `dotnet ef`, không cần
cài bản global. SDK mới hơn vẫn build ứng dụng theo target `net8.0` của project.

Nếu cần khởi tạo database mới bằng các migration có sẵn:

```powershell
dotnet ef database update --project JobPortal.csproj
```

Khi bổ sung migration về sau, đặt đúng thư mục Backend:

```powershell
dotnet ef migrations add TenThayDoi --project JobPortal.csproj --output-dir Backend/Migrations
```

Ứng dụng không tự chạy migration khi khởi động. Bản sửa có migration
`20261104100316_ConsolidateCvAndIntegrity`: hợp nhất `UserCVs` vào `CVs`, chuyển
JSON CV vào các bảng chi tiết có khóa ngoại `CVId`, liên kết hồ sơ bằng `UserId`,
bỏ các cột tên công ty/ngành nghề trùng lặp và bổ sung ràng buộc/index.
Migration `20261104182819_AddInterviewSessions` bổ sung phiên phỏng vấn và tạo
phòng cho các hồ sơ đã ở trạng thái Phỏng vấn; giữ nguyên dữ liệu ứng tuyển/CV.

Sao lưu SQL Server trước khi cập nhật database cũ. Migration chuyển dữ liệu hồ sơ,
CV và đơn ứng tuyển trong cùng giao dịch; dừng nếu email chủ sở hữu không xác định,
đơn bị trùng hoặc các cột hồ sơ không còn sử dụng vẫn chứa dữ liệu. Các trường hợp
này cần xử lý dữ liệu trước khi chạy lại. Không dùng `database update` về migration
cũ để hoàn tác: cấu trúc đã hợp nhất cần phục hồi bản sao lưu trước nâng cấp.

## Giao diện và tài nguyên tĩnh

`JobPortal.csproj` ánh xạ các Razor view trong `Frontend` về đường dẫn ảo quen thuộc
`/Views/...` và `/Areas/...` khi biên dịch. Vì vậy các layout, Tag Helper và trang
Identity tiếp tục được tìm thấy mà không cần sao chép view về thư mục gốc.

Web root là `Frontend/wwwroot`. Ảnh đại diện PNG/JPEG tối đa 2 MB nằm trong
`uploads/avatars`. PDF tối đa 10 MB nằm ngoài web root tại `Backend/Storage/cvs`;
có thể đặt `Storage:RootPath` thành thư mục lưu trữ bền vững khi triển khai.
`/CV/Download/{id}` kiểm tra chủ CV, quản trị viên hoặc nhà tuyển dụng nhận hồ sơ.
Các liên kết xem CV trả PDF ở chế độ `inline` để mở trực tiếp trong trình duyệt,
không bắt buộc tải xuống. Người xem vẫn có thể tự lưu bằng thanh công cụ PDF.
URL `/uploads/cvs/{fileName}` cũ cũng đi qua kiểm tra quyền. Khi khởi động,
ứng dụng chuyển PDF cũ sang kho riêng tư, so sánh nội dung trước khi xóa bản công khai.
CSS chính nằm tại `Frontend/wwwroot/css/portal.css`, tương tác giao diện tại
`Frontend/wwwroot/js/portal.js`. Hai layout chính là `_Layout.cshtml` (công khai)
và `_WorkspaceLayout.cshtml` (ba vai trò); các layout cũ được giữ làm lớp tương thích.
Icon dùng SVG cục bộ; bộ Bootstrap Icons dùng trong mẫu CV cũng được lưu cục bộ.

Publish từ thư mục gốc:

```powershell
dotnet publish JobPortal.csproj -o artifacts/publish
Set-Location artifacts/publish
dotnet JobPortal.dll
```

Bản publish chứa cả `Frontend/wwwroot` và các tài nguyên cần thiết. Sao lưu kho
PDF/ảnh cùng database; khi đổi thư mục triển khai cần giữ lại các file đã tải lên.
Bootstrap, SignalR và html2pdf được phục vụ từ thư viện cục bộ.

## Hành vi các chức năng đã sửa

- CV builder lưu nội dung, các mục chi tiết, màu sắc và PDF thật. Mở lại sẽ tải
  dữ liệu đã lưu. Nút tải PDF thực hiện lưu trước; không có CV mặc định giả.
- Ứng tuyển chỉ nhận tin đã duyệt và CV có file thật. Mỗi đơn giữ một bản CV tại
  thời điểm nộp; cập nhật CV hiện tại không đổi hồ sơ đã gửi. Đơn cũ thiếu CV không
  được tự gán file mẫu.
- Nhà tuyển dụng cập nhật trạng thái đơn thuộc tin của mình. Sửa nội dung tin đưa
  tin về chờ duyệt. Xóa tin đã có đơn hoặc được lưu sẽ gỡ hiển thị và giữ lịch sử.
- Đăng ký chỉ nhận Candidate/Employer. POST có CSRF; khóa hoặc đổi quyền tài khoản
  kết thúc phiên đăng nhập cũ. Đăng nhập sai 5 lần tạm khóa 5 phút.
- Chat chỉ dành cho ứng viên và nhà tuyển dụng có liên hệ qua đơn ứng tuyển.
  Người gửi lấy từ phiên đăng nhập; tin gửi riêng, giới hạn độ dài/tần suất;
  lịch sử tải 50 tin mỗi lần, có nút xem trước đó.
- Chuyển hồ sơ sang Phỏng vấn tự tạo phòng video riêng. Hai bên có thể bật/tắt
  thiết bị và rời/vào lại; kết thúc phiên hoặc đổi trạng thái đóng quyền vào phòng.
  Lịch sử phiên được lưu, website không ghi hình/ghi âm. Gọi khác mạng cần cấu hình
  HTTPS/TURN và kiểm chứng trên hạ tầng thực tế.
- AI đọc khóa/model từ cấu hình, giới hạn 3 yêu cầu/phút/tài khoản, có timeout và
  kiểm tra kết quả. Điểm lưu lấy từ kết quả đã xác thực phía server; chỉnh nội dung
  CV sẽ xóa điểm cũ. Khóa API không xuất hiện trong URL hoặc mã giao diện.

## Kiểm tra tích hợp

```powershell
dotnet run --project Tests/JobPortal.SmokeTests
```

Bộ kiểm tra dùng `WebApplicationFactory`, SQLite trong bộ nhớ, file riêng và
Gemini giả lập. Kiểm tra Razor/Identity, quyền ba vai trò, URL mới/cũ, CSRF,
CV riêng tư/bất biến, upload, CV builder, AI, kiểm duyệt tin, trạng thái đơn,
phân trang, khóa tài khoản và SignalR qua WebSocket. Không ghi vào database chính.

```powershell
dotnet run --project Tests/JobPortal.DatabaseTests
dotnet run --project Tests/JobPortal.BrowserTests --configuration Fixes
dotnet run --project Tests/JobPortal.BrowserTests --configuration Fixes -- --interview-only
```

Hai bộ bổ sung cần SQL Server Express cục bộ và quyền tạo/xóa database tạm
`JobPortal_Verify_<GUID>`. DatabaseTests kiểm tra cả nâng cấp có dữ liệu cũ và
rollback khi bị chặn. BrowserTests cần Chrome hoặc Edge trên Windows, chạy ẩn
để kiểm tra tạo PDF thật từ hai mẫu và chat giữa hai phiên riêng; kiểm tra các trang
của ba vai trò ở chiều rộng 1440/390 px, menu điện thoại, modal và ảnh bị lỗi; chạy ứng dụng
từ bản build `Fixes`. File kiểm tra và ảnh chụp nằm trong `artifacts/browser-tests`.
Không bộ kiểm tra nào gọi Gemini thật. Kết nối/hạn mức Gemini thực tế cần khóa hợp lệ.
