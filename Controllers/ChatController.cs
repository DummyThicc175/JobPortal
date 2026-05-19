using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JobPortal.Data;
using System.Security.Claims;

namespace JobPortal.Controllers
{
    public class ChatController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ChatController(ApplicationDbContext context)
        {
            _context = context;
        }
        [HttpGet]
        public async Task<IActionResult> GetChatPartners()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId)) return BadRequest();

            // Lấy tất cả tin nhắn liên quan đến người dùng hiện tại
            var messages = await _context.ChatMessages
                .Where(m => m.SenderId == currentUserId || m.ReceiverId == currentUserId)
                .OrderByDescending(m => m.Timestamp)
                .ToListAsync();

            // Lọc ra các ID của đối tác chat (không trùng lặp)
            var partnerIds = messages
                .Select(m => m.SenderId == currentUserId ? m.ReceiverId : m.SenderId)
                .Distinct()
                .ToList();

            // Lấy thông tin hiển thị (Ưu tiên lấy tên công ty nếu đối tác là Employer, hoặc tên tài khoản)
            var partners = new List<object>();
            foreach (var id in partnerIds)
            {
                // Kiểm tra xem đối tác có phải nhà tuyển dụng có công ty không
                var company = await _context.Companies.FirstOrDefaultAsync(c => c.EmployerId == id);
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
                
                partners.Add(new
                {
                    id = id,
                    name = company?.Name ?? user?.UserName ?? "Nhà tuyển dụng"
                });
            }

            return Json(partners);
        }
        // API lấy lịch sử tin nhắn giữa người dùng hiện tại và đối phương
        [HttpGet]
        public async Task<IActionResult> GetChatHistory(string partnerId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId) || string.IsNullOrEmpty(partnerId))
            {
                return BadRequest();
            }

            var messages = await _context.ChatMessages
                .Where(m => (m.SenderId == currentUserId && m.ReceiverId == partnerId) ||
                            (m.SenderId == partnerId && m.ReceiverId == currentUserId))
                .OrderBy(m => m.Timestamp)
                .Select(m => new {
                    senderId = m.SenderId,
                    content = m.Content,
                    time = m.Timestamp.ToString("HH:mm"),
                    isMe = m.SenderId == currentUserId
                })
                .ToListAsync();

            return Json(messages);
        }
        // Trang quản lý danh sách các ứng viên đã nhắn tin (GET)
       /* public IActionResult Messages()
        {
            ViewData["Title"] = "Quản lý tin nhắn";
            return View();
        }*/
    }
}