using System.Security.Claims;
using JobPortal.Data;
using JobPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;

namespace JobPortal.Controllers;

[Authorize]
public sealed class CVController(ApplicationDbContext database, PrivateFileStorage files) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Download(int id, CancellationToken token)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var cv = await database.CVs.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id, token);
        if (cv == null) return NotFound();
        var permitted = cv.CandidateId == userId || User.IsInRole("Admin") ||
            (User.IsInRole("Employer") && await database.Applications.AnyAsync(a => a.CVId == id && a.JobPost.EmployerId == userId, token));
        if (!permitted) return NotFound();
        var path = files.GetPdfPath(cv.FileName);
        if (path == null) return NotFound();
        Response.Headers.CacheControl = "private, no-store";
        Response.Headers.XContentTypeOptions = "nosniff";
        var name = string.IsNullOrWhiteSpace(cv.Title) ? "CV.pdf" : string.Concat(cv.Title.Where(c => !Path.GetInvalidFileNameChars().Contains(c))).Trim() + ".pdf";
        var disposition = new ContentDispositionHeaderValue("inline");
        disposition.SetHttpFileName(name);
        Response.Headers.ContentDisposition = disposition.ToString();
        return PhysicalFile(path, "application/pdf", enableRangeProcessing: true);
    }

    [HttpGet]
    public async Task<IActionResult> LegacyDownload(string fileName, CancellationToken token)
    {
        if (fileName != Path.GetFileName(fileName)) return NotFound();
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var accessible = database.CVs.AsNoTracking().Where(c => c.FileName == fileName &&
            (c.CandidateId == userId || User.IsInRole("Admin") ||
                (User.IsInRole("Employer") && database.Applications.Any(a => a.CVId == c.Id && a.JobPost.EmployerId == userId))));
        var id = await accessible.Select(c => (int?)c.Id).FirstOrDefaultAsync(token);
        return id.HasValue ? await Download(id.Value, token) : NotFound();
    }
}
