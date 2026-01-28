using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace QuanLiSanCauLong.ViewModels
{
    /// <summary>
    /// ViewModel cho danh sách sân (Admin)
    /// </summary>
    public class CourtListViewModel
    {
        public int? FacilityId { get; set; }
        public string FacilityName { get; set; }
        public List<CourtItemViewModel> Courts { get; set; }
        public string FilterCourtType { get; set; }
        public string FilterStatus { get; set; }
        public int TotalCount { get; set; }
    }

    /// <summary>
    /// ViewModel cho từng sân trong danh sách
    /// </summary>
    public class CourtItemViewModel
    {
        public int CourtId { get; set; }
        public string CourtNumber { get; set; }
        public string CourtType { get; set; }
        public string FacilityName { get; set; }
        public string Status { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public int TodayBookings { get; set; }
        public decimal TodayRevenue { get; set; }
    }

    /// <summary>
    /// ViewModel cho quản lý sân (Admin)
    /// </summary>
    public class CourtManageViewModel
    {
        public int CourtId { get; set; }

        [Required(ErrorMessage = "Cơ sở không được để trống")]
        [Display(Name = "Cơ sở")]
        public int FacilityId { get; set; }

        [Required(ErrorMessage = "Số sân không được để trống")]
        [Display(Name = "Số sân")]
        public string CourtNumber { get; set; }

        [Required(ErrorMessage = "Loại sân không được để trống")]
        [Display(Name = "Loại sân")]
        public string CourtType { get; set; } // Indoor, Outdoor, Plastic, Wooden

        [Display(Name = "Mô tả")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Trạng thái không được để trống")]
        [Display(Name = "Trạng thái")]
        public string Status { get; set; } // Available, Maintenance, Unavailable

        [Display(Name = "Hoạt động")]
        public bool IsActive { get; set; }

        // Dropdown options
        public List<FacilityOptionViewModel> AvailableFacilities { get; set; }
        public List<CourtTypeOptionViewModel> CourtTypeOptions { get; set; }
        public List<StatusOptionViewModel> StatusOptions { get; set; }
    }

    /// <summary>
    /// ViewModel cho chi tiết sân
    /// </summary>
    public class CourtDetailsViewModel
    {
        public int CourtId { get; set; }
        public string CourtNumber { get; set; }
        public string CourtType { get; set; }
        public string FacilityName { get; set; }
        public string FacilityAddress { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public bool IsActive { get; set; }

        // Thống kê
        public int TotalBookings { get; set; }
        public int MonthBookings { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal MonthRevenue { get; set; }
        public decimal UtilizationRate { get; set; }

        // Khung giờ
        public List<CourtPriceSlotViewModel> PriceSlots { get; set; }

        // Lịch đặt gần đây
        public List<CourtRecentBookingViewModel> RecentBookings { get; set; }
    }

    /// <summary>
    /// ViewModel cho khung giờ giá của sân
    /// </summary>
    public class CourtPriceSlotViewModel
    {
        public int PriceSlotId { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public decimal Price { get; set; }
        public bool IsPeakHour { get; set; }
        public string DayOfWeek { get; set; }
        public string DisplayTime => $"{StartTime:hh\\:mm} - {EndTime:hh\\:mm}";
        public string PriceDisplay => Price.ToString("N0") + "đ";
    }

    /// <summary>
    /// ViewModel cho booking gần đây của sân
    /// </summary>
    public class CourtRecentBookingViewModel
    {
        public int BookingId { get; set; }
        public string BookingCode { get; set; }
        public string CustomerName { get; set; }
        public DateTime BookingDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public decimal Price { get; set; }
        public string Status { get; set; }
    }

    /// <summary>
    /// ViewModel cho option loại sân
    /// </summary>
    public class CourtTypeOptionViewModel
    {
        public string Value { get; set; }
        public string Text { get; set; }
    }

    /// <summary>
    /// ViewModel cho option trạng thái
    /// </summary>
    public class StatusOptionViewModel
    {
        public string Value { get; set; }
        public string Text { get; set; }
    }

    /// <summary>
    /// ViewModel cho option cơ sở
    /// </summary>
/*    public class FacilityOptionViewModel
    {
        public int FacilityId { get; set; }
        public string FacilityName { get; set; }
    }*/
}
