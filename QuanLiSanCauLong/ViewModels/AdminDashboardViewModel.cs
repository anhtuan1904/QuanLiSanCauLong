using Microsoft.AspNetCore.Mvc;

namespace QuanLiSanCauLong.ViewModels
{
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
}
