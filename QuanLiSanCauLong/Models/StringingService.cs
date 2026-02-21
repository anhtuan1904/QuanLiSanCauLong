using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLiSanCauLong.Models
{
    [Table("StringingServices")]
    public class StringingService
    {
        [Key]
        public int StringingId { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Tên Dịch Vụ")]
        public string ServiceName { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Thương Hiệu Dây")]
        public string? Brand { get; set; }          // Yonex, Victor, Lining...

        [StringLength(100)]
        [Display(Name = "Model Dây")]
        public string? StringModel { get; set; }   // BG80, VS850...

        [StringLength(500)]
        [Display(Name = "Mô Tả Ngắn")]
        public string? ShortDesc { get; set; }

        [Required]
        [Display(Name = "Mô Tả Chi Tiết")]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Giá Dịch Vụ (VNĐ)")]
        [Column(TypeName = "decimal(18,0)")]
        public decimal? Price { get; set; }

        [Display(Name = "Thời Gian Thực Hiện (phút)")]
        public int? DurationMinutes { get; set; }

        [Display(Name = "Bảo Hành (ngày)")]
        public int? WarrantyDays { get; set; }

        [StringLength(100)]
        [Display(Name = "Lực Căng Hỗ Trợ")]
        public string? Tension { get; set; }        // VD: "20-30 lbs"

        [StringLength(500)]
        [Display(Name = "Ảnh Đại Diện")]
        public string? FeaturedImage { get; set; }

        [Display(Name = "Tính Năng")]
        public string? Features { get; set; }

        [StringLength(20)]
        [Display(Name = "Trạng Thái")]
        public string Status { get; set; } = "Active";

        [Display(Name = "Nổi Bật")]
        public bool IsFeatured { get; set; } = false;

        [Display(Name = "Phổ Biến")]
        public bool IsPopular { get; set; } = false;

        [Display(Name = "Thứ Tự Hiển Thị")]
        public int DisplayOrder { get; set; } = 0;

        [Display(Name = "Tổng Đơn Hàng")]
        public int TotalOrders { get; set; } = 0;

        [Display(Name = "Lượt Xem")]
        public int ViewCount { get; set; } = 0;

        [StringLength(300)]
        public string? Slug { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
    }
}
