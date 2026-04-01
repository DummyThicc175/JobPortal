using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JobPortal.Data;

[Area("Admin")]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _context;
    public DashboardController(ApplicationDbContext context) => _context = context;

    // Liệt kê các tin chưa duyệt
    public async Task<IActionResult> PendingJobs()
    {
        var pending = await _context.JobPosts.Where(j => !j.IsApproved).ToListAsync();
        return View(pending);
    }

    [HttpPost]
    public async Task<IActionResult> Approve(int id)
    {
        var job = await _context.JobPosts.FindAsync(id);
        if (job != null)
        {
            job.IsApproved = true;
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(PendingJobs));
    }
}