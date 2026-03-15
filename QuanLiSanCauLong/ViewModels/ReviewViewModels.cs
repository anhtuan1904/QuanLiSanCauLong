// ═══════════════════════════════════════════════════════════════════
//  FILE: ViewModels/ReviewViewModels.cs
// ═══════════════════════════════════════════════════════════════════
using System.ComponentModel.DataAnnotations;
using QuanLiSanCauLong.Models;

namespace QuanLiSanCauLong.ViewModels
{
    // ─────────────────────────────────────────────────────────────────
    //  ADMIN — Index/List
    // ─────────────────────────────────────────────────────────────────
    public class AdminReviewIndexViewModel
    {
        public List<ReviewCardViewModel> Reviews { get; set; } = new();
        public ReviewStatsViewModel Stats { get; set; } = new();
        public ReviewFilterViewModel Filter { get; set; } = new();

        // For dropdowns
        public List<FacilitySelectItem> Facilities { get; set; } = new();
        public List<CourtSelectItem> Courts { get; set; } = new();

        // Pagination
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalCount { get; set; } = 0;
        public int PageSize { get; set; } = 15;
    }

    // ─────────────────────────────────────────────────────────────────
    //  REVIEW CARD  — hiển thị trong list
    // ─────────────────────────────────────────────────────────────────
    public class ReviewCardViewModel
    {
        public int ReviewId { get; set; }
        public int CourtId { get; set; }
        public string CourtName { get; set; } = string.Empty;
        public string FacilityName { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserAvatar { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string? Content { get; set; }
        public string Status { get; set; } = "Pending";
        public bool IsVerifiedBooking { get; set; }
        public bool IsFeatured { get; set; }
        public int LikeCount { get; set; }
        public int ReplyCount { get; set; }
        public int ImageCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? ApprovedBy { get; set; }
        public string? RejectionReason { get; set; }

        // Sub-ratings
        public int? RatingCleanliness { get; set; }
        public int? RatingCourtQuality { get; set; }
        public int? RatingService { get; set; }
        public int? RatingValue { get; set; }

        // First image thumbnail
        public string? ThumbnailUrl { get; set; }

        // Admin reply (nếu có)
        public ReviewReplyViewModel? AdminReply { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────
    //  REVIEW DETAIL  — modal chi tiết
    // ─────────────────────────────────────────────────────────────────
    public class ReviewDetailViewModel
    {
        public ReviewCardViewModel Review { get; set; } = new();
        public List<ReviewImageViewModel> Images { get; set; } = new();
        public List<ReviewReplyViewModel> Replies { get; set; } = new();
        public int LikeCount { get; set; }
        public int HelpfulCount { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────
    //  REPLY ViewModel
    // ─────────────────────────────────────────────────────────────────
    public class ReviewReplyViewModel
    {
        public int ReplyId { get; set; }
        public int ReviewId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserAvatar { get; set; } = string.Empty;
        public string AuthorRole { get; set; } = "Customer";
        public string Content { get; set; } = string.Empty;
        public bool IsAdminReply { get; set; }
        public bool IsEdited { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? ParentReplyId { get; set; }
        public List<ReviewReplyViewModel> ChildReplies { get; set; } = new();
    }

    // ─────────────────────────────────────────────────────────────────
    //  IMAGE ViewModel
    // ─────────────────────────────────────────────────────────────────
    public class ReviewImageViewModel
    {
        public int ImageId { get; set; }
        public string ImagePath { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────
    //  STATS  — dashboard numbers
    // ─────────────────────────────────────────────────────────────────
    public class ReviewStatsViewModel
    {
        public int Total { get; set; }
        public int Pending { get; set; }
        public int Approved { get; set; }
        public int Rejected { get; set; }
        public int Hidden { get; set; }
        public double AverageRating { get; set; }
        public int FiveStars { get; set; }
        public int FourStars { get; set; }
        public int ThreeStars { get; set; }
        public int TwoStars { get; set; }
        public int OneStar { get; set; }
        public int WithImages { get; set; }
        public int Verified { get; set; }
        public int Featured { get; set; }
        public int TodayCount { get; set; }
        public int ThisWeekCount { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────
    //  FILTER
    // ─────────────────────────────────────────────────────────────────
    public class ReviewFilterViewModel
    {
        public int? FacilityId { get; set; }
        public int? CourtId { get; set; }
        public string? Status { get; set; }
        public int? Rating { get; set; }
        public string? Search { get; set; }
        public bool? IsVerified { get; set; }
        public bool? HasImages { get; set; }
        public bool? IsFeatured { get; set; }
        public string? DateRange { get; set; }  // today / week / month
        public string SortBy { get; set; } = "newest";
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 15;
    }

    // ─────────────────────────────────────────────────────────────────
    //  ADMIN ACTIONS
    // ─────────────────────────────────────────────────────────────────
    public class AdminReplyRequest
    {
        [Required]
        public int ReviewId { get; set; }

        [Required]
        [MaxLength(1000)]
        public string Content { get; set; } = string.Empty;

        public int? ParentReplyId { get; set; }
    }

    public class AdminModerateRequest
    {
        [Required]
        public int ReviewId { get; set; }

        [Required]
        public string Action { get; set; } = string.Empty; // Approve|Reject|Hide|Feature|Unfeature

        public string? RejectionReason { get; set; }
    }

    public class BulkModerateRequest
    {
        [Required]
        public List<int> ReviewIds { get; set; } = new();
        [Required]
        public string Action { get; set; } = string.Empty;
    }
    // ─────────────────────────────────────────────────────────────────
    //  USER INTERACTIONS — Request từ client gửi lên
    // ─────────────────────────────────────────────────────────────────
    public class LikeRequest
    {
        [Required]
        public int ReviewId { get; set; }

        // Đổi sang string để khớp với DB
        public string LikeType { get; set; } = "1";

        public string TargetType { get; set; } = "CourtReview";
    }
    // ─────────────────────────────────────────────────────────────────
    //  HELPERS
    // ─────────────────────────────────────────────────────────────────
    public record FacilitySelectItem(int FacilityId, string FacilityName);
    public record CourtSelectItem(int CourtId, string CourtNumber, string FacilityName);
}
