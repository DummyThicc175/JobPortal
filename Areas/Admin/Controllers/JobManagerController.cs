using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JobPortal.Data;
using System.Linq;
using System.Threading.Tasks;

namespace JobPortal.Areas.Admin.Controllers
{
    [Area("Admin")]
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
                category = job.Category?.Name ?? "Chưa phân loại",
                location = job.JobLocation?.Name ?? "Toàn quốc",
                salary = job.SalaryRange ?? "Thỏa thuận",
                createdAt = job.CreatedAt.ToString("dd/MM/yyyy"),
                description = job.Description
            });
        }
    }
}