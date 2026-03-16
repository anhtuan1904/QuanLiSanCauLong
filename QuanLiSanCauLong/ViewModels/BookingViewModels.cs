using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QuanLiSanCauLong.ViewModels
{
    public class CreateBookingViewModel
    {
        public int CourtId { get; set; }
        public int FacilityId { get; set; }
        public string CourtNumber { get; set; } = "";
        public string CourtType { get; set; } = "";
        public string? CourtTypeLabel { get; set; }
        public string? CourtDescription { get; set; }
        public string? CourtImageUrl { get; set; }
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

        public string PaymentMethod { get; set; } = "Transfer";

        public List<OrderItemViewModel> OrderItems { get; set; } = new();
    }

    public class BookingHistoryViewModel
    {
        public int BookingId { get; set; }
        public string BookingCode { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string FacilityName { get; set; } = "";
        public string? FacilityAddress { get; set; }
        public string CourtNumber { get; set; } = "";
        public string CourtType { get; set; } = "";
        public string? CourtTypeLabel { get; set; }
        public string? CourtImageUrl { get; set; }

        public DateTime BookingDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int Duration { get; set; }

        public decimal CourtPrice { get; set; }
        public decimal ServiceFee { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal GrandTotal => TotalPrice + (RelatedOrders?.Sum(o => o.TotalAmount) ?? 0);

        public string Status { get; set; } = "";
        public string? PaymentStatus { get; set; }
        public string PaymentMethod { get; set; } = "Transfer";
        public string? Note { get; set; }
        public string? CancelReason { get; set; }
        public string? VoucherCode { get; set; }
        public bool CanCancel { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CancelledAt { get; set; }

        public List<OrderViewModel> RelatedOrders { get; set; } = new();
    }

    public class BookingProductItemViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public string? Unit { get; set; }
        public string? Description { get; set; }
    }

    public class SearchCourtRequest
    {
        public int FacilityId { get; set; }
        public DateTime BookingDate { get; set; }
        public string StartTime { get; set; } = "";
    }
}
