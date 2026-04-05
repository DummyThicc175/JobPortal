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

namespace JobPortal.Areas.Candidate.Controllers
{
    [Area("Candidate")]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. Trang chủ của ứng viên (Danh sách công việc)
        public async Task<IActionResult> Index()
        {
            var jobs = await _context.JobPosts.ToListAsync();
            return View(jobs);
        }

        // 2. Trang Quản lý CV - Hiển thị danh sách CV đã tải lên
        public async Task<IActionResult> ManageCV()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var myCVs = await _context.CVs
                .Where(c => c.CandidateId == userId)
                .OrderByDescending(c => c.UploadDate)
                .ToListAsync();

            // Load CV templates để view hiển thị ảnh thumbnail
            var templates = await _context.CVTemplates
                .OrderBy(t => t.Name)
                .ToListAsync();

            ViewBag.Templates = templates;

            return View(myCVs);
        }

        // 3. Action xử lý tải CV lên
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadCV(IFormFile file, string title)
        {
            if (file != null && file.Length > 0)
            {
                // Kiểm tra định dạng file (chỉ nhận PDF)
                var extension = Path.GetExtension(file.FileName).ToLower();
                if (extension != ".pdf")
                {
                    TempData["Error"] = "Chỉ chấp nhận định dạng file .pdf";
                    return RedirectToAction("ManageCV");
                }

                // Tạo tên file duy nhất để không bị trùng
                var fileName = Guid.NewGuid().ToString() + extension;
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/cvs", fileName);

                // Lưu file vào thư mục wwwroot/uploads/cvs
                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Lưu thông tin vào Database
                var newCV = new CV
                {
                    FileName = fileName,
                    Title = title ?? file.FileName,
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
    }
}