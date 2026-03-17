using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLiSanCauLong.Models
{
    public class StringingService
    {
        [Key]
        public int StringingId { get; set; }

        [Required(ErrorMessage = "Tên dịch vụ không được để trống")]
        [StringLength(200)]
        public string ServiceName { get; set; } = string.Empty;

        [Required]
        [StringLength(250)]
        public string Slug { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Brand { get; set; }

        [StringLength(100)]
        public string? StringModel { get; set; }

        public string? ShortDesc { get; set; }

        [Required(ErrorMessage = "Mô tả chi tiết không được để trống")]
        public string Description { get; set; } = string.Empty;

        public string? FeaturedImage { get; set; }
        public string? VideoUrl { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Price { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? DiscountPrice { get; set; }

        public int? DurationMinutes { get; set; }
        public int? WarrantyDays { get; set; }

        // Thông số kỹ thuật
        [StringLength(50)]
        public string? Tension { get; set; }

        public int? TensionMin { get; set; }
        public int? TensionMax { get; set; }

        [StringLength(50)]
        public string? StringColor { get; set; }

        [StringLength(20)]
        public string? StringGauge { get; set; }

        // Nội dung thu hút
        public string? Features { get; set; }
        public string? Benefits { get; set; }

        public int DisplayOrder { get; set; } = 0;

        [StringLength(20)]
        public string Status { get; set; } = "Active";

        public bool IsFeatured { get; set; } = false;
        public bool IsPopular { get; set; } = false;

        public int ViewCount { get; set; } = 0;
        public int TotalOrders { get; set; } = 0;

        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public virtual ICollection<StringingImage>? StringingImages { get; set; }
    }

    public class StringingImage
    {
        [Key]
        public int ImageId { get; set; }

        [Required]
        public int StringingId { get; set; }

        [Required]
        public string ImagePath { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Caption { get; set; }

        public bool IsPrimary { get; set; } = false;
        public int DisplayOrder { get; set; } = 0;
        public DateTime UploadedAt { get; set; } = DateTime.Now;

        public virtual StringingService? StringingService { get; set; }
    }
}
