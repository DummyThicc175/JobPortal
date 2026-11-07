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
    [ActionName("Index")]
    public async Task<IActionResult> DanhSach(string? keyword, int page = 1, CancellationToken token = default)
    {
        var owned = database.JobPosts.AsNoTracking().Where(j => j.EmployerId == UserId);
        ViewBag.TotalJobs = await owned.CountAsync(token);
        ViewBag.ApprovedJobs = await owned.CountAsync(j => j.IsApproved, token);
        ViewBag.PendingJobs = await owned.CountAsync(j => !j.IsApproved, token);
        if (!string.IsNullOrWhiteSpace(keyword)) owned = owned.Where(j => j.Title.Contains(keyword.Trim()));
        ViewBag.Keyword = keyword;
        return View(await owned.Include(j => j.Company).Include(j => j.Category).Include(j => j.JobLocation)
            .OrderByDescending(j => j.CreatedAt).ThenByDescending(j => j.Id).PhanTrangAsync(this, page, token));
    }

    [ActionName("Create")]
    public async Task<IActionResult> TaoTin(CancellationToken token)
    { await TaiDanhMucAsync(token); return View(new JobFormViewModel()); }

    [HttpPost]
    [ActionName("Create")]
    public async Task<IActionResult> TaoTin(JobFormViewModel model, CancellationToken token)
    {
        await KiemTraDanhMucAsync(model, token);
        if (ModelState.IsValid)
        {
            var company = await jobs.LayHoacTaoCongTyAsync(UserId, model.CompanyName, token);
            var job = new JobPost { EmployerId = UserId, CreatedAt = DateTime.UtcNow, IsApproved = false, Company = company };
            GanDuLieuTin(job, model);
            database.JobPosts.Add(job);
            try
            {
                await database.SaveChangesAsync(token);
                TempData["Success"] = "Đăng tin thành công. Vui lòng chờ quản trị viên duyệt.";
                return RedirectToAction("Index");
            }
            catch (DbUpdateException exception) when (DbErrors.LaLoiTrungDuLieu(exception))
            { ModelState.AddModelError("", "Thông tin công ty vừa thay đổi. Vui lòng gửi lại biểu mẫu."); }
        }
        await TaiDanhMucAsync(token); return View(model);
    }

    [ActionName("Edit")]
    public async Task<IActionResult> ChinhSuaTin(int id, CancellationToken token)
    {
        var job = await database.JobPosts.AsNoTracking().Include(j => j.Company).SingleOrDefaultAsync(j => j.Id == id && j.EmployerId == UserId, token);
        if (job == null) return NotFound();
        await TaiDanhMucAsync(token);
        return View(new JobFormViewModel { Id = job.Id, Title = job.Title, Description = job.Description ?? "", CompanyName = job.CompanyName ?? "", SalaryRange = job.SalaryRange, CategoryId = job.CategoryId, LocationId = job.LocationId });
    }

    [HttpPost]
    [ActionName("Edit")]
    public async Task<IActionResult> ChinhSuaTin(int id, JobFormViewModel model, CancellationToken token)
    {
        if (id != model.Id) return BadRequest();
        var job = await database.JobPosts.Include(j => j.Company).SingleOrDefaultAsync(j => j.Id == id && j.EmployerId == UserId, token);
        if (job == null) return NotFound();
        await KiemTraDanhMucAsync(model, token);
        if (!ModelState.IsValid) { await TaiDanhMucAsync(token); return View(model); }
        var changed = job.Title != model.Title.Trim() || job.Description != model.Description.Trim() || job.CompanyName != model.CompanyName.Trim() ||
            job.SalaryRange != model.SalaryRange?.Trim() || job.CategoryId != model.CategoryId || job.LocationId != model.LocationId;
        job.Company = await jobs.LayHoacTaoCongTyAsync(UserId, model.CompanyName, token);
        GanDuLieuTin(job, model);
        if (changed) job.IsApproved = false;
        try { await database.SaveChangesAsync(token); }
        catch (DbUpdateException exception) when (DbErrors.LaLoiTrungDuLieu(exception))
        { ModelState.AddModelError("", "Thông tin công ty vừa thay đổi. Vui lòng gửi lại biểu mẫu."); await TaiDanhMucAsync(token); return View(model); }
        TempData["Success"] = changed ? "Đã lưu nội dung sửa. Tin cần được duyệt lại trước khi hiển thị." : "Nội dung tin không thay đổi.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ActionName("Delete")]
    public async Task<IActionResult> XoaTin(int id, CancellationToken token)
    {
        var job = await database.JobPosts.SingleOrDefaultAsync(j => j.Id == id && j.EmployerId == UserId, token);
        if (job == null) return NotFound();
        TempData["Success"] = await jobs.XoaHoacAnTinAsync(job, token) ? "Đã xóa tin." : "Tin đã có hồ sơ hoặc được lưu: đã gỡ hiển thị và giữ lịch sử.";
        return RedirectToAction("Index");
    }
    private static void GanDuLieuTin(JobPost job, JobFormViewModel model)
    {
        job.Title = model.Title.Trim(); job.Description = model.Description.Trim(); job.SalaryRange = model.SalaryRange?.Trim();
        job.CategoryId = model.CategoryId; job.LocationId = model.LocationId;
    }
    private async Task KiemTraDanhMucAsync(JobFormViewModel model, CancellationToken token)
    {
        if (!await database.Categories.AnyAsync(c => c.Id == model.CategoryId, token)) ModelState.AddModelError(nameof(model.CategoryId), "Ngành nghề không tồn tại.");
        if (!await database.Locations.AnyAsync(l => l.Id == model.LocationId, token)) ModelState.AddModelError(nameof(model.LocationId), "Địa điểm không tồn tại.");
    }
    private async Task TaiDanhMucAsync(CancellationToken token)
    {
        ViewBag.Locations = await database.Locations.AsNoTracking().OrderBy(l => l.Name).ToListAsync(token);
        ViewBag.Categories = await database.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync(token);
    }
}
