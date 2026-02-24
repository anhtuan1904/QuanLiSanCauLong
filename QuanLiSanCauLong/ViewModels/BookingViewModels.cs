using System;
using System.Collections.Generic;
using System.Linq;

namespace QuanLiSanCauLong.ViewModels
{
    // =====================================================================
    // BOOKING HISTORY VIEW MODEL
    // Dùng cho: Booking/MyBookings, Booking/Details, Booking/Cancel
    //           AdminBooking/Index, AdminBooking/Details
    // =====================================================================
    public class BookingHistoryViewModel
    {
        // --- Core ---
        public int BookingId { get; set; }
        public string BookingCode { get; set; }

        // --- Customer (Admin only — Booking controller để null) ---
        public string CustomerName { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }

        // --- Court & Facility ---
        public string FacilityName { get; set; }
        public string CourtNumber { get; set; }   // string vì view render trực tiếp
        public string CourtType { get; set; }

        // --- Timing ---
        public DateTime BookingDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int Duration { get; set; }

        // --- Pricing ---
        public decimal TotalPrice { get; set; }   // Tiền sân + phí dịch vụ - voucher

        // GrandTotal = TotalPrice + tổng sản phẩm đi kèm
        public decimal GrandTotal =>
            TotalPrice + (RelatedOrders?.Sum(o => o.TotalAmount) ?? 0);

        // --- Status ---
        public string Status { get; set; }
        public string PaymentStatus { get; set; }
        public string PaymentMethod { get; set; }

        // --- Meta ---
        public DateTime CreatedAt { get; set; }
        public string Note { get; set; }
        public bool CanCancel { get; set; }

        // --- Related ---
        public List<OrderViewModel> RelatedOrders { get; set; } = new();
    }


    // =====================================================================
    // CREATE BOOKING VIEW MODEL
    // =====================================================================
    public class CreateBookingViewModel
    {
        [System.ComponentModel.DataAnnotations.Required]
        public int CourtId { get; set; }

        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Date)]
        [System.ComponentModel.DataAnnotations.Display(Name = "Ngày đặt sân")]
        public DateTime BookingDate { get; set; }

        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.Display(Name = "Giờ bắt đầu")]
        public TimeSpan StartTime { get; set; }

        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.Display(Name = "Giờ kết thúc")]
        public TimeSpan EndTime { get; set; }

        [System.ComponentModel.DataAnnotations.Display(Name = "Ghi chú")]
        public string Note { get; set; }

        [System.ComponentModel.DataAnnotations.Display(Name = "Phương thức thanh toán")]
        public string PaymentMethod { get; set; }

        [System.ComponentModel.DataAnnotations.Display(Name = "Mã voucher")]
        public string VoucherCode { get; set; }

        // --- Display info ---
        public string FacilityName { get; set; }
        public string FacilityAddress { get; set; }   // ĐÃ THÊM
        public string CourtNumber { get; set; }
        public string CourtType { get; set; }
        public string CourtImageUrl { get; set; }     // ĐÃ THÊM
        public string CourtDescription { get; set; }  // ĐÃ THÊM
        public int Duration { get; set; }

        // --- Pricing ---
        public decimal CourtPrice { get; set; }
        public decimal ServiceFee { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalPrice { get; set; }       // CourtPrice + ServiceFee - Discount

        // --- Products ---
        public List<OrderItemViewModel> OrderItems { get; set; } = new();
        public decimal OrderTotal => OrderItems?.Sum(i => i.TotalPrice) ?? 0;
        public decimal GrandTotal => TotalPrice + OrderTotal;
    }
}
