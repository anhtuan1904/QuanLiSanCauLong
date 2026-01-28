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
        public List<RevenueByDateViewModel> RevenueByDate { get; set; }
        public List<RevenueByFacilityViewModel> RevenueByFacility { get; set; }
        public List<PopularTimeSlotViewModel> PopularTimeSlots { get; set; }
        public List<TopProductViewModel> TopProducts { get; set; }
        public List<TopCustomerViewModel> TopCustomers { get; set; }
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

    public class BulkPriceSlotRequest
    {
        public List<int> CourtIds { get; set; }
        public List<PriceSlotDetailRequest> TimeSlots { get; set; }
    }

    public class PriceSlotDetailRequest
    {
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public decimal Price { get; set; }
        public bool IsPeakHour { get; set; }
    }
}
