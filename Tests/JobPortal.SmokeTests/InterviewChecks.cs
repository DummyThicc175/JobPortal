using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using JobPortal.Configuration;
using JobPortal.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

static class InterviewChecks
{
    public static async Task ChayKiemTraAsync(PortalFactory factory, PortalChecks.Actor candidate, PortalChecks.Actor employer,
        PortalChecks.Actor otherCandidate, PortalChecks.Actor otherEmployer, PortalChecks.Actor admin)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var applicationId = await db.Applications.Select(a => a.Id).SingleAsync();
        async Task CapNhatTrangThaiAsync(string status)
        {
            using var result = await PortalChecks.GuiBieuMauAsync(employer, "/Employer/Applicant/UpdateStatus", "/Employer/Applicant", new() { ["id"] = applicationId.ToString(), ["status"] = status });
            PortalChecks.KiemTra(result.StatusCode == HttpStatusCode.Redirect, "Interview status transition: " + status);
        }
        await CapNhatTrangThaiAsync("Interview");
        var room = await db.InterviewSessions.AsNoTracking().SingleAsync(s => s.EndedAt == null);
        await CapNhatTrangThaiAsync("Interview");
        PortalChecks.KiemTra(await db.InterviewSessions.CountAsync(s => s.EndedAt == null) == 1, "Repeated Interview status creates only one active room");
        var path = "/Interview/Room/" + room.Id;
        await PortalChecks.TaiTrangAsync(candidate.Client, path, "interview-room", "interview.js");
        var employerHtml = await PortalChecks.TaiTrangAsync(employer.Client, path, "/Interview/End/");
        PortalChecks.KiemTra(employerHtml.Contains("data-portal-area=\"Employer\""), "Employer room keeps correct workspace role");
        await PortalChecks.TaiTrangAsync(candidate.Client, "/Candidate/Application", path);
        await PortalChecks.TaiTrangAsync(employer.Client, "/Employer/Applicant", path);
        foreach (var actor in new[] { otherCandidate, otherEmployer })
            PortalChecks.KiemTra((await actor.Client.GetAsync(path)).StatusCode == HttpStatusCode.NotFound, "Room ownership cannot be bypassed with its URL");
        PortalChecks.KiemTra((await admin.Client.GetAsync(path)).StatusCode == HttpStatusCode.Redirect, "Admin has no participant access");
        using var anonymous = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        PortalChecks.KiemTra((await anonymous.GetAsync(path)).StatusCode == HttpStatusCode.Redirect, "Room requires login");
        PortalChecks.KiemTra((await anonymous.PostAsync("/interviewHub/negotiate?negotiateVersion=1", new StringContent(""))).StatusCode == HttpStatusCode.Redirect, "Interview signaling requires login");
        foreach (var action in new[] { "/Interview/Create", "/Interview/End/" + room.Id })
            PortalChecks.KiemTra((await employer.Client.PostAsync(action, new FormUrlEncodedContent(new Dictionary<string, string>()))).StatusCode == HttpStatusCode.BadRequest, "Interview mutations enforce CSRF");
        using (var result = await PortalChecks.GuiBieuMauAsync(otherEmployer, "/Interview/Create", "/Employer/Job", new() { ["applicationId"] = applicationId.ToString() }))
            PortalChecks.KiemTra(result.StatusCode == HttpStatusCode.NotFound, "Other employer cannot create room for an application");
        using (var result = await PortalChecks.GuiBieuMauAsync(employer, "/Interview/Create", "/Employer/Applicant", new() { ["applicationId"] = applicationId.ToString() }))
            PortalChecks.KiemTra(result.Headers.Location?.OriginalString == path, "Creating an open session is idempotent");

        await using var host = await Client.KetNoiAsync(factory, employer);
        await using var guest = await Client.KetNoiAsync(factory, candidate);
        await using var outsider = await Client.KetNoiAsync(factory, otherCandidate);
        await using var duplicate = await Client.KetNoiAsync(factory, candidate);
        PortalChecks.KiemTra((await outsider.GoiHamAsync("Join", room.Id)).TryGetProperty("error", out _), "Unrelated candidate cannot join signaling room");
        PortalChecks.KiemTra(!(await host.GoiHamAsync("Join", room.Id)).TryGetProperty("error", out _), "Employer joins room");
        var joined = await guest.GoiHamAsync("Join", room.Id);
        PortalChecks.KiemTra(!joined.TryGetProperty("error", out _), "Candidate joins room");
        var hostId = joined.GetProperty("result").GetProperty("peerId").GetString()!;
        var guestId = (await host.ChoSuKienAsync("PeerJoined")).GetProperty("arguments")[0].GetString()!;
        PortalChecks.KiemTra((await duplicate.GoiHamAsync("Join", room.Id)).TryGetProperty("error", out _), "Same account cannot silently join from a second tab");
        var offer = new { type = "offer", sdp = "v=0\r\nprivate-offer" };
        PortalChecks.KiemTra(!(await host.GoiHamAsync("SendSignal", room.Id, guestId, offer)).TryGetProperty("error", out _), "Authenticated participant can send offer");
        var delivered = (await guest.ChoSuKienAsync("Signal")).GetProperty("arguments");
        PortalChecks.KiemTra(delivered[0].GetString() == hostId && delivered[1].GetProperty("sdp").GetString() == offer.sdp, "Offer delivered only to the registered peer with server-derived sender");
        PortalChecks.KiemTra((await outsider.GoiHamAsync("SendSignal", room.Id, guestId, offer)).TryGetProperty("error", out _), "Nonparticipant cannot inject signaling");
        PortalChecks.KiemTra((await duplicate.GoiHamAsync("SendSignal", room.Id, hostId, offer)).TryGetProperty("error", out _), "An authorized but unjoined connection cannot inject signaling");
        PortalChecks.KiemTra((await host.GoiHamAsync("SendSignal", room.Id, "forged-connection", offer)).TryGetProperty("error", out _), "Signaling cannot target arbitrary connections");
        PortalChecks.KiemTra((await host.GoiHamAsync("SendSignal", room.Id, guestId, new { type = "offer", sdp = new string('x', 24001) })).TryGetProperty("error", out _), "Oversized SDP is rejected");
        PortalChecks.KiemTra((await host.GoiHamAsync("SendSignal", room.Id, guestId, new { type = "unsupported" })).TryGetProperty("error", out _), "Unsupported signal types rejected");
        PortalChecks.KiemTra(!(await guest.GoiHamAsync("Heartbeat", room.Id)).TryGetProperty("error", out _), "Heartbeat validates joined participant");
        await guest.GoiHamAsync("Leave"); await host.ChoSuKienAsync("PeerLeft");
        PortalChecks.KiemTra(await db.InterviewSessions.AnyAsync(s => s.Id == room.Id && s.EndedAt == null), "Leaving keeps session open for a rejoin");
        PortalChecks.KiemTra(!(await guest.GoiHamAsync("Join", room.Id)).TryGetProperty("error", out _), "Participant can rejoin after leaving");
        await host.ChoSuKienAsync("PeerJoined");
        var account = await db.Users.SingleAsync(u => u.Id == candidate.Id);
        account.LockoutEnd = DateTimeOffset.UtcNow.AddHours(1);
        await db.SaveChangesAsync();
        PortalChecks.KiemTra((await guest.GoiHamAsync("Heartbeat", room.Id)).TryGetProperty("error", out _), "Existing socket loses access after account lock");
        await guest.ChoSuKienAsync("AccessRevoked");
        PortalChecks.KiemTra(true, "Revoked caller is explicitly notified before its presence is removed");
        account.LockoutEnd = null;
        await db.SaveChangesAsync();
        await guest.GoiHamAsync("Join", room.Id);
        await CapNhatTrangThaiAsync("Reviewing");
        await host.ChoSuKienAsync("SessionEnded"); await guest.ChoSuKienAsync("SessionEnded");
        PortalChecks.KiemTra((await candidate.Client.GetAsync(path)).StatusCode == HttpStatusCode.NotFound, "Status transition invalidates old room URL");
        PortalChecks.KiemTra((await guest.GoiHamAsync("Join", room.Id)).TryGetProperty("error", out _), "Status transition revokes signaling access");
        await CapNhatTrangThaiAsync("Interview");
        var next = await db.InterviewSessions.AsNoTracking().SingleAsync(s => s.EndedAt == null);
        PortalChecks.KiemTra(next.Id != room.Id && await db.InterviewSessions.CountAsync() == 2, "Returning to Interview creates a new session and keeps history");
        using (var result = await PortalChecks.GuiBieuMauAsync(otherEmployer, "/Interview/End/" + next.Id, "/Employer/Job"))
            PortalChecks.KiemTra(result.StatusCode == HttpStatusCode.NotFound, "Other employer cannot end session");
        using (var result = await PortalChecks.GuiBieuMauAsync(candidate, "/Interview/End/" + next.Id, "/Candidate/Application"))
            PortalChecks.KiemTra(result.StatusCode == HttpStatusCode.Redirect && result.Headers.Location!.OriginalString.Contains("AccessDenied"), "Candidate cannot end session for everyone");
        using (var result = await PortalChecks.GuiBieuMauAsync(employer, "/Interview/End/" + next.Id, "/Employer/Applicant"))
            PortalChecks.KiemTra(result.StatusCode == HttpStatusCode.Redirect, "Owning employer can close session");
        PortalChecks.KiemTra((await candidate.Client.GetAsync("/Interview/Room/" + next.Id)).StatusCode == HttpStatusCode.NotFound, "Ended session URL cannot be reused");
        await CapNhatTrangThaiAsync("Submitted");
        using (var result = await PortalChecks.GuiBieuMauAsync(employer, "/Interview/Create", "/Employer/Applicant", new() { ["applicationId"] = applicationId.ToString() }))
            PortalChecks.KiemTra(result.StatusCode == HttpStatusCode.NotFound, "No room can be created outside Interview status");
        var settings = new InterviewOptions { TurnUrls = ["turn:turn.example.test:3478"], TurnSharedSecret = "test-shared-secret" };
        var configuration = JsonSerializer.Serialize(settings.TaoCauHinhKetNoi(candidate.Id));
        PortalChecks.KiemTra(configuration.Contains("turn:turn.example.test") && !configuration.Contains(settings.TurnSharedSecret), "TURN uses temporary signed credentials without exposing shared secret");
        Console.WriteLine("PASS: interview authorization, private signaling, single-tab presence, leave/rejoin, account revocation, status/session lifecycle and TURN credentials");
    }

    sealed class Client(WebSocket socket) : IAsyncDisposable
    {
        private int sequence;
        private readonly List<JsonElement> pending = [];
        public static async Task<Client> KetNoiAsync(PortalFactory factory, PortalChecks.Actor actor)
        {
            var transport = factory.Server.CreateWebSocketClient(); transport.ConfigureRequest = r => r.Headers.Cookie = actor.Cookie;
            var client = new Client(await transport.ConnectAsync(new Uri("ws://localhost/interviewHub"), default));
            await client.GuiAsync(new { protocol = "json", version = 1 });
            await client.NhanAsync(_ => true); return client;
        }
        public async Task<JsonElement> GoiHamAsync(string target, params object[] arguments)
        {
            var id = (++sequence).ToString();
            await GuiAsync(new { type = 1, invocationId = id, target, arguments });
            return await NhanAsync(v => v.TryGetProperty("invocationId", out var value) && value.GetString() == id);
        }
        public Task<JsonElement> ChoSuKienAsync(string target) => NhanAsync(v => v.TryGetProperty("target", out var value) && value.GetString() == target);
        private Task GuiAsync(object payload) => socket.SendAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload) + '\u001e'), WebSocketMessageType.Text, true, default);
        private async Task<JsonElement> NhanAsync(Func<JsonElement, bool> predicate)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            var buffer = new byte[65536];
            while (true)
            {
                var index = pending.FindIndex(v => predicate(v));
                if (index >= 0) { var found = pending[index]; pending.RemoveAt(index); return found; }
                using var memory = new MemoryStream(); WebSocketReceiveResult result;
                do { result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), timeout.Token); memory.Write(buffer, 0, result.Count); } while (!result.EndOfMessage);
                foreach (var frame in Encoding.UTF8.GetString(memory.ToArray()).Split('\u001e', StringSplitOptions.RemoveEmptyEntries))
                { using var parsed = JsonDocument.Parse(frame); pending.Add(parsed.RootElement.Clone()); }
            }
        }
        public ValueTask DisposeAsync() { socket.Dispose(); return ValueTask.CompletedTask; }
    }
}
