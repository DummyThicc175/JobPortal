using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;

namespace JobPortal.Areas.Identity.Pages.Account
{
    [AllowAnonymous] // Phải cho phép mọi người xem trang này dù chưa đăng nhập hoặc sai quyền
    public class AccessDeniedModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}