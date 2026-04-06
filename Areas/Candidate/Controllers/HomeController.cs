using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using JobPortal.Data;
using JobPortal.Models;
using System.Security.Claims;

// ... các using giữ nguyên ...

namespace JobPortal.Areas.Candidate.Controllers
{
    [Area("Candidate")]
    [Authorize] // Đảm bảo chỉ người dùng đã đăng nhập mới vào được
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. Trang chủ ứng viên
        public async Task<IActionResult> Index()
        {
            var jobs = await _context.JobPosts.ToListAsync();
            return View(jobs);
        }

        // 2. Trang Quản lý CV (Trang trí CV)
        public async Task<IActionResult> ManageCV()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Lấy danh sách CV cá nhân
            var myCVs = await _context.CVs
                .Where(c => c.CandidateId == userId)
                .OrderByDescending(c => c.UploadDate)
                .ToListAsync();

            // Lấy danh sách các mẫu thiết kế (Templates) để hiển thị giống 24h
            var templates = await _context.CVTemplates
                .OrderBy(t => t.Name)
                .ToListAsync();

            ViewBag.Templates = templates;

            return View(myCVs);
        }

        // 3. Xử lý tải CV lên (Sửa tham số 'file' thành 'fileCV' cho khớp với View)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadCV(IFormFile fileCV, string title)
        {
            if (fileCV != null && fileCV.Length > 0)
            {
                var extension = Path.GetExtension(fileCV.FileName).ToLower();
                if (extension != ".pdf")
                {
                    TempData["Error"] = "Chỉ chấp nhận định dạng file .pdf";
                    return RedirectToAction("ManageCV");
                }

                // Tạo thư mục nếu chưa tồn tại
                var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/cvs");
                if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

                var fileName = Guid.NewGuid().ToString() + extension;
                var path = Path.Combine(uploadPath, fileName);

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await fileCV.CopyToAsync(stream);
                }

                var newCV = new CV
                {
                    FileName = fileName,
                    Title = title ?? fileCV.FileName,
                    CandidateId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                    UploadDate = DateTime.Now
                };

                _context.CVs.Add(newCV);
                await _context.SaveChangesAsync();

                TempData["Message"] = "Tải CV lên thành công!";
            }
            else
            {
                TempData["Error"] = "Vui lòng chọn một file để tải lên.";
            }

            return RedirectToAction("ManageCV");
        }

        // 4. Action tạo CV từ mẫu (Bạn cần tạo thêm hàm này để nút "Dùng mẫu này" hoạt động)


        // Đường dẫn: Candidate/Home/EditCV/5
        public async Task<IActionResult> EditCV(int id)
        {
            var model = await _context.UserCVs
                .Include(x => x.Experiences)
                .Include(x => x.Educations)
                .Include(x => x.Languages)
                .Include(x => x.ITSKills)
                .Include(x => x.References)
                .FirstOrDefaultAsync(x => x.TemplateId == id);

            if (model == null)
            {
                var template = await _context.CVTemplates.FirstOrDefaultAsync(t => t.Id == id);
                if (template == null) return NotFound();

                // PHẢI XỬ LÝ LẤY PATH TẠI ĐÂY
                string path = template.TemplateViewPath;
                if (path.Contains("Templates/"))
                {
                    path = path.Substring(path.IndexOf("Templates/"));
                    path = path.Replace(".cshtml", "");
                }

                model = new EditCVViewModel
                {
                    TemplateId = id,
                    TemplatePath = path, // QUAN TRỌNG: Phải gán cái này để không bị lỗi Null
                    FullName = "HỌ VÀ TÊN CỦA BẠN",
                    Experiences = new List<ExperienceVM>(),
                    Educations = new List<EducationVM>(),
                    Languages = new List<LanguageVM>(),
                    ITSKills = new List<ITSkillVM>(),
                    References = new List<ReferenceVM>()
                };
            }

            // Kiểm tra thêm một lần nữa cho chắc chắn trước khi return
            if (string.IsNullOrEmpty(model.TemplatePath))
            {
                // Nếu vẫn null thì gán mặc định file T1
                model.TemplatePath = "Templates/T1";
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SaveCV([FromBody] EditCVViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Nếu là CV mới thì Add, nếu có Id rồi thì Update
                if (model.Id == 0)
                {
                    _context.UserCVs.Add(model);
                }
                else
                {
                    _context.UserCVs.Update(model);
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Lưu CV thành công!" });
            }
            return Json(new { success = false, message = "Lỗi dữ liệu!" });
        }
    }
}