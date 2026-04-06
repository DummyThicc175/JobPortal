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
        var jobPosts = await _context.JobPosts
            .Include(j => j.Company)      // Load bảng Company
            .Include(j => j.Category)     // Load bảng Category
            .Include(j => j.JobLocation)  // Load bảng Location (thông qua thuộc tính JobLocation)
            .AsNoTracking()               // Tăng tốc độ đọc dữ liệu
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync();

        return View(jobPosts);
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