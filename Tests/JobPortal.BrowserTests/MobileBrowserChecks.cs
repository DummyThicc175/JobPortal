using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using JobPortal.Data;
using Microsoft.EntityFrameworkCore;

static class MobileBrowserChecks
{
    public static async Task ChayKiemTraAsync(string storage,string origin,ApplicationDbContext database,int jobId,Cdp anonymous,Cdp candidate,Cdp employer,Cdp admin,Action<bool,string> kiemTra)
    {
        var thuMuc=Path.Combine(storage,"screenshots"); Directory.CreateDirectory(thuMuc);
        async Task ChupAnhAsync(Cdp trang,string ten)
        {
            await trang.GoiLenhAsync("Page.bringToFront");
            var anh=await trang.GoiLenhAsync("Page.captureScreenshot",new {format="png",captureBeyondViewport=false});
            await File.WriteAllBytesAsync(Path.Combine(thuMuc,ten+".png"),Convert.FromBase64String(anh.GetProperty("data").GetString()!));
        }
        async Task DoiKhungAsync(Cdp trang,int width=390,int height=844)
        {
            await trang.GoiLenhAsync("Emulation.setDeviceMetricsOverride",new {width,height,deviceScaleFactor=1,mobile=true});
            await trang.GoiLenhAsync("Emulation.setTouchEmulationEnabled",new {enabled=true});
        }
        async Task ChamAsync(Cdp trang,string boChon)
        {
            await trang.GoiLenhAsync("Page.bringToFront");
            await Task.Delay(350);
            var toaDo=await trang.ThucThiJavaScriptAsync("(async()=>{const e=document.querySelector("+JsonSerializer.Serialize(boChon)+");e.scrollIntoView({block:'center'});await new Promise(r=>requestAnimationFrame(()=>requestAnimationFrame(r)));const r=e.getBoundingClientRect();return{x:r.x+r.width/2,y:r.y+r.height/2}})()");
            await trang.ThucThiJavaScriptAsync("document.addEventListener('pointerdown',e=>window.portalTestPointer=e.pointerType,{once:true,capture:true})");
            await trang.GoiLenhAsync("Input.dispatchTouchEvent",new {type="touchStart",timestamp=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()/1000.0,touchPoints=new[]{new {x=toaDo.GetProperty("x").GetDouble(),y=toaDo.GetProperty("y").GetDouble(),id=1,force=1}}});
            await Task.Delay(80);
            await trang.GoiLenhAsync("Input.dispatchTouchEvent",new {type="touchEnd",timestamp=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()/1000.0,touchPoints=Array.Empty<object>()});
            await trang.ThucThiJavaScriptAsync("new Promise(r=>requestAnimationFrame(()=>requestAnimationFrame(()=>r(true))))");
            if((await trang.ThucThiJavaScriptAsync("window.portalTestPointer")).GetString()!="touch") throw new InvalidOperationException("Expected an actual touch pointer for "+boChon);
        }
        async Task KiemTraKhongTranAsync(Cdp trang,string ten)
        {
            var kichThuoc=await trang.ThucThiJavaScriptAsync("({width:innerWidth,scroll:document.documentElement.scrollWidth})");
            kiemTra(kichThuoc.GetProperty("scroll").GetInt32()<=kichThuoc.GetProperty("width").GetInt32()+1,ten+" fits viewport: "+kichThuoc);
        }
        try
        {
            foreach(var (trang,duongDan,ten) in new[]{(candidate,"/Candidate/Profile","candidate"),(employer,"/Employer/Applicant","employer"),(admin,"/Admin/UserManager","admin")})
            {
                await DoiKhungAsync(trang); await trang.MoTrangAsync(origin+duongDan);
                kiemTra((await trang.ThucThiJavaScriptAsync("document.querySelector('.workspace-sidebar').inert")).GetBoolean(),"Closed mobile menu cannot receive hidden keyboard focus: "+ten);
                await ChamAsync(trang,"[data-sidebar-toggle]");
                await trang.ChoDieuKienAsync("document.body.classList.contains('sidebar-open')");
                kiemTra((await trang.ThucThiJavaScriptAsync("document.querySelector('.workspace-main').inert && !document.querySelector('.workspace-sidebar').inert && getComputedStyle(document.body).overflow==='hidden'")).GetBoolean(),"Mobile menu isolates background and locks page scroll: "+ten);
                await ChupAnhAsync(trang,"phone-menu-"+ten);
                await ChamAsync(trang,"[data-sidebar-close]"); await trang.ChoDieuKienAsync("!document.body.classList.contains('sidebar-open')");
                kiemTra((await trang.ThucThiJavaScriptAsync("document.activeElement.matches('[data-sidebar-toggle]') && !document.querySelector('.workspace-main').inert")).GetBoolean(),"Close menu restores the page and trigger focus: "+ten);
                kiemTra((await trang.ThucThiJavaScriptAsync("[...document.querySelectorAll('tbody td')].every(e=>e.dataset.label && e.getAttribute('role')==='cell') && getComputedStyle(document.querySelector('tbody tr')).display==='block'")).GetBoolean(),"Phone tables become labeled cards with preserved table semantics: "+ten);
                kiemTra((await trang.ThucThiJavaScriptAsync("[...document.querySelectorAll('tbody .btn,tbody .icon-button')].filter(e=>!e.disabled).every(e=>e.getBoundingClientRect().height>=43.9)")).GetBoolean(),"Card action buttons have usable touch height: "+ten);
                await KiemTraKhongTranAsync(trang,ten); await ChupAnhAsync(trang,"phone-cards-"+ten);
            }
            await candidate.MoTrangAsync(origin+"/Candidate/Profile/Edit");
            kiemTra((await candidate.ThucThiJavaScriptAsync("[...document.querySelectorAll('input:not([type=hidden]):not([type=file]),select')].every(e=>parseFloat(getComputedStyle(e).fontSize)>=16)")).GetBoolean(),"Profile inputs stay readable without iPhone focus zoom");
            await candidate.MoTrangAsync(origin+"/Candidate/Profile"); await candidate.ChoDieuKienAsync("window.portalChatStarted===true");
            await ChamAsync(candidate,"#chat-bubble"); await candidate.ChoDieuKienAsync("document.body.classList.contains('chat-open')");
            kiemTra((await candidate.ThucThiJavaScriptAsync("(()=>{const r=document.getElementById('chat-window').getBoundingClientRect();return r.width<=innerWidth+1 && r.height>=visualViewport.height-2 && document.getElementById('chat-bubble').getAttribute('aria-expanded')==='true'})()")).GetBoolean(),"Mobile chat opens as a full-height conversation");
            await candidate.ChoDieuKienAsync("document.querySelector('#partner-list button')!==null"); await ChamAsync(candidate,"#partner-list button");
            await candidate.ChoDieuKienAsync("!document.getElementById('chat-input').disabled");
            await DoiKhungAsync(candidate,390,430);
            await candidate.ChoDieuKienAsync("(()=>{const r=document.querySelector('#chat-window .chat-compose').getBoundingClientRect();return r.bottom<=visualViewport.height+visualViewport.offsetTop+1 && r.top>=0})()");
            kiemTra(true,"Chat composer remains inside a reduced viewport"); await ChupAnhAsync(candidate,"phone-chat-reduced-viewport");
            await ChamAsync(candidate,"#chat-window button[aria-label='Đóng tin nhắn']");
            kiemTra((await candidate.ThucThiJavaScriptAsync("!document.body.classList.contains('chat-open') && document.getElementById('chat-bubble').getAttribute('aria-expanded')==='false'")).GetBoolean(),"Closing chat releases the page on phone");
            await DoiKhungAsync(candidate,844,390); await ChamAsync(candidate,"#chat-bubble");
            await candidate.ChoDieuKienAsync("document.body.classList.contains('chat-open')");
            kiemTra((await candidate.ThucThiJavaScriptAsync("(()=>{const r=document.getElementById('chat-window').getBoundingClientRect(),c=document.querySelector('#chat-window .chat-compose').getBoundingClientRect();return r.width>=innerWidth-2 && c.bottom<=visualViewport.height+1})()")).GetBoolean(),"Landscape phone chat uses the available screen and keeps composer visible");
            kiemTra((await candidate.ThucThiJavaScriptAsync("parseFloat(getComputedStyle(document.getElementById('chat-input')).fontSize)>=16")).GetBoolean(),"Phone landscape inputs retain readable font size");
            await ChupAnhAsync(candidate,"phone-chat-landscape"); await ChamAsync(candidate,"#chat-window button[aria-label='Đóng tin nhắn']");
            await DoiKhungAsync(candidate);
            await candidate.MoTrangAsync(origin+"/Candidate/CV/Create?templateId=4");
            await candidate.ChoDieuKienAsync("document.querySelectorAll('[data-cv-mobile-view]').length===2 && document.querySelectorAll('[data-block-key]').length===9");
            kiemTra((await candidate.ThucThiJavaScriptAsync("getComputedStyle(document.querySelector('.cv-studio-inspector-body')).overflowY==='visible' && !document.querySelector('.cv-studio-preview').getClientRects().length")).GetBoolean(),"Phone CV editor uses page scrolling and a separate preview view");
            await candidate.ThucThiJavaScriptAsync("(()=>{for(const[id,value]of Object.entries({inName:'Nguyễn Minh Anh',inJob:'Lập trình viên .NET',inEmail:'candidate@example.test',inSummary:'Thiết kế, phát triển và kiểm thử ứng dụng tuyển dụng. '.repeat(65)})){const e=document.getElementById(id);e.value=value;e.dispatchEvent(new Event('input'));}})()");
            await candidate.ThucThiJavaScriptAsync("(()=>{if(document.querySelectorAll('.exp-item').length<2)themMucKinhNghiem();for(const [i,e]of [...document.querySelectorAll('.exp-desc')].entries()){e.value='Phát triển ứng dụng, phân tích yêu cầu và kiểm thử với nhóm dự án. '.repeat(45);e.dispatchEvent(new Event('input'));document.querySelectorAll('.exp-comp')[i].value='Công ty '+(i+1);}})()");
            await ChamAsync(candidate,"[data-cv-mobile-view=preview]");
            await candidate.ChoDieuKienAsync("document.querySelector('.cv-studio-preview').getClientRects().length>0 && document.querySelectorAll('.cv-page').length>1");
            kiemTra((await candidate.ThucThiJavaScriptAsync("(()=>{const p=document.querySelector('#cv-preview-viewport'),s=document.querySelector('#cv-preview-scaler');return s.getBoundingClientRect().width<=p.clientWidth && !document.querySelector('.cv-studio-inspector').getClientRects().length})()")).GetBoolean(),"Phone A4 preview fits the screen and leaves document layout intact");
            var soTrang=(await candidate.ThucThiJavaScriptAsync("document.querySelectorAll('.cv-page').length")).GetInt32();
            await ChupAnhAsync(candidate,"phone-cv-preview");
            await ChamAsync(candidate,"[data-cv-mobile-view=edit]"); await ChamAsync(candidate,"#cv-tab-layout");
            await candidate.ThucThiJavaScriptAsync("document.getElementById('cv-layout-main').scrollIntoView({block:'start'});window.scrollBy(0,-160)");
            var haiKhoi=await candidate.ThucThiJavaScriptAsync("(()=>{const a=document.querySelector('[data-block-key=experience] .cv-drag-handle').getBoundingClientRect(),b=document.querySelector('[data-block-key=summary]').getBoundingClientRect();return {sx:a.x+a.width/2,sy:a.y+a.height/2,tx:b.x+30,ty:b.y+30}})()");
            var sx=haiKhoi.GetProperty("sx").GetDouble(); var sy=haiKhoi.GetProperty("sy").GetDouble(); var tx=haiKhoi.GetProperty("tx").GetDouble(); var ty=haiKhoi.GetProperty("ty").GetDouble();
            await candidate.GoiLenhAsync("Input.dispatchTouchEvent",new {type="touchStart",touchPoints=new[]{new{x=sx,y=sy}}});
            for(var i=1;i<=6;i++) await candidate.GoiLenhAsync("Input.dispatchTouchEvent",new {type="touchMove",touchPoints=new[]{new{x=sx+(tx-sx)*i/6,y=sy+(ty-sy)*i/6}}});
            await candidate.GoiLenhAsync("Input.dispatchTouchEvent",new {type="touchEnd",touchPoints=Array.Empty<object>()});
            await candidate.ChoDieuKienAsync("document.querySelector('#cv-layout-main [data-block-key]').dataset.blockKey==='experience'");
            kiemTra(true,"Real touch drag reorders CV content blocks");
            await Task.Delay(1200);
            await ChamAsync(candidate,"[data-block-key=experience] [data-layout-action=down]");
            await candidate.ChoDieuKienAsync("document.querySelector('#cv-layout-main [data-block-key]').dataset.blockKey==='summary'");
            kiemTra(true,"Touch buttons provide an alternative to dragging");
            await ChupAnhAsync(candidate,"phone-cv-layout");
            await ChamAsync(candidate,"#btnSaveCV");
            await candidate.ChoDieuKienAsync("!document.getElementById('btnSaveCV').disabled",90);
            var thongBao=(await candidate.ThucThiJavaScriptAsync("document.getElementById('cvSaveStatus').textContent")).GetString();
            kiemTra(thongBao!.StartsWith("Đã lưu"),"Phone saves a real PDF while preview is hidden: "+thongBao);
            var cv=await database.CVs.AsNoTracking().SingleAsync(c=>!c.IsSnapshot && c.CandidateId=="d83b7113-a8f8-4035-99a7-a20dd400f6a4");
            var tep=await File.ReadAllBytesAsync(Path.Combine(storage,"cvs",cv.FileName!));
            kiemTra(Regex.Matches(Encoding.Latin1.GetString(tep),@"/Type\s*/Page\b").Count==soTrang,"Phone PDF has the same pages as its A4 preview");
            await candidate.GoiLenhAsync("Page.reload"); await candidate.ChoDieuKienAsync("document.getElementById('inName')?.value==='Nguyễn Minh Anh'");
            kiemTra((await candidate.ThucThiJavaScriptAsync("document.getElementById('inSummary').value.startsWith('Thiết kế')")).GetBoolean(),"Phone form content persists after reopening");
            foreach(var (width,height) in new[]{(320,740),(430,932),(844,390),(768,1024)})
            {
                await DoiKhungAsync(candidate,width,height); await ChamAsync(candidate,"[data-cv-mobile-view=preview]");
                await candidate.ChoDieuKienAsync("document.querySelector('#cv-preview-scaler').getBoundingClientRect().width<=document.querySelector('#cv-preview-viewport').clientWidth+1");
                await KiemTraKhongTranAsync(candidate,"CV "+width+"x"+height);
                await ChupAnhAsync(candidate,"phone-cv-"+width+"x"+height);
            }
            await candidate.GoiLenhAsync("Emulation.setDeviceMetricsOverride",new {width=1440,height=1000,deviceScaleFactor=1,mobile=false});
            await candidate.ChoDieuKienAsync("document.querySelector('.cv-studio-inspector').getClientRects().length>0 && document.querySelector('.cv-studio-preview').getClientRects().length>0");
            kiemTra(true,"Returning to desktop restores simultaneous CV editing and preview");
            await DoiKhungAsync(anonymous,320,740); await anonymous.MoTrangAsync(origin+"/Identity/Account/Login");
            await KiemTraKhongTranAsync(anonymous,"Small-phone authentication"); await ChupAnhAsync(anonymous,"phone-login-320");
            await DoiKhungAsync(anonymous,844,390); await anonymous.MoTrangAsync(origin+"/Job/Details/"+jobId);
            await KiemTraKhongTranAsync(anonymous,"Landscape public job details");
            await DoiKhungAsync(admin); await admin.MoTrangAsync(origin+"/Admin/JobManager/Pending");
            await ChamAsync(admin,"[data-job-details]"); await admin.ChoDieuKienAsync("document.getElementById('job-modal-data').hidden===false");
            kiemTra((await admin.ThucThiJavaScriptAsync("(()=>{const r=document.querySelector('#job-details-modal .modal-content').getBoundingClientRect();return r.width<=innerWidth && r.height<=visualViewport.height})()")).GetBoolean(),"Job review dialog fits a phone viewport");
            await ChupAnhAsync(admin,"phone-job-dialog");
            Console.WriteLine("PASS: real touch navigation, phone cards, chat viewport, touch CV drag, hidden-preview PDF save, portrait/landscape and desktop restoration");
        }
        catch
        {
            foreach(var (trang,ten) in new[]{(candidate,"candidate"),(employer,"employer"),(admin,"admin"),(anonymous,"anonymous")})
                try { await ChupAnhAsync(trang,"mobile-failed-"+ten); } catch { }
            throw;
        }
    }
}
