/**
 * avatar-cropper.js
 * Đặt tại: wwwroot/js/avatar-cropper.js
 *
 * Yêu cầu CDN trong layout:
 * <link  rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/cropperjs/1.6.2/cropper.min.css"/>
 * <script src="https://cdnjs.cloudflare.com/ajax/libs/cropperjs/1.6.2/cropper.min.js"></script>
 * <script src="https://cdn.jsdelivr.net/npm/sweetalert2@11"></script>
 */

'use strict';

// ── Trạng thái per-user (key = userId) ─────────────────────────────────────
const _avState = {};

function _getState(userId) {
    if (!_avState[userId]) {
        _avState[userId] = {
            cropper: null,
            sourceFile: null,
            scaleX: 1,
            scaleY: 1,
            modal: null
        };
    }
    return _avState[userId];
}

// ── Mở modal avatar ─────────────────────────────────────────────────────────
function openAvatarCropper(userId) {
    const modal = bootstrap.Modal.getOrCreateInstance(
        document.getElementById(`avatarModal_${userId}`)
    );
    _getState(userId).modal = modal;
    backToStep1(userId);
    modal.show();
}

// ── Xử lý file được chọn (input hoặc drop) ──────────────────────────────────
function handleFileSelect(userId, input) {
    const file = input.files && input.files[0];
    if (!file) return;
    _loadFileForCrop(userId, file);
}

function handleDrop(userId, event) {
    event.preventDefault();
    document.getElementById(`avDropZone_${userId}`)?.classList.remove('drag-over');
    const file = event.dataTransfer?.files?.[0];
    if (!file || !file.type.startsWith('image/')) {
        _showError('Vui lòng kéo thả file ảnh!');
        return;
    }
    _loadFileForCrop(userId, file);
}

// ── Load file → đọc DataURL → khởi động cropper ─────────────────────────────
function _loadFileForCrop(userId, file) {
    if (file.size > 10 * 1024 * 1024) {
        _showError('File quá lớn! Tối đa 10MB.');
        return;
    }
    const state = _getState(userId);
    state.sourceFile = file;

    const reader = new FileReader();
    reader.onload = (e) => {
        _showStep2(userId, e.target.result, file);
    };
    reader.readAsDataURL(file);
}

// ── Hiện thư viện avatar mẫu ─────────────────────────────────────────────────
const LIBRARY_AVATARS = [
    // Gradient initials avatars (SVG data URIs, màu sắc phong phú)
    { type: 'gradient', colors: ['#d4a017', '#f5c842'], label: 'Vàng' },
    { type: 'gradient', colors: ['#4f46e5', '#818cf8'], label: 'Tím' },
    { type: 'gradient', colors: ['#16a34a', '#4ade80'], label: 'Xanh lá' },
    { type: 'gradient', colors: ['#dc2626', '#f87171'], label: 'Đỏ' },
    { type: 'gradient', colors: ['#0891b2', '#38bdf8'], label: 'Xanh dương' },
    { type: 'gradient', colors: ['#d97706', '#fcd34d'], label: 'Cam' },
    { type: 'gradient', colors: ['#9333ea', '#d8b4fe'], label: 'Tím nhạt' },
    { type: 'gradient', colors: ['#0f766e', '#5eead4'], label: 'Ngọc lam' },
    { type: 'gradient', colors: ['#be185d', '#f9a8d4'], label: 'Hồng' },
    { type: 'gradient', colors: ['#1d4ed8', '#93c5fd'], label: 'Navy' },
    { type: 'gradient', colors: ['#92400e', '#d97706'], label: 'Nâu đất' },
    { type: 'gradient', colors: ['#374151', '#9ca3af'], label: 'Xám' },
];

function showAvatarLibrary(userId) {
    const lib = document.getElementById(`avLibrary_${userId}`);
    const grid = document.getElementById(`avLibGrid_${userId}`);
    if (!lib || !grid) return;

    lib.style.display = lib.style.display === 'none' ? 'block' : 'none';
    if (lib.style.display === 'none') return;

    // Render library items nếu chưa có
    if (grid.children.length === 0) {
        LIBRARY_AVATARS.forEach((av, idx) => {
            const el = document.createElement('div');
            el.className = 'av-lib-item';
            el.title = av.label;

            const svg = _makeGradientSVG(av.colors[0], av.colors[1], '?');
            el.style.background = `linear-gradient(135deg, ${av.colors[0]}, ${av.colors[1]})`;
            el.style.color = '#fff';
            el.style.fontSize = '1.1rem';
            el.style.fontFamily = "'Playfair Display', serif";
            el.style.fontWeight = '700';
            el.innerHTML = String.fromCodePoint(0x1F464); // 👤 mặc định
            el.style.background = `linear-gradient(135deg, ${av.colors[0]}, ${av.colors[1]})`;
            el.innerHTML = `<svg width="44" height="44" viewBox="0 0 44 44" xmlns="http://www.w3.org/2000/svg">
                <defs><linearGradient id="lg${idx}" x1="0%" y1="0%" x2="100%" y2="100%">
                <stop offset="0%" stop-color="${av.colors[0]}"/>
                <stop offset="100%" stop-color="${av.colors[1]}"/>
                </linearGradient></defs>
                <circle cx="22" cy="22" r="22" fill="url(#lg${idx})"/>
                <circle cx="22" cy="16" r="7" fill="rgba(255,255,255,0.9)"/>
                <ellipse cx="22" cy="38" rx="13" ry="10" fill="rgba(255,255,255,0.9)"/>
            </svg>`;

            el.addEventListener('click', () => {
                // Tạo Canvas, vẽ gradient circle, chuyển thành File
                const canvas = document.createElement('canvas');
                canvas.width = 256; canvas.height = 256;
                const ctx = canvas.getContext('2d');
                const grad = ctx.createLinearGradient(0, 0, 256, 256);
                grad.addColorStop(0, av.colors[0]);
                grad.addColorStop(1, av.colors[1]);
                ctx.fillStyle = grad;
                ctx.beginPath();
                ctx.arc(128, 128, 128, 0, Math.PI * 2);
                ctx.fill();

                // Người dùng icon
                ctx.fillStyle = 'rgba(255,255,255,0.92)';
                ctx.beginPath();
                ctx.arc(128, 90, 50, 0, Math.PI * 2);
                ctx.fill();
                ctx.beginPath();
                ctx.ellipse(128, 210, 82, 60, 0, 0, Math.PI * 2);
                ctx.fill();

                canvas.toBlob(blob => {
                    const file = new File([blob], `library_${idx}.png`, { type: 'image/png' });
                    grid.querySelectorAll('.av-lib-item').forEach(e => e.classList.remove('selected'));
                    el.classList.add('selected');
                    _loadFileForCrop(userId, file);
                }, 'image/png');
            });

            grid.appendChild(el);
        });
    }
}

function _makeGradientSVG(c1, c2, text) {
    return `data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='64' height='64'><defs><linearGradient id='g' x1='0%25' y1='0%25' x2='100%25' y2='100%25'><stop offset='0%25' stop-color='${encodeURIComponent(c1)}'/><stop offset='100%25' stop-color='${encodeURIComponent(c2)}'/></linearGradient></defs><circle cx='32' cy='32' r='32' fill='url(%23g)'/><text x='50%25' y='55%25' text-anchor='middle' fill='white' font-size='22' font-family='serif' font-weight='bold'>${text}</text></svg>`;
}

// ── Chuyển sang bước 2: Hiển thị cropper ────────────────────────────────────
function _showStep2(userId, dataUrl, file) {
    const state = _getState(userId);

    document.getElementById(`avStep1_${userId}`).style.display = 'none';
    document.getElementById(`avStep2_${userId}`).style.display = 'block';
    document.getElementById(`avStep2Back_${userId}`).style.display = '';
    document.getElementById(`avSaveBtn_${userId}`).style.display = '';

    const imgEl = document.getElementById(`avCropImg_${userId}`);
    imgEl.src = dataUrl;

    // Ghi kích thước gốc
    const tmp = new Image();
    tmp.onload = () => {
        const el = document.getElementById(`avOrigSize_${userId}`);
        if (el) el.textContent = `${tmp.naturalWidth} × ${tmp.naturalHeight} px`;
    };
    tmp.src = dataUrl;

    // Destroy cropper cũ nếu có
    if (state.cropper) {
        state.cropper.destroy();
        state.cropper = null;
    }

    state.scaleX = 1;
    state.scaleY = 1;

    // Khởi tạo Cropper.js
    state.cropper = new Cropper(imgEl, {
        aspectRatio: 1, // mặc định 1:1
        viewMode: 1,
        dragMode: 'move',
        autoCropArea: 0.85,
        restore: false,
        guides: true,
        center: true,
        highlight: false,
        cropBoxMovable: true,
        cropBoxResizable: true,
        toggleDragModeOnDblclick: false,
        preview: [
            document.getElementById(`avPreviewCircle_${userId}`),
            document.getElementById(`avPreviewSquare_${userId}`)
        ],
        crop(event) {
            const el = document.getElementById(`avCropSize_${userId}`);
            if (el) {
                el.textContent = `${Math.round(event.detail.width)} × ${Math.round(event.detail.height)} px`;
            }
        }
    });
}

// ── Quay về bước 1 ───────────────────────────────────────────────────────────
function backToStep1(userId) {
    const state = _getState(userId);
    if (state.cropper) {
        state.cropper.destroy();
        state.cropper = null;
    }
    state.sourceFile = null;
    state.scaleX = 1;
    state.scaleY = 1;

    const step1 = document.getElementById(`avStep1_${userId}`);
    const step2 = document.getElementById(`avStep2_${userId}`);
    const backBtn = document.getElementById(`avStep2Back_${userId}`);
    const saveBtn = document.getElementById(`avSaveBtn_${userId}`);

    if (step1) step1.style.display = 'block';
    if (step2) step2.style.display = 'none';
    if (backBtn) backBtn.style.display = 'none';
    if (saveBtn) saveBtn.style.display = 'none';

    // Reset file inputs
    document.querySelectorAll(`#avatarModal_${userId} input[type=file]`).forEach(el => {
        el.value = '';
    });
    // Reset library selection
    document.querySelectorAll(`#avLibGrid_${userId} .av-lib-item`).forEach(el => {
        el.classList.remove('selected');
    });
}

// ── Các action crop toolbar ──────────────────────────────────────────────────
function cropAction(userId, action) {
    const state = _getState(userId);
    const c = state.cropper;
    if (!c) return;

    switch (action) {
        case 'zoom-in': c.zoom(0.1); break;
        case 'zoom-out': c.zoom(-0.1); break;
        case 'rotate-left': c.rotate(-45); break;
        case 'rotate-right': c.rotate(45); break;
        case 'flip-h':
            state.scaleX *= -1;
            c.scaleX(state.scaleX);
            break;
        case 'flip-v':
            state.scaleY *= -1;
            c.scaleY(state.scaleY);
            break;
        case 'reset':
            c.reset();
            state.scaleX = 1;
            state.scaleY = 1;
            break;
    }
}

// ── Đặt tỉ lệ crop ──────────────────────────────────────────────────────────
function setRatio(userId, ratio) {
    const state = _getState(userId);
    if (state.cropper) state.cropper.setAspectRatio(ratio);

    // Toggle active
    document.querySelectorAll(`#avatarModal_${userId} .crop-ratio`).forEach(btn => {
        btn.classList.toggle('active',
            parseFloat(btn.dataset.ratio) === (ratio === 0 ? 0 : ratio));
    });
}

// ── Lưu crop ─────────────────────────────────────────────────────────────────
async function saveCrop(userId) {
    const state = _getState(userId);
    if (!state.cropper || !state.sourceFile) {
        _showError('Chưa có ảnh để lưu!');
        return;
    }

    // Lấy crop data
    const cropData = state.cropper.getData(true); // rounded = true

    // Hiện loader
    document.getElementById(`avSaveLoader_${userId}`).style.display = 'block';
    document.getElementById(`avSaveBtn_${userId}`).disabled = true;
    document.getElementById(`avStep2Back_${userId}`).disabled = true;

    try {
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value
            || document.querySelector('meta[name="csrf-token"]')?.content
            || '';

        const fd = new FormData();
        fd.append('userId', userId);
        fd.append('sourceFile', state.sourceFile);
        fd.append('cropX', cropData.x);
        fd.append('cropY', cropData.y);
        fd.append('cropWidth', cropData.width);
        fd.append('cropHeight', cropData.height);
        fd.append('rotate', cropData.rotate);
        fd.append('scaleX', cropData.scaleX);
        fd.append('scaleY', cropData.scaleY);
        fd.append('__RequestVerificationToken', token);

        const res = await fetch('/Avatar/SaveCrop', { method: 'POST', body: fd });
        const data = await res.json();

        if (data.success) {
            // Cập nhật UI avatar ngay lập tức
            _updateAvatarUI(userId, data.avatarUrl);

            // Đóng modal
            state.modal?.hide();

            // Toast success
            Swal.fire({
                icon: 'success',
                title: 'Đã lưu!',
                text: data.message,
                timer: 1800,
                showConfirmButton: false,
                toast: true,
                position: 'top-end'
            });
        } else {
            _showError(data.message || 'Lưu ảnh thất bại!');
        }
    } catch (err) {
        _showError('Không thể kết nối máy chủ!');
        console.error('Avatar save error:', err);
    } finally {
        document.getElementById(`avSaveLoader_${userId}`).style.display = 'none';
        document.getElementById(`avSaveBtn_${userId}`).disabled = false;
        document.getElementById(`avStep2Back_${userId}`).disabled = false;
    }
}

// ── Xóa avatar ───────────────────────────────────────────────────────────────
function deleteAvatar(userId, fullName) {
    Swal.fire({
        title: 'Xóa ảnh đại diện?',
        text: `Ảnh của "${fullName}" sẽ được xóa và hiển thị tên viết tắt.`,
        icon: 'question',
        showCancelButton: true,
        confirmButtonColor: '#c94b2a',
        cancelButtonColor: '#8a7560',
        confirmButtonText: 'Xóa',
        cancelButtonText: 'Hủy',
        reverseButtons: true
    }).then(async (r) => {
        if (!r.isConfirmed) return;

        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
        const fd = new FormData();
        fd.append('userId', userId);
        fd.append('__RequestVerificationToken', token);

        try {
            const res = await fetch('/Avatar/Delete', { method: 'POST', body: fd });
            const data = await res.json();
            if (data.success) {
                _clearAvatarUI(userId);
                Swal.fire({ icon: 'success', title: 'Đã xóa!', timer: 1500, showConfirmButton: false, toast: true, position: 'top-end' });
            } else {
                _showError(data.message);
            }
        } catch {
            _showError('Không thể kết nối máy chủ!');
        }
    });
}

// ── Cập nhật UI sau khi lưu/xóa ─────────────────────────────────────────────
function _updateAvatarUI(userId, avatarUrl) {
    const imgEls = document.querySelectorAll(`[id="avImg_${userId}"]`);
    const initEls = document.querySelectorAll(`[id="avInitials_${userId}"]`);
    const delBtns = document.querySelectorAll(`[id="avDelBtn_${userId}"]`);

    const cacheBuster = `${avatarUrl}?v=${Date.now()}`;

    imgEls.forEach(img => {
        img.src = cacheBuster;
        img.style.display = 'block';
    });
    initEls.forEach(el => el.style.display = 'none');
    delBtns.forEach(btn => btn.classList.remove('d-none'));
}

function _clearAvatarUI(userId) {
    const imgEls = document.querySelectorAll(`[id="avImg_${userId}"]`);
    const initEls = document.querySelectorAll(`[id="avInitials_${userId}"]`);
    const delBtns = document.querySelectorAll(`[id="avDelBtn_${userId}"]`);

    imgEls.forEach(img => { img.src = ''; img.style.display = 'none'; });
    initEls.forEach(el => el.style.display = '');
    delBtns.forEach(btn => btn.classList.add('d-none'));
}

function _showError(msg) {
    Swal.fire({ icon: 'error', title: 'Lỗi!', text: msg, confirmButtonColor: '#d4a017' });
}
