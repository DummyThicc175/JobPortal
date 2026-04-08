using JobPortal.Data; // Đảm bảo có namespace này để dùng ApplicationDbContext
using JobPortal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;

namespace JobPortal.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context; // Khai báo context

    // Tiêm ApplicationDbContext vào Constructor
    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }


    public async Task<IActionResult> Index()
    {
        // Thêm .Where để chỉ lấy những tin đã được duyệt (IsApproved = true)
        // Nên kết hợp .Include để hiển thị đầy đủ tên công ty, địa điểm, ngành nghề
        var jobs = await _context.JobPosts
            .Include(j => j.Category)
            .Include(j => j.JobLocation)
            .Include(j => j.Company)
            .Where(j => j.IsApproved == true)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync();

        return View(jobs);
    }
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> SaveJob(int jobId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Kiểm tra xem đã lưu tin này chưa
        var existingSave = await _context.SavedJobs
            .FirstOrDefaultAsync(s => s.JobPostId == jobId && s.CandidateId == userId);

        if (existingSave != null)
        {
            TempData["Message"] = "Bạn đã lưu tin tuyển dụng này rồi!";
            return RedirectToAction("JobDetails", new { id = jobId });
        }

        var savedJob = new SavedJob
        {
            JobPostId = jobId,
            CandidateId = userId,
            SavedDate = DateTime.Now
        };

        _context.SavedJobs.Add(savedJob);
        await _context.SaveChangesAsync();

        TempData["SaveSuccess"] = "Đã lưu tin tuyển dụng thành công!";
        return RedirectToAction("JobDetails", new { id = jobId });
    }
    public async Task<IActionResult> JobDetails(int id)
    {
        // Tìm tin tuyển dụng theo ID, bao gồm cả dữ liệu liên quan như Địa điểm, Ngành nghề
        var job = await _context.JobPosts
            .Include(j => j.JobLocation)
            .Include(j => j.Category)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (job == null)
        {
            return NotFound();
        }

        return View(job);
    }
    [HttpPost]
    [Authorize] // Bắt buộc đăng nhập mới được nộp
    public async Task<IActionResult> ApplyJob(int jobId)
    {
        var userEmail = User.Identity.Name;
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // 1. Kiểm tra xem đã ứng tuyển chưa để tránh trùng lặp
        var existingApp = await _context.Applications
            .FirstOrDefaultAsync(a => a.JobPostId == jobId && a.CandidateId == userId);

        if (existingApp != null)
        {
            TempData["Message"] = "Bạn đã ứng tuyển công việc này rồi!";
            return RedirectToAction("JobDetails", new { id = jobId });
        }

        // 2. Tạo bản ghi ứng tuyển mới
        var application = new Application
        {
            JobPostId = jobId,
            CandidateId = userId,
            AppliedDate = DateTime.Now,
            Status = ApplicationStatus.Submitted
        };

        _context.Applications.Add(application);
        await _context.SaveChangesAsync();

        // 3. Gửi thông báo thành công
        TempData["ApplySuccess"] = true;
        return RedirectToAction("JobDetails", new { id = jobId });
    }
    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}