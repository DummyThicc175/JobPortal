using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using JobPortal.Data;
using JobPortal.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

static class ChatChecks
{
    public static async Task ChayKiemTraAsync(PortalFactory factory, PortalChecks.Actor candidate, PortalChecks.Actor employer, PortalChecks.Actor outsider)
    {
        async Task<WebSocket> KetNoiAsync(PortalChecks.Actor actor)
        {
            var client = factory.Server.CreateWebSocketClient();
            client.ConfigureRequest = request => request.Headers.Cookie = actor.Cookie;
            var socket = await client.ConnectAsync(new Uri("ws://localhost/chatHub"), default);
            await GuiAsync(socket, new { protocol = "json", version = 1 });
            var handshake = await NhanAsync(socket, _ => true);
            PortalChecks.KiemTra(!handshake.TryGetProperty("error", out _), "SignalR handshake succeeds for authenticated actor");
            return socket;
        }
        using var sender = await KetNoiAsync(candidate);
        using var receiver = await KetNoiAsync(employer);
        using var unrelated = await KetNoiAsync(outsider);
        var message = "<img src=x onerror=alert(1)> real text";
        await GuiAsync(sender, new { type = 1, invocationId = "send", target = "SendMessage", arguments = new object[] { employer.Id, message } });
        var delivered = await NhanAsync(receiver, value => value.TryGetProperty("target", out var target) && target.GetString() == "ReceiveMessage");
        var args = delivered.GetProperty("arguments");
        PortalChecks.KiemTra(args[0].GetString() == candidate.Id && args[1].GetString() == employer.Id && args[2].GetString() == message, "SignalR derives sender from authenticated identity and delivers exact text");
        var completed = await NhanAsync(sender, value => value.TryGetProperty("invocationId", out var id) && id.GetString() == "send");
        PortalChecks.KiemTra(!completed.TryGetProperty("error", out _), "Authorized chat send completes");
        using (var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(300)))
        {
            try { await NhanAsync(unrelated, value => value.TryGetProperty("target", out _), timeout.Token); PortalChecks.KiemTra(false, "Unrelated connection received private message"); }
            catch (OperationCanceledException) { PortalChecks.KiemTra(true, "Private message is not broadcast to other users"); }
            catch (WebSocketException) when (timeout.IsCancellationRequested) { PortalChecks.KiemTra(true, "Private message is not broadcast to other users"); }
        }
        await GuiAsync(sender, new { type = 1, invocationId = "forged", target = "SendMessage", arguments = new object[] { outsider.Id, employer.Id, "fake sender" } });
        var forged = await NhanAsync(sender, value => value.TryGetProperty("invocationId", out var id) && id.GetString() == "forged");
        PortalChecks.KiemTra(forged.TryGetProperty("error", out _), "Old client-supplied sender signature is rejected");
        await GuiAsync(sender, new { type = 1, invocationId = "wrong-partner", target = "SendMessage", arguments = new object[] { outsider.Id, "unauthorized" } });
        var rejected = await NhanAsync(sender, value => value.TryGetProperty("invocationId", out var id) && id.GetString() == "wrong-partner");
        PortalChecks.KiemTra(rejected.TryGetProperty("error", out _), "Users cannot chat without an application relationship");
        await GuiAsync(sender, new { type = 1, invocationId = "long", target = "SendMessage", arguments = new object[] { employer.Id, new string('x', 2001) } });
        var tooLong = await NhanAsync(sender, value => value.TryGetProperty("invocationId", out var id) && id.GetString() == "long");
        PortalChecks.KiemTra(tooLong.TryGetProperty("error", out _), "Message length enforced on server");
        using (var scope = factory.Services.CreateScope()) PortalChecks.KiemTra(await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().ChatMessages.CountAsync() == 1, "Rejected chat sends do not reach storage");
        PortalChecks.KiemTra((await outsider.Client.GetAsync("/Chat/GetChatHistory?partnerId=" + candidate.Id)).StatusCode == HttpStatusCode.NotFound, "History API enforces conversation access");
        await PortalChecks.TaiTrangAsync(employer.Client, "/Chat/GetChatHistory?partnerId=" + candidate.Id);
        using (var scope = factory.Services.CreateScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            PortalChecks.KiemTra((await database.ChatMessages.SingleAsync()).IsRead, "Loading history updates read state");
            for (var i = 0; i < 60; i++) database.ChatMessages.Add(new ChatMessage { SenderId = candidate.Id, ReceiverId = employer.Id, Content = "history " + i, Timestamp = DateTime.UtcNow });
            await database.SaveChangesAsync();
        }
        using var history = await employer.Client.GetAsync("/Chat/GetChatHistory?partnerId=" + candidate.Id);
        using var parsed = JsonDocument.Parse(await history.Content.ReadAsStringAsync());
        PortalChecks.KiemTra(parsed.RootElement.GetArrayLength() == 50 && history.Headers.GetValues("X-Has-More").Single() == "true", "History API uses bounded pages");
        var cursor = history.Headers.GetValues("X-Next-Before").Single();
        using var older = await employer.Client.GetAsync($"/Chat/GetChatHistory?partnerId={candidate.Id}&beforeId={cursor}");
        using var parsedOlder = JsonDocument.Parse(await older.Content.ReadAsStringAsync());
        PortalChecks.KiemTra(parsedOlder.RootElement.GetArrayLength() == 11, "History cursor retrieves older messages without overlap");
    }

    private static Task GuiAsync(WebSocket socket, object payload) => socket.SendAsync(
        Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload) + '\u001e'), WebSocketMessageType.Text, true, default);

    private static async Task<JsonElement> NhanAsync(WebSocket socket, Func<JsonElement, bool> predicate, CancellationToken token = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token); timeout.CancelAfter(TimeSpan.FromSeconds(5));
        var buffer = new byte[32768];
        while (true)
        {
            using var data = new MemoryStream();
            WebSocketReceiveResult result;
            do
            {
                result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), timeout.Token);
                if (result.MessageType == WebSocketMessageType.Close) throw new InvalidOperationException("SignalR connection closed unexpectedly");
                data.Write(buffer, 0, result.Count);
            } while (!result.EndOfMessage);
            foreach (var frame in Encoding.UTF8.GetString(data.ToArray()).Split('\u001e', StringSplitOptions.RemoveEmptyEntries))
            {
                using var parsed = JsonDocument.Parse(frame);
                if (predicate(parsed.RootElement)) return parsed.RootElement.Clone();
            }
        }
    }
}
