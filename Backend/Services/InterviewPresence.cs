namespace JobPortal.Services;

// One application instance owns the signaling connections. Scale-out needs shared presence.
public sealed class InterviewPresence
{
    private readonly object gate = new();
    private readonly Dictionary<string, Member> members = [];
    public sealed record Member(Guid RoomId, string UserId, string ConnectionId)
    {
        public DateTime SeenAt { get; set; } = DateTime.UtcNow;
        public DateTime WindowAt { get; set; } = DateTime.UtcNow;
        public int Signals { get; set; }
    }

    public (bool Accepted, string? Peer) ThamGia(Guid roomId, string userId, string connectionId)
    {
        lock (gate)
        {
            DonKetNoiHetHan();
            if (members.TryGetValue(connectionId, out var current))
                return (current.RoomId == roomId, TimNguoiConLai(current)?.ConnectionId);
            if (members.Values.Any(m => m.RoomId == roomId && m.UserId == userId)) return (false, null);
            if (members.Values.Count(m => m.RoomId == roomId) >= 2) return (false, null);
            var member = new Member(roomId, userId, connectionId);
            members.Add(connectionId, member);
            return (true, TimNguoiConLai(member)?.ConnectionId);
        }
    }

    public Member? TimKetNoi(string connectionId)
    {
        lock (gate) { DonKetNoiHetHan(); return members.GetValueOrDefault(connectionId); }
    }

    public string? CapNhatHoatDong(string connectionId, Guid roomId, bool signal = false)
    {
        lock (gate)
        {
            DonKetNoiHetHan();
            if (!members.TryGetValue(connectionId, out var member) || member.RoomId != roomId)
                throw new InvalidOperationException("Bạn chưa tham gia phòng hoặc phiên kết nối đã hết hạn.");
            member.SeenAt = DateTime.UtcNow;
            if (signal)
            {
                if (member.WindowAt < DateTime.UtcNow.AddSeconds(-10)) { member.WindowAt = DateTime.UtcNow; member.Signals = 0; }
                if (++member.Signals > 160) throw new InvalidOperationException("Tín hiệu gửi quá nhanh. Vui lòng vào lại phòng.");
            }
            return TimNguoiConLai(member)?.ConnectionId;
        }
    }

    public string? RoiPhong(string connectionId)
    {
        lock (gate)
        {
            if (!members.Remove(connectionId, out var member)) return null;
            return TimNguoiConLai(member)?.ConnectionId;
        }
    }

    public string[] DongPhong(Guid roomId)
    {
        lock (gate)
        {
            var connections = members.Values.Where(m => m.RoomId == roomId).Select(m => m.ConnectionId).ToArray();
            foreach (var connection in connections) members.Remove(connection);
            return connections;
        }
    }

    private Member? TimNguoiConLai(Member member) => members.Values.FirstOrDefault(m => m.RoomId == member.RoomId && m.ConnectionId != member.ConnectionId);
    private void DonKetNoiHetHan()
    {
        foreach (var stale in members.Values.Where(m => m.SeenAt < DateTime.UtcNow.AddSeconds(-70)).ToArray()) members.Remove(stale.ConnectionId);
    }
}
