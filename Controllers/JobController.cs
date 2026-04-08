using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JobPortal.Data;

namespace JobPortal.Controllers
{
    public class JobController : Controller
    {
        private readonly ApplicationDbContext _context;

        public JobController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Trang danh sách tất cả công việc
        public async Task<IActionResult> Index(string keyword, int? locationId)
        {
            // 1. Lấy danh sách địa điểm để nạp vào Dropdown
            var locations = await _context.Locations.OrderBy(l => l.Name).ToListAsync();
            ViewBag.Locations = locations;

            // 2. Logic tìm kiếm
            var query = _context.JobPosts
                .Include(j => j.JobLocation)
                .Include(j => j.Category)
                .Where(j => j.IsApproved == true)
                .AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
            {
                query = query.Where(j => j.Title.Contains(keyword) || j.CompanyName.Contains(keyword));
            }

            // Lọc theo Id địa điểm từ CSDL
            if (locationId.HasValue)
            {
                query = query.Where(j => j.LocationId == locationId);
            }

            var results = await query.OrderByDescending(j => j.CreatedAt).ToListAsync();

            ViewBag.SelectedLocation = locationId;
            ViewBag.Keyword = keyword;

            return View(results);
        }
    }
}