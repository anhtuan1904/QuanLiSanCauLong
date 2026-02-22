using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiSanCauLong.Data;
using QuanLiSanCauLong.Models;
using QuanLiSanCauLong.ViewModels;

namespace QuanLiSanCauLong.Controllers
{
    public class BlogReviewController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<BlogReviewController> _log;
        private const int DefaultPageSize = 10;

        public BlogReviewController(ApplicationDbContext db,
            ILogger<BlogReviewController> log)
        {
            _db = db;
            _log = log;
        }

        // ── GET /BlogReview?blogId=1&page=1&sort=newest ────────
        // Trả về trang đầy đủ hoặc partial (AJAX load more)
        public async Task<IActionResult> Index(
            int blogId, int page = 1, string sort = "newest",
            int? filterRating = null)
        {
            var ip = GetClientIp();
            var userId = User.Identity?.IsAuthenticated == true
                ? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                : null;

            var vm = await BuildPageViewModel(blogId, page, sort, filterRating, ip, userId);
            if (vm == null) return NotFound();

            if (Request.IsAjax())
                return PartialView("_ReviewListPartial", vm);

            return View(vm);
        }

        // ── POST /BlogReview/Submit ────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(BlogReviewSubmitViewModel form)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return Json(new BlogReviewApiResponse
                {
                    Success = false,
                    Message = string.Join(", ", errors)
                });
            }

            // Anti-spam: kiểm tra IP gửi trong 1 giờ qua
            var ip = GetClientIp();
            var recentCount = await _db.BlogReviews
                .CountAsync(r => r.IpAddress == ip &&
                             r.BlogId == form.BlogId &&
                             r.CreatedAt >= DateTime.Now.AddHours(-1));
            if (recentCount >= 3)
                return Json(new BlogReviewApiResponse
                {
                    Success = false,
                    Message = "Bạn đã gửi quá nhiều đánh giá. Vui lòng thử lại sau."
                });

            var userId = User.Identity?.IsAuthenticated == true
                ? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                : null;

            var review = new BlogReview
            {
                BlogId = form.BlogId,
                ReviewerName = form.ReviewerName.Trim(),
                ReviewerEmail = form.ReviewerEmail?.Trim().ToLower(),
                UserId = userId,
                IsMember = userId != null,
                Content = form.Content.Trim(),
                Rating = form.Rating,
                Reaction = form.Reaction,
                Status = "Pending",   // luôn chờ duyệt
                IpAddress = ip,
                CreatedAt = DateTime.Now,
            };

            _db.BlogReviews.Add(review);
            await _db.SaveChangesAsync();

            _log.LogInformation("New blog review #{Id} for blog #{BlogId} from {Ip}",
                review.ReviewId, review.BlogId, ip);

            return Json(new BlogReviewApiResponse
            {
                Success = true,
                Message = "Cảm ơn bạn đã đánh giá! Bình luận sẽ hiển thị sau khi được kiểm duyệt.",
                Data = new { reviewId = review.ReviewId }
            });
        }

        // ── POST /BlogReview/Like ──────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Like(int reviewId, bool isLike)
        {
            var ip = GetClientIp();
            var userId = User.Identity?.IsAuthenticated == true
                ? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                : null;

            var review = await _db.BlogReviews.FindAsync(reviewId);
            if (review == null || review.Status != "Approved")
                return Json(new { success = false, message = "Không tìm thấy đánh giá" });

            // Kiểm tra đã vote chưa
            BlogReviewLike? existing = null;
            if (userId != null)
                existing = await _db.BlogReviewLikes
                    .FirstOrDefaultAsync(l => l.ReviewId == reviewId && l.UserId == userId);
            else
                existing = await _db.BlogReviewLikes
                    .FirstOrDefaultAsync(l => l.ReviewId == reviewId && l.IpAddress == ip);

            if (existing != null)
            {
                if (existing.IsLike == isLike)
                {
                    // Bỏ vote
                    _db.BlogReviewLikes.Remove(existing);
                    if (isLike) review.LikeCount = Math.Max(0, review.LikeCount - 1);
                    else review.DislikeCount = Math.Max(0, review.DislikeCount - 1);
                    await _db.SaveChangesAsync();
                    return Json(new
                    {
                        success = true,
                        liked = false,
                        disliked = false,
                        likes = review.LikeCount,
                        dislikes = review.DislikeCount
                    });
                }
                else
                {
                    // Đổi chiều vote
                    if (existing.IsLike) { review.LikeCount--; review.DislikeCount++; }
                    else { review.DislikeCount--; review.LikeCount++; }
                    existing.IsLike = isLike;
                }
            }
            else
            {
                _db.BlogReviewLikes.Add(new BlogReviewLike
                {
                    ReviewId = reviewId,
                    UserId = userId,
                    IpAddress = ip,
                    IsLike = isLike,
                });
                if (isLike) review.LikeCount++;
                else review.DislikeCount++;
            }

            await _db.SaveChangesAsync();
            return Json(new
            {
                success = true,
                liked = isLike,
                disliked = !isLike,
                likes = review.LikeCount,
                dislikes = review.DislikeCount
            });
        }

        // ── POST /BlogReview/Report ────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Report(int reviewId)
        {
            var review = await _db.BlogReviews.FindAsync(reviewId);
            if (review == null)
                return Json(new { success = false });

            review.ReportCount++;
            // Tự động chuyển Spam nếu bị report >= 5 lần
            if (review.ReportCount >= 5)
                review.Status = "Spam";

            await _db.SaveChangesAsync();
            return Json(new { success = true, message = "Đã gửi báo cáo. Cảm ơn bạn!" });
        }

        // ── GET /BlogReview/LoadMore ───────────────────────────
        // AJAX endpoint - trả về partial HTML
        [HttpGet]
        public async Task<IActionResult> LoadMore(
            int blogId, int page, string sort = "newest", int? filterRating = null)
        {
            var ip = GetClientIp();
            var userId = User.Identity?.IsAuthenticated == true
                ? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                : null;

            var vm = await BuildPageViewModel(blogId, page, sort, filterRating, ip, userId);
            if (vm == null) return NotFound();

            return PartialView("_ReviewListPartial", vm);
        }

        // ── PRIVATE HELPERS ────────────────────────────────────
        private async Task<BlogReviewPageViewModel?> BuildPageViewModel(
            int blogId, int page, string sort, int? filterRating,
            string? ip, string? userId)
        {
            // Validate blogId (nếu có Blog model):
            // var blog = await _db.Blogs.FindAsync(blogId);
            // if (blog == null) return null;

            var query = _db.BlogReviews
                .Where(r => r.BlogId == blogId && r.Status == "Approved");

            if (filterRating.HasValue)
                query = query.Where(r => r.Rating == filterRating);

            // Thống kê trước khi phân trang
            var allApproved = await _db.BlogReviews
                .Where(r => r.BlogId == blogId && r.Status == "Approved")
                .ToListAsync();

            var starDist = Enumerable.Range(1, 5)
                .ToDictionary(s => s, s => allApproved.Count(r => r.Rating == s));
            var avg = allApproved.Any() ? allApproved.Average(r => r.Rating) : 0;

            // Sort
            query = sort switch
            {
                "oldest" => query.OrderBy(r => r.CreatedAt),
                "highest" => query.OrderByDescending(r => r.Rating),
                "lowest" => query.OrderBy(r => r.Rating),
                "helpful" => query.OrderByDescending(r => r.LikeCount),
                _ => query.OrderByDescending(r => r.IsPinned)
                                  .ThenByDescending(r => r.CreatedAt)
            };

            var total = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * DefaultPageSize)
                .Take(DefaultPageSize)
                .ToListAsync();

            // Lấy likes của user hiện tại
            var reviewIds = items.Select(r => r.ReviewId).ToList();
            HashSet<int> likedSet = new();
            HashSet<int> dislikedSet = new();

            if (userId != null)
            {
                var userLikes = await _db.BlogReviewLikes
                    .Where(l => reviewIds.Contains(l.ReviewId) && l.UserId == userId)
                    .ToListAsync();
                likedSet = userLikes.Where(l => l.IsLike).Select(l => l.ReviewId).ToHashSet();
                dislikedSet = userLikes.Where(l => !l.IsLike).Select(l => l.ReviewId).ToHashSet();
            }
            else
            {
                var ipLikes = await _db.BlogReviewLikes
                    .Where(l => reviewIds.Contains(l.ReviewId) && l.IpAddress == ip)
                    .ToListAsync();
                likedSet = ipLikes.Where(l => l.IsLike).Select(l => l.ReviewId).ToHashSet();
                dislikedSet = ipLikes.Where(l => !l.IsLike).Select(l => l.ReviewId).ToHashSet();
            }

            // Featured reviews (ghim đầu, không phân trang)
            var featured = allApproved
                .Where(r => r.IsFeatured || r.IsPinned)
                .OrderByDescending(r => r.IsPinned)
                .Take(3).ToList();

            return new BlogReviewPageViewModel
            {
                BlogId = blogId,
                BlogTitle = $"Bài viết #{blogId}",
                // BlogTitle = blog.Title,

                AverageRating = Math.Round(avg, 1),
                TotalReviews = await _db.BlogReviews.CountAsync(r => r.BlogId == blogId),
                ApprovedCount = allApproved.Count,
                StarDistribution = starDist,
                HelpfulCount = allApproved.Count(r => r.Reaction == "helpful"),
                InsightfulCount = allApproved.Count(r => r.Reaction == "insightful"),
                WellWrittenCount = allApproved.Count(r => r.Reaction == "well-written"),

                Reviews = items.Select(r => MapToItemVm(r, likedSet, dislikedSet)).ToList(),
                FeaturedReviews = featured.Select(r => MapToItemVm(r, likedSet, dislikedSet)).ToList(),

                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(total / (double)DefaultPageSize),
                PageSize = DefaultPageSize,
                FilterRating = filterRating,
                SortBy = sort,

                SubmitForm = new BlogReviewSubmitViewModel { BlogId = blogId },
            };
        }

        private static BlogReviewItemViewModel MapToItemVm(
            BlogReview r, HashSet<int> liked, HashSet<int> disliked)
            => new()
            {
                ReviewId = r.ReviewId,
                BlogId = r.BlogId,
                DisplayName = r.DisplayName,
                Initial = r.Initial,
                IsMember = r.IsMember,
                Content = r.Content,
                Rating = r.Rating,
                Reaction = r.Reaction,
                LikeCount = r.LikeCount,
                DislikeCount = r.DislikeCount,
                IsFeatured = r.IsFeatured,
                IsPinned = r.IsPinned,
                AdminReply = r.AdminReply,
                AdminRepliedBy = r.AdminRepliedBy,
                AdminRepliedAt = r.AdminRepliedAt,
                CreatedAt = r.CreatedAt,
                TimeAgo = r.TimeAgo,
                UserHasLiked = liked.Contains(r.ReviewId),
                UserHasDisliked = disliked.Contains(r.ReviewId),
            };

        private string GetClientIp()
        {
            var forwarded = Request.Headers["X-Forwarded-For"].FirstOrDefault();
            return string.IsNullOrEmpty(forwarded)
                ? HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0"
                : forwarded.Split(',')[0].Trim();
        }
    }
}
