using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QuanLiSanCauLong.ViewModels
{
    // ═══════════════════════════════════════════════════════════════════════════
    // CREATE BOOKING
    // ═══════════════════════════════════════════════════════════════════════════
    public class CreateBookingViewModel
    {
        public int CourtId { get; set; }
        public int FacilityId { get; set; }
        public string CourtNumber { get; set; } = "";
        public string CourtType { get; set; } = "";
        public string? CourtTypeLabel { get; set; }    // Indoor / Outdoor label
        public string? CourtDescription { get; set; }
        public string? CourtImageUrl { get; set; }   // ← Court.ImagePath

        public string FacilityName { get; set; } = "";
        public string? FacilityAddress { get; set; }

        [DataType(DataType.Date)]
        public DateTime BookingDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int Duration { get; set; }

        public decimal CourtPrice { get; set; }
        public decimal ServiceFee { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalPrice { get; set; }

        [StringLength(500)]
        public string? Note { get; set; }

        [StringLength(50)]
        public string? VoucherCode { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán")]
        public string PaymentMethod { get; set; } = "Cash";

        public List<OrderItemViewModel> OrderItems { get; set; } = new();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // BOOKING HISTORY — MyBookings, Details, Invoice, Cancel, Reschedule
    // ═══════════════════════════════════════════════════════════════════════════
    public class BookingHistoryViewModel
    {
        public int BookingId { get; set; }
        public string BookingCode { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public string? Phone { get; set; }
        public string? Email { get; set; }

        // Facility & Court
        public string FacilityName { get; set; } = "";
        public string? FacilityAddress { get; set; }
        public string CourtNumber { get; set; } = "";
        public string CourtType { get; set; } = "";
        public string? CourtTypeLabel { get; set; }   // "Trong nhà" / "Ngoài trời"
        public string? CourtImageUrl { get; set; }   // Court.ImagePath

        // Thời gian
        public DateTime BookingDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int Duration { get; set; }

        // Giá — chi tiết breakdown (dùng trong Details + Invoice)
        public decimal CourtPrice { get; set; }
        public decimal ServiceFee { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalPrice { get; set; }   // = CourtPrice + ServiceFee − Discount

        // Tổng kể cả đơn hàng đi kèm
        public decimal GrandTotal => TotalPrice + (RelatedOrders?.Sum(o => o.TotalAmount) ?? 0);

        // Trạng thái
        public string Status { get; set; } = "";
        public string? PaymentStatus { get; set; }
        public string PaymentMethod { get; set; } = "";

        // Thông tin thêm
        public string? Note { get; set; }
        public string? CancelReason { get; set; }
        public string? VoucherCode { get; set; }   // từ VoucherUsages.Voucher.VoucherCode
        public bool CanCancel { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CancelledAt { get; set; }   // ← dùng trong Details timeline

        // Đơn hàng đi kèm
        public List<OrderViewModel> RelatedOrders { get; set; } = new();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // PRODUCT ITEM — ViewBag.FoodItems / BeverageItems / EquipmentItems
    // ═══════════════════════════════════════════════════════════════════════════
    public class BookingProductItemViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public string? Unit { get; set; }
        public string? Description { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // TIME SLOT — Search results + GetTimeSlots API
    // ═══════════════════════════════════════════════════════════════════════════
/*    public class TimeSlotViewModel
    {
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public decimal Price { get; set; }
        public bool IsPeakHour { get; set; }
        public bool IsAvailable { get; set; }
        public bool IsBooked => !IsAvailable;
    }*/

    // ═══════════════════════════════════════════════════════════════════════════
    // SEARCH REQUEST — POST /Booking/SearchAvailableCourts
    // ═══════════════════════════════════════════════════════════════════════════
    public class SearchCourtRequest
    {
        public int FacilityId { get; set; }
        public DateTime BookingDate { get; set; }
        public string StartTime { get; set; } = "";
    }
}
