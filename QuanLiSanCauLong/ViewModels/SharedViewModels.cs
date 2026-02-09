using Microsoft.AspNetCore.Mvc;

namespace QuanLiSanCauLong.ViewModels
{
    /// <summary>
    /// ViewModel cho thông tin cơ sở thể thao và sân trống
    /// </summary>
/*    public class FacilityAvailabilityViewModel
    {
        public int FacilityId { get; set; }
        public string FacilityName { get; set; }
        public string Address { get; set; }
        public string District { get; set; }
        public string City { get; set; }
        public string Phone { get; set; }
        public string ImageUrl { get; set; }
        public TimeSpan? OpenTime { get; set; }
        public TimeSpan? CloseTime { get; set; }
        public int TotalCourts { get; set; }
        public List<CourtAvailabilityViewModel> AvailableCourts { get; set; } = new();
    }
*/
    /// <summary>
    /// ViewModel cho thông tin sân và các khung giờ trống
    /// </summary>
/*    public class CourtAvailabilityViewModel
    {
        public int CourtId { get; set; }
        public string CourtNumber { get; set; }
        public string CourtType { get; set; }
        public string Status { get; set; }
        public List<TimeSlotViewModel> AvailableTimeSlots { get; set; } = new();
    }

*/    /// <summary>
    /// ViewModel cho khung giờ và giá
    /// </summary>
    public class TimeSlotViewModel
    {
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public decimal Price { get; set; }
        public bool IsPeakHour { get; set; }
        public bool IsAvailable { get; set; }

        // Fix lỗi CS1061 ở Details.cshtml
        public bool IsBooked => !IsAvailable;
        public string TimeRange => $"{StartTime:hh\\:mm} - {EndTime:hh\\:mm}";
        public string DisplayTime => TimeRange;
        public string PriceDisplay => Price.ToString("N0") + "đ";
    }

    /// <summary>
    /// ViewModel dành cho nhân viên quản lý lịch đặt
    /// </summary>
    public class TimeSlotBookingViewModel
    {
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string DisplayTime => $"{StartTime:hh\\:mm} - {EndTime:hh\\:mm}";
        public bool IsBooked { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public int? BookingId { get; set; }
        public string BookingStatus { get; set; }
    }


    /// <summary>
    /// ViewModel cho thông tin sản phẩm
    /// </summary>
    public class ProductItemViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public string Unit { get; set; }
        public string ImageUrl { get; set; }
        public int StockQuantity { get; set; }
        public bool IsAvailable => StockQuantity > 0;
    }
}