(() => {
    'use strict';
    const room = document.getElementById('interview-room');
    if (!room) return;
    const el = id => document.getElementById('interview-' + id);
    const roomId = room.dataset.roomId;
    const polite = room.dataset.polite === 'true';
    const rtcConfig = JSON.parse(el('rtc-config').textContent);
    const localVideo = el('local'), remoteVideo = el('remote');
    let connection, stream, peer, peerId, joined = false, busy = false, closed = false;
    let heartbeatTimer, recoveryTimer, durationTimer, lastHeartbeat = 0, connectedAt;
    let mediaVersion = 0;
    let remoteCamera = false, remoteMic = false;
    let signalQueue = Promise.resolve();

    function status(state, text) { room.dataset.callState = state; el('status').textContent = text; }
    function notice(text, error = false) {
        el('notice').textContent = text;
        el('notice').className = 'alert ' + (error ? 'alert-warning' : 'alert-info');
        el('notice').hidden = !text;
    }
    function track(kind) { return stream?.getTracks().find(t => t.kind === kind && t.readyState === 'live'); }
    function syncMedia() {
        for (const [kind, id] of [['audio', 'mic'], ['video', 'camera']]) {
            const on = Boolean(track(kind)?.enabled);
            el(id).setAttribute('aria-pressed', String(on));
            el(id).textContent = (on ? 'Tắt ' : 'Bật ') + (kind === 'audio' ? 'micro' : 'camera');
        }
        el('local-placeholder').hidden = Boolean(track('video')?.enabled);
    }
    function syncRemote() {
        const hasVideo = remoteVideo.srcObject?.getVideoTracks().some(t => t.readyState === 'live' && !t.muted);
        el('remote-placeholder').hidden = Boolean(remoteCamera && hasVideo);
        el('remote-media').hidden = !peerId;
        el('remote-media').textContent = [remoteMic ? 'Micro bật' : 'Micro tắt', remoteCamera ? 'Camera bật' : 'Camera tắt'].join(' · ');
    }
    function controls() {
        el('join').hidden = joined || busy || closed;
        el('join').disabled = busy;
        el('leave').hidden = !joined;
        el('preflight').hidden = joined;
        for (const id of ['mic', 'camera', 'reconnect']) el(id).disabled = !joined || busy;
        syncMedia();
    }
    function resetPeer() {
        clearTimeout(recoveryTimer);
        if (peer) {
            peer.ontrack = peer.onicecandidate = peer.onnegotiationneeded = peer.onconnectionstatechange = peer.oniceconnectionstatechange = null;
            peer.close(); peer = null;
        }
        peerId = null;
        remoteCamera = remoteMic = false;
        remoteVideo.srcObject = null;
        el('peer-status').textContent = 'Đang chờ người còn lại vào phòng.';
        syncRemote();
    }
    async function send(signal, destination = peerId) {
        if (!joined || !destination || connection?.state !== signalR.HubConnectionState.Connected) return;
        await connection.invoke('SendSignal', roomId, destination, signal);
    }
    function sendMedia() {
        return send({ type: 'media', mic: Boolean(track('audio')?.enabled), camera: Boolean(track('video')?.enabled) });
    }
    function recover() {
        if (!joined || !peerId) return;
        // ICE can restart while the existing media transport remains connected.
        status(peer?.connectionState === 'connected' ? 'connected' : 'connecting', 'Đang làm mới kết nối…');
        peer?.restartIce();
        clearTimeout(recoveryTimer);
        recoveryTimer = setTimeout(() => {
            if (!joined) return;
            if (peer?.connectionState === 'connected') status('connected', 'Đã kết nối với người phỏng vấn');
            else void leave('Không kết nối được với người còn lại. Bạn có thể vào lại phòng; nếu gọi khác mạng, cần kiểm tra cấu hình TURN.', true);
        }, 30000);
    }
    function preparePeer(id) {
        if (!joined || !id || (id === peerId && peer && peer.connectionState !== 'closed')) return;
        resetPeer(); peerId = id;
        const pc = peer = new RTCPeerConnection(rtcConfig);
        const negotiation = { makingOffer: false, ignoreOffer: false, settingAnswer: false, pendingIce: [] };
        pc.portalNegotiation = negotiation;
        for (const kind of ['audio', 'video']) {
            const current = track(kind);
            if (current) pc.addTransceiver(current, { direction: 'sendrecv', streams: [stream] });
            else pc.addTransceiver(kind, { direction: 'sendrecv' });
        }
        const remote = new MediaStream(); remoteVideo.srcObject = remote;
        pc.ontrack = event => {
            if (pc !== peer) return;
            if (!remote.getTracks().some(t => t.id === event.track.id)) remote.addTrack(event.track);
            event.track.onmute = event.track.onunmute = event.track.onended = syncRemote;
            syncRemote();
            remoteVideo.play().catch(() => { el('play-audio').hidden = false; });
        };
        pc.onicecandidate = ({ candidate }) => {
            if (!candidate || pc !== peer) return;
            void send({ type: 'candidate', candidate: candidate.candidate, sdpMid: candidate.sdpMid,
                sdpMLineIndex: candidate.sdpMLineIndex, usernameFragment: candidate.usernameFragment }, id).catch(() => {});
        };
        pc.onnegotiationneeded = async () => {
            try {
                negotiation.makingOffer = true;
                await pc.setLocalDescription();
                if (pc === peer) await send({ type: pc.localDescription.type, sdp: pc.localDescription.sdp }, id);
            } catch (error) { if (pc === peer && joined) notice('Đang thử thiết lập cuộc gọi. Nếu chờ lâu, chọn Kết nối lại.'); }
            finally { negotiation.makingOffer = false; }
        };
        pc.onconnectionstatechange = () => {
            if (pc !== peer || !joined) return;
            if (pc.connectionState === 'connected') {
                clearTimeout(recoveryTimer);
                status('connected', 'Đã kết nối với người phỏng vấn');
                notice('');
                el('peer-status').textContent = 'Người còn lại đang tắt camera.';
                connectedAt ??= Date.now();
                syncRemote();
                void sendMedia().catch(() => {});
            } else if (pc.connectionState === 'disconnected' || pc.connectionState === 'failed') {
                status('connecting', 'Kết nối bị gián đoạn…');
                clearTimeout(recoveryTimer);
                recoveryTimer = setTimeout(recover, pc.connectionState === 'failed' ? 100 : 5000);
            }
        };
        pc.oniceconnectionstatechange = () => {
            if (pc === peer && joined && ['connected', 'completed'].includes(pc.iceConnectionState) && pc.connectionState === 'connected') {
                clearTimeout(recoveryTimer);
                status('connected', 'Đã kết nối với người phỏng vấn');
            }
        };
        status('connecting', 'Đang thiết lập kết nối…');
        el('peer-status').textContent = 'Đang kết nối micro và camera…';
        recoveryTimer = setTimeout(recover, 15000);
        void sendMedia().catch(() => {});
    }
    async function receive(id, signal) {
        if (!joined || id !== peerId || !peer) return;
        if (signal.type === 'media') { remoteCamera = signal.camera; remoteMic = signal.mic; syncRemote(); return; }
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
                await send({ type: pc.localDescription.type, sdp: pc.localDescription.sdp }, id);
            }
        } else if (signal.type === 'candidate' && !n.ignoreOffer) {
            const candidate = { candidate: signal.candidate, sdpMid: signal.sdpMid,
                sdpMLineIndex: signal.sdpMLineIndex, usernameFragment: signal.usernameFragment };
            if (pc.remoteDescription) await pc.addIceCandidate(candidate);
            else n.pendingIce.push(candidate);
        }
    }
    async function enter() {
        joined = true;
        const result = await connection.invoke('Join', roomId);
        if (!joined) return;
        if (result.peerId) preparePeer(result.peerId);
        else { status('waiting', 'Đã vào phòng · Chờ người còn lại'); el('peer-status').textContent = 'Đang chờ người còn lại vào phòng.'; }
        lastHeartbeat = Date.now();
        clearInterval(heartbeatTimer);
        heartbeatTimer = setInterval(async () => {
            if (!joined) return;
            if (Date.now() - lastHeartbeat > 65000) { await leave('Mất kết nối với máy chủ. Vui lòng vào lại phòng.', true); return; }
            if (connection.state !== signalR.HubConnectionState.Connected) return;
            try {
                const result = await connection.invoke('Heartbeat', roomId);
                lastHeartbeat = Date.now();
                if (result.peerId && result.peerId !== peerId) preparePeer(result.peerId);
                else if (!result.peerId && peerId) { resetPeer(); status('waiting', 'Đang chờ người còn lại quay lại'); }
            } catch (error) { if (connection.state === signalR.HubConnectionState.Connected) await leave('Phiên kết nối đã hết hạn hoặc quyền tham gia đã thay đổi. Vui lòng tải lại danh sách hồ sơ.', true); }
        }, 15000);
    }
    async function join() {
        if (busy || joined || closed) return;
        if (!window.isSecureContext || !navigator.mediaDevices?.getUserMedia || !window.RTCPeerConnection) {
            notice('Cuộc gọi cần trình duyệt có hỗ trợ camera/micro và địa chỉ HTTPS (localhost có thể dùng HTTP).', true); return;
        }
        busy = true; controls(); notice(''); status('connecting', 'Đang chuẩn bị thiết bị…');
        const version = ++mediaVersion;
        try {
            const audio = el('use-mic').checked, video = el('use-camera').checked;
            const acquired = audio || video ? await navigator.mediaDevices.getUserMedia({
                audio: audio ? { echoCancellation: true, noiseSuppression: true } : false,
                video: video ? { width: { ideal: 1280 }, height: { ideal: 720 }, frameRate: { ideal: 24, max: 30 } } : false
            }) : new MediaStream();
            if (version !== mediaVersion) { acquired.getTracks().forEach(t => t.stop()); return; }
            stream = acquired; localVideo.srcObject = stream;
            for (const t of stream.getTracks()) t.onended = () => { syncMedia(); void sendMedia().catch(() => {}); notice('Một thiết bị đã ngắt. Bạn có thể bật lại micro hoặc camera.'); };
            connection = new signalR.HubConnectionBuilder().withUrl('/interviewHub').withAutomaticReconnect([0, 2000, 5000, 10000]).build();
            connection.on('PeerJoined', id => { preparePeer(id); });
            connection.on('PeerLeft', id => { if (id === peerId) { resetPeer(); status('waiting', 'Người còn lại đã rời phòng'); } });
            connection.on('Signal', (id, signal) => {
                // The peer's first offer can arrive before our Join completion.
                if (joined && !peerId) preparePeer(id);
                signalQueue = signalQueue.then(() => receive(id, signal)).catch(() => {
                    if (joined) notice('Kết nối đang được điều chỉnh. Chọn Kết nối lại nếu cuộc gọi chưa hoạt động.');
                });
            });
            connection.on('SessionEnded', () => { closed = true; void leave('Phiên phỏng vấn đã kết thúc. Bạn có thể trở về danh sách hồ sơ.'); });
            connection.on('AccessRevoked', () => { closed = true; void leave('Phiên đã kết thúc hoặc quyền tham gia đã thay đổi. Vui lòng trở về danh sách hồ sơ.'); });
            connection.onreconnecting(() => { resetPeer(); status('connecting', 'Mất kết nối máy chủ · Đang thử lại…'); });
            connection.onreconnected(async () => {
                if (!joined) return;
                try { await enter(); } catch { await leave('Không thể vào lại phòng. Vui lòng tải lại danh sách hồ sơ.', true); }
            });
            connection.onclose(() => { if (joined) void leave('Kết nối đã đóng. Bạn có thể vào lại phòng.', true); });
            await connection.start();
            await enter();
            connectedAt = null;
            durationTimer = setInterval(() => {
                if (connectedAt) {
                    const seconds = Math.floor((Date.now() - connectedAt) / 1000);
                    el('duration').textContent = 'Thời gian kết nối ' + String(Math.floor(seconds / 60)).padStart(2, '0') + ':' + String(seconds % 60).padStart(2, '0');
                }
            }, 1000);
        } catch (error) {
            const messages = {
                NotAllowedError: 'Bạn chưa cho phép dùng micro/camera. Hãy cấp quyền trong trình duyệt hoặc bỏ chọn thiết bị rồi vào lại.',
                NotFoundError: 'Không tìm thấy micro hoặc camera đã chọn. Hãy bỏ chọn thiết bị không có rồi vào lại.',
                NotReadableError: 'Thiết bị đang bận hoặc không truy cập được. Hãy đóng ứng dụng đang dùng camera, hoặc bỏ chọn camera rồi vào lại.'
            };
            await leave(messages[error.name] || 'Không thể vào phòng. Phòng có thể đã kết thúc hoặc đang mở ở cửa sổ khác. Hãy kiểm tra kết nối và tải lại danh sách hồ sơ.', true);
        } finally { busy = false; controls(); }
    }
    async function leave(message = 'Bạn đã rời phòng. Có thể vào lại khi phiên vẫn còn mở.', error = false) {
        joined = false; ++mediaVersion;
        clearInterval(heartbeatTimer); clearInterval(durationTimer);
        resetPeer();
        stream?.getTracks().forEach(t => t.stop()); stream = null; localVideo.srcObject = null;
        const current = connection; connection = null;
        if (current) {
            try { if (current.state === signalR.HubConnectionState.Connected) await current.invoke('Leave'); } catch { }
            try { await current.stop(); } catch { }
        }
        status(closed ? 'ended' : 'ready', closed ? 'Phiên phỏng vấn đã kết thúc' : 'Đã rời phòng');
        el('duration').textContent = 'Micro và camera đã dừng.';
        el('play-audio').hidden = true;
        controls(); notice(message, error);
    }
    async function toggle(kind) {
        if (!joined) return;
        const version = mediaVersion;
        const button = el(kind === 'audio' ? 'mic' : 'camera'); button.disabled = true;
        try {
            let current = track(kind);
            if (current) current.enabled = !current.enabled;
            else {
                const acquired = await navigator.mediaDevices.getUserMedia({ audio: kind === 'audio', video: kind === 'video' });
                if (!joined || version !== mediaVersion) { acquired.getTracks().forEach(t => t.stop()); return; }
                current = acquired.getTracks()[0]; stream.addTrack(current);
                current.onended = () => { syncMedia(); void sendMedia().catch(() => {}); };
                const transceiver = peer?.getTransceivers().find(t => t.receiver.track.kind === kind);
                if (transceiver) await transceiver.sender.replaceTrack(current);
            }
            syncMedia(); await sendMedia();
        } catch { notice('Không bật được thiết bị. Hãy kiểm tra kết nối thiết bị và quyền trong trình duyệt.', true); }
        finally { button.disabled = !joined; }
    }
    el('join').addEventListener('click', () => void join());
    el('leave').addEventListener('click', () => void leave());
    el('mic').addEventListener('click', () => void toggle('audio'));
    el('camera').addEventListener('click', () => void toggle('video'));
    el('reconnect').addEventListener('click', recover);
    el('play-audio').addEventListener('click', () => remoteVideo.play().then(() => { el('play-audio').hidden = true; }).catch(() => notice('Hãy cho phép phát âm thanh trong trình duyệt.', true)));
    window.addEventListener('pagehide', () => {
        joined = false; ++mediaVersion;
        stream?.getTracks().forEach(t => t.stop());
        peer?.close();
        void connection?.stop();
    });
    controls();
})();
