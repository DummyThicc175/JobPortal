using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace JobPortal.Areas.Employer.Controllers
{
    [Area("Employer")]
    [Authorize(Roles = "Employer")]
    public class ChatController : Controller
    {

        public IActionResult Index()
        {
            ViewData["Title"] = "Quản lý tin nhắn";
            return View();
        }
    }
}
