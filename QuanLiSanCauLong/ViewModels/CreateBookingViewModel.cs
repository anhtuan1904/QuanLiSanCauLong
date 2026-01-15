using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace QuanLiSanCauLong.ViewModels
{
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
}
