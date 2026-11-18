using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using JobPortal.Configuration;
using JobPortal.Models;
using JobPortal.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
try
{
    if (args.Contains("--speech-long"))
    {
        await KiemTraBanGhiDaiAsync(root);
        return;
    }
    if (args.Contains("--speech-vietnamese"))
    {
        await KiemTraTiengVietAsync(root);
        return;
    }
    var options = Options.Create(new AiInterviewOptions());
    using var handler = new GhiNhanPhanHoi();
    using var http = new HttpClient(handler) { BaseAddress = new Uri(options.Value.BaseUrl), Timeout = Timeout.InfiniteTimeSpan };
    var ai = new OllamaInterviewClient(http, options);
    if (args.Contains("--grading-regression") || args.Contains("--answer-regression") || args.Contains("--student-answer-regression"))
    {
        var regression = new AiInterviewSession
        {
            JobTitle = "Lập trình viên Backend .NET", CategoryName = "Công nghệ thông tin",
            JobContext = "ASP.NET Core, Entity Framework Core, SQL Server."
        };
        regression.Turns.Add(new AiInterviewTurn
        {
            Number = 1,
            Question = "Hãy mô tả một dự án hoặc kinh nghiệm cá nhân liên quan đến vị trí ứng tuyển.",
            ExpectedPoints = "Mô tả dự án hoặc kinh nghiệm; phần việc cá nhân; công cụ hoặc phương pháp đã dùng; kết quả đạt được."
        });
        var answer = "mình từng làm 1 dự án về một website phỏng vấn";
        if (args.Contains("--student-answer-regression"))
        {
            answer = "Mình chỉ là sinh viên thôi";
            // This is context for generating the NEXT question, never for grading the project question.
            regression.JobContext = "Kiểm tra quyền truy cập giữa Candidate, Employer và Admin trong hệ thống tuyển dụng web.";
        }
        if (args.Contains("--answer-regression"))
        {
            regression.Turns.Clear();
            regression.Turns.Add(new AiInterviewTurn { Number = 1, Question = "Hãy mô tả dự án bạn từng làm.", Answer = "Một website phỏng vấn.", AnsweredAt = DateTime.UtcNow });
            regression.Turns.Add(new AiInterviewTurn
            {
                Number = 2,
                Question = "Bạn phân quyền truy cập theo vai trò trong ASP.NET Core Identity như thế nào?",
                ExpectedPoints = "Gán vai trò cho tài khoản; kiểm tra quyền tại Backend; giới hạn thao tác đúng vai trò; ví dụ Candidate, Employer, Admin."
            });
            answer = "vì nó sẽ phân luồng ra cho mỗi chức năng là một bộ phận khác nhau một chức vụ khác nhau và khi đó người ở chức vụ này không phải tiếp cận chức vụ khác hoặc chức vụ khác không phải tiếp cận chức vụ này.";
        }
        var started = Stopwatch.StartNew();
        AiInterviewGeneration result;
        try { result = await ai.TaoLuotAsync(regression, answer, default); }
        catch { HienThiChanDoanModel(handler.LastResponse, answer); throw; }
        if (result.Feedback.Contains("Entity Framework", StringComparison.OrdinalIgnoreCase) ||
            result.Feedback.Contains("EF Core", StringComparison.OrdinalIgnoreCase) || result.Score is < 0 or > 100)
            throw new InvalidOperationException("Feedback drifted from the current project-description question.");
        if (args.Contains("--student-answer-regression"))
        {
            var evaluation = result.Feedback + " " + result.Improvement;
            if (evaluation.Contains("quyền truy cập", StringComparison.OrdinalIgnoreCase) ||
                evaluation.Contains("phân quyền", StringComparison.OrdinalIgnoreCase) ||
                (!evaluation.Contains("dự án", StringComparison.OrdinalIgnoreCase) && !evaluation.Contains("kinh nghiệm", StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Feedback for the student answer drifted from the project rubric to the next question's access-control topic.");
            Console.WriteLine("PASS: student answer is evaluated against the project rubric, without access-control feedback from job context.");
        }
        Console.WriteLine($"PASS: isolated real grading and next-question generation ({started.Elapsed.TotalSeconds:F1}s), score={result.Score}.");
        return;
    }
    var session = new AiInterviewSession
    {
        JobTitle = "Lập trình viên Backend .NET", CategoryName = "Công nghệ thông tin",
        JobContext = "Phát triển API với ASP.NET Core, thiết kế SQL Server, kiểm thử và tối ưu truy vấn. Cấp độ fresher.",
        HasCvContext = true, CvContext = "Ứng viên mới tốt nghiệp CNTT, làm đồ án tuyển dụng với C#, EF Core, SQL Server và SignalR."
    };
    var clock = Stopwatch.StartNew();
    AiInterviewGeneration first;
    try { first = await ai.TaoLuotAsync(session, null, default); }
    catch { Console.Error.WriteLine("Synthetic first response: " + handler.LastResponse); throw; }
    if (string.IsNullOrWhiteSpace(first.Question) || string.IsNullOrWhiteSpace(first.ExpectedPoints)) throw new InvalidOperationException("Real AI did not generate a valid question.");
    Console.WriteLine($"PASS: real Ollama first question ({clock.Elapsed.TotalSeconds:F1}s): {first.Question}");
    session.Turns.Add(new AiInterviewTurn { Number = 1, Question = first.Question, ExpectedPoints = first.ExpectedPoints });
    clock.Restart();
    AiInterviewGeneration next;
    try { next = await ai.TaoLuotAsync(session, "Em làm Backend đồ án tuyển dụng bằng ASP.NET Core MVC, dùng EF Core với SQL Server. Em viết controller theo chức năng, kiểm tra quyền bằng Identity, dùng truy vấn bất đồng bộ và AsNoTracking khi chỉ đọc dữ liệu.", default); }
    catch { Console.Error.WriteLine("Synthetic model response: " + handler.LastResponse); throw; }
    if (next.Score is < 0 or > 100 || next.Question == first.Question || string.IsNullOrWhiteSpace(next.Feedback)) throw new InvalidOperationException("Real AI returned an invalid follow-up evaluation.");
    Console.WriteLine($"PASS: real Ollama evaluation/follow-up ({clock.Elapsed.TotalSeconds:F1}s), score={next.Score}: {next.Question}");
    await KiemTraTiengVietAsync(root);
}
catch (Exception ex) { Console.Error.WriteLine(ex); Environment.ExitCode = 1; }

static void HienThiChanDoanModel(string response, string answer)
{
    if (string.IsNullOrWhiteSpace(response)) { Console.Error.WriteLine("No model response received."); return; }
    try
    {
        using var outer = JsonDocument.Parse(response);
        var root = outer.RootElement;
        var content = root.GetProperty("message").GetProperty("content").GetString() ?? "";
        Console.Error.WriteLine($"Model metadata: reason={root.GetProperty("done_reason").GetString()}, contentLength={content.Length}");
        using var result = JsonDocument.Parse(content);
        foreach (var field in new[] { "evidence", "feedback", "improvement", "nextQuestion", "nextExpectedPoints" })
            if (result.RootElement.TryGetProperty(field, out var value))
                Console.Error.WriteLine($"{field}: type={value.ValueKind}, length={(value.ValueKind == JsonValueKind.String ? value.GetString()?.Length : 0)}");
        if (result.RootElement.TryGetProperty("evidence", out var quote) && quote.ValueKind == JsonValueKind.String)
            Console.Error.WriteLine("Evidence is exact substring: " + answer.Contains(quote.GetString() ?? "", StringComparison.Ordinal));
    }
    catch (JsonException) { Console.Error.WriteLine("Model returned incomplete JSON."); }
}

static async Task KiemTraTiengVietAsync(string root)
{
    var directory = Path.Combine(root, "Backend", "Storage", "AI", "speech", "fixtures");
    using var fixtures = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(directory, "references.json")));
    var options = Options.Create(new AiInterviewOptions());
    using var runtime = new AiInterviewRuntime();
    var speech = new InterviewSpeechService(new MoiTruongKiemTra(root), options, runtime, NullLogger<InterviewSpeechService>.Instance);
    foreach (var fixture in fixtures.RootElement.EnumerateArray())
    {
        await using var wav = File.OpenRead(Path.Combine(directory, fixture.GetProperty("file").GetString()!));
        var started = Stopwatch.StartNew();
        var text = await speech.ChuyenThanhVanBanAsync(wav, default);
        var reference = fixture.GetProperty("reference").GetString()!;
        var error = TinhTyLeSaiTu(reference, text);
        if (text.Length < 10 || error > .5) throw new InvalidOperationException($"Vietnamese fixture regression, token error={error:P1}: {text}");
        Console.WriteLine($"PASS: PhoWhisper-small worker, {started.Elapsed.TotalSeconds:F1}s, whitespace-token error={error:P1}\nReference: {reference}\nActual: {text}");
    }
}

static double TinhTyLeSaiTu(string reference, string actual)
{
    string[] TachTu(string text) => Regex.Matches(text.Normalize().ToLowerInvariant(), @"[\p{L}\p{Nd}]+").Select(m => m.Value).ToArray();
    var expected = TachTu(reference); var received = TachTu(actual);
    var previous = Enumerable.Range(0, received.Length + 1).ToArray();
    for (var i = 1; i <= expected.Length; i++)
    {
        var current = new int[received.Length + 1]; current[0] = i;
        for (var j = 1; j <= received.Length; j++)
            current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + (expected[i - 1] == received[j - 1] ? 0 : 1));
        previous = current;
    }
    return previous[^1] / (double)Math.Max(expected.Length, 1);
}

static async Task KiemTraBanGhiDaiAsync(string root)
{
    var directory = Path.Combine(root, "Backend", "Storage", "AI", "speech", "fixtures");
    using var fixtures = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(directory, "references.json")));
    var sample = await File.ReadAllBytesAsync(Path.Combine(directory, fixtures.RootElement[0].GetProperty("file").GetString()!));
    var pcm = sample.AsSpan(44).ToArray();
    using var wav = new MemoryStream();
    wav.Write(sample.AsSpan(0, 44));
    for (var i = 0; i < 4; i++) wav.Write(pcm);
    var data = wav.ToArray(); BitConverter.GetBytes(data.Length - 8).CopyTo(data, 4); BitConverter.GetBytes(data.Length - 44).CopyTo(data, 40);
    using var input = new MemoryStream(data); using var runtime = new AiInterviewRuntime();
    var speech = new InterviewSpeechService(new MoiTruongKiemTra(root), Options.Create(new AiInterviewOptions()), runtime, NullLogger<InterviewSpeechService>.Instance);
    var started = Stopwatch.StartNew();
    var transcript = await speech.ChuyenThanhVanBanAsync(input, default);
    if (Regex.Matches(transcript, "các sản phẩm thủ công", RegexOptions.IgnoreCase).Count < 3)
        throw new InvalidOperationException("Long recording was truncated or poorly transcribed: " + transcript);
    Console.WriteLine($"PASS: >30s Vietnamese audio chunking, {(data.Length - 44) / 32000.0:F1}s audio, {started.Elapsed.TotalSeconds:F1}s inference, {transcript.Length} transcript characters.");
}

sealed class MoiTruongKiemTra(string root) : IWebHostEnvironment
{
    public string ApplicationName { get; set; } = "JobPortal";
    public string EnvironmentName { get; set; } = "Development";
    public string ContentRootPath { get; set; } = root;
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    public string WebRootPath { get; set; } = Path.Combine(root, "Frontend", "wwwroot");
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
}

sealed class GhiNhanPhanHoi : DelegatingHandler
{
    public string LastResponse = "";
    public GhiNhanPhanHoi() : base(new HttpClientHandler()) { }
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);
        LastResponse = await response.Content.ReadAsStringAsync(cancellationToken);
        return response;
    }
}
