using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace QuanLiSanCauLong.ViewModels
{
    /// <summary>
    /// ViewModel cho thông báo/Message
    /// </summary>
    public class AlertViewModel
    {
        public string Type { get; set; } // success, error, warning, info
        public string Title { get; set; }
        public string Message { get; set; }
        public bool AutoClose { get; set; }
        public int AutoCloseDelay { get; set; } = 3000; // milliseconds
    }

    /// <summary>
    /// ViewModel cho phân trang
    /// </summary>
    public class PaginationViewModel
    {
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public bool HasPrevious => CurrentPage > 1;
        public bool HasNext => CurrentPage < TotalPages;
        public string BaseUrl { get; set; }
    }

    /// <summary>
    /// ViewModel cho breadcrumb
    /// </summary>
    public class BreadcrumbViewModel
    {
        public List<BreadcrumbItem> Items { get; set; }
    }

    public class BreadcrumbItem
    {
        public string Title { get; set; }
        public string Url { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// ViewModel cho thống kê card
    /// </summary>
    public class StatCardViewModel
    {
        public string Title { get; set; }
        public string Value { get; set; }
        public string Icon { get; set; }
        public string Color { get; set; } // primary, success, danger, warning, info
        public string Change { get; set; }
        public string ChangeType { get; set; } // up, down, neutral
        public string Link { get; set; }
    }

    /// <summary>
    /// ViewModel cho contact form
    /// </summary>
/*    public class ContactViewModel
    {
        [Required(ErrorMessage = "Họ tên không được để trống")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [Display(Name = "Số điện thoại")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Chủ đề không được để trống")]
        [Display(Name = "Chủ đề")]
        public string Subject { get; set; }

        [Required(ErrorMessage = "Nội dung không được để trống")]
        [Display(Name = "Nội dung")]
        public string Message { get; set; }
    }*/

    /// <summary>
    /// ViewModel cho search/filter chung
    /// </summary>
    public class SearchFilterViewModel
    {
        public string Keyword { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string Status { get; set; }
        public string SortBy { get; set; }
        public string SortOrder { get; set; } // asc, desc
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    /// <summary>
    /// ViewModel cho modal confirm
    /// </summary>
    public class ConfirmDialogViewModel
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public string ConfirmText { get; set; } = "Xác nhận";
        public string CancelText { get; set; } = "Hủy";
        public string ConfirmButtonClass { get; set; } = "btn-primary";
        public string Action { get; set; }
        public Dictionary<string, string> Parameters { get; set; }
    }

    /// <summary>
    /// ViewModel cho file upload
    /// </summary>
    public class FileUploadViewModel
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public long FileSize { get; set; }
        public string FileType { get; set; }
        public DateTime UploadedAt { get; set; }
        public string UploadedBy { get; set; }
    }

    /// <summary>
    /// ViewModel cho notification
    /// </summary>
    public class NotificationViewModel
    {
        public int NotificationId { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string Type { get; set; } // info, success, warning, error
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Link { get; set; }
        public string Icon { get; set; }
        public string TimeAgo { get; set; }
    }

    /// <summary>
    /// ViewModel cho activity log
    /// </summary>
    public class ActivityLogViewModel
    {
        public int LogId { get; set; }
        public string UserName { get; set; }
        public string Action { get; set; }
        public string Entity { get; set; }
        public string Description { get; set; }
        public string IpAddress { get; set; }
        public DateTime Timestamp { get; set; }
        public string TimeAgo { get; set; }
    }

    /// <summary>
    /// ViewModel cho settings
    /// </summary>
    public class SettingsViewModel
    {
        [Display(Name = "Tên hệ thống")]
        public string SystemName { get; set; }

        [Display(Name = "Email hệ thống")]
        [EmailAddress]
        public string SystemEmail { get; set; }

        [Display(Name = "Số điện thoại")]
        [Phone]
        public string SystemPhone { get; set; }

        [Display(Name = "Địa chỉ")]
        public string SystemAddress { get; set; }

        [Display(Name = "Logo")]
        public string LogoUrl { get; set; }

        [Display(Name = "Favicon")]
        public string FaviconUrl { get; set; }

        [Display(Name = "Múi giờ")]
        public string TimeZone { get; set; }

        [Display(Name = "Ngôn ngữ")]
        public string Language { get; set; }

        [Display(Name = "Định dạng tiền tệ")]
        public string CurrencyFormat { get; set; }

        [Display(Name = "Bật bảo trì")]
        public bool MaintenanceMode { get; set; }

        [Display(Name = "Thông báo bảo trì")]
        public string MaintenanceMessage { get; set; }
    }

    /// <summary>
    /// ViewModel cho email settings
    /// </summary>
    public class EmailSettingsViewModel
    {
        [Required]
        [Display(Name = "SMTP Host")]
        public string SmtpHost { get; set; }

        [Required]
        [Display(Name = "SMTP Port")]
        public int SmtpPort { get; set; }

        [Required]
        [Display(Name = "SMTP Username")]
        public string SmtpUsername { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "SMTP Password")]
        public string SmtpPassword { get; set; }

        [Display(Name = "Enable SSL")]
        public bool EnableSsl { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "From Email")]
        public string FromEmail { get; set; }

        [Display(Name = "From Name")]
        public string FromName { get; set; }

        [Display(Name = "Test Email")]
        [EmailAddress]
        public string TestEmail { get; set; }
    }

    /// <summary>
    /// ViewModel cho chart data
    /// </summary>
    public class ChartDataViewModel
    {
        public List<string> Labels { get; set; }
        public List<ChartDatasetViewModel> Datasets { get; set; }
    }

    public class ChartDatasetViewModel
    {
        public string Label { get; set; }
        public List<decimal> Data { get; set; }
        public string BackgroundColor { get; set; }
        public string BorderColor { get; set; }
        public int BorderWidth { get; set; } = 1;
    }

    /// <summary>
    /// ViewModel cho export options
    /// </summary>
    public class ExportOptionsViewModel
    {
        public string Format { get; set; } // Excel, PDF, CSV
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public List<string> Columns { get; set; }
        public string FileName { get; set; }
    }

    /// <summary>
    /// ViewModel cho API response
    /// </summary>
    public class ApiResponseViewModel<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
        public List<string> Errors { get; set; }
    }

    /// <summary>
    /// ViewModel cho dropdown/select options
    /// </summary>
    public class SelectOptionViewModel
    {
        public string Value { get; set; }
        public string Text { get; set; }
        public bool Selected { get; set; }
        public bool Disabled { get; set; }
    }
}
