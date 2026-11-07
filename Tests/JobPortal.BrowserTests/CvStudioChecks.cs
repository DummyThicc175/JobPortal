using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using JobPortal.Data;
using JobPortal.Models;
using Microsoft.EntityFrameworkCore;

static class CvStudioChecks
{
    public static async Task ChayKiemTraAsync(string storage, Cdp page, ApplicationDbContext database, int cvId, Action<bool, string> kiemTra)
    {
        await page.GoiLenhAsync("Emulation.setDeviceMetricsOverride", new { width = 1440, height = 1000, deviceScaleFactor = 1, mobile = false });
        async Task ChupAnhAsync(string ten)
        {
            var anh = await page.GoiLenhAsync("Page.captureScreenshot", new { format = "png", captureBeyondViewport = false });
            await File.WriteAllBytesAsync(Path.Combine(storage, ten + ".png"), Convert.FromBase64String(anh.GetProperty("data").GetString()!));
        }
        async Task LuuVaKiemTraPDFAsync(int soTrang)
        {
            await page.ThucThiJavaScriptAsync("document.getElementById('btnSaveCV').click()");
            await page.ChoDieuKienAsync("!document.getElementById('btnSaveCV').disabled", 65);
            var thongBao = await page.ThucThiJavaScriptAsync("document.getElementById('cvSaveStatus').textContent");
            kiemTra(thongBao.GetString()!.StartsWith("Đã lưu"), "CV saves successfully: " + thongBao);
            var cv = await database.CVs.AsNoTracking().SingleAsync(c => c.Id == cvId);
            var tep = await File.ReadAllBytesAsync(Path.Combine(storage, "cvs", cv.FileName!));
            var chuoi = Encoding.Latin1.GetString(tep);
            kiemTra(tep.Length > 5000 && chuoi.StartsWith("%PDF-"), "Each template produces a real PDF");
            kiemTra(Regex.Matches(chuoi, @"/Type\s*/Page\b").Count == soTrang, "PDF page count exactly matches A4 preview, with no extra blank page");
            File.Copy(Path.Combine(storage, "cvs", cv.FileName!), Path.Combine(storage, "cv-template-" + cv.TemplateId + "-" + soTrang + "pages.pdf"), true);
        }
        foreach (var mau in CvTemplateCatalog.Templates)
        {
            await page.ThucThiJavaScriptAsync("(()=>{const e=document.getElementById('cvTemplateSelect');e.value='" + mau.Id + "';e.dispatchEvent(new Event('change'));})()");
            await page.ChoDieuKienAsync("document.querySelector('.cv-page.cv-style-" + mau.Style + "') !== null");
            kiemTra((await page.ThucThiJavaScriptAsync("document.querySelectorAll('.cv-page').length === 1 && [...document.querySelectorAll('.cv-zone')].every(c=>c.scrollHeight<=c.clientHeight+1)")).GetBoolean(), "All six template previews fit their actual paper columns");
            kiemTra((await page.ThucThiJavaScriptAsync("document.getElementById('inName').value === 'Browser Candidate'")).GetBoolean(), "Switching templates preserves form contents");
            await ChupAnhAsync("cv-template-" + mau.Id);
            await LuuVaKiemTraPDFAsync(1);
        }
        await page.ThucThiJavaScriptAsync("(()=>{const e=document.getElementById('cvTemplateSelect');e.value='1';e.dispatchEvent(new Event('change'));document.getElementById('cv-tab-layout').click()})()");
        await page.ChoDieuKienAsync("document.querySelector('[data-block-key=skills] [data-layout-action=zone]')?.disabled === false");
        await page.ThucThiJavaScriptAsync("document.querySelector('[data-block-key=skills] [data-layout-action=zone]').click();document.querySelector('[data-block-key=experience] [data-layout-action=up]').click()");
        kiemTra((await page.ThucThiJavaScriptAsync("document.querySelector('#cv-layout-main [data-block-key]').dataset.blockKey === 'experience' && document.querySelector('#cv-layout-main [data-block-key=skills]') !== null")).GetBoolean(), "Block buttons reorder sections and move them between columns");
        await page.ThucThiJavaScriptAsync("(()=>{const source=document.querySelector('[data-block-key=education]'),target=document.querySelector('#cv-layout-side [data-block-key]'),dataTransfer=new DataTransfer();source.dispatchEvent(new DragEvent('dragstart',{bubbles:true,dataTransfer}));target.dispatchEvent(new DragEvent('dragover',{bubbles:true,cancelable:true,dataTransfer}));target.dispatchEvent(new DragEvent('drop',{bubbles:true,cancelable:true,dataTransfer}));source.dispatchEvent(new DragEvent('dragend',{bubbles:true,dataTransfer}));})()");
        kiemTra((await page.ThucThiJavaScriptAsync("document.querySelector('#cv-layout-side [data-block-key]').dataset.blockKey === 'education'")).GetBoolean(), "Native drag-and-drop events move a whole CV block into another column");
        await page.ChoDieuKienAsync("document.querySelector('.cv-zone-main [data-cv-block]')?.dataset.cvBlock === 'experience'");
        await ChupAnhAsync("cv-layout-controls"); await LuuVaKiemTraPDFAsync(1);
        var daLuu = await database.CVs.AsNoTracking().SingleAsync(c => c.Id == cvId);
        kiemTra(CvTemplateCatalog.DocBoCuc(daLuu.LayoutJson, 1).First().Key == "experience", "Ordered layout is stored in the database");
        await page.GoiLenhAsync("Page.reload"); await page.ChoDieuKienAsync("document.readyState==='complete' && document.querySelector('#cv-layout-side [data-block-key]')?.dataset.blockKey==='education'");
        kiemTra((await page.ThucThiJavaScriptAsync("document.querySelector('#cv-layout-main [data-block-key]').dataset.blockKey === 'experience'")).GetBoolean(), "Reload restores both block ordering and column placement");
        await page.ThucThiJavaScriptAsync("(()=>{window.cvTestFetch=window.fetch;window.fetch=(url,options)=>String(url).includes('/CVReview/Analyze')?Promise.resolve(new Response(JSON.stringify({success:true,score:88,suggestions:['Gợi ý <img src=x onerror=alert(1)>']}),{status:200,headers:{'Content-Type':'application/json'}})):window.cvTestFetch(url,options);document.getElementById('btnCheckAI').click()})()");
        await page.ChoDieuKienAsync("!document.getElementById('btnCheckAI').disabled && document.getElementById('aiScoreBadge').textContent.includes('88')");
        kiemTra((await page.ThucThiJavaScriptAsync("document.querySelectorAll('#aiSuggestionsList img').length === 0 && document.getElementById('aiSuggestionsList').textContent.includes('<img')")).GetBoolean(), "AI review UI safely renders score and untrusted suggestions (mock response; no real API call)");
        await page.ThucThiJavaScriptAsync("(()=>{const e=document.getElementById('themeColorPicker');e.value='#087f6d';e.dispatchEvent(new Event('input'));})()");
        kiemTra((await page.ThucThiJavaScriptAsync("!document.getElementById('aiReviewSection').classList.contains('d-none')")).GetBoolean(), "Changing appearance retains AI review in the editor");
        await page.ThucThiJavaScriptAsync("(()=>{window.fetch=window.cvTestFetch;const t=document.getElementById('cvTemplateSelect');t.value='4';t.dispatchEvent(new Event('change'));const values={inName:'Nguyễn Minh Anh',inSummary:'Xây dựng hệ thống ổn định, kiểm thử và cải tiến sản phẩm. '.repeat(38),inCertifications:'Chứng chỉ chuyên ngành công nghệ thông tin',inActivities:'Xây dựng website tuyển dụng và quản lý phỏng vấn.',inInterests:'Đọc sách, lập trình và nghiên cứu'};for(const[id,value]of Object.entries(values)){const e=document.getElementById(id);e.value=value;e.dispatchEvent(new Event('input'));}for(const e of document.querySelectorAll('.exp-desc')){e.value='Phát triển ứng dụng, phân tích yêu cầu và kiểm thử với nhóm dự án. '.repeat(45);e.dispatchEvent(new Event('input'));}document.getElementById('cv-tab-content').click();})()");
        await page.ChoDieuKienAsync("document.querySelectorAll('.cv-page').length > 1 && document.getElementById('outName').textContent === 'Nguyễn Minh Anh'");
        kiemTra((await page.ThucThiJavaScriptAsync("document.getElementById('aiReviewSection').classList.contains('d-none')")).GetBoolean(), "Content edits clear the AI review display");
        kiemTra((await page.ThucThiJavaScriptAsync("[...document.querySelectorAll('.cv-zone')].every(c=>c.scrollHeight<=c.clientHeight+1)")).GetBoolean(), "Long descriptions paginate without vertical clipping");
        kiemTra((await page.ThucThiJavaScriptAsync("(()=>{const original=[...document.querySelectorAll('.exp-desc')].map(e=>e.value.trim()).join(''),rendered=[...document.querySelectorAll('[data-cv-block=experience] .cv-entry-description')].map(e=>e.textContent).join('');return original===rendered})()")).GetBoolean(), "Splitting long entries across pages preserves complete normalized content");
        var soTrang = (await page.ThucThiJavaScriptAsync("document.querySelectorAll('.cv-page').length")).GetInt32();
        kiemTra(soTrang <= 6, "Long CV fixture remains within supported six-page limit");
        await ChupAnhAsync("cv-multiple-pages"); await LuuVaKiemTraPDFAsync(soTrang);
        var ketQua = await database.CVs.AsNoTracking().SingleAsync(c => c.Id == cvId);
        kiemTra(ketQua.FullName == "Nguyễn Minh Anh" && ketQua.Certifications!.Contains("Chứng chỉ") && ketQua.Activities!.Contains("website"), "Vietnamese and newly editable CV fields save correctly");
        Console.WriteLine("PASS: six CV designs, drag/drop, layout persistence, mocked AI UI, Vietnamese and exact multipage PDFs");
    }
}
