using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using JobPortal.Data;
using JobPortal.Models;
using JobPortal.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

static class PortalChecks
{
    private static int checks;
    internal static void Check(bool value, string description)
    {
        if (!value) throw new InvalidOperationException(description);
        checks++;
    }
    internal static string Token(string html)
    {
        var match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        if (!match.Success) throw new InvalidOperationException("Antiforgery token missing");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }
    internal static async Task<string> Page(HttpClient client, string path, params string[] text)
    {
        using var response = await client.GetAsync(path);
        var html = await response.Content.ReadAsStringAsync();
        Check(response.StatusCode == HttpStatusCode.OK, $"GET {path}: expected 200, got {(int)response.StatusCode}: {html[..Math.Min(html.Length, 2500)]}");
        foreach (var item in text) Check(html.Contains(item) || WebUtility.HtmlDecode(html).Contains(item), $"GET {path}: missing {item}");
        return html;
    }
    internal sealed record Actor(HttpClient Client, string Id, string Cookie);
    internal static async Task<Actor> Login(PortalFactory factory, string role, string? name = null)
    {
        name ??= role.ToLowerInvariant();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var html = await Page(client, "/Identity/Account/Login");
        using var response = await client.PostAsync("/Identity/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Email"] = $"smoke-{name}@example.test", ["Input.Password"] = PortalFactory.TestPassword,
            ["__RequestVerificationToken"] = Token(html)
        }));
        Check(response.StatusCode == HttpStatusCode.Redirect, $"Login failed for {role}/{name}");
        var cookies = response.Headers.GetValues("Set-Cookie").Select(value => value.Split(';')[0]).Where(value => value.StartsWith(".AspNetCore.Identity.Application", StringComparison.Ordinal));
        return new Actor(client, "smoke-" + name, string.Join("; ", cookies));
    }
    internal static async Task<HttpResponseMessage> Post(Actor actor, string path, string sourcePage, Dictionary<string, string>? fields = null)
    {
        fields ??= [];
        fields["__RequestVerificationToken"] = Token(await Page(actor.Client, sourcePage));
        return await actor.Client.PostAsync(path, new FormUrlEncodedContent(fields));
    }
    private static byte[] Pdf(string text) => Encoding.ASCII.GetBytes("%PDF-1.4\n" + text + "\n%%EOF\n");

    public static async Task RunAsync()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        Environment.SetEnvironmentVariable("ASPNETCORE_TEST_CONTENTROOT_JOBPORTAL", root);
        await using var factory = new PortalFactory(root);
        using (var scope = factory.Services.CreateScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Check(database.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite", "Tests must never write to SQL Server");
            await database.Database.EnsureCreatedAsync();
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            foreach (var (role, name) in new[] { ("Candidate", "candidate"), ("Candidate", "other-candidate"), ("Candidate", "account-test"), ("Employer", "employer"), ("Employer", "other-employer"), ("Admin", "admin") })
            {
                var user = new IdentityUser { Id = "smoke-" + name, UserName = $"smoke-{name}@example.test", Email = $"smoke-{name}@example.test", EmailConfirmed = true, LockoutEnabled = true };
                Check((await manager.CreateAsync(user, PortalFactory.TestPassword)).Succeeded, "Create test account");
                Check((await manager.AddToRoleAsync(user, role)).Succeeded, "Assign test role");
            }
            database.Categories.Add(new Category { Id = 100, Name = "Smoke category" });
            var company = new Company { Id = 100, EmployerId = "smoke-employer", Name = "SmokeUniqueCompany" };
            database.Companies.Add(company);
            database.JobPosts.AddRange(new JobPost { Id = 100, Title = "Smoke visible job", Description = "<img src=x onerror=alert(1)>", Company = company, EmployerId = "smoke-employer", IsApproved = true, CategoryId = 100, LocationId = 1 },
                new JobPost { Id = 101, Title = "Smoke hidden job", Company = company, EmployerId = "smoke-employer", IsApproved = false, CategoryId = 100, LocationId = 1 });
            await database.SaveChangesAsync();
        }
        using var anonymous = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await Page(anonymous, "/", "Smoke visible job");
        await Page(anonymous, "/Job?keyword=SmokeUniqueCompany", "Smoke visible job");
        await Page(anonymous, "/Job/Search?searchString=SmokeUniqueCompany", "Smoke visible job");
        var details = await Page(anonymous, "/Job/Details/100", "/Candidate/Application/Apply", "&lt;img");
        Check(!details.Contains("<img src=x onerror"), "Job description must be HTML encoded");
        await Page(anonymous, "/Home/JobDetails/100");
        foreach (var path in new[] { "/Job/Details/101", "/Home/JobDetails/101", "/Job/Details/999999" })
            Check((await anonymous.GetAsync(path)).StatusCode == HttpStatusCode.NotFound, "Hidden/missing job must not be public");
        foreach (var path in new[] { "/css/site.css", "/css/layout.css", "/css/home.css", "/js/cv-builder.js", "/js/chat.js", "/lib/bootstrap/dist/css/bootstrap.min.css", "/lib/signalr/signalr.min.js", "/lib/html2pdf/html2pdf.bundle.min.js", "/images/templates/t1.jpg" }) await Page(anonymous, path);
        foreach (var path in new[] { "/Candidate/Profile", "/Candidate/CV/SelectTemplate", "/Candidate/Profile/SelectTemplate", "/Candidate/Application", "/Employer/Applicant", "/Admin/Dashboard", "/uploads/cvs/default-cv.pdf" })
            Check((await anonymous.GetAsync(path)).StatusCode == HttpStatusCode.Redirect, "Authentication required: " + path);
        Check((await anonymous.PostAsync("/chatHub/negotiate?negotiateVersion=1", new StringContent(""))).StatusCode == HttpStatusCode.Redirect, "Anonymous SignalR must be rejected");

        foreach (var role in new[] { "Admin", "admin", "ADMIN", " Admin ", "Anything" })
        {
            using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            var form = await Page(client, "/Identity/Account/Register");
            var email = "bad-" + Guid.NewGuid().ToString("N") + "@example.test";
            using var response = await client.PostAsync("/Identity/Account/Register", new FormUrlEncodedContent(new Dictionary<string, string>
            { ["Input.Email"] = email, ["Input.Password"] = PortalFactory.TestPassword, ["Input.ConfirmPassword"] = PortalFactory.TestPassword, ["Input.Role"] = role, ["__RequestVerificationToken"] = Token(form) }));
            Check(response.StatusCode == HttpStatusCode.OK, "Invalid role must return validation page");
            using var scope = factory.Services.CreateScope();
            Check(!await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Users.AnyAsync(u => u.Email == email), "Invalid role must not create any account");
        }
        foreach (var role in new[] { "Candidate", "Employer" })
        {
            using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            var form = await Page(client, "/Identity/Account/Register");
            var email = "valid-" + Guid.NewGuid().ToString("N") + "@example.test";
            using var response = await client.PostAsync("/Identity/Account/Register", new FormUrlEncodedContent(new Dictionary<string, string>
            { ["Input.Email"] = email, ["Input.Password"] = PortalFactory.TestPassword, ["Input.ConfirmPassword"] = PortalFactory.TestPassword, ["Input.Role"] = role, ["__RequestVerificationToken"] = Token(form) }));
            Check(response.StatusCode == HttpStatusCode.Redirect, "Normal registration must still work");
        }
        var candidate = await Login(factory, "Candidate");
        var otherCandidate = await Login(factory, "Candidate", "other-candidate");
        var employer = await Login(factory, "Employer");
        var otherEmployer = await Login(factory, "Employer", "other-employer");
        var admin = await Login(factory, "Admin");
        foreach (var path in new[] { "/Candidate/Profile", "/Candidate/Profile/Edit", "/Candidate/Application", "/Candidate/Profile/AppliedJobs", "/Candidate/SavedJob", "/Candidate/Profile/SavedJobs", "/Candidate/CV/SelectTemplate", "/Candidate/Profile/SelectTemplate", "/Candidate/CV/Create?templateId=1", "/Candidate/CV/Create?templateId=2" }) await Page(candidate.Client, path);
        foreach (var actor in new[] { employer, admin }) Check((await actor.Client.GetAsync("/Candidate/CV/SelectTemplate")).StatusCode == HttpStatusCode.Redirect, "Candidate feature must enforce its role");
        Check((await candidate.Client.GetAsync("/Candidate/CV/Create?templateId=999")).StatusCode == HttpStatusCode.NotFound, "Unknown template must be rejected");
        var editorHtml = await Page(candidate.Client, "/Candidate/CV/Create?templateId=1", "cv-editor-data", "/js/cv-builder.js");
        Check(!editorHtml.Contains("Nguyễn Văn B") && !editorHtml.Contains("0123456789"), "CV editor must not insert fake personal data");
        foreach (var path in new[] { "/Candidate/Application/Apply", "/Candidate/SavedJob/Save" })
            Check((await candidate.Client.PostAsync(path, new FormUrlEncodedContent(new Dictionary<string, string> { ["jobId"] = "100" }))).StatusCode == HttpStatusCode.BadRequest, "CSRF token is required");
        using (var response = await Post(candidate, "/Candidate/Application/Apply", "/Job/Details/100", new() { ["jobId"] = "100" }))
            Check(response.Headers.Location?.OriginalString.Contains("Profile/Edit") == true, "Missing CV must send applicant to their profile");
        using (var scope = factory.Services.CreateScope()) Check(!await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Applications.AnyAsync(), "Missing CV must not create an application");
        using (var response = await Post(candidate, "/Candidate/Application/Apply", "/Job/Details/100", new() { ["jobId"] = "101" })) Check(response.StatusCode == HttpStatusCode.NotFound, "Hidden job application rejected");
        using (var response = await Post(candidate, "/Candidate/SavedJob/Save", "/Job/Details/100", new() { ["jobId"] = "101" })) Check(response.StatusCode == HttpStatusCode.NotFound, "Hidden job save rejected");

        await UploadProfile(candidate, Pdf("original"), "real.pdf", true);
        int originalId, snapshotId; string originalFile;
        using (var scope = factory.Services.CreateScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var profile = await database.Candidates.Include(p => p.DefaultCV).SingleAsync(p => p.UserId == candidate.Id);
            originalId = profile.DefaultCVId!.Value; originalFile = profile.DefaultCV!.FileName!;
            Check(File.Exists(Path.Combine(factory.TestStorage, "cvs", originalFile)), "Uploaded PDF is stored privately");
        }
        foreach (var path in new[] { "/Candidate/Application/Apply", "/Home/ApplyJob" })
        {
            using var response = await Post(candidate, path, "/Job/Details/100", new() { ["jobId"] = "100" });
            Check(response.StatusCode == HttpStatusCode.Redirect && response.Headers.Location?.OriginalString == "/Job/Details/100", "New and legacy submission URLs must work");
        }
        using (var scope = factory.Services.CreateScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var application = await database.Applications.Include(a => a.CV).SingleAsync(); snapshotId = application.CVId!.Value;
            Check(application.CVId != originalId && application.CV!.IsSnapshot, "Submission must own an immutable CV snapshot");
            Check(application.CV!.FileName == originalFile, "Snapshot uses the uploaded immutable file");
        }
        foreach (var path in new[] { "/Candidate/SavedJob/Save", "/Home/SaveJob" })
            using (var response = await Post(candidate, path, "/Job/Details/100", new() { ["jobId"] = "100" })) Check(response.StatusCode == HttpStatusCode.Redirect, "New and legacy saved-job URLs work");
        using (var scope = factory.Services.CreateScope()) Check(await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().SavedJobs.CountAsync() == 1, "Repeated saves do not create duplicates");

        foreach (var actor in new[] { candidate, employer, admin })
        {
            using var response = await actor.Client.GetAsync("/CV/Download/" + snapshotId);
            Check(response.StatusCode == HttpStatusCode.OK && response.Content.Headers.ContentType?.MediaType == "application/pdf", "Authorized CV download");
            Check(response.Content.Headers.ContentDisposition?.DispositionType == "inline", "View CV opens the PDF in the browser instead of forcing a download");
            Check(response.Headers.CacheControl?.NoStore == true, "Private CV is not cached publicly");
        }
        using (var request = new HttpRequestMessage(HttpMethod.Get, "/CV/Download/" + snapshotId))
        {
            request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(0, 4);
            using var response = await employer.Client.SendAsync(request);
            Check(response.StatusCode == HttpStatusCode.PartialContent && await response.Content.ReadAsStringAsync() == "%PDF-", "Browser PDF viewer can request a file range");
        }
        foreach (var actor in new[] { otherCandidate, otherEmployer }) Check((await actor.Client.GetAsync("/CV/Download/" + snapshotId)).StatusCode == HttpStatusCode.NotFound, "Other users cannot download the submitted CV");
        Check((await employer.Client.GetAsync("/CV/Download/" + originalId)).StatusCode == HttpStatusCode.NotFound, "Employer cannot read the mutable draft");
        Check((await employer.Client.GetAsync("/uploads/cvs/" + originalFile)).StatusCode == HttpStatusCode.OK, "Legacy CV URL applies the same authorization");
        Check((await otherEmployer.Client.GetAsync("/uploads/cvs/" + originalFile)).StatusCode == HttpStatusCode.NotFound, "Legacy CV URL does not bypass authorization");
        Check((await candidate.Client.GetAsync("/uploads/cvs/default-cv.pdf")).StatusCode == HttpStatusCode.NotFound, "Default placeholder CV cannot be served as a real document");
        var applicantHtml = await Page(employer.Client, "/Employer/Applicant", "/CV/Download/" + snapshotId, "UpdateStatus");
        Check(!applicantHtml.Contains("default-cv.pdf"), "Applicant UI has no fake CV fallback");
        await Page(employer.Client, "/Employer/Applicant/ByJob?jobId=100", "/CV/Download/" + snapshotId);
        Check((await otherEmployer.Client.GetAsync("/Employer/Applicant/ByJob?jobId=100")).StatusCode == HttpStatusCode.NotFound, "Applicant lists enforce job ownership");

        await UploadProfile(candidate, Pdf("replacement"), "new.pdf", true);
        using (var scope = factory.Services.CreateScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Check((await database.Applications.Include(a => a.CV).SingleAsync()).CV!.FileName == originalFile, "Replacing profile CV must not change a submitted CV");
            Check(File.Exists(Path.Combine(factory.TestStorage, "cvs", originalFile)), "A file referenced by a snapshot must not be deleted");
        }
        await UploadProfile(candidate, Encoding.UTF8.GetBytes("<html>not a pdf</html>"), "bad.pdf", false);
        await UploadProfile(candidate, Pdf("disguised"), "bad.html", false);
        await UploadProfile(candidate, new byte[PrivateFileStorage.MaxPdfBytes + 1], "large.pdf", false);
        await BuilderAndAi(factory, candidate);
        await JobsAndAdmin(factory, employer, otherEmployer, candidate, admin);
        await ChatChecks.RunAsync(factory, candidate, employer, otherEmployer);
        await InterviewChecks.RunAsync(factory, candidate, employer, otherCandidate, otherEmployer, admin);
        await AccountChecks(factory, admin);
        await PaginationChecks(factory, anonymous);
        await UniqueConstraints(factory, candidate);
        Console.WriteLine($"PASS: {checks} assertions. Verified roles, CSRF, private/immutable CVs, uploads, builder persistence, AI validation/rate limits, job moderation/search, account sessions, paging, indexes and private SignalR delivery. Database: SQLite in-memory; Gemini: fake HTTP handler.");

        async Task UploadProfile(Actor actor, byte[] content, string name, bool accepted)
        {
            var edit = await Page(actor.Client, "/Candidate/Profile/Edit");
            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(Token(edit)), "__RequestVerificationToken");
            form.Add(new StringContent("Smoke candidate"), "FullName"); form.Add(new StringContent("0123456789"), "Phone");
            form.Add(new ByteArrayContent(content), "CVFile", name);
            using var response = await actor.Client.PostAsync("/Candidate/Profile/SaveProfile", form);
            Check(response.StatusCode == (accepted ? HttpStatusCode.Redirect : HttpStatusCode.OK), "Profile upload validation: " + name);
        }
    }

    private static EditCVViewModel BuilderModel() => new()
    {
        TemplateId = 1, FullName = "CV owner", JobTitle = "Backend developer", Email = "contact@example.test", Summary = "Experience building reliable systems.",
        ThemeColor = "#123456", HeaderColor = "#212529", BackgroundColor = "#ffffff",
        Experiences = [new() { Company = "First", Role = "Developer", Description = "<img src=x onerror=alert(1)>" }, new() { Company = "Second", Duration = "2025" }],
        Educations = [new() { School = "University", Major = "IT" }], ITSKills = [new() { SkillName = "C#" }, new() { SkillName = "SQL" }],
        Languages = [new() { LanguageName = "English", Level = "B2" }], References = [new() { Name = "Reference", Company = "Company", Phone = "contact@example.test" }]
    };

    private static async Task<JsonElement> SaveBuilder(Actor candidate, EditCVViewModel model, HttpStatusCode expected = HttpStatusCode.OK)
    {
        var editor = await Page(candidate.Client, "/Candidate/CV/Create?templateId=1");
        using var form = new MultipartFormDataContent(); form.Add(new StringContent(Token(editor)), "__RequestVerificationToken");
        form.Add(new StringContent(JsonSerializer.Serialize(model)), "modelJson"); form.Add(new ByteArrayContent(Pdf("builder")), "pdfFile", "cv.pdf");
        using var response = await candidate.Client.PostAsync("/Candidate/CV/Save", form);
        Check(response.StatusCode == expected, "Builder save status: " + (int)response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        return string.IsNullOrEmpty(body) ? default : JsonDocument.Parse(body).RootElement.Clone();
    }

    private static async Task BuilderAndAi(PortalFactory factory, Actor candidate)
    {
        var model = BuilderModel();
        var first = await SaveBuilder(candidate, model); model.Id = first.GetProperty("id").GetInt32();
        using (var scope = factory.Services.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<CvService>();
            var saved = await service.Documents.AsNoTracking().SingleAsync(c => c.Id == model.Id);
            Check(saved.Experiences.Count == 2 && saved.Skills.Count == 2 && saved.References.Count == 1, "All builder sections are stored in normalized tables");
            Check(saved.ThemeColor == "#123456" && saved.References[0].Company == "Company", "Styles and reference fields survive saving");
            var editor = await service.GetEditorAsync(candidate.Id, 1, default);
            Check(editor!.Id == model.Id && editor.Experiences.Count == 2, "Editor reopens the saved CV");
        }
        var page = await Page(candidate.Client, "/Candidate/CV/Create?templateId=1");
        Check(!page.Contains("<img src=x onerror"), "Stored CV input is encoded in editor HTML/JSON");
        model.Summary = "Updated summary"; await SaveBuilder(candidate, model);
        using (var scope = factory.Services.CreateScope()) Check(await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Experiences.CountAsync(e => e.CVId == model.Id) == 2, "Editing replaces child sections rather than appending duplicates");
        var forged = BuilderModel(); forged.Id = model.Id; forged.FullName = "Changed";
        var other = await Login(factory, "Candidate", "other-candidate"); await SaveBuilder(other, forged, HttpStatusCode.NotFound);
        forged = BuilderModel(); forged.TemplateId = 999; await SaveBuilder(candidate, forged, HttpStatusCode.BadRequest);
        using (var scope = factory.Services.CreateScope())
        {
            var snapshot = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().CVs.SingleAsync(c => c.IsSnapshot);
            forged = BuilderModel(); forged.Id = snapshot.Id;
        }
        await SaveBuilder(candidate, forged, HttpStatusCode.NotFound);

        var csrf = Token(await Page(candidate.Client, "/Candidate/CV/Create?templateId=1"));
        async Task<HttpResponseMessage> Analyze(EditCVViewModel data)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/Candidate/CVReview/Analyze") { Content = JsonContent.Create(data) };
            request.Headers.Add("X-CSRF-TOKEN", csrf); return await candidate.Client.SendAsync(request);
        }
        using (var result = await Analyze(model)) Check(result.StatusCode == HttpStatusCode.OK, "Valid AI review works");
        model.AiScore = 999; model.AiSuggestions = ["forged"];
        await SaveBuilder(candidate, model);
        using (var scope = factory.Services.CreateScope()) Check((await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().CVs.SingleAsync(c => c.Id == model.Id)).AiScore == 88, "Only server-verified AI score is stored");
        var calls = factory.Gemini.Calls;
        using (var result = await Analyze(model)) Check(result.StatusCode == HttpStatusCode.OK && factory.Gemini.Calls == calls, "Unchanged CV review uses the cache");
        model.Summary += " edited";
        await SaveBuilder(candidate, model);
        using (var scope = factory.Services.CreateScope()) Check((await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().CVs.SingleAsync(c => c.Id == model.Id)).AiScore == null, "AI score is cleared when reviewed content changes");
        using (var result = await Analyze(model)) Check(result.StatusCode == HttpStatusCode.OK, "Third AI request allowed");
        using (var result = await Analyze(model)) Check(result.StatusCode == HttpStatusCode.TooManyRequests, "Server rate limit cannot be bypassed by the browser");
        Check(!factory.Gemini.KeyAppearedInQuery, "Gemini API key must not appear in URLs");
        using (var scope = factory.Services.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<GeminiCvReviewService>();
            foreach (var invalid in new[] { "{\"Score\":101,\"Suggestions\":[\"bad\"]}", "{\"Suggestions\":[\"missing score\"]}", "not json", "{\"Score\":50,\"Suggestions\":null}" })
            {
                factory.Gemini.ReviewJson = invalid;
                try { await service.AnalyzeAsync("invalid-" + Guid.NewGuid(), BuilderModel(), default); Check(false, "Invalid AI response accepted"); }
                catch (AiReviewException exception) { Check(exception.StatusCode == 502, "Invalid AI schema is handled"); }
            }
            factory.Gemini.Status = HttpStatusCode.TooManyRequests;
            try { await service.AnalyzeAsync("quota-test", BuilderModel(), default); Check(false, "Upstream quota accepted"); }
            catch (AiReviewException exception) { Check(exception.StatusCode == 429, "Upstream quota has clear status"); }
            factory.Gemini.Status = HttpStatusCode.OK;
        }
    }

    private static async Task JobsAndAdmin(PortalFactory factory, Actor employer, Actor otherEmployer, Actor candidate, Actor admin)
    {
        foreach (var path in new[] { "/Employer/Job", "/Employer/Job/Create", "/Employer/Job/Edit/100", "/Employer/Applicant", "/Employer/Applicant/ByJob?jobId=100", "/Employer/Job/Candidates1", "/Employer/Job/Candidates?jobId=100", "/Employer/Chat", "/Employer/Job/Messages" }) await Page(employer.Client, path);
        foreach (var path in new[] { "/Admin/Dashboard", "/Admin/Categories", "/Admin/Categories/Create", "/Admin/Categories/Edit/100", "/Admin/JobManager", "/Admin/JobManager/Pending", "/Admin/UserManager" }) await Page(admin.Client, path);
        using (var scope = factory.Services.CreateScope())
        {
            var id = (await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Applications.SingleAsync()).Id.ToString();
            using var forbidden = await Post(otherEmployer, "/Employer/Applicant/UpdateStatus", "/Employer/Job/Create", new() { ["id"] = id, ["status"] = "Accepted" });
            Check(forbidden.StatusCode == HttpStatusCode.NotFound, "Other employer cannot change application status");
            using var invalid = await Post(employer, "/Employer/Applicant/UpdateStatus", "/Employer/Applicant", new() { ["id"] = id, ["status"] = "999" });
            Check(invalid.StatusCode == HttpStatusCode.BadRequest, "Invalid status rejected");
            using var valid = await Post(employer, "/Employer/Applicant/UpdateStatus", "/Employer/Applicant", new() { ["id"] = id, ["status"] = "Interview" });
            Check(valid.StatusCode == HttpStatusCode.Redirect, "Employer updates existing application status");
        }
        await Page(candidate.Client, "/Candidate/Application", "Phỏng vấn");
        var fields = new Dictionary<string, string> { ["Id"] = "100", ["Title"] = "Edited visible job", ["Description"] = "Changed content", ["CompanyName"] = "SmokeUniqueCompany", ["CategoryId"] = "100", ["LocationId"] = "1", ["IsApproved"] = "true", ["EmployerId"] = otherEmployer.Id };
        using (var response = await Post(employer, "/Employer/Job/Edit/100", "/Employer/Job/Edit/100", fields)) Check(response.StatusCode == HttpStatusCode.Redirect, "Employer edits own job");
        Check((await candidate.Client.GetAsync("/Job/Details/100")).StatusCode == HttpStatusCode.NotFound, "Edited job returns to moderation");
        using (var scope = factory.Services.CreateScope())
        {
            var job = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().JobPosts.SingleAsync(j => j.Id == 100);
            Check(job.EmployerId == employer.Id && !job.IsApproved, "Posted system fields cannot override owner or approval");
        }
        using (var response = await Post(admin, "/Admin/JobManager/Approve", "/Admin/JobManager/Pending", new() { ["id"] = "100" })) Check(response.StatusCode == HttpStatusCode.Redirect, "Admin can approve edited job");
        await Page(candidate.Client, "/Job/Details/100", "Edited visible job");
        using (var response = await Post(admin, "/Admin/Categories/Delete/100", "/Admin/Categories/Delete/100", new() { ["id"] = "100" })) Check(response.StatusCode == HttpStatusCode.Redirect, "Deleting a referenced category is handled");
        using (var response = await Post(employer, "/Employer/Job/Delete/100", "/Employer/Job", new() { ["id"] = "100" })) Check(response.StatusCode == HttpStatusCode.Redirect, "Deleting used job is handled");
        using (var scope = factory.Services.CreateScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Check(await database.JobPosts.AnyAsync(j => j.Id == 100 && !j.IsApproved) && await database.Applications.AnyAsync(a => a.JobPostId == 100), "Removing used job preserves application history");
            Check(await database.Categories.AnyAsync(c => c.Id == 100), "Referenced category is preserved");
        }
    }

    private static async Task AccountChecks(PortalFactory factory, Actor admin)
    {
        var account = await Login(factory, "Candidate", "account-test");
        using (var response = await Post(admin, "/Admin/UserManager/AssignRole", "/Admin/UserManager", new() { ["userId"] = account.Id, ["roleName"] = "arbitrary" })) Check(response.StatusCode == HttpStatusCode.BadRequest, "Admin cannot create arbitrary role names");
        using (var response = await Post(admin, "/Admin/UserManager/ToggleLock", "/Admin/UserManager", new() { ["userId"] = account.Id })) Check(response.StatusCode == HttpStatusCode.Redirect, "Lock account");
        Check((await account.Client.GetAsync("/Candidate/Profile")).StatusCode == HttpStatusCode.Redirect, "Lock invalidates an existing login cookie");
        using (var response = await Post(admin, "/Admin/UserManager/ToggleLock", "/Admin/UserManager", new() { ["userId"] = account.Id })) Check(response.StatusCode == HttpStatusCode.Redirect, "Unlock account");
        account = await Login(factory, "Candidate", "account-test");
        using (var response = await Post(admin, "/Admin/UserManager/AssignRole", "/Admin/UserManager", new() { ["userId"] = account.Id, ["roleName"] = "Employer" })) Check(response.StatusCode == HttpStatusCode.Redirect, "Change account role");
        Check((await account.Client.GetAsync("/Candidate/Profile")).StatusCode == HttpStatusCode.Redirect, "Role change invalidates old role cookie");
        using (var response = await Post(admin, "/Admin/UserManager/ToggleLock", "/Admin/UserManager", new() { ["userId"] = admin.Id })) Check(response.StatusCode == HttpStatusCode.Redirect, "Self-lock is safely rejected");
        await Page(admin.Client, "/Admin/UserManager");
    }

    private static async Task PaginationChecks(PortalFactory factory, HttpClient anonymous)
    {
        using (var scope = factory.Services.CreateScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            for (var i = 0; i < 25; i++) database.JobPosts.Add(new JobPost { Title = $"Batch job {i}", Description = "text", CompanyId = 100, EmployerId = "smoke-employer", IsApproved = true, CategoryId = 100, LocationId = 1 });
            await database.SaveChangesAsync();
        }
        var first = await Page(anonymous, "/Job?keyword=Batch"); var second = await Page(anonymous, "/Job?keyword=Batch&page=2");
        Check(first.Contains("page=2") && first.Contains("keyword=Batch"), "Pagination preserves filters");
        Check(Regex.Matches(first, "href=\"/Job/Details/").Count == 20, "First job page is bounded");
        Check(Regex.Matches(second, "href=\"/Job/Details/").Count == 5, "Second job page contains remaining records");
    }

    private static async Task UniqueConstraints(PortalFactory factory, Actor candidate)
    {
        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        database.SavedJobs.Add(new SavedJob { CandidateId = candidate.Id, JobPostId = 100 });
        try { await database.SaveChangesAsync(); Check(false, "Duplicate saved job accepted by database"); }
        catch (DbUpdateException) { Check(true, "Database enforces unique saved job"); database.ChangeTracker.Clear(); }
        database.Applications.Add(new Application { CandidateId = candidate.Id, JobPostId = 100 });
        try { await database.SaveChangesAsync(); Check(false, "Duplicate application accepted by database"); }
        catch (DbUpdateException) { Check(true, "Database enforces unique application"); }
    }
}
