using System;
using System.Collections.Generic;

namespace QuanLiSanCauLong.ViewModels
{
    public class AdminDashboardViewModel
    {
        // ── Bộ lọc ngày ────────────────────────────────────────────
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        // ── Thống kê booking ───────────────────────────────────────
        public int TotalBookings { get; set; }
        public int PendingBookings { get; set; }
        public int CompletedBookings { get; set; }
        public int CancelledBookings { get; set; }

        // ── Doanh thu ──────────────────────────────────────────────
        public decimal TotalRevenue { get; set; }
        public decimal BookingRevenue { get; set; }
        public decimal ProductRevenue { get; set; }
        public double RevenueGrowth { get; set; }
        public double BookingRevenuePercent { get; set; }
        public double ProductRevenuePercent { get; set; }

        // ── Khách hàng ─────────────────────────────────────────────
        public int TotalCustomers { get; set; }
        public int NewCustomers { get; set; }

        // ── Đơn hàng ───────────────────────────────────────────────
        public int TotalOrders { get; set; }

        // ── Cơ sở & Sân ────────────────────────────────────────────
        public int TotalFacilities { get; set; }
        public int TotalCourts { get; set; }
        public int AvailableCourts { get; set; }
        public int BusyCourts { get; set; }

        // 🔥 Phase 1 NEW properties: Status Live của Sân
        public List<LiveCourtStatusViewModel> LiveCourtStatuses { get; set; } = new();
        public int CourtsAvailable { get; set; }
        public int CourtsOccupied { get; set; }
        public int CourtsReserved { get; set; }
        public int CourtsOnMaintenance { get; set; }
        public double OccupancyRate { get; set; }

        // 🔥 Phase 1 NEW properties: Rating & Review hệ thống
        public double OverallAverageRating { get; set; }
        public int OverallReviewCount { get; set; }

        // 🔥 Phase 1 NEW properties: Voucher
        public int VouchersUsed { get; set; }
        public decimal VoucherDiscount { get; set; }

        // 🔥 Phase 1 NEW properties: Activity & Forecast
        public List<ActivityFeedItem> ActivityFeed { get; set; } = new();
        public int[,] HeatmapData { get; set; } = new int[7, 24]; // [Thứ, Giờ]
        public List<ForecastDayViewModel> ForecastDays { get; set; } = new();

        // ── Cảnh báo ───────────────────────────────────────────────
        public int LowStockCount { get; set; }

        // ── Danh sách chi tiết (Biểu đồ & Bảng) ──────────────────────
        public List<RevenueByDateViewModel> RevenueByDate { get; set; } = new();
        public List<RevenueByFacilityViewModel> RevenueByFacility { get; set; } = new();
        public List<PopularTimeSlotViewModel> PopularTimeSlots { get; set; } = new();
        public List<TopProductViewModel> TopProducts { get; set; } = new();
        public List<TopCustomerViewModel> TopCustomers { get; set; } = new();
        public List<RecentBookingViewModel> RecentBookings { get; set; } = new();
        public List<FacilityStatsViewModel> FacilityStats { get; set; } = new();
    }

    // ─────────────────────────────────────────────────────────────────
    // CÁC CLASS HỖ TRỢ (KEEP AS IS)
    // ─────────────────────────────────────────────────────────────────

    public class LiveCourtStatusViewModel
    {
        public int CourtId { get; set; }
        public string CourtNumber { get; set; } = "";
        public string FacilityName { get; set; } = "";
        public string Status { get; set; } = "";
        public string LiveStatus { get; set; } = "Available";
        public string? CurrentPlayer { get; set; }
        public int RemainingMinutes { get; set; }
        public string? NextPlayer { get; set; }
        public string? NextBookingTime { get; set; }
    }

    public class ActivityFeedItem
    {
        public string ActorName { get; set; } = "";
        public string Description { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public string Type { get; set; } = ""; // Booking / Đơn hàng / Review
        public string TypeClass { get; set; } = "p-ok"; // CSS pill class
    }

    public class ForecastDayViewModel
    {
        public DateTime Date { get; set; }
        public int BookingCount { get; set; }
        public decimal Revenue { get; set; }
    }

    public class RevenueByDateViewModel
    {
        public DateTime Date { get; set; }
        public decimal BookingRevenue { get; set; }
        public decimal ProductRevenue { get; set; }
        public decimal TotalRevenue { get; set; }
        public string DateLabel => Date.ToString("dd/MM");
    }

    public class RevenueByFacilityViewModel
    {
        public string FacilityName { get; set; } = "";
        public int BookingCount { get; set; }
        public decimal BookingRevenue { get; set; }
        public decimal ProductRevenue { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class PopularTimeSlotViewModel
    {
        public string TimeSlot { get; set; } = "";
        public int BookingCount { get; set; }
        public decimal Revenue { get; set; }
    }

    public class TopProductViewModel
    {
        public string ProductName { get; set; } = "";
        public string CategoryType { get; set; } = "";
        public int QuantitySold { get; set; }
        public decimal Revenue { get; set; }
    }

    public class TopCustomerViewModel
    {
        public string CustomerName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public int BookingCount { get; set; }
        public decimal TotalSpent { get; set; }
    }

    public class RecentBookingViewModel
    {
        public int BookingId { get; set; }
        public string BookingCode { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public string CourtNumber { get; set; } = "";
        public string FacilityName { get; set; } = "";
        public DateTime BookingDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "";
        public string PaymentStatus { get; set; } = "";
    }

    public class FacilityStatsViewModel
    {
        public int FacilityId { get; set; }
        public string FacilityName { get; set; } = "";
        public string Address { get; set; } = "";
        public int TotalBookings { get; set; }
        public decimal TotalRevenue { get; set; }
        public double OccupancyRate { get; set; }
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
    }
}