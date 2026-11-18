# Lộ trình nâng cấp JobHub thành đồ án chuyên ngành

Phạm vi thống nhất: đồng bộ giao diện; phỏng vấn video với nhà tuyển dụng; luyện
phỏng vấn AI theo công việc ứng tuyển; CV theo mẫu có thể sắp xếp từng khối.
Triển khai giao diện trước. Mỗi chặng phải chạy và được kiểm tra trước khi chuyển tiếp.

**Nền tảng:** website responsive dùng trên máy tính, máy tính bảng và điện thoại.
Không làm ứng dụng mobile riêng; các chặng tiếp theo thực hiện trong website hiện có.

**Trạng thái hiện tại:** đã hoàn thiện chặng giao diện. Người dùng xác nhận chặng
phỏng vấn video đã hoàn thành sau khi thử gọi giữa hai iPhone và gọi bằng laptop.
Lỗi âm thanh trên PC riêng được bỏ qua theo yêu cầu; không sửa code cuộc gọi.
Hạ tầng TURN riêng chưa được nghiệm thu; kiểm chứng phần này khi triển khai chính
thức nếu cần. Chặng CV đã triển khai sáu mẫu, kéo thả khối, lưu bố cục và PDF
nhiều trang; giữ đánh giá CV bằng Gemini. Người dùng đã xác nhận phần CV hoàn thành.
Chặng AI đã triển khai bản đầu theo hướng 1: Qwen3 có sẵn chạy cục bộ qua Ollama,
PhoWhisper-small nhận tiếng Việt qua Transformers CPU; đã tách lời gọi chấm và
tạo câu để tránh lẫn nhận xét. Chưa huấn luyện/fine-tune. Xem [AI_INTERVIEWS.md](AI_INTERVIEWS.md).
Hướng dẫn chặng 2 nằm trong [INTERVIEWS.md](INTERVIEWS.md).
Theo yêu cầu mới, đã bổ sung giao diện web điện thoại trước AI: menu cảm ứng,
bảng dạng thẻ, form dễ nhập, CV chuyển chế độ chỉnh sửa/xem trước và kéo cảm ứng,
chat thích ứng vùng nhìn, phòng phỏng vấn dùng được trên màn hình nhỏ.
Thứ tự tiếp theo: thử AI trên máy yếu và micro điện thoại thực tế, đối chiếu chất
lượng câu hỏi/nhận xét với giảng viên, rồi kiểm thử tổng thể và hoàn thiện đồ án.

| Chặng | Công việc | Điều kiện hoàn thành |
|---|---|---|
| 1 — Giao diện (đã hoàn thiện) | Thống nhất thiết kế trang công khai, tài khoản và cả Admin/Employer/Candidate; ưu tiên web desktop; menu theo quyền; bảng, form, thông báo, trạng thái trống | Đã kiểm tra 30 màn hình web, menu và modal; 258 kiểm tra tích hợp và 308 kiểm tra trên Chrome; PDF và chat hiện có vẫn hoạt động |
| Bổ sung — Web responsive (đã triển khai) | Cả ba khu vực, trang công khai và tài khoản; bảng có nhãn trên điện thoại; nút cảm ứng; CV theo chế độ; chat dọc/ngang và phòng phỏng vấn | Đạt 676 kiểm tra Chrome, 391 kiểm tra tích hợp, 42 kiểm tra cảm ứng và 42 kiểm tra phòng phỏng vấn; rà 1440/768/390/360 px, thêm CV 320/430 px và xoay ngang 844×390. Kiểm tra bằng Chrome giả lập cảm ứng; Safari/Android trên thiết bị thật cần người dùng thử |
| 2 — Phỏng vấn video (người dùng đã xác nhận hoàn thành) | Khi hồ sơ ở trạng thái Phỏng vấn, tạo phiên và quyền vào phòng cho đúng hai bên; mic/camera; tham gia, rời phòng, kết thúc phiên và xử lý mất kết nối | Đã kiểm tra tự động quyền, vòng đời phiên và truyền RTP; người dùng xác nhận gọi được trên iPhone/laptop. TURN riêng còn kiểm chứng khi triển khai chính thức |
| 3 — CV (người dùng đã xác nhận hoàn thành) | Sáu mẫu; chín khối kéo thả/chuyển cột; lưu/mở lại bố cục và màu; đầy đủ trường nội dung; xem trước A4 và PDF tối đa sáu trang; đánh giá CV bằng Gemini, khóa chỉ trong appsettings.json | Đã đạt 391 kiểm tra tích hợp, 355 kiểm tra Chrome và 28 kiểm tra migration SQL Server; có PDF thật cả sáu mẫu và CV ba trang, giữ đủ nội dung; hồ sơ ứng tuyển vẫn giữ bản chụp riêng |
| 4 — AI (đã triển khai bản đầu hướng 1) | Ollama/Qwen3 cục bộ; chuyên ngành, JD và CV đã nộp; 5 câu có ngữ cảnh; chấm riêng câu hiện tại; hỏi bằng chữ, trả lời bằng micro qua PhoWhisper-small; bản chữ cho xem lại; báo cáo riêng | Cần nghiệm thu nội dung chuyên ngành, nhận giọng nói tiếng Việt và tốc độ trên các máy của nhóm; chưa huấn luyện/fine-tune |
| 5 — Hoàn thiện đồ án | Kiểm thử toàn luồng ba vai trò; sửa lỗi; dữ liệu demo có kiểm soát; triển khai; sơ đồ, báo cáo và kịch bản bảo vệ | Chạy từ source mới, nâng cấp database và khôi phục sao lưu được; có bằng chứng kiểm thử và hướng dẫn trình diễn |

## Các quyết định về AI còn chờ xác nhận

Người dùng đã chọn **hướng 1: xây hệ thống phỏng vấn, dùng model mở có sẵn chạy
cục bộ**, ưu tiên cấu hình i5/UHD/12 GB RAM và máy RTX 2050/8 GB RAM.
Nếu giảng viên yêu cầu huấn luyện/fine-tune thì vẫn cần bổ sung sau khi chốt yêu
cầu; bản hiện tại không được mô tả là model tự huấn luyện.
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
