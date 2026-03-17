using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLiSanCauLong.Models
{
    public class Course
    {
        [Key]
        public int CourseId { get; set; }

        [Required(ErrorMessage = "Tên khóa học không được để trống")]
        [StringLength(200)]
        public string CourseName { get; set; } = string.Empty;

        [Required]
        [StringLength(250)]
        public string Slug { get; set; } = string.Empty;

        public string? ShortDesc { get; set; }

        [Required(ErrorMessage = "Mô tả chi tiết không được để trống")]
        public string Description { get; set; } = string.Empty;

        // Huấn luyện viên
        [StringLength(100)]
        public string? Instructor { get; set; }

        [StringLength(150)]
        public string? InstructorTitle { get; set; }

        public string? InstructorAvatar { get; set; }

        // Media
        public string? FeaturedImage { get; set; }
        public string? VideoUrl { get; set; }

        // Học phí
        [Column(TypeName = "decimal(18,2)")]
        public decimal? TuitionFee { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? DiscountFee { get; set; }

        // Chi tiết khóa học
        [Required(ErrorMessage = "Vui lòng chọn trình độ")]
        [StringLength(50)]
        public string Level { get; set; } = string.Empty;

        public int? DurationWeeks { get; set; }

        [StringLength(200)]
        public string? Schedule { get; set; }

        public int? MaxStudents { get; set; }
        public int CurrentStudents { get; set; } = 0;

        // Nội dung thu hút
        public string? Features { get; set; }
        public string? Highlights { get; set; }
        public string? Requirements { get; set; }
        public string? Outcomes { get; set; }

        // Hiển thị
        public int DisplayOrder { get; set; } = 0;

        [StringLength(20)]
        public string Status { get; set; } = "Active";

        public bool IsFeatured { get; set; } = false;
        public bool IsPopular { get; set; } = false;

        public int ViewCount { get; set; } = 0;

        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public virtual ICollection<CourseImage>? CourseImages { get; set; }

        [NotMapped]
        public int AvailableSlots => (MaxStudents ?? 0) - CurrentStudents;

        [NotMapped]
        public bool IsFull => MaxStudents.HasValue && CurrentStudents >= MaxStudents.Value;

        [NotMapped]
        public decimal EffectivePrice => DiscountFee ?? TuitionFee ?? 0;
    }

    public class CourseImage
    {
        [Key]
        public int ImageId { get; set; }

        [Required]
        public int CourseId { get; set; }

        [Required]
        public string ImagePath { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Caption { get; set; }

        public bool IsPrimary { get; set; } = false;
        public int DisplayOrder { get; set; } = 0;
        public DateTime UploadedAt { get; set; } = DateTime.Now;

        public virtual Course? Course { get; set; }
    }
}
