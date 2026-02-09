using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QuanLiSanCauLong.ViewModels
{
    // ViewModel cho tìm kiếm sân
    public class CourtSearchViewModel
    {
        public int? FacilityId { get; set; }
        public DateTime BookingDate { get; set; } = DateTime.Today;
        public TimeSpan? StartTime { get; set; }
        public string CourtType { get; set; }
        public string City { get; set; }
        public string District { get; set; }

        public List<FacilityAvailabilityViewModel> AvailableFacilities { get; set; }
    }

    // ViewModel cho cơ sở có sân trống
    public class FacilityAvailabilityViewModel
    {
        public int FacilityId { get; set; }
        public string FacilityName { get; set; }
        public string Address { get; set; }
        public string District { get; set; }
        public string City { get; set; }
        public string Phone { get; set; }
        public string ImageUrl { get; set; }
        public TimeSpan? OpenTime { get; set; }
        public TimeSpan? CloseTime { get; set; }
        public int TotalCourts { get; set; }
        public List<CourtAvailabilityViewModel> AvailableCourts { get; set; }
    }

    // ViewModel cho sân có khung giờ trống
    public class CourtAvailabilityViewModel
    {
        public int CourtId { get; set; }
        public string CourtNumber { get; set; }
        public string CourtType { get; set; }
        public string Status { get; set; }
        public List<TimeSlotViewModel> AvailableTimeSlots { get; set; }
    }


    // ViewModel cho tạo đặt sân mới
    public class CreateBookingViewModel
    {
        [Required]
        public int CourtId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Ngày đặt sân")]
        public DateTime BookingDate { get; set; }

        [Required]
        [Display(Name = "Giờ bắt đầu")]
        public TimeSpan StartTime { get; set; }

        [Required]
        [Display(Name = "Giờ kết thúc")]
        public TimeSpan EndTime { get; set; }

        [Display(Name = "Ghi chú")]
        public string Note { get; set; }

        [Display(Name = "Phương thức thanh toán")]
        public string PaymentMethod { get; set; }

        [Display(Name = "Mã voucher")]
        public string VoucherCode { get; set; }

        // Thông tin hiển thị
        public string FacilityName { get; set; }
        public string CourtNumber { get; set; }
        public string CourtType { get; set; }
        public int Duration { get; set; }
        public decimal CourtPrice { get; set; }
        public decimal ServiceFee { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalPrice { get; set; }

        // Sản phẩm đi kèm
        public List<OrderItemViewModel> OrderItems { get; set; }
        public decimal OrderTotal => OrderItems?.Sum(i => i.TotalPrice) ?? 0;
        public decimal GrandTotal => TotalPrice + OrderTotal;
    }


    // ViewModel cho lịch sử đặt sân
    public class BookingHistoryViewModel
    {
        public int BookingId { get; set; }
        public string BookingCode { get; set; }

        // Customer info
        public string CustomerName { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }

        // Court info
        public string FacilityName { get; set; }
        public string CourtNumber { get; set; }
        public string CourtType { get; set; }

        // Booking details
        public DateTime BookingDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int Duration { get; set; }
        public decimal TotalPrice { get; set; }
        public string Status { get; set; }
        public string PaymentStatus { get; set; }
        public string PaymentMethod { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool CanCancel { get; set; }
        public string Note { get; set; }

        // Related orders
        public List<OrderViewModel> RelatedOrders { get; set; }
        public decimal GrandTotal => TotalPrice + (RelatedOrders?.Sum(o => o.TotalAmount) ?? 0);
    }


    // ViewModel cho booking item (list)
    public class BookingItemViewModel
    {
        public int BookingId { get; set; }
        public string BookingCode { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string CourtNumber { get; set; }
        public string CourtType { get; set; }
        public DateTime BookingDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int Duration { get; set; }
        public string Status { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime? CheckInTime { get; set; }
        public bool HasOrders { get; set; }
        public int OrderCount { get; set; }
    }
}
