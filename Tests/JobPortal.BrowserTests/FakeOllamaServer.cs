using System.Net;
using System.Text.Json;

sealed class FakeOllamaServer : IAsyncDisposable
{
    private readonly HttpListener _listener = new();
    private readonly CancellationTokenSource _stop = new();
    private readonly Task _loop;
    public string Address { get; }
    public bool FailNext;
    public FakeOllamaServer(int port)
    {
        Address = $"http://127.0.0.1:{port}/";
        _listener.Prefixes.Add(Address); _listener.Start(); _loop = NhanYeuCauAsync();
    }
    private async Task NhanYeuCauAsync()
    {
        while (!_stop.IsCancellationRequested)
        {
            HttpListenerContext request;
            try { request = await _listener.GetContextAsync().WaitAsync(_stop.Token); }
            catch (Exception) when (_stop.IsCancellationRequested) { return; }
            try
            {
                using var body = await JsonDocument.ParseAsync(request.Request.InputStream);
                using var context = JsonDocument.Parse(body.RootElement.GetProperty("messages")[1].GetProperty("content").GetString()!);
                var number = context.RootElement.GetProperty("turn").GetInt32();
                var grading = context.RootElement.GetProperty("task").GetString() == "evaluate";
                var evidence = grading ? body.RootElement.GetProperty("format").GetProperty("properties")
                    .GetProperty("evidence").GetProperty("enum")[0].GetString()! : "";
                var failed = FailNext; FailNext = false;
                request.Response.StatusCode = failed ? 503 : 200;
                request.Response.ContentType = "application/json";
                await JsonSerializer.SerializeAsync(request.Response.OutputStream, new
                {
                    done = true, done_reason = "stop", message = new { content = JsonSerializer.Serialize(new
                    {
                        questionNumber = number, evidence,
                        nextQuestion = number > 5 ? "" : $"Câu {number}: Bạn kiểm tra hiệu năng truy vấn SQL trong dự án .NET như thế nào?",
                        nextExpectedPoints = number > 5 ? "" : "Đo thời gian, xem kế hoạch thực thi, cân nhắc chỉ mục.", score = 78,
                        feedback = grading ? "Bạn đã nêu cách đo thời gian truy vấn." : "",
                        improvement = grading ? "Bổ sung ví dụ số liệu trước và sau tối ưu." : ""
                    }) }
                });
            }
            finally { request.Response.Close(); }
        }
    }
    public async ValueTask DisposeAsync()
    {
        _stop.Cancel(); _listener.Close(); await _loop; _stop.Dispose();
    }
}
