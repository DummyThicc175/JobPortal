using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JobPortal.Data;
using System.Linq;

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
        public IActionResult Pending()
        {
            // Lấy các tin có IsApproved == false
            var pendingJobs = _context.JobPosts.Where(j => !j.IsApproved).ToList();
            return View(pendingJobs);
        }

        // 2. Xử lý Duyệt tin (Dùng POST để bảo mật)
        [HttpPost]
        public IActionResult Approve(int id)
        {
            var job = _context.JobPosts.Find(id);
            if (job != null)
            {
                job.IsApproved = true;
                _context.SaveChanges();
            }
            return RedirectToAction("Pending");
        }
        // 3. Lấy thông tin chi tiết để xem trước (Dùng cho Modal)
        public IActionResult GetDetails(int id)
        {
            var job = _context.JobPosts.Find(id);
            if (job == null) return NotFound();

            // Trả về một PartialView hoặc Json (Ở đây ta dùng Json cho đơn giản với Modal)
            return Json(new
            {
                title = job.Title,
                description = job.Description,
                createdAt = job.CreatedAt.ToString("dd/MM/yyyy")
            });
        }
    }
}