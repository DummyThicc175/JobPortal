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
    internal static void KiemTra(bool value, string description)
    {
        if (!value) throw new InvalidOperationException(description);
        checks++;
    }
    internal static string LayMaChongGiaMao(string html)
    {
        var match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        if (!match.Success) throw new InvalidOperationException("Antiforgery token missing");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }
    internal static async Task<string> TaiTrangAsync(HttpClient client, string path, params string[] text)
    {
        using var response = await client.GetAsync(path);
        var html = await response.Content.ReadAsStringAsync();
        KiemTra(response.StatusCode == HttpStatusCode.OK, $"GET {path}: expected 200, got {(int)response.StatusCode}: {html[..Math.Min(html.Length, 2500)]}");
        foreach (var item in text) KiemTra(html.Contains(item) || WebUtility.HtmlDecode(html).Contains(item), $"GET {path}: missing {item}");
        return html;
    }
    internal sealed record Actor(HttpClient Client, string Id, string Cookie);
    internal static async Task<Actor> DangNhapAsync(PortalFactory factory, string role, string? name = null)
    {
        name ??= role.ToLowerInvariant();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var html = await TaiTrangAsync(client, "/Identity/Account/Login");
        using var response = await client.PostAsync("/Identity/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Email"] = $"smoke-{name}@example.test", ["Input.Password"] = PortalFactory.TestPassword,
            ["__RequestVerificationToken"] = LayMaChongGiaMao(html)
        }));
        KiemTra(response.StatusCode == HttpStatusCode.Redirect, $"Login failed for {role}/{name}");
        var cookies = response.Headers.GetValues("Set-Cookie").Select(value => value.Split(';')[0]).Where(value => value.StartsWith(".AspNetCore.Identity.Application", StringComparison.Ordinal));
        return new Actor(client, "smoke-" + name, string.Join("; ", cookies));
    }
    internal static async Task<HttpResponseMessage> GuiBieuMauAsync(Actor actor, string path, string sourcePage, Dictionary<string, string>? fields = null)
    {
        fields ??= [];
        fields["__RequestVerificationToken"] = LayMaChongGiaMao(await TaiTrangAsync(actor.Client, sourcePage));
        return await actor.Client.PostAsync(path, new FormUrlEncodedContent(fields));
    }
    private static byte[] TaoPDF(string text) => Encoding.ASCII.GetBytes("%PDF-1.4\n" + text + "\n%%EOF\n");

    public static async Task ChayKiemTraAsync()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        Environment.SetEnvironmentVariable("ASPNETCORE_TEST_CONTENTROOT_JOBPORTAL", root);
        await using var factory = new PortalFactory(root);
        using (var scope = factory.Services.CreateScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            KiemTra(database.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite", "Tests must never write to SQL Server");
            await database.Database.EnsureCreatedAsync();
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            foreach (var (role, name) in new[] { ("Candidate", "candidate"), ("Candidate", "other-candidate"), ("Candidate", "account-test"), ("Employer", "employer"), ("Employer", "other-employer"), ("Admin", "admin") })
            {
                var user = new IdentityUser { Id = "smoke-" + name, UserName = $"smoke-{name}@example.test", Email = $"smoke-{name}@example.test", EmailConfirmed = true, LockoutEnabled = true };
                KiemTra((await manager.CreateAsync(user, PortalFactory.TestPassword)).Succeeded, "Create test account");
                KiemTra((await manager.AddToRoleAsync(user, role)).Succeeded, "Assign test role");
            }
            database.Categories.Add(new Category { Id = 100, Name = "Smoke category" });
            var company = new Company { Id = 100, EmployerId = "smoke-employer", Name = "SmokeUniqueCompany" };
            database.Companies.Add(company);
            database.JobPosts.AddRange(new JobPost { Id = 100, Title = "Smoke visible job", Description = "<img src=x onerror=alert(1)>", Company = company, EmployerId = "smoke-employer", IsApproved = true, CategoryId = 100, LocationId = 1 },
                new JobPost { Id = 101, Title = "Smoke hidden job", Company = company, EmployerId = "smoke-employer", IsApproved = false, CategoryId = 100, LocationId = 1 });
            await database.SaveChangesAsync();
        }
        using var anonymous = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await TaiTrangAsync(anonymous, "/", "Smoke visible job");
        await TaiTrangAsync(anonymous, "/Job?keyword=SmokeUniqueCompany", "Smoke visible job");
        await TaiTrangAsync(anonymous, "/Job/Search?searchString=SmokeUniqueCompany", "Smoke visible job");
        var details = await TaiTrangAsync(anonymous, "/Job/Details/100", "/Candidate/Application/Apply", "&lt;img");
        KiemTra(!details.Contains("<img src=x onerror"), "Job description must be HTML encoded");
        await TaiTrangAsync(anonymous, "/Home/JobDetails/100");
        foreach (var path in new[] { "/Job/Details/101", "/Home/JobDetails/101", "/Job/Details/999999" })
            KiemTra((await anonymous.GetAsync(path)).StatusCode == HttpStatusCode.NotFound, "Hidden/missing job must not be public");
        foreach (var path in new[] { "/css/site.css", "/css/layout.css", "/css/home.css", "/js/cv-builder.js", "/js/chat.js", "/lib/bootstrap/dist/css/bootstrap.min.css", "/lib/signalr/signalr.min.js", "/lib/html2pdf/html2pdf.bundle.min.js", "/images/templates/t1.jpg" }) await TaiTrangAsync(anonymous, path);
        foreach (var path in new[] { "/Candidate/Profile", "/Candidate/CV/SelectTemplate", "/Candidate/Profile/SelectTemplate", "/Candidate/Application", "/Employer/Applicant", "/Admin/Dashboard", "/uploads/cvs/default-cv.pdf" })
            KiemTra((await anonymous.GetAsync(path)).StatusCode == HttpStatusCode.Redirect, "Authentication required: " + path);
        KiemTra((await anonymous.PostAsync("/chatHub/negotiate?negotiateVersion=1", new StringContent(""))).StatusCode == HttpStatusCode.Redirect, "Anonymous SignalR must be rejected");

        foreach (var role in new[] { "Admin", "admin", "ADMIN", " Admin ", "Anything" })
        {
            using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            var form = await TaiTrangAsync(client, "/Identity/Account/Register");
            var email = "bad-" + Guid.NewGuid().ToString("N") + "@example.test";
            using var response = await client.PostAsync("/Identity/Account/Register", new FormUrlEncodedContent(new Dictionary<string, string>
            { ["Input.Email"] = email, ["Input.Password"] = PortalFactory.TestPassword, ["Input.ConfirmPassword"] = PortalFactory.TestPassword, ["Input.Role"] = role, ["__RequestVerificationToken"] = LayMaChongGiaMao(form) }));
            KiemTra(response.StatusCode == HttpStatusCode.OK, "Invalid role must return validation page");
            using var scope = factory.Services.CreateScope();
            KiemTra(!await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Users.AnyAsync(u => u.Email == email), "Invalid role must not create any account");
        }
        foreach (var role in new[] { "Candidate", "Employer" })
        {
            using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            var form = await TaiTrangAsync(client, "/Identity/Account/Register");
            var email = "valid-" + Guid.NewGuid().ToString("N") + "@example.test";
            using var response = await client.PostAsync("/Identity/Account/Register", new FormUrlEncodedContent(new Dictionary<string, string>
            { ["Input.Email"] = email, ["Input.Password"] = PortalFactory.TestPassword, ["Input.ConfirmPassword"] = PortalFactory.TestPassword, ["Input.Role"] = role, ["__RequestVerificationToken"] = LayMaChongGiaMao(form) }));
            KiemTra(response.StatusCode == HttpStatusCode.Redirect, "Normal registration must still work");
        }
        var candidate = await DangNhapAsync(factory, "Candidate");
        var otherCandidate = await DangNhapAsync(factory, "Candidate", "other-candidate");
        var employer = await DangNhapAsync(factory, "Employer");
        var otherEmployer = await DangNhapAsync(factory, "Employer", "other-employer");
        var admin = await DangNhapAsync(factory, "Admin");
        foreach (var path in new[] { "/Candidate/Profile", "/Candidate/Profile/Edit", "/Candidate/Application", "/Candidate/Profile/AppliedJobs", "/Candidate/SavedJob", "/Candidate/Profile/SavedJobs", "/Candidate/CV/SelectTemplate", "/Candidate/Profile/SelectTemplate", "/Candidate/CV/Create?templateId=1", "/Candidate/CV/Create?templateId=2" }) await TaiTrangAsync(candidate.Client, path);
        foreach (var actor in new[] { employer, admin }) KiemTra((await actor.Client.GetAsync("/Candidate/CV/SelectTemplate")).StatusCode == HttpStatusCode.Redirect, "Candidate feature must enforce its role");
        KiemTra((await candidate.Client.GetAsync("/Candidate/CV/Create?templateId=999")).StatusCode == HttpStatusCode.NotFound, "Unknown template must be rejected");
        var editorHtml = await TaiTrangAsync(candidate.Client, "/Candidate/CV/Create?templateId=1", "cv-editor-data", "/js/cv-builder.js");
        KiemTra(!editorHtml.Contains("Nguyễn Văn B") && !editorHtml.Contains("0123456789"), "CV editor must not insert fake personal data");
        foreach (var path in new[] { "/Candidate/Application/Apply", "/Candidate/SavedJob/Save" })
            KiemTra((await candidate.Client.PostAsync(path, new FormUrlEncodedContent(new Dictionary<string, string> { ["jobId"] = "100" }))).StatusCode == HttpStatusCode.BadRequest, "CSRF token is required");
        using (var response = await GuiBieuMauAsync(candidate, "/Candidate/Application/Apply", "/Job/Details/100", new() { ["jobId"] = "100" }))
            KiemTra(response.Headers.Location?.OriginalString.Contains("Profile/Edit") == true, "Missing CV must send applicant to their profile");
        using (var scope = factory.Services.CreateScope()) KiemTra(!await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Applications.AnyAsync(), "Missing CV must not create an application");
        using (var response = await GuiBieuMauAsync(candidate, "/Candidate/Application/Apply", "/Job/Details/100", new() { ["jobId"] = "101" })) KiemTra(response.StatusCode == HttpStatusCode.NotFound, "Hidden job application rejected");
        using (var response = await GuiBieuMauAsync(candidate, "/Candidate/SavedJob/Save", "/Job/Details/100", new() { ["jobId"] = "101" })) KiemTra(response.StatusCode == HttpStatusCode.NotFound, "Hidden job save rejected");

        await TaiLenHoSoAsync(candidate, TaoPDF("original"), "real.pdf", true);
        int originalId, snapshotId; string originalFile;
        using (var scope = factory.Services.CreateScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var profile = await database.Candidates.Include(p => p.DefaultCV).SingleAsync(p => p.UserId == candidate.Id);
            originalId = profile.DefaultCVId!.Value; originalFile = profile.DefaultCV!.FileName!;
            KiemTra(File.Exists(Path.Combine(factory.TestStorage, "cvs", originalFile)), "Uploaded PDF is stored privately");
        }
        foreach (var path in new[] { "/Candidate/Application/Apply", "/Home/ApplyJob" })
        {
            using var response = await GuiBieuMauAsync(candidate, path, "/Job/Details/100", new() { ["jobId"] = "100" });
            KiemTra(response.StatusCode == HttpStatusCode.Redirect && response.Headers.Location?.OriginalString == "/Job/Details/100", "New and legacy submission URLs must work");
        }
        using (var scope = factory.Services.CreateScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var application = await database.Applications.Include(a => a.CV).SingleAsync(); snapshotId = application.CVId!.Value;
            KiemTra(application.CVId != originalId && application.CV!.IsSnapshot, "Submission must own an immutable CV snapshot");
            KiemTra(application.CV!.FileName == originalFile, "Snapshot uses the uploaded immutable file");
        }
        foreach (var path in new[] { "/Candidate/SavedJob/Save", "/Home/SaveJob" })
            using (var response = await GuiBieuMauAsync(candidate, path, "/Job/Details/100", new() { ["jobId"] = "100" })) KiemTra(response.StatusCode == HttpStatusCode.Redirect, "New and legacy saved-job URLs work");
        using (var scope = factory.Services.CreateScope()) KiemTra(await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().SavedJobs.CountAsync() == 1, "Repeated saves do not create duplicates");

        foreach (var actor in new[] { candidate, employer, admin })
        {
            using var response = await actor.Client.GetAsync("/CV/Download/" + snapshotId);
            KiemTra(response.StatusCode == HttpStatusCode.OK && response.Content.Headers.ContentType?.MediaType == "application/pdf", "Authorized CV download");
            KiemTra(response.Content.Headers.ContentDisposition?.DispositionType == "inline", "View CV opens the PDF in the browser instead of forcing a download");
            KiemTra(response.Headers.CacheControl?.NoStore == true, "Private CV is not cached publicly");
        }
        using (var request = new HttpRequestMessage(HttpMethod.Get, "/CV/Download/" + snapshotId))
        {
            request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(0, 4);
            using var response = await employer.Client.SendAsync(request);
            KiemTra(response.StatusCode == HttpStatusCode.PartialContent && await response.Content.ReadAsStringAsync() == "%PDF-", "Browser PDF viewer can request a file range");
        }
        foreach (var actor in new[] { otherCandidate, otherEmployer }) KiemTra((await actor.Client.GetAsync("/CV/Download/" + snapshotId)).StatusCode == HttpStatusCode.NotFound, "Other users cannot download the submitted CV");
        KiemTra((await employer.Client.GetAsync("/CV/Download/" + originalId)).StatusCode == HttpStatusCode.NotFound, "Employer cannot read the mutable draft");
        KiemTra((await employer.Client.GetAsync("/uploads/cvs/" + originalFile)).StatusCode == HttpStatusCode.OK, "Legacy CV URL applies the same authorization");
        KiemTra((await otherEmployer.Client.GetAsync("/uploads/cvs/" + originalFile)).StatusCode == HttpStatusCode.NotFound, "Legacy CV URL does not bypass authorization");
        KiemTra((await candidate.Client.GetAsync("/uploads/cvs/default-cv.pdf")).StatusCode == HttpStatusCode.NotFound, "Default placeholder CV cannot be served as a real document");
        var applicantHtml = await TaiTrangAsync(employer.Client, "/Employer/Applicant", "/CV/Download/" + snapshotId, "UpdateStatus");
        KiemTra(!applicantHtml.Contains("default-cv.pdf"), "Applicant UI has no fake CV fallback");
        await TaiTrangAsync(employer.Client, "/Employer/Applicant/ByJob?jobId=100", "/CV/Download/" + snapshotId);
        KiemTra((await otherEmployer.Client.GetAsync("/Employer/Applicant/ByJob?jobId=100")).StatusCode == HttpStatusCode.NotFound, "Applicant lists enforce job ownership");

        await TaiLenHoSoAsync(candidate, TaoPDF("replacement"), "new.pdf", true);
        using (var scope = factory.Services.CreateScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            KiemTra((await database.Applications.Include(a => a.CV).SingleAsync()).CV!.FileName == originalFile, "Replacing profile CV must not change a submitted CV");
            KiemTra(File.Exists(Path.Combine(factory.TestStorage, "cvs", originalFile)), "A file referenced by a snapshot must not be deleted");
        }
        await TaiLenHoSoAsync(candidate, Encoding.UTF8.GetBytes("<html>not a pdf</html>"), "bad.pdf", false);
        await TaiLenHoSoAsync(candidate, TaoPDF("disguised"), "bad.html", false);
        await TaiLenHoSoAsync(candidate, new byte[PrivateFileStorage.MaxPdfBytes + 1], "large.pdf", false);
        await KiemTraTrinhTaoCVVaAIAsync(factory, candidate);
        await KiemTraTuyenDungVaQuanTriAsync(factory, employer, otherEmployer, candidate, admin);
        await ChatChecks.ChayKiemTraAsync(factory, candidate, employer, otherEmployer);
        await InterviewChecks.ChayKiemTraAsync(factory, candidate, employer, otherCandidate, otherEmployer, admin);
        await KiemTraTaiKhoanAsync(factory, admin);
        await KiemTraPhanTrangAsync(factory, anonymous);
        await KiemTraRangBuocDuyNhatAsync(factory, candidate);
        Console.WriteLine($"PASS: {checks} assertions. Verified roles, CSRF, private/immutable CVs, uploads, builder persistence, AI validation/rate limits, job moderation/search, account sessions, paging, indexes and private SignalR delivery. Database: SQLite in-memory; Gemini: fake HTTP handler.");

        async Task TaiLenHoSoAsync(Actor actor, byte[] content, string name, bool accepted)
        {
            var edit = await TaiTrangAsync(actor.Client, "/Candidate/Profile/Edit");
            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(LayMaChongGiaMao(edit)), "__RequestVerificationToken");
            form.Add(new StringContent("Smoke candidate"), "FullName"); form.Add(new StringContent("0123456789"), "Phone");
            form.Add(new ByteArrayContent(content), "CVFile", name);
            using var response = await actor.Client.PostAsync("/Candidate/Profile/SaveProfile", form);
            KiemTra(response.StatusCode == (accepted ? HttpStatusCode.Redirect : HttpStatusCode.OK), "Profile upload validation: " + name);
        }
    }

    private static EditCVViewModel TaoDuLieuCV() => new()
    {
        TemplateId = 1, FullName = "CV owner", JobTitle = "Backend developer", Email = "contact@example.test", Summary = "Experience building reliable systems.",
        ThemeColor = "#123456", HeaderColor = "#212529", BackgroundColor = "#ffffff",
        Experiences = [new() { Company = "First", Role = "Developer", Description = "<img src=x onerror=alert(1)>" }, new() { Company = "Second", Duration = "2025" }],
        Educations = [new() { School = "University", Major = "IT" }], ITSKills = [new() { SkillName = "C#" }, new() { SkillName = "SQL" }],
        Languages = [new() { LanguageName = "English", Level = "B2" }], References = [new() { Name = "Reference", Company = "Company", Phone = "contact@example.test" }]
    };

    private static async Task<JsonElement> LuuCVAsync(Actor candidate, EditCVViewModel model, HttpStatusCode expected = HttpStatusCode.OK)
    {
        var editor = await TaiTrangAsync(candidate.Client, "/Candidate/CV/Create?templateId=1");
        using var form = new MultipartFormDataContent(); form.Add(new StringContent(LayMaChongGiaMao(editor)), "__RequestVerificationToken");
        form.Add(new StringContent(JsonSerializer.Serialize(model)), "modelJson"); form.Add(new ByteArrayContent(TaoPDF("builder")), "pdfFile", "cv.pdf");
        using var response = await candidate.Client.PostAsync("/Candidate/CV/Save", form);
        KiemTra(response.StatusCode == expected, "Builder save status: " + (int)response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        return string.IsNullOrEmpty(body) ? default : JsonDocument.Parse(body).RootElement.Clone();
    }

    private static async Task KiemTraTrinhTaoCVVaAIAsync(PortalFactory factory, Actor candidate)
    {
        var model = TaoDuLieuCV();
        var first = await LuuCVAsync(candidate, model); model.Id = first.GetProperty("id").GetInt32();
        using (var scope = factory.Services.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<CvService>();
            var saved = await service.Documents.AsNoTracking().SingleAsync(c => c.Id == model.Id);
            KiemTra(saved.Experiences.Count == 2 && saved.Skills.Count == 2 && saved.References.Count == 1, "All builder sections are stored in normalized tables");
            KiemTra(saved.ThemeColor == "#123456" && saved.References[0].Company == "Company", "Styles and reference fields survive saving");
            var editor = await service.LayDuLieuChinhSuaAsync(candidate.Id, 1, default);
            KiemTra(editor!.Id == model.Id && editor.Experiences.Count == 2, "Editor reopens the saved CV");
        }
        var page = await TaiTrangAsync(candidate.Client, "/Candidate/CV/Create?templateId=1");
        KiemTra(!page.Contains("<img src=x onerror"), "Stored CV input is encoded in editor HTML/JSON");
        KiemTra(!page.Contains("test-key"), "Gemini key is never rendered into editor HTML or JSON");
        foreach (var template in CvTemplateCatalog.Templates)
        {
            model.TemplateId = template.Id; model.BlockLayout = CvTemplateCatalog.TaoBoCucMacDinh(template.Id);
            model.BlockLayout.Reverse(); model.Website = "https://example.test";
            model.Certifications = "Chứng chỉ kiểm thử"; model.Activities = "Dự án website"; model.Interests = "Đọc sách";
            await LuuCVAsync(candidate, model);
            await TaiTrangAsync(candidate.Client, "/Candidate/CV/Create?templateId=" + template.Id, "btnCheckAI", "cv-studio.css");
            using var scope = factory.Services.CreateScope(); var service = scope.ServiceProvider.GetRequiredService<CvService>();
            var reopened = await service.LayDuLieuChinhSuaAsync(candidate.Id, template.Id, default);
            KiemTra(JsonSerializer.Serialize(reopened!.BlockLayout) == JsonSerializer.Serialize(model.BlockLayout), "All six templates preserve ordered block layout");
            KiemTra(reopened.Certifications == model.Certifications && reopened.Activities == model.Activities && reopened.Website == model.Website, "Additional CV content survives reopening");
            var snapshot = await service.TaoBanChupAsync(candidate.Id, model.Id, default);
            KiemTra(snapshot!.LayoutJson == JsonSerializer.Serialize(model.BlockLayout) && snapshot.Certifications == model.Certifications, "Submission snapshot includes layout and complete content");
            var changed = await service.LayDuLieuChinhSuaAsync(candidate.Id, template.Id == 1 ? 2 : 1, default);
            KiemTra(changed!.BlockLayout[0].Key == "summary" && changed.FullName == model.FullName, "Changing template applies its default layout and preserves content");
        }
        model.TemplateId = 1; model.BlockLayout = CvTemplateCatalog.TaoBoCucMacDinh(1);
        foreach (var layout in new List<CvBlockLayout>[] {
            [new() { Key = "summary" }],
            CvTemplateCatalog.BlockKeys.Select(_ => new CvBlockLayout { Key = "summary" }).ToList(),
            CvTemplateCatalog.BlockKeys.Select(k => new CvBlockLayout { Key = k, Zone = "unknown" }).ToList(),
            CvTemplateCatalog.BlockKeys.Select(k => new CvBlockLayout { Key = k == "summary" ? "<script>" : k }).ToList(),
            [null!] })
        {
            var invalid = TaoDuLieuCV(); invalid.Id = model.Id; invalid.BlockLayout = layout;
            await LuuCVAsync(candidate, invalid, HttpStatusCode.BadRequest);
        }
        var invalidSingle = TaoDuLieuCV(); invalidSingle.Id = model.Id; invalidSingle.TemplateId = 4;
        invalidSingle.BlockLayout = CvTemplateCatalog.TaoBoCucMacDinh(1);
        await LuuCVAsync(candidate, invalidSingle, HttpStatusCode.BadRequest);
        KiemTra(CvTemplateCatalog.DocBoCuc("not-json", 1).Count == 9 && CvTemplateCatalog.DocBoCuc(null, 4).All(b => b.Zone == "main"), "Legacy CVs receive a valid layout without altering content");
        model.Summary = "Updated summary"; await LuuCVAsync(candidate, model);
        using (var scope = factory.Services.CreateScope()) KiemTra(await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Experiences.CountAsync(e => e.CVId == model.Id) == 2, "Editing replaces child sections rather than appending duplicates");
        var forged = TaoDuLieuCV(); forged.Id = model.Id; forged.FullName = "Changed";
        var other = await DangNhapAsync(factory, "Candidate", "other-candidate"); await LuuCVAsync(other, forged, HttpStatusCode.NotFound);
        forged = TaoDuLieuCV(); forged.TemplateId = 999; await LuuCVAsync(candidate, forged, HttpStatusCode.BadRequest);
        using (var scope = factory.Services.CreateScope())
        {
            var snapshot = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().CVs.SingleAsync(c => c.IsSnapshot);
            forged = TaoDuLieuCV(); forged.Id = snapshot.Id;
        }
        await LuuCVAsync(candidate, forged, HttpStatusCode.NotFound);

        var csrf = LayMaChongGiaMao(await TaiTrangAsync(candidate.Client, "/Candidate/CV/Create?templateId=1"));
        async Task<HttpResponseMessage> DanhGiaAsync(EditCVViewModel data)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/Candidate/CVReview/Analyze") { Content = JsonContent.Create(data) };
            request.Headers.Add("X-CSRF-TOKEN", csrf); return await candidate.Client.SendAsync(request);
        }
        using (var result = await DanhGiaAsync(model)) KiemTra(result.StatusCode == HttpStatusCode.OK, "Valid AI review works");
        model.AiScore = 999; model.AiSuggestions = ["forged"];
        await LuuCVAsync(candidate, model);
        using (var scope = factory.Services.CreateScope()) KiemTra((await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().CVs.SingleAsync(c => c.Id == model.Id)).AiScore == 88, "Only server-verified AI score is stored");
        var calls = factory.Gemini.Calls;
        using (var result = await DanhGiaAsync(model)) KiemTra(result.StatusCode == HttpStatusCode.OK && factory.Gemini.Calls == calls, "Unchanged CV review uses the cache");
        using (var scope = factory.Services.CreateScope())
            scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>().Remove(GeminiCvReviewService.TaoKhoaBoNhoDem(candidate.Id, model));
        model.TemplateId = 3; model.BlockLayout.Reverse(); model.ThemeColor = "#abcdef";
        var styleSaved = await LuuCVAsync(candidate, model);
        KiemTra(styleSaved.GetProperty("score").GetInt32() == 88, "Layout and color edits preserve server-verified AI score after cache expiry");
        model.Summary += " edited";
        await LuuCVAsync(candidate, model);
        using (var scope = factory.Services.CreateScope()) KiemTra((await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().CVs.SingleAsync(c => c.Id == model.Id)).AiScore == null, "AI score is cleared when reviewed content changes");
        using (var result = await DanhGiaAsync(model)) KiemTra(result.StatusCode == HttpStatusCode.OK, "Third AI request allowed");
        using (var result = await DanhGiaAsync(model)) KiemTra(result.StatusCode == HttpStatusCode.TooManyRequests, "Server rate limit cannot be bypassed by the browser");
        KiemTra(!factory.Gemini.KeyAppearedInQuery, "Gemini API key must not appear in URLs");
        using (var scope = factory.Services.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<GeminiCvReviewService>();
            foreach (var invalid in new[] { "{\"Score\":101,\"Suggestions\":[\"bad\"]}", "{\"Suggestions\":[\"missing score\"]}", "not json", "{\"Score\":50,\"Suggestions\":null}" })
            {
                factory.Gemini.ReviewJson = invalid;
                try { await service.DanhGiaAsync("invalid-" + Guid.NewGuid(), TaoDuLieuCV(), default); KiemTra(false, "Invalid AI response accepted"); }
                catch (AiReviewException exception) { KiemTra(exception.StatusCode == 502, "Invalid AI schema is handled"); }
            }
            factory.Gemini.Status = HttpStatusCode.TooManyRequests;
            try { await service.DanhGiaAsync("quota-test", TaoDuLieuCV(), default); KiemTra(false, "Upstream quota accepted"); }
            catch (AiReviewException exception) { KiemTra(exception.StatusCode == 429, "Upstream quota has clear status"); }
            factory.Gemini.Status = HttpStatusCode.OK;
        }
        var settings = Path.Combine(factory.TestStorage, "configuration-test"); Directory.CreateDirectory(settings);
        File.WriteAllText(Path.Combine(settings, "appsettings.json"), "{\"Gemini\":{\"ApiKey\":\"file-only-key\",\"Model\":\"file-only-model\"}}");
        File.WriteAllText(Path.Combine(settings, "appsettings.Development.json"), "{\"Gemini\":{\"ApiKey\":\"wrong-key\"}}");
        var oldKey = Environment.GetEnvironmentVariable("Gemini__ApiKey"); Environment.SetEnvironmentVariable("Gemini__ApiKey", "wrong-env-key");
        try
        {
            using var config = new JobPortal.Configuration.CauHinhGeminiCucBo(settings);
            KiemTra(config.ApiKey == "file-only-key" && config.Model == "file-only-model", "Gemini only reads root appsettings.json, ignoring environment and Development overrides");
        }
        finally { Environment.SetEnvironmentVariable("Gemini__ApiKey", oldKey); }
    }

    private static async Task KiemTraTuyenDungVaQuanTriAsync(PortalFactory factory, Actor employer, Actor otherEmployer, Actor candidate, Actor admin)
    {
        foreach (var path in new[] { "/Employer/Job", "/Employer/Job/Create", "/Employer/Job/Edit/100", "/Employer/Applicant", "/Employer/Applicant/ByJob?jobId=100", "/Employer/Job/Candidates1", "/Employer/Job/Candidates?jobId=100", "/Employer/Chat", "/Employer/Job/Messages" }) await TaiTrangAsync(employer.Client, path);
        foreach (var path in new[] { "/Admin/Dashboard", "/Admin/Categories", "/Admin/Categories/Create", "/Admin/Categories/Edit/100", "/Admin/JobManager", "/Admin/JobManager/Pending", "/Admin/UserManager" }) await TaiTrangAsync(admin.Client, path);
        using (var scope = factory.Services.CreateScope())
        {
            var id = (await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Applications.SingleAsync()).Id.ToString();
            using var forbidden = await GuiBieuMauAsync(otherEmployer, "/Employer/Applicant/UpdateStatus", "/Employer/Job/Create", new() { ["id"] = id, ["status"] = "Accepted" });
            KiemTra(forbidden.StatusCode == HttpStatusCode.NotFound, "Other employer cannot change application status");
            using var invalid = await GuiBieuMauAsync(employer, "/Employer/Applicant/UpdateStatus", "/Employer/Applicant", new() { ["id"] = id, ["status"] = "999" });
            KiemTra(invalid.StatusCode == HttpStatusCode.BadRequest, "Invalid status rejected");
            using var valid = await GuiBieuMauAsync(employer, "/Employer/Applicant/UpdateStatus", "/Employer/Applicant", new() { ["id"] = id, ["status"] = "Interview" });
            KiemTra(valid.StatusCode == HttpStatusCode.Redirect, "Employer updates existing application status");
        }
        await TaiTrangAsync(candidate.Client, "/Candidate/Application", "Phỏng vấn");
        var fields = new Dictionary<string, string> { ["Id"] = "100", ["Title"] = "Edited visible job", ["Description"] = "Changed content", ["CompanyName"] = "SmokeUniqueCompany", ["CategoryId"] = "100", ["LocationId"] = "1", ["IsApproved"] = "true", ["EmployerId"] = otherEmployer.Id };
        using (var response = await GuiBieuMauAsync(employer, "/Employer/Job/Edit/100", "/Employer/Job/Edit/100", fields)) KiemTra(response.StatusCode == HttpStatusCode.Redirect, "Employer edits own job");
        KiemTra((await candidate.Client.GetAsync("/Job/Details/100")).StatusCode == HttpStatusCode.NotFound, "Edited job returns to moderation");
        using (var scope = factory.Services.CreateScope())
        {
            var job = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().JobPosts.SingleAsync(j => j.Id == 100);
            KiemTra(job.EmployerId == employer.Id && !job.IsApproved, "Posted system fields cannot override owner or approval");
        }
        using (var response = await GuiBieuMauAsync(admin, "/Admin/JobManager/Approve", "/Admin/JobManager/Pending", new() { ["id"] = "100" })) KiemTra(response.StatusCode == HttpStatusCode.Redirect, "Admin can approve edited job");
        await TaiTrangAsync(candidate.Client, "/Job/Details/100", "Edited visible job");
        using (var response = await GuiBieuMauAsync(admin, "/Admin/Categories/Delete/100", "/Admin/Categories/Delete/100", new() { ["id"] = "100" })) KiemTra(response.StatusCode == HttpStatusCode.Redirect, "Deleting a referenced category is handled");
        using (var response = await GuiBieuMauAsync(employer, "/Employer/Job/Delete/100", "/Employer/Job", new() { ["id"] = "100" })) KiemTra(response.StatusCode == HttpStatusCode.Redirect, "Deleting used job is handled");
        using (var scope = factory.Services.CreateScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            KiemTra(await database.JobPosts.AnyAsync(j => j.Id == 100 && !j.IsApproved) && await database.Applications.AnyAsync(a => a.JobPostId == 100), "Removing used job preserves application history");
            KiemTra(await database.Categories.AnyAsync(c => c.Id == 100), "Referenced category is preserved");
        }
    }

    private static async Task KiemTraTaiKhoanAsync(PortalFactory factory, Actor admin)
    {
        var account = await DangNhapAsync(factory, "Candidate", "account-test");
        using (var response = await GuiBieuMauAsync(admin, "/Admin/UserManager/AssignRole", "/Admin/UserManager", new() { ["userId"] = account.Id, ["roleName"] = "arbitrary" })) KiemTra(response.StatusCode == HttpStatusCode.BadRequest, "Admin cannot create arbitrary role names");
        using (var response = await GuiBieuMauAsync(admin, "/Admin/UserManager/ToggleLock", "/Admin/UserManager", new() { ["userId"] = account.Id })) KiemTra(response.StatusCode == HttpStatusCode.Redirect, "Lock account");
        KiemTra((await account.Client.GetAsync("/Candidate/Profile")).StatusCode == HttpStatusCode.Redirect, "Lock invalidates an existing login cookie");
        using (var response = await GuiBieuMauAsync(admin, "/Admin/UserManager/ToggleLock", "/Admin/UserManager", new() { ["userId"] = account.Id })) KiemTra(response.StatusCode == HttpStatusCode.Redirect, "Unlock account");
        account = await DangNhapAsync(factory, "Candidate", "account-test");
        using (var response = await GuiBieuMauAsync(admin, "/Admin/UserManager/AssignRole", "/Admin/UserManager", new() { ["userId"] = account.Id, ["roleName"] = "Employer" })) KiemTra(response.StatusCode == HttpStatusCode.Redirect, "Change account role");
        KiemTra((await account.Client.GetAsync("/Candidate/Profile")).StatusCode == HttpStatusCode.Redirect, "Role change invalidates old role cookie");
        using (var response = await GuiBieuMauAsync(admin, "/Admin/UserManager/ToggleLock", "/Admin/UserManager", new() { ["userId"] = admin.Id })) KiemTra(response.StatusCode == HttpStatusCode.Redirect, "Self-lock is safely rejected");
        await TaiTrangAsync(admin.Client, "/Admin/UserManager");
    }

    private static async Task KiemTraPhanTrangAsync(PortalFactory factory, HttpClient anonymous)
    {
        using (var scope = factory.Services.CreateScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            for (var i = 0; i < 25; i++) database.JobPosts.Add(new JobPost { Title = $"Batch job {i}", Description = "text", CompanyId = 100, EmployerId = "smoke-employer", IsApproved = true, CategoryId = 100, LocationId = 1 });
            await database.SaveChangesAsync();
        }
        var first = await TaiTrangAsync(anonymous, "/Job?keyword=Batch"); var second = await TaiTrangAsync(anonymous, "/Job?keyword=Batch&page=2");
        KiemTra(first.Contains("page=2") && first.Contains("keyword=Batch"), "Pagination preserves filters");
        KiemTra(Regex.Matches(first, "href=\"/Job/Details/").Count == 20, "First job page is bounded");
        KiemTra(Regex.Matches(second, "href=\"/Job/Details/").Count == 5, "Second job page contains remaining records");
    }

    private static async Task KiemTraRangBuocDuyNhatAsync(PortalFactory factory, Actor candidate)
    {
        using var scope = factory.Services.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        database.SavedJobs.Add(new SavedJob { CandidateId = candidate.Id, JobPostId = 100 });
        try { await database.SaveChangesAsync(); KiemTra(false, "Duplicate saved job accepted by database"); }
        catch (DbUpdateException) { KiemTra(true, "Database enforces unique saved job"); database.ChangeTracker.Clear(); }
        database.Applications.Add(new Application { CandidateId = candidate.Id, JobPostId = 100 });
        try { await database.SaveChangesAsync(); KiemTra(false, "Duplicate application accepted by database"); }
        catch (DbUpdateException) { KiemTra(true, "Database enforces unique application"); }
    }
}
