using System.Security.Claims;
using JobPortal.Data;
using JobPortal.Infrastructure;
using JobPortal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobPortal.Areas.Candidate.Controllers;

[Area("Candidate")]
[Authorize(Roles = "Candidate")]
public sealed class SavedJobController(ApplicationDbContext database) : Controller
{
    [ActionName("Index")]
    public async Task<IActionResult> DanhSach(int page = 1, CancellationToken token = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        return View(await database.SavedJobs.AsNoTracking().Include(s => s.JobPost).ThenInclude(j => j!.Company)
            .Include(s => s.JobPost).ThenInclude(j => j!.JobLocation).Where(s => s.CandidateId == userId)
            .OrderByDescending(s => s.SavedDate).ThenByDescending(s => s.Id).PhanTrangAsync(this, page, token));
    }

    [HttpPost]
    [ActionName("Save")]
    public async Task<IActionResult> LuuViec(int jobId, CancellationToken token)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (!await database.JobPosts.AnyAsync(j => j.Id == jobId && j.IsApproved, token)) return NotFound();
        if (!await database.SavedJobs.AnyAsync(s => s.JobPostId == jobId && s.CandidateId == userId, token))
        {
            database.SavedJobs.Add(new SavedJob { JobPostId = jobId, CandidateId = userId, SavedDate = DateTime.UtcNow });
            try { await database.SaveChangesAsync(token); }
            catch (DbUpdateException exception) when (DbErrors.LaLoiTrungDuLieu(exception)) { }
        }
        TempData["SaveSuccess"] = "Đã lưu tin tuyển dụng.";
        return RedirectToAction("Details", "Job", new { area = "", id = jobId });
    }
}
