using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JobPortal.Data;
using JobPortal.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace JobPortal.Areas.Employer.Controllers
{
    [Area("Employer")]
    // [Authorize(Roles = "Employer")] // Mở dòng này khi bạn đã phân quyền xong
    public class JobController : Controller
    {
        private readonly ApplicationDbContext _context;

        public JobController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. Trang danh sách tin đã đăng + Thống kê nhanh cho Dashboard
        public async Task<IActionResult> Index()
        {
            // Lấy ID của người dùng đang đăng nhập (Employer)
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Truy vấn danh sách tin (Sắp xếp tin mới nhất lên đầu)
            var jobs = await _context.JobPosts
                .OrderByDescending(j => j.CreatedAt)
                .ToListAsync();

            // Nếu bạn muốn lọc chỉ xem tin của riêng User đó, dùng dòng này:
            // var jobs = await _context.JobPosts.Where(j => j.EmployerId == userId).OrderByDescending(j => j.CreatedAt).ToListAsync();

            return View(jobs);
        }

        // 2. Trang Đăng tin mới (GET)
        public async Task<IActionResult> Create()
        {
            ViewBag.Locations = await _context.Locations.OrderBy(l => l.Name).ToListAsync();
            return View();
        }

        // 3. Xử lý Đăng tin (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(JobPost job)
        {
            if (ModelState.IsValid)
            {
                // Gán ID người dùng đang đăng nhập
                job.EmployerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                job.CreatedAt = DateTime.Now;
                job.IsApproved = false; // Chờ Admin duyệt

                _context.Add(job);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Đăng tin tuyển dụng thành công! Vui lòng chờ Admin duyệt.";
                return RedirectToAction(nameof(Index));
            }

            // Nếu lỗi, phải load lại danh sách Dropdown trước khi trả về View
            ViewBag.Locations = await _context.Locations.OrderBy(l => l.Name).ToListAsync();
            return View(job);
        }

        // 4. Xem danh sách ứng viên (Dành cho chức năng "Xem ứng viên" trong Index)
        public async Task<IActionResult> Candidates(int jobId)
        {
            if (jobId <= 0)
            {
                return BadRequest();
            }

            var job = await _context.JobPosts.FirstOrDefaultAsync(j => j.Id == jobId);
            if (job == null)
            {
                return NotFound();
            }

            var applications = await _context.Applications
                .Include(a => a.CV) // Lấy thông tin CV đi kèm
                .Where(a => a.JobPostId == jobId)
                .OrderByDescending(a => a.AppliedDate)
                .ToListAsync();

            var candidateIds = applications
                .Select(a => a.CandidateId)
                .Distinct()
                .ToList();

            var users = await _context.Users
                .Where(u => candidateIds.Contains(u.Id))
                .ToListAsync();

            var applicantViewModels = new List<EmployerApplicantViewModel>();

            foreach (var application in applications)
            {
                var user = users.FirstOrDefault(u => u.Id == application.CandidateId);
                applicantViewModels.Add(new EmployerApplicantViewModel
                {
                    Id = application.Id,
                    JobPostId = application.JobPostId,
                    JobTitle = job.Title,
                    CandidateId = application.CandidateId,
                    CandidateName = user?.UserName ?? application.CandidateId,
                    CandidateEmail = user?.Email ?? "Không có email",
                    CvFileName = application.CV?.FileName,
                    CvUrl = application.CV != null ? $"/uploads/cvs/{application.CV.FileName}" : null,
                    AppliedDate = application.AppliedDate,
                    Status = application.Status.ToString()
                });
            }

            return View(applicantViewModels);
        }

        // 5. Thêm chức năng Xóa tin (Cần thiết cho quản lý)
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var job = await _context.JobPosts.FindAsync(id);
            if (job != null)
            {
                _context.JobPosts.Remove(job);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}