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
    [HubMethodName("Join")]
    public async Task<object> ThamGiaPhong(Guid roomId)
    {
        await KiemTraQuyenAsync(roomId);
        var result = presence.ThamGia(roomId, Context.UserIdentifier!, Context.ConnectionId);
        if (!result.Accepted) throw new HubException("Bạn đã mở phòng này ở cửa sổ khác. Hãy rời phòng ở cửa sổ đó trước.");
        await database.InterviewSessions.Where(s => s.Id == roomId && s.StartedAt == null && s.EndedAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(s => s.StartedAt, DateTime.UtcNow), Context.ConnectionAborted);
        if (result.Peer != null) await Clients.Client(result.Peer).SendAsync("PeerJoined", Context.ConnectionId);
        return new { peerId = result.Peer };
    }

    [HubMethodName("SendSignal")]
    public async Task GuiTinHieu(Guid roomId, string peerId, InterviewSignal signal)
    {
        await KiemTraQuyenAsync(roomId);
        if (!signal.KiemTraHopLe()) throw new HubException("Tín hiệu cuộc gọi không hợp lệ.");
        string? peer;
        try { peer = presence.CapNhatHoatDong(Context.ConnectionId, roomId, signal: true); }
        catch (InvalidOperationException error) { throw new HubException(error.Message); }
        if (peer == null || peer != peerId) throw new HubException("Người còn lại đã rời phòng. Đang chờ kết nối lại.");
        await Clients.Client(peer).SendAsync("Signal", Context.ConnectionId, signal);
    }

    [HubMethodName("Heartbeat")]
    public async Task<object> DuyTriKetNoi(Guid roomId)
    {
        await KiemTraQuyenAsync(roomId);
        try { return new { peerId = presence.CapNhatHoatDong(Context.ConnectionId, roomId) }; }
        catch (InvalidOperationException error) { throw new HubException(error.Message); }
    }

    [HubMethodName("Leave")]
    public Task RoiPhong() => XoaKetNoiAsync();
    public override async Task OnDisconnectedAsync(Exception? exception) { await XoaKetNoiAsync(); await base.OnDisconnectedAsync(exception); }

    private async Task XoaKetNoiAsync()
    {
        var peer = presence.RoiPhong(Context.ConnectionId);
        if (peer != null) await Clients.Client(peer).SendAsync("PeerLeft", Context.ConnectionId);
    }

    private async Task KiemTraQuyenAsync(Guid roomId)
    {
        if (await interviews.LayPhienCoQuyenAsync(roomId, Context.User!, Context.ConnectionAborted) != null) return;
        // Notify the caller before removing presence: a concurrent close may otherwise
        // miss this connection when it takes the list of remaining participants.
        await Clients.Caller.SendAsync("AccessRevoked");
        await XoaKetNoiAsync();
        throw new HubException("Phiên phỏng vấn đã kết thúc hoặc bạn không còn quyền tham gia.");
    }
}

public sealed record InterviewSignal(string Type, string? Sdp = null, string? Candidate = null,
    string? SdpMid = null, int? SdpMLineIndex = null, string? UsernameFragment = null, bool? Mic = null, bool? Camera = null)
{
    [HubMethodName("Valid")]
    public bool KiemTraHopLe() => Type switch
    {
        "offer" or "answer" => Sdp is { Length: > 0 and <= 24000 } && Candidate == null,
        "candidate" => Candidate is { Length: > 0 and <= 2048 } && Sdp == null &&
            SdpMid is { Length: <= 64 } && SdpMLineIndex is >= 0 and <= 32 && (UsernameFragment == null || UsernameFragment.Length <= 256),
        "media" => Mic.HasValue && Camera.HasValue && Sdp == null && Candidate == null,
        _ => false
    };
}
