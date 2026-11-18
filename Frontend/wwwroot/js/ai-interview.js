(() => {
    "use strict";
    const phong = document.querySelector("[data-ai-room]");
    if (!phong) return;
    const nutGhi = phong.querySelector("[data-ai-record]");
    const nhanGhi = phong.querySelector("[data-ai-record-label]");
    const nutGui = phong.querySelector("[data-ai-submit]");
    const nutThuLai = phong.querySelector("[data-ai-retry]");
    const cauTraLoi = phong.querySelector("[data-ai-answer]");
    const trangThai = phong.querySelector("[data-ai-status]");
    const dongHo = phong.querySelector("[data-ai-timer]");
    const ngheLai = phong.querySelector("[data-ai-playback]");
    const maChongGiaMao = phong.querySelector('input[name="__RequestVerificationToken"]').value;
    const khoaBanNhap = `phong-van-ai:${phong.dataset.session}:${phong.dataset.number}`;
    const khoaNhanGiong = khoaBanNhap + ":nhan-giong";
    const khoaGuiDapAn = khoaBanNhap + ":gui-dap-an";
    let mayGhi = null, luongMic = null, xuLyAmThanh = null, boHenGio = null;
    let duLieuGhi = [], banGhi = null, duongDanNghe = null, batDauLuc = 0, dangXuLy = false, daRoiTrang = false;
    try { cauTraLoi.value = sessionStorage.getItem(khoaBanNhap) || ""; } catch { }

    function thongBao(noiDung, loi = false) {
        trangThai.textContent = noiDung;
        trangThai.dataset.error = String(loi);
    }
    function capNhatBanNhap() {
        phong.querySelector("[data-ai-count]").textContent = `${cauTraLoi.value.length.toLocaleString("vi-VN")} / 4.000 ký tự`;
        try { sessionStorage.setItem(khoaBanNhap, cauTraLoi.value); } catch { }
    }
    function capNhatTrangThai(ban) {
        dangXuLy = ban;
        phong.dataset.busy = String(ban);
        nutGhi.disabled = ban;
        nutGui.disabled = ban || mayGhi?.state === "recording";
        nutThuLai.disabled = ban;
        cauTraLoi.readOnly = ban;
        phong.querySelector("[data-ai-end] button").disabled = ban || mayGhi?.state === "recording";
    }
    function dungMicro() {
        luongMic?.getTracks().forEach(kenh => kenh.stop());
        luongMic = null;
        clearInterval(boHenGio);
        nutGhi.setAttribute("aria-pressed", "false");
        nhanGhi.textContent = "Ghi âm lại";
    }
    async function dongBoXuLyAmThanh() {
        const boXuLy = xuLyAmThanh;
        xuLyAmThanh = null;
        if (boXuLy && boXuLy.state !== "closed") { try { await boXuLy.close(); } catch { } }
    }
    async function guiYeuCau(diaChi, duLieu, laJson = false, phuongThuc = "POST", thoiGianCho = 310000) {
        const dieuKhien = new AbortController();
        const henGio = setTimeout(() => dieuKhien.abort(), thoiGianCho);
        try {
            const headers = { "X-CSRF-TOKEN": maChongGiaMao };
            if (laJson) headers["Content-Type"] = "application/json";
            const phanHoi = await fetch(diaChi, { method: phuongThuc, headers, body: phuongThuc === "GET" ? undefined : laJson ? JSON.stringify(duLieu) : duLieu, signal: dieuKhien.signal, cache: "no-store" });
            if (phanHoi.redirected || phanHoi.status === 401 || phanHoi.status === 403) throw new Error("Phiên đăng nhập đã hết hạn. Bạn đăng nhập lại rồi tiếp tục nhé.");
            const ketQua = await phanHoi.json().catch(() => ({}));
            if (!phanHoi.ok) {
                const loiCauNoi = {
                    500: "Máy chủ gặp lỗi khi xử lý (HTTP 500). Nội dung trả lời vẫn được giữ lại.",
                    502: "Máy chủ hoặc đường truyền Cloudflare đang gián đoạn (HTTP 502). Nội dung trả lời vẫn được giữ lại.",
                    504: "Máy chủ phản hồi quá lâu qua đường truyền trung gian (HTTP 504). Nội dung trả lời vẫn được giữ lại.",
                    524: "Cloudflare đã hết thời gian chờ máy chủ (HTTP 524). Nội dung trả lời vẫn được giữ lại."
                };
                const loi = new Error(ketQua.message || loiCauNoi[phanHoi.status] || "Chưa xử lý được yêu cầu. Bạn thử lại nhé.");
                loi.status = phanHoi.status;
                throw loi;
            }
            return ketQua;
        } catch (loi) {
            if (loi.name === "AbortError") throw new Error("Xử lý lâu hơn dự kiến. Nội dung vẫn còn ở đây để bạn thử lại.");
            if (loi instanceof TypeError) throw new Error("Mất kết nối. Bạn kiểm tra mạng rồi thử lại nhé.");
            throw loi;
        } finally { clearTimeout(henGio); }
    }
    async function choBanChu(diaChi) {
        const url = new URL(diaChi, location.origin);
        if (url.origin !== location.origin) throw new Error("Đường dẫn nhận giọng nói chưa hợp lệ.");
        try { sessionStorage.setItem(khoaNhanGiong, url.href); } catch { }
        const batDau = Date.now();
        while (!daRoiTrang && Date.now() - batDau < 16 * 60 * 1000) {
            let ketQua;
            try { ketQua = await guiYeuCau(url.href, null, false, "GET", 20000); }
            catch (loi) {
                if (loi.status === 404) { try { sessionStorage.removeItem(khoaNhanGiong); } catch { } }
                throw loi;
            }
            if (ketQua.status === "completed") {
                try { sessionStorage.removeItem(khoaNhanGiong); } catch { }
                return ketQua;
            }
            if (ketQua.status === "failed") {
                try { sessionStorage.removeItem(khoaNhanGiong); } catch { }
                throw new Error(ketQua.message || "Chưa nhận được giọng nói. Bạn thử lại nhé.");
            }
            thongBao(`Đang chuyển giọng nói thành chữ… (${Math.floor((Date.now() - batDau) / 1000)} giây). Bạn có thể đợi ở trang này.`);
            await new Promise(hoanTat => setTimeout(hoanTat, 2000));
        }
        if (daRoiTrang) return null;
        throw new Error("Nhận giọng nói lâu hơn dự kiến. Bạn thử bản ghi ngắn hơn nhé.");
    }
    function dienBanChu(ketQua) {
        if (!ketQua) return;
        cauTraLoi.value = ketQua.text;
        capNhatBanNhap();
        thongBao("Đã nhận giọng nói. Bạn kiểm tra, chỉnh lại bản chữ nếu cần rồi gửi câu trả lời.");
        cauTraLoi.focus();
    }
    async function nhanGiongNoi() {
        if (!banGhi || dangXuLy) return;
        capNhatTrangThai(true);
        nutThuLai.hidden = true;
        thongBao("Đang chuyển giọng nói thành chữ…");
        try {
            let diaChiCho = null;
            try { diaChiCho = sessionStorage.getItem(khoaNhanGiong); } catch { }
            if (diaChiCho) { dienBanChu(await choBanChu(diaChiCho)); return; }
            const bieuMau = new FormData();
            bieuMau.append("audio", banGhi, "cau-tra-loi.wav");
            const ketQua = await guiYeuCau(phong.dataset.speechUrl, bieuMau);
            dienBanChu(ketQua.statusUrl ? await choBanChu(ketQua.statusUrl) : ketQua);
        } catch (loi) {
            thongBao(loi.message, true);
            nutThuLai.hidden = false;
        } finally { capNhatTrangThai(false); }
    }
    function taoTepWav(mau) {
        const duLieu = new ArrayBuffer(44 + mau.length * 2);
        const khung = new DataView(duLieu);
        function ghiChuoi(viTri, chuoi) { for (let i = 0; i < chuoi.length; i++) khung.setUint8(viTri + i, chuoi.charCodeAt(i)); }
        ghiChuoi(0, "RIFF"); khung.setUint32(4, duLieu.byteLength - 8, true); ghiChuoi(8, "WAVEfmt ");
        khung.setUint32(16, 16, true); khung.setUint16(20, 1, true); khung.setUint16(22, 1, true);
        khung.setUint32(24, 16000, true); khung.setUint32(28, 32000, true); khung.setUint16(32, 2, true); khung.setUint16(34, 16, true);
        ghiChuoi(36, "data"); khung.setUint32(40, mau.length * 2, true);
        for (let i = 0; i < mau.length; i++) {
            const giaTri = Math.max(-1, Math.min(1, mau[i]));
            khung.setInt16(44 + i * 2, Math.round(giaTri * (giaTri < 0 ? 32768 : 32767)), true);
        }
        return new Blob([duLieu], { type: "audio/wav" });
    }
    async function hoanTatBanGhi() {
        dungMicro();
        if (daRoiTrang) { await dongBoXuLyAmThanh(); return; }
        capNhatTrangThai(true);
        thongBao("Đang chuẩn bị bản ghi…");
        try {
            const tep = new Blob(duLieuGhi, { type: mayGhi.mimeType });
            if (tep.size === 0 || tep.size > 8 * 1024 * 1024) throw new Error("Bản ghi chưa hợp lệ. Bạn ghi âm lại nhé.");
            const amThanh = await xuLyAmThanh.decodeAudioData(await tep.arrayBuffer());
            if (amThanh.duration < .5) throw new Error("Bản ghi quá ngắn. Bạn nói ít nhất một giây nhé.");
            const soMau = Math.min(Math.floor(amThanh.duration * 16000), 120 * 16000);
            const ngoaiTuyen = new OfflineAudioContext(1, soMau, 16000);
            const nguon = ngoaiTuyen.createBufferSource();
            nguon.buffer = amThanh; nguon.connect(ngoaiTuyen.destination); nguon.start();
            const ketQua = await ngoaiTuyen.startRendering();
            banGhi = taoTepWav(ketQua.getChannelData(0));
            if (duongDanNghe) URL.revokeObjectURL(duongDanNghe);
            duongDanNghe = URL.createObjectURL(banGhi);
            ngheLai.src = duongDanNghe; ngheLai.hidden = false;
        } catch (loi) {
            thongBao(loi.message || "Trình duyệt chưa đọc được bản ghi. Bạn thử ghi lại hoặc nhập câu trả lời nhé.", true);
            return;
        } finally { await dongBoXuLyAmThanh(); capNhatTrangThai(false); }
        await nhanGiongNoi();
    }
    async function batTatGhiAm() {
        if (dangXuLy) return;
        if (mayGhi?.state === "recording") { mayGhi.stop(); dungMicro(); capNhatTrangThai(true); return; }
        if (!window.isSecureContext || !navigator.mediaDevices?.getUserMedia || !window.MediaRecorder) {
            thongBao("Micro cần trình duyệt hỗ trợ và đường dẫn HTTPS (hoặc localhost). Bạn có thể nhập câu trả lời ở bên dưới.", true); return;
        }
        capNhatTrangThai(true);
        try {
            const LopAmThanh = window.AudioContext || window.webkitAudioContext;
            xuLyAmThanh = new LopAmThanh();
            await xuLyAmThanh.resume();
            luongMic = await navigator.mediaDevices.getUserMedia({ audio: { echoCancellation: true, noiseSuppression: true }, video: false });
            if (daRoiTrang) { dungMicro(); await dongBoXuLyAmThanh(); return; }
            const dinhDang = ["audio/webm;codecs=opus", "audio/mp4", "audio/webm"].find(muc => MediaRecorder.isTypeSupported(muc));
            mayGhi = new MediaRecorder(luongMic, dinhDang ? { mimeType: dinhDang } : undefined);
            duLieuGhi = [];
            mayGhi.addEventListener("dataavailable", suKien => { if (suKien.data.size) duLieuGhi.push(suKien.data); });
            mayGhi.addEventListener("stop", hoanTatBanGhi, { once: true });
            mayGhi.addEventListener("error", () => { dungMicro(); thongBao("Ghi âm bị gián đoạn. Bạn thử lại nhé.", true); capNhatTrangThai(false); });
            mayGhi.start(1000); batDauLuc = Date.now();
            nutThuLai.hidden = true;
            nutGhi.setAttribute("aria-pressed", "true"); nhanGhi.textContent = "Dừng ghi âm";
            thongBao("Đang ghi âm. Bấm dừng khi bạn trả lời xong.");
            boHenGio = setInterval(() => {
                const giay = Math.min(120, Math.floor((Date.now() - batDauLuc) / 1000));
                dongHo.textContent = `${String(Math.floor(giay / 60)).padStart(2, "0")}:${String(giay % 60).padStart(2, "0")} / 02:00`;
                if (giay >= 120 && mayGhi.state === "recording") { mayGhi.stop(); dungMicro(); capNhatTrangThai(true); }
            }, 250);
        } catch (loi) {
            dungMicro(); await dongBoXuLyAmThanh();
            thongBao(loi.name === "NotAllowedError" ? "Bạn chưa cho phép dùng micro. Mở quyền micro của trình duyệt rồi thử lại nhé." : "Chưa mở được micro. Bạn kiểm tra thiết bị rồi thử lại nhé.", true);
        } finally { capNhatTrangThai(false); }
    }
    async function guiCauTraLoi() {
        if (dangXuLy || mayGhi?.state === "recording") return;
        const congViec = layCongViecGuiDapAn();
        if (!congViec && cauTraLoi.value.trim().length < 2) { thongBao("Bạn cần có câu trả lời trước khi gửi.", true); cauTraLoi.focus(); return; }
        capNhatTrangThai(true); ngheLai.pause();
        thongBao("AI đang xem câu trả lời và chuẩn bị lượt tiếp theo…");
        try {
            if (congViec) { await choChamDapAn(congViec.statusUrl, congViec.batDauLuc); return; }
            const ketQua = await guiYeuCau(phong.dataset.submitUrl, { number: Number(phong.dataset.number), answer: cauTraLoi.value.trim() }, true, "POST", 20000);
            if (ketQua.statusUrl) await choChamDapAn(ketQua.statusUrl);
            else moLuotTiepTheo(ketQua);
        } catch (loi) { hienThiLoiGuiDapAn(loi); capNhatTrangThai(false); }
    }
    function layCongViecGuiDapAn() {
        try {
            const congViec = JSON.parse(sessionStorage.getItem(khoaGuiDapAn) || "null");
            return congViec?.statusUrl ? congViec : null;
        } catch { return null; }
    }
    function xoaCongViecGuiDapAn() {
        try { sessionStorage.removeItem(khoaGuiDapAn); } catch { }
    }
    function hienThiLoiGuiDapAn(loi) {
        const huongDan = layCongViecGuiDapAn() ? " Bấm Gửi câu trả lời để tiếp tục kiểm tra kết quả; câu trả lời sẽ không bị gửi lại." : "";
        thongBao(loi.message + huongDan, true);
    }
    function moLuotTiepTheo(ketQua) {
        if (typeof ketQua?.url !== "string" || !ketQua.url.trim()) throw new Error("Máy chủ chưa trả về đường dẫn tiếp tục. Nội dung trả lời vẫn được giữ lại.");
        const diaChi = new URL(ketQua.url, location.origin);
        if (diaChi.origin !== location.origin) throw new Error("Đường dẫn tiếp tục chưa hợp lệ. Nội dung trả lời vẫn được giữ lại.");
        try { sessionStorage.removeItem(khoaBanNhap); } catch { }
        xoaCongViecGuiDapAn();
        location.assign(diaChi.href);
    }
    async function choChamDapAn(diaChi, batDau = Date.now()) {
        const url = new URL(diaChi, location.origin);
        if (url.origin !== location.origin) throw new Error("Đường dẫn kiểm tra câu trả lời chưa hợp lệ.");
        if (!Number.isFinite(batDau) || batDau > Date.now()) batDau = Date.now();
        try { sessionStorage.setItem(khoaGuiDapAn, JSON.stringify({ statusUrl: url.href, batDauLuc: batDau })); } catch { }
        while (!daRoiTrang && Date.now() - batDau < 20 * 60 * 1000) {
            let ketQua;
            const conLai = 20 * 60 * 1000 - (Date.now() - batDau);
            try { ketQua = await guiYeuCau(url.href, null, false, "GET", Math.max(1, Math.min(20000, conLai))); }
            catch (loi) {
                if (loi.status === 404 || loi.status === 410) xoaCongViecGuiDapAn();
                throw loi;
            }
            if (ketQua.status === "completed") { moLuotTiepTheo(ketQua); return; }
            if (ketQua.status === "failed") {
                xoaCongViecGuiDapAn();
                throw new Error(ketQua.message || "AI chưa chấm được câu trả lời. Nội dung vẫn còn để bạn thử lại.");
            }
            if (ketQua.status !== "pending" && ketQua.status !== "running") throw new Error("Trạng thái xử lý chưa hợp lệ. Nội dung trả lời vẫn được giữ lại.");
            thongBao(`AI đang chấm câu trả lời và chuẩn bị lượt tiếp theo… (${Math.floor((Date.now() - batDau) / 1000)} giây). Bạn có thể tải lại trang để tiếp tục chờ.`);
            await new Promise(hoanTat => setTimeout(hoanTat, Math.max(0, Math.min(2000, 20 * 60 * 1000 - (Date.now() - batDau)))));
        }
        if (!daRoiTrang) throw new Error("AI xử lý lâu hơn 20 phút. Nội dung trả lời vẫn được giữ lại; bạn tải lại trang để kiểm tra phiên phỏng vấn.");
    }
    nutGhi.addEventListener("click", batTatGhiAm);
    nutThuLai.addEventListener("click", nhanGiongNoi);
    nutGui.addEventListener("click", guiCauTraLoi);
    cauTraLoi.addEventListener("input", capNhatBanNhap);
    document.addEventListener("visibilitychange", () => { if (document.hidden && mayGhi?.state === "recording") { mayGhi.stop(); dungMicro(); } });
    window.addEventListener("pagehide", () => {
        daRoiTrang = true;
        if (mayGhi?.state === "recording") mayGhi.stop();
        dungMicro(); ngheLai.pause();
        if (duongDanNghe) URL.revokeObjectURL(duongDanNghe);
    });
    window.addEventListener("pageshow", suKien => { if (suKien.persisted) location.reload(); });
    capNhatBanNhap();
    const dangGuiDapAn = layCongViecGuiDapAn();
    let dangNhan = null;
    try { dangNhan = sessionStorage.getItem(khoaNhanGiong); } catch { }
    if (dangGuiDapAn) {
        capNhatTrangThai(true);
        thongBao("Đang kiểm tra kết quả câu trả lời đã gửi…");
        choChamDapAn(dangGuiDapAn.statusUrl, dangGuiDapAn.batDauLuc)
            .catch(hienThiLoiGuiDapAn).finally(() => capNhatTrangThai(false));
    } else if (dangNhan) {
        capNhatTrangThai(true);
        choBanChu(dangNhan).then(dienBanChu).catch(loi => thongBao(loi.message, true)).finally(() => capNhatTrangThai(false));
    }
})();
