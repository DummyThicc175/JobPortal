using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using JobPortal.Models;
using JobPortal.Data;
using JobPortal;
using JobPortal.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    WebRootPath = "Frontend/wwwroot"
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
// Cấu hình Identity hỗ trợ Role và Razor Pages
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.User.RequireUniqueEmail = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
})
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.Events.OnValidatePrincipal = context => context.HttpContext.RequestServices
        .GetRequiredService<CookieSessionValidator>().KiemTraPhienAsync(context);
});

builder.Services.AddControllersWithViews(options => options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");
builder.Services.AddRazorPages(); // Đảm bảo dịch vụ Razor Pages được đăng ký
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 13 * 1024 * 1024;
});

builder.Services.AddSignalR();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<CvService>();
builder.Services.AddScoped<JobService>();
builder.Services.AddScoped<PrivateFileStorage>();
builder.Services.AddScoped<ChatAccessService>();
builder.Services.AddScoped<CookieSessionValidator>();
builder.Services.AddSingleton<ChatMessageThrottle>();
builder.Services.Configure<JobPortal.Configuration.InterviewOptions>(builder.Configuration.GetSection("Interview"));
builder.Services.AddScoped<InterviewService>();
builder.Services.AddSingleton<InterviewPresence>();
builder.Services.AddHostedService<LegacyCvFileImporter>();
builder.Services.AddSingleton(_ => new JobPortal.Configuration.CauHinhGeminiCucBo(builder.Environment.ContentRootPath));
builder.Services.AddHttpClient<GeminiCvReviewService>(client =>
{
    client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.Headers.RetryAfter = "60";
        await context.HttpContext.Response.WriteAsJsonAsync(new { success = false, message = "Bạn đã gửi quá nhiều yêu cầu. Vui lòng thử lại sau một phút." }, token);
    };
    options.AddPolicy("cv-ai", context => RateLimitPartition.GetFixedWindowLimiter(
        context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 3, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
// Legacy public PDF URLs must pass through the authorized download controller.
app.UseWhen(context => !context.Request.Path.StartsWithSegments("/uploads/cvs"), branch => branch.UseStaticFiles());

app.UseRouting();

app.MapHub<JobPortal.Hubs.ChatHub>("/chatHub");
app.MapHub<JobPortal.Hubs.InterviewHub>("/interviewHub", options => options.CloseOnAuthenticationExpiration = true);

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllerRoute("legacy_private_cv", "uploads/cvs/{fileName}", new { controller = "CV", action = "LegacyDownload" });
app.MapControllerRoute(
    name: "MyAreas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.AnhXaDuongDanCu();
app.MapRazorPages();

app.Run();
