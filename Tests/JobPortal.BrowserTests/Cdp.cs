using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

sealed class Cdp : IAsyncDisposable
{
    private readonly ClientWebSocket socket = new();
    private int nextId;
    public List<string> Errors { get; } = [];
    public async Task KetNoiAsync(string url) => await socket.ConnectAsync(new Uri(url), default);
    public async Task<JsonElement> GoiLenhAsync(string method, object? args = null)
    {
        var id = ++nextId;
        await socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { id, method, @params = args ?? new { } }))), WebSocketMessageType.Text, true, default);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(70));
        var buffer = new byte[65536];
        while (true)
        {
            using var data = new MemoryStream(); WebSocketReceiveResult result;
            do
            {
                result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), timeout.Token);
                data.Write(buffer, 0, result.Count);
            } while (!result.EndOfMessage);
            using var parsed = JsonDocument.Parse(data.ToArray()); var value = parsed.RootElement;
            if (value.TryGetProperty("method", out var eventName) && eventName.GetString() == "Runtime.exceptionThrown") Errors.Add(value.GetProperty("params").ToString());
            if (!value.TryGetProperty("id", out var responseId) || responseId.GetInt32() != id) continue;
            if (value.TryGetProperty("error", out var error)) throw new InvalidOperationException(method + ": " + error);
            return value.GetProperty("result").Clone();
        }
    }
    public async Task<JsonElement> ThucThiJavaScriptAsync(string expression)
    {
        var result = await GoiLenhAsync("Runtime.evaluate", new { expression, awaitPromise = true, returnByValue = true, userGesture = true });
        if (result.TryGetProperty("exceptionDetails", out var error)) throw new InvalidOperationException(error.ToString());
        return result.GetProperty("result").TryGetProperty("value", out var value) ? value.Clone() : default;
    }
    public async Task ChoDieuKienAsync(string expression, int seconds = 15)
    {
        var stop = DateTime.UtcNow.AddSeconds(seconds);
        while (DateTime.UtcNow < stop)
        {
            try { if ((await ThucThiJavaScriptAsync(expression)).ValueKind == JsonValueKind.True) return; }
            catch (InvalidOperationException) { }
            await Task.Delay(100);
        }
        throw new TimeoutException("Browser condition not met: " + expression);
    }
    public async Task MoTrangAsync(string url)
    {
        url = new Uri(url).AbsoluteUri;
        await GoiLenhAsync("Page.navigate", new { url });
        try { await ChoDieuKienAsync("location.href === " + JsonSerializer.Serialize(url) + " && document.readyState === 'complete'", 30); }
        catch (TimeoutException error) { throw new TimeoutException("Navigation failed: " + await ThucThiJavaScriptAsync("({href:location.href,title:document.title,ready:document.readyState})"), error); }
    }
    public async ValueTask DisposeAsync() { socket.Dispose(); await Task.CompletedTask; }
}
