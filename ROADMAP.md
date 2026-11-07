# Lộ trình nâng cấp JobHub thành đồ án chuyên ngành

Phạm vi thống nhất: đồng bộ giao diện; phỏng vấn video với nhà tuyển dụng; luyện
phỏng vấn AI theo công việc ứng tuyển; CV theo mẫu có thể sắp xếp từng khối.
Triển khai giao diện trước. Mỗi chặng phải chạy và được kiểm tra trước khi chuyển tiếp.

**Nền tảng:** chỉ phát triển website, ưu tiên trải nghiệm trên máy tính. Chưa triển
khai ứng dụng mobile; các chặng tiếp theo đều thực hiện trong website hiện có.

**Trạng thái hiện tại:** đã hoàn thiện chặng giao diện. Người dùng xác nhận chặng
phỏng vấn video đã hoàn thành sau khi thử gọi giữa hai iPhone và gọi bằng laptop.
Lỗi âm thanh trên PC riêng được bỏ qua theo yêu cầu; không sửa code cuộc gọi.
Hạ tầng TURN riêng chưa được nghiệm thu; kiểm chứng phần này khi triển khai chính
thức nếu cần. Chặng AI và CV kéo thả chưa triển khai.
Hướng dẫn chặng 2 nằm trong [INTERVIEWS.md](INTERVIEWS.md).
Thứ tự tiếp theo đã thống nhất: hoàn thiện CV trước, AI là chức năng làm cuối cùng,
sau đó kiểm thử tổng thể và hoàn thiện đồ án.

| Chặng | Công việc | Điều kiện hoàn thành |
|---|---|---|
| 1 — Giao diện (đã hoàn thiện) | Thống nhất thiết kế trang công khai, tài khoản và cả Admin/Employer/Candidate; ưu tiên web desktop; menu theo quyền; bảng, form, thông báo, trạng thái trống | Đã kiểm tra 30 màn hình web, menu và modal; 258 kiểm tra tích hợp và 308 kiểm tra trên Chrome; PDF và chat hiện có vẫn hoạt động |
| 2 — Phỏng vấn video (người dùng đã xác nhận hoàn thành) | Khi hồ sơ ở trạng thái Phỏng vấn, tạo phiên và quyền vào phòng cho đúng hai bên; mic/camera; tham gia, rời phòng, kết thúc phiên và xử lý mất kết nối | Đã kiểm tra tự động quyền, vòng đời phiên và truyền RTP; người dùng xác nhận gọi được trên iPhone/laptop. TURN riêng còn kiểm chứng khi triển khai chính thức |
| 3 — CV hoàn thiện (tiếp theo) | Chọn mẫu, điền thông tin; kéo thả các khối trong vùng của mẫu; lưu cả dữ liệu và bố cục; bổ sung mẫu có thiết kế khác nhau | Mở lại giữ bố cục; PDF khớp xem trước, đúng tiếng Việt, không tràn nội dung; ứng tuyển tiếp tục giữ bản CV lúc nộp |
| 4 — AI tự xây (chức năng cuối cùng) | Chốt mức yêu cầu với giảng viên; chuẩn bị dữ liệu chuyên ngành, tiêu chí đánh giá và mô hình; xây phiên phỏng vấn gắn với JD/CV; avatar tĩnh, trạng thái xử lý; hội thoại văn bản trước, cân nhắc đọc câu hỏi bằng trình duyệt | Có dữ liệu và kết quả đánh giá đối chứng; câu hỏi bám công việc; hỏi tiếp có ngữ cảnh; báo cáo cuối phiên; kiểm tra trên máy triển khai thật |
| 5 — Hoàn thiện đồ án | Kiểm thử toàn luồng ba vai trò; sửa lỗi; dữ liệu demo có kiểm soát; triển khai; sơ đồ, báo cáo và kịch bản bảo vệ | Chạy từ source mới, nâng cấp database và khôi phục sao lưu được; có bằng chứng kiểm thử và hướng dẫn trình diễn |

## Các quyết định về AI còn chờ xác nhận

Người dùng đang nghiêng về yêu cầu **tự huấn luyện hoặc fine-tune**, sẽ xác nhận lại
với giảng viên. Chưa coi việc gọi Gemini/OpenAI là phương án đáp ứng yêu cầu mới.
Chức năng đánh giá CV bằng Gemini có sẵn được giữ nguyên trong đợt giao diện;
đó không phải phần AI phỏng vấn tự xây của chặng 4.

Trước khi thực hiện chặng 4, cần xác định:

- Fine-tune mô hình mở có được chấp nhận hay phải huấn luyện từ đầu; hai phạm vi
  này có yêu cầu dữ liệu, phần cứng và thời gian rất khác nhau.
- Ngành/vị trí ưu tiên, ngôn ngữ phỏng vấn, thời hạn và máy dùng để huấn luyện/chạy.
- Nguồn dữ liệu được phép dùng; tách tập huấn luyện/đánh giá; bộ câu hỏi và tiêu chí
  chấm được kiểm chứng, tránh chỉ đánh giá bằng một vài câu demo.
- Ưu tiên không mất phí API. Phần cứng huấn luyện/chạy vẫn cần được tính vào nguồn lực.
- Giao diện AI: ảnh tĩnh với vòng trạng thái, không yêu cầu avatar chuyển động.

## Quy tắc thực hiện

- Giữ ASP.NET Core MVC, EF Core và SQL Server hiện có; nâng cấp theo từng chặng.
- Chặng giao diện không thêm nút hoặc dữ liệu giả cho chức năng chưa triển khai.
- Giữ quyền truy cập, CSRF, PDF riêng tư và lịch sử CV đã nộp.
- Migration dữ liệu chỉ thực hiện khi cần cho chức năng, có sao lưu và kiểm tra.
- Chốt tiến độ thời gian sau khi biết hạn nộp, quy mô nhóm và yêu cầu AI chính thức.

## Chặng 1 — Hướng thiết kế

JobHub dùng giao diện sáng, xanh ngọc, nền trung tính và các thành phần dùng chung.
Trang công khai tập trung tìm việc; ba khu vực dùng thanh điều hướng theo vai trò.
Biểu mẫu có nhãn và lỗi rõ ràng, bảng có vùng cuộn trên màn hình nhỏ, trạng thái
hồ sơ phân biệt bằng cả chữ và màu. Nội dung CV trên giấy giữ thiết kế riêng của mẫu.

Kiểm tra dùng tài khoản, file và database tạm; không thêm dữ liệu demo vào database
chính. Ảnh chụp mỗi màn hình được lưu trong `artifacts/browser-tests`.
