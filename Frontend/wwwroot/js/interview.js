(() => {
    'use strict';
    const room = document.getElementById('interview-room');
    if (!room) return;
    const layPhanTu = id => document.getElementById('interview-' + id);
    const roomId = room.dataset.roomId;
    const polite = room.dataset.polite === 'true';
    const rtcConfig = JSON.parse(layPhanTu('rtc-config').textContent);
    const localVideo = layPhanTu('local'), remoteVideo = layPhanTu('remote');
    let connection, stream, peer, peerId, joined = false, busy = false, closed = false;
    let heartbeatTimer, recoveryTimer, durationTimer, lastHeartbeat = 0, connectedAt;
    let mediaVersion = 0;
    let remoteCamera = false, remoteMic = false;
    let signalQueue = Promise.resolve();

    function capNhatTrangThai(state, text) { room.dataset.callState = state; layPhanTu('status').textContent = text; }
    function hienThiThongBao(text, error = false) {
        layPhanTu('notice').textContent = text;
        layPhanTu('notice').className = 'alert ' + (error ? 'alert-warning' : 'alert-info');
        layPhanTu('notice').hidden = !text;
    }
    function layLuongThietBi(kind) { return stream?.getTracks().find(t => t.kind === kind && t.readyState === 'live'); }
    function dongBoThietBi() {
        for (const [kind, id] of [['audio', 'mic'], ['video', 'camera']]) {
            const on = Boolean(layLuongThietBi(kind)?.enabled);
            layPhanTu(id).setAttribute('aria-pressed', String(on));
            layPhanTu(id).textContent = (on ? 'Tắt ' : 'Bật ') + (kind === 'audio' ? 'micro' : 'camera');
        }
        layPhanTu('local-placeholder').hidden = Boolean(layLuongThietBi('video')?.enabled);
    }
    function dongBoNguoiConLai() {
        const hasVideo = remoteVideo.srcObject?.getVideoTracks().some(t => t.readyState === 'live' && !t.muted);
        layPhanTu('remote-placeholder').hidden = Boolean(remoteCamera && hasVideo);
        layPhanTu('remote-media').hidden = !peerId;
        layPhanTu('remote-media').textContent = [remoteMic ? 'Micro bật' : 'Micro tắt', remoteCamera ? 'Camera bật' : 'Camera tắt'].join(' · ');
    }
    function capNhatNutDieuKhien() {
        layPhanTu('join').hidden = joined || busy || closed;
        layPhanTu('join').disabled = busy;
        layPhanTu('leave').hidden = !joined;
        layPhanTu('preflight').hidden = joined;
        for (const id of ['mic', 'camera', 'reconnect']) layPhanTu(id).disabled = !joined || busy;
        dongBoThietBi();
    }
    function datLaiKetNoiDoiTac() {
        clearTimeout(recoveryTimer);
        if (peer) {
            peer.ontrack = peer.onicecandidate = peer.onnegotiationneeded = peer.onconnectionstatechange = peer.oniceconnectionstatechange = null;
            peer.close(); peer = null;
        }
        peerId = null;
        remoteCamera = remoteMic = false;
        remoteVideo.srcObject = null;
        layPhanTu('peer-status').textContent = 'Đang chờ người còn lại vào phòng.';
        dongBoNguoiConLai();
    }
    async function guiTinHieu(signal, destination = peerId) {
        if (!joined || !destination || connection?.state !== signalR.HubConnectionState.Connected) return;
        await connection.invoke('SendSignal', roomId, destination, signal);
    }
    function guiTrangThaiThietBi() {
        return guiTinHieu({ type: 'media', mic: Boolean(layLuongThietBi('audio')?.enabled), camera: Boolean(layLuongThietBi('video')?.enabled) });
    }
    function khoiPhucKetNoi() {
        if (!joined || !peerId) return;
        // ICE can restart while the existing media transport remains connected.
        capNhatTrangThai(peer?.connectionState === 'connected' ? 'connected' : 'connecting', 'Đang làm mới kết nối…');
        peer?.restartIce();
        clearTimeout(recoveryTimer);
        recoveryTimer = setTimeout(() => {
            if (!joined) return;
            if (peer?.connectionState === 'connected') capNhatTrangThai('connected', 'Đã kết nối với người phỏng vấn');
            else void roiPhong('Không kết nối được với người còn lại. Bạn có thể vào lại phòng; nếu gọi khác mạng, cần kiểm tra cấu hình TURN.', true);
        }, 30000);
    }
    function chuanBiKetNoiDoiTac(id) {
        if (!joined || !id || (id === peerId && peer && peer.connectionState !== 'closed')) return;
        datLaiKetNoiDoiTac(); peerId = id;
        const pc = peer = new RTCPeerConnection(rtcConfig);
        const negotiation = { makingOffer: false, ignoreOffer: false, settingAnswer: false, pendingIce: [] };
        pc.portalNegotiation = negotiation;
        for (const kind of ['audio', 'video']) {
            const current = layLuongThietBi(kind);
            if (current) pc.addTransceiver(current, { direction: 'sendrecv', streams: [stream] });
            else pc.addTransceiver(kind, { direction: 'sendrecv' });
        }
        const remote = new MediaStream(); remoteVideo.srcObject = remote;
        pc.ontrack = event => {
            if (pc !== peer) return;
            if (!remote.getTracks().some(t => t.id === event.track.id)) remote.addTrack(event.track);
            event.track.onmute = event.track.onunmute = event.track.onended = dongBoNguoiConLai;
            dongBoNguoiConLai();
            remoteVideo.play().catch(() => { layPhanTu('play-audio').hidden = false; });
        };
        pc.onicecandidate = ({ candidate }) => {
            if (!candidate || pc !== peer) return;
            void guiTinHieu({ type: 'candidate', candidate: candidate.candidate, sdpMid: candidate.sdpMid,
                sdpMLineIndex: candidate.sdpMLineIndex, usernameFragment: candidate.usernameFragment }, id).catch(() => {});
        };
        pc.onnegotiationneeded = async () => {
            try {
                negotiation.makingOffer = true;
                await pc.setLocalDescription();
                if (pc === peer) await guiTinHieu({ type: pc.localDescription.type, sdp: pc.localDescription.sdp }, id);
            } catch (error) { if (pc === peer && joined) hienThiThongBao('Đang thử thiết lập cuộc gọi. Nếu chờ lâu, chọn Kết nối lại.'); }
            finally { negotiation.makingOffer = false; }
        };
        pc.onconnectionstatechange = () => {
            if (pc !== peer || !joined) return;
            if (pc.connectionState === 'connected') {
                clearTimeout(recoveryTimer);
                capNhatTrangThai('connected', 'Đã kết nối với người phỏng vấn');
                hienThiThongBao('');
                layPhanTu('peer-status').textContent = 'Người còn lại đang tắt camera.';
                connectedAt ??= Date.now();
                dongBoNguoiConLai();
                void guiTrangThaiThietBi().catch(() => {});
            } else if (pc.connectionState === 'disconnected' || pc.connectionState === 'failed') {
                capNhatTrangThai('connecting', 'Kết nối bị gián đoạn…');
                clearTimeout(recoveryTimer);
                recoveryTimer = setTimeout(khoiPhucKetNoi, pc.connectionState === 'failed' ? 100 : 5000);
            }
        };
        pc.oniceconnectionstatechange = () => {
            if (pc === peer && joined && ['connected', 'completed'].includes(pc.iceConnectionState) && pc.connectionState === 'connected') {
                clearTimeout(recoveryTimer);
                capNhatTrangThai('connected', 'Đã kết nối với người phỏng vấn');
            }
        };
        capNhatTrangThai('connecting', 'Đang thiết lập kết nối…');
        layPhanTu('peer-status').textContent = 'Đang kết nối micro và camera…';
        recoveryTimer = setTimeout(khoiPhucKetNoi, 15000);
        void guiTrangThaiThietBi().catch(() => {});
    }
    async function nhanTinHieu(id, signal) {
        if (!joined || id !== peerId || !peer) return;
        if (signal.type === 'media') { remoteCamera = signal.camera; remoteMic = signal.mic; dongBoNguoiConLai(); return; }
        const pc = peer, n = pc.portalNegotiation;
        if (signal.type === 'offer' || signal.type === 'answer') {
            const ready = !n.makingOffer && (pc.signalingState === 'stable' || n.settingAnswer);
            n.ignoreOffer = !polite && signal.type === 'offer' && !ready;
            if (n.ignoreOffer) return;
            n.settingAnswer = signal.type === 'answer';
            try { await pc.setRemoteDescription({ type: signal.type, sdp: signal.sdp }); }
            finally { n.settingAnswer = false; }
            if (pc !== peer) return;
            for (const candidate of n.pendingIce.splice(0)) await pc.addIceCandidate(candidate);
            if (signal.type === 'offer') {
                await pc.setLocalDescription();
                await guiTinHieu({ type: pc.localDescription.type, sdp: pc.localDescription.sdp }, id);
            }
        } else if (signal.type === 'candidate' && !n.ignoreOffer) {
            const candidate = { candidate: signal.candidate, sdpMid: signal.sdpMid,
                sdpMLineIndex: signal.sdpMLineIndex, usernameFragment: signal.usernameFragment };
            if (pc.remoteDescription) await pc.addIceCandidate(candidate);
            else n.pendingIce.push(candidate);
        }
    }
    async function thamGiaPhien() {
        joined = true;
        const result = await connection.invoke('Join', roomId);
        if (!joined) return;
        if (result.peerId) chuanBiKetNoiDoiTac(result.peerId);
        else { capNhatTrangThai('waiting', 'Đã vào phòng · Chờ người còn lại'); layPhanTu('peer-status').textContent = 'Đang chờ người còn lại vào phòng.'; }
        lastHeartbeat = Date.now();
        clearInterval(heartbeatTimer);
        heartbeatTimer = setInterval(async () => {
            if (!joined) return;
            if (Date.now() - lastHeartbeat > 65000) { await roiPhong('Mất kết nối với máy chủ. Vui lòng vào lại phòng.', true); return; }
            if (connection.state !== signalR.HubConnectionState.Connected) return;
            try {
                const result = await connection.invoke('Heartbeat', roomId);
                lastHeartbeat = Date.now();
                if (result.peerId && result.peerId !== peerId) chuanBiKetNoiDoiTac(result.peerId);
                else if (!result.peerId && peerId) { datLaiKetNoiDoiTac(); capNhatTrangThai('waiting', 'Đang chờ người còn lại quay lại'); }
            } catch (error) { if (connection.state === signalR.HubConnectionState.Connected) await roiPhong('Phiên kết nối đã hết hạn hoặc quyền tham gia đã thay đổi. Vui lòng tải lại danh sách hồ sơ.', true); }
        }, 15000);
    }
    async function vaoPhong() {
        if (busy || joined || closed) return;
        if (!window.isSecureContext || !navigator.mediaDevices?.getUserMedia || !window.RTCPeerConnection) {
            hienThiThongBao('Cuộc gọi cần trình duyệt có hỗ trợ camera/micro và địa chỉ HTTPS (localhost có thể dùng HTTP).', true); return;
        }
        busy = true; capNhatNutDieuKhien(); hienThiThongBao(''); capNhatTrangThai('connecting', 'Đang chuẩn bị thiết bị…');
        const version = ++mediaVersion;
        try {
            const audio = layPhanTu('use-mic').checked, video = layPhanTu('use-camera').checked;
            const acquired = audio || video ? await navigator.mediaDevices.getUserMedia({
                audio: audio ? { echoCancellation: true, noiseSuppression: true } : false,
                video: video ? { width: { ideal: 1280 }, height: { ideal: 720 }, frameRate: { ideal: 24, max: 30 } } : false
            }) : new MediaStream();
            if (version !== mediaVersion) { acquired.getTracks().forEach(t => t.stop()); return; }
            stream = acquired; localVideo.srcObject = stream;
            for (const t of stream.getTracks()) t.onended = () => { dongBoThietBi(); void guiTrangThaiThietBi().catch(() => {}); hienThiThongBao('Một thiết bị đã ngắt. Bạn có thể bật lại micro hoặc camera.'); };
            connection = new signalR.HubConnectionBuilder().withUrl('/interviewHub').withAutomaticReconnect([0, 2000, 5000, 10000]).build();
            connection.on('PeerJoined', id => { chuanBiKetNoiDoiTac(id); });
            connection.on('PeerLeft', id => { if (id === peerId) { datLaiKetNoiDoiTac(); capNhatTrangThai('waiting', 'Người còn lại đã rời phòng'); } });
            connection.on('Signal', (id, signal) => {
                // The peer's first offer can arrive before our Join completion.
                if (joined && !peerId) chuanBiKetNoiDoiTac(id);
                signalQueue = signalQueue.then(() => nhanTinHieu(id, signal)).catch(() => {
                    if (joined) hienThiThongBao('Kết nối đang được điều chỉnh. Chọn Kết nối lại nếu cuộc gọi chưa hoạt động.');
                });
            });
            connection.on('SessionEnded', () => { closed = true; void roiPhong('Phiên phỏng vấn đã kết thúc. Bạn có thể trở về danh sách hồ sơ.'); });
            connection.on('AccessRevoked', () => { closed = true; void roiPhong('Phiên đã kết thúc hoặc quyền tham gia đã thay đổi. Vui lòng trở về danh sách hồ sơ.'); });
            connection.onreconnecting(() => { datLaiKetNoiDoiTac(); capNhatTrangThai('connecting', 'Mất kết nối máy chủ · Đang thử lại…'); });
            connection.onreconnected(async () => {
                if (!joined) return;
                try { await thamGiaPhien(); } catch { await roiPhong('Không thể vào lại phòng. Vui lòng tải lại danh sách hồ sơ.', true); }
            });
            connection.onclose(() => { if (joined) void roiPhong('Kết nối đã đóng. Bạn có thể vào lại phòng.', true); });
            await connection.start();
            await thamGiaPhien();
            connectedAt = null;
            durationTimer = setInterval(() => {
                if (connectedAt) {
                    const seconds = Math.floor((Date.now() - connectedAt) / 1000);
                    layPhanTu('duration').textContent = 'Thời gian kết nối ' + String(Math.floor(seconds / 60)).padStart(2, '0') + ':' + String(seconds % 60).padStart(2, '0');
                }
            }, 1000);
        } catch (error) {
            const messages = {
                NotAllowedError: 'Bạn chưa cho phép dùng micro/camera. Hãy cấp quyền trong trình duyệt hoặc bỏ chọn thiết bị rồi vào lại.',
                NotFoundError: 'Không tìm thấy micro hoặc camera đã chọn. Hãy bỏ chọn thiết bị không có rồi vào lại.',
                NotReadableError: 'Thiết bị đang bận hoặc không truy cập được. Hãy đóng ứng dụng đang dùng camera, hoặc bỏ chọn camera rồi vào lại.'
            };
            await roiPhong(messages[error.name] || 'Không thể vào phòng. Phòng có thể đã kết thúc hoặc đang mở ở cửa sổ khác. Hãy kiểm tra kết nối và tải lại danh sách hồ sơ.', true);
        } finally { busy = false; capNhatNutDieuKhien(); }
    }
    async function roiPhong(message = 'Bạn đã rời phòng. Có thể vào lại khi phiên vẫn còn mở.', error = false) {
        joined = false; ++mediaVersion;
        clearInterval(heartbeatTimer); clearInterval(durationTimer);
        datLaiKetNoiDoiTac();
        stream?.getTracks().forEach(t => t.stop()); stream = null; localVideo.srcObject = null;
        const current = connection; connection = null;
        if (current) {
            try { if (current.state === signalR.HubConnectionState.Connected) await current.invoke('Leave'); } catch { }
            try { await current.stop(); } catch { }
        }
        capNhatTrangThai(closed ? 'ended' : 'ready', closed ? 'Phiên phỏng vấn đã kết thúc' : 'Đã rời phòng');
        layPhanTu('duration').textContent = 'Micro và camera đã dừng.';
        layPhanTu('play-audio').hidden = true;
        capNhatNutDieuKhien(); hienThiThongBao(message, error);
    }
    async function batTatThietBi(kind) {
        if (!joined) return;
        const version = mediaVersion;
        const button = layPhanTu(kind === 'audio' ? 'mic' : 'camera'); button.disabled = true;
        try {
            let current = layLuongThietBi(kind);
            if (current) current.enabled = !current.enabled;
            else {
                const acquired = await navigator.mediaDevices.getUserMedia({ audio: kind === 'audio', video: kind === 'video' });
                if (!joined || version !== mediaVersion) { acquired.getTracks().forEach(t => t.stop()); return; }
                current = acquired.getTracks()[0]; stream.addTrack(current);
                current.onended = () => { dongBoThietBi(); void guiTrangThaiThietBi().catch(() => {}); };
                const transceiver = peer?.getTransceivers().find(t => t.receiver.track.kind === kind);
                if (transceiver) await transceiver.sender.replaceTrack(current);
            }
            dongBoThietBi(); await guiTrangThaiThietBi();
        } catch { hienThiThongBao('Không bật được thiết bị. Hãy kiểm tra kết nối thiết bị và quyền trong trình duyệt.', true); }
        finally { button.disabled = !joined; }
    }
    layPhanTu('join').addEventListener('click', () => void vaoPhong());
    layPhanTu('leave').addEventListener('click', () => void roiPhong());
    layPhanTu('mic').addEventListener('click', () => void batTatThietBi('audio'));
    layPhanTu('camera').addEventListener('click', () => void batTatThietBi('video'));
    layPhanTu('reconnect').addEventListener('click', khoiPhucKetNoi);
    layPhanTu('play-audio').addEventListener('click', () => remoteVideo.play().then(() => { layPhanTu('play-audio').hidden = true; }).catch(() => hienThiThongBao('Hãy cho phép phát âm thanh trong trình duyệt.', true)));
    window.addEventListener('pagehide', () => {
        joined = false; ++mediaVersion;
        stream?.getTracks().forEach(t => t.stop());
        peer?.close();
        void connection?.stop();
    });
    capNhatNutDieuKhien();
})();
