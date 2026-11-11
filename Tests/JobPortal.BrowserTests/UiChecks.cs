using System.Text.Json;

static class UiChecks
{
    public static async Task ChayKiemTraAsync(string storage, string origin, int jobId, int categoryId, Cdp anonymous, Cdp candidate, Cdp employer, Cdp admin, Action<bool, string> check)
    {
        var screenshots = Path.Combine(storage, "screenshots"); Directory.CreateDirectory(screenshots);
        var layoutFailures = new List<string>();
        void KiemTraBoCuc(bool condition, string description)
        {
            if (condition) check(true, description);
            else { layoutFailures.Add(description); Console.WriteLine("UI ISSUE: " + description); }
        }
        async Task ChupManHinhAsync(Cdp page, string name)
        {
            await page.GoiLenhAsync("Page.bringToFront");
            await page.ThucThiJavaScriptAsync("new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(() => resolve(true))))");
            var picture = await page.GoiLenhAsync("Page.captureScreenshot", new { format = "png", captureBeyondViewport = false });
            await File.WriteAllBytesAsync(Path.Combine(screenshots, name + ".png"), Convert.FromBase64String(picture.GetProperty("data").GetString()!));
        }
        async Task KiemTraTrangAsync(Cdp page, string name, string path, bool workspace)
        {
            await page.MoTrangAsync(origin + path);
            await page.ChoDieuKienAsync("document.querySelector('link[href*=\"portal.css\"]') !== null && document.styleSheets.length > 1");
            var metrics = await page.ThucThiJavaScriptAsync("({width:innerWidth,scroll:document.documentElement.scrollWidth,lang:document.documentElement.lang,main:document.querySelectorAll('main').length,active:document.querySelectorAll('.sidebar-link[aria-current=page]').length,broken:[...document.images].filter(i=>i.complete && !i.naturalWidth && i.getClientRects().length>0).map(i=>i.getAttribute('src')),title:document.title})");
            KiemTraBoCuc(metrics.GetProperty("scroll").GetInt32() <= metrics.GetProperty("width").GetInt32() + 1, name + " has horizontal overflow: " + metrics);
            KiemTraBoCuc(metrics.GetProperty("lang").GetString() == "vi" && metrics.GetProperty("main").GetInt32() == 1, name + " has one semantic main and Vietnamese language");
            KiemTraBoCuc(metrics.GetProperty("broken").GetArrayLength() == 0, name + " contains broken images: " + metrics);
            if (workspace) KiemTraBoCuc(metrics.GetProperty("active").GetInt32() == 1, name + " highlights exactly one role navigation item: " + metrics);
            KiemTraBoCuc(page.Errors.Count == 0, name + " has unhandled browser errors: " + string.Join("; ", page.Errors));
            await ChupManHinhAsync(page, name);
        }
        foreach (var width in new[] { 1440, 768, 390, 360 })
        {
            foreach (var page in new[] { anonymous, candidate, employer, admin })
            {
                await page.GoiLenhAsync("Emulation.setDeviceMetricsOverride", new { width, height = width == 1440 ? 1000 : width == 768 ? 1024 : 844, deviceScaleFactor = 1, mobile = width < 992 });
                await page.GoiLenhAsync("Emulation.setTouchEmulationEnabled", new { enabled = width < 992 });
            }
            var prefix = width == 1440 ? "desktop-" : "mobile-" + width + "-";
            foreach (var (name, path) in new[] { ("home", "/"), ("jobs", "/Job"), ("job-details", "/Job/Details/" + jobId), ("login", "/Identity/Account/Login"), ("register", "/Identity/Account/Register"), ("privacy", "/Home/Privacy"), ("access-denied", "/Identity/Account/AccessDenied"), ("lockout", "/Identity/Account/Lockout") })
                await KiemTraTrangAsync(anonymous, prefix + name, path, false);
            foreach (var (name, path) in new[] { ("candidate-profile", "/Candidate/Profile"), ("candidate-edit", "/Candidate/Profile/Edit"), ("candidate-applications", "/Candidate/Application"), ("candidate-saved", "/Candidate/SavedJob"), ("candidate-templates", "/Candidate/CV/SelectTemplate"), ("candidate-editor", "/Candidate/CV/Create?templateId=1"), ("candidate-editor-two", "/Candidate/CV/Create?templateId=2") })
                await KiemTraTrangAsync(candidate, prefix + name, path, true);
            foreach (var (name, path) in new[] { ("employer-overview", "/Employer/Job"), ("employer-create", "/Employer/Job/Create"), ("employer-edit", "/Employer/Job/Edit/" + jobId), ("employer-applicants", "/Employer/Applicant"), ("employer-by-job", "/Employer/Applicant/ByJob?jobId=" + jobId), ("employer-chat", "/Employer/Chat") })
                await KiemTraTrangAsync(employer, prefix + name, path, true);
            foreach (var (name, path) in new[] { ("admin-overview", "/Admin/Dashboard"), ("admin-users", "/Admin/UserManager"), ("admin-jobs", "/Admin/JobManager"), ("admin-pending", "/Admin/JobManager/Pending"), ("admin-categories", "/Admin/Categories"), ("admin-category-create", "/Admin/Categories/Create"), ("admin-category-edit", "/Admin/Categories/Edit/" + categoryId), ("admin-category-details", "/Admin/Categories/Details/" + categoryId), ("admin-category-delete", "/Admin/Categories/Delete/" + categoryId) })
                await KiemTraTrangAsync(admin, prefix + name, path, true);
            if (width == 390)
            {
                await candidate.GoiLenhAsync("Input.dispatchMouseEvent", new { type = "mousePressed", x = 34, y = 34, button = "left", clickCount = 1 });
                await candidate.GoiLenhAsync("Input.dispatchMouseEvent", new { type = "mouseReleased", x = 34, y = 34, button = "left", clickCount = 1 });
                await candidate.ChoDieuKienAsync("document.body.classList.contains('sidebar-open')");
                check((await candidate.ThucThiJavaScriptAsync("document.querySelector('[data-sidebar-toggle]').getAttribute('aria-expanded')==='true'")).GetBoolean(), "Mobile menu opens with accessible state");
                await ChupManHinhAsync(candidate, "mobile-navigation");
                await candidate.GoiLenhAsync("Input.dispatchKeyEvent", new { type = "keyDown", key = "Escape", code = "Escape", windowsVirtualKeyCode = 27 });
                await candidate.ChoDieuKienAsync("!document.body.classList.contains('sidebar-open')");
                check(true, "Escape closes mobile menu");
            }
            Console.WriteLine("Completed UI routes at " + width + "px; layout issues so far: " + layoutFailures.Count);
        }
        await candidate.MoTrangAsync(origin + "/Candidate/CV/SelectTemplate");
        await candidate.ThucThiJavaScriptAsync("document.querySelector('[data-template-filter=Creative]').click()");
        check((await candidate.ThucThiJavaScriptAsync("document.querySelectorAll('[data-template-category]:not([hidden])').length === 3")).GetBoolean(), "Template filter changes visible cards");
        await candidate.ThucThiJavaScriptAsync("document.querySelector('[data-template-category]:not([hidden]) [data-template-preview]').click()");
        await candidate.ChoDieuKienAsync("document.querySelector('#template-preview-modal').classList.contains('show')");
        check(true, "Template preview opens in a dialog");
        await admin.MoTrangAsync(origin + "/Admin/JobManager/Pending");
        await admin.ThucThiJavaScriptAsync("document.querySelector('[data-job-details]').click()");
        await admin.ChoDieuKienAsync("document.getElementById('job-modal-data').hidden === false");
        check((await admin.ThucThiJavaScriptAsync("document.querySelector('[data-job-field=description]').textContent.includes('Tin tuyển dụng')")).GetBoolean(), "Admin review dialog loads and renders real job details");
        await KiemTraTrangAsync(candidate, "logout-confirmation", "/Identity/Account/Logout", false);
        Console.WriteLine("UI screenshots: " + screenshots);
        check(layoutFailures.Count == 0, "UI layout checks failed:\n" + string.Join("\n", layoutFailures));
    }
}
