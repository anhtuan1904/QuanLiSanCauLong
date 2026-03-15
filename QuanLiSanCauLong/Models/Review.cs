// ═══════════════════════════════════════════════════════════════════
//  FILE: Models/CourtReview.cs  (và các model liên quan)
//  Thêm vào namespace QuanLiSanCauLong.Models
// ═══════════════════════════════════════════════════════════════════
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLiSanCauLong.Models
{
    // ─────────────────────────────────────────────────────────────────
    //  1. COURT REVIEW  — đánh giá chính
    // ─────────────────────────────────────────────────────────────────
    public class CourtReview
    {
        [Key]
        public int ReviewId { get; set; }

        // ── FKs ──
        [Required]
        public int CourtId { get; set; }

        [Required]
        public int UserId { get; set; }

        /// <summary>Booking đã hoàn thành — dùng để verify "đã chơi"</summary>
        public int? BookingId { get; set; }

        // ── Nội dung ──
        [Required]
        [Range(1, 5, ErrorMessage = "Số sao phải từ 1 đến 5")]
        public int Rating { get; set; }   // 1–5 sao

        [MaxLength(2000)]
        public string? Content { get; set; }

        // ── Phân loại con (optional richness) ──
        /// <summary>1–5: sạch sẽ, tiện nghi</summary>
        public int? RatingCleanliness { get; set; }
        /// <summary>1–5: chất lượng sân</summary>
        public int? RatingCourtQuality { get; set; }
        /// <summary>1–5: dịch vụ nhân viên</summary>
        public int? RatingService { get; set; }
        /// <summary>1–5: giá cả</summary>
        public int? RatingValue { get; set; }

        // ── Trạng thái kiểm duyệt ──
        /// <summary>Pending | Approved | Rejected | Hidden</summary>
        [MaxLength(20)]
        public string Status { get; set; } = "Pending";

        public string? RejectionReason { get; set; }

        // ── Flags ──
        public bool IsVerifiedBooking { get; set; } = false;
        public bool IsFeatured { get; set; } = false;  // ghim lên đầu

        // ── Timestamps ──
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public int? ApprovedByUserId { get; set; }

        // ── Navigations ──
        [ForeignKey(nameof(CourtId))]
        public virtual Court? Court { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual User? User { get; set; }

        [ForeignKey(nameof(BookingId))]
        public virtual Booking? Booking { get; set; }

        public virtual ICollection<ReviewReply> Replies { get; set; } = new List<ReviewReply>();
        public virtual ICollection<ReviewLike> Likes { get; set; } = new List<ReviewLike>();
        public virtual ICollection<ReviewImage> Images { get; set; } = new List<ReviewImage>();
        public virtual ICollection<CourtReview> CourtReviews { get; set; } = new List<CourtReview>();
    }

    // ─────────────────────────────────────────────────────────────────
    //  2. REVIEW REPLY  — phản hồi (admin hoặc user khác)
    // ─────────────────────────────────────────────────────────────────
    public class ReviewReply
    {
        [Key]
        public int ReplyId { get; set; }

        [Required]
        public int ReviewId { get; set; }

        [Required]
        public int UserId { get; set; }

        /// <summary>Null = reply to review; có giá trị = nested reply</summary>
        public int? ParentReplyId { get; set; }

        [Required]
        [MaxLength(1000)]
        public string Content { get; set; } = string.Empty;

        /// <summary>Customer | Admin | Staff</summary>
        [MaxLength(20)]
        public string AuthorRole { get; set; } = "Customer";

        public bool IsAdminReply { get; set; } = false;
        public bool IsEdited { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        // ── Navigations ──
        [ForeignKey(nameof(ReviewId))]
        public virtual CourtReview? Review { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual User? User { get; set; }

        [ForeignKey(nameof(ParentReplyId))]
        public virtual ReviewReply? ParentReply { get; set; }
        public virtual ICollection<ReviewReply> ChildReplies { get; set; } = new List<ReviewReply>();
    }

    // ─────────────────────────────────────────────────────────────────
    //  3. REVIEW LIKE  — like / helpful / unhelpful
    // ─────────────────────────────────────────────────────────────────
    public class ReviewLike
    {
        [Key]
        public int LikeId { get; set; }

        [Required]
        public int ReviewId { get; set; }

        [Required]
        public int UserId { get; set; }

        /// <summary>Like | Dislike | Helpful</summary>
        [MaxLength(20)]
        public string LikeType { get; set; } = "Like";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey(nameof(ReviewId))]
        public virtual CourtReview? Review { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual User? User { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────
    //  4. REVIEW IMAGE  — ảnh đính kèm theo đánh giá
    // ─────────────────────────────────────────────────────────────────
    public class ReviewImage
    {
        [Key]
        public int ImageId { get; set; }

        [Required]
        public int ReviewId { get; set; }

        [Required]
        [MaxLength(500)]
        public string ImagePath { get; set; } = string.Empty;

        public int DisplayOrder { get; set; } = 0;
        public DateTime UploadedAt { get; set; } = DateTime.Now;

        [ForeignKey(nameof(ReviewId))]
        public virtual CourtReview? Review { get; set; }
    }
}

// ═══════════════════════════════════════════════════════════════════
//  THÊM VÀO ApplicationDbContext.cs:
// ═══════════════════════════════════════════════════════════════════
/*
    public DbSet<CourtReview>  CourtReviews  { get; set; }
    public DbSet<ReviewReply>  ReviewReplies { get; set; }
    public DbSet<ReviewLike>   ReviewLikes   { get; set; }
    public DbSet<ReviewImage>  ReviewImages  { get; set; }

    // Trong OnModelCreating:
    modelBuilder.Entity<ReviewLike>()
        .HasIndex(l => new { l.ReviewId, l.UserId, l.LikeType })
        .IsUnique(); // mỗi user chỉ like 1 lần

    modelBuilder.Entity<CourtReview>()
        .HasIndex(r => new { r.CourtId, r.UserId, r.BookingId })
        .IsUnique(); // mỗi booking chỉ đánh giá 1 lần
*/
