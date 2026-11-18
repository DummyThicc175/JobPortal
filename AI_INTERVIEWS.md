# Luyện phỏng vấn AI cục bộ

Hướng 1: hệ thống tự quản lý ngữ cảnh, phiên hỏi đáp, quyền truy cập và báo cáo;
dùng Qwen3 1.7B có sẵn qua Ollama để tạo câu hỏi/nhận xét. **Chưa huấn luyện hoặc
fine-tune model.** PhoWhisper-small chuyển giọng nói tiếng Việt thành chữ tại máy chủ. Không dùng
Gemini cho phỏng vấn; đánh giá CV bằng Gemini vẫn giữ cấu hình riêng như trước.

## Chạy trên Windows

Từ thư mục gốc dự án trên ổ E, lần đầu chạy:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Backend/Scripts/KhoiDongAi.ps1 -CaiDat -TaiModel
dotnet tool restore
dotnet ef database update --project JobPortal.csproj
dotnet run --project JobPortal.csproj
```

Những lần sau chỉ cần:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Backend/Scripts/KhoiDongAi.ps1
dotnet run --project JobPortal.csproj
```

Máy đã cài bản nhận giọng nói cũ: chạy **một lần** lệnh dưới đây rồi khởi động lại
website. Script giữ nguyên khóa Gemini và các cấu hình khác trong appsettings.json:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Backend/Scripts/NangCapNhanGiongNoi.ps1 -PhienBan small
```

Máy hiện tại đã được nâng cấp. Không cần tải lại model, thêm migration hay sửa database.

Script chạy Ollama nền, chỉ lắng nghe `127.0.0.1:11434`; các thiết bị dùng web
không cần cài model. Đăng nhập ứng viên → **Luyện phỏng vấn AI** → chọn hồ sơ đã
ứng tuyển → bắt đầu. Để dùng micro từ điện thoại, mở website qua HTTPS như đường
hầm đã dùng cho phỏng vấn video. `localhost` trên máy chủ cũng dùng được micro.

Nếu hồ sơ đó còn phiên chưa kết thúc, **Bắt đầu luyện tập** sẽ mở tiếp phiên này,
giữ nguyên các câu trả lời và nhận xét đã lưu. Muốn thử một phiên hoàn toàn mới,
vào phòng cũ → **Kết thúc và xem kết quả hiện tại** → quay lại danh sách → chọn
công việc và bắt đầu. Ngày giờ bắt đầu được hiển thị ở danh sách và báo cáo.
Thay đổi bộ chấm không tự chấm lại báo cáo cũ.

Các file AI nằm trong `Backend/Storage/AI`, tức ổ E nếu dự án nằm trên ổ E:

- `models`: Qwen3 1.7B, khoảng 1,4 GB theo đơn vị của nhà phát hành.
- `speech/phowhisper-small-source`: model chính thức của VinAI, checkpoint khoảng
  967 MB, cùng tokenizer/cấu hình. Bộ chạy dùng Transformers/PyTorch CPU trực tiếp,
  không chuyển model sang GGML. Không dùng lại Whisper base để nhận bản ghi mới.
- `tools/python`: Python portable và thư viện CPU; `tools/pip-cache` giữ gói cài.
  Dành khoảng 2 GB cho công cụ và cache. Không cần cài Python vào ổ C.
- `ollama`: bộ chạy CPU khoảng 85 MB. Script chỉ tải phần CPU từ gói ZIP chính thức.
- `tmp`, log và các file tải tạm cũng dùng thư mục này. Lần đầu cần Internet;
  khi đã đủ thư viện/model, suy luận và nhận giọng nói chạy cục bộ.

Nên để trống 8–10 GB khi cài, tính cả thư viện .NET, công cụ và file tạm. Không chạy lệnh
`ollama pull` qua một bản Ollama khác vì server đó có thể dùng nơi lưu model khác.
Luôn dùng script của dự án; nó đặt `OLLAMA_MODELS` cho tiến trình server trên ổ E.
Thư mục Storage không được theo dõi bằng Git, không được đưa vào thư mục web công khai.

`NuGet.Config` của dự án cũng đặt cache thư viện trong `Backend/Storage/AI/nuget`,
để lệnh restore/run thông thường không tải gói mới sang ổ C:

```powershell
dotnet restore
```

## Cấu hình

Thêm mục `AiInterview` vào **appsettings.json tại thư mục gốc** nếu cần đổi giá trị
mặc định; xem cấu trúc trong `appsettings.example.json`. Mặc định dùng
`http://127.0.0.1:11434/`, `qwen3:1.7b`, model giọng nói ở
`Backend/Storage/AI/speech/phowhisper-small-source`, Python tại
`Backend/Storage/AI/tools/python/python.exe`. Chờ tạo nội dung tối đa 180 giây;
nhận giọng nói tối đa 600 giây, chỉnh bằng `SpeechTimeoutSeconds` (60–900).
Khóa/model Gemini tiếp tục chỉ đọc qua `CauHinhGeminiCucBo` từ appsettings.json.
Không đặt khóa vào frontend, file mẫu hoặc Git.

Nếu dùng một máy chủ AI chung cho nhóm, Backend có thể đổi `BaseUrl` sang địa chỉ
nội bộ máy đó; chỉ cho Backend truy cập, cần bảo vệ mạng/dịch vụ khi triển khai.
Điện thoại và các máy truy cập website không tải các model này.

## Luồng và giới hạn

1. Backend lấy chuyên ngành (`Category.Name`), tiêu đề và mô tả tin tuyển dụng
   cùng bản CV **đã nộp**. Lưu ngữ cảnh cố định cho phiên, không nhận JD/CV từ JS.
2. CV tạo bằng trình dựng được đọc từ các trường có cấu trúc. PDF tải lên được
   trích văn bản tối đa 6 trang. PDF ảnh/chưa đọc được được thông báo rõ, phiên đó
   chỉ dựa trên công việc. Không có OCR và không giả vờ đã đọc CV.
3. AI hiện từng câu hỏi. Ứng viên ghi âm tối đa 2 phút, nghe lại, xem/sửa bản chữ
   trước khi gửi. Có thể nhập chữ nếu thiết bị không dùng được micro.
4. Backend chấm câu hiện tại bằng một lời gọi riêng, chỉ đưa câu hỏi, tiêu chí và
   câu trả lời hiện tại. Không đưa JD/CV hay mục tiêu câu kế tiếp vào lời gọi chấm.
   Model phải trả đúng số câu và chọn một trích dẫn nguyên văn do Backend cắt từ
   câu trả lời (tối đa 8 lựa chọn, mỗi lựa chọn tối đa 80 ký tự). Schema giới hạn
   bằng enum để model không diễn đạt lại trích dẫn rồi bị từ chối. Backend vẫn
   kiểm tra lại nguyên văn và điểm trước khi chấp nhận. Lời gọi thứ hai mới tạo
   câu tiếp theo theo JD/CV.
   Lưu câu trả lời/nhận xét/câu tiếp theo cùng nhau. Gửi trùng không ghi đè; lỗi AI
   không lưu một nửa lượt trả lời. Báo cáo cũ không tự được chấm lại.
5. Kết thúc sau 5 câu, hoặc kết thúc sớm. Điểm báo cáo là trung bình các lượt đã
   trả lời, do server tính. Báo cáo cá nhân, không chuyển cho nhà tuyển dụng.

Nhận giọng nói và gửi đáp án đều dùng tác vụ nền: POST trả `202` kèm mã tác vụ,
trình duyệt hỏi trạng thái mỗi 2 giây. Mỗi HTTP request ngắn, tránh phải giữ kết nối
Cloudflare trong suốt thời gian nhận dạng, chấm bài và tạo câu tiếp theo.
Quyền đọc kết quả gắn với đúng ứng viên và phiên;
kết quả tạm hết hạn sau 20 phút, không cache HTTP. Tải lại tab có thể tiếp tục đợi
nếu tác vụ còn trong bộ nhớ. Khởi động lại server làm mất các tác vụ đang xử lý.
Gửi lại cùng đáp án đang xử lý sẽ dùng lại tác vụ cũ, không chấm trùng; đáp án khác
cho cùng câu đang chấm bị từ chối để tránh ghi đè. Bản nháp chỉ xóa khi nhận kết quả
hoàn tất và đường dẫn tiếp tục hợp lệ. Khi mất mạng, tải lại hoặc bấm Gửi sẽ tiếp
tục kiểm tra tác vụ cũ. Lỗi model không lưu dở đáp án/điểm/câu tiếp theo.
Chỉ bước bắt đầu phiên và tạo câu đầu vẫn dùng request thông thường; tunnel có thể
hết thời gian chờ nếu bước đó quá lâu trên máy yếu. Log chấm bài chỉ ghi mã tác vụ,
thời gian, loại lỗi/mã HTTP, không ghi đáp án, CV/JD hay khóa API.

Âm thanh được xử lý trong bộ nhớ và truyền sang worker qua stdin, không lưu file ghi âm.
Worker chạy offline, ưu tiên chia bản ghi dài tại khoảng nghỉ, mỗi đoạn tối đa
30 giây. Nếu nói liên tục, giữ chồng lấn 150ms ở biên để hạn chế mất âm; không ghép
chữ bằng cách loại các câu lặp có thật. Giữ tiếng Việt và tác vụ chép lời, giải mã
beam 5. Không dùng LLM sửa hay đoán lại lời ứng viên.
Lịch sử chỉ lưu chữ và
nhận xét. Bản chữ nháp được giữ trong sessionStorage của tab để tải lại không mất;
xóa sau khi gửi thành công. Không để lộ đáp án gợi ý của câu chưa trả lời.

Model nhỏ có thể hiểu sai hoặc đánh giá chưa đúng. Cần đối chiếu câu hỏi/điểm với
giảng viên, nhất là các ngành ngoài CNTT. PhoWhisper vẫn có thể nhận sai tên công nghệ,
tiếng địa phương hoặc tiếng ồn; bản chữ luôn cho kiểm tra trước khi gửi. Hiện chỉ
hỏi bằng chữ, không có avatar chuyển động hay giọng đọc AI.

Tối ưu máy yếu: dùng CPU, mỗi server chỉ xử lý một tác vụ AI/nhận giọng nói một lúc,
giới hạn ngữ cảnh, giải phóng model Qwen sau lượt hỏi/chấm và tắt worker sau mỗi bản ghi.
Tốc độ thực tế phụ thuộc CPU/RAM; cần đo trên từng máy nhóm. Không cần card rời.
Phép thử CPU i5-4460 / 8 GB RAM: lượt chấm riêng câu mô tả dự án và hỏi tiếp khoảng
90 giây. Hai mẫu tiếng Việt công khai dài khoảng 9 giây mất khoảng 47–60 giây qua
worker Backend, cho kết quả tốt hơn bộ cũ,
nhưng vẫn sai một số từ và tên riêng. Đây là kiểm tra tích hợp với mẫu nhỏ, không
phải đánh giá độ chính xác tổng quát hay cam kết tốc độ trên mọi máy. Giọng thật
của người dùng và Safari iPhone cần kiểm tra lại sau khi khởi động lại website.
Bản thử dài 35,5 giây (ghép lặp mẫu công khai) đã nhận qua nhiều đoạn trong khoảng
168 giây, không bị gộp mất câu như cách ghép theo chữ trước đó. Ba kiểm tra riêng
đã xác nhận chia âm thanh không bỏ khoảng dữ liệu, ưu tiên khoảng nghỉ và không
tạo đoạn đuôi quá ngắn.

## Tổ chức mã

| Phần | Vị trí |
|---|---|
| Phiên luyện tập | `Backend/Areas/Candidate/Controllers/AiInterviewController.cs` |
| Gửi câu trả lời | `Backend/Areas/Candidate/Controllers/AiInterviewAnswerController.cs` |
| Nhận giọng nói | `Backend/Areas/Candidate/Controllers/AiInterviewSpeechController.cs` |
| Báo cáo | `Backend/Areas/Candidate/Controllers/AiInterviewReportController.cs` |
| Nghiệp vụ, prompt, đọc CV, gọi model, nhận giọng nói | `Backend/Services/AiInterview/` |
| Mô hình dữ liệu/cấu hình | `Backend/Models/`, `Backend/Configuration/` |
| Razor, CSS, ghi âm và tương tác hiển thị | `Frontend/Areas/Candidate/Views/AiInterview*/`, `Frontend/wwwroot/` |

## Kiểm thử

```powershell
dotnet run --project Tests/JobPortal.SmokeTests --configuration Fixes
dotnet run --project Tests/JobPortal.DatabaseTests --configuration Fixes
dotnet build --configuration Fixes
dotnet run --project Tests/JobPortal.BrowserTests --configuration Fixes -- --ai-only
```

Kiểm thử tích hợp dùng HTTP model giả có kiểm soát; kiểm thử trình duyệt chạy
MediaRecorder/AudioContext và tạo WAV thật với micro giả của Chrome, chỉ thay
kết quả ASR và model bằng dữ liệu kiểm thử. Database kiểm thử tách riêng và tự xóa.

Đợt sửa gửi đáp án qua Cloudflare đã đạt 519 kiểm tra tích hợp và 27 kiểm tra Chrome
cho phần AI. Migration SQL Server trước đó đạt 33 kiểm tra; đợt sửa này không đổi schema.
Đã thử suy luận thật với Ollama và PhoWhisper cục bộ.
Micro/giọng nói tiếng Việt trên Safari iPhone vẫn cần thử trên thiết bị thực tế.

Kiểm thử model cục bộ thật bằng `Tests/JobPortal.AiRuntimeTests` dùng ngữ cảnh tổng
hợp, không dùng CV cá nhân. Tải hai mẫu giọng tiếng Việt từ
[Google FLEURS](https://huggingface.co/datasets/google/fleurs) (CC-BY-4.0) vào Storage
được Git bỏ qua, rồi kiểm tra Backend → worker thật:

```powershell
& Backend/Storage/AI/tools/python/python.exe Tests/JobPortal.AiRuntimeTests/TaiMauTiengViet.py
& Backend/Storage/AI/tools/python/python.exe Tests/JobPortal.AiRuntimeTests/KiemTraChiaAmThanh.py
dotnet run --project Tests/JobPortal.AiRuntimeTests --configuration Fixes -- --speech-vietnamese
dotnet run --project Tests/JobPortal.AiRuntimeTests --configuration Fixes -- --speech-long
dotnet run --project Tests/JobPortal.AiRuntimeTests --configuration Fixes -- --grading-regression
dotnet run --project Tests/JobPortal.AiRuntimeTests --configuration Fixes -- --answer-regression
```

Kiểm tra ghi rõ bản chữ thực tế, thời gian và sai lệch theo đơn vị cách trắng. Hai
mẫu ngắn không đại diện cho giọng nói/micro của mọi người. Không có bản ghi 7 giây
của người dùng trong kho dự án nên chưa thể đối chiếu chính bản ghi đó.

Ca kiểm tra đáp án dài khoảng 202 ký tự với câu hỏi phân quyền tổng hợp đã chấm và
tạo câu tiếp theo thành công trên model thật trong khoảng 85 giây. Trước khi ràng
buộc enum, model diễn đạt lại trích dẫn nên bị Backend từ chối dù Ollama trả HTTP
200. Kiểm tra tích hợp cũng bao phủ đáp án 4.000 ký tự Unicode và giữ từ chối bằng
chứng bịa, điểm sai, gửi trùng hoặc lỗi giữa chừng.

Nguồn: [Ollama API](https://docs.ollama.com/api/chat),
[Qwen3 1.7B](https://ollama.com/library/qwen3:1.7b),
[PhoWhisper của VinAI](https://github.com/VinAIResearch/PhoWhisper),
[PhoWhisper-small](https://huggingface.co/vinai/PhoWhisper-small).
