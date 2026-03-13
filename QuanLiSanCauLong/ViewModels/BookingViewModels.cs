using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QuanLiSanCauLong.ViewModels
{
    // ═══════════════════════════════════════════════════════════════════════════
    // CREATE BOOKING (Booking/Create & Booking/Cancel views)
    // ═══════════════════════════════════════════════════════════════════════════

    public class CreateBookingViewModel
    {
        // Thông tin sân
        public int CourtId { get; set; }
        public int FacilityId { get; set; }
        public string CourtNumber { get; set; } = string.Empty;
        public string CourtType { get; set; } = string.Empty;
        public string? CourtDescription { get; set; }
        public string? CourtImageUrl { get; set; }   // ← map từ Court.ImagePath

        // Thông tin cơ sở
        public string FacilityName { get; set; } = string.Empty;
        public string? FacilityAddress { get; set; }

        // Thời gian
        [DataType(DataType.Date)]
        public DateTime BookingDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int Duration { get; set; }

        // Giá
        public decimal CourtPrice { get; set; }
        public decimal ServiceFee { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalPrice { get; set; }

        // Form
        [StringLength(500)]
        public string? Note { get; set; }

        [StringLength(50)]
        public string? VoucherCode { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán")]
        public string PaymentMethod { get; set; } = "Cash";

        // OrderItemViewModel đã có trong OrderViewModels.cs — dùng trực tiếp
        public List<OrderItemViewModel> OrderItems { get; set; } = new();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // BOOKING HISTORY (MyBookings, Details, Invoice views)
    // ═══════════════════════════════════════════════════════════════════════════

    public class BookingHistoryViewModel
    {
        public int BookingId { get; set; }
        public string BookingCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }

        public string FacilityName { get; set; } = string.Empty;
        public string? FacilityAddress { get; set; }
        public string CourtNumber { get; set; } = string.Empty;
        public string CourtType { get; set; } = string.Empty;
        public string? CourtImageUrl { get; set; }

        public DateTime BookingDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int Duration { get; set; }

        public decimal TotalPrice { get; set; }

        // Views dùng Model.GrandTotal / b.GrandTotal
        public decimal GrandTotal => TotalPrice + (RelatedOrders?.Sum(o => o.TotalAmount) ?? 0);

        public string Status { get; set; } = string.Empty;
        public string? PaymentStatus { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string? Note { get; set; }
        public bool CanCancel { get; set; }
        public DateTime CreatedAt { get; set; }

        // OrderViewModel đã có trong OrderViewModels.cs — dùng trực tiếp
        public List<OrderViewModel> RelatedOrders { get; set; } = new();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // PRODUCT ITEM — ViewBag.FoodItems / BeverageItems / EquipmentItems
    // View truy cập: item.ProductId, item.ProductName, item.Price,
    //                item.ImageUrl, item.Unit, item.Description (dynamic binding)
    // ═══════════════════════════════════════════════════════════════════════════

    public class BookingProductItemViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public string? Unit { get; set; }
        public string? Description { get; set; }
    }

    // ── Request model dùng chung ──
    public class SearchCourtRequest
    {
        public int FacilityId { get; set; }
        public DateTime BookingDate { get; set; }
        public string StartTime { get; set; } = "";
    }
}
