using JobPortal.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using JobPortal.Infrastructure;
using JobPortal.Services;

namespace JobPortal.Areas.Admin.Controllers
{
    [Area("Admin")] // Khai báo thuộc tính vùng
    [Authorize(Roles = "Admin")]
    public class JobManagerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly JobService _jobs;

        public JobManagerController(ApplicationDbContext context, JobService jobs)
        {
            _context = context;
            _jobs = jobs;
        }

        // 1. Trang danh sách tin CHƯA DUYỆT
        [ActionName("Pending")]
        public async Task<IActionResult> ChoDuyet(int page = 1, CancellationToken token = default)
        {
            // Thêm Include để hiển thị sơ bộ tên công ty/địa điểm ngoài danh sách nếu cần
            var pendingJobs = await _context.JobPosts
                .AsNoTracking().Include(j => j.Company)
                .Include(j => j.JobLocation)
                .Include(j => j.Category)
                .Where(j => !j.IsApproved)
                .OrderByDescending(j => j.CreatedAt)
                .ThenByDescending(j => j.Id).PhanTrangAsync(this, page, token);

            return View(pendingJobs);
        }

        // 2. Xử lý Duyệt tin
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Approve")]
        public async Task<IActionResult> DuyetTin(int id)
        {
            var job = await _context.JobPosts.FindAsync(id);
            if (job != null)
            {
                job.IsApproved = true;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã duyệt bài đăng thành công!";
            }
            return RedirectToAction("Pending");
        }

        // 3. Lấy thông tin CHI TIẾT ĐẦY ĐỦ (Dùng cho Modal)
        [HttpGet]
        [ActionName("GetDetails")]
        public async Task<IActionResult> LayChiTiet(int id)
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
        [ActionName("Index")]
        public async Task<IActionResult> DanhSach(int? searchId, int page = 1, CancellationToken token = default)
        {
            var jobPosts = _context.JobPosts.AsNoTracking().Include(j => j.Company).Include(j => j.Category).Include(j => j.JobLocation).AsQueryable();

            if (searchId.HasValue)
            {
                jobPosts = jobPosts.Where(j => j.Id == searchId.Value);
            }

            return View(await jobPosts.OrderByDescending(j => j.CreatedAt).ThenByDescending(j => j.Id).PhanTrangAsync(this, page, token));
        }

        // 2. Chức năng Gỡ bài / Duyệt lại (Bật/Tắt trạng thái IsApproved)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("ToggleApproval")]
        public async Task<IActionResult> ChuyenTrangThaiDuyet(int id)
        {
            var job = await _context.JobPosts.FindAsync(id);
            if (job == null) return NotFound();

            // Đảo ngược trạng thái duyệt
            job.IsApproved = !job.IsApproved;
            await _context.SaveChangesAsync();

            TempData["Success"] = job.IsApproved ? "Đã hiển thị lại bài đăng." : "Đã gỡ bài đăng khỏi trang chủ.";
            return RedirectToAction("Index");
        }

        // 3. Xóa bài đăng vĩnh viễn
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("DeletePost")]
        public async Task<IActionResult> XoaTin(int id)
        {
            var job = await _context.JobPosts.FindAsync(id);
            if (job != null)
            {
                var removed = await _jobs.XoaHoacAnTinAsync(job, HttpContext.RequestAborted);
                TempData["Success"] = removed ? "Đã xóa tin." : "Tin đã có hồ sơ hoặc được lưu: đã gỡ hiển thị và giữ lịch sử.";
            }
            return RedirectToAction("Index");
        }
    }
}
