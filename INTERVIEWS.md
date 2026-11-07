# Phỏng vấn video trên website

## Cách sử dụng

1. Nhà tuyển dụng mở **Hồ sơ ứng tuyển**, chọn trạng thái **Phỏng vấn** và bấm
   **Lưu**. Hệ thống tạo một phòng riêng cho hồ sơ đó.
2. Nhà tuyển dụng bấm **Vào phỏng vấn**. Ứng viên tải lại **Việc đã ứng tuyển** và
   bấm nút cùng tên. Hai bên dùng hai hồ sơ trình duyệt, hai trình duyệt hoặc hai máy
   riêng; các tab trong cùng một hồ sơ trình duyệt dùng chung tài khoản đăng nhập.
3. Chọn bật/tắt thiết bị trước khi vào, bấm **Vào phòng phỏng vấn** rồi cấp quyền
   micro/camera. Có thể chỉ bật micro hoặc vào mà tắt cả hai, sau đó bật thiết bị.
4. Khi cả hai tham gia, cuộc gọi bắt đầu. Các nút micro/camera điều khiển thiết bị
   thực tế; **Kết nối lại** làm mới kết nối media nếu cuộc gọi bị gián đoạn.
5. **Rời phòng** dừng thiết bị và cho phép vào lại. Chỉ nhà tuyển dụng có quyền
   **Kết thúc phiên cho cả hai**. Phiên đã kết thúc không thể sử dụng lại URL cũ.
   Nếu hồ sơ vẫn ở trạng thái Phỏng vấn, nhà tuyển dụng có thể tạo một phiên mới
   tại danh sách hồ sơ.
6. Chuyển hồ sơ sang trạng thái khác sẽ đóng phòng đang mở. Chuyển lại Phỏng vấn
   tạo phiên mới; các phiên cũ được giữ trong CSDL.

Phòng không phải liên kết công khai. Biết URL không cấp quyền truy cập; Admin và
tài khoản ngoài hồ sơ không được vào. Mỗi bên chỉ tham gia phòng từ một tab.
Website không ghi âm, ghi hình hoặc lưu nội dung SDP/ICE vào CSDL. Việc khóa/đổi
quyền tài khoản được kiểm tra lại khi gửi tín hiệu và qua heartbeat mỗi 15 giây.

## Chạy tại máy phát triển

Ứng dụng vẫn chạy chung một project; không cần dịch vụ frontend riêng.

```powershell
dotnet tool restore
dotnet ef database update --project JobPortal.csproj
dotnet run --project JobPortal.csproj --launch-profile http
```

Mở `http://localhost:5188`. Migration `AddInterviewSessions` chỉ thêm bảng phiên,
khóa ngoại và chỉ mục; các hồ sơ đã ở trạng thái Phỏng vấn cũng được tạo phòng.
Sao lưu database trước khi áp dụng migration trên môi trường có dữ liệu.

## Gọi từ hai máy / hai mạng

Camera và micro cần **HTTPS**, ngoại trừ địa chỉ localhost. Truy cập HTTP bằng IP
LAN không đáp ứng điều kiện này. Khi triển khai, dùng tên miền HTTPS với chứng chỉ
hợp lệ, cho phép WebSocket và giữ kết nối SignalR qua reverse proxy/IIS.
[Điều kiện bảo mật của getUserMedia](https://developer.mozilla.org/en-US/docs/Web/API/MediaDevices/getUserMedia).

STUN mặc định giúp tìm đường kết nối trực tiếp. Một số mạng NAT/firewall cần máy
chủ **TURN** để chuyển tiếp media; chỉ có STUN không bảo đảm gọi được khác mạng.
[Hướng dẫn TURN của WebRTC](https://webrtc.org/getting-started/turn-server).

Cấu hình trong `appsettings.json` cục bộ (đã được Git bỏ qua), hoặc biến môi trường
`Interview__TurnSharedSecret`, `Interview__TurnUrls__0`, ...:

```json
"Interview": {
  "StunUrls": ["stun:stun.l.google.com:19302"],
  "TurnUrls": [
    "turn:turn.ten-mien-cua-ban:3478?transport=udp",
    "turn:turn.ten-mien-cua-ban:3478?transport=tcp"
  ],
  "TurnSharedSecret": "dien-bi-mat-ngau-nhien-cua-may-chu-turn",
  "ForceRelay": false
}
```

TURN dùng cơ chế REST credentials của Coturn. Cấu hình tương ứng trên máy chủ:

```ini
listening-port=3478
fingerprint
realm=turn.ten-mien-cua-ban
use-auth-secret
static-auth-secret=dien-cung-bi-mat-nhu-Interview-TurnSharedSecret
min-port=49160
max-port=49200
no-cli
no-multicast-peers
```

Đây là phần cấu hình xác thực/cổng cần ghép vào cấu hình mạng của máy chủ thực tế.
Mở TCP/UDP 3478 và dải UDP relay 49160–49200; nếu TURN ở sau NAT, khai báo
`external-ip=IP_PUBLIC/IP_PRIVATE` và chuyển tiếp đúng các cổng. Nếu dùng `turns:`,
cần cấu hình thêm TLS và chứng chỉ của TURN. Xem
[cấu hình chính thức Coturn](https://github.com/coturn/coturn/blob/master/examples/etc/turnserver.conf).

Ứng dụng cấp username/password TURN ký HMAC có hạn hai giờ cho người có quyền vào
phòng; shared secret không gửi xuống trình duyệt. Nếu phiên kéo dài hơn hai giờ và
cần thiết lập kết nối mới, tải lại phòng để nhận thông tin mới. Đồng bộ giờ của
máy chủ ứng dụng và TURN.

Đặt `ForceRelay=true` để kiểm tra riêng đường TURN: cuộc gọi chỉ thành công khi
relay thực sự hoạt động. Sau đó dùng `false` để ưu tiên đường kết nối có sẵn.
Coturn là phần mềm nguồn mở; máy chủ và băng thông vẫn phụ thuộc hạ tầng bạn dùng.

## Cấu trúc và giới hạn triển khai

- `InterviewController`: trang phòng, tạo lại phiên và kết thúc phiên; POST có CSRF.
- `InterviewService`: trạng thái hồ sơ, phiên đang mở, quyền truy cập và thu hồi.
- `InterviewHub`: trao đổi SDP/ICE/trạng thái thiết bị riêng cho đúng peer đã vào.
  Giới hạn kích thước và tần suất tín hiệu; không nhận user ID người gửi từ client.
- `InterviewPresence`: hai kết nối cho mỗi phòng, một kết nối cho mỗi người.
- `InterviewSessions`: lịch sử tạo/bắt đầu/kết thúc, gắn với hồ sơ ứng tuyển; chỉ
  một phiên đang mở cho mỗi hồ sơ, được ràng buộc bằng chỉ mục unique có điều kiện.
- `interview.js`: WebRTC, perfect negotiation, thiết bị, ICE restart và khôi phục
  SignalR. Rời trang/phòng hoặc kết thúc phiên dừng các media track.

Thiết kế hiện tại chạy **một instance ứng dụng**. Presence nằm trong bộ nhớ; khởi
động lại server yêu cầu các tab gọi kết nối lại. Chạy nhiều instance cần bổ sung
presence chia sẻ và hạ tầng SignalR tương ứng trước khi nhân bản server.

## Kiểm thử

```powershell
dotnet run --project Tests/JobPortal.SmokeTests --configuration Fixes
dotnet run --project Tests/JobPortal.DatabaseTests --configuration Fixes
dotnet run --project Tests/JobPortal.BrowserTests --configuration Fixes -- --interview-only
```

SmokeTests dùng SQLite trong bộ nhớ. DatabaseTests và BrowserTests dùng SQL Server
Express, tạo rồi xóa database riêng `JobPortal_Verify_<GUID>`. Chrome chạy ẩn với
camera/micro **giả**, không sử dụng thiết bị thật. BrowserTests xác minh trạng thái
kết nối và dữ liệu RTP video/audio thực nhận, bật/tắt thiết bị, ICE restart, ngắt
transport SignalR rồi tự nối lại, trùng tab, rời/vào lại, kết thúc phiên và thu hồi
khi đổi trạng thái. Ảnh chụp nằm trong `artifacts/browser-tests`.

Kết quả trên localhost không thay thế kiểm chứng triển khai: cần thử hai máy khác
mạng bằng HTTPS, gồm cả `ForceRelay=true`, trên TURN thật trước khi đánh dấu hoàn
tất điều kiện nghiệm thu khác mạng.

Kết quả kiểm tra đợt triển khai: **319** kiểm tra tích hợp, **28** kiểm tra migration
SQL Server và **26** kiểm tra cuộc gọi trên Chrome đều qua. Log cục bộ:
`artifacts/interview-smoke.log`, `artifacts/interview-database.log`,
`artifacts/interview-browser.log`. Database dự án đã được sao lưu, xác minh bản
sao lưu và cập nhật migration phỏng vấn.
