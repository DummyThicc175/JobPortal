using System.Security.Claims;
using JobPortal.Data;
using JobPortal.Infrastructure;
using JobPortal.Models;
using JobPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobPortal.Areas.Candidate.Controllers;

[Area("Candidate")]
[Authorize(Roles = "Candidate")]
public sealed class ApplicationController(ApplicationDbContext database, CvService cvs) : Controller
{
    [ActionName("Index")]
    public async Task<IActionResult> DanhSach(int page = 1, CancellationToken token = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        return View(await database.Applications.AsNoTracking().Include(a => a.JobPost).ThenInclude(j => j.Company)
            .Include(a => a.JobPost).ThenInclude(j => j.JobLocation).Include(a => a.CV)
            .Include(a => a.InterviewSessions.Where(s => s.EndedAt == null))
            .Where(a => a.CandidateId == userId).OrderByDescending(a => a.AppliedDate).ThenByDescending(a => a.Id)
            .PhanTrangAsync(this, page, token));
    }

    [HttpPost]
    [ActionName("Apply")]
    public async Task<IActionResult> UngTuyen(int jobId, CancellationToken token)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (!await database.JobPosts.AnyAsync(j => j.Id == jobId && j.IsApproved, token)) return NotFound();
        if (await database.Applications.AnyAsync(a => a.JobPostId == jobId && a.CandidateId == userId, token))
        {
            TempData["Message"] = "Bạn đã ứng tuyển công việc này rồi.";
            return RedirectToAction("Details", "Job", new { area = "", id = jobId });
        }
        var currentCv = await database.Candidates.Where(c => c.UserId == userId).Select(c => c.DefaultCVId).SingleOrDefaultAsync(token);
        var snapshot = currentCv.HasValue ? await cvs.TaoBanChupAsync(userId, currentCv.Value, token) : null;
        if (snapshot == null)
        {
            TempData["Error"] = "Bạn cần tải lên hoặc lưu CV trước khi ứng tuyển.";
            return RedirectToAction("Edit", "Profile");
        }
        await using var transaction = await database.Database.BeginTransactionAsync(token);
        try
        {
            database.Applications.Add(new Application
            {
                JobPostId = jobId, CandidateId = userId, CV = snapshot,
                AppliedDate = DateTime.UtcNow, Status = ApplicationStatus.Submitted
            });
            await database.SaveChangesAsync(token);
            await transaction.CommitAsync(token);
        }
        catch (DbUpdateException exception) when (DbErrors.LaLoiTrungDuLieu(exception))
        {
            await transaction.RollbackAsync(token);
            TempData["Message"] = "Bạn đã ứng tuyển công việc này rồi.";
            return RedirectToAction("Details", "Job", new { area = "", id = jobId });
        }
        TempData["ApplySuccess"] = true;
        return RedirectToAction("Details", "Job", new { area = "", id = jobId });
    }
}
