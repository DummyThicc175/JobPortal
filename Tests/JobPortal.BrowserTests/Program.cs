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
void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); checks++; }
int FreePort() { var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start(); var port = ((IPEndPoint)listener.LocalEndpoint).Port; listener.Stop(); return port; }
try
{
    await database.Database.MigrateAsync();
    const string employerId = "c9284cfc-2dc1-460d-8521-396c0966a4f1";
    const string candidateId = "d83b7113-a8f8-4035-99a7-a20dd400f6a4";
    var category = new Category { Name = "Công nghệ thông tin" };
    var company = new Company { Name = "Công nghệ An Việt", EmployerId = employerId };
    var job = new JobPost { Title = "Lập trình viên Backend .NET", Description = "Tham gia xây dựng các sản phẩm web cùng đội ngũ kỹ thuật.\n\nYêu cầu\nCó kiến thức về C#, SQL và phát triển API.\n\nQuyền lợi\nMôi trường làm việc cởi mở, cùng học hỏi và phát triển.", SalaryRange = "18–25 triệu", EmployerId = employerId, Company = company, Category = category, LocationId = 1, IsApproved = true };
    database.Add(job); await database.SaveChangesAsync();
    var appPort = FreePort(); var debugPort = FreePort(); var origin = "http://127.0.0.1:" + appPort;
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
    async Task WaitHttp(string url)
    {
        for (var i = 0; i < 150; i++)
        {
            try { if ((await http.GetAsync(url)).IsSuccessStatusCode) return; } catch (HttpRequestException) { }
            await Task.Delay(100);
        }
        throw new TimeoutException("Server did not start: " + url);
    }
    await WaitHttp(origin);
    var executable = @"C:\Program Files\Google\Chrome\Application\chrome.exe";
    if (!File.Exists(executable)) executable = @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe";
    var chromeStart = new ProcessStartInfo(executable) { UseShellExecute = false, CreateNoWindow = true };
    foreach (var argument in new[] { "--headless=new", "--disable-gpu", "--no-first-run", "--no-default-browser-check", "--use-fake-device-for-media-stream", "--use-fake-ui-for-media-stream", "--autoplay-policy=no-user-gesture-required", "--remote-debugging-port=" + debugPort, "--user-data-dir=" + Path.Combine(storage, "chrome"), "about:blank" }) chromeStart.ArgumentList.Add(argument);
    chrome = Process.Start(chromeStart)!;
    var debug = "http://127.0.0.1:" + debugPort;
    await WaitHttp(debug + "/json/version");
    using var version = JsonDocument.Parse(await http.GetStringAsync(debug + "/json/version"));
    await using var browser = new Cdp(); await browser.ConnectAsync(version.RootElement.GetProperty("webSocketDebuggerUrl").GetString()!);
    async Task<Cdp> Page()
    {
        var context = await browser.Call("Target.createBrowserContext");
        var target = await browser.Call("Target.createTarget", new { url = "about:blank", browserContextId = context.GetProperty("browserContextId").GetString() });
        var page = new Cdp(); await page.ConnectAsync($"ws://127.0.0.1:{debugPort}/devtools/page/{target.GetProperty("targetId").GetString()}");
        await page.Call("Runtime.enable"); await page.Call("Page.enable"); return page;
    }
    async Task Login(Cdp page, string email, string password)
    {
        await page.Navigate(origin + "/Identity/Account/Login");
        var status = await page.Eval("(async()=>{const f=document.querySelector('form');const d=new FormData(f);d.set('Input.Email'," + JsonSerializer.Serialize(email) + ");d.set('Input.Password'," + JsonSerializer.Serialize(password) + ");return (await fetch(f.action,{method:'POST',body:d})).status})()");
        Check(status.GetInt32() == 200, "Browser login completes");
    }
    if (args.Contains("--interview-only"))
    {
        database.Applications.Add(new Application { CandidateId = candidateId, JobPostId = job.Id }); await database.SaveChangesAsync();
        await InterviewBrowserChecks.RunAsync(storage, origin, database, Page, Login, Check);
        Console.WriteLine($"PASS: {checks} real Chrome interview assertions. Synthetic camera/microphone; isolated accounts and disposable SQL database.");
        return;
    }
    await using var candidate = await Page(); await Login(candidate, "candidate@jobhub.com", "Candidate@123");
    await candidate.Navigate(origin + "/Candidate/CV/Create?templateId=1");
    await candidate.Wait("typeof html2pdf === 'function' && typeof addExperienceField === 'function'");
    await candidate.Eval("(()=>{for(const [id,value] of Object.entries({inName:'Browser Candidate',inJob:'Backend Developer',inEmail:'candidate@example.test',inSummary:'Built real software.',inSkills:'C#\\nSQL',inLang:'English'})){const e=document.getElementById(id);e.value=value;e.dispatchEvent(new Event('input',{bubbles:true}))}document.querySelector('.exp-comp').value='Browser Experience';document.querySelector('.exp-role').value='Developer';addExperienceField();document.querySelectorAll('.exp-comp')[1].value='Second Experience';document.querySelectorAll('.exp-comp')[1].dispatchEvent(new Event('input'));return true})()");
    Check((await candidate.Eval("document.getElementById('outExperienceList').children.length === 2")).GetBoolean(), "Editor renders all experience rows");
    await candidate.Eval("document.getElementById('btnSaveCV').click()");
    await candidate.Wait("!document.getElementById('cvStoredDownload').hidden && !document.getElementById('btnSaveCV').disabled", 60);
    Check(!(await candidate.Eval("document.getElementById('cvSaveStatus').classList.contains('alert-danger')")).GetBoolean(), "Actual html2pdf save succeeds");
    var profile = await database.Candidates.AsNoTracking().Include(c => c.DefaultCV).SingleAsync(c => c.UserId == candidateId);
    var firstId = profile.DefaultCVId!.Value;
    var pdf = await File.ReadAllBytesAsync(Path.Combine(storage, "cvs", profile.DefaultCV!.FileName!));
    Check(pdf.Length > 5000 && System.Text.Encoding.ASCII.GetString(pdf, 0, 5) == "%PDF-", "Browser generates a nonempty real PDF and stores it");
    Check(await database.Experiences.CountAsync(e => e.CVId == firstId) == 2, "Browser submits complete normalized content");
    Console.WriteLine("PASS: first template creates a real PDF");
    await candidate.Navigate(origin + "/Candidate/CV/Create?templateId=2");
    await candidate.Wait("document.querySelectorAll('.exp-item').length === 2");
    Check((await candidate.Eval("document.getElementById('inName').value === 'Browser Candidate'")).GetBoolean(), "Saved editor contents reopen in second template");
    await candidate.Eval("document.getElementById('btnSaveCV').click()");
    await candidate.Wait("!document.getElementById('btnSaveCV').disabled && document.getElementById('cvSaveStatus').textContent.includes('Đã lưu')", 60);
    Check((await database.CVs.AsNoTracking().SingleAsync(c => c.Id == firstId)).TemplateId == 2, "Second template PDF saves to the existing CV");
    Console.WriteLine("PASS: second template reopens content and saves a real PDF");
    await candidate.Navigate(origin + "/Job/Details/" + job.Id);
    Check((await candidate.Eval("(async()=>{const f=document.querySelector('form[action*=\"Application/Apply\"]');return (await fetch(f.action,{method:'POST',body:new FormData(f)})).status})()")).GetInt32() == 200, "Browser submits real CV to job");
    Check(await database.Applications.AnyAsync(a => a.CandidateId == candidateId && a.CV!.IsSnapshot), "Browser application uses a real snapshot");
    Console.WriteLine("PASS: browser application submits an immutable CV");
    await candidate.Navigate(origin);
    await candidate.Wait("window.portalChatStarted === true");
    await candidate.Eval("(()=>{const b=document.createElement('button');b.className='btn-open-chat';b.dataset.partnerId=" + JsonSerializer.Serialize(employerId) + ";b.dataset.partnerName='Employer';document.body.append(b);b.click()})()");
    await candidate.Wait("!document.getElementById('chat-input').disabled");
    var unsafeText = "<img src=x onerror=alert(1)> private browser message";
    await candidate.Eval("document.getElementById('chat-input').value=" + JsonSerializer.Serialize(unsafeText) + ";document.getElementById('btn-send-chat').click()");
    await candidate.Wait("document.getElementById('chat-body').textContent.includes('private browser message')");
    Check((await candidate.Eval("document.querySelectorAll('#chat-body img').length === 0")).GetBoolean(), "Chat renders untrusted text safely");
    await using var employer = await Page(); await Login(employer, "employer@jobhub.com", "Employer@123");
    await employer.Navigate(origin + "/Employer/Chat");
    await employer.Wait("document.querySelector('#employer-partner-list button') !== null");
    await employer.Eval("document.querySelector('#employer-partner-list button').click()");
    await employer.Wait("document.getElementById('employer-chat-body').textContent.includes('private browser message')");
    await employer.Eval("document.getElementById('employer-chat-input').value='Employer reply';document.getElementById('employer-btn-send').click()");
    await candidate.Wait("document.getElementById('chat-body').textContent.includes('Employer reply')");
    Check((await candidate.Eval("Array.from(document.querySelectorAll('#chat-body div')).filter(e=>e.textContent==='Employer reply').length === 1")).GetBoolean(), "Realtime chat delivers once between isolated browser sessions");
    var profileForUi = await database.Candidates.SingleAsync(c => c.UserId == candidateId);
    profileForUi.FullName = "Nguyễn Minh Anh"; profileForUi.Phone = "0901234567"; profileForUi.Address = "Thành phố Hồ Chí Minh";
    database.JobPosts.Add(new JobPost { Title = "Chuyên viên kiểm thử phần mềm", Description = "Tin tuyển dụng đang chờ kiểm duyệt.", SalaryRange = "Thỏa thuận", Company = company, Category = category, LocationId = 1, EmployerId = employerId, IsApproved = false });
    database.SavedJobs.Add(new SavedJob { CandidateId = candidateId, JobPostId = job.Id });
    await database.SaveChangesAsync();
    await using var anonymous = await Page();
    await using var admin = await Page(); await Login(admin, "admin@jobhub.com", "Admin@123");
    await UiChecks.RunAsync(storage, origin, job.Id, category.Id, anonymous, candidate, employer, admin, Check);
    Check(candidate.Errors.Count == 0 && employer.Errors.Count == 0, "Browser pages have no unhandled JavaScript errors: " + string.Join("; ", candidate.Errors.Concat(employer.Errors)));
    Console.WriteLine($"PASS: {checks} Chrome browser assertions: real PDFs from both templates, persistence, submission, private realtime chat and safe DOM rendering.");
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
