using System.Security.Claims;
using JobPortal.Data;
using JobPortal.Infrastructure;
using JobPortal.Models;
using JobPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace JobPortal.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public sealed class UserManagerController(ApplicationDbContext database, UserManager<IdentityUser> users, IMemoryCache cache) : Controller
{
    [ActionName("Index")]
    public async Task<IActionResult> DanhSach(string? searchEmail, int page = 1, CancellationToken token = default)
    {
        var query = database.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(searchEmail)) query = query.Where(u => u.Email != null && u.Email.Contains(searchEmail.Trim()));
        var list = await query.OrderBy(u => u.Email).Select(u => new AdminUserViewModel
        {
            Id = u.Id, Email = u.Email, LockoutEnd = u.LockoutEnd,
            Role = (from assignment in database.UserRoles join role in database.Roles on assignment.RoleId equals role.Id
                    where assignment.UserId == u.Id orderby role.Name select role.Name).FirstOrDefault() ?? "Chưa phân quyền"
        }).PhanTrangAsync(this, page, token);
        return View(list);
    }

    [HttpPost]
    [ActionName("ToggleLock")]
    public async Task<IActionResult> ChuyenTrangThaiKhoa(string userId)
    {
        var user = await users.FindByIdAsync(userId);
        if (user == null) return NotFound();
        if (userId == User.FindFirstValue(ClaimTypes.NameIdentifier))
        { TempData["Error"] = "Bạn không thể tự khóa tài khoản đang quản trị."; return RedirectToAction("Index"); }
        var locked = user.LockoutEnd > DateTimeOffset.UtcNow;
        if (!user.LockoutEnabled) await users.SetLockoutEnabledAsync(user, true);
        var result = await users.SetLockoutEndDateAsync(user, locked ? null : DateTimeOffset.UtcNow.AddYears(100));
        if (!result.Succeeded) { TempData["Error"] = "Chưa cập nhật được trạng thái tài khoản."; return RedirectToAction("Index"); }
        await users.UpdateSecurityStampAsync(user);
        cache.Remove(CookieSessionValidator.TaoKhoaPhien(userId));
        TempData["Success"] = locked ? "Đã mở khóa tài khoản." : "Đã khóa tài khoản và kết thúc các phiên đăng nhập cũ.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ActionName("AssignRole")]
    public async Task<IActionResult> PhanQuyen(string userId, string roleName, CancellationToken token)
    {
        if (roleName is not ("Candidate" or "Employer" or "Admin")) return BadRequest();
        var user = await users.FindByIdAsync(userId);
        if (user == null) return NotFound();
        var current = await users.GetRolesAsync(user);
        if (userId == User.FindFirstValue(ClaimTypes.NameIdentifier) && roleName != "Admin")
        { TempData["Error"] = "Không thể tự gỡ quyền của tài khoản đang quản trị."; return RedirectToAction("Index"); }
        if (current.Count == 1 && current[0] == roleName) return RedirectToAction("Index");
        await using var transaction = await database.Database.BeginTransactionAsync(token);
        if (!current.Contains(roleName))
        {
            var added = await users.AddToRoleAsync(user, roleName);
            if (!added.Succeeded) { await transaction.RollbackAsync(token); TempData["Error"] = "Không thể cấp quyền mới."; return RedirectToAction("Index"); }
        }
        var removed = await users.RemoveFromRolesAsync(user, current.Where(role => role != roleName));
        if (!removed.Succeeded || !(await users.UpdateSecurityStampAsync(user)).Succeeded)
        { await transaction.RollbackAsync(token); TempData["Error"] = "Không thể cập nhật quyền. Các quyền cũ được giữ lại."; return RedirectToAction("Index"); }
        await transaction.CommitAsync(token);
        cache.Remove(CookieSessionValidator.TaoKhoaPhien(userId));
        TempData["Success"] = "Đã cập nhật quyền. Người dùng cần đăng nhập lại.";
        return RedirectToAction("Index");
    }
}
