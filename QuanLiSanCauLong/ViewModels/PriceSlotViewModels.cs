using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace QuanLiSanCauLong.ViewModels
{
    /// <summary>
    /// ViewModel cho danh sách khung giờ giá
    /// </summary>
    public class PriceSlotListViewModel
    {
        public int? FacilityId { get; set; }
        public int? CourtId { get; set; }
        public string FacilityName { get; set; }
        public string CourtNumber { get; set; }
        public List<PriceSlotItemViewModel> PriceSlots { get; set; }
        public int TotalCount { get; set; }
    }

    /// <summary>
    /// ViewModel cho từng khung giờ giá
    /// </summary>
    public class PriceSlotItemViewModel
    {
        public int PriceSlotId { get; set; }
        public string FacilityName { get; set; }
        public string CourtNumber { get; set; }
        public string CourtType { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public decimal Price { get; set; }
        public bool IsPeakHour { get; set; }
        public string DayOfWeek { get; set; }
        public bool IsActive { get; set; }
        public string DisplayTime => $"{StartTime:hh\\:mm} - {EndTime:hh\\:mm}";
        public string PriceDisplay => Price.ToString("N0") + "đ";
        public string PeakHourLabel => IsPeakHour ? "Giờ cao điểm" : "Giờ thường";
    }

    /// <summary>
    /// ViewModel cho quản lý khung giờ giá
    /// </summary>
    public class PriceSlotManageViewModel
    {
        public int PriceSlotId { get; set; }

        [Required(ErrorMessage = "Cơ sở không được để trống")]
        [Display(Name = "Cơ sở")]
        public int FacilityId { get; set; }

        [Required(ErrorMessage = "Sân không được để trống")]
        [Display(Name = "Sân")]
        public int CourtId { get; set; }

        [Required(ErrorMessage = "Giờ bắt đầu không được để trống")]
        [Display(Name = "Giờ bắt đầu")]
        public TimeSpan StartTime { get; set; }

        [Required(ErrorMessage = "Giờ kết thúc không được để trống")]
        [Display(Name = "Giờ kết thúc")]
        public TimeSpan EndTime { get; set; }

        [Required(ErrorMessage = "Giá không được để trống")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá phải lớn hơn 0")]
        [Display(Name = "Giá (VNĐ)")]
        public decimal Price { get; set; }

        [Display(Name = "Giờ cao điểm")]
        public bool IsPeakHour { get; set; }

        [Display(Name = "Ngày trong tuần")]
        public string DayOfWeek { get; set; } // All, Monday, Tuesday, ...

        [Display(Name = "Hoạt động")]
        public bool IsActive { get; set; }

        // Dropdown options
        public List<FacilityOptionViewModel> AvailableFacilities { get; set; }
        public List<CourtOptionViewModel> AvailableCourts { get; set; }
        public List<DayOfWeekOptionViewModel> DayOfWeekOptions { get; set; }
    }

    /// <summary>
    /// ViewModel cho tạo nhiều khung giờ cùng lúc
    /// </summary>
    public class BulkCreatePriceSlotViewModel
    {
        [Required(ErrorMessage = "Cơ sở không được để trống")]
        [Display(Name = "Cơ sở")]
        public int FacilityId { get; set; }

        [Display(Name = "Áp dụng cho tất cả sân")]
        public bool ApplyToAllCourts { get; set; }

        [Display(Name = "Hoặc chọn sân cụ thể")]
        public List<int> SelectedCourtIds { get; set; }

        [Required(ErrorMessage = "Giờ bắt đầu không được để trống")]
        [Display(Name = "Giờ mở cửa")]
        public TimeSpan OpenTime { get; set; }

        [Required(ErrorMessage = "Giờ kết thúc không được để trống")]
        [Display(Name = "Giờ đóng cửa")]
        public TimeSpan CloseTime { get; set; }

        [Required(ErrorMessage = "Thời lượng slot không được để trống")]
        [Display(Name = "Thời lượng mỗi slot (phút)")]
        public int SlotDuration { get; set; } // 60, 90, 120 minutes

        [Display(Name = "Ngày áp dụng")]
        public List<string> ApplicableDays { get; set; }

        // Giá theo thời gian
        public List<TimeBasedPriceViewModel> TimeBasedPrices { get; set; }

        // Dropdown options
        public List<FacilityOptionViewModel> AvailableFacilities { get; set; }
        public List<CourtOptionViewModel> AvailableCourts { get; set; }
    }

    /// <summary>
    /// ViewModel cho giá theo khung giờ
    /// </summary>
    public class TimeBasedPriceViewModel
    {
        [Required]
        [Display(Name = "Từ giờ")]
        public TimeSpan FromTime { get; set; }

        [Required]
        [Display(Name = "Đến giờ")]
        public TimeSpan ToTime { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Giá phải lớn hơn 0")]
        [Display(Name = "Giá")]
        public decimal Price { get; set; }

        [Display(Name = "Giờ cao điểm")]
        public bool IsPeakHour { get; set; }
    }

    /// <summary>
    /// ViewModel cho lịch giá theo tuần
    /// </summary>
    public class WeeklyPriceScheduleViewModel
    {
        public int FacilityId { get; set; }
        public string FacilityName { get; set; }
        public int? CourtId { get; set; }
        public string CourtNumber { get; set; }

        public Dictionary<string, List<DailyPriceSlotViewModel>> WeeklySchedule { get; set; }
    }

    /// <summary>
    /// ViewModel cho slot giá trong ngày
    /// </summary>
    public class DailyPriceSlotViewModel
    {
        public int PriceSlotId { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public decimal Price { get; set; }
        public bool IsPeakHour { get; set; }
        public string DisplayTime => $"{StartTime:hh\\:mm}-{EndTime:hh\\:mm}";
        public string PriceDisplay => Price.ToString("N0") + "đ";
    }

    /// <summary>
    /// ViewModel cho option cơ sở
    /// </summary>
/*    public class FacilityOptionViewModel
    {
        public int FacilityId { get; set; }
        public string FacilityName { get; set; }
    }*/

    /// <summary>
    /// ViewModel cho option sân
    /// </summary>
    public class CourtOptionViewModel
    {
        public int CourtId { get; set; }
        public string CourtNumber { get; set; }
        public string CourtType { get; set; }
    }

    /// <summary>
    /// ViewModel cho option ngày trong tuần
    /// </summary>
    public class DayOfWeekOptionViewModel
    {
        public string Value { get; set; }
        public string Text { get; set; }
    }
}
