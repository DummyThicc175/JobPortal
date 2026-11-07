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
public sealed class JobController(ApplicationDbContext database, JobService jobs) : Controller
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    public async Task<IActionResult> Index(string? keyword, int page = 1, CancellationToken token = default)
    {
        var owned = database.JobPosts.AsNoTracking().Where(j => j.EmployerId == UserId);
        ViewBag.TotalJobs = await owned.CountAsync(token);
        ViewBag.ApprovedJobs = await owned.CountAsync(j => j.IsApproved, token);
        ViewBag.PendingJobs = await owned.CountAsync(j => !j.IsApproved, token);
        if (!string.IsNullOrWhiteSpace(keyword)) owned = owned.Where(j => j.Title.Contains(keyword.Trim()));
        ViewBag.Keyword = keyword;
        return View(await owned.Include(j => j.Company).Include(j => j.Category).Include(j => j.JobLocation)
            .OrderByDescending(j => j.CreatedAt).ThenByDescending(j => j.Id).PageAsync(this, page, token));
    }

    public async Task<IActionResult> Create(CancellationToken token)
    { await Lookups(token); return View(new JobFormViewModel()); }

    [HttpPost]
    public async Task<IActionResult> Create(JobFormViewModel model, CancellationToken token)
    {
        await ValidateLookups(model, token);
        if (ModelState.IsValid)
        {
            var company = await jobs.CompanyAsync(UserId, model.CompanyName, token);
            var job = new JobPost { EmployerId = UserId, CreatedAt = DateTime.UtcNow, IsApproved = false, Company = company };
            Apply(job, model);
            database.JobPosts.Add(job);
            try
            {
                await database.SaveChangesAsync(token);
                TempData["Success"] = "Đăng tin thành công. Vui lòng chờ quản trị viên duyệt.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException exception) when (DbErrors.IsUniqueViolation(exception))
            { ModelState.AddModelError("", "Thông tin công ty vừa thay đổi. Vui lòng gửi lại biểu mẫu."); }
        }
        await Lookups(token); return View(model);
    }

    public async Task<IActionResult> Edit(int id, CancellationToken token)
    {
        var job = await database.JobPosts.AsNoTracking().Include(j => j.Company).SingleOrDefaultAsync(j => j.Id == id && j.EmployerId == UserId, token);
        if (job == null) return NotFound();
        await Lookups(token);
        return View(new JobFormViewModel { Id = job.Id, Title = job.Title, Description = job.Description ?? "", CompanyName = job.CompanyName ?? "", SalaryRange = job.SalaryRange, CategoryId = job.CategoryId, LocationId = job.LocationId });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, JobFormViewModel model, CancellationToken token)
    {
        if (id != model.Id) return BadRequest();
        var job = await database.JobPosts.Include(j => j.Company).SingleOrDefaultAsync(j => j.Id == id && j.EmployerId == UserId, token);
        if (job == null) return NotFound();
        await ValidateLookups(model, token);
        if (!ModelState.IsValid) { await Lookups(token); return View(model); }
        var changed = job.Title != model.Title.Trim() || job.Description != model.Description.Trim() || job.CompanyName != model.CompanyName.Trim() ||
            job.SalaryRange != model.SalaryRange?.Trim() || job.CategoryId != model.CategoryId || job.LocationId != model.LocationId;
        job.Company = await jobs.CompanyAsync(UserId, model.CompanyName, token);
        Apply(job, model);
        if (changed) job.IsApproved = false;
        try { await database.SaveChangesAsync(token); }
        catch (DbUpdateException exception) when (DbErrors.IsUniqueViolation(exception))
        { ModelState.AddModelError("", "Thông tin công ty vừa thay đổi. Vui lòng gửi lại biểu mẫu."); await Lookups(token); return View(model); }
        TempData["Success"] = changed ? "Đã lưu nội dung sửa. Tin cần được duyệt lại trước khi hiển thị." : "Nội dung tin không thay đổi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id, CancellationToken token)
    {
        var job = await database.JobPosts.SingleOrDefaultAsync(j => j.Id == id && j.EmployerId == UserId, token);
        if (job == null) return NotFound();
        TempData["Success"] = await jobs.RemoveOrHideAsync(job, token) ? "Đã xóa tin." : "Tin đã có hồ sơ hoặc được lưu: đã gỡ hiển thị và giữ lịch sử.";
        return RedirectToAction(nameof(Index));
    }
    private static void Apply(JobPost job, JobFormViewModel model)
    {
        job.Title = model.Title.Trim(); job.Description = model.Description.Trim(); job.SalaryRange = model.SalaryRange?.Trim();
        job.CategoryId = model.CategoryId; job.LocationId = model.LocationId;
    }
    private async Task ValidateLookups(JobFormViewModel model, CancellationToken token)
    {
        if (!await database.Categories.AnyAsync(c => c.Id == model.CategoryId, token)) ModelState.AddModelError(nameof(model.CategoryId), "Ngành nghề không tồn tại.");
        if (!await database.Locations.AnyAsync(l => l.Id == model.LocationId, token)) ModelState.AddModelError(nameof(model.LocationId), "Địa điểm không tồn tại.");
    }
    private async Task Lookups(CancellationToken token)
    {
        ViewBag.Locations = await database.Locations.AsNoTracking().OrderBy(l => l.Name).ToListAsync(token);
        ViewBag.Categories = await database.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync(token);
    }
}
