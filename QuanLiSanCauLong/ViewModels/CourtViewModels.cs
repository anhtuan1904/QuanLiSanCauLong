using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QuanLiSanCauLong.ViewModels
{
    // ===================================================================
    // 1. SỬA LỖI CS0234: CourtSearchViewModel không tồn tại (Ảnh b837e4)
    // ===================================================================
    public class CourtSearchViewModel
    {
        public int? FacilityId { get; set; }
        public string SearchTerm { get; set; }
        public string CourtType { get; set; }
        public string Status { get; set; }
        public List<CourtItemViewModel> Results { get; set; } = new();
    }

    // ===================================================================
    // 2. SỬA LỖI CS1061: Thiếu CustomerPhone (Ảnh b83bbd) & CS0246 (Ảnh b837e4)
    // ===================================================================
    public class BookingItemViewModel
    {
        public int BookingId { get; set; }
        public string BookingCode { get; set; }
        public string CustomerName { get; set; }

        // Thêm trường này để sửa lỗi tại Dashboard.cshtml dòng 200, 252, 300
        public string CustomerPhone { get; set; }

        public DateTime BookingDate { get; set; }
        public string CourtNumber { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }
    }

    // ===================================================================
    // 3. DANH SÁCH & QUẢN LÝ SÂN
    // ===================================================================
    public class CourtListViewModel
    {
        public int? FacilityId { get; set; }
        public string FacilityName { get; set; }
        public List<CourtItemViewModel> Courts { get; set; } = new();
        public string FilterCourtType { get; set; }
        public string FilterStatus { get; set; }
        public int TotalCount { get; set; }
    }

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

    public class CourtManageViewModel
    {
        public int CourtId { get; set; }

        [Required(ErrorMessage = "Cơ sở không được để trống")]
        public int FacilityId { get; set; }

        [Required(ErrorMessage = "Số sân không được để trống")]
        public string CourtNumber { get; set; }

        [Required(ErrorMessage = "Loại sân không được để trống")]
        public string CourtType { get; set; }

        public string Description { get; set; }

        [Required(ErrorMessage = "Trạng thái không được để trống")]
        public string Status { get; set; }

        public bool IsActive { get; set; }

        public List<FacilityOptionViewModel> AvailableFacilities { get; set; } = new();
        public List<CourtTypeOptionViewModel> CourtTypeOptions { get; set; } = new();
        public List<StatusOptionViewModel> StatusOptions { get; set; } = new();
    }

    // ===================================================================
    // 4. SỬA LỖI CS1061: StockTransactionViewModel (Ảnh a9993c)
    // ===================================================================
/*    public class StockTransactionViewModel
    {
        public int FacilityId { get; set; }
        public DateTime TransactionDate { get; set; } = DateTime.Now;

        // Thêm các trường này để sửa lỗi tại StockOut.cshtml dòng 45, 57, 72, 85
        public string OutReason { get; set; } = "Sale";
        public string DocumentReference { get; set; }
        public int? SupplierId { get; set; }
        public string BatchNumber { get; set; }
        public DateTime? ExpiryDate { get; set; }

        public string Note { get; set; }
        public List<StockTransactionItemViewModel> Items { get; set; } = new();
    }
*/
/*    public class StockTransactionItemViewModel
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public string Unit { get; set; }
        public decimal CostPrice { get; set; }
    }
*/
    // ===================================================================
    // 5. CÁC LỚP HỖ TRỢ CHI TIẾT
    // ===================================================================
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

        public int TotalBookings { get; set; }
        public int MonthBookings { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal MonthRevenue { get; set; }
        public decimal UtilizationRate { get; set; }

        public List<CourtPriceSlotViewModel> PriceSlots { get; set; } = new();
        public List<CourtRecentBookingViewModel> RecentBookings { get; set; } = new();
    }

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

    // ===================================================================
    // 6. CÁC LỚP OPTION (Dropdown)
    // ===================================================================
    public class CourtTypeOptionViewModel { public string Value { get; set; } public string Text { get; set; } }
    public class StatusOptionViewModel { public string Value { get; set; } public string Text { get; set; } }

    // Mở lại class này để sửa lỗi dòng 68 trong ManageViewModel
/*    public class FacilityOptionViewModel
    {
        public int FacilityId { get; set; }
        public string FacilityName { get; set; }
    }*/
}