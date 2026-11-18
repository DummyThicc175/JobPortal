using System.Net;
using System.Text;
using JobPortal.Controllers;
using JobPortal.Data;
using JobPortal.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Threading.RateLimiting;

sealed class PortalFactory(string root) : WebApplicationFactory<HomeController>
{
    public const string TestPassword = "SmokeTest@123";
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    public readonly FakeGeminiHandler Gemini = new();
    public readonly FakeOllamaHandler Ollama = new();
    public readonly FakeInterviewSpeech Speech = new();
    public string TestStorage { get; } = Path.Combine(root, "artifacts", "test-storage", Guid.NewGuid().ToString("N"));
    private PhysicalFileProvider? provider;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        connection.Open();
        builder.UseContentRoot(root).UseEnvironment("Development");
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Storage:RootPath"] = Path.Combine(TestStorage, "cvs"),
            ["Storage:AvatarRootPath"] = Path.Combine(TestStorage, "public", "uploads", "avatars"),
            ["Storage:ImportLegacyFiles"] = "false"
        }));
        builder.ConfigureTestServices(services =>
        {
            // Model calls finish instantly in tests; the bulk suite exceeds a human-paced minute.
            // Keep production limits unchanged and preserve the CV/speech limits in this test host.
            services.RemoveAll<IConfigureOptions<RateLimiterOptions>>();
            services.Configure<RateLimiterOptions>(options =>
            {
                options.RejectionStatusCode = 429;
                options.OnRejected = async (context, token) =>
                {
                    context.HttpContext.Response.Headers.RetryAfter = "60";
                    await Microsoft.AspNetCore.Http.HttpResponseJsonExtensions.WriteAsJsonAsync(context.HttpContext.Response,
                        new { success = false, message = "Bạn đã gửi quá nhiều yêu cầu. Vui lòng thử lại sau một phút." }, token);
                };
                void ThemGioiHan(string name, int limit) => options.AddPolicy(name, context => RateLimitPartition.GetFixedWindowLimiter(
                    context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                    _ => new FixedWindowRateLimiterOptions { PermitLimit = limit, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
                ThemGioiHan("cv-ai", 3); ThemGioiHan("interview-ai", 100); ThemGioiHan("interview-speech", 10);
            });
            services.RemoveAll<IInterviewSpeechService>();
            services.AddSingleton<IInterviewSpeechService>(Speech);
            services.AddHttpClient<OllamaInterviewClient>().ConfigurePrimaryHttpMessageHandler(() => Ollama);
            var thuMucCauHinh = Path.Combine(TestStorage, "settings"); Directory.CreateDirectory(thuMucCauHinh);
            File.WriteAllText(Path.Combine(thuMucCauHinh, "appsettings.json"), "{\"Gemini\":{\"ApiKey\":\"test-key\",\"Model\":\"test-model\"}}");
            services.RemoveAll<JobPortal.Configuration.CauHinhGeminiCucBo>();
            services.AddSingleton(new JobPortal.Configuration.CauHinhGeminiCucBo(thuMucCauHinh));
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.AddSingleton(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);
            services.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
            services.AddScoped(sp => new GeminiCvReviewService(new HttpClient(Gemini, false)
                { BaseAddress = new Uri("https://generativelanguage.googleapis.com/"), Timeout = TimeSpan.FromSeconds(30) },
                sp.GetRequiredService<JobPortal.Configuration.CauHinhGeminiCucBo>(), sp.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>(), sp.GetRequiredService<ILogger<GeminiCvReviewService>>()));
            var environment = services.First(d => d.ServiceType == typeof(IWebHostEnvironment)).ImplementationInstance as IWebHostEnvironment;
            if (environment != null)
            {
                var publicPath = Path.Combine(TestStorage, "public"); Directory.CreateDirectory(publicPath);
                provider = new PhysicalFileProvider(publicPath);
                environment.WebRootFileProvider = new CompositeFileProvider(provider, environment.WebRootFileProvider);
            }
        });
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync(); await connection.DisposeAsync(); provider?.Dispose(); Gemini.Dispose();
        var allowed = Path.GetFullPath(Path.Combine(root, "artifacts", "test-storage")) + Path.DirectorySeparatorChar;
        if (!Path.GetFullPath(TestStorage).StartsWith(allowed, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Unexpected test storage path");
        if (Directory.Exists(TestStorage)) Directory.Delete(TestStorage, true);
    }
}

sealed class FakeInterviewSpeech : IInterviewSpeechService
{
    public TaskCompletionSource<string> Result = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public Task<string> ChuyenThanhVanBanAsync(Stream wav, CancellationToken token) => Result.Task.WaitAsync(token);
}

sealed class FakeGeminiHandler : HttpMessageHandler
{
    public int Calls;
    public bool KeyAppearedInQuery;
    public HttpStatusCode Status = HttpStatusCode.OK;
    public string? RawResponse { get; set; }
    public string ReviewJson = "{\"Score\":88,\"Suggestions\":[\"Viết rõ kết quả công việc.\"]}";
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref Calls);
        KeyAppearedInQuery |= !string.IsNullOrEmpty(request.RequestUri?.Query);
        var response = RawResponse ?? System.Text.Json.JsonSerializer.Serialize(new
        { candidates = new[] { new { content = new { parts = new[] { new { text = ReviewJson } } } } } });
        return Task.FromResult(new HttpResponseMessage(Status) { Content = new StringContent(response, Encoding.UTF8, "application/json") });
    }
}
