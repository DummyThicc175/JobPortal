using System.Diagnostics;
using JobPortal.Data;
using JobPortal.Infrastructure;
using JobPortal.Models;
using JobPortal.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace JobPortal.Controllers;

public sealed class HomeController(ApplicationDbContext database, JobService jobs) : Controller
{
    public async Task<IActionResult> Index(int page = 1, CancellationToken token = default)
    {
        ViewBag.Locations = new SelectList(await database.Locations.AsNoTracking().OrderBy(l => l.Name).ToListAsync(token), "Id", "Name");
        return View(await jobs.Search(null, null).PageAsync(this, page, token));
    }
    public IActionResult Privacy() => View();
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
