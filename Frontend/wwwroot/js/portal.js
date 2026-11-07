(() => {
    'use strict';
    const toggle = document.querySelector('[data-sidebar-toggle]');
    const backdrop = document.querySelector('[data-sidebar-backdrop]');
    function dongThanhDieuHuong() { const opened = document.body.classList.contains('sidebar-open'); document.body.classList.remove('sidebar-open'); toggle?.setAttribute('aria-expanded', 'false'); if (opened) toggle?.focus(); }
    toggle?.addEventListener('click', () => {
        const open = document.body.classList.toggle('sidebar-open'); toggle.setAttribute('aria-expanded', String(open));
        if (open) document.querySelector('.workspace-sidebar a')?.focus();
    });
    backdrop?.addEventListener('click', dongThanhDieuHuong);
    document.addEventListener('keydown', event => {
        if (event.key === 'Escape') { dongThanhDieuHuong(); const chat = document.querySelector('[data-chat-window]'); if (chat) chat.style.display = 'none'; }
        if (event.key === 'Tab' && document.body.classList.contains('sidebar-open')) {
            const items = [...document.querySelectorAll('.workspace-sidebar a, .workspace-sidebar button')];
            const first = items[0], last = items[items.length - 1];
            if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus(); }
            else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus(); }
        }
    });
    document.querySelectorAll('[data-confirm]').forEach(form => form.addEventListener('submit', event => { if (!window.confirm(form.dataset.confirm)) event.preventDefault(); }));
    const fileInput = document.querySelector('[data-avatar-input]');
    let previousAvatar;
    fileInput?.addEventListener('change', () => {
        const file = fileInput.files[0]; if (!file) return;
        if (previousAvatar) URL.revokeObjectURL(previousAvatar);
        previousAvatar = URL.createObjectURL(file); document.querySelector('[data-avatar-preview]').src = previousAvatar;
    });
    document.querySelectorAll('[data-password-toggle]').forEach(button => button.addEventListener('click', () => {
        const input = document.getElementById(button.dataset.passwordToggle); const visible = input.type === 'password';
        input.type = visible ? 'text' : 'password'; button.textContent = visible ? 'Ẩn' : 'Hiện'; button.setAttribute('aria-pressed', String(visible));
    }));
    document.querySelectorAll('[data-template-filter]').forEach(button => button.addEventListener('click', () => {
        document.querySelectorAll('[data-template-filter]').forEach(item => {
            const selected = item === button; item.classList.toggle('btn-primary', selected); item.classList.toggle('btn-light', !selected); item.setAttribute('aria-pressed', String(selected));
        });
        document.querySelectorAll('[data-template-category]').forEach(card => { card.hidden = button.dataset.templateFilter !== 'all' && card.dataset.templateCategory !== button.dataset.templateFilter; });
    }));
    document.querySelectorAll('[data-template-preview]').forEach(button => button.addEventListener('click', () => {
        const image = document.getElementById('template-preview-image'); image.src = button.dataset.templatePreview; image.alt = button.dataset.templateName;
        document.getElementById('template-preview-title').textContent = button.dataset.templateName;
        bootstrap.Modal.getOrCreateInstance(document.getElementById('template-preview-modal')).show();
    }));
    let detailsRequest;
    document.querySelectorAll('[data-job-details]').forEach(button => button.addEventListener('click', async () => {
        detailsRequest?.abort(); detailsRequest = new AbortController();
        const current = detailsRequest;
        const status = document.getElementById('job-modal-status'), data = document.getElementById('job-modal-data');
        status.hidden = false; status.textContent = 'Đang tải nội dung…'; data.hidden = true;
        document.getElementById('job-modal-title').textContent = 'Chi tiết tin tuyển dụng';
        bootstrap.Modal.getOrCreateInstance(document.getElementById('job-details-modal')).show();
        try {
            const response = await fetch('/Admin/JobManager/GetDetails?id=' + encodeURIComponent(button.dataset.jobDetails), { signal: current.signal });
            if (!response.ok) throw new Error('Chưa tải được nội dung. Vui lòng đóng và thử lại.');
            const result = await response.json(); if (current !== detailsRequest) return;
            document.getElementById('job-modal-title').textContent = result.title;
            document.querySelectorAll('[data-job-field]').forEach(node => { node.textContent = result[node.dataset.jobField] || 'Chưa cập nhật'; });
            status.hidden = true; data.hidden = false;
        } catch (error) { if (current === detailsRequest && error.name !== 'AbortError') status.textContent = error.message; }
    }));
})();
