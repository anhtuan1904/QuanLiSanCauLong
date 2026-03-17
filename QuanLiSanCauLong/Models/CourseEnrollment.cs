using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLiSanCauLong.Models
{
    /// <summary>
    /// Lưu thông tin đăng ký dịch vụ (khóa học / căng vợt / giải đấu)
    /// — mỗi bản ghi liên kết với 1 Order để xử lý thanh toán.
    /// </summary>
    public class ServiceEnrollment
    {
        [Key]
        public int EnrollmentId { get; set; }

        // Loại dịch vụ: Course | Stringing | Tournament
        [Required]
        [StringLength(20)]
        public string ServiceType { get; set; } = string.Empty;

        // ID của dịch vụ tương ứng
        public int? CourseId { get; set; }
        public int? StringingId { get; set; }
        public int? TournamentId { get; set; }

        // Liên kết đơn hàng (bắt buộc)
        [Required]
        public int OrderId { get; set; }

        // Thông tin người đăng ký (khách vãng lai — không có tài khoản)
        [Required]
        [StringLength(100)]
        public string CustomerName { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Phone { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(500)]
        public string? Note { get; set; }

        // Trạng thái đăng ký
        // Pending → Paid → Active / Confirmed → Cancelled
        [StringLength(20)]
        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Navigation
        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; } = null!;

        [ForeignKey("CourseId")]
        public virtual Course? Course { get; set; }

        [ForeignKey("StringingId")]
        public virtual StringingService? StringingService { get; set; }

        [ForeignKey("TournamentId")]
        public virtual Tournament? Tournament { get; set; }
    }
}
