using System.Security.Claims;
using JobPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JobPortal.Areas.Candidate.Controllers;

[Area("Candidate"), Authorize(Roles = "Candidate")]
public sealed class AiInterviewReportController(AiInterviewService interviews) : Controller
{
    [HttpGet, ActionName("Details")]
    public async Task<IActionResult> XemBaoCao(Guid id, CancellationToken token)
    {
        var report = await interviews.LayBaoCaoAsync(id, User.FindFirstValue(ClaimTypes.NameIdentifier)!, token);
        if (report == null) return NotFound();
        ViewData["WorkspaceController"] = "AiInterview";
        return View("Details", report);
    }
}
