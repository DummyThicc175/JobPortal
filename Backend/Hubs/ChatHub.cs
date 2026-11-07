using System.Security.Claims;
using JobPortal.Data;
using JobPortal.Models;
using JobPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace JobPortal.Hubs;

[Authorize(Roles = "Candidate,Employer")]
public sealed class ChatHub(ApplicationDbContext database, ChatAccessService access, ChatMessageThrottle throttle) : Hub
{
    public async Task SendMessage(string receiverId, string message)
    {
        var senderId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (senderId == null) throw new HubException("Bạn cần đăng nhập.");
        message = message?.Trim() ?? "";
        if (message.Length is < 1 or > 2000) throw new HubException("Tin nhắn phải có từ 1 đến 2000 ký tự.");
        if (!await access.CanChatAsync(senderId, receiverId, Context.ConnectionAborted)) throw new HubException("Không có quyền gửi tin trong cuộc trò chuyện này.");
        if (!throttle.TrySend(senderId)) throw new HubException("Bạn gửi tin quá nhanh. Vui lòng chờ vài giây.");
        var chat = new ChatMessage { SenderId = senderId, ReceiverId = receiverId, Content = message, Timestamp = DateTime.UtcNow };
        database.ChatMessages.Add(chat);
        await database.SaveChangesAsync(Context.ConnectionAborted);
        await Clients.Users(senderId, receiverId).SendAsync("ReceiveMessage", senderId, receiverId, message,
            chat.Timestamp.ToString("O"), chat.Id, Context.ConnectionAborted);
    }
}
