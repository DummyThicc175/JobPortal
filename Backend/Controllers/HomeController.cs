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
    [ActionName("Index")]
    public async Task<IActionResult> TrangChu(int page = 1, CancellationToken token = default)
    {
        ViewBag.Locations = new SelectList(await database.Locations.AsNoTracking().OrderBy(l => l.Name).ToListAsync(token), "Id", "Name");
        return View(await jobs.TimKiem(null, null).PhanTrangAsync(this, page, token));
    }
    [ActionName("Privacy")]
    public IActionResult BaoMat() => View();
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [ActionName("Error")]
    public IActionResult ThongBaoLoi() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
