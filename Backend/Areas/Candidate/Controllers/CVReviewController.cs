using System.Security.Claims;
using JobPortal.Models;
using JobPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace JobPortal.Areas.Candidate.Controllers;

[Area("Candidate")]
[Authorize(Roles = "Candidate")]
public sealed class CVReviewController(GeminiCvReviewService reviews) : Controller
{
    [HttpPost]
    [EnableRateLimiting("cv-ai")]
    [RequestSizeLimit(100000)]
    [ActionName("Analyze")]
    public async Task<IActionResult> DanhGiaCV([FromBody] EditCVViewModel? model, CancellationToken token)
    {
        if (model == null || !ModelState.IsValid)
            return BadRequest(new { success = false, message = "Kiểm tra lại các thông tin CV trước khi đánh giá." });
        try
        {
            var result = await reviews.DanhGiaAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!, model, token);
            return Json(new { success = true, score = result.Score, suggestions = result.Suggestions });
        }
        catch (AiReviewException exception)
        { return StatusCode(exception.StatusCode, new { success = false, message = exception.Message }); }
    }
}
