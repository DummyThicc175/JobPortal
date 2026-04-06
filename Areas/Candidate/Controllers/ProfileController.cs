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
            // 1. Lấy danh sách Tỉnh thành và Ngành nghề để hiển thị trong Dropdown (ô chọn)
            ViewBag.Locations = await _context.Locations.OrderBy(l => l.Name).ToListAsync();
            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();

            var model = new CandidateProfileViewModel
            {
                // 2. Lấy dữ liệu gợi ý và phải có .Include để lấy Tên Địa điểm/Ngành nghề
                SuggestedJobs = await _context.JobPosts
                    .Include(j => j.Company)
                    .Include(j => j.JobLocation) // Quan trọng: Để lấy Tên Tỉnh thành
                    .Include(j => j.Category)    // Quan trọng: Để lấy Tên Ngành nghề
                    .Where(j => j.IsApproved)    // Chỉ lấy tin đã được duyệt
                    .Take(5)
                    .ToListAsync()
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