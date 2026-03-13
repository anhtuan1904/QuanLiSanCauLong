using Microsoft.AspNetCore.Mvc;

namespace QuanLiSanCauLong.ViewModels
{
    /// <summary>
    /// ViewModel cho dashboard quản trị - thống kê tổng quan
    /// </summary>
    public class AdminDashboardViewModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        // Thống kê tổng quan
        public decimal TotalRevenue { get; set; }
        public decimal BookingRevenue { get; set; }
        public decimal ProductRevenue { get; set; }
        public int TotalBookings { get; set; }
        public int CompletedBookings { get; set; }
        public int CancelledBookings { get; set; }
        public int TotalCustomers { get; set; }
        public int NewCustomers { get; set; }
        public int TotalOrders { get; set; }

        // Biểu đồ & phân tích
        public List<RevenueByDateViewModel> RevenueByDate { get; set; } = new();
        public List<RevenueByFacilityViewModel> RevenueByFacility { get; set; } = new();
        public List<PopularTimeSlotViewModel> PopularTimeSlots { get; set; } = new();
        public List<TopProductViewModel> TopProducts { get; set; } = new();
        public List<TopCustomerViewModel> TopCustomers { get; set; } = new();

        // ── BỔ SUNG: Analytics.cshtml cần FacilityStats ──
        public List<FacilityStatsViewModel> FacilityStats { get; set; } = new();

        // ── BỔ SUNG: Index.cshtml (dashboard) cần RecentBookings ──
        public List<RecentBookingViewModel> RecentBookings { get; set; } = new();
    }

    /// <summary>
    /// ViewModel cho doanh thu theo ngày
    /// </summary>
    public class RevenueByDateViewModel
    {
        public DateTime Date { get; set; }
        public decimal BookingRevenue { get; set; }
        public decimal ProductRevenue { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    /// <summary>
    /// ViewModel cho doanh thu theo cơ sở
    /// </summary>
    public class RevenueByFacilityViewModel
    {
        public string FacilityName { get; set; }
        public int BookingCount { get; set; }
        public decimal BookingRevenue { get; set; }
        public decimal ProductRevenue { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    /// <summary>
    /// ViewModel cho khung giờ phổ biến
    /// </summary>
    public class PopularTimeSlotViewModel
    {
        public string TimeSlot { get; set; }
        public int BookingCount { get; set; }
        public decimal Revenue { get; set; }
    }

    /// <summary>
    /// ViewModel cho sản phẩm bán chạy
    /// </summary>
    public class TopProductViewModel
    {
        public string ProductName { get; set; }
        public string CategoryType { get; set; }
        public int QuantitySold { get; set; }
        public decimal Revenue { get; set; }
    }

    /// <summary>
    /// ViewModel cho khách hàng VIP
    /// </summary>
    public class TopCustomerViewModel
    {
        public string CustomerName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public int BookingCount { get; set; }
        public decimal TotalSpent { get; set; }
    }

    /// <summary>
    /// Thống kê hiệu suất từng cơ sở — dùng trong Analytics.cshtml
    /// </summary>
    public class FacilityStatsViewModel
    {
        public int FacilityId { get; set; }
        public string FacilityName { get; set; }
        public string Address { get; set; }
        public int TotalBookings { get; set; }
        public decimal TotalRevenue { get; set; }

        /// <summary>Tỷ lệ lấp đầy 0–100 (%)</summary>
        public double OccupancyRate { get; set; }

        /// <summary>Đánh giá trung bình (★)</summary>
        public double AverageRating { get; set; }
    }

    /// <summary>
    /// Đặt sân gần đây — dùng trong Index.cshtml (dashboard)
    /// </summary>
    public class RecentBookingViewModel
    {
        public int BookingId { get; set; }
        public string BookingCode { get; set; }
        public string CustomerName { get; set; }

        /// <summary>Tên / số sân, ví dụ "Sân A1"</summary>
        public string CourtNumber { get; set; }

        public TimeSpan StartTime { get; set; }
        public decimal TotalAmount { get; set; }

        /// <summary>Pending | Confirmed | Playing | Completed | Cancelled</summary>
        public string Status { get; set; }
    }

    public class BulkPriceSlotRequest
    {
        public List<int> CourtIds { get; set; }
        public List<PriceSlotDetailRequest> TimeSlots { get; set; }
        public List<string> Days { get; set; }  // ← THÊM DÒNG NÀY
    }

    public class PriceSlotDetailRequest
    {
        public string StartTimeStr { get; set; }
        public string EndTimeStr { get; set; }
        public decimal Price { get; set; }
        public bool IsPeakHour { get; set; }

        // ← THÊM 4 DÒNG NÀY:
        public decimal Surcharge { get; set; }
        public string SlotName { get; set; }
        public string CustType { get; set; }
        public string SurNote { get; set; }

        public TimeSpan StartTime => TimeSpan.Parse(StartTimeStr);
        public TimeSpan EndTime => TimeSpan.Parse(EndTimeStr);
    }
}
