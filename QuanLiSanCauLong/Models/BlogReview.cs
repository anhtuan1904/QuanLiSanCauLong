using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLiSanCauLong.Models
{
    [Table("BlogReviews")]
    public class BlogReview
    {
        [Key]
        public int ReviewId { get; set; }

        [Required]
        [Display(Name = "Bài Viết")]
        public int BlogId { get; set; }

        // Navigation đến Blog (nếu đã có Blog model)
        // public virtual Blog? Blog { get; set; }

        // ── Thông tin người đánh giá ───────────────────
        [StringLength(100)]
        [Display(Name = "Họ Tên")]
        public string? ReviewerName { get; set; }

        [StringLength(150)]
        [EmailAddress]
        [Display(Name = "Email")]
        public string? ReviewerEmail { get; set; }

        [StringLength(450)]
        [Display(Name = "User ID")]
        public string? UserId { get; set; }   // ASP.NET Identity UserId

        [Display(Name = "Là Thành Viên")]
        public bool IsMember { get; set; } = false;

        // ── Nội dung ───────────────────────────────────
        [Required(ErrorMessage = "Vui lòng nhập nội dung đánh giá")]
        [MinLength(10, ErrorMessage = "Nội dung phải có ít nhất 10 ký tự")]
        [StringLength(2000)]
        [Display(Name = "Nội Dung")]
        public string Content { get; set; } = string.Empty;

        // ── Điểm ───────────────────────────────────────
        [Range(1, 5, ErrorMessage = "Điểm từ 1 đến 5")]
        [Display(Name = "Điểm Đánh Giá")]
        public int Rating { get; set; } = 5;

        // ── Reaction (helpful / insightful / well-written) ──
        [StringLength(50)]
        [Display(Name = "Reaction")]
        public string? Reaction { get; set; }

        // ── Phản hồi admin ─────────────────────────────
        [Display(Name = "Phản Hồi Admin")]
        public string? AdminReply { get; set; }

        [Display(Name = "Ngày Phản Hồi")]
        public DateTime? AdminRepliedAt { get; set; }

        [StringLength(100)]
        [Display(Name = "Admin Phản Hồi Bởi")]
        public string? AdminRepliedBy { get; set; }

        // ── Trạng thái ─────────────────────────────────
        [StringLength(20)]
        [Display(Name = "Trạng Thái")]
        public string Status { get; set; } = "Pending";
        // Pending | Approved | Rejected | Spam

        [Display(Name = "Nổi Bật")]
        public bool IsFeatured { get; set; } = false;

        [Display(Name = "Ghim")]
        public bool IsPinned { get; set; } = false;

        // ── Thống kê ───────────────────────────────────
        [Display(Name = "Lượt Thích")]
        public int LikeCount { get; set; } = 0;

        [Display(Name = "Lượt Không Thích")]
        public int DislikeCount { get; set; } = 0;

        [Display(Name = "Số Báo Cáo")]
        public int ReportCount { get; set; } = 0;

        // ── Meta ───────────────────────────────────────
        [StringLength(45)]
        public string? IpAddress { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        // ── Navigation ─────────────────────────────────
        public virtual ICollection<BlogReviewLike>? Likes { get; set; }

        // ── Computed ───────────────────────────────────
        [NotMapped]
        public string StatusLabel => Status switch
        {
            "Pending" => "Chờ Duyệt",
            "Approved" => "Đã Duyệt",
            "Rejected" => "Từ Chối",
            "Spam" => "Spam",
            _ => Status
        };

        [NotMapped]
        public string StatusColor => Status switch
        {
            "Pending" => "#92660a",
            "Approved" => "#16a34a",
            "Rejected" => "#c94b2a",
            "Spam" => "#8a7560",
            _ => "#8a7560"
        };

        [NotMapped]
        public string DisplayName => !string.IsNullOrEmpty(ReviewerName)
            ? ReviewerName : IsMember ? "Hội Viên" : "Khách";

        [NotMapped]
        public string Initial => DisplayName.Length > 0
            ? DisplayName.Substring(0, 1).ToUpper() : "?";

        [NotMapped]
        public bool HasAdminReply => !string.IsNullOrEmpty(AdminReply);

        [NotMapped]
        public string TimeAgo
        {
            get
            {
                var diff = DateTime.Now - CreatedAt;
                if (diff.TotalMinutes < 1) return "Vừa xong";
                if (diff.TotalHours < 1) return $"{(int)diff.TotalMinutes} phút trước";
                if (diff.TotalDays < 1) return $"{(int)diff.TotalHours} giờ trước";
                if (diff.TotalDays < 30) return $"{(int)diff.TotalDays} ngày trước";
                return CreatedAt.ToString("dd/MM/yyyy");
            }
        }
    }
}
