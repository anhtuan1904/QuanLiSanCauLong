using System;
using System.ComponentModel.DataAnnotations;

namespace QuanLiSanCauLong.ViewModels
{
    /// <summary>
    /// ViewModel hiển thị chi tiết lịch sử sử dụng của một Voucher cụ thể
    /// </summary>
    public class VoucherUsageViewModel
    {
        public int UsageId { get; set; }

        // Thông tin khách hàng
        [Display(Name = "Họ tên khách hàng")]
        public string CustomerName { get; set; }

        [Display(Name = "Số điện thoại")]
        public string CustomerPhone { get; set; }

        [Display(Name = "Email")]
        public string CustomerEmail { get; set; }

        // Thông tin đơn hàng liên quan
        [Display(Name = "Mã đơn hàng/đặt sân")]
        public string OrderCode { get; set; }

        [Display(Name = "Loại dịch vụ")]
        public string OrderType { get; set; } // Ví dụ: "Đặt sân", "Mua sản phẩm", "Combo"

        [Display(Name = "Ngày sử dụng")]
        public DateTime UsedAt { get; set; }

        // Thông tin tài chính
        [Display(Name = "Giá trị đơn hàng")]
        public decimal OrderTotal { get; set; }

        [Display(Name = "Số tiền được giảm")]
        public decimal DiscountAmount { get; set; }

        [Display(Name = "Trạng thái đơn hàng")]
        public string OrderStatus { get; set; } // Ví dụ: "Đã thanh toán", "Hoàn thành"

        /// <summary>
        /// Thuộc tính phụ để hiển thị kiểu badge trên giao diện
        /// </summary>
        public string StatusClass => OrderStatus switch
        {
            "Hoàn thành" => "success",
            "Đã thanh toán" => "primary",
            "Đã hủy" => "danger",
            _ => "secondary"
        };
    }
}