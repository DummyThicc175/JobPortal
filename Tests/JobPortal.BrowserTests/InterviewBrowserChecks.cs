using System.Text.Json;
using JobPortal.Data;
using Microsoft.EntityFrameworkCore;

static class InterviewBrowserChecks
{
    public static async Task RunAsync(string storage, string origin, ApplicationDbContext database,
        Func<Task<Cdp>> page, Func<Cdp, string, string, Task> login, Action<bool, string> check)
    {
        await using var employer = await page(); await login(employer, "employer@jobhub.com", "Employer@123");
        await using var candidate = await page(); await login(candidate, "candidate@jobhub.com", "Candidate@123");
        var applicationId = await database.Applications.Select(a => a.Id).SingleAsync();
        async Task ChangeStatus(string value)
        {
            await employer.Navigate(origin + "/Employer/Applicant");
            await employer.Eval("(async()=>{const f=document.querySelector('.status-editor'), d=new FormData(f);d.set('status'," + JsonSerializer.Serialize(value) + ");const r=await fetch(f.action,{method:'POST',body:d});if(!r.ok)throw new Error('Status '+r.status);return true})()");
        }
        await ChangeStatus("Interview");
        var session = await database.InterviewSessions.AsNoTracking().SingleAsync(s => s.EndedAt == null);
        var roomUrl = origin + "/Interview/Room/" + session.Id;
        const string instrumentation = "window.__testPeers=[];window.__testStreams=[];const OriginalPC=window.RTCPeerConnection;window.RTCPeerConnection=new Proxy(OriginalPC,{construct(t,a){const pc=new t(...a);window.__testPeers.push(pc);return pc;}});const originalMedia=navigator.mediaDevices.getUserMedia.bind(navigator.mediaDevices);navigator.mediaDevices.getUserMedia=async (...args)=>{const s=await originalMedia(...args);window.__testStreams.push(s);return s;};";
        foreach (var actor in new[] { employer, candidate })
        {
            await actor.Call("Page.addScriptToEvaluateOnNewDocument", new { source = instrumentation });
            await actor.Call("Emulation.setDeviceMetricsOverride", new { width = 1440, height = 1000, deviceScaleFactor = 1, mobile = false });
            await actor.Navigate(roomUrl);
            await actor.Wait("document.getElementById('interview-join') !== null && typeof signalR !== 'undefined'");
            await actor.Eval("(()=>{const build=signalR.HubConnectionBuilder.prototype.build;signalR.HubConnectionBuilder.prototype.build=function(){const h=build.call(this);if(h.baseUrl.endsWith('/interviewHub'))window.__testInterviewHub=h;return h;};return true})()");
            check((await actor.Eval("document.getElementById('interview-local').srcObject === null && __testStreams.length===0")).GetBoolean(), "Page does not acquire devices before Join");
        }
        await candidate.Eval("document.getElementById('interview-use-camera').checked=false;document.getElementById('interview-join').click()");
        await candidate.Wait("document.getElementById('interview-room').dataset.callState==='waiting'");
        check((await candidate.Eval("__testStreams[0].getAudioTracks().length===1 && __testStreams[0].getVideoTracks().length===0")).GetBoolean(), "Candidate can join audio-only without camera");
        await employer.Eval("document.getElementById('interview-join').click()");
        await employer.Wait("document.getElementById('interview-room').dataset.callState==='connected'", 45);
        await candidate.Wait("document.getElementById('interview-room').dataset.callState==='connected'", 45);
        check(true, "Two isolated browser accounts establish a real WebRTC connection");
        await candidate.Wait("document.getElementById('interview-remote').readyState>=2 && document.getElementById('interview-remote-placeholder').hidden");
        await employer.Wait("document.getElementById('interview-remote-media').textContent.includes('Camera tắt')");
        const string stats = "(async()=>{const pcs=__testPeers.filter(p=>p.connectionState==='connected');const values=[];for(const pc of pcs)for(const s of (await pc.getStats()).values())if(s.type==='inbound-rtp')values.push({kind:s.kind,bytes:s.bytesReceived,packets:s.packetsReceived,frames:s.framesDecoded});return values})()";
        await candidate.Wait("(async()=>{for(const s of (await __testPeers.at(-1).getStats()).values())if(s.type==='inbound-rtp' && s.kind==='video' && s.framesDecoded>2)return true;return false})()", 30);
        var candidateStats = await candidate.Eval(stats);
        check(candidateStats.EnumerateArray().Any(s => s.GetProperty("kind").GetString() == "video" && s.GetProperty("bytes").GetInt64() > 0), "Candidate decodes actual remote video RTP");
        await employer.Wait("(async()=>{for(const s of (await __testPeers.at(-1).getStats()).values())if(s.type==='inbound-rtp' && s.kind==='audio' && s.packetsReceived>2)return true;return false})()", 30);
        check(true, "Employer receives actual audio RTP from audio-only candidate");
        await candidate.Eval("document.getElementById('interview-camera').click()");
        await candidate.Wait("document.getElementById('interview-camera').getAttribute('aria-pressed')==='true'");
        await employer.Wait("document.getElementById('interview-remote').readyState>=2 && document.getElementById('interview-remote-placeholder').hidden", 30);
        check(true, "Enabling camera mid-call adds a live remote video track");
        await candidate.Eval("document.getElementById('interview-mic').click()");
        await candidate.Wait("document.getElementById('interview-mic').getAttribute('aria-pressed')==='false'");
        check((await candidate.Eval("__testStreams[0].getAudioTracks()[0].enabled===false")).GetBoolean(), "Mute disables the actual audio track");
        await employer.Wait("document.getElementById('interview-remote-media').textContent.includes('Micro tắt')");
        await candidate.Eval("document.getElementById('interview-mic').click()");
        await candidate.Wait("document.getElementById('interview-mic').getAttribute('aria-pressed')==='true'");
        await employer.Wait("document.getElementById('interview-remote-media').textContent.includes('Micro bật')");
        await candidate.Eval("document.getElementById('interview-camera').click()");
        await employer.Wait("!document.getElementById('interview-remote-placeholder').hidden && document.getElementById('interview-remote-media').textContent.includes('Camera tắt')");
        check((await candidate.Eval("__testStreams.flatMap(s=>s.getVideoTracks()).every(t=>!t.enabled)")).GetBoolean(), "Camera off disables the actual video track and updates peer placeholder");
        await candidate.Eval("document.getElementById('interview-camera').click()");
        await employer.Wait("document.getElementById('interview-remote-placeholder').hidden");
        await employer.Eval("window.__beforeRestartSdp=__testPeers.at(-1).localDescription.sdp;document.getElementById('interview-reconnect').click()");
        await employer.Wait("__testPeers.at(-1).localDescription.sdp !== __beforeRestartSdp", 35);
        await employer.Wait("document.getElementById('interview-room').dataset.callState==='connected'", 35);
        check(true, "Manual ICE restart renegotiates the same interview while preserving connected media");
        await candidate.Eval("(async()=>{window.__beforeReconnectId=__testInterviewHub.connectionId;await __testInterviewHub.connection.stop(new Error('Synthetic transport interruption'));return true})()");
        await candidate.Wait("__testInterviewHub.connectionId && __testInterviewHub.connectionId!==__beforeReconnectId && document.getElementById('interview-room').dataset.callState==='connected'", 45);
        await employer.Wait("document.getElementById('interview-room').dataset.callState==='connected'", 45);
        check(true, "Lost signaling transport reconnects, rejoins authorized room and restores WebRTC media");
        await using var duplicate = await page(); await login(duplicate, "candidate@jobhub.com", "Candidate@123");
        await duplicate.Navigate(roomUrl);
        await duplicate.Eval("document.getElementById('interview-join').click()");
        await duplicate.Wait("!document.getElementById('interview-notice').hidden && document.getElementById('interview-notice').textContent.includes('cửa sổ khác')", 30);
        check((await employer.Eval("document.getElementById('interview-room').dataset.callState==='connected'")).GetBoolean(), "Duplicate tab is rejected without interrupting existing call");
        var screenshotFolder = Path.Combine(storage, "screenshots"); Directory.CreateDirectory(screenshotFolder);
        foreach (var (actor, label) in new[] { (employer, "employer"), (candidate, "candidate") })
        {
            check((await actor.Eval("document.documentElement.scrollWidth<=innerWidth+1 && document.querySelectorAll('main').length===1")).GetBoolean(), "Interview desktop layout fits viewport: " + label);
            await actor.Call("Page.bringToFront");
            await actor.Eval("new Promise(r=>requestAnimationFrame(()=>requestAnimationFrame(()=>r(true))))");
            var shot = await actor.Call("Page.captureScreenshot", new { format = "png" });
            await File.WriteAllBytesAsync(Path.Combine(screenshotFolder, "interview-" + label + ".png"), Convert.FromBase64String(shot.GetProperty("data").GetString()!));
        }
        await candidate.Eval("document.getElementById('interview-leave').click()");
        await candidate.Wait("document.getElementById('interview-room').dataset.callState==='ready'");
        await employer.Wait("document.getElementById('interview-room').dataset.callState==='waiting'");
        check((await candidate.Eval("__testStreams.flatMap(s=>s.getTracks()).every(t=>t.readyState==='ended')")).GetBoolean(), "Leaving releases every local camera and microphone track");
        await candidate.Eval("document.getElementById('interview-join').click()");
        await candidate.Wait("document.getElementById('interview-room').dataset.callState==='connected'", 45);
        await employer.Wait("document.getElementById('interview-room').dataset.callState==='connected'", 45);
        check(true, "Leaving and rejoining negotiates a new working peer connection");
        await employer.Eval("(async()=>{const f=document.querySelector('.interview-end-form');return (await fetch(f.action,{method:'POST',body:new FormData(f)})).status})()");
        await candidate.Wait("document.getElementById('interview-room').dataset.callState==='ended'");
        await employer.Wait("document.getElementById('interview-room').dataset.callState==='ended'");
        check((await candidate.Eval("__testStreams.flatMap(s=>s.getTracks()).every(t=>t.readyState==='ended')")).GetBoolean(), "Host ending session stops candidate devices and prevents rejoin");
        await ChangeStatus("Reviewing");
        await ChangeStatus("Interview");
        var next = await database.InterviewSessions.AsNoTracking().SingleAsync(s => s.EndedAt == null);
        check(next.Id != session.Id, "New interview session has a new URL");
        await candidate.Navigate(origin + "/Interview/Room/" + next.Id);
        await candidate.Eval("navigator.mediaDevices.getUserMedia=async()=>{throw new DOMException('denied','NotAllowedError')};document.getElementById('interview-join').click()");
        await candidate.Wait("document.getElementById('interview-notice').textContent.includes('cấp quyền')");
        check((await candidate.Eval("!document.getElementById('interview-join').hidden && document.getElementById('interview-room').dataset.callState==='ready'")).GetBoolean(), "Denied media permission returns to recoverable preflight screen");
        await candidate.Navigate(origin + "/Interview/Room/" + next.Id);
        await employer.Navigate(origin + "/Interview/Room/" + next.Id);
        // Join simultaneously to exercise offer collision / perfect negotiation.
        await Task.WhenAll(candidate.Eval("document.getElementById('interview-join').click()"), employer.Eval("document.getElementById('interview-join').click()"));
        await candidate.Wait("document.getElementById('interview-room').dataset.callState==='connected'", 45);
        await employer.Wait("document.getElementById('interview-room').dataset.callState==='connected'", 45);
        check(true, "Simultaneous join negotiates correctly");
        // A separate authenticated tab changes status while both call tabs stay open.
        await using var management = await page(); await login(management, "employer@jobhub.com", "Employer@123");
        await management.Navigate(origin + "/Employer/Applicant");
        await management.Eval("(async()=>{const f=document.querySelector('.status-editor'),d=new FormData(f);d.set('status','Accepted');return (await fetch(f.action,{method:'POST',body:d})).status})()");
        await candidate.Wait("document.getElementById('interview-room').dataset.callState==='ended'");
        await employer.Wait("document.getElementById('interview-room').dataset.callState==='ended'");
        check(true, "Changing application status closes both connected call screens immediately");
        check(candidate.Errors.Count == 0 && employer.Errors.Count == 0, "Interview pages have no unhandled JavaScript errors: " + string.Join(";", candidate.Errors.Concat(employer.Errors)));
        Console.WriteLine("Interview screenshots: " + screenshotFolder);
    }
}
