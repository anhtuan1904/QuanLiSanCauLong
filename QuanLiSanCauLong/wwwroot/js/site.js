// ===================================
// GLOBAL FUNCTIONS
// ===================================

// Show loading overlay
function showLoading() {
    if ($('#loadingOverlay').length === 0) {
        $('body').append(`
            <div id="loadingOverlay" class="loading-overlay">
                <div class="spinner-border text-light spinner-border-lg" role="status">
                    <span class="visually-hidden">Loading...</span>
                </div>
            </div>
        `);
    }
    $('#loadingOverlay').fadeIn();
}

// Hide loading overlay
function hideLoading() {
    $('#loadingOverlay').fadeOut(function () {
        $(this).remove();
    });
}

// Format currency VND
function formatCurrency(amount) {
    return new Intl.NumberFormat('vi-VN', {
        style: 'currency',
        currency: 'VND'
    }).format(amount);
}

// Format date
function formatDate(date, format = 'dd/MM/yyyy') {
    const d = new Date(date);
    const day = String(d.getDate()).padStart(2, '0');
    const month = String(d.getMonth() + 1).padStart(2, '0');
    const year = d.getFullYear();

    if (format === 'dd/MM/yyyy') {
        return `${day}/${month}/${year}`;
    } else if (format === 'yyyy-MM-dd') {
        return `${year}-${month}-${day}`;
    }
    return date;
}

// Format time
function formatTime(timeSpan) {
    const parts = timeSpan.split(':');
    return `${parts[0]}:${parts[1]}`;
}

// Confirm dialog
function confirmDialog(message, callback) {
    if (typeof Swal !== 'undefined') {
        Swal.fire({
            title: 'Xác nhận',
            text: message,
            icon: 'question',
            showCancelButton: true,
            confirmButtonColor: '#198754',
            cancelButtonColor: '#6c757d',
            confirmButtonText: 'Xác nhận',
            cancelButtonText: 'Hủy'
        }).then((result) => {
            if (result.isConfirmed) {
                callback();
            }
        });
    } else {
        if (confirm(message)) {
            callback();
        }
    }
}

// Success notification
function showSuccess(message) {
    if (typeof toastr !== 'undefined') {
        toastr.success(message);
    } else {
        alert(message);
    }
}

// Error notification
function showError(message) {
    if (typeof toastr !== 'undefined') {
        toastr.error(message);
    } else {
        alert(message);
    }
}

// Info notification
function showInfo(message) {
    if (typeof toastr !== 'undefined') {
        toastr.info(message);
    } else {
        alert(message);
    }
}

// Warning notification
function showWarning(message) {
    if (typeof toastr !== 'undefined') {
        toastr.warning(message);
    } else {
        alert(message);
    }
}

// ===================================
// AJAX HELPERS
// ===================================

function ajaxGet(url, successCallback, errorCallback) {
    showLoading();
    $.ajax({
        url: url,
        type: 'GET',
        success: function (response) {
            hideLoading();
            if (successCallback) successCallback(response);
        },
        error: function (xhr) {
            hideLoading();
            if (errorCallback) {
                errorCallback(xhr);
            } else {
                showError('Có lỗi xảy ra! Vui lòng thử lại.');
            }
        }
    });
}

function ajaxPost(url, data, successCallback, errorCallback) {
    showLoading();
    $.ajax({
        url: url,
        type: 'POST',
        data: data,
        headers: {
            'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
        },
        success: function (response) {
            hideLoading();
            if (successCallback) successCallback(response);
        },
        error: function (xhr) {
            hideLoading();
            if (errorCallback) {
                errorCallback(xhr);
            } else {
                showError('Có lỗi xảy ra! Vui lòng thử lại.');
            }
        }
    });
}

function ajaxPostJson(url, data, successCallback, errorCallback) {
    showLoading();
    $.ajax({
        url: url,
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(data),
        headers: {
            'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
        },
        success: function (response) {
            hideLoading();
            if (successCallback) successCallback(response);
        },
        error: function (xhr) {
            hideLoading();
            if (errorCallback) {
                errorCallback(xhr);
            } else {
                showError('Có lỗi xảy ra! Vui lòng thử lại.');
            }
        }
    });
}

// ===================================
// FORM VALIDATION
// ===================================

// Validate email
function isValidEmail(email) {
    const regex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return regex.test(email);
}

// Validate phone number (Vietnam)
function isValidPhone(phone) {
    const regex = /^(0|\+84)[0-9]{9}$/;
    return regex.test(phone);
}

// Validate required field
function validateRequired(value, fieldName) {
    if (!value || value.trim() === '') {
        showError(`${fieldName} không được để trống`);
        return false;
    }
    return true;
}

// ===================================
// DATATABLE HELPER
// ===================================

function initDataTable(tableId, options = {}) {
    const defaultOptions = {
        language: {
            url: '//cdn.datatables.net/plug-ins/1.13.7/i18n/vi.json'
        },
        responsive: true,
        pageLength: 25,
        order: [[0, 'desc']],
        dom: "<'row'<'col-sm-12 col-md-6'l><'col-sm-12 col-md-6'f>>" +
            "<'row'<'col-sm-12'tr>>" +
            "<'row'<'col-sm-12 col-md-5'i><'col-sm-12 col-md-7'p>>"
    };

    const mergedOptions = { ...defaultOptions, ...options };

    if (typeof $.fn.DataTable !== 'undefined') {
        return $(tableId).DataTable(mergedOptions);
    }
}

// ===================================
// DATE PICKER HELPER
// ===================================

function initDatePicker(selector, options = {}) {
    const defaultOptions = {
        dateFormat: 'dd/mm/yy',
        changeMonth: true,
        changeYear: true,
        yearRange: '-1:+1',
        minDate: 0,
        maxDate: '+30d'
    };

    const mergedOptions = { ...defaultOptions, ...options };

    if (typeof $.fn.datepicker !== 'undefined') {
        $(selector).datepicker(mergedOptions);
    }
}

// ===================================
// SELECT2 HELPER
// ===================================

function initSelect2(selector, options = {}) {
    const defaultOptions = {
        theme: 'bootstrap-5',
        width: '100%',
        language: 'vi'
    };

    const mergedOptions = { ...defaultOptions, ...options };

    if (typeof $.fn.select2 !== 'undefined') {
        $(selector).select2(mergedOptions);
    }
}

// ===================================
// DOCUMENT READY
// ===================================

$(document).ready(function () {

    // Auto-hide alerts after 5 seconds
    setTimeout(function () {
        $('.alert:not(.alert-permanent)').fadeOut('slow');
    }, 5000);

    // Smooth scroll to anchor links
    $('a[href^="#"]').on('click', function (event) {
        const target = $(this.getAttribute('href'));
        if (target.length) {
            event.preventDefault();
            $('html, body').stop().animate({
                scrollTop: target.offset().top - 100
            }, 1000);
        }
    });

    // Auto-close dropdown on click outside
    $(document).on('click', function (e) {
        if (!$(e.target).closest('.dropdown').length) {
            $('.dropdown-menu').removeClass('show');
        }
    });

    // Confirm delete buttons
    $('.btn-delete, .delete-btn').on('click', function (e) {
        e.preventDefault();
        const href = $(this).attr('href') || $(this).data('href');
        const message = $(this).data('confirm') || 'Bạn có chắc chắn muốn xóa?';

        confirmDialog(message, function () {
            window.location.href = href;
        });
    });

    // Confirm forms with class confirm-form
    $('.confirm-form').on('submit', function (e) {
        const message = $(this).data('confirm') || 'Bạn có chắc chắn?';
        if (!confirm(message)) {
            e.preventDefault();
            return false;
        }
    });

    // Auto format currency inputs
    $('.currency-input').on('blur', function () {
        const value = parseFloat($(this).val().replace(/[^0-9.-]+/g, ''));
        if (!isNaN(value)) {
            $(this).val(formatCurrency(value));
        }
    });

    // Auto format phone inputs
    $('.phone-input').on('input', function () {
        let value = $(this).val().replace(/\D/g, '');
        if (value.length > 10) {
            value = value.substring(0, 10);
        }
        $(this).val(value);
    });

    // Initialize tooltips
    if (typeof bootstrap !== 'undefined') {
        const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
        tooltipTriggerList.map(function (tooltipTriggerEl) {
            return new bootstrap.Tooltip(tooltipTriggerEl);
        });
    }

    // Back to top button
    if ($('#backToTop').length === 0) {
        $('body').append('<button id="backToTop" class="btn btn-success position-fixed" style="bottom: 20px; right: 20px; display: none; z-index: 1000;"><i class="bi bi-arrow-up"></i></button>');
    }

    $(window).scroll(function () {
        if ($(this).scrollTop() > 300) {
            $('#backToTop').fadeIn();
        } else {
            $('#backToTop').fadeOut();
        }
    });

    $('#backToTop').on('click', function () {
        $('html, body').animate({ scrollTop: 0 }, 800);
        return false;
    });

    // Print button
    $('.btn-print').on('click', function (e) {
        e.preventDefault();
        window.print();
    });

    // Auto-focus first input in modal
    $('.modal').on('shown.bs.modal', function () {
        $(this).find('input:text:visible:first').focus();
    });

    // Number input validation
    $('.number-only').on('keypress', function (e) {
        const charCode = e.which ? e.which : e.keyCode;
        if (charCode > 31 && (charCode < 48 || charCode > 57)) {
            e.preventDefault();
        }
    });

    // Prevent double submit
    $('form').on('submit', function () {
        $(this).find('button[type="submit"]').prop('disabled', true);
        setTimeout(function () {
            $('button[type="submit"]').prop('disabled', false);
        }, 3000);
    });
});

// ===================================
// EXPORT FUNCTIONS
// ===================================

window.badmintonBooking = {
    showLoading,
    hideLoading,
    formatCurrency,
    formatDate,
    formatTime,
    confirmDialog,
    showSuccess,
    showError,
    showInfo,
    showWarning,
    ajaxGet,
    ajaxPost,
    ajaxPostJson,
    isValidEmail,
    isValidPhone,
    validateRequired,
    initDataTable,
    initDatePicker,
    initSelect2
};