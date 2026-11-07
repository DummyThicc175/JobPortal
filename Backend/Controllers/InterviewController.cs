using System.Security.Claims;
using JobPortal.Configuration;
using JobPortal.Data;
using JobPortal.Infrastructure;
using JobPortal.Models;
using JobPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace JobPortal.Controllers;

[Authorize(Roles = "Candidate,Employer")]
[Route("Interview")]
public sealed class InterviewController(InterviewService interviews, ApplicationDbContext database, IOptions<InterviewOptions> options) : Controller
{
    [HttpGet("Room/{id:guid}")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    [ActionName("Room")]
    public async Task<IActionResult> PhongPhongVan(Guid id, CancellationToken token)
    {
        var session = await interviews.LayPhienCoQuyenAsync(id, User, token);
        if (session == null) return NotFound();
        Response.Headers["Permissions-Policy"] = "camera=(self), microphone=(self)";
        Response.Headers["Referrer-Policy"] = "same-origin";
        var application = session.Application;
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var isEmployer = application.JobPost.EmployerId == userId;
        var candidateName = await database.Candidates.Where(c => c.UserId == application.CandidateId).Select(c => c.FullName).FirstOrDefaultAsync(token);
        ViewData["WorkspaceArea"] = isEmployer ? "Employer" : "Candidate";
        ViewData["WorkspaceController"] = isEmployer ? "Applicant" : "Application";
        return View(new InterviewRoomViewModel(id, application.Id, application.JobPost.Title, application.JobPost.CompanyName ?? "Nhà tuyển dụng",
            isEmployer ? candidateName ?? "Ứng viên" : application.JobPost.CompanyName ?? "Nhà tuyển dụng", isEmployer, options.Value.TaoCauHinhKetNoi(userId)));
    }

    [Authorize(Roles = "Employer")]
    [HttpPost("Create")]
    [ActionName("Create")]
    public async Task<IActionResult> TaoPhien(int applicationId, CancellationToken token)
    {
        try
        {
            var id = await interviews.TaoPhienAsync(applicationId, User.FindFirstValue(ClaimTypes.NameIdentifier)!, token);
            return id == null ? NotFound() : RedirectToAction("Room", new { id });
        }
        catch (DbUpdateException error) when (DbErrors.LaLoiTrungDuLieu(error))
        { return Conflict("Phòng vừa được tạo ở cửa sổ khác. Vui lòng tải lại danh sách hồ sơ."); }
    }

    [Authorize(Roles = "Employer")]
    [HttpPost("End/{id:guid}")]
    [ActionName("End")]
    public async Task<IActionResult> KetThucPhien(Guid id, CancellationToken token)
    {
        if (!await interviews.KetThucPhienAsync(id, User.FindFirstValue(ClaimTypes.NameIdentifier)!, token)) return NotFound();
        TempData["Success"] = "Đã kết thúc phiên phỏng vấn. Bạn có thể tạo phiên mới nếu cần trao đổi thêm.";
        return RedirectToAction("Index", "Applicant", new { area = "Employer" });
    }
}
