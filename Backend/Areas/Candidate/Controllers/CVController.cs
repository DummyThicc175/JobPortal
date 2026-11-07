using System.Security.Claims;
using System.Text.Json;
using JobPortal.Data;
using JobPortal.Models;
using JobPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobPortal.Areas.Candidate.Controllers;

[Area("Candidate")]
[Authorize(Roles = "Candidate")]
public sealed class CVController(ApplicationDbContext database, CvService cvs) : Controller
{
    [ActionName("SelectTemplate")]
    public async Task<IActionResult> ChonMau(CancellationToken token) =>
        View(await database.CVTemplates.AsNoTracking().Where(t => t.Id >= 1 && t.Id <= 6).OrderBy(t => t.Id).ToListAsync(token));

    [ActionName("Create")]
    public async Task<IActionResult> TaoCV(int templateId, CancellationToken token)
    {
        if (templateId <= 0) return RedirectToAction("SelectTemplate");
        var editor = await cvs.LayDuLieuChinhSuaAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!, templateId, token);
        return editor == null ? NotFound() : View(editor);
    }

    [HttpPost]
    [RequestSizeLimit(13 * 1024 * 1024)]
    [ActionName("Save")]
    public async Task<IActionResult> LuuCV(string modelJson, IFormFile? pdfFile, IFormFile? avatarFile, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(modelJson) || modelJson.Length > 100000 || pdfFile == null)
            return BadRequest(new { success = false, message = "Thiếu nội dung hoặc file PDF của CV." });
        EditCVViewModel? model;
        try { model = JsonSerializer.Deserialize<EditCVViewModel>(modelJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }); }
        catch (JsonException) { return BadRequest(new { success = false, message = "Dữ liệu CV không hợp lệ." }); }
        ModelState.Clear();
        if (model == null || !TryValidateModel(model))
            return BadRequest(new { success = false, message = "Kiểm tra lại họ tên, email, vị trí công việc và độ dài các mục CV." });
        try
        {
            var document = await cvs.LuuCVTuTrinhTaoAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!, model, pdfFile, avatarFile, token);
            return Json(new { success = true, id = document.Id, url = CvService.TaoDuongDanXemCV(document.Id),
                score = document.AiScore, suggestions = CvService.ChuyenSangDuLieuChinhSua(document).AiSuggestions });
        }
        catch (InvalidDataException exception) { return BadRequest(new { success = false, message = exception.Message }); }
        catch (UnauthorizedAccessException) { return NotFound(); }
    }
}
