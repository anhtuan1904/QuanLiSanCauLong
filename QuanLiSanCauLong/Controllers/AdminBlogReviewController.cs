using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiSanCauLong.Data;
using QuanLiSanCauLong.Models;
using QuanLiSanCauLong.ViewModels;

namespace QuanLiSanCauLong.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminBlogReviewController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<AdminBlogReviewController> _log;
        private const int PageSize = 15;

        public AdminBlogReviewController(ApplicationDbContext db,
            ILogger<AdminBlogReviewController> log)
        {
            _db = db;
            _log = log;
        }

        // ── GET /AdminBlogReview ──────────────────────────────
        public async Task<IActionResult> Index(
            string? status, int? rating, int? blogId,
            string? q, string sort = "newest", int page = 1)
        {
            var query = _db.BlogReviews.AsQueryable();

            // Filter
            if (!string.IsNullOrEmpty(status)) query = query.Where(r => r.Status == status);
            if (rating.HasValue) query = query.Where(r => r.Rating == rating);
            if (blogId.HasValue) query = query.Where(r => r.BlogId == blogId);
            if (!string.IsNullOrEmpty(q))
                query = query.Where(r =>
                    r.ReviewerName!.Contains(q) ||
                    r.ReviewerEmail!.Contains(q) ||
                    r.Content.Contains(q));

            // Sort
            query = sort switch
            {
                "oldest" => query.OrderBy(r => r.CreatedAt),
                "highest" => query.OrderByDescending(r => r.Rating),
                "lowest" => query.OrderBy(r => r.Rating),
                "pending" => query.OrderBy(r => r.Status == "Pending" ? 0 : 1).ThenByDescending(r => r.CreatedAt),
                _ => query.OrderByDescending(r => r.CreatedAt)
            };

            var total = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            // Blog titles map (giả sử có Blog model)
            // var blogIds = items.Select(r => r.BlogId).Distinct().ToList();
            // var blogTitles = await _db.Blogs.Where(b => blogIds.Contains(b.BlogId))
            //     .ToDictionaryAsync(b => b.BlogId, b => b.Title);

            var vm = new AdminBlogReviewListViewModel
            {
                Reviews = items.Select(r => new AdminBlogReviewRowViewModel
                {
                    ReviewId = r.ReviewId,
                    BlogId = r.BlogId,
                    BlogTitle = $"Bài viết #{r.BlogId}", // thay bằng blogTitles.GetValueOrDefault(r.BlogId, ...)
                    DisplayName = r.DisplayName,
                    Initial = r.Initial,
                    Email = r.ReviewerEmail,
                    IsMember = r.IsMember,
                    Content = r.Content,
                    Rating = r.Rating,
                    Reaction = r.Reaction,
                    Status = r.Status,
                    StatusLabel = r.StatusLabel,
                    StatusColor = r.StatusColor,
                    LikeCount = r.LikeCount,
                    ReportCount = r.ReportCount,
                    IsFeatured = r.IsFeatured,
                    IsPinned = r.IsPinned,
                    HasAdminReply = r.HasAdminReply,
                    TimeAgo = r.TimeAgo,
                    CreatedAt = r.CreatedAt,
                    IpAddress = r.IpAddress,
                }).ToList(),

                FilterStatus = status,
                FilterRating = rating,
                FilterBlogId = blogId,
                SearchQuery = q,
                SortBy = sort,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(total / (double)PageSize),
                TotalCount = total,

                PendingCount = await _db.BlogReviews.CountAsync(r => r.Status == "Pending"),
                ApprovedCount = await _db.BlogReviews.CountAsync(r => r.Status == "Approved"),
                RejectedCount = await _db.BlogReviews.CountAsync(r => r.Status == "Rejected"),
                SpamCount = await _db.BlogReviews.CountAsync(r => r.Status == "Spam"),
                OverallAverage = await _db.BlogReviews
                    .Where(r => r.Status == "Approved")
                    .AverageAsync(r => (double?)r.Rating) ?? 0,

                // BlogList = await _db.Blogs.Select(b => new BlogDropdownItem
                //     { BlogId = b.BlogId, BlogTitle = b.Title }).ToListAsync(),
            };

            return View(vm);
        }

        // ── GET /AdminBlogReview/Detail/5 ─────────────────────
        public async Task<IActionResult> Detail(int id)
        {
            var review = await _db.BlogReviews
                .Include(r => r.Likes)
                .FirstOrDefaultAsync(r => r.ReviewId == id);
            if (review == null) return NotFound();

            var vm = new AdminBlogReviewDetailViewModel
            {
                Review = review,
                BlogTitle = $"Bài viết #{review.BlogId}",
                AdminReply = review.AdminReply,
            };
            return View(vm);
        }

        // ── POST /AdminBlogReview/Approve/5 ───────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var r = await _db.BlogReviews.FindAsync(id);
            if (r == null) return Json(new { success = false, message = "Không tìm thấy" });

            r.Status = "Approved";
            r.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();
            _log.LogInformation("Review #{Id} approved by {User}", id, User.Identity?.Name);

            TempData["Success"] = $"Đã duyệt đánh giá #{id}";
            return Request.IsAjax()
                ? Json(new { success = true, message = "Đã duyệt" })
                : RedirectToAction(nameof(Index));
        }

        // ── POST /AdminBlogReview/Reject/5 ────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var r = await _db.BlogReviews.FindAsync(id);
            if (r == null) return Json(new { success = false });

            r.Status = "Rejected";
            r.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();

            TempData["Warning"] = $"Đã từ chối đánh giá #{id}";
            return Request.IsAjax()
                ? Json(new { success = true })
                : RedirectToAction(nameof(Index));
        }

        // ── POST /AdminBlogReview/MarkSpam/5 ──────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkSpam(int id)
        {
            var r = await _db.BlogReviews.FindAsync(id);
            if (r == null) return Json(new { success = false });

            r.Status = "Spam";
            r.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();

            return Request.IsAjax()
                ? Json(new { success = true })
                : RedirectToAction(nameof(Index));
        }

        // ── POST /AdminBlogReview/ToggleFeatured/5 ────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleFeatured(int id)
        {
            var r = await _db.BlogReviews.FindAsync(id);
            if (r == null) return Json(new { success = false });

            r.IsFeatured = !r.IsFeatured;
            r.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();

            return Json(new { success = true, featured = r.IsFeatured });
        }

        // ── POST /AdminBlogReview/TogglePinned/5 ──────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePinned(int id)
        {
            var r = await _db.BlogReviews.FindAsync(id);
            if (r == null) return Json(new { success = false });

            r.IsPinned = !r.IsPinned;
            r.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();

            return Json(new { success = true, pinned = r.IsPinned });
        }

        // ── POST /AdminBlogReview/Reply ────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(int reviewId, string replyContent)
        {
            if (string.IsNullOrWhiteSpace(replyContent))
            {
                TempData["Error"] = "Nội dung phản hồi không được để trống";
                return RedirectToAction(nameof(Detail), new { id = reviewId });
            }

            var r = await _db.BlogReviews.FindAsync(reviewId);
            if (r == null) return NotFound();

            r.AdminReply = replyContent.Trim();
            r.AdminRepliedAt = DateTime.Now;
            r.AdminRepliedBy = User.Identity?.Name ?? "Admin";
            r.UpdatedAt = DateTime.Now;

            // Auto-approve nếu đang Pending
            if (r.Status == "Pending") r.Status = "Approved";

            await _db.SaveChangesAsync();
            TempData["Success"] = "Đã gửi phản hồi thành công";
            return RedirectToAction(nameof(Detail), new { id = reviewId });
        }

        // ── POST /AdminBlogReview/DeleteReply/5 ───────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReply(int id)
        {
            var r = await _db.BlogReviews.FindAsync(id);
            if (r == null) return Json(new { success = false });

            r.AdminReply = null;
            r.AdminRepliedAt = null;
            r.AdminRepliedBy = null;
            r.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();

            return Json(new { success = true });
        }

        // ── POST /AdminBlogReview/Delete/5 ────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var r = await _db.BlogReviews.FindAsync(id);
            if (r == null) return Json(new { success = false });

            _db.BlogReviews.Remove(r);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Đã xóa đánh giá #{id}";
            return Request.IsAjax()
                ? Json(new { success = true })
                : RedirectToAction(nameof(Index));
        }

        // ── POST /AdminBlogReview/BulkAction ──────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkAction(string action, int[] ids)
        {
            if (ids == null || ids.Length == 0)
                return Json(new { success = false, message = "Chưa chọn đánh giá nào" });

            var reviews = await _db.BlogReviews
                .Where(r => ids.Contains(r.ReviewId))
                .ToListAsync();

            switch (action)
            {
                case "approve":
                    reviews.ForEach(r => { r.Status = "Approved"; r.UpdatedAt = DateTime.Now; });
                    break;
                case "reject":
                    reviews.ForEach(r => { r.Status = "Rejected"; r.UpdatedAt = DateTime.Now; });
                    break;
                case "spam":
                    reviews.ForEach(r => { r.Status = "Spam"; r.UpdatedAt = DateTime.Now; });
                    break;
                case "delete":
                    _db.BlogReviews.RemoveRange(reviews);
                    await _db.SaveChangesAsync();
                    return Json(new { success = true, message = $"Đã xóa {ids.Length} đánh giá" });
                default:
                    return Json(new { success = false, message = "Hành động không hợp lệ" });
            }

            await _db.SaveChangesAsync();
            return Json(new { success = true, message = $"Đã cập nhật {ids.Length} đánh giá" });
        }

        // ── GET /AdminBlogReview/Stats ─────────────────────────
        public async Task<IActionResult> Stats()
        {
            var byRating = await _db.BlogReviews
                .Where(r => r.Status == "Approved")
                .GroupBy(r => r.Rating)
                .Select(g => new { Rating = g.Key, Count = g.Count() })
                .ToListAsync();

            var byDay = await _db.BlogReviews
                .Where(r => r.CreatedAt >= DateTime.Now.AddDays(-30))
                .GroupBy(r => r.CreatedAt.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .OrderBy(g => g.Date)
                .ToListAsync();

            ViewBag.ByRating = byRating;
            ViewBag.ByDay = byDay;
            ViewBag.Average = await _db.BlogReviews
                .Where(r => r.Status == "Approved")
                .AverageAsync(r => (double?)r.Rating) ?? 0;

            return View();
        }
    }

    // Extension method kiểm tra AJAX request
    public static class HttpRequestExtensions
    {
        public static bool IsAjax(this HttpRequest request)
            => request.Headers["X-Requested-With"] == "XMLHttpRequest";
    }
}
