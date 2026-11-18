using System.Security.Claims;
using JobPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace JobPortal.Areas.Candidate.Controllers;

[Area("Candidate"), Authorize(Roles = "Candidate")]
public sealed class AiInterviewSpeechController(AiInterviewService interviews, InterviewSpeechJobs jobs) : Controller
{
    [HttpPost, ActionName("Transcribe"), EnableRateLimiting("interview-speech")]
    [RequestSizeLimit(InterviewSpeechService.MaxAudioBytes + 65536)]
    [RequestFormLimits(MemoryBufferThreshold = InterviewSpeechService.MaxAudioBytes + 65536, MultipartBodyLengthLimit = InterviewSpeechService.MaxAudioBytes + 65536)]
    public async Task<IActionResult> NhanGiongNoi(Guid id, IFormFile? audio, CancellationToken token)
    {
        var session = await interviews.LayPhienAsync(id, User.FindFirstValue(ClaimTypes.NameIdentifier)!, token);
        if (session == null) return NotFound();
        if (session.EndedAt != null) return Conflict(new { message = "Phiên luyện tập đã kết thúc." });
        if (audio == null || audio.Length is <= 0 or > InterviewSpeechService.MaxAudioBytes)
            return BadRequest(new { message = "Bản ghi âm không hợp lệ hoặc dài quá 2 phút." });
        try
        {
            await using var stream = audio.OpenReadStream();
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, token);
            var bytes = buffer.ToArray();
            InterviewSpeechService.KiemTraAmThanh(bytes);
            var jobId = jobs.ThemCongViec(User.FindFirstValue(ClaimTypes.NameIdentifier)!, id, bytes);
            return Accepted(new { jobId, statusUrl = Url.Action("Status", new { id, jobId }) });
        }
        catch (AiInterviewException ex) { return StatusCode(ex.StatusCode, new { message = ex.Message }); }
    }

    [HttpGet, ActionName("Status"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult LayTrangThai(Guid id, Guid jobId)
    {
        var status = jobs.LayTrangThai(jobId, User.FindFirstValue(ClaimTypes.NameIdentifier)!, id);
        return status == null ? NotFound(new { message = "Bản nhận giọng nói đã hết hạn. Bạn thử lại nhé." }) : Json(status);
    }
}
