using System.Text.Json;
using JobPortal.Data;
using Microsoft.EntityFrameworkCore;

static class AiInterviewBrowserChecks
{
    public static async Task ChayKiemTraAsync(string storage, string origin, Cdp page, ApplicationDbContext db, FakeOllamaServer ai, Action<bool, string> kiemTra)
    {
        await page.MoTrangAsync(origin + "/Candidate/AiInterview");
        await page.ThucThiJavaScriptAsync("document.querySelector('[data-ai-start] button').click()");
        await page.ChoDieuKienAsync("!!document.querySelector('[data-ai-room]')");
        var room = (await page.ThucThiJavaScriptAsync("location.href")).GetString()!;
        await page.GoiLenhAsync("Emulation.setDeviceMetricsOverride", new { width = 1440, height = 1000, deviceScaleFactor = 1, mobile = false });
        var roomShot = await page.GoiLenhAsync("Page.captureScreenshot", new { format = "png" });
        await File.WriteAllBytesAsync(Path.Combine(storage, "ai-room-desktop.png"), Convert.FromBase64String(roomShot.GetProperty("data").GetString()!));
        var id = Guid.Parse(room.Split('/').Last());
        kiemTra((await page.ThucThiJavaScriptAsync("document.querySelector('[data-ai-question]').textContent.includes('SQL')")).GetBoolean(), "Backend generated question is displayed");
        foreach (var width in new[] { 1440, 768, 390, 320 })
        {
            await page.GoiLenhAsync("Emulation.setDeviceMetricsOverride", new { width, height = 900, deviceScaleFactor = 1, mobile = width < 800 });
            kiemTra((await page.ThucThiJavaScriptAsync("document.documentElement.scrollWidth<=innerWidth+1")).GetBoolean(), "AI room fits viewport " + width);
        }
        await page.GoiLenhAsync("Page.bringToFront");
        // Keep the actual MediaRecorder, AudioContext, resampling and WAV encoder. Only ASR text is deterministic.
        await page.ThucThiJavaScriptAsync("(()=>{const original=window.fetch.bind(window);window.testSpeechPolls=0;window.fetch=async(url,options)=>{if(String(url).includes('AiInterviewSpeech/Transcribe')){const b=options.body.get('audio');const a=await b.arrayBuffer();const v=new DataView(a);window.testWav={size:b.size,rate:v.getUint32(24,true),channels:v.getUint16(22,true),format:v.getUint16(20,true)};return new Response(JSON.stringify({statusUrl:'/Candidate/AiInterviewSpeech/Status/test'}),{status:202,headers:{'Content-Type':'application/json'}})}if(String(url).includes('AiInterviewSpeech/Status/test')){window.testSpeechPolls++;return new Response(JSON.stringify(window.testSpeechPolls===1?{status:'running'}:{status:'completed',text:'Tôi đo thời gian truy vấn và kiểm tra kế hoạch thực thi SQL.'}),{status:200,headers:{'Content-Type':'application/json'}})}return original(url,options)}})()");
        await page.ThucThiJavaScriptAsync("document.querySelector('[data-ai-record]').click()");
        await page.ChoDieuKienAsync("document.querySelector('[data-ai-record]').getAttribute('aria-pressed')==='true'");
        await Task.Delay(1400);
        await page.ThucThiJavaScriptAsync("document.querySelector('[data-ai-record]').click()");
        await page.ChoDieuKienAsync("document.querySelector('[data-ai-answer]').value.includes('kế hoạch thực thi') && !document.querySelector('[data-ai-submit]').disabled", 30);
        kiemTra((await page.ThucThiJavaScriptAsync("testWav.rate===16000 && testWav.channels===1 && testWav.format===1 && testWav.size>16044")).GetBoolean(), "Microphone recording produces backend-compatible PCM WAV");
        kiemTra((await page.ThucThiJavaScriptAsync("testSpeechPolls===2 && !Object.keys(sessionStorage).some(k=>k.endsWith(':nhan-giong'))")).GetBoolean(), "Speech polls short requests until complete and clears the pending job");
        kiemTra((await page.ThucThiJavaScriptAsync("!document.querySelector('[data-ai-playback]').hidden")).GetBoolean(), "Candidate can listen to the recording before sending");
        await page.ThucThiJavaScriptAsync("document.querySelector('[data-ai-answer]').dispatchEvent(new Event('input'))");
        await page.MoTrangAsync(room);
        kiemTra((await page.ThucThiJavaScriptAsync("document.querySelector('[data-ai-answer]').value.includes('kế hoạch thực thi')")).GetBoolean(), "Text draft survives reload");
        var transport = await page.GoiLenhAsync("Page.addScriptToEvaluateOnNewDocument", new
        {
            source = """
                (() => {
                    const goc = window.fetch.bind(window);
                    window.fetch = async (url, options) => {
                        const cheDo = sessionStorage.getItem('testAnswerTransport');
                        if (String(url).includes('AiInterviewAnswer/Submit') && cheDo === 'cloudflare')
                            return new Response('<html>Cloudflare timeout</html>', { status: 524, headers: { 'Content-Type': 'text/html' } });
                        if (String(url).includes('AiInterviewAnswer/Submit') && cheDo === 'pending') {
                            sessionStorage.setItem('testAnswerPosts', String(Number(sessionStorage.getItem('testAnswerPosts') || 0) + 1));
                            return new Response(JSON.stringify({ statusUrl: '/Candidate/AiInterviewAnswer/Status/test-resume' }), { status: 202, headers: { 'Content-Type': 'application/json' } });
                        }
                        if (String(url).includes('AiInterviewAnswer/Status/test-resume')) {
                            sessionStorage.setItem('testAnswerPolls', String(Number(sessionStorage.getItem('testAnswerPolls') || 0) + 1));
                            if (cheDo === 'network') return new Response('<html>Gateway timeout</html>', { status: 504, headers: { 'Content-Type': 'text/html' } });
                            return new Response(JSON.stringify(cheDo === 'failed'
                                ? { status: 'failed', message: 'Lỗi thử nghiệm: câu trả lời vẫn được giữ lại.' }
                                : { status: 'running' }), { status: 200, headers: { 'Content-Type': 'application/json' } });
                        }
                        return goc(url, options);
                    };
                })();
                """
        });
        await page.ThucThiJavaScriptAsync("sessionStorage.setItem('testAnswerTransport','cloudflare')");
        await page.MoTrangAsync(room);
        await page.ThucThiJavaScriptAsync("document.querySelector('[data-ai-submit]').click()");
        await page.ChoDieuKienAsync("document.querySelector('[data-ai-status]').textContent.includes('HTTP 524') && !document.querySelector('[data-ai-submit]').disabled");
        kiemTra((await page.ThucThiJavaScriptAsync("document.querySelector('[data-ai-answer]').value.includes('kế hoạch thực thi')")).GetBoolean(), "HTML Cloudflare timeout is explained and preserves the draft");
        await page.ThucThiJavaScriptAsync("sessionStorage.setItem('testAnswerTransport','pending');document.querySelector('[data-ai-submit]').click()");
        await page.ChoDieuKienAsync("Number(sessionStorage.getItem('testAnswerPolls'))>=1 && document.querySelector('[data-ai-submit]').disabled");
        kiemTra((await page.ThucThiJavaScriptAsync("document.querySelector('[data-ai-answer]').readOnly && document.querySelector('[data-ai-record]').disabled && document.querySelector('[data-ai-end] button').disabled")).GetBoolean(), "Pending answer locks recording, editing and ending the interview");
        var polls = (await page.ThucThiJavaScriptAsync("Number(sessionStorage.getItem('testAnswerPolls'))")).GetInt32();
        await page.MoTrangAsync(room);
        await page.ChoDieuKienAsync($"Number(sessionStorage.getItem('testAnswerPolls'))>{polls} && document.querySelector('[data-ai-submit]').disabled");
        kiemTra((await page.ThucThiJavaScriptAsync("sessionStorage.getItem('testAnswerPosts')==='1' && document.querySelector('[data-ai-answer]').value.includes('kế hoạch thực thi')")).GetBoolean(), "Reload resumes polling the saved answer job without submitting it again");
        await page.ThucThiJavaScriptAsync("sessionStorage.setItem('testAnswerTransport','network')");
        await page.ChoDieuKienAsync("document.querySelector('[data-ai-status]').textContent.includes('HTTP 504') && !document.querySelector('[data-ai-submit]').disabled");
        kiemTra((await page.ThucThiJavaScriptAsync("Object.keys(sessionStorage).some(k=>k.endsWith(':gui-dap-an')) && document.querySelector('[data-ai-answer]').value.includes('kế hoạch thực thi')")).GetBoolean(), "Interrupted polling keeps the job and draft for a safe retry");
        await page.ThucThiJavaScriptAsync("sessionStorage.setItem('testAnswerTransport','failed');document.querySelector('[data-ai-submit]').click()");
        await page.ChoDieuKienAsync("document.querySelector('[data-ai-status]').textContent.includes('Lỗi thử nghiệm') && !document.querySelector('[data-ai-submit]').disabled");
        kiemTra((await page.ThucThiJavaScriptAsync("sessionStorage.getItem('testAnswerPosts')==='1' && !Object.keys(sessionStorage).some(k=>k.endsWith(':gui-dap-an')) && document.querySelector('[data-ai-answer]').value.includes('kế hoạch thực thi')")).GetBoolean(), "Retry checks the existing job and clears it only after a terminal failure");
        await page.GoiLenhAsync("Page.removeScriptToEvaluateOnNewDocument", new { identifier = transport.GetProperty("identifier").GetString() });
        await page.ThucThiJavaScriptAsync("sessionStorage.removeItem('testAnswerTransport');sessionStorage.removeItem('testAnswerPosts');sessionStorage.removeItem('testAnswerPolls')");
        await page.MoTrangAsync(room);
        ai.FailNext = true;
        await page.ThucThiJavaScriptAsync("document.querySelector('[data-ai-submit]').click()");
        await page.ChoDieuKienAsync("document.querySelector('[data-ai-status]').dataset.error==='true' && !document.querySelector('[data-ai-submit]').disabled");
        kiemTra((await page.ThucThiJavaScriptAsync("document.querySelector('[data-ai-answer]').value.includes('kế hoạch thực thi')")).GetBoolean(), "AI connection failure preserves the candidate answer");
        kiemTra(await db.AiInterviewTurns.CountAsync(t => t.SessionId == id && t.AnsweredAt != null) == 0, "Failed evaluation does not persist a half-completed answer");
        for (var number = 1; number <= 5; number++)
        {
            await page.ThucThiJavaScriptAsync("(()=>{const a=document.querySelector('[data-ai-answer]');a.value='Tôi đo thời gian truy vấn và kiểm tra kế hoạch thực thi SQL.';a.dispatchEvent(new Event('input'));document.querySelector('[data-ai-submit]').click()})()");
            await page.ChoDieuKienAsync(number == 5 ? "location.pathname.includes('AiInterviewReport/Details') && document.readyState==='complete'" : $"document.querySelector('[data-ai-room]')?.dataset.number==='{number + 1}'");
            kiemTra(await db.AiInterviewTurns.CountAsync(t => t.SessionId == id && t.AnsweredAt != null) == number, "Answer saved and next turn displayed: " + number);
        }
        kiemTra((await page.ThucThiJavaScriptAsync("document.querySelectorAll('.ai-report-turn').length===5 && document.querySelector('.ai-score strong').textContent==='78'")).GetBoolean(), "Final report displays five evaluations and server-calculated average");
        foreach (var width in new[] { 1440, 390, 320 })
        {
            await page.GoiLenhAsync("Emulation.setDeviceMetricsOverride", new { width, height = 900, deviceScaleFactor = 1, mobile = width < 800 });
            kiemTra((await page.ThucThiJavaScriptAsync("document.documentElement.scrollWidth<=innerWidth+1")).GetBoolean(), "Report fits viewport " + width);
        }
        var shot = await page.GoiLenhAsync("Page.captureScreenshot", new { format = "png" });
        await File.WriteAllBytesAsync(Path.Combine(storage, "ai-report-phone.png"), Convert.FromBase64String(shot.GetProperty("data").GetString()!));
        kiemTra(page.Errors.Count == 0, "No unhandled JavaScript errors: " + string.Join(";", page.Errors));
    }
}
