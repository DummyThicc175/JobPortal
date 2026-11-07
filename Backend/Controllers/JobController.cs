using JobPortal.Data;
using JobPortal.Infrastructure;
using JobPortal.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace JobPortal.Controllers;

public sealed class JobController(ApplicationDbContext database, JobService jobs) : Controller
{
    public async Task<IActionResult> Index(string? keyword, int? locationId, int page = 1, CancellationToken token = default)
    {
        ViewBag.Locations = new SelectList(await database.Locations.AsNoTracking().OrderBy(l => l.Name).ToListAsync(token), "Id", "Name", locationId);
        ViewBag.Keyword = keyword?.Trim(); ViewBag.SelectedLocation = locationId;
        return View("Index", await jobs.Search(keyword, locationId).PageAsync(this, page, token));
    }

    public Task<IActionResult> Search(string? searchString, int? locationId, int page = 1, CancellationToken token = default) =>
        Index(searchString, locationId, page, token);

    public async Task<IActionResult> Details(int id, CancellationToken token)
    {
        var job = await database.JobPosts.AsNoTracking().Include(j => j.Company).Include(j => j.Category).Include(j => j.JobLocation)
            .SingleOrDefaultAsync(j => j.Id == id && j.IsApproved, token);
        return job == null ? NotFound() : View(job);
    }
}
