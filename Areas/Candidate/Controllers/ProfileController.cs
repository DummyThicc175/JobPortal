using Microsoft.AspNetCore.Mvc;
using JobPortal.Data;
using JobPortal.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace JobPortal.Areas.Candidate.Controllers
{
    [Area("Candidate")]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProfileController(ApplicationDbContext context) => _context = context;

        public async Task<IActionResult> Index()
        {
            // Lấy Email người dùng đang đăng nhập
            var email = User.Identity?.Name;
            var candidate = await _context.Candidates.FirstOrDefaultAsync(c => c.Email == email);

            var model = new CandidateProfileViewModel();
            if (candidate != null)
            {
                model.FullName = candidate.FullName;
                model.Email = candidate.Email;
                model.Phone = candidate.Phone;
                model.AvatarUrl = candidate.AvatarUrl;
                model.Address = candidate.Address;
                // ... nạp các trường khác tương tự
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SaveProfile(CandidateProfileViewModel model)
        {
            var email = User.Identity?.Name;
            var candidate = await _context.Candidates.FirstOrDefaultAsync(c => c.Email == email);

            if (candidate == null) return NotFound();

            candidate.FullName = model.FullName;
            candidate.Phone = model.Phone;
            candidate.Address = model.Address;

            // Xử lý Upload ảnh đại diện
            if (model.AvatarFile != null)
            {
                var fileName = Guid.NewGuid() + Path.GetExtension(model.AvatarFile.FileName);
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/avatars", fileName);

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await model.AvatarFile.CopyToAsync(stream);
                }
                candidate.AvatarUrl = "/uploads/avatars/" + fileName;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }
    }
}
