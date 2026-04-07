using JobPortal.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace JobPortal.Areas.Admin.Controllers
{
    [Area("Admin")] // Khai báo thuộc tính vùng
    [Authorize(Roles = "Admin")]
    public class JobManagerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public JobManagerController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. Trang danh sách tin CHƯA DUYỆT
        public async Task<IActionResult> Pending()
        {
            // Thêm Include để hiển thị sơ bộ tên công ty/địa điểm ngoài danh sách nếu cần
            var pendingJobs = await _context.JobPosts
                .Include(j => j.JobLocation)
                .Include(j => j.Category)
                .Where(j => !j.IsApproved)
                .OrderByDescending(j => j.CreatedAt)
                .ToListAsync();

            return View(pendingJobs);
        }

        // 2. Xử lý Duyệt tin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var job = await _context.JobPosts.FindAsync(id);
            if (job != null)
            {
                job.IsApproved = true;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã duyệt bài đăng thành công!";
            }
            return RedirectToAction(nameof(Pending));
        }

        // 3. Lấy thông tin CHI TIẾT ĐẦY ĐỦ (Dùng cho Modal)
        [HttpGet]
        public async Task<IActionResult> GetDetails(int id)
        {
            // Phải có .Include để tránh lỗi Null Reference
            var job = await _context.JobPosts
                .Include(j => j.Category)
                .Include(j => j.JobLocation)
                .Include(j => j.Company)
                .FirstOrDefaultAsync(j => j.Id == id);

            if (job == null) return NotFound();

            return Json(new
            {
                title = job.Title,
                company = job.CompanyName ?? job.Company?.Name ?? "Chưa rõ",
                category = job.CategoryName ?? job.Category?.Name ?? "Chưa phân loại",
                location = job.JobLocation?.Name ?? "Toàn quốc",
                salary = job.SalaryRange ?? "Thỏa thuận",
                createdAt = job.CreatedAt.ToString("dd/MM/yyyy"),
                description = job.Description
            });
        }
        // 1. Xem tất cả bài đăng trên hệ thống
        public async Task<IActionResult> Index(int? searchId)
        {
            var jobPosts = _context.JobPosts.AsQueryable();

            if (searchId.HasValue)
            {
                jobPosts = jobPosts.Where(j => j.Id == searchId.Value);
            }

            return View(await jobPosts.ToListAsync());
        }

        // 2. Chức năng Gỡ bài / Duyệt lại (Bật/Tắt trạng thái IsApproved)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleApproval(int id)
        {
            var job = await _context.JobPosts.FindAsync(id);
            if (job == null) return NotFound();

            // Đảo ngược trạng thái duyệt
            job.IsApproved = !job.IsApproved;
            await _context.SaveChangesAsync();

            TempData["Success"] = job.IsApproved ? "Đã hiển thị lại bài đăng." : "Đã gỡ bài đăng khỏi trang chủ.";
            return RedirectToAction(nameof(Index));
        }

        // 3. Xóa bài đăng vĩnh viễn
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePost(int id)
        {
            var job = await _context.JobPosts.FindAsync(id);
            if (job != null)
            {
                _context.JobPosts.Remove(job);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã xóa bài đăng vĩnh viễn.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}