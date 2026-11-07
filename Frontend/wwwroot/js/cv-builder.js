(() => {
    'use strict';
    const dataElement = document.getElementById('cv-editor-data');
    if (!dataElement) return;
    const initial = JSON.parse(dataElement.textContent);
    let documentId = initial.Id || 0;
    const byId = id => document.getElementById(id);
    const status = byId('cvSaveStatus');
    const reviewSection = byId('aiReviewSection');
    const csrf = document.querySelector('input[name="__RequestVerificationToken"]').value;
    const specs = {
        experience: { container: 'experienceContainer', item: 'exp-item', fields: [['Company', 'exp-comp', 'Tên công ty'], ['Role', 'exp-role', 'Vai trò'], ['Duration', 'exp-time', 'Thời gian'], ['Description', 'exp-desc', 'Mô tả', true]] },
        education: { container: 'educationContainer', item: 'edu-item', fields: [['School', 'edu-school', 'Trường học'], ['Major', 'edu-major', 'Ngành học'], ['Duration', 'edu-time', 'Thời gian']] },
        reference: { container: 'referenceContainer', item: 'ref-item', fields: [['Name', 'ref-name', 'Họ tên'], ['Position', 'ref-pos', 'Chức vụ'], ['Phone', 'ref-contact', 'Điện thoại hoặc email'], ['Company', 'ref-company', '', false, true]] }
    };
    const create = (tag, text = '', className = '') => {
        const node = document.createElement(tag); node.textContent = text; node.className = className; return node;
    };
    const message = (text, error = false) => {
        status.textContent = text; status.className = `alert ${error ? 'alert-danger' : 'alert-info'} mx-3`; status.hidden = false;
    };
    function addField(kind, values = {}) {
        const spec = specs[kind];
        if (byId(spec.container).children.length >= 20) { message('Mỗi mục CV tối đa 20 dòng.', true); return; }
        const item = create('div', '', `p-2 border rounded bg-light mb-2 position-relative ${spec.item}`);
        const remove = create('button', '×', 'btn btn-sm btn-outline-danger float-end'); remove.type = 'button'; remove.setAttribute('aria-label', 'Xóa mục');
        remove.addEventListener('click', () => { item.remove(); renderLists(); reviewSection.classList.add('d-none'); }); item.append(remove);
        for (const [property, className, placeholder, multiline, hidden] of spec.fields) {
            const input = document.createElement(multiline ? 'textarea' : 'input');
            if (!multiline) input.type = hidden ? 'hidden' : 'text';
            input.className = `form-control form-control-sm mb-2 ${className}`; input.placeholder = placeholder;
            input.setAttribute('aria-label', placeholder || property);
            input.value = values[property] || ''; input.maxLength = multiline ? 4000 : property === 'Duration' ? 100 : property === 'Phone' ? 256 : 200;
            input.addEventListener('input', () => { renderLists(); reviewSection.classList.add('d-none'); }); item.append(input);
        }
        byId(spec.container).append(item); renderLists();
    }
    function items(kind) {
        const spec = specs[kind];
        return [...byId(spec.container).querySelectorAll(`.${spec.item}`)].map(item => Object.fromEntries(
            spec.fields.map(([property, className]) => [property, item.querySelector(`.${className}`).value.trim()])
        )).filter(item => Object.values(item).some(Boolean));
    }
    function renderLists() {
        const targets = { experience: 'outExperienceList', education: 'outEducationList', reference: 'outReferenceList' };
        for (const [kind, targetId] of Object.entries(targets)) {
            const target = byId(targetId); if (!target) continue; target.replaceChildren();
            for (const item of items(kind)) {
                const section = create('div', '', 'mb-3 cv-keep-together');
                if (kind === 'experience') {
                    section.append(create('div', item.Company, 'fw-bold'), create('div', item.Role, 'cv-theme-text small'), create('small', item.Duration, 'text-muted'));
                    const description = create('p', item.Description, 'small text-secondary mb-0'); description.style.whiteSpace = 'pre-wrap'; section.append(description);
                } else if (kind === 'education') {
                    section.append(create('div', item.School, 'fw-bold'), create('div', item.Major, 'small'), create('small', item.Duration, 'text-muted'));
                } else section.append(create('div', item.Name, 'fw-bold'), create('div', item.Position, 'small'), create('div', item.Company, 'small'), create('div', item.Phone, 'small'));
                target.append(section);
            }
        }
        document.querySelectorAll('.cv-theme-text').forEach(node => { node.style.color = byId('themeColorPicker').value; });
    }
    const skillText = (initial.ITSKills || []).map(item => item.SkillName || '').join('\n');
    const languageText = (initial.Languages || []).map(item => [item.LanguageName, item.Level].filter(Boolean).join(': ')).join('\n');
    byId('inSkills').value = skillText; byId('inLang').value = languageText;
    byId('themeColorPicker').value = initial.ThemeColor || '#0d6efd';
    byId('headerBgColorPicker').value = initial.HeaderColor || (initial.TemplateId === 2 ? '#eaddd5' : '#212529');
    byId('bgColorPicker').value = initial.BackgroundColor || '#ffffff';
    for (const kind of Object.keys(specs)) byId(specs[kind].container).replaceChildren();
    const populate = (kind, entries) => { for (const entry of entries.length ? entries : [{}]) addField(kind, entry); };
    populate('experience', initial.Experiences || []); populate('education', initial.Educations || []); populate('reference', initial.References || []);
    window.addExperienceField = () => addField('experience');
    window.addEducationField = () => addField('education');
    window.addReferenceField = () => addField('reference');
    function colors() {
        renderLists();
        document.querySelectorAll('.cv-theme-border').forEach(node => node.style.setProperty('border-color', byId('themeColorPicker').value, 'important'));
        byId('cvPreviewHeader').style.backgroundColor = byId('headerBgColorPicker').value;
        document.querySelectorAll('.bg-inherit-custom').forEach(node => { node.style.backgroundColor = byId('bgColorPicker').value; });
    }
    function scaleText() {
        for (const [containerId, textId] of [['nameScaleContainer', 'outName'], ['jobScaleContainer', 'outJob']]) {
            const container = byId(containerId), text = byId(textId); if (!container || !text) continue;
            const maximum = initial.TemplateId === 2 ? 370 : 260;
            container.style.transform = `scale(${Math.min(1, maximum / Math.max(1, text.offsetWidth))})`;
        }
    }
    for (const [inputId, targetId] of [['inName', 'outName'], ['inJob', 'outJob'], ['inPhone', 'outPhone'], ['inEmail', 'outEmail'], ['inAddress', 'outAddress'], ['inSummary', 'outSummary'], ['inSkills', 'outSkills'], ['inLang', 'outLang']]) {
        const input = byId(inputId), target = byId(targetId); if (!input || !target) continue;
        const sync = () => { target.textContent = input.value; scaleText(); };
        input.addEventListener('input', () => { sync(); reviewSection.classList.add('d-none'); }); sync();
    }
    for (const id of ['themeColorPicker', 'headerBgColorPicker', 'bgColorPicker']) byId(id).addEventListener('input', colors);
    colors();
    let avatarFile = null, avatarObjectUrl = null;
    byId('uploadAvatarFile').addEventListener('change', event => {
        const file = event.target.files[0]; if (!file) return;
        if (!['image/png', 'image/jpeg'].includes(file.type) || file.size > 2 * 1024 * 1024) { message('Chọn ảnh PNG/JPEG tối đa 2 MB.', true); event.target.value = ''; return; }
        avatarFile = file; if (avatarObjectUrl) URL.revokeObjectURL(avatarObjectUrl); avatarObjectUrl = URL.createObjectURL(file);
        for (const id of ['imgAvatarForm', 'cvPreviewAvatar']) { const image = byId(id); if (image) { image.src = avatarObjectUrl; image.style.display = 'block'; } }
        byId('avatarPlaceholderText').classList.add('d-none');
    });
    function gather() {
        const skills = byId('inSkills').value, languages = byId('inLang').value;
        return {
            Id: documentId, TemplateId: initial.TemplateId, FullName: byId('inName').value.trim(), JobTitle: byId('inJob').value.trim(),
            Email: byId('inEmail').value.trim(), Phone: byId('inPhone').value.trim(), Address: byId('inAddress').value.trim(), Summary: byId('inSummary').value,
            Experiences: items('experience'), Educations: items('education'), References: items('reference'),
            ITSKills: skills === skillText ? initial.ITSKills || [] : skills.split('\n').map(text => text.trim()).filter(Boolean).map(SkillName => ({ SkillName })),
            Languages: languages === languageText ? initial.Languages || [] : languages.split('\n').map(text => text.trim()).filter(Boolean).map(LanguageName => ({ LanguageName })),
            Website: initial.Website, Interests: initial.Interests, Activities: initial.Activities, Certifications: initial.Certifications,
            ThemeColor: byId('themeColorPicker').value, HeaderColor: byId('headerBgColorPicker').value, BackgroundColor: byId('bgColorPicker').value
        };
    }
    async function responseData(response) {
        const data = await response.json().catch(() => ({}));
        if (!response.ok || !data.success) throw new Error(data.message || (response.status === 400 ? 'Phiên biểu mẫu đã hết hạn hoặc dữ liệu chưa hợp lệ. Vui lòng kiểm tra và tải lại trang.' : 'Không thể hoàn tất yêu cầu. Vui lòng thử lại.'));
        return data;
    }
    let busy = false;
    async function operation(action) {
        if (busy) return; busy = true;
        const states = [...document.querySelectorAll('.container-fluid input, .container-fluid textarea, .container-fluid button')].map(node => [node, node.disabled]);
        states.forEach(([node]) => { node.disabled = true; });
        try { await action(); } catch (error) { message(error.message, true); }
        finally { states.forEach(([node, disabled]) => { node.disabled = disabled; }); busy = false; }
    }
    async function save(download) {
        const model = gather();
        if (!model.FullName || !model.JobTitle || !model.Email) throw new Error('Điền họ tên, email và vị trí công việc trước khi lưu CV.');
        message('Đang tạo và lưu CV...');
        if (!window.html2pdf) throw new Error('Chưa tải được thư viện PDF. Vui lòng kiểm tra kết nối và tải lại trang.');
        if (document.fonts) await document.fonts.ready;
        const pdf = await html2pdf().set({ margin: 0, image: { type: 'jpeg', quality: 0.98 }, html2canvas: { scale: 2, useCORS: true, logging: false }, jsPDF: { unit: 'mm', format: 'a4', orientation: 'portrait' }, pagebreak: { mode: ['css', 'legacy'], avoid: ['.cv-keep-together'] } }).from(byId('cvSheet')).toPdf().outputPdf('blob');
        const form = new FormData(); form.append('modelJson', JSON.stringify(model)); form.append('pdfFile', pdf, 'cv.pdf'); if (avatarFile) form.append('avatarFile', avatarFile);
        const data = await responseData(await fetch('/Candidate/CV/Save', { method: 'POST', headers: { 'X-CSRF-TOKEN': csrf }, body: form }));
        documentId = data.id;
        const link = byId('cvStoredDownload'); link.href = data.url; link.hidden = false;
        message('Đã lưu CV. Bạn có thể dùng CV này để ứng tuyển.');
        if (download) {
            const url = URL.createObjectURL(pdf), anchor = document.createElement('a');
            anchor.href = url; anchor.download = `CV_${model.FullName.replace(/[<>:"/\\|?*\u0000-\u001f]/g, '_')}.pdf`; anchor.click(); setTimeout(() => URL.revokeObjectURL(url), 1000);
        }
    }
    byId('btnSaveCV').addEventListener('click', () => operation(() => save(false)));
    byId('btnDownloadPDF').addEventListener('click', () => operation(() => save(true)));
    function showReview(score, suggestions) {
        byId('aiScoreBadge').textContent = `${score} / 100 Điểm`;
        byId('aiSuggestionsList').replaceChildren(...suggestions.map(text => create('li', text, 'mb-1')));
        reviewSection.classList.remove('d-none');
    }
    byId('btnCheckAI').addEventListener('click', () => operation(async () => {
        message('Đang đánh giá CV...');
        const data = await responseData(await fetch('/Candidate/CVReview/Analyze', { method: 'POST', headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': csrf }, body: JSON.stringify(gather()) }));
        showReview(data.score, data.suggestions); message('Đã đánh giá CV. Lưu CV để giữ kết quả này cùng nội dung hiện tại.');
    }));
    if (initial.AiScore != null && initial.AiSuggestions?.length) showReview(initial.AiScore, initial.AiSuggestions);
    window.addEventListener('resize', scaleText);
})();
