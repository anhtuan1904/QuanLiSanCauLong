// Booking Page JavaScript

// Global variables
let currentDate = selectedDate || new Date();
let currentPricePerHour = 0;

// Initialize on page load
document.addEventListener('DOMContentLoaded', function () {
    initializeCalendar();
    initializeEventListeners();
});

// ==================== CALENDAR FUNCTIONS ====================
function initializeCalendar() {
    renderCalendar(currentDate);
}

function renderCalendar(date) {
    const year = date.getFullYear();
    const month = date.getMonth();

    // Việt hóa tên tháng
    const monthNames = ["Tháng 1", "Tháng 2", "Tháng 3", "Tháng 4", "Tháng 5", "Tháng 6",
        "Tháng 7", "Tháng 8", "Tháng 9", "Tháng 10", "Tháng 11", "Tháng 12"];

    const monthDisplay = document.getElementById('currentMonth');
    if (monthDisplay) monthDisplay.textContent = `${monthNames[month]} ${year}`;

    const calendarGrid = document.getElementById('calendarGrid');
    if (!calendarGrid) return;
    calendarGrid.innerHTML = '';

    // Việt hóa tiêu đề thứ
    const dayHeaders = ['CN', 'T2', 'T3', 'T4', 'T5', 'T6', 'T7'];
    dayHeaders.forEach(day => {
        const header = document.createElement('div');
        header.className = 'calendar-day-header';
        header.textContent = day;
        calendarGrid.appendChild(header);
    });

    const firstDay = new Date(year, month, 1).getDay();
    const daysInMonth = new Date(year, month + 1, 0).getDate();

    for (let i = 0; i < firstDay; i++) {
        calendarGrid.appendChild(document.createElement('div'));
    }

    const today = new Date();
    today.setHours(0, 0, 0, 0);

    for (let day = 1; day <= daysInMonth; day++) {
        const dateCell = document.createElement('div');
        dateCell.className = 'calendar-date';
        dateCell.textContent = day;

        const cellDate = new Date(year, month, day);
        cellDate.setHours(0, 0, 0, 0);

        if (cellDate < today) {
            dateCell.classList.add('disabled');
        } else {
            dateCell.onclick = () => selectDate(year, month, day);
            if (currentDate.getDate() === day && currentDate.getMonth() === month && currentDate.getFullYear() === year) {
                dateCell.classList.add('selected');
            }
        }
        calendarGrid.appendChild(dateCell);
    }
}

function selectDate(year, month, day) {
    currentDate = new Date(year, month, day);
    renderCalendar(currentDate);

    // Cập nhật text hiển thị ngày (ID displayDate trong HTML mới)
    const displayDate = document.getElementById('displayDate');
    if (displayDate) displayDate.textContent = `${day}/${month + 1}/${year}`;

    loadAvailableCourts();
}

// ==================== CALENDAR NAVIGATION ====================
document.getElementById('prevMonth')?.addEventListener('click', function () {
    currentDate.setMonth(currentDate.getMonth() - 1);
    renderCalendar(currentDate);
});

document.getElementById('nextMonth')?.addEventListener('click', function () {
    currentDate.setMonth(currentDate.getMonth() + 1);
    renderCalendar(currentDate);
});

// ==================== LOAD AVAILABLE COURTS ====================
function loadAvailableCourts() {
    const dateStr = currentDate.toISOString().split('T')[0];

    fetch(`/Booking/GetAvailableCourts?facilityId=${facilityId}&date=${dateStr}`)
        .then(response => response.json())
        .then(data => {
            renderCourts(data.courts);
        })
        .catch(error => {
            console.error('Error loading courts:', error);
            showAlert('Không thể tải danh sách sân', 'error');
        });
}

function renderCourts(courts) {
    const container = document.querySelector('.courts-list-grid'); // Cập nhật selector theo HTML mới
    if (!container) return;

    container.innerHTML = '';
    courts.forEach(court => {
        const courtCard = document.createElement('div');
        courtCard.className = 'court-modern-card';

        courtCard.innerHTML = `
            <div class="court-header">
                <h4>${court.courtName}</h4>
                <span class="badge-status ${court.isAvailable ? 'available' : 'unavailable'}">
                    ${court.isAvailable ? 'Còn trống' : 'Đã đặt'}
                </span>
            </div>
            <div class="court-body">
                <div class="court-location-info">
                    <i class="fas fa-map-marker-alt"></i>
                    <span>Sân cầu lông - ${court.area || 'Khu vực chính'}</span>
                </div>
                <div class="court-type-info">
                    <strong>Loại sân:</strong> ${court.courtType}
                </div>
                <div class="court-price-info" style="margin-top: 5px; font-weight: 600;">
                    <strong>Giá:</strong> ${court.pricePerHour.toLocaleString()}đ/giờ
                </div>
            </div>
            ${court.isAvailable ? `
                <div class="court-footer">
                    <button class="btn-select-modern" onclick="openBookingModal(${court.courtId}, '${court.courtName}', '${court.courtType}', ${court.pricePerHour})">
                        Chọn sân này
                    </button>
                </div>` : ''}
        `;
        container.appendChild(courtCard);
    });
}

function createCourtCard(court) {
    const card = document.createElement('div');
    card.className = 'court-card';
    card.dataset.courtId = court.courtId;

    card.innerHTML = `
        <div class="court-card-header">
            <h4>${court.courtName}</h4>
            <span class="status-badge ${court.isAvailable ? 'available' : 'unavailable'}">
                ${court.isAvailable ? 'Available' : 'Unavailable'}
            </span>
        </div>
        <div class="court-card-body">
            <div class="court-location-info">
                <i class="fas fa-map-marker-alt"></i>
                <span>${court.area}</span>
            </div>
            <div class="court-type">
                <strong>Type:</strong> ${court.courtType}
            </div>
            <div class="court-price">
                <strong>Price:</strong> ${court.pricePerHour.toLocaleString()}đ/giờ
            </div>
            ${court.isAvailable ?
            `<button class="btn btn-book" onclick="openBookingModal(${court.courtId}, '${court.courtName}', '${court.courtType}', ${court.pricePerHour})">
                    <i class="fas fa-calendar-plus"></i>
                    Đặt sân
                </button>` :
            ''}
        </div>
    `;

    return card;
}

// ==================== BOOKING MODAL ====================
function openBookingModal(courtId, courtName, courtType, pricePerHour) {
    const modal = document.getElementById('bookingModal');

    // Set values
    document.getElementById('modalCourtId').value = courtId;
    document.getElementById('modalCourtName').value = courtName;
    document.getElementById('modalCourtType').value = courtType;

    const dateStr = currentDate.toLocaleDateString('vi-VN');
    document.getElementById('modalDate').value = dateStr;
    document.getElementById('modalBookingDate').value = currentDate.toISOString().split('T')[0];

    currentPricePerHour = pricePerHour;

    // Reset form
    document.getElementById('StartTime').selectedIndex = 0;
    document.getElementById('EndTime').selectedIndex = 0;
    document.getElementById('Notes').value = '';
    document.getElementById('totalPrice').textContent = '0đ';

    // Show modal
    modal.classList.add('show');
    modal.style.display = 'flex';
}

// Close modal
document.querySelector('.modal .close')?.addEventListener('click', function () {
    closeBookingModal();
});

function closeBookingModal() {
    const modal = document.getElementById('bookingModal');
    modal.classList.remove('show');
    setTimeout(() => {
        modal.style.display = 'none';
    }, 300);
}

// Close modal when clicking outside
window.addEventListener('click', function (event) {
    const modal = document.getElementById('bookingModal');
    if (event.target === modal) {
        closeBookingModal();
    }
});

// ==================== PRICE CALCULATION ====================
function calculateTotalPrice() {
    const startTime = document.getElementById('StartTime').value;
    const endTime = document.getElementById('EndTime').value;

    if (!startTime || !endTime) return;

    const start = parseTime(startTime);
    const end = parseTime(endTime);

    if (end <= start) {
        document.getElementById('totalPrice').textContent = 'Giờ không hợp lệ';
        return;
    }

    const hours = (end - start) / (1000 * 60 * 60);
    const total = hours * currentPricePerHour;

    document.getElementById('totalPrice').textContent = total.toLocaleString() + 'đ';
}

function parseTime(timeStr) {
    const parts = timeStr.split(':');
    const hours = parseInt(parts[0]);
    const minutes = parseInt(parts[1]);
    return new Date(2000, 0, 1, hours, minutes);
}

document.getElementById('StartTime')?.addEventListener('change', calculateTotalPrice);
document.getElementById('EndTime')?.addEventListener('change', calculateTotalPrice);

// ==================== BOOKING FORM SUBMISSION ====================
document.getElementById('bookingForm')?.addEventListener('submit', function (e) {
    e.preventDefault();

    const formData = new FormData(this);
    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

    fetch('/Booking/Create', {
        method: 'POST',
        headers: {
            'RequestVerificationToken': token
        },
        body: formData
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                showAlert(data.message, 'success');
                closeBookingModal();
                loadAvailableCourts();
            } else {
                showAlert(data.message, 'error');
            }
        })
        .catch(error => {
            console.error('Error:', error);
            showAlert('Có lỗi xảy ra khi đặt sân', 'error');
        });
});

// ==================== REVIEW FORM SUBMISSION ====================
document.getElementById('reviewForm')?.addEventListener('submit', function (e) {
    e.preventDefault();

    const formData = new FormData(this);
    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

    fetch('/Booking/SubmitReview', {
        method: 'POST',
        headers: {
            'RequestVerificationToken': token
        },
        body: formData
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                showAlert(data.message, 'success');
                this.reset();
            } else {
                showAlert(data.message, 'error');
            }
        })
        .catch(error => {
            console.error('Error:', error);
            showAlert('Có lỗi xảy ra khi gửi đánh giá', 'error');
        });
});

// ==================== GALLERY IMAGE SWITCHER ====================
function changeImage(thumbnail) {
    const mainImage = document.getElementById('mainImage');
    const thumbnails = document.querySelectorAll('.thumbnail');

    // Remove active class from all thumbnails
    thumbnails.forEach(thumb => thumb.classList.remove('active'));

    // Add active class to clicked thumbnail
    thumbnail.classList.add('active');

    // Change main image
    mainImage.src = thumbnail.src.replace('/thumb-', '/main-');
}

// ==================== TAB SWITCHING ====================
function initializeEventListeners() {
    document.querySelectorAll('.tab-btn').forEach(btn => {
        btn.addEventListener('click', function () {
            const tab = this.dataset.tab;
            switchTab(tab);
        });
    });
}

function switchTab(tabName) {
    const tabButtons = document.querySelectorAll('.tab-btn');
    tabButtons.forEach(btn => btn.classList.remove('active'));

    const activeTab = document.querySelector(`[data-tab="${tabName}"]`);
    if (activeTab) {
        activeTab.classList.add('active');
    }

    if (tabName === 'mybookings') {
        window.location.href = '/Booking/MyBookings';
    }
}

// ==================== FAVORITE BUTTON ====================
document.getElementById('favoriteBtn')?.addEventListener('click', function () {
    const icon = this.querySelector('i');

    if (icon.classList.contains('far')) {
        icon.classList.remove('far');
        icon.classList.add('fas');
        this.style.borderColor = '#ef4444';
        this.style.color = '#ef4444';
        showAlert('Đã thêm vào yêu thích', 'success');
    } else {
        icon.classList.remove('fas');
        icon.classList.add('far');
        this.style.borderColor = '';
        this.style.color = '';
        showAlert('Đã xóa khỏi yêu thích', 'success');
    }
});

// ==================== SCROLL TO BOOKING ====================
function scrollToBooking() {
    const bookingSection = document.getElementById('bookingSection');
    if (bookingSection) {
        bookingSection.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }
}

// ==================== ALERT FUNCTION ====================
function showAlert(message, type) {
    const alertDiv = document.createElement('div');
    alertDiv.className = `alert alert-${type}`;
    alertDiv.innerHTML = `
        <i class="fas fa-${type === 'success' ? 'check-circle' : 'exclamation-circle'}"></i>
        ${message}
    `;

    // Insert at top of main content
    const mainContent = document.querySelector('.main-content');
    mainContent.insertBefore(alertDiv, mainContent.firstChild);

    // Auto remove after 5 seconds
    setTimeout(() => {
        alertDiv.style.opacity = '0';
        setTimeout(() => alertDiv.remove(), 300);
    }, 5000);
}

// ==================== ANIMATION ON SCROLL ====================
function isElementInViewport(el) {
    const rect = el.getBoundingClientRect();
    return (
        rect.top >= 0 &&
        rect.left >= 0 &&
        rect.bottom <= (window.innerHeight || document.documentElement.clientHeight) &&
        rect.right <= (window.innerWidth || document.documentElement.clientWidth)
    );
}

function handleScrollAnimation() {
    const animatedElements = document.querySelectorAll('.court-card, .rating-bar-item');

    animatedElements.forEach(element => {
        if (isElementInViewport(element)) {
            element.style.opacity = '1';
            element.style.transform = 'translateY(0)';
        }
    });
}

// Initialize animations
document.querySelectorAll('.court-card, .rating-bar-item').forEach(element => {
    element.style.opacity = '0';
    element.style.transform = 'translateY(20px)';
    element.style.transition = 'all 0.5s ease';
});

window.addEventListener('scroll', handleScrollAnimation);
handleScrollAnimation();