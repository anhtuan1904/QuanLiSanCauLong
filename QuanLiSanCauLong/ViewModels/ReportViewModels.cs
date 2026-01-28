using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace QuanLiSanCauLong.ViewModels
{
    /// <summary>
    /// ViewModel cho bộ lọc báo cáo
    /// </summary>
    public class ReportFilterViewModel
    {
        [Display(Name = "Từ ngày")]
        public DateTime FromDate { get; set; } = DateTime.Now.AddDays(-30);

        [Display(Name = "Đến ngày")]
        public DateTime ToDate { get; set; } = DateTime.Now;

        [Display(Name = "Cơ sở")]
        public int? FacilityId { get; set; }

        [Display(Name = "Loại báo cáo")]
        public string ReportType { get; set; }

        [Display(Name = "Nhóm theo")]
        public string GroupBy { get; set; } // Day, Week, Month, Year

        public List<FacilityFilterOption> Facilities { get; set; }
    }

    /// <summary>
    /// ViewModel cho báo cáo doanh thu
    /// </summary>
    public class RevenueReportViewModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string FacilityName { get; set; }

        // Tổng quan
        public decimal TotalRevenue { get; set; }
        public decimal BookingRevenue { get; set; }
        public decimal ProductRevenue { get; set; }
        public decimal CashRevenue { get; set; }
        public decimal CardRevenue { get; set; }
        public decimal OnlineRevenue { get; set; }

        // Chi tiết theo thời gian
        public List<RevenueByPeriodViewModel> RevenueByPeriod { get; set; }

        // Chi tiết theo cơ sở
        public List<RevenueByFacilityDetailViewModel> RevenueByFacility { get; set; }

        // Top sản phẩm
        public List<TopRevenueProductViewModel> TopProducts { get; set; }

        // Xu hướng
        public decimal GrowthRate { get; set; }
        public string GrowthTrend { get; set; }
    }

    /// <summary>
    /// ViewModel cho doanh thu theo kỳ
    /// </summary>
    public class RevenueByPeriodViewModel
    {
        public DateTime Date { get; set; }
        public string Period { get; set; }
        public decimal BookingRevenue { get; set; }
        public decimal ProductRevenue { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TransactionCount { get; set; }
    }

    /// <summary>
    /// ViewModel cho doanh thu theo cơ sở chi tiết
    /// </summary>
    public class RevenueByFacilityDetailViewModel
    {
        public string FacilityName { get; set; }
        public decimal BookingRevenue { get; set; }
        public decimal ProductRevenue { get; set; }
        public decimal TotalRevenue { get; set; }
        public int BookingCount { get; set; }
        public int OrderCount { get; set; }
        public decimal AverageOrderValue { get; set; }
    }

    /// <summary>
    /// ViewModel cho sản phẩm doanh thu cao
    /// </summary>
    public class TopRevenueProductViewModel
    {
        public string ProductName { get; set; }
        public string CategoryName { get; set; }
        public int QuantitySold { get; set; }
        public decimal Revenue { get; set; }
        public decimal Percentage { get; set; }
    }

    /// <summary>
    /// ViewModel cho báo cáo đặt sân
    /// </summary>
    public class BookingReportViewModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        // Tổng quan
        public int TotalBookings { get; set; }
        public int CompletedBookings { get; set; }
        public int CancelledBookings { get; set; }
        public int NoShowBookings { get; set; }
        public decimal CompletionRate { get; set; }
        public decimal CancellationRate { get; set; }

        // Chi tiết theo thời gian
        public List<BookingByPeriodViewModel> BookingsByPeriod { get; set; }

        // Theo khung giờ
        public List<BookingByTimeSlotViewModel> BookingsByTimeSlot { get; set; }

        // Theo loại sân
        public List<BookingByCourtTypeViewModel> BookingsByCourtType { get; set; }

        // Theo cơ sở
        public List<BookingByFacilityViewModel> BookingsByFacility { get; set; }
    }

    /// <summary>
    /// ViewModel cho booking theo kỳ
    /// </summary>
    public class BookingByPeriodViewModel
    {
        public DateTime Date { get; set; }
        public string Period { get; set; }
        public int TotalBookings { get; set; }
        public int CompletedBookings { get; set; }
        public int CancelledBookings { get; set; }
        public decimal Revenue { get; set; }
    }

    /// <summary>
    /// ViewModel cho booking theo khung giờ
    /// </summary>
    public class BookingByTimeSlotViewModel
    {
        public string TimeSlot { get; set; }
        public int BookingCount { get; set; }
        public decimal Revenue { get; set; }
        public decimal Percentage { get; set; }
    }

    /// <summary>
    /// ViewModel cho booking theo loại sân
    /// </summary>
    public class BookingByCourtTypeViewModel
    {
        public string CourtType { get; set; }
        public int BookingCount { get; set; }
        public decimal Revenue { get; set; }
        public decimal AveragePrice { get; set; }
    }

    /// <summary>
    /// ViewModel cho booking theo cơ sở
    /// </summary>
    public class BookingByFacilityViewModel
    {
        public string FacilityName { get; set; }
        public int BookingCount { get; set; }
        public decimal Revenue { get; set; }
        public decimal UtilizationRate { get; set; }
    }

    /// <summary>
    /// ViewModel cho báo cáo khách hàng
    /// </summary>
    public class CustomerReportViewModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        // Tổng quan
        public int TotalCustomers { get; set; }
        public int NewCustomers { get; set; }
        public int ActiveCustomers { get; set; }
        public int ReturningCustomers { get; set; }
        public decimal RetentionRate { get; set; }

        // Top khách hàng
        public List<TopCustomerDetailViewModel> TopCustomers { get; set; }

        // Phân khúc khách hàng
        public List<CustomerSegmentViewModel> CustomerSegments { get; set; }

        // Xu hướng khách hàng
        public List<CustomerGrowthViewModel> CustomerGrowth { get; set; }
    }

    /// <summary>
    /// ViewModel cho top khách hàng chi tiết
    /// </summary>
    public class TopCustomerDetailViewModel
    {
        public string CustomerName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public int BookingCount { get; set; }
        public decimal TotalSpent { get; set; }
        public decimal AverageOrderValue { get; set; }
        public DateTime LastBookingDate { get; set; }
        public string CustomerType { get; set; }
    }

    /// <summary>
    /// ViewModel cho phân khúc khách hàng
    /// </summary>
    public class CustomerSegmentViewModel
    {
        public string SegmentName { get; set; }
        public int CustomerCount { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal Percentage { get; set; }
    }

    /// <summary>
    /// ViewModel cho tăng trưởng khách hàng
    /// </summary>
    public class CustomerGrowthViewModel
    {
        public DateTime Date { get; set; }
        public string Period { get; set; }
        public int NewCustomers { get; set; }
        public int TotalCustomers { get; set; }
        public int ActiveCustomers { get; set; }
    }

    /// <summary>
    /// ViewModel cho báo cáo so sánh
    /// </summary>
    public class ComparisonReportViewModel
    {
        public DateTime Period1Start { get; set; }
        public DateTime Period1End { get; set; }
        public DateTime Period2Start { get; set; }
        public DateTime Period2End { get; set; }

        public ComparisonMetricsViewModel Period1Metrics { get; set; }
        public ComparisonMetricsViewModel Period2Metrics { get; set; }
        public ComparisonDifferenceViewModel Differences { get; set; }
    }

    /// <summary>
    /// ViewModel cho chỉ số so sánh
    /// </summary>
    public class ComparisonMetricsViewModel
    {
        public decimal Revenue { get; set; }
        public int Bookings { get; set; }
        public int Customers { get; set; }
        public decimal AverageOrderValue { get; set; }
    }

    /// <summary>
    /// ViewModel cho chênh lệch so sánh
    /// </summary>
    public class ComparisonDifferenceViewModel
    {
        public decimal RevenueChange { get; set; }
        public decimal RevenueChangePercent { get; set; }
        public int BookingChange { get; set; }
        public decimal BookingChangePercent { get; set; }
        public int CustomerChange { get; set; }
        public decimal CustomerChangePercent { get; set; }
    }

    /// <summary>
    /// ViewModel cho option cơ sở trong filter
    /// </summary>
    public class FacilityFilterOption
    {
        public int FacilityId { get; set; }
        public string FacilityName { get; set; }
    }
}
