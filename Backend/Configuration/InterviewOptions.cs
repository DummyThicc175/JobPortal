using System.Security.Cryptography;
using System.Text;

namespace JobPortal.Configuration;

public sealed class InterviewOptions
{
    public List<string> StunUrls { get; set; } = ["stun:stun.l.google.com:19302"];
    public List<string> TurnUrls { get; set; } = [];
    // Shared secret stays on the server. Coturn REST credentials expire after two hours.
    public string TurnSharedSecret { get; set; } = "";
    public bool ForceRelay { get; set; }

    public object TaoCauHinhKetNoi(string userId)
    {
        var servers = new List<object>();
        var stun = StunUrls.Where(u => u.StartsWith("stun:", StringComparison.OrdinalIgnoreCase) || u.StartsWith("stuns:", StringComparison.OrdinalIgnoreCase)).ToArray();
        if (stun.Length > 0) servers.Add(new { urls = stun });
        var turn = TurnUrls.Where(u => u.StartsWith("turn:", StringComparison.OrdinalIgnoreCase) || u.StartsWith("turns:", StringComparison.OrdinalIgnoreCase)).ToArray();
        if (turn.Length > 0 && !string.IsNullOrWhiteSpace(TurnSharedSecret))
        {
            var username = $"{DateTimeOffset.UtcNow.AddHours(2).ToUnixTimeSeconds()}:{userId}";
            var credential = Convert.ToBase64String(HMACSHA1.HashData(Encoding.UTF8.GetBytes(TurnSharedSecret), Encoding.UTF8.GetBytes(username)));
            servers.Add(new { urls = turn, username, credential });
        }
        return new { iceServers = servers, iceTransportPolicy = ForceRelay ? "relay" : "all" };
    }
}
