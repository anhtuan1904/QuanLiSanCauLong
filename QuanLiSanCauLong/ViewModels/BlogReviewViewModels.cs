using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using QuanLiSanCauLong.Models;

namespace QuanLiSanCauLong.ViewModels
{
    // ── Dùng để hiển thị 1 đánh giá (public) ──────────────
    public class BlogReviewItemViewModel
    {
        public int ReviewId { get; set; }
        public int BlogId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string Initial { get; set; } = string.Empty;
        public bool IsMember { get; set; }
        public string Content { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string? Reaction { get; set; }
        public int LikeCount { get; set; }
        public int DislikeCount { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsPinned { get; set; }
        public string? AdminReply { get; set; }
        public string? AdminRepliedBy { get; set; }
        public DateTime? AdminRepliedAt { get; set; }
        public bool HasAdminReply => !string.IsNullOrEmpty(AdminReply);
        public DateTime CreatedAt { get; set; }
        public string TimeAgo { get; set; } = string.Empty;
        // Người dùng hiện tại đã like chưa (inject từ controller)
        public bool UserHasLiked { get; set; }
        public bool UserHasDisliked { get; set; }
    }

    // ── Form submit đánh giá mới (public) ─────────────────
    public class BlogReviewSubmitViewModel
    {
        public int BlogId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên của bạn")]
        [StringLength(100)]
        [Display(Name = "Họ Tên")]
        public string ReviewerName { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(150)]
        [Display(Name = "Email (tuỳ chọn)")]
        public string? ReviewerEmail { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập nội dung đánh giá")]
        [MinLength(10, ErrorMessage = "Nội dung phải có ít nhất 10 ký tự")]
        [MaxLength(2000, ErrorMessage = "Nội dung tối đa 2000 ký tự")]
        [Display(Name = "Nội Dung")]
        public string Content { get; set; } = string.Empty;

        [Range(1, 5, ErrorMessage = "Vui lòng chọn số sao")]
        [Display(Name = "Điểm Đánh Giá")]
        public int Rating { get; set; } = 5;

        [StringLength(50)]
        [Display(Name = "Cảm Nhận Nhanh")]
        public string? Reaction { get; set; }
        // "helpful" | "insightful" | "well-written"
    }

    // ── Trang public: danh sách + thống kê + form ─────────
    public class BlogReviewPageViewModel
    {
        // Thông tin bài viết
        public int BlogId { get; set; }
        public string BlogTitle { get; set; } = string.Empty;
        public string? BlogSlug { get; set; }
        public string? BlogImage { get; set; }

        // Thống kê tổng quan
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public int ApprovedCount { get; set; }
        public Dictionary<int, int> StarDistribution { get; set; } = new();
        // Key = 1..5, Value = count

        // Reaction counts
        public int HelpfulCount { get; set; }
        public int InsightfulCount { get; set; }
        public int WellWrittenCount { get; set; }

        // Danh sách đánh giá
        public List<BlogReviewItemViewModel> Reviews { get; set; } = new();
        public List<BlogReviewItemViewModel> FeaturedReviews { get; set; } = new();

        // Phân trang
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        // Filter
        public int? FilterRating { get; set; }
        public string? SortBy { get; set; } = "newest";
        // newest | oldest | highest | lowest | helpful

        // Form submit mới
        public BlogReviewSubmitViewModel SubmitForm { get; set; } = new();

        // Helper
        public int StarPercent(int star) =>
            TotalReviews == 0 ? 0 :
            (int)Math.Round((StarDistribution.GetValueOrDefault(star, 0) * 100.0) / TotalReviews);
    }

    // ── Admin: list với filter và phân trang ──────────────
    public class AdminBlogReviewListViewModel
    {
        public List<AdminBlogReviewRowViewModel> Reviews { get; set; } = new();

        // Filter
        public string? FilterStatus { get; set; }
        public int? FilterRating { get; set; }
        public int? FilterBlogId { get; set; }
        public string? SearchQuery { get; set; }
        public string SortBy { get; set; } = "newest";

        // Phân trang
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalCount { get; set; }

        // Thống kê nhanh
        public int PendingCount { get; set; }
        public int ApprovedCount { get; set; }
        public int RejectedCount { get; set; }
        public int SpamCount { get; set; }
        public double OverallAverage { get; set; }

        // Dropdown list blog titles
        public List<BlogDropdownItem> BlogList { get; set; } = new();
    }

    public class AdminBlogReviewRowViewModel
    {
        public int ReviewId { get; set; }
        public int BlogId { get; set; }
        public string BlogTitle { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Initial { get; set; } = string.Empty;
        public string? Email { get; set; }
        public bool IsMember { get; set; }
        public string Content { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string? Reaction { get; set; }
        public string Status { get; set; } = string.Empty;
        public string StatusLabel { get; set; } = string.Empty;
        public string StatusColor { get; set; } = string.Empty;
        public int LikeCount { get; set; }
        public int ReportCount { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsPinned { get; set; }
        public bool HasAdminReply { get; set; }
        public string TimeAgo { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? IpAddress { get; set; }
    }

    // ── Admin: detail + reply form ─────────────────────────
    public class AdminBlogReviewDetailViewModel
    {
        public BlogReview Review { get; set; } = null!;
        public string BlogTitle { get; set; } = string.Empty;

        [MaxLength(2000, ErrorMessage = "Phản hồi tối đa 2000 ký tự")]
        [Display(Name = "Nội Dung Phản Hồi")]
        public string? AdminReply { get; set; }
    }

    // ── Dropdown helper ────────────────────────────────────
    public class BlogDropdownItem
    {
        public int BlogId { get; set; }
        public string BlogTitle { get; set; } = string.Empty;
        public int ReviewCount { get; set; }
    }

    // ── API Response (cho AJAX) ────────────────────────────
    public class BlogReviewApiResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }
    }
}
