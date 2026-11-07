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
public sealed class ProfileController(ApplicationDbContext database, CvService cvs, PrivateFileStorage files, ILogger<ProfileController> logger) : Controller
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [ActionName("Index")]
    public async Task<IActionResult> HoSo(CancellationToken token)
    {
        var model = await TaiHoSoAsync(token);
        model.AppliedJobs = await database.Applications.AsNoTracking().Include(a => a.JobPost).ThenInclude(j => j.Company)
            .Where(a => a.CandidateId == UserId).OrderByDescending(a => a.AppliedDate).Take(10).ToListAsync(token);
        return View(model);
    }

    [ActionName("Edit")]
    public async Task<IActionResult> ChinhSua(CancellationToken token) => View(await TaiHoSoAsync(token));

    [HttpPost]
    [RequestSizeLimit(13 * 1024 * 1024)]
    [ActionName("SaveProfile")]
    public async Task<IActionResult> LuuHoSo(CandidateProfileViewModel model, CancellationToken token)
    {
        if (model.Birthday is DateTime birthday && (birthday.Date > DateTime.Today || birthday.Year < 1900))
            ModelState.AddModelError(nameof(model.Birthday), "Ngày sinh không hợp lệ.");
        var originalView = await TaiHoSoAsync(token);
        model.Email = originalView.Email; model.CVUrl = originalView.CVUrl; model.AvatarUrl = originalView.AvatarUrl;
        if (!ModelState.IsValid) return View("Edit", model);
        string? newPdf = null, newAvatar = null;
        var profile = await cvs.LayHoSoAsync(UserId, token);
        var previousCv = profile.DefaultCVId;
        var oldAvatar = profile.AvatarUrl;
        try
        {
            if (model.CVFile != null) newPdf = await files.LuuPDFAsync(model.CVFile, token);
            if (model.AvatarFile != null) newAvatar = await files.LuuAnhDaiDienAsync(model.AvatarFile, token);
            profile.FullName = model.FullName?.Trim(); profile.Phone = model.Phone?.Trim();
            profile.Address = model.Address?.Trim(); profile.Gender = model.Gender; profile.Birthday = model.Birthday;
            if (newAvatar != null) profile.AvatarUrl = newAvatar;
            if (newPdf != null) profile.DefaultCV = cvs.TaoCVTaiLen(UserId, newPdf, profile, originalView.Email);
            await database.SaveChangesAsync(token);
        }
        catch (InvalidDataException exception)
        {
            files.XoaPDFMoi(newPdf); files.XoaAnhDaiDien(newAvatar);
            ModelState.AddModelError("", exception.Message);
            return View("Edit", model);
        }
        catch (Exception exception) when (exception is IOException or DbUpdateException)
        {
            files.XoaPDFMoi(newPdf); files.XoaAnhDaiDien(newAvatar);
            logger.LogError(exception, "Unable to save candidate profile for {UserId}", UserId);
            ModelState.AddModelError("", "Chưa lưu được hồ sơ. Vui lòng thử lại.");
            return View("Edit", model);
        }
        if (newPdf != null && previousCv.HasValue) await cvs.XoaBanNhapDaThayTheAsync(previousCv.Value, token);
        await cvs.XoaCVKhongDuocThamChieuAsync(null, oldAvatar, token);
        TempData["Success"] = "Cập nhật hồ sơ thành công.";
        return RedirectToAction("Index");
    }

    private async Task<CandidateProfileViewModel> TaiHoSoAsync(CancellationToken token)
    {
        var candidate = await database.Candidates.AsNoTracking().SingleOrDefaultAsync(c => c.UserId == UserId, token);
        var email = await database.Users.Where(u => u.Id == UserId).Select(u => u.Email).SingleAsync(token);
        return new CandidateProfileViewModel
        {
            FullName = candidate?.FullName, Email = email, Phone = candidate?.Phone, Address = candidate?.Address,
            AvatarUrl = candidate?.AvatarUrl ?? "/images/default-avatar.png", Gender = candidate?.Gender,
            Birthday = candidate?.Birthday, CVUrl = candidate?.DefaultCVId is int id ? CvService.TaoDuongDanXemCV(id) : null
        };
    }
}
