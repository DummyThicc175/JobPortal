using Microsoft.AspNetCore.Mvc;
using JobPortal.Data;
using JobPortal.Models;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace JobPortal.Areas.Candidate.Controllers
{
    [Area("Candidate")]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProfileController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. TRANG HIỂN THỊ HỒ SƠ
        public async Task<IActionResult> Index()
        {
            // Lấy ứng viên đầu tiên trong bảng Candidates
            var candidate = await _context.Candidates.FirstOrDefaultAsync();

            var model = new CandidateProfileViewModel();

            if (candidate != null)
            {
                // Đổ dữ liệu từ DB vào ViewModel để hiện lên Form
                model.FullName = candidate.FullName;
                model.Phone = candidate.Phone;
                model.Gender = candidate.Gender;
                model.Birthday = candidate.Birthday;
                model.MaritalStatus = candidate.MaritalStatus;
                model.Industry = candidate.Industry;
                model.DesiredLocation = candidate.DesiredLocation;
                model.ExpectedSalary = candidate.ExpectedSalary;
                model.AvatarUrl = candidate.AvatarUrl;
                model.Email = candidate.Email;
                model.Address = candidate.Address;
                model.CVUrl = candidate.CVUrl;
              
            }

            // Load dữ liệu cho các ô Dropdown (Ngành nghề, Tỉnh thành)
            ViewBag.Locations = await _context.Locations.OrderBy(l => l.Name).ToListAsync();
            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();

            return View(model);
        }

        // 2. XỬ LÝ LƯU THÔNG TIN
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveProfile(CandidateProfileViewModel model)
        {
            // Bỏ qua kiểm tra validate nghiêm ngặt để ưu tiên lưu dữ liệu trước
            var candidate = await _context.Candidates.FirstOrDefaultAsync();

            // Nếu chưa có ứng viên nào (Database trống), tạo mới hoàn toàn
            if (candidate == null)
            {
                candidate = new JobPortal.Models.Candidate();
                _context.Candidates.Add(candidate);
            }

            // Cập nhật thông tin cơ bản
            candidate.FullName = model.FullName;
            candidate.Phone = model.Phone;
            candidate.Gender = model.Gender;
            candidate.Birthday = model.Birthday;
            candidate.MaritalStatus = model.MaritalStatus;
            candidate.Industry = model.Industry;
            candidate.DesiredLocation = model.DesiredLocation;
            candidate.ExpectedSalary = model.ExpectedSalary;
            candidate.Email = model.Email;
            candidate.Address = model.Address;
           

            // --- XỬ LÝ LƯU AVATAR ---
            if (model.AvatarFile != null && model.AvatarFile.Length > 0)
            {
                string uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "avatars");
                if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);

                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.AvatarFile.FileName);
                string filePath = Path.Combine(uploadDir, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.AvatarFile.CopyToAsync(stream);
                }
                candidate.AvatarUrl = "/uploads/avatars/" + fileName;
            }

            // --- XỬ LÝ LƯU CV ---
            if (model.CVFile != null && model.CVFile.Length > 0)
            {
                string cvDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "cvs");
                if (!Directory.Exists(cvDir)) Directory.CreateDirectory(cvDir);

                string cvFileName = Guid.NewGuid().ToString() + Path.GetExtension(model.CVFile.FileName);
                string cvFilePath = Path.Combine(cvDir, cvFileName);

                using (var stream = new FileStream(cvFilePath, FileMode.Create))
                {
                    await model.CVFile.CopyToAsync(stream);
                }
                candidate.CVUrl = "/uploads/cvs/" + cvFileName;
            }

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Hồ sơ của " + candidate.FullName + " đã được cập nhật!";
            }
            catch (Exception ex)
            {
                // Thông báo lỗi chi tiết ra màn hình nếu lưu thất bại
                TempData["ErrorMessage"] = "Lỗi lưu Database: " + ex.InnerException?.Message ?? ex.Message;
                ViewBag.Locations = await _context.Locations.OrderBy(l => l.Name).ToListAsync();
                ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
                return View("Index", model);
            }

            return RedirectToAction(nameof(Index));
        }
    }
}