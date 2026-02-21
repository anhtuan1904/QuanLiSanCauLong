using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLiSanCauLong.Models
{
    /// <summary>
    /// Model cho dịch vụ
    /// </summary>
    public class Service
    {
        [Key]
        public int ServiceId { get; set; }

        [Required(ErrorMessage = "Tên dịch vụ không được để trống")]
        [StringLength(200)]
        public string ServiceName { get; set; } = string.Empty;

        [Required]
        [StringLength(250)]
        public string Slug { get; set; } = string.Empty;

        public string? ShortDescription { get; set; }

        [Required]
        public string Description { get; set; } = string.Empty;

        public string? Icon { get; set; }
        public string? FeaturedImage { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Price { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? DiscountPrice { get; set; }

        [StringLength(20)]
        public string? PriceUnit { get; set; } // /giờ, /tháng, /lần

        public string? Features { get; set; } // JSON array

        public int DisplayOrder { get; set; } = 0;

        [StringLength(20)]
        public string Status { get; set; } = "Active"; // Active, Inactive

        public bool IsFeatured { get; set; } = false;
        public bool IsPopular { get; set; } = false;

        public int ViewCount { get; set; } = 0;

        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Navigation
        public virtual ICollection<ServiceImage>? ServiceImages { get; set; }
        public virtual ICollection<ServiceInquiry>? Inquiries { get; set; }
    }

    /// <summary>
    /// Hình ảnh dịch vụ
    /// </summary>
    public class ServiceImage
    {
        [Key]
        public int ImageId { get; set; }

        [Required]
        public int ServiceId { get; set; }

        [Required]
        public string ImagePath { get; set; } = string.Empty;

        public bool IsPrimary { get; set; } = false;
        public int DisplayOrder { get; set; } = 0;
        public DateTime UploadedAt { get; set; } = DateTime.Now;

        // Navigation
        public virtual Service? Service { get; set; }
    }

    /// <summary>
    /// Yêu cầu tư vấn dịch vụ
    /// </summary>
    public class ServiceInquiry
    {
        [Key]
        public int InquiryId { get; set; }

        [Required]
        public int ServiceId { get; set; }

        [Required]
        [StringLength(100)]
        public string CustomerName { get; set; } = string.Empty;

        [Required]
        [StringLength(15)]
        public string Phone { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Email { get; set; }

        public string? Message { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "New"; // New, Contacted, Completed, Cancelled

        public string? Notes { get; set; }
        public string? AssignedTo { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? ContactedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        // Navigation
        public virtual Service? Service { get; set; }
    }
}
