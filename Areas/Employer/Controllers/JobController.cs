using JobPortal.Data;
using JobPortal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace JobPortal.Areas.Employer.Controllers
{
    [Area("Employer")]
    [Authorize(Roles = "Employer")]
   
    public class JobController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        public JobController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager; // Gán giá trị được truyền vào cho biến _userManager
        }

        // 1. Trang danh sách tin đã đăng của riêng Employer đó
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
// Chỉ lấy danh sách tin do chính User này đăng 
            var jobs = await _context.JobPosts
                .Include(j => j.Category)
                .Include(j => j.JobLocation)
                .Where(j => j.EmployerId == userId)
                .OrderByDescending(j => j.CreatedAt)
                .ToListAsync();

            return View(jobs);
        }
        public async Task<IActionResult> Candidates1()
        {
            // Lấy ID của nhà tuyển dụng hiện tại
            var userId = _userManager.GetUserId(User);

            // 1. Lấy danh sách các bài đăng thuộc về nhà tuyển dụng này
            var myJobs = await _context.JobPosts
                .Where(j => j.EmployerId == userId)
                .Select(j => j.Id)
                .ToListAsync();

            // 2. Lấy danh sách hồ sơ ứng tuyển của các bài đăng đó
            var applications = await _context.Applications
                .Include(a => a.JobPost)
                .Where(a => myJobs.Contains(a.JobPostId))
                .OrderByDescending(a => a.AppliedDate)
                .ToListAsync();

            // 3. Chuẩn bị danh sách ViewModels truyền ra giao diện
            var applicantViewModels = applications.Select(application =>
            {
                // Tìm tài khoản Identity để lấy Email/UserName
                var user = _context.Users.FirstOrDefault(u => u.Id == application.CandidateId);

                // --- ĐOẠN SỬA QUAN TRỌNG: Lấy thông tin Candidate từ DB để lấy đúng file CV cá nhân ---
                var candidateInfo = _context.Candidates.FirstOrDefault(c => c.Email == user.Email);

                return new EmployerApplicantViewModel
                {
                    Id = application.Id,
                    JobPostId = application.JobPostId,
                    JobTitle = application.JobPost?.Title ?? "N/A",
                    CandidateId = application.CandidateId,
                    CandidateName = candidateInfo?.FullName ?? user?.UserName ?? "Ẩn danh",
                    CandidateEmail = user?.Email ?? "N/A",

                    // Lấy trực tiếp đường dẫn CVUrl được lưu trong bảng Candidates của ứng viên đó
                    CvUrl = candidateInfo?.CVUrl,

                    AppliedDate = application.AppliedDate,
                    Status = application.Status.ToString()
                };
            }).ToList();

            return View(applicantViewModels);
        }
        // 2. Trang Đăng tin mới (GET)
        public async Task<IActionResult> Create()
        {
            // Nạp danh sách để hiển thị Dropdown trên View [cite: 163, 165]
            ViewBag.Locations = await _context.Locations.OrderBy(l => l.Name).ToListAsync();
            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            return View();
        }

        // 3. Xử lý Đăng tin (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(JobPost job)
        {
            if (ModelState.IsValid)
            {
                job.EmployerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                job.CreatedAt = DateTime.Now;
                job.IsApproved = false; // Chờ Admin duyệt 

                _context.Add(job);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Đăng tin tuyển dụng thành công! Vui lòng chờ Admin duyệt.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Locations = await _context.Locations.OrderBy(l => l.Name).ToListAsync();
            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            return View(job);
        }

        // 4. Chỉnh sửa tin tuyển dụng (GET)
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var job = await _context.JobPosts
                .FirstOrDefaultAsync(j => j.Id == id && j.EmployerId == userId);

            if (job == null) return NotFound();

            ViewBag.Locations = await _context.Locations.OrderBy(l => l.Name).ToListAsync();
            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            return View(job);
        }

        // 5. Xử lý Chỉnh sửa (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, JobPost job)
        {
            if (id != job.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    var existingJob = await _context.JobPosts.AsNoTracking()
                        .FirstOrDefaultAsync(j => j.Id == id && j.EmployerId == userId);

                    if (existingJob == null) return Forbid();

                    // Giữ lại các thông tin hệ thống không cho phép sửa từ Form 
                    job.EmployerId = existingJob.EmployerId;
                    job.CreatedAt = existingJob.CreatedAt;
                    job.IsApproved = existingJob.IsApproved;

                    _context.Update(job);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Cập nhật tin tuyển dụng thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.JobPosts.Any(e => e.Id == job.Id)) return NotFound();
                    else throw;
                }
            }
            ViewBag.Locations = await _context.Locations.OrderBy(l => l.Name).ToListAsync();
            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            return View(job);
        }

        // 6. Xem danh sách ứng viên của một bài đăng cụ thể
        public async Task<IActionResult> Candidates(int jobId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var job = await _context.JobPosts.FirstOrDefaultAsync(j => j.Id == jobId && j.EmployerId == userId);

            if (job == null) return NotFound();

            var applications = await _context.Applications
                .Include(a => a.CV)
                .Where(a => a.JobPostId == jobId)
                .OrderByDescending(a => a.AppliedDate)
                .ToListAsync();

            var candidateIds = applications.Select(a => a.CandidateId).Distinct().ToList();
            var users = await _context.Users.Where(u => candidateIds.Contains(u.Id)).ToListAsync();

            var applicantViewModels = applications.Select(application => {
                var user = users.FirstOrDefault(u => u.Id == application.CandidateId);
                return new EmployerApplicantViewModel
                {
                    Id = application.Id,
                    JobPostId = application.JobPostId,
                    JobTitle = job.Title,
                    CandidateId = application.CandidateId,
                    CandidateName = user?.UserName ?? "Ẩn danh",
                    CandidateEmail = user?.Email ?? "N/A",
                    CvFileName = application.CV?.FileName,
                    CvUrl = application.CV != null ? $"/uploads/cvs/{application.CV.FileName}" : null,
                    AppliedDate = application.AppliedDate,
                    Status = application.Status.ToString()
                };
            }).ToList();

            return View(applicantViewModels);
        }

        // 7. Xóa tin tuyển dụng (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var job = await _context.JobPosts.FirstOrDefaultAsync(j => j.Id == id && j.EmployerId == userId);

            if (job != null)
            {
                _context.JobPosts.Remove(job);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Xóa tin tuyển dụng thành công!";
            }
            return RedirectToAction(nameof(Index));
        }
        // Thêm vào trong JobController.cs của Areas/Employer
        public IActionResult Messages()
        {
            ViewData["Title"] = "Quản lý tin nhắn";
            return View();
        }
    }
}