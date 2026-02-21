using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLiSanCauLong.Models
{
    [Table("Courses")]
    public class Course
    {
        [Key]
        public int CourseId { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Tên Khóa Học")]
        public string CourseName { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Mô Tả Ngắn")]
        public string? ShortDesc { get; set; }

        [Required]
        [Display(Name = "Mô Tả Chi Tiết")]
        public string Description { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Huấn Luyện Viên")]
        public string? Instructor { get; set; }

        [StringLength(50)]
        [Display(Name = "Trình Độ")]
        public string? Level { get; set; }         // Beginner / Intermediate / Advanced

        [Display(Name = "Thời Lượng (tuần)")]
        public int? DurationWeeks { get; set; }

        [StringLength(300)]
        [Display(Name = "Lịch Học")]
        public string? Schedule { get; set; }      // VD: "Thứ 2,4,6 - 18h-19h30"

        [Display(Name = "Sĩ Số Tối Đa")]
        public int? MaxStudents { get; set; }

        [Display(Name = "Học Viên Hiện Tại")]
        public int CurrentStudents { get; set; } = 0;

        [Display(Name = "Học Phí (VNĐ)")]
        [Column(TypeName = "decimal(18,0)")]
        public decimal? TuitionFee { get; set; }

        [Display(Name = "Học Phí Khuyến Mãi")]
        [Column(TypeName = "decimal(18,0)")]
        public decimal? DiscountFee { get; set; }

        [StringLength(500)]
        [Display(Name = "Ảnh Đại Diện")]
        public string? FeaturedImage { get; set; }

        [Display(Name = "Tính Năng / Nội Dung")]
        public string? Features { get; set; }

        [StringLength(20)]
        [Display(Name = "Trạng Thái")]
        public string Status { get; set; } = "Active"; // Active / Inactive / Full

        [Display(Name = "Nổi Bật")]
        public bool IsFeatured { get; set; } = false;

        [Display(Name = "Thứ Tự Hiển Thị")]
        public int DisplayOrder { get; set; } = 0;

        [Display(Name = "Lượt Xem")]
        public int ViewCount { get; set; } = 0;

        [StringLength(300)]
        public string? Slug { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        // Computed
        [NotMapped]
        public int AvailableSlots => (MaxStudents ?? 0) - CurrentStudents;

        [NotMapped]
        public bool IsFull => MaxStudents.HasValue && CurrentStudents >= MaxStudents;

        [NotMapped]
        public decimal? EffectivePrice => DiscountFee ?? TuitionFee;
    }
}