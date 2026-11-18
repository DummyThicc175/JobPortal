using System.Security.Claims;
using JobPortal.Services;
using JobPortal.ViewModels.Candidate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace JobPortal.Areas.Candidate.Controllers;

[Area("Candidate"), Authorize(Roles = "Candidate")]
public sealed class AiInterviewController(AiInterviewService interviews) : Controller
{
    [HttpGet, ActionName("Index")]
    public async Task<IActionResult> DanhSach(CancellationToken token) =>
        View("Index", await interviews.LayDanhSachAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!, token));

    [HttpPost, ActionName("Start"), EnableRateLimiting("interview-ai")]
    public async Task<IActionResult> BatDau(int applicationId, CancellationToken token)
    {
        try
        {
            var id = await interviews.BatDauAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!, applicationId, token);
            return RedirectToAction("Room", new { id });
        }
        catch (AiInterviewException ex)
        {
            if (ex.StatusCode == 404) return NotFound();
            TempData["Error"] = ex.Message;
            return RedirectToAction("Index");
        }
    }

    [HttpGet, ActionName("Room")]
    public async Task<IActionResult> VaoPhong(Guid id, CancellationToken token)
    {
        var session = await interviews.LayPhienAsync(id, User.FindFirstValue(ClaimTypes.NameIdentifier)!, token);
        if (session == null) return NotFound();
        if (session.EndedAt != null) return RedirectToAction("Details", "AiInterviewReport", new { id });
        var turn = session.Turns.Single(t => t.AnsweredAt == null);
        return View("Room", new AiInterviewRoomViewModel(id, session.JobTitle, session.CategoryName, session.CompanyName,
            session.HasCvContext, session.QuestionCount, turn.Number, turn.Question));
    }

    [HttpPost, ActionName("End")]
    public async Task<IActionResult> KetThuc(Guid id, CancellationToken token)
    {
        try { await interviews.KetThucAsync(id, User.FindFirstValue(ClaimTypes.NameIdentifier)!, token); }
        catch (AiInterviewException ex) when (ex.StatusCode == 404) { return NotFound(); }
        catch (AiInterviewException ex) { TempData["Error"] = ex.Message; return RedirectToAction("Room", new { id }); }
        return RedirectToAction("Details", "AiInterviewReport", new { id });
    }
}
