using JobPortal.Data; // Đảm bảo đúng namespace chứa ApplicationDbContext
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace JobPortal.Areas.Admin.Controllers
{
    [Area("Admin")] // Khai báo thuộc tính vùng
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        // Constructor để lấy DbContext từ hệ thống
        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // Lấy số liệu thật từ các bảng trong Database
            ViewBag.TotalJobs = _context.JobPosts.Count();

            // Đếm các tin có IsApproved là false (Chờ duyệt)
            ViewBag.PendingJobs = _context.JobPosts.Count(j => j.IsApproved == false);

            // Đếm tổng số tài khoản người dùng
            ViewBag.TotalUsers = _context.Users.Count();

            return View();
        }
    }
}