using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace QuanLiSanCauLong.ViewModels
{
    public class VoucherViewModel
    {
        public int VoucherId { get; set; }

        [Required]
        [Display(Name = "Mã voucher")]
        public string VoucherCode { get; set; }

        [Required]
        [Display(Name = "Tên voucher")]
        public string VoucherName { get; set; }

        [Display(Name = "Mô tả")]
        public string Description { get; set; }

        [Required]
        [Display(Name = "Loại giảm giá")]
        public string DiscountType { get; set; }

        [Required]
        [Display(Name = "Giá trị giảm")]
        public decimal DiscountValue { get; set; }

        [Display(Name = "Đơn hàng tối thiểu")]
        public decimal? MinOrderAmount { get; set; }

        [Display(Name = "Giảm tối đa")]
        public decimal? MaxDiscount { get; set; }

        [Display(Name = "Áp dụng cho")]
        public string ApplicableFor { get; set; }

        [Required]
        [Display(Name = "Ngày bắt đầu")]
        public DateTime StartDate { get; set; }

        [Required]
        [Display(Name = "Ngày kết thúc")]
        public DateTime EndDate { get; set; }

        [Display(Name = "Giới hạn số lượt")]
        public int? UsageLimit { get; set; }

        [Display(Name = "Giới hạn mỗi user")]
        public int UsageLimitPerUser { get; set; }

        public int UsedCount { get; set; }
        public bool IsActive { get; set; }
        public bool IsExpired => DateTime.Now > EndDate;
        public bool IsValid => IsActive && !IsExpired && (UsageLimit == null || UsedCount < UsageLimit);
    }
}
