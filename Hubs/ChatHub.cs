using Microsoft.AspNetCore.SignalR;
using JobPortal.Data;
using JobPortal.Models;

namespace JobPortal.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _context;

        public ChatHub(ApplicationDbContext context)
        {
            _context = context;
        }

        // Hàm xử lý gửi tin nhắn
        public async Task SendMessage(string senderId, string receiverId, string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            // 1. Lưu tin nhắn vào Cơ sở dữ liệu
            var chatMsg = new ChatMessage
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Content = message,
                Timestamp = DateTime.Now,
                IsRead = false
            };

            _context.ChatMessages.Add(chatMsg);
            await _context.SaveChangesAsync();

            // 2. Bắn tin nhắn real-time về cho người gửi và người nhận
            // Sử dụng Clients.All tạm thời hoặc tối ưu hơn là gửi theo User Id nhận dạng được cấu hình
            await Clients.All.SendAsync("ReceiveMessage", senderId, receiverId, message, chatMsg.Timestamp.ToString("HH:mm"));
        }
    }
}