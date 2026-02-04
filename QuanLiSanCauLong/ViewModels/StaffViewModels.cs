using Microsoft.AspNetCore.Mvc;
using static QuanLiSanCauLong.ViewModels.TimeSlotViewModel;

namespace QuanLiSanCauLong.ViewModels
{
    /// <summary>
    /// ViewModel cho dashboard nhân viên - quản lý lịch sân trong ngày
    /// </summary>
    public class StaffDashboardViewModel
    {
        public DateTime SelectedDate { get; set; }
        public int FacilityId { get; set; }
        public string FacilityName { get; set; }

        public List<CourtScheduleViewModel> CourtSchedules { get; set; }
        public List<BookingItemViewModel> PendingBookings { get; set; }
        public List<BookingItemViewModel> UpcomingBookings { get; set; }
        public List<BookingItemViewModel> PlayingBookings { get; set; }

        public StatisticSummaryViewModel Statistics { get; set; }
    }

    /// <summary>
    /// ViewModel cho lịch từng sân
    /// </summary>
    public class CourtScheduleViewModel
    {
        public int CourtId { get; set; }
        public string CourtNumber { get; set; }
        public string CourtType { get; set; }
        public string CurrentStatus { get; set; } // Empty, Playing, Upcoming, Maintenance
        public BookingItemViewModel CurrentBooking { get; set; }
        public BookingItemViewModel NextBooking { get; set; }
        public List<TimeSlotBookingViewModel> TimeSlots { get; set; }
    }

    /// <summary>
    /// ViewModel cho check-in khách hàng
    /// </summary>
    public class CheckInViewModel
    {
        public int BookingId { get; set; }
        public string BookingCode { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string CourtNumber { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public DateTime CheckInTime { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// ViewModel cho bán hàng (thức uống, đồ dùng)
    /// </summary>
    public class SalesViewModel
    {
        public int? BookingId { get; set; }
        public int FacilityId { get; set; }
        public int UserId { get; set; }
        public List<ProductCategoryViewModel> ProductCategories { get; set; }
        public List<OrderItemViewModel> CartItems { get; set; }
        public decimal SubTotal => CartItems?.Sum(i => i.TotalPrice) ?? 0;
        public string VoucherCode { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount => SubTotal - DiscountAmount;
    }

    /// <summary>
    /// ViewModel cho thống kê tổng quan nhanh
    /// </summary>
    public class StatisticSummaryViewModel
    {
        public int TodayBookings { get; set; }
        public int PendingBookings { get; set; }
        public int PlayingBookings { get; set; }
        public int CompletedBookings { get; set; }
        public decimal TodayRevenue { get; set; }
    }
}
