using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JobPortal.Data;
using JobPortal.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

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