using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JobPortal.Data;
using JobPortal.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace JobPortal.Areas.Candidate.Controllers
{
    [Area("Candidate")]
    [Authorize] // Bắt buộc đăng nhập
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        public ProfileController(ApplicationDbContext context) => _context = context;

        // Trang chính: Hiển thị thông tin + Danh sách đã ứng tuyển
        public async Task<IActionResult> Index()
        {
            // 1. Lấy thông tin người dùng hiện tại
            var userEmail = User.Identity?.Name;
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 2. Truy vấn dữ liệu ứng viên từ Database
            var candidate = await _context.Candidates
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Email == userEmail);

            // 3. Đổ dữ liệu vào ViewModel
            var model = new CandidateProfileViewModel();
            if (candidate != null)
            {
                model.FullName = candidate.FullName;
                model.Email = candidate.Email;
                model.Phone = candidate.Phone;
                model.Address = candidate.Address;
                model.AvatarUrl = candidate.AvatarUrl;
                model.Gender = candidate.Gender;
                model.Birthday = candidate.Birthday;

                // --- SỬA LỖI 1: BỔ SUNG ĐƯỜNG DẪN CV SANG VIEWINDEX ---
                model.CVUrl = candidate.CVUrl;
            }

            // 4. Lấy danh sách việc làm đã ứng tuyển
            model.AppliedJobs = await _context.Applications
                .Include(a => a.JobPost)
                .Where(a => a.CandidateId == userId)
                .OrderByDescending(a => a.AppliedDate)
                .ToListAsync();

            return View(model);
        }

        public async Task<IActionResult> SavedJobs()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var savedJobs = await _context.SavedJobs
                .Include(s => s.JobPost)
                    .ThenInclude(j => j.JobLocation)
                .Where(s => s.CandidateId == userId)
                .OrderByDescending(s => s.SavedDate)
                .ToListAsync();

            return View(savedJobs);
        }

        // Action hiển thị danh sách việc làm đã ứng tuyển
        public async Task<IActionResult> AppliedJobs()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var applications = await _context.Applications
                .Include(a => a.JobPost)
                    .ThenInclude(j => j.JobLocation)
                .Where(a => a.CandidateId == userId)
                .OrderByDescending(a => a.AppliedDate)
                .ToListAsync();

            return View(applications);
        }

        public async Task<IActionResult> Edit()
        {
            var userEmail = User.Identity?.Name;
            var candidate = await _context.Candidates.FirstOrDefaultAsync(c => c.Email == userEmail);

            var model = new CandidateProfileViewModel();
            if (candidate != null)
            {
                // Đổ dữ liệu vào model để hiện lên form edit
                model.FullName = candidate.FullName;
                model.Phone = candidate.Phone;
                model.Address = candidate.Address;
                model.AvatarUrl = candidate.AvatarUrl;
                model.Gender = candidate.Gender;
                model.Birthday = candidate.Birthday;

                // --- SỬA LỖI 2: BỔ SUNG ĐỂ TRANG EDIT BIẾT FILE CŨ ĐÃ CÓ HAY CHƯA ---
                model.CVUrl = candidate.CVUrl;
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> AnalyzeCVByAi([FromBody] EditCVViewModel model)
        {
            try
            {
                // 1. Cấu hình chuỗi kết nối API Gemini (Gói miễn phí gemini-1.5-flash)
                // Mẹo: Bạn có thể thay chuỗi này bằng API Key thực tế lấy từ Google AI Studio
                string apiKey = "AIzaSyDCN_46XBPr_RR3lzNPXq-0mV_ian77BBI";
                string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent?key={apiKey}";

                // 2. Gom toàn bộ text từ CV ứng viên nhập vào thành 1 chuỗi dữ liệu
                string expText = model.Experiences != null ? string.Join(" | ", model.Experiences.Select(e => $"{e.Company} - {e.Role}: {e.Description}")) : "Chưa có";
                string eduText = model.Educations != null ? string.Join(" | ", model.Educations.Select(e => $"{e.School} - {e.Major}")) : "Chưa có";
                string skillsText = model.ITSKills != null ? string.Join(", ", model.ITSKills.Select(s => s.SkillName)) : "Chưa có";

                string cvDataText = $"[Họ tên]: {model.FullName}\n[Vị trí]: {model.JobTitle}\n[Giới thiệu]: {model.Summary}\n[Kinh nghiệm]: {expText}\n[Học vấn]: {eduText}\n[Kỹ năng]: {skillsText}";

                // 3. Xây dựng Prompt (Câu lệnh) ép AI đóng vai chuyên gia và chỉ trả về JSON
                string systemPrompt = "Bạn là một giám đốc nhân sự cấp cao chuyên nghiệp. Hãy đọc thông tin CV bằng tiếng Việt sau đây, chấm điểm dựa trên độ chuyên nghiệp của câu từ và đưa ra các gợi ý sửa đổi văn phong ngắn gọn giúp ứng viên dễ trúng tuyển.\n" +
                                      "Yêu Cầu Cực Kỳ Quan Trọng: Bạn BẮT BUỘC chỉ được trả về một chuỗi dạng JSON có đúng cấu trúc như sau, không được kèm theo bất kỳ lời chào, giải thích hay ký hiệu đánh dấu Markdown nào khác:\n" +
                                      "{\n  \"Score\": 85,\n  \"Suggestions\": [\"Gợi ý 1\", \"Gợi ý 2\", \"Gợi ý 3\"]\n}\n\nDữ liệu CV cần phân tích:\n";

                // 4. Đóng gói Body theo chuẩn quy định cấu trúc API của Google Gemini
                var requestBody = new
                {
                    contents = new[]
                    {
                new { parts = new[] { new { text = systemPrompt + cvDataText } } }
            }
                };

                using var client = new HttpClient();
                var jsonPayload = JsonSerializer.Serialize(requestBody);
                var httpContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                // 5. Gửi request và đọc kết quả trả về
                var response = await client.PostAsync(url, httpContent);
                if (!response.IsSuccessStatusCode)
                    if (!response.IsSuccessStatusCode)
                    {
                        return Json(new { success = false, message = "Không thể kết nối tới máy chủ AI hoặc vượt quá định mức phút." });
                    }

                var jsonResponse = await response.Content.ReadAsStringAsync();

                // 6. Bóc tách dữ liệu chuỗi JSON từ Gemini
                using JsonDocument doc = JsonDocument.Parse(jsonResponse);
                string? aiRawText = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                if (string.IsNullOrEmpty(aiRawText))
                {
                    return Json(new { success = false, message = "AI trả về kết quả rỗng." });
                }

                // Làm sạch chuỗi đề phòng AI tự ý chèn thẻ ```json ... ```
                aiRawText = aiRawText.Replace("```json", "").Replace("```", "").Trim();

                // Ép chuỗi văn bản sạch về đối tượng Object C#
                var aiResult = JsonSerializer.Deserialize<AiReviewResult>(aiRawText);

                return Json(new { success = true, score = aiResult?.Score ?? 0, suggestions = aiResult?.Suggestions });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi hệ thống: {ex.Message}" });
            }
        }
        // 1. GET: Candidate/Profile/SelectTemplate
        public IActionResult SelectTemplate()
{
    // Tạo sẵn danh sách 2 mẫu CV nền đại diện để hiển thị ra View
    var templates = new List<CVTemplate>
    {
        new CVTemplate { Id = 1, Name = "Mẫu Thiết Kế Truyền Thống (Blue)", Thumbnail = "/images/templates/t1.jpg", TemplateViewPath = "Template1" }, 
       new CVTemplate { Id = 2, Name = "Mẫu Công Nghệ Hiện Đại (Dark)", Thumbnail = "/images/templates/t2.jpg", TemplateViewPath = "Template2" }
    };

    return View(templates);
}

// 2. GET: Candidate/Profile/CreateCV?templateId=1
public IActionResult CreateCV(int templateId)
{
    // Nếu ứng viên cố tình vào thẳng trang này mà chưa chọn mẫu, bắt quay về trang chọn mẫu trước
    if (templateId <= 0)
    {
        return RedirectToAction(nameof(SelectTemplate));
    }

    // Nạp dữ liệu mặc định ban đầu cho Form viết CV dựa theo tài khoản đang đăng nhập
    var model = new EditCVViewModel
    {
        TemplateId = templateId, // Giữ lại ID mẫu đã chọn để View load đúng Layout
        FullName = "Nguyễn Văn B",
        Email = User.Identity?.Name,
        Phone = "0123456789",
        JobTitle = "Lập trình viên Fullstack"
    };

    return View(model);
}
        // 1. GET: Candidate/Profile/SelectTemplate
        // Xử lý lưu hồ sơ và Upload File
        [HttpPost]
        [ValidateAntiForgeryToken] // Xóa bỏ dòng [HttpPost] bị lặp ở đây
        public async Task<IActionResult> SaveProfile(CandidateProfileViewModel model)
        {
            // Kiểm tra tính hợp lệ của dữ liệu đầu vào cơ bản
            if (!ModelState.IsValid)
            {
                return View("Edit", model);
            }

            try
            {
                var userEmail = User.Identity?.Name;
                var candidate = await _context.Candidates.FirstOrDefaultAsync(c => c.Email == userEmail);

                if (candidate == null)
                {
                    candidate = new JobPortal.Models.Candidate { Email = userEmail };
                    _context.Candidates.Add(candidate);
                }

                // Gán dữ liệu text
                candidate.FullName = model.FullName;
                candidate.Phone = model.Phone;
                candidate.Address = model.Address;
                candidate.Gender = model.Gender;
                candidate.Birthday = model.Birthday;

                // Xử lý Ảnh đại diện
                if (model.AvatarFile != null && model.AvatarFile.Length > 0)
                {
                    string uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "avatars");
                    if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.AvatarFile.FileName);
                    string fullPath = Path.Combine(uploadPath, fileName);

                    using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        await model.AvatarFile.CopyToAsync(stream);
                    }
                    candidate.AvatarUrl = "/uploads/avatars/" + fileName;
                }

                // Xử lý CV PDF
                if (model.CVFile != null && model.CVFile.Length > 0)
                {
                    string cvPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "cvs");
                    if (!Directory.Exists(cvPath)) Directory.CreateDirectory(cvPath);

                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.CVFile.FileName);
                    string fullPath = Path.Combine(cvPath, fileName);

                    using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        await model.CVFile.CopyToAsync(stream);
                    }
                    candidate.CVUrl = "/uploads/cvs/" + fileName;
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = "Cập nhật thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                return Content($"Lỗi khi lưu file: {ex.Message}. Hãy đảm bảo thư mục wwwroot/uploads có quyền ghi.");
            }
        }
    }
}