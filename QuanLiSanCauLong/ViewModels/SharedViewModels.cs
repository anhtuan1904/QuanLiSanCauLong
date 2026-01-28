using Microsoft.AspNetCore.Mvc;

namespace QuanLiSanCauLong.ViewModels
{
    /// <summary>
    /// ViewModel cho thông tin cơ sở thể thao và sân trống
    /// </summary>
    public class FacilityAvailabilityViewModel
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
        public List<CourtAvailabilityViewModel> AvailableCourts { get; set; }
    }

    /// <summary>
    /// ViewModel cho thông tin sân và các khung giờ trống
    /// </summary>
    public class CourtAvailabilityViewModel
    {
        public int CourtId { get; set; }
        public string CourtNumber { get; set; }
        public string CourtType { get; set; }
        public string Status { get; set; }
        public List<TimeSlotViewModel> AvailableTimeSlots { get; set; }
    }

    /// <summary>
    /// ViewModel cho khung giờ và giá
    /// </summary>
    public class TimeSlotViewModel
    {
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public decimal Price { get; set; }
        public bool IsPeakHour { get; set; }
        public bool IsAvailable { get; set; }
        public string DisplayTime => $"{StartTime:hh\\:mm} - {EndTime:hh\\:mm}";
        public string PriceDisplay => Price.ToString("N0") + "đ";
    }

    /// <summary>
    /// ViewModel cho lịch sân theo khung giờ (dùng cho Staff)
    /// </summary>
    public class TimeSlotBookingViewModel
    {
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public bool IsBooked { get; set; }
        public string CustomerName { get; set; }
        public int? BookingId { get; set; }
    }

    /// <summary>
    /// ViewModel cho danh mục sản phẩm
    /// </summary>
    public class ProductCategoryViewModel
    {
        public string CategoryName { get; set; }
        public string CategoryType { get; set; }
        public List<ProductItemViewModel> Products { get; set; }
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
