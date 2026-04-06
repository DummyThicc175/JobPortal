using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class UserManagerController : Controller
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public UserManagerController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    // 1. Danh sách người dùng
    public async Task<IActionResult> Index(string searchEmail)
    {
        var users = _userManager.Users.AsQueryable();

        if (!string.IsNullOrEmpty(searchEmail))
        {
            users = users.Where(u => u.Email.Contains(searchEmail));
        }

        return View(await users.ToListAsync());
    }
    // 2. Khóa hoặc Mở khóa tài khoản
    [HttpPost]
    public async Task<IActionResult> ToggleLock(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        if (user.LockoutEnd == null || user.LockoutEnd < DateTime.Now)
        {
            // Khóa vĩnh viễn (đặt thời hạn khóa 100 năm)
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
            TempData["Success"] = "Đã khóa tài khoản " + user.Email;
        }
        else
        {
            // Mở khóa
            await _userManager.SetLockoutEndDateAsync(user, null);
            TempData["Success"] = "Đã mở khóa tài khoản " + user.Email;
        }
        return RedirectToAction(nameof(Index));
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignRole(string userId, string roleName)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        // 1. Lấy danh sách các quyền hiện tại của user
        var currentRoles = await _userManager.GetRolesAsync(user);

        // 2. Xóa tất cả các quyền cũ để tránh xung đột
        var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);

        if (!removeResult.Succeeded)
        {
            TempData["Error"] = "Không thể xóa quyền cũ của người dùng.";
            return RedirectToAction(nameof(Index));
        }

        // 3. Kiểm tra Role mới có tồn tại không, nếu không thì tạo mới
        if (!await _roleManager.RoleExistsAsync(roleName))
        {
            await _roleManager.CreateAsync(new IdentityRole(roleName));
        }

        // 4. Gán quyền mới
        var addResult = await _userManager.AddToRoleAsync(user, roleName);

        if (addResult.Succeeded)
        {
            TempData["Success"] = $"Đã thay đổi quyền của {user.Email} thành {roleName}";
        }
        else
        {
            TempData["Error"] = "Lỗi khi gán quyền mới.";
        }

        return RedirectToAction(nameof(Index));
    }
}