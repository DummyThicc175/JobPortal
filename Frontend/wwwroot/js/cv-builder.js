(() => {
    'use strict';
    const goc = document.getElementById('cv-studio');
    if (!goc) return;
    const layPhanTu = id => document.getElementById(id);
    const duLieuCu = JSON.parse(layPhanTu('cv-editor-data').textContent);
    const cacMau = JSON.parse(layPhanTu('cv-template-data').textContent);
    const tenKhoi = { summary: 'Giới thiệu bản thân', experience: 'Kinh nghiệm làm việc', education: 'Học vấn', skills: 'Kỹ năng', languages: 'Ngoại ngữ', certifications: 'Chứng chỉ', activities: 'Hoạt động / dự án', interests: 'Sở thích', references: 'Người tham chiếu' };
    let maCV = duLieuCu.Id || 0, maMau = duLieuCu.TemplateId, dangXuLy = false, daThayDoi = false;
    let anhTaiLen = null, duongDanAnhTam = null, duongDanAnh = duLieuCu.AvatarPath;
    let boCuc = duLieuCu.BlockLayout?.length ? duLieuCu.BlockLayout : taoBoCucMacDinh();
    let ketQuaAI = duLieuCu.AiScore != null ? { score: duLieuCu.AiScore, suggestions: duLieuCu.AiSuggestions || [] } : null;
    let henVe, mucThuPhong = 0.7, khoiDangKeo = null, soTrang = 1, loiPhanTrang = false;
    const maChongGiaMao = goc.querySelector('input[name=__RequestVerificationToken]').value;
    const cauHinhMuc = {
        experience: { container: 'experienceContainer', item: 'exp-item', fields: [['Company','exp-comp','Công ty',200],['Role','exp-role','Vai trò',200],['Duration','exp-time','Thời gian',100],['Description','exp-desc','Mô tả công việc',4000,true]] },
        education: { container: 'educationContainer', item: 'edu-item', fields: [['School','edu-school','Trường học',200],['Major','edu-major','Ngành học',200],['Duration','edu-time','Thời gian',100]] },
        reference: { container: 'referenceContainer', item: 'ref-item', fields: [['Name','ref-name','Họ tên',200],['Position','ref-pos','Chức vụ',200],['Company','ref-company','Công ty',200],['Phone','ref-contact','Điện thoại / email',256]] }
    };
    function layMau() { return cacMau.find(m => m.Id === maMau); }
    function taoBoCucMacDinh() { return Object.keys(tenKhoi).map(Key => ({ Key, Zone: layMau().TwoColumns && ['skills','languages','interests','references'].includes(Key) ? 'side' : 'main' })); }
    function taoPhanTu(the, noiDung = '', lop = '') { const nut = document.createElement(the); nut.textContent = noiDung; nut.className = lop; return nut; }
    function hienThiThongBao(noiDung, loi = false) { const nut = layPhanTu('cvSaveStatus'); nut.textContent = noiDung; nut.className = 'alert ' + (loi ? 'alert-danger' : 'alert-info'); nut.hidden = !noiDung; }
    function danhDauThayDoi(noiDung = false) {
        daThayDoi = true; layPhanTu('cvDirtyStatus').textContent = 'Có thay đổi chưa lưu';
        if (noiDung) { ketQuaAI = null; hienThiDanhGia(); }
        clearTimeout(henVe); henVe = setTimeout(veCV, 120);
    }
    function themTruong(loai, giaTri = {}) {
        const cauHinh = cauHinhMuc[loai], vung = layPhanTu(cauHinh.container);
        if (vung.children.length >= 20) { hienThiThongBao('Mỗi mục CV tối đa 20 dòng.', true); return; }
        const muc = taoPhanTu('div', '', 'cv-edit-entry ' + cauHinh.item);
        const xoa = taoPhanTu('button', 'Xóa mục', 'cv-remove-entry'); xoa.type = 'button';
        xoa.addEventListener('click', () => { muc.remove(); danhDauThayDoi(true); }); muc.append(xoa);
        for (const [thuocTinh, lop, nhan, gioiHan, nhieuDong] of cauHinh.fields) {
            const truong = document.createElement(nhieuDong ? 'textarea' : 'input'); if (!nhieuDong) truong.type = 'text';
            truong.className = 'form-control form-control-sm ' + lop; truong.maxLength = gioiHan; truong.value = giaTri[thuocTinh] || '';
            truong.setAttribute('aria-label', nhan); if (nhieuDong) truong.rows = 3;
            const tieuDe = taoPhanTu('label', nhan); tieuDe.append(truong); muc.append(tieuDe);
            truong.addEventListener('input', () => danhDauThayDoi(true));
        }
        vung.append(muc);
    }
    function layCacMuc(loai) {
        const cauHinh = cauHinhMuc[loai];
        return [...layPhanTu(cauHinh.container).children].map(muc => Object.fromEntries(cauHinh.fields.map(([thuocTinh, lop]) => [thuocTinh, muc.querySelector('.' + lop).value.trim()]))).filter(muc => Object.values(muc).some(Boolean));
    }
    window.themMucKinhNghiem = () => { themTruong('experience'); danhDauThayDoi(true); };
    window.themMucHocVan = () => { themTruong('education'); danhDauThayDoi(true); };
    window.themMucNguoiThamChieu = () => { themTruong('reference'); danhDauThayDoi(true); };
    layPhanTu('cv-add-experience').addEventListener('click', window.themMucKinhNghiem);
    layPhanTu('cv-add-education').addEventListener('click', window.themMucHocVan);
    layPhanTu('cv-add-reference').addEventListener('click', window.themMucNguoiThamChieu);
    for (const [loai, duLieu] of [['experience', duLieuCu.Experiences], ['education', duLieuCu.Educations], ['reference', duLieuCu.References]]) for (const muc of duLieu?.length ? duLieu : [{}]) themTruong(loai, muc);
    const kyNangCu = (duLieuCu.ITSKills || []).map(m => m.SkillName || '').join('\n');
    const ngoaiNguCu = (duLieuCu.Languages || []).map(m => [m.LanguageName,m.Level].filter(Boolean).join(': ')).join('\n');
    layPhanTu('inSkills').value = kyNangCu; layPhanTu('inLang').value = ngoaiNguCu;
    function thuThapDuLieu() {
        const doc = id => layPhanTu(id).value;
        return { Id: maCV, TemplateId: maMau, FullName: doc('inName').trim(), JobTitle: doc('inJob').trim(), Email: doc('inEmail').trim(), Phone: doc('inPhone').trim(), Address: doc('inAddress').trim(), Website: doc('inWebsite').trim(), Summary: doc('inSummary'),
            Experiences: layCacMuc('experience'), Educations: layCacMuc('education'), References: layCacMuc('reference'),
            ITSKills: doc('inSkills') === kyNangCu ? duLieuCu.ITSKills || [] : doc('inSkills').split('\n').map(m => m.trim()).filter(Boolean).map(SkillName => ({ SkillName })),
            Languages: doc('inLang') === ngoaiNguCu ? duLieuCu.Languages || [] : doc('inLang').split('\n').map(m => m.trim()).filter(Boolean).map(LanguageName => ({ LanguageName })),
            Certifications: doc('inCertifications'), Activities: doc('inActivities'), Interests: doc('inInterests'),
            ThemeColor: doc('themeColorPicker'), HeaderColor: doc('headerBgColorPicker'), BackgroundColor: doc('bgColorPicker'), BlockLayout: boCuc.map(m => ({...m})) };
    }
    function hienThiDanhGia() {
        layPhanTu('aiReviewSection').classList.toggle('d-none', !ketQuaAI);
        if (!ketQuaAI) return;
        layPhanTu('aiScoreBadge').textContent = ketQuaAI.score + ' / 100';
        layPhanTu('aiSuggestionsList').replaceChildren(...ketQuaAI.suggestions.map(m => taoPhanTu('li', m)));
    }
    function hienThiBoCuc() {
        layPhanTu('cv-layout-side-column').hidden = !layMau().TwoColumns;
        for (const cot of ['main','side']) {
            const danhSach = layPhanTu('cv-layout-' + cot); danhSach.replaceChildren();
            const cacKhoi = boCuc.filter(m => m.Zone === cot);
            for (const [viTri, khoi] of cacKhoi.entries()) {
                const muc = taoPhanTu('li', '', 'cv-layout-block'); muc.draggable = matchMedia('(pointer:fine)').matches; muc.dataset.blockKey = khoi.Key;
                // Chuột dùng HTML drag/drop, cảm ứng dùng Pointer Events để không tranh chấp cử chỉ.
                muc.addEventListener('pointerdown',suKien => { if(suKien.pointerType==='mouse' && !muc.draggable) muc.draggable=true; },true);
                const tayKeo = taoPhanTu('button','⠿','cv-drag-handle'); tayKeo.type='button'; tayKeo.setAttribute('aria-label','Kéo để sắp xếp ' + tenKhoi[khoi.Key]);
                ganKeoCamUng(tayKeo,khoi.Key); muc.append(tayKeo,taoPhanTu('span', tenKhoi[khoi.Key])); const nutDieuKhien = document.createElement('div');
                for (const [lenh, chu, nhan, voHieu] of [['up','↑','Đưa lên',viTri === 0],['down','↓','Đưa xuống',viTri === cacKhoi.length-1],['zone','⇄','Chuyển cột',!layMau().TwoColumns]]) {
                    const nut = taoPhanTu('button', chu); nut.type = 'button'; nut.disabled = voHieu; nut.dataset.layoutAction = lenh; nut.setAttribute('aria-label', nhan + ' ' + tenKhoi[khoi.Key]);
                    nut.addEventListener('click', () => {
                        if (dangXuLy) return;
                        if (lenh === 'zone') chuyenKhoi(khoi.Key, cot === 'main' ? 'side' : 'main');
                        else {
                            const chiSo = boCuc.findIndex(m => m.Key === khoi.Key), keBen = boCuc.findIndex(m => m.Key === cacKhoi[viTri + (lenh === 'up' ? -1 : 1)].Key);
                            [boCuc[chiSo],boCuc[keBen]] = [boCuc[keBen],boCuc[chiSo]]; capNhatBoCuc(khoi.Key, lenh);
                        }
                    }); nutDieuKhien.append(nut);
                }
                muc.append(nutDieuKhien);
                muc.addEventListener('dragstart', suKien => { if (dangXuLy) { suKien.preventDefault(); return; } khoiDangKeo = khoi.Key; suKien.dataTransfer.setData('text/plain', khoi.Key); suKien.dataTransfer.effectAllowed = 'move'; muc.classList.add('cv-dragging'); });
                muc.addEventListener('dragend', () => { khoiDangKeo = null; muc.classList.remove('cv-dragging'); document.querySelectorAll('.cv-drag-over').forEach(n => n.classList.remove('cv-drag-over')); });
                danhSach.append(muc);
            }
        }
    }
    function ganKeoCamUng(tayKeo,khoa) {
        let conTro = null, toaDoDau = null, daKeo = false;
        function donDep() {
            conTro=null; toaDoDau=null; daKeo=false; khoiDangKeo=null;
            document.querySelectorAll('.cv-drag-over,.cv-dragging').forEach(nut => nut.classList.remove('cv-drag-over','cv-dragging'));
        }
        tayKeo.addEventListener('pointerdown',suKien => {
            if (suKien.pointerType === 'mouse' || dangXuLy || !suKien.isPrimary) return;
            suKien.preventDefault(); conTro=suKien.pointerId; toaDoDau={x:suKien.clientX,y:suKien.clientY};
            tayKeo.closest('li').draggable=false;
            tayKeo.setPointerCapture(conTro);
        });
        tayKeo.addEventListener('pointermove',suKien => {
            if (suKien.pointerId !== conTro) return;
            if (!daKeo && Math.hypot(suKien.clientX-toaDoDau.x,suKien.clientY-toaDoDau.y)<6) return;
            daKeo=true; khoiDangKeo=khoa; tayKeo.closest('li').classList.add('cv-dragging');
            document.querySelectorAll('.cv-drag-over').forEach(nut => nut.classList.remove('cv-drag-over'));
            document.elementFromPoint(suKien.clientX,suKien.clientY)?.closest('[data-drop-zone]')?.classList.add('cv-drag-over');
            if (suKien.clientY > innerHeight-60) window.scrollBy(0,18); else if (suKien.clientY<100) window.scrollBy(0,-18);
        });
        tayKeo.addEventListener('pointerup',suKien => {
            if (suKien.pointerId !== conTro) return;
            const dich=document.elementFromPoint(suKien.clientX,suKien.clientY), cot=dich?.closest('[data-drop-zone]'), truoc=dich?.closest('[data-block-key]')?.dataset.blockKey;
            const canChuyen=daKeo && cot; tayKeo.releasePointerCapture(conTro); donDep();
            if (canChuyen) requestAnimationFrame(() => chuyenKhoi(khoa,cot.dataset.dropZone,truoc));
        });
        tayKeo.addEventListener('pointercancel',donDep);
    }
    function capNhatBoCuc(khoa, lenh = '') {
        hienThiBoCuc(); danhDauThayDoi();
        layPhanTu('cv-layout-message').textContent = 'Đã sắp xếp khối ' + tenKhoi[khoa] + '.';
        if (lenh) goc.querySelector('[data-block-key="' + khoa + '"] [data-layout-action="' + lenh + '"]')?.focus();
    }
    function chuyenKhoi(khoa, cot, truocKhoa = null) {
        if (dangXuLy || (cot === 'side' && !layMau().TwoColumns) || khoa === truocKhoa) return;
        const muc = boCuc.find(m => m.Key === khoa); if (!muc) return;
        boCuc = boCuc.filter(m => m !== muc); muc.Zone = cot;
        const viTri = truocKhoa ? boCuc.findIndex(m => m.Key === truocKhoa) : -1;
        if (viTri >= 0) boCuc.splice(viTri, 0, muc); else boCuc.push(muc);
        capNhatBoCuc(khoa);
    }
    for (const danhSach of goc.querySelectorAll('[data-drop-zone]')) {
        danhSach.addEventListener('dragover', suKien => { if (!khoiDangKeo || dangXuLy) return; suKien.preventDefault(); suKien.dataTransfer.dropEffect = 'move'; danhSach.classList.add('cv-drag-over'); });
        danhSach.addEventListener('dragleave', () => danhSach.classList.remove('cv-drag-over'));
        danhSach.addEventListener('drop', suKien => {
            suKien.preventDefault(); danhSach.classList.remove('cv-drag-over');
            const dich = suKien.target.closest('[data-block-key]');
            if (khoiDangKeo) chuyenKhoi(khoiDangKeo, danhSach.dataset.dropZone, dich?.dataset.blockKey);
        });
    }
    function chonTab(ten) {
        for (const nut of goc.querySelectorAll('[data-cv-tab]')) { const chon = nut.dataset.cvTab === ten; nut.setAttribute('aria-selected', String(chon)); nut.tabIndex = chon ? 0 : -1; layPhanTu('cv-panel-' + nut.dataset.cvTab).hidden = !chon; }
    }
    for (const nut of goc.querySelectorAll('[data-cv-tab]')) { nut.addEventListener('click', () => chonTab(nut.dataset.cvTab)); nut.addEventListener('keydown', suKien => { if (['ArrowLeft','ArrowRight','Home','End'].includes(suKien.key)) { suKien.preventDefault(); const ten = suKien.key === 'Home' ? 'content' : suKien.key === 'End' ? 'layout' : nut.dataset.cvTab === 'content' ? 'layout' : 'content'; chonTab(ten); layPhanTu('cv-tab-' + ten).focus(); } }); }
    for (const nut of goc.querySelectorAll('[data-cv-mobile-view]')) nut.addEventListener('click',() => {
        goc.dataset.mobileView=nut.dataset.cvMobileView;
        goc.querySelectorAll('[data-cv-mobile-view]').forEach(muc => muc.setAttribute('aria-pressed',String(muc===nut)));
        if (nut.dataset.cvMobileView==='preview') requestAnimationFrame(() => { veCV(); vuaKhung(); });
    });
    layPhanTu('cv-reset-layout').addEventListener('click', () => { boCuc = taoBoCucMacDinh(); hienThiBoCuc(); danhDauThayDoi(); layPhanTu('cv-layout-message').textContent = 'Đã khôi phục bố cục của mẫu.'; });
    layPhanTu('cvTemplateSelect').addEventListener('change', () => {
        maMau = Number(layPhanTu('cvTemplateSelect').value); const mau = layMau(); boCuc = taoBoCucMacDinh();
        layPhanTu('themeColorPicker').value = mau.Accent; layPhanTu('headerBgColorPicker').value = mau.Header; layPhanTu('bgColorPicker').value = mau.Background;
        hienThiBoCuc(); danhDauThayDoi(); hienThiThongBao('Đã đổi mẫu, giữ nguyên nội dung. Bố cục và màu được áp dụng theo mẫu mới.');
    });
    for (const truong of goc.querySelectorAll('[data-cv-content]')) truong.addEventListener('input', () => danhDauThayDoi(true));
    for (const id of ['themeColorPicker','headerBgColorPicker','bgColorPicker']) layPhanTu(id).addEventListener('input', () => danhDauThayDoi());
    layPhanTu('uploadAvatarFile').addEventListener('change', async suKien => {
        const tep = suKien.target.files[0]; if (!tep) return;
        if (!['image/png','image/jpeg'].includes(tep.type) || tep.size > 2 * 1024 * 1024) { hienThiThongBao('Chọn ảnh PNG/JPEG tối đa 2 MB.', true); suKien.target.value = ''; return; }
        const duongDan = URL.createObjectURL(tep), anh = new Image(); anh.src = duongDan;
        try { await anh.decode(); } catch { URL.revokeObjectURL(duongDan); hienThiThongBao('Ảnh không đọc được. Hãy chọn ảnh PNG/JPEG hợp lệ.', true); suKien.target.value = ''; return; }
        if (duongDanAnhTam) URL.revokeObjectURL(duongDanAnhTam);
        anhTaiLen = tep; duongDanAnhTam = duongDan; duongDanAnh = duongDan; layPhanTu('imgAvatarForm').src = duongDan; danhDauThayDoi();
    });
    function taoDong(tieuDe, vaiTro, thoiGian, moTa) {
        const dong = taoPhanTu('div', '', 'cv-entry');
        if (tieuDe) dong.append(taoPhanTu('strong', tieuDe));
        if (vaiTro) dong.append(taoPhanTu('div', vaiTro, 'cv-entry-role'));
        if (thoiGian) dong.append(taoPhanTu('span', thoiGian, 'cv-entry-time'));
        if (moTa) dong.append(taoPhanTu('p', moTa, 'cv-entry-description'));
        return dong;
    }
    function layNoiDungKhoi(khoa, duLieu) {
        if (khoa === 'experience') return duLieu.Experiences.map(m => taoDong(m.Company,m.Role,m.Duration,m.Description));
        if (khoa === 'education') return duLieu.Educations.map(m => taoDong(m.School,m.Major,m.Duration,''));
        if (khoa === 'references') return duLieu.References.map(m => taoDong(m.Name,m.Position,'',[m.Company,m.Phone].filter(Boolean).join('\n')));
        const chuoi = khoa === 'skills' ? duLieu.ITSKills.map(m => [m.SkillName,m.Description].filter(Boolean).join(' — ')).join('\n') : khoa === 'languages' ? duLieu.Languages.map(m => [m.LanguageName,m.Level].filter(Boolean).join(': ')).join('\n') : duLieu[{summary:'Summary',certifications:'Certifications',activities:'Activities',interests:'Interests'}[khoa]];
        return chuoi?.trim() ? [taoDong('','','',chuoi)] : [];
    }
    function veCV() {
        clearTimeout(henVe); const duLieu = thuThapDuLieu(), mau = layMau(), giay = layPhanTu('cvSheet');
        const khungXem = goc.querySelector('.cv-studio-preview'), dangAn = !khungXem.getClientRects().length;
        if (dangAn) khungXem.classList.add('cv-measuring');
        giay.style.transform = 'none'; giay.style.setProperty('--cv-accent', duLieu.ThemeColor); giay.style.setProperty('--cv-header', duLieu.HeaderColor); giay.style.setProperty('--cv-paper', duLieu.BackgroundColor); giay.replaceChildren();
        const cacTrang = []; const idDaGan = new Set(); loiPhanTrang = false;
        function taoTrang() {
            const trang = taoPhanTu('article', '', 'cv-page cv-style-' + mau.Style + (cacTrang.length ? ' cv-page-continuation' : ''));
            const dau = taoPhanTu('header', '', 'cv-paper-header');
            if (duongDanAnh) { const anh = document.createElement('img'); anh.src = duongDanAnh; anh.alt = ''; anh.className = 'cv-paper-avatar'; dau.append(anh); }
            const thongTin = taoPhanTu('div', '', 'cv-paper-identity'), hoTen = taoPhanTu('h2', duLieu.FullName || 'Họ và tên', 'cv-paper-name');
            if (!cacTrang.length) hoTen.id = 'outName'; thongTin.append(hoTen, taoPhanTu('p', duLieu.JobTitle || 'Vị trí công việc', 'cv-paper-job'));
            const lienHe = taoPhanTu('div', '', 'cv-paper-contact'); for (const giaTri of [duLieu.Email,duLieu.Phone,duLieu.Address,duLieu.Website]) if (giaTri) lienHe.append(taoPhanTu('span', giaTri));
            thongTin.append(lienHe); dau.append(thongTin); trang.append(dau);
            const than = taoPhanTu('div', '', 'cv-paper-body'), chinh = taoPhanTu('div', '', 'cv-zone cv-zone-main'), phu = taoPhanTu('div', '', 'cv-zone cv-zone-side'); than.append(chinh); if (mau.TwoColumns) than.append(phu);
            trang.append(than); const chan = taoPhanTu('footer', '', 'cv-page-footer'); chan.append(taoPhanTu('span', duLieu.FullName || ''), taoPhanTu('span', '')); trang.append(chan); giay.append(trang);
            const ketQua = {trang, main:chinh, side:phu, chan}; cacTrang.push(ketQua); return ketQua;
        }
        function taoMuc(khoa, tiep) {
            const muc = taoPhanTu('section', '', 'cv-paper-section'); muc.dataset.cvBlock = khoa; muc.append(taoPhanTu('h3', tenKhoi[khoa] + (tiep ? ' (tiếp)' : '')));
            const noiDung = document.createElement('div');
            const id = {experience:'outExperienceList',education:'outEducationList',references:'outReferenceList',summary:'outSummary',skills:'outSkills',languages:'outLang'}[khoa];
            if (id && !idDaGan.has(id)) { noiDung.id = id; idDaGan.add(id); } muc.append(noiDung); return {muc,noiDung};
        }
        function vuotCot(cot) { return cot.scrollHeight > cot.clientHeight + 1; }
        taoTrang();
        for (const cotTen of mau.TwoColumns ? ['main','side'] : ['main']) {
            let viTriTrang = 0;
            for (const khoi of boCuc.filter(m => m.Zone === cotTen)) {
                const cacDong = layNoiDungKhoi(khoi.Key, duLieu); if (!cacDong.length) continue;
                let cot = cacTrang[viTriTrang][cotTen], muc = taoMuc(khoi.Key, false); cot.append(muc.muc);
                for (let chiSoDong = 0; chiSoDong < cacDong.length; chiSoDong++) {
                    let dong = cacDong[chiSoDong]; muc.noiDung.append(dong);
                    if (!vuotCot(cot)) continue;
                    dong.remove();
                    if (!muc.noiDung.children.length) { muc.muc.remove(); if (muc.noiDung.id) idDaGan.delete(muc.noiDung.id); }
                    function sangTrang() {
                        viTriTrang++; if (!cacTrang[viTriTrang]) taoTrang(); cot = cacTrang[viTriTrang][cotTen]; muc = taoMuc(khoi.Key, true); cot.append(muc.muc);
                    }
                    if (cot.children.length) sangTrang(); else { muc = taoMuc(khoi.Key, false); cot.append(muc.muc); }
                    muc.noiDung.append(dong);
                    while (vuotCot(cot)) {
                        const moTa = dong.querySelector('.cv-entry-description');
                        if (!moTa || !moTa.textContent.length) { loiPhanTrang = true; break; }
                        let cacTu = moTa.textContent.split(/(\s+)/); if (cacTu.length === 1) cacTu = Array.from(moTa.textContent);
                        let duoi = 1, tren = cacTu.length - 1, vua = 0;
                        while (duoi <= tren) { const giua = Math.floor((duoi + tren) / 2); moTa.textContent = cacTu.slice(0,giua).join(''); if (!vuotCot(cot)) { vua = giua; duoi = giua + 1; } else tren = giua - 1; }
                        if (!vua) { moTa.textContent = cacTu.join(''); loiPhanTrang = true; break; }
                        moTa.textContent = cacTu.slice(0,vua).join(''); const tiep = dong.cloneNode(true); tiep.querySelector('.cv-entry-description').textContent = cacTu.slice(vua).join(''); sangTrang(); dong = tiep; muc.noiDung.append(dong);
                        if (cacTrang.length > 25) { loiPhanTrang = true; break; }
                    }
                }
            }
        }
        soTrang = cacTrang.length;
        for (const [chiSo,trang] of cacTrang.entries()) trang.chan.lastChild.textContent = (chiSo+1) + ' / ' + soTrang;
        layPhanTu('cvPageCount').textContent = soTrang + ' trang';
        layPhanTu('cv-pagination-note').textContent = loiPhanTrang ? 'Một mục quá dài để trình bày. Hãy chia nội dung thành các đoạn ngắn hơn.' : soTrang > 6 ? 'CV đang vượt 6 trang. Hãy rút gọn nội dung trước khi lưu PDF.' : 'Các trang xem trước cũng là các trang được xuất PDF.';
        capNhatThuPhong();
        if (dangAn) khungXem.classList.remove('cv-measuring');
    }
    function capNhatThuPhong() {
        const giay = layPhanTu('cvSheet'); giay.style.transform = 'scale(' + mucThuPhong + ')';
        layPhanTu('cv-preview-scaler').style.width = (giay.offsetWidth * mucThuPhong) + 'px'; layPhanTu('cv-preview-scaler').style.height = (giay.offsetHeight * mucThuPhong) + 'px'; layPhanTu('cvZoomValue').textContent = Math.round(mucThuPhong*100) + '%';
    }
    function vuaKhung() { const khung=layPhanTu('cv-preview-viewport'); if (!khung.clientWidth) return; const kieu=getComputedStyle(khung); mucThuPhong = Math.max(.2, Math.min(1, (khung.clientWidth-parseFloat(kieu.paddingLeft)-parseFloat(kieu.paddingRight)-2) / (210 * 96/25.4))); capNhatThuPhong(); }
    layPhanTu('cv-zoom-out').addEventListener('click', () => { mucThuPhong = Math.max(.25, mucThuPhong-.1); capNhatThuPhong(); });
    layPhanTu('cv-zoom-in').addEventListener('click', () => { mucThuPhong = Math.min(1.5, mucThuPhong+.1); capNhatThuPhong(); });
    window.addEventListener('resize', vuaKhung);
    let chieuRongCu=0;
    new ResizeObserver(() => { const rong=layPhanTu('cv-preview-viewport').clientWidth; if (rong>0 && rong!==chieuRongCu) { chieuRongCu=rong; vuaKhung(); } }).observe(layPhanTu('cv-preview-viewport'));
    function kiemTraNoiDung(duLieu) {
        if (!duLieu.FullName || !duLieu.JobTitle || !duLieu.Email || !layPhanTu('inEmail').checkValidity()) throw new Error('Điền họ tên, vị trí công việc và email hợp lệ trước khi tiếp tục.');
        if (duLieu.ITSKills.length > 20 || duLieu.Languages.length > 20) throw new Error('Kỹ năng và ngoại ngữ tối đa 20 dòng mỗi mục.');
    }
    async function docDuLieuPhanHoi(phanHoi) {
        const duLieu = await phanHoi.json().catch(() => ({}));
        if (!phanHoi.ok || !duLieu.success) throw new Error(duLieu.message || 'Không hoàn tất được yêu cầu. Hãy kiểm tra phiên đăng nhập và thử lại.');
        return duLieu;
    }
    async function thucHienThaoTac(thaoTac) {
        if (dangXuLy) return; dangXuLy = true;
        const cacNut = [...goc.querySelectorAll('button,input,textarea,select')].map(n => [n,n.disabled]); cacNut.forEach(([n]) => {n.disabled=true;});
        try { await thaoTac(); } catch (loi) { hienThiThongBao(loi.message, true); }
        finally { cacNut.forEach(([n,voHieu]) => {n.disabled=voHieu;}); dangXuLy=false; }
    }
    async function taoPDF() {
        if (document.fonts) await document.fonts.ready; veCV();
        if (loiPhanTrang || soTrang > 6) throw new Error(layPhanTu('cv-pagination-note').textContent);
        const vungXuat = document.createElement('div'); vungXuat.className='cv-export'; vungXuat.style.cssText=layPhanTu('cvSheet').style.cssText; vungXuat.style.transform='none'; vungXuat.style.position='fixed'; vungXuat.style.left='-12000px'; vungXuat.style.top='0'; vungXuat.style.width='210mm'; vungXuat.style.fontFamily='"Segoe UI",Arial,sans-serif'; vungXuat.style.fontSize='10pt'; vungXuat.style.lineHeight='1.5'; vungXuat.style.color='#25323b';
        for (const trang of layPhanTu('cvSheet').children) {
            const banSao = trang.cloneNode(true);
            // html2pdf đặt trang trong container riêng, nên màu và font phải đi cùng trang.
            banSao.style.cssText = layPhanTu('cvSheet').style.cssText;
            banSao.style.transform = 'none'; banSao.style.margin = '0'; banSao.style.boxShadow = 'none';
            banSao.style.fontFamily = '"Segoe UI",Arial,sans-serif'; banSao.style.fontSize = '10pt'; banSao.style.lineHeight = '1.5'; banSao.style.color = '#25323b';
            vungXuat.append(banSao);
        }
        document.body.append(vungXuat);
        try {
            await Promise.all([...vungXuat.querySelectorAll('img')].map(anh => anh.decode()));
            let pdf;
            for (const [chiSo,trang] of [...vungXuat.children].entries()) {
                hienThiThongBao('Đang tạo PDF · trang ' + (chiSo+1) + '/' + soTrang + '…');
                const xuLy = html2pdf().set({margin:0,image:{type:'jpeg',quality:.95},html2canvas:{scale:2,useCORS:true,logging:false},jsPDF:{unit:'mm',format:'a4',orientation:'portrait'},pagebreak:{mode:[]}}).from(trang).toCanvas();
                const canvas = await xuLy.get('canvas');
                if (!pdf) { pdf = await xuLy.toPdf().get('pdf'); while (pdf.getNumberOfPages() > 1) pdf.deletePage(pdf.getNumberOfPages()); pdf.setPage(1); }
                else pdf.addPage();
                pdf.addImage(canvas.toDataURL('image/jpeg',.95),'JPEG',0,0,210,297,undefined,'FAST');
            }
            return pdf.output('blob');
        } finally { vungXuat.remove(); }
    }
    async function luuCV(taiVe) {
        const duLieu = thuThapDuLieu(); kiemTraNoiDung(duLieu); const pdf = await taoPDF();
        const bieuMau = new FormData(); bieuMau.append('modelJson',JSON.stringify(duLieu)); bieuMau.append('pdfFile',pdf,'cv.pdf'); if (anhTaiLen) bieuMau.append('avatarFile',anhTaiLen);
        const ketQua = await docDuLieuPhanHoi(await fetch('/Candidate/CV/Save',{method:'POST',headers:{'X-CSRF-TOKEN':maChongGiaMao},body:bieuMau}));
        maCV=ketQua.id; daThayDoi=false; layPhanTu('cvDirtyStatus').textContent='Đã lưu'; layPhanTu('cvStoredDownload').href=ketQua.url; layPhanTu('cvStoredDownload').hidden=false;
        history.replaceState(null,'','/Candidate/CV/Create?templateId='+maMau);
        ketQuaAI=ketQua.score != null ? {score:ketQua.score,suggestions:ketQua.suggestions} : null; hienThiDanhGia(); hienThiThongBao('Đã lưu nội dung, bố cục và PDF CV.');
        if (taiVe) { const url=URL.createObjectURL(pdf), nut=document.createElement('a'); nut.href=url; nut.download='CV_'+duLieu.FullName.replace(/[<>:"/\\|?*\u0000-\u001f]/g,'_')+'.pdf'; nut.click(); setTimeout(() => URL.revokeObjectURL(url),1000); }
    }
    layPhanTu('btnSaveCV').addEventListener('click', () => thucHienThaoTac(() => luuCV(false)));
    layPhanTu('btnDownloadPDF').addEventListener('click', () => thucHienThaoTac(() => luuCV(true)));
    layPhanTu('btnCheckAI').addEventListener('click', () => thucHienThaoTac(async () => {
        const duLieu=thuThapDuLieu(); kiemTraNoiDung(duLieu); hienThiThongBao('AI đang đánh giá nội dung CV…');
        ketQuaAI=await docDuLieuPhanHoi(await fetch('/Candidate/CVReview/Analyze',{method:'POST',headers:{'Content-Type':'application/json','X-CSRF-TOKEN':maChongGiaMao},body:JSON.stringify(duLieu)}));
        hienThiDanhGia(); hienThiThongBao('Đã đánh giá CV. Lưu CV để giữ kết quả cùng nội dung này.');
    }));
    window.addEventListener('beforeunload', suKien => { if (daThayDoi) {suKien.preventDefault();suKien.returnValue='';} });
    window.addEventListener('pagehide', () => {if(duongDanAnhTam)URL.revokeObjectURL(duongDanAnhTam);});
    hienThiBoCuc(); hienThiDanhGia(); veCV(); vuaKhung();
})();
