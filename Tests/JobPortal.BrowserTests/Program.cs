using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using JobPortal.Data;
using JobPortal.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
var name = "JobPortal_Verify_" + Guid.NewGuid().ToString("N");
var storage = Path.Combine(root, "artifacts", "browser-tests", Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(storage);
var cs = new SqlConnectionStringBuilder { DataSource = @".\SQLEXPRESS", InitialCatalog = name, IntegratedSecurity = true, TrustServerCertificate = true };
await using var database = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(cs.ConnectionString).Options);
Process? app = null, chrome = null;
var logs = new List<string>();
var checks = 0;
void KiemTra(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); checks++; }
int LayCongTrong() { var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start(); var port = ((IPEndPoint)listener.LocalEndpoint).Port; listener.Stop(); return port; }
try
{
    await database.Database.MigrateAsync();
    const string employerId = "c9284cfc-2dc1-460d-8521-396c0966a4f1";
    const string candidateId = "d83b7113-a8f8-4035-99a7-a20dd400f6a4";
    var category = new Category { Name = "Công nghệ thông tin" };
    var company = new Company { Name = "Công nghệ An Việt", EmployerId = employerId };
    var job = new JobPost { Title = "Lập trình viên Backend .NET", Description = "Tham gia xây dựng các sản phẩm web cùng đội ngũ kỹ thuật.\n\nYêu cầu\nCó kiến thức về C#, SQL và phát triển API.\n\nQuyền lợi\nMôi trường làm việc cởi mở, cùng học hỏi và phát triển.", SalaryRange = "18–25 triệu", EmployerId = employerId, Company = company, Category = category, LocationId = 1, IsApproved = true };
    database.Add(job); await database.SaveChangesAsync();
    var appPort = LayCongTrong(); var debugPort = LayCongTrong(); var origin = "http://127.0.0.1:" + appPort;
    var appStart = new ProcessStartInfo("dotnet") { WorkingDirectory = root, UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
    appStart.ArgumentList.Add(Path.Combine(root, "bin", "Fixes", "net8.0", "JobPortal.dll")); appStart.ArgumentList.Add("--urls"); appStart.ArgumentList.Add(origin);
    appStart.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
    appStart.Environment["ConnectionStrings__DefaultConnection"] = cs.ConnectionString;
    appStart.Environment["Storage__RootPath"] = Path.Combine(storage, "cvs");
    appStart.Environment["Storage__AvatarRootPath"] = Path.Combine(storage, "avatars");
    appStart.Environment["Storage__ImportLegacyFiles"] = "false";
    appStart.Environment["Gemini__ApiKey"] = "";
    appStart.Environment["Interview__TurnSharedSecret"] = "";
    appStart.Environment["Interview__ForceRelay"] = "false";
    appStart.Environment["Logging__LogLevel__Default"] = "Warning";
    app = Process.Start(appStart)!;
    app.OutputDataReceived += (_, e) => { if (e.Data != null) lock (logs) logs.Add(e.Data); };
    app.ErrorDataReceived += (_, e) => { if (e.Data != null) lock (logs) logs.Add(e.Data); };
    app.BeginOutputReadLine(); app.BeginErrorReadLine();
    using var http = new HttpClient();
    async Task ChoWebsiteAsync(string url)
    {
        for (var i = 0; i < 150; i++)
        {
            try { if ((await http.GetAsync(url)).IsSuccessStatusCode) return; } catch (HttpRequestException) { }
            await Task.Delay(100);
        }
        throw new TimeoutException("Server did not start: " + url);
    }
    await ChoWebsiteAsync(origin);
    var executable = @"C:\Program Files\Google\Chrome\Application\chrome.exe";
    if (!File.Exists(executable)) executable = @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe";
    var chromeStart = new ProcessStartInfo(executable) { UseShellExecute = false, CreateNoWindow = true };
    foreach (var argument in new[] { "--headless=new", "--disable-gpu", "--no-first-run", "--no-default-browser-check", "--use-fake-device-for-media-stream", "--use-fake-ui-for-media-stream", "--autoplay-policy=no-user-gesture-required", "--remote-debugging-port=" + debugPort, "--user-data-dir=" + Path.Combine(storage, "chrome"), "about:blank" }) chromeStart.ArgumentList.Add(argument);
    chrome = Process.Start(chromeStart)!;
    var debug = "http://127.0.0.1:" + debugPort;
    await ChoWebsiteAsync(debug + "/json/version");
    using var version = JsonDocument.Parse(await http.GetStringAsync(debug + "/json/version"));
    await using var browser = new Cdp(); await browser.KetNoiAsync(version.RootElement.GetProperty("webSocketDebuggerUrl").GetString()!);
    async Task<Cdp> TaoTrangAsync()
    {
        var context = await browser.GoiLenhAsync("Target.createBrowserContext");
        var target = await browser.GoiLenhAsync("Target.createTarget", new { url = "about:blank", browserContextId = context.GetProperty("browserContextId").GetString() });
        var page = new Cdp(); await page.KetNoiAsync($"ws://127.0.0.1:{debugPort}/devtools/page/{target.GetProperty("targetId").GetString()}");
        await page.GoiLenhAsync("Runtime.enable"); await page.GoiLenhAsync("Page.enable"); return page;
    }
    async Task DangNhapAsync(Cdp page, string email, string password)
    {
        await page.MoTrangAsync(origin + "/Identity/Account/Login");
        var status = await page.ThucThiJavaScriptAsync("(async()=>{const f=document.querySelector('form');const d=new FormData(f);d.set('Input.Email'," + JsonSerializer.Serialize(email) + ");d.set('Input.Password'," + JsonSerializer.Serialize(password) + ");return (await fetch(f.action,{method:'POST',body:d})).status})()");
        KiemTra(status.GetInt32() == 200, "Browser login completes");
    }
    if (args.Contains("--interview-only"))
    {
        database.Applications.Add(new Application { CandidateId = candidateId, JobPostId = job.Id }); await database.SaveChangesAsync();
        await InterviewBrowserChecks.ChayKiemTraAsync(storage, origin, database, TaoTrangAsync, DangNhapAsync, KiemTra);
        Console.WriteLine($"PASS: {checks} real Chrome interview assertions. Synthetic camera/microphone; isolated accounts and disposable SQL database.");
        return;
    }
    await using var candidate = await TaoTrangAsync(); await DangNhapAsync(candidate, "candidate@jobhub.com", "Candidate@123");
    await candidate.MoTrangAsync(origin + "/Candidate/CV/Create?templateId=1");
    await candidate.ChoDieuKienAsync("typeof html2pdf === 'function' && typeof themMucKinhNghiem === 'function'");
    await candidate.ThucThiJavaScriptAsync("(()=>{for(const [id,value] of Object.entries({inName:'Browser Candidate',inJob:'Backend Developer',inEmail:'candidate@example.test',inSummary:'Built real software.',inSkills:'C#\\nSQL',inLang:'English'})){const e=document.getElementById(id);e.value=value;e.dispatchEvent(new Event('input',{bubbles:true}))}document.querySelector('.exp-comp').value='Browser Experience';document.querySelector('.exp-role').value='Developer';themMucKinhNghiem();document.querySelectorAll('.exp-comp')[1].value='Second Experience';document.querySelectorAll('.exp-comp')[1].dispatchEvent(new Event('input'));return true})()");
    await candidate.ChoDieuKienAsync("document.getElementById('outExperienceList')?.children.length === 2");
    KiemTra((await candidate.ThucThiJavaScriptAsync("document.getElementById('outExperienceList').children.length === 2")).GetBoolean(), "Editor renders all experience rows");
    await candidate.ThucThiJavaScriptAsync("document.getElementById('btnSaveCV').click()");
    await candidate.ChoDieuKienAsync("!document.getElementById('cvStoredDownload').hidden && !document.getElementById('btnSaveCV').disabled", 60);
    KiemTra(!(await candidate.ThucThiJavaScriptAsync("document.getElementById('cvSaveStatus').classList.contains('alert-danger')")).GetBoolean(), "Actual html2pdf save succeeds");
    var profile = await database.Candidates.AsNoTracking().Include(c => c.DefaultCV).SingleAsync(c => c.UserId == candidateId);
    var firstId = profile.DefaultCVId!.Value;
    var pdf = await File.ReadAllBytesAsync(Path.Combine(storage, "cvs", profile.DefaultCV!.FileName!));
    KiemTra(pdf.Length > 5000 && System.Text.Encoding.ASCII.GetString(pdf, 0, 5) == "%PDF-", "Browser generates a nonempty real PDF and stores it");
    KiemTra(await database.Experiences.CountAsync(e => e.CVId == firstId) == 2, "Browser submits complete normalized content");
    Console.WriteLine("PASS: first template creates a real PDF");
    await candidate.MoTrangAsync(origin + "/Candidate/CV/Create?templateId=2");
    await candidate.ChoDieuKienAsync("document.querySelectorAll('.exp-item').length === 2");
    KiemTra((await candidate.ThucThiJavaScriptAsync("document.getElementById('inName').value === 'Browser Candidate'")).GetBoolean(), "Saved editor contents reopen in second template");
    await candidate.ThucThiJavaScriptAsync("document.getElementById('btnSaveCV').click()");
    await candidate.ChoDieuKienAsync("!document.getElementById('btnSaveCV').disabled && document.getElementById('cvSaveStatus').textContent.includes('Đã lưu')", 60);
    KiemTra((await database.CVs.AsNoTracking().SingleAsync(c => c.Id == firstId)).TemplateId == 2, "Second template PDF saves to the existing CV");
    Console.WriteLine("PASS: second template reopens content and saves a real PDF");
    await CvStudioChecks.ChayKiemTraAsync(storage, candidate, database, firstId, KiemTra);
    await candidate.MoTrangAsync(origin + "/Job/Details/" + job.Id);
    KiemTra((await candidate.ThucThiJavaScriptAsync("(async()=>{const f=document.querySelector('form[action*=\"Application/Apply\"]');return (await fetch(f.action,{method:'POST',body:new FormData(f)})).status})()")).GetInt32() == 200, "Browser submits real CV to job");
    KiemTra(await database.Applications.AnyAsync(a => a.CandidateId == candidateId && a.CV!.IsSnapshot), "Browser application uses a real snapshot");
    Console.WriteLine("PASS: browser application submits an immutable CV");
    await candidate.MoTrangAsync(origin);
    await candidate.ChoDieuKienAsync("window.portalChatStarted === true");
    await candidate.ThucThiJavaScriptAsync("(()=>{const b=document.createElement('button');b.className='btn-open-chat';b.dataset.partnerId=" + JsonSerializer.Serialize(employerId) + ";b.dataset.partnerName='Employer';document.body.append(b);b.click()})()");
    await candidate.ChoDieuKienAsync("!document.getElementById('chat-input').disabled");
    var unsafeText = "<img src=x onerror=alert(1)> private browser message";
    await candidate.ThucThiJavaScriptAsync("document.getElementById('chat-input').value=" + JsonSerializer.Serialize(unsafeText) + ";document.getElementById('btn-send-chat').click()");
    await candidate.ChoDieuKienAsync("document.getElementById('chat-body').textContent.includes('private browser message')");
    KiemTra((await candidate.ThucThiJavaScriptAsync("document.querySelectorAll('#chat-body img').length === 0")).GetBoolean(), "Chat renders untrusted text safely");
    await using var employer = await TaoTrangAsync(); await DangNhapAsync(employer, "employer@jobhub.com", "Employer@123");
    await employer.MoTrangAsync(origin + "/Employer/Chat");
    await employer.ChoDieuKienAsync("document.querySelector('#employer-partner-list button') !== null");
    await employer.ThucThiJavaScriptAsync("document.querySelector('#employer-partner-list button').click()");
    await employer.ChoDieuKienAsync("document.getElementById('employer-chat-body').textContent.includes('private browser message')");
    await employer.ThucThiJavaScriptAsync("document.getElementById('employer-chat-input').value='Employer reply';document.getElementById('employer-btn-send').click()");
    await candidate.ChoDieuKienAsync("document.getElementById('chat-body').textContent.includes('Employer reply')");
    KiemTra((await candidate.ThucThiJavaScriptAsync("Array.from(document.querySelectorAll('#chat-body div')).filter(e=>e.textContent==='Employer reply').length === 1")).GetBoolean(), "Realtime chat delivers once between isolated browser sessions");
    var profileForUi = await database.Candidates.SingleAsync(c => c.UserId == candidateId);
    profileForUi.FullName = "Nguyễn Minh Anh"; profileForUi.Phone = "0901234567"; profileForUi.Address = "Thành phố Hồ Chí Minh";
    database.JobPosts.Add(new JobPost { Title = "Chuyên viên kiểm thử phần mềm", Description = "Tin tuyển dụng đang chờ kiểm duyệt.", SalaryRange = "Thỏa thuận", Company = company, Category = category, LocationId = 1, EmployerId = employerId, IsApproved = false });
    database.SavedJobs.Add(new SavedJob { CandidateId = candidateId, JobPostId = job.Id });
    await database.SaveChangesAsync();
    await using var anonymous = await TaoTrangAsync();
    await using var admin = await TaoTrangAsync(); await DangNhapAsync(admin, "admin@jobhub.com", "Admin@123");
    await UiChecks.ChayKiemTraAsync(storage, origin, job.Id, category.Id, anonymous, candidate, employer, admin, KiemTra);
    KiemTra(candidate.Errors.Count == 0 && employer.Errors.Count == 0, "Browser pages have no unhandled JavaScript errors: " + string.Join("; ", candidate.Errors.Concat(employer.Errors)));
    Console.WriteLine($"PASS: {checks} Chrome browser assertions: six CV templates, exact multipage PDFs, layout persistence, mocked AI UI, submission, private realtime chat and safe DOM rendering.");
}
catch (Exception error) { Console.Error.WriteLine(error); Environment.ExitCode = 1; }
finally
{
    if (chrome is { HasExited: false }) { chrome.Kill(true); await chrome.WaitForExitAsync(); }
    if (app is { HasExited: false }) { app.Kill(true); await app.WaitForExitAsync(); }
    await File.WriteAllLinesAsync(Path.Combine(storage, "app.log"), logs);
    if (!name.StartsWith("JobPortal_Verify_", StringComparison.Ordinal) || !Guid.TryParseExact(name[17..], "N", out _)) throw new InvalidOperationException("Unexpected database name");
    await database.Database.EnsureDeletedAsync();
}
