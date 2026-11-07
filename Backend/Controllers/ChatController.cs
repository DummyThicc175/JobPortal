using System.Security.Claims;
using JobPortal.Data;
using JobPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobPortal.Controllers;

[Authorize(Roles = "Candidate,Employer")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ChatController(ApplicationDbContext database, ChatAccessService access) : Controller
{
    [HttpGet]
    public async Task<IActionResult> GetChatPartners(CancellationToken token)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var latest = database.ChatMessages.AsNoTracking().Where(m => m.SenderId == userId || m.ReceiverId == userId)
            .GroupBy(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
            .Select(g => new { PartnerId = g.Key, LastId = g.Max(m => m.Id) });
        var partners = await (from partner in latest
            join user in database.Users on partner.PartnerId equals user.Id
            orderby partner.LastId descending
            select new
            {
                id = user.Id,
                name = database.Candidates.Where(c => c.UserId == user.Id).Select(c => c.FullName).FirstOrDefault()
                    ?? database.Companies.Where(c => c.EmployerId == user.Id).OrderBy(c => c.Id).Select(c => c.Name).FirstOrDefault()
                    ?? user.UserName ?? "Người dùng"
            }).Take(50).ToListAsync(token);
        return Json(partners);
    }

    [HttpGet]
    public async Task<IActionResult> GetChatHistory(string partnerId, int? beforeId, CancellationToken token)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (!await access.CanChatAsync(userId, partnerId, token)) return NotFound();
        var query = database.ChatMessages.AsNoTracking().Where(m =>
            (m.SenderId == userId && m.ReceiverId == partnerId) || (m.SenderId == partnerId && m.ReceiverId == userId));
        if (beforeId.HasValue) query = query.Where(m => m.Id < beforeId.Value);
        var messages = await query.OrderByDescending(m => m.Id).Take(51)
            .Select(m => new { id = m.Id, senderId = m.SenderId, content = m.Content, time = DateTime.SpecifyKind(m.Timestamp, DateTimeKind.Utc), isMe = m.SenderId == userId })
            .ToListAsync(token);
        var hasMore = messages.Count > 50;
        if (hasMore) messages.RemoveAt(50);
        messages.Reverse();
        var ids = messages.Where(m => !m.isMe).Select(m => m.id).ToArray();
        if (ids.Length > 0) await database.ChatMessages.Where(m => ids.Contains(m.Id) && m.ReceiverId == userId && !m.IsRead)
            .ExecuteUpdateAsync(setters => setters.SetProperty(m => m.IsRead, true), token);
        Response.Headers["X-Has-More"] = hasMore ? "true" : "false";
        Response.Headers["X-Next-Before"] = messages.FirstOrDefault()?.id.ToString() ?? "";
        return Json(messages);
    }
}
