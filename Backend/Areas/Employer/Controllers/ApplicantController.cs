using System.Security.Claims;
using JobPortal.Data;
using JobPortal.Infrastructure;
using JobPortal.Models;
using JobPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobPortal.Areas.Employer.Controllers;

[Area("Employer")]
[Authorize(Roles = "Employer")]
public sealed class ApplicantController(ApplicationDbContext database, InterviewService interviews) : Controller
{
    private IQueryable<Application> Owned => database.Applications.AsNoTracking()
        .Where(a => a.JobPost.EmployerId == User.FindFirstValue(ClaimTypes.NameIdentifier));

    [ActionName("Index")]
    public async Task<IActionResult> DanhSach(int page = 1, CancellationToken token = default) => View(await LayDanhSachAsync(null, page, token));
    [ActionName("ByJob")]
    public async Task<IActionResult> TheoTinTuyenDung(int jobId, int page = 1, CancellationToken token = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!await database.JobPosts.AnyAsync(j => j.Id == jobId && j.EmployerId == userId, token)) return NotFound();
        return View(await LayDanhSachAsync(jobId, page, token));
    }

    [HttpPost]
    [ActionName("UpdateStatus")]
    public async Task<IActionResult> CapNhatTrangThai(int id, ApplicationStatus status, CancellationToken token)
    {
        if (!ModelState.IsValid || !Enum.IsDefined(status)) return BadRequest();
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (!await interviews.CapNhatTrangThaiAsync(id, userId, status, token)) return NotFound();
        TempData["Success"] = status == ApplicationStatus.Interview
            ? "Đã chuyển hồ sơ sang Phỏng vấn. Phòng gọi video đã sẵn sàng cho hai bên."
            : "Đã cập nhật trạng thái hồ sơ.";
        return RedirectToAction("Index");
    }

    private async Task<List<EmployerApplicantViewModel>> LayDanhSachAsync(int? jobId, int page, CancellationToken token)
    {
        var query = Owned;
        if (jobId.HasValue) query = query.Where(a => a.JobPostId == jobId);
        var rows = await query.OrderByDescending(a => a.AppliedDate).ThenByDescending(a => a.Id).Select(a => new EmployerApplicantViewModel
        {
            Id = a.Id, JobPostId = a.JobPostId, JobTitle = a.JobPost.Title, CandidateId = a.CandidateId,
            CandidateName = a.CV != null && a.CV.FullName != null ? a.CV.FullName :
                database.Candidates.Where(c => c.UserId == a.CandidateId).Select(c => c.FullName).FirstOrDefault() ?? "Chưa có hồ sơ",
            CandidateEmail = a.CV != null && a.CV.Email != null ? a.CV.Email : database.Users.Where(u => u.Id == a.CandidateId).Select(u => u.Email).FirstOrDefault() ?? "",
            CvFileName = a.CV != null ? a.CV.FileName : null,
            CvUrl = a.CVId.HasValue ? "/CV/Download/" + a.CVId.Value : null,
            AppliedDate = a.AppliedDate, Status = a.Status.ToString(),
            InterviewId = a.InterviewSessions.Where(s => s.EndedAt == null).Select(s => (Guid?)s.Id).FirstOrDefault()
        }).PhanTrangAsync(this, page, token);
        return rows;
    }
}
