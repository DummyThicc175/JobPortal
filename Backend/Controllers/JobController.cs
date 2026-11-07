using JobPortal.Data;
using JobPortal.Infrastructure;
using JobPortal.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace JobPortal.Controllers;

public sealed class JobController(ApplicationDbContext database, JobService jobs) : Controller
{
    [ActionName("Index")]
    public async Task<IActionResult> DanhSach(string? keyword, int? locationId, int page = 1, CancellationToken token = default)
    {
        ViewBag.Locations = new SelectList(await database.Locations.AsNoTracking().OrderBy(l => l.Name).ToListAsync(token), "Id", "Name", locationId);
        ViewBag.Keyword = keyword?.Trim(); ViewBag.SelectedLocation = locationId;
        return View("Index", await jobs.TimKiem(keyword, locationId).PhanTrangAsync(this, page, token));
    }

    [ActionName("Search")]
    public Task<IActionResult> TimKiem(string? searchString, int? locationId, int page = 1, CancellationToken token = default) =>
        DanhSach(searchString, locationId, page, token);

    [ActionName("Details")]
    public async Task<IActionResult> ChiTiet(int id, CancellationToken token)
    {
        var job = await database.JobPosts.AsNoTracking().Include(j => j.Company).Include(j => j.Category).Include(j => j.JobLocation)
            .SingleOrDefaultAsync(j => j.Id == id && j.IsApproved, token);
        return job == null ? NotFound() : View(job);
    }
}
