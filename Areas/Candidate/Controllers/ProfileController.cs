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

        // Hiển thị trang hồ sơ
        public async Task<IActionResult> Index()
        {
            var model = new CandidateProfileViewModel
            {
                // Lấy dữ liệu gợi ý từ Database
                SuggestedJobs = await _context.JobPosts.Include(j => j.Company).Take(5).ToListAsync()
            };
            return View(model);
        }

        // Xử lý khi nhấn nút LƯU THÔNG TIN HỒ SƠ
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveProfile(CandidateProfileViewModel model)
        {
            if (ModelState.IsValid)
            {
                // 1. Xử lý lưu ảnh đại diện (nếu có chọn file mới)
                if (model.AvatarFile != null)
                {
                    string folder = "images/avatars/";
                    string fileName = Guid.NewGuid().ToString() + "_" + model.AvatarFile.FileName;
                    string serverPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", folder, fileName);

                    using (var stream = new FileStream(serverPath, FileMode.Create))
                    {
                        await model.AvatarFile.CopyToAsync(stream);
                    }
                    model.AvatarUrl = "/" + folder + fileName;
                }

                // 2. Logic lưu các thông tin khác vào Database ở đây...
                // _context.Update(candidateRecord);
                // await _context.SaveChangesAsync();

                TempData["Success"] = "Cập nhật hồ sơ thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View("Index", model);
        }
    }
}