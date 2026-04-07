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
            var userEmail = User.Identity.Name;
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var candidate = await _context.Candidates.FirstOrDefaultAsync(c => c.Email == userEmail);

            var model = new CandidateProfileViewModel();
            if (candidate != null)
            {
                model.FullName = candidate.FullName;
                model.Email = candidate.Email;
                model.Phone = candidate.Phone;
                model.Address = candidate.Address;
                model.AvatarUrl = candidate.AvatarUrl;
                model.CVUrl = candidate.CVUrl;
                model.Gender = candidate.Gender;
                model.Birthday = candidate.Birthday;
            }

            // Lấy danh sách việc làm đã ứng tuyển
            model.AppliedJobs = await _context.Applications
                .Include(a => a.JobPost)
                .Where(a => a.CandidateId == userId)
                .OrderByDescending(a => a.AppliedDate)
                .ToListAsync();

            return View(model);
        }

        // Xử lý lưu hồ sơ và Upload File
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveProfile(CandidateProfileViewModel model)
        {
            var userEmail = User.Identity.Name;
            var candidate = await _context.Candidates.FirstOrDefaultAsync(c => c.Email == userEmail);

            if (candidate == null)
            {
                candidate = new JobPortal.Models.Candidate { Email = userEmail };
                _context.Candidates.Add(candidate);
            }

            candidate.FullName = model.FullName;
            candidate.Phone = model.Phone;
            candidate.Address = model.Address;
            candidate.Gender = model.Gender;
            candidate.Birthday = model.Birthday;

            // Xử lý Ảnh đại diện
            if (model.AvatarFile != null)
            {
                string folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/avatars");
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                string fileName = Guid.NewGuid() + Path.GetExtension(model.AvatarFile.FileName);
                using (var stream = new FileStream(Path.Combine(folder, fileName), FileMode.Create))
                {
                    await model.AvatarFile.CopyToAsync(stream);
                }
                candidate.AvatarUrl = "/uploads/avatars/" + fileName;
            }

            // Xử lý File CV PDF
            if (model.CVFile != null)
            {
                string folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/cvs");
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                string fileName = Guid.NewGuid() + Path.GetExtension(model.CVFile.FileName);
                using (var stream = new FileStream(Path.Combine(folder, fileName), FileMode.Create))
                {
                    await model.CVFile.CopyToAsync(stream);
                }
                candidate.CVUrl = "/uploads/cvs/" + fileName;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Cập nhật hồ sơ thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}