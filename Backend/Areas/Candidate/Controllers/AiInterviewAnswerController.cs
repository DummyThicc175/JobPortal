using System.Security.Claims;
using JobPortal.Services;
using JobPortal.ViewModels.Candidate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace JobPortal.Areas.Candidate.Controllers;

[Area("Candidate"), Authorize(Roles = "Candidate")]
public sealed class AiInterviewAnswerController(AiInterviewService interviews, AiInterviewAnswerJobs jobs,
    ILogger<AiInterviewAnswerController> logger) : Controller
{
    [HttpPost, ActionName("Submit"), EnableRateLimiting("interview-ai"), RequestSizeLimit(30000)]
    public async Task<IActionResult> GuiCauTraLoi(Guid id, [FromBody] AiInterviewAnswerInput? input, CancellationToken token)
    {
        if (input == null || !ModelState.IsValid) return BadRequest(new { message = "Câu trả lời cần từ 2 đến 4.000 ký tự." });
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            await interviews.KiemTraLuotTraLoiAsync(id, userId, input, token);
            var jobId = jobs.ThemCongViec(userId, id, input);
            return Accepted(new { jobId, statusUrl = Url.Action("Status", new { id, jobId }) });
        }
        catch (AiInterviewException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            logger.LogWarning("Kiểm tra yêu cầu chấm phỏng vấn gặp {ErrorType}.", ex.GetType().Name);
            return StatusCode(500, new { message = "Máy chủ chưa tiếp nhận được câu trả lời. Nội dung vẫn được giữ để bạn thử lại." });
        }
    }

    [HttpGet, ActionName("Status"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult LayTrangThai(Guid id, Guid jobId)
    {
        var status = jobs.LayTrangThai(jobId, User.FindFirstValue(ClaimTypes.NameIdentifier)!, id);
        if (status == null) return NotFound(new { message = "Tác vụ chấm đã hết hạn hoặc máy chủ vừa khởi động lại. Bạn tải lại trang để kiểm tra câu hỏi hiện tại nhé." });
        return Json(new
        {
            status = status.Status, completed = status.Completed, message = status.Message, statusCode = status.StatusCode,
            url = status.Status == "completed" ? status.Completed == true ?
                Url.Action("Details", "AiInterviewReport", new { id }) : Url.Action("Room", "AiInterview", new { id }) : null
        });
    }
}
