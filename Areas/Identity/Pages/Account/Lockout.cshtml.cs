using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace JobPortal.Areas.Identity.Pages.Account
{
    [AllowAnonymous] // Cho phép người dùng xem trang này mà không cần đăng nhập
    public class LockoutModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}