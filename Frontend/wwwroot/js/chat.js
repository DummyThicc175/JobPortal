(() => {
    'use strict';
    if (window.portalChatStarted || !window.signalR) return;
    const layPhanTu = id => document.getElementById(id);
    const userId = layPhanTu('employer-current-id')?.value || layPhanTu('current-user-id')?.value;
    if (!userId) return;
    window.portalChatStarted = true;
    const connection = new signalR.HubConnectionBuilder().withUrl('/chatHub').withAutomaticReconnect().build();
    const panels = [];
    function taoKhungChat(bodyId, inputId, sendId, partnerId, titleId, listId) {
        const body = layPhanTu(bodyId), input = layPhanTu(inputId), send = layPhanTu(sendId);
        if (!body || !input || !send) return;
        const state = { body, input, send, partner: layPhanTu(partnerId), title: layPhanTu(titleId), list: layPhanTu(listId), ids: new Set(), generation: 0, before: null, loading: false };
        const notice = document.createElement('div'); notice.className = 'small text-muted px-3 py-1'; notice.setAttribute('role', 'status'); body.before(notice); state.notice = notice;
        input.maxLength = 2000; input.disabled = true; send.disabled = true;
        const older = document.createElement('button'); older.type = 'button'; older.className = 'btn btn-sm btn-light align-self-center'; older.textContent = 'Xem tin nhắn trước'; older.hidden = true; state.older = older;
        older.addEventListener('click', () => taiLichSu(state, true)); body.replaceChildren(older);
        send.addEventListener('click', () => guiTinNhan(state));
        input.addEventListener('keydown', event => { if (event.key === 'Enter' && !event.shiftKey) { event.preventDefault(); guiTinNhan(state); } });
        panels.push(state); return state;
    }
    const widget = taoKhungChat('chat-body', 'chat-input', 'btn-send-chat', 'active-partner-id', 'chat-partner-name', 'partner-list');
    taoKhungChat('employer-chat-body', 'employer-chat-input', 'employer-btn-send', 'employer-active-partner', 'current-chat-title', 'employer-partner-list');
    if (!panels.length) return;
    function dinhDangGio(time) { const date = new Date(time); return Number.isNaN(date.getTime()) ? '' : date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }); }
    function themTinNhan(state, message, prepend = false) {
        if (state.ids.has(message.id)) return;
        state.ids.add(message.id);
        const bubble = document.createElement('div');
        const isMe = message.senderId === userId;
        bubble.className = 'chat-message ' + (isMe ? 'chat-message-own' : 'chat-message-peer');
        const text = document.createElement('div'); text.textContent = message.content;
        const time = document.createElement('small'); time.className = 'd-block text-end opacity-75'; time.textContent = dinhDangGio(message.time);
        bubble.append(text, time);
        if (prepend) state.older.after(bubble); else state.body.append(bubble);
    }
    async function taiLichSu(state, older = false) {
        const partner = state.partner.value;
        if (!partner || state.loading) return;
        const generation = state.generation; state.loading = true; state.older.disabled = true;
        try {
            const url = `/Chat/GetChatHistory?partnerId=${encodeURIComponent(partner)}${older && state.before ? `&beforeId=${state.before}` : ''}`;
            const response = await fetch(url, { cache: 'no-store' });
            if (!response.ok) throw new Error('Không tải được hội thoại. Kiểm tra quyền truy cập và thử lại.');
            const messages = await response.json(); if (generation !== state.generation) return;
            const height = state.body.scrollHeight;
            if (older) [...messages].reverse().forEach(message => themTinNhan(state, message, true));
            else messages.forEach(message => themTinNhan(state, message));
            state.before = response.headers.get('X-Next-Before'); state.older.hidden = response.headers.get('X-Has-More') !== 'true';
            if (older) state.body.scrollTop += state.body.scrollHeight - height;
            else state.body.scrollTop = state.body.scrollHeight;
            state.notice.textContent = '';
        } catch (error) { if (generation === state.generation) state.notice.textContent = error.message; }
        finally { if (generation === state.generation) { state.loading = false; state.older.disabled = false; } }
    }
    async function chonDoiThoai(state, id, name) {
        state.generation++; state.loading = false; state.partner.value = id; state.ids.clear(); state.before = null;
        state.body.replaceChildren(state.older); state.older.hidden = true; state.title.textContent = name;
        state.list?.querySelectorAll('[data-partner-id]').forEach(button => { button.classList.toggle('active', button.dataset.partnerId === id); });
        const connected = connection.state === signalR.HubConnectionState.Connected;
        state.input.disabled = !connected; state.send.disabled = !connected;
        await taiLichSu(state);
    }
    async function taiDanhSachDoiThoai() {
        if (!panels.some(state => state.list)) return;
        try {
            const response = await fetch('/Chat/GetChatPartners', { cache: 'no-store' }); if (!response.ok) return;
            const entries = await response.json();
            for (const state of panels.filter(item => item.list)) {
                state.list.replaceChildren();
                if (!entries.length) { const empty = document.createElement('p'); empty.className = 'small text-muted p-3'; empty.textContent = 'Chưa có cuộc trò chuyện.'; state.list.append(empty); }
                for (const entry of entries) {
                    const button = document.createElement('button'); button.type = 'button'; button.className = 'list-group-item list-group-item-action w-100 text-start p-3'; button.textContent = entry.name;
                    button.dataset.partnerId = entry.id; button.classList.toggle('active', state.partner.value === entry.id);
                    button.addEventListener('click', () => chonDoiThoai(state, entry.id, entry.name)); state.list.append(button);
                }
            }
        } catch { /* Existing conversation remains usable if the partner list cannot refresh. */ }
    }
    async function guiTinNhan(state) {
        const message = state.input.value.trim(); if (state.sending || !message || !state.partner.value || connection.state !== signalR.HubConnectionState.Connected) return;
        state.sending = true; state.send.disabled = true;
        try { await connection.invoke('SendMessage', state.partner.value, message); state.input.value = ''; state.notice.textContent = ''; }
        catch { state.notice.textContent = 'Chưa gửi được tin. Có thể bạn gửi quá nhanh hoặc không còn quyền truy cập hội thoại.'; }
        finally { state.sending = false; state.send.disabled = connection.state !== signalR.HubConnectionState.Connected; }
    }
    let refreshTimer;
    connection.on('ReceiveMessage', (senderId, receiverId, content, time, id) => {
        for (const state of panels) {
            const partner = state.partner.value;
            if ((senderId === userId && receiverId === partner) || (senderId === partner && receiverId === userId)) {
                const atBottom = state.body.scrollHeight - state.body.scrollTop - state.body.clientHeight < 100;
                themTinNhan(state, { id, senderId, content, time }); if (atBottom) state.body.scrollTop = state.body.scrollHeight;
            }
        }
        clearTimeout(refreshTimer); refreshTimer = setTimeout(taiDanhSachDoiThoai, 300);
    });
    connection.onreconnecting(() => panels.forEach(state => { state.send.disabled = true; state.notice.textContent = 'Đang kết nối lại...'; }));
    connection.onreconnected(() => { panels.forEach(state => { state.send.disabled = !state.partner.value; state.input.disabled = !state.partner.value; if (state.partner.value) taiLichSu(state); }); taiDanhSachDoiThoai(); });
    connection.onclose(() => panels.forEach(state => { state.send.disabled = true; state.notice.textContent = 'Kết nối đã đóng. Tải lại trang để tiếp tục.'; }));
    function hienThiKhungChat(mo, traFocus = false) {
        const chat = layPhanTu('chat-window'); if (!chat) return;
        chat.style.display = mo ? 'flex' : 'none'; document.body.classList.toggle('chat-open',mo);
        layPhanTu('chat-bubble')?.setAttribute('aria-expanded',String(mo));
        if (mo) { taiDanhSachDoiThoai(); chat.querySelector('button[aria-label="Đóng tin nhắn"]')?.focus(); }
        else if (traFocus) layPhanTu('chat-bubble')?.focus();
    }
    window.batTatKhungChat = () => hienThiKhungChat(layPhanTu('chat-window')?.style.display !== 'flex',true);
    window.dongKhungChat = () => { if (layPhanTu('chat-window')?.style.display === 'flex') hienThiKhungChat(false,true); };
    document.addEventListener('click', async event => {
        const button = event.target.closest('.btn-open-chat'); if (!button || !widget) return;
        hienThiKhungChat(true); await chonDoiThoai(widget, button.dataset.partnerId, button.dataset.partnerName || 'Hội thoại');
    });
    connection.start().then(() => { panels.forEach(state => { state.notice.textContent = ''; state.input.disabled = !state.partner.value; state.send.disabled = !state.partner.value; }); taiDanhSachDoiThoai(); })
        .catch(() => panels.forEach(state => { state.notice.textContent = 'Không kết nối được tin nhắn. Vui lòng tải lại trang.'; }));
})();
