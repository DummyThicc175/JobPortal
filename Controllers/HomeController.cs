using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using JobPortal.Models;
using JobPortal.Data; // Đảm bảo có namespace này để dùng ApplicationDbContext
using Microsoft.EntityFrameworkCore;

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