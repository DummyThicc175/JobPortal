using System.Security.Claims;
using JobPortal.Data;
using JobPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace JobPortal.Hubs;

[Authorize(Roles = "Candidate,Employer")]
public sealed class InterviewHub(InterviewService interviews, InterviewPresence presence, ApplicationDbContext database) : Hub
{
    public async Task<object> Join(Guid roomId)
    {
        await RequireAccess(roomId);
        var result = presence.Join(roomId, Context.UserIdentifier!, Context.ConnectionId);
        if (!result.Accepted) throw new HubException("Bạn đã mở phòng này ở cửa sổ khác. Hãy rời phòng ở cửa sổ đó trước.");
        await database.InterviewSessions.Where(s => s.Id == roomId && s.StartedAt == null && s.EndedAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(s => s.StartedAt, DateTime.UtcNow), Context.ConnectionAborted);
        if (result.Peer != null) await Clients.Client(result.Peer).SendAsync("PeerJoined", Context.ConnectionId);
        return new { peerId = result.Peer };
    }

    public async Task SendSignal(Guid roomId, string peerId, InterviewSignal signal)
    {
        await RequireAccess(roomId);
        if (!signal.Valid()) throw new HubException("Tín hiệu cuộc gọi không hợp lệ.");
        string? peer;
        try { peer = presence.Touch(Context.ConnectionId, roomId, signal: true); }
        catch (InvalidOperationException error) { throw new HubException(error.Message); }
        if (peer == null || peer != peerId) throw new HubException("Người còn lại đã rời phòng. Đang chờ kết nối lại.");
        await Clients.Client(peer).SendAsync("Signal", Context.ConnectionId, signal);
    }

    public async Task<object> Heartbeat(Guid roomId)
    {
        await RequireAccess(roomId);
        try { return new { peerId = presence.Touch(Context.ConnectionId, roomId) }; }
        catch (InvalidOperationException error) { throw new HubException(error.Message); }
    }

    public Task Leave() => RemoveAsync();
    public override async Task OnDisconnectedAsync(Exception? exception) { await RemoveAsync(); await base.OnDisconnectedAsync(exception); }

    private async Task RemoveAsync()
    {
        var peer = presence.Leave(Context.ConnectionId);
        if (peer != null) await Clients.Client(peer).SendAsync("PeerLeft", Context.ConnectionId);
    }

    private async Task RequireAccess(Guid roomId)
    {
        if (await interviews.AuthorizedAsync(roomId, Context.User!, Context.ConnectionAborted) != null) return;
        // Notify the caller before removing presence: a concurrent close may otherwise
        // miss this connection when it takes the list of remaining participants.
        await Clients.Caller.SendAsync("AccessRevoked");
        await RemoveAsync();
        throw new HubException("Phiên phỏng vấn đã kết thúc hoặc bạn không còn quyền tham gia.");
    }
}

public sealed record InterviewSignal(string Type, string? Sdp = null, string? Candidate = null,
    string? SdpMid = null, int? SdpMLineIndex = null, string? UsernameFragment = null, bool? Mic = null, bool? Camera = null)
{
    public bool Valid() => Type switch
    {
        "offer" or "answer" => Sdp is { Length: > 0 and <= 24000 } && Candidate == null,
        "candidate" => Candidate is { Length: > 0 and <= 2048 } && Sdp == null &&
            SdpMid is { Length: <= 64 } && SdpMLineIndex is >= 0 and <= 32 && (UsernameFragment == null || UsernameFragment.Length <= 256),
        "media" => Mic.HasValue && Camera.HasValue && Sdp == null && Candidate == null,
        _ => false
    };
}
