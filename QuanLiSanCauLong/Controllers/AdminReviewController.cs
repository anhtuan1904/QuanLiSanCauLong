// ═══════════════════════════════════════════════════════════════════
//  FILE: Controllers/AdminReviewController.cs
//  Route: /Admin/Review/[action]
// ═══════════════════════════════════════════════════════════════════
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiSanCauLong.Data;
using QuanLiSanCauLong.Models;
using QuanLiSanCauLong.ViewModels;

namespace QuanLiSanCauLong.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("Admin/Review/[action]/{id?}")]
    public class AdminReviewController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private const int PAGE_SIZE = 15;

        public AdminReviewController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ══════════════════════════════════════════════════════════
        //  INDEX — trang quản lý chính
        // ══════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Index(ReviewFilterViewModel filter)
        {
            var query = _context.CourtReviews
                .Include(r => r.User)
                .Include(r => r.Court).ThenInclude(c => c!.Facility)
                .Include(r => r.Images)
                .Include(r => r.Likes)
                .Include(r => r.Replies).ThenInclude(rp => rp.User)
                .AsQueryable();

            // ── Filters ──
            if (filter.FacilityId.HasValue)
                query = query.Where(r => r.Court!.FacilityId == filter.FacilityId.Value);

            if (filter.CourtId.HasValue)
                query = query.Where(r => r.CourtId == filter.CourtId.Value);

            if (!string.IsNullOrEmpty(filter.Status) && filter.Status != "All")
                query = query.Where(r => r.Status == filter.Status);

            if (filter.Rating.HasValue)
                query = query.Where(r => r.Rating == filter.Rating.Value);

            if (!string.IsNullOrEmpty(filter.Search))
            {
                var s = filter.Search.ToLower();
                query = query.Where(r =>
                    (r.Content != null && r.Content.ToLower().Contains(s)) ||
                    r.User!.FullName.ToLower().Contains(s) ||
                    r.Court!.CourtNumber.ToLower().Contains(s));
            }

            if (filter.IsVerified == true) query = query.Where(r => r.IsVerifiedBooking);
            if (filter.HasImages == true) query = query.Where(r => r.Images.Any());
            if (filter.IsFeatured == true) query = query.Where(r => r.IsFeatured);

            if (filter.DateRange == "today")
                query = query.Where(r => r.CreatedAt.Date == DateTime.Today);
            else if (filter.DateRange == "week")
                query = query.Where(r => r.CreatedAt >= DateTime.Today.AddDays(-7));
            else if (filter.DateRange == "month")
                query = query.Where(r => r.CreatedAt >= DateTime.Today.AddMonths(-1));

            // ── Sort ──
            query = filter.SortBy switch
            {
                "oldest" => query.OrderBy(r => r.CreatedAt),
                "rating_high" => query.OrderByDescending(r => r.Rating),
                "rating_low" => query.OrderBy(r => r.Rating),
                "most_liked" => query.OrderByDescending(r => r.Likes.Count),
                _ => query.OrderByDescending(r => r.CreatedAt)
            };

            var total = await query.CountAsync();

            var page = Math.Max(1, filter.Page);
            var reviews = await query
                .Skip((page - 1) * PAGE_SIZE)
                .Take(PAGE_SIZE)
                .ToListAsync();

            // ── Stats (toàn bộ, không lọc) ──
            var allReviews = await _context.CourtReviews
                .Include(r => r.Images)
                .ToListAsync();

            var stats = new ReviewStatsViewModel
            {
                Total = allReviews.Count,
                Pending = allReviews.Count(r => r.Status == "Pending"),
                Approved = allReviews.Count(r => r.Status == "Approved"),
                Rejected = allReviews.Count(r => r.Status == "Rejected"),
                Hidden = allReviews.Count(r => r.Status == "Hidden"),
                AverageRating = allReviews.Any() ? Math.Round(allReviews.Average(r => r.Rating), 1) : 0,
                FiveStars = allReviews.Count(r => r.Rating == 5),
                FourStars = allReviews.Count(r => r.Rating == 4),
                ThreeStars = allReviews.Count(r => r.Rating == 3),
                TwoStars = allReviews.Count(r => r.Rating == 2),
                OneStar = allReviews.Count(r => r.Rating == 1),
                WithImages = allReviews.Count(r => r.Images.Any()),
                Verified = allReviews.Count(r => r.IsVerifiedBooking),
                Featured = allReviews.Count(r => r.IsFeatured),
                TodayCount = allReviews.Count(r => r.CreatedAt.Date == DateTime.Today),
                ThisWeekCount = allReviews.Count(r => r.CreatedAt >= DateTime.Today.AddDays(-7)),
            };

            var vm = new AdminReviewIndexViewModel
            {
                Reviews = reviews.Select(MapToCard).ToList(),
                Stats = stats,
                Filter = filter,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)total / PAGE_SIZE),
                TotalCount = total,
                PageSize = PAGE_SIZE,
                Facilities = await _context.Facilities.Where(f => f.IsActive)
                    .Select(f => new FacilitySelectItem(f.FacilityId, f.FacilityName)).ToListAsync(),
                Courts = await _context.Courts.Include(c => c.Facility)
                    .Select(c => new CourtSelectItem(c.CourtId, c.CourtNumber, c.Facility!.FacilityName)).ToListAsync(),
            };

            return View(vm);
        }

        // ══════════════════════════════════════════════════════════
        //  DETAIL — modal chi tiết + ảnh + replies
        // ══════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            var review = await _context.CourtReviews
                .Include(r => r.User)
                .Include(r => r.Court).ThenInclude(c => c!.Facility)
                .Include(r => r.Images)
                .Include(r => r.Likes)
                .Include(r => r.Replies)
                    .ThenInclude(rp => rp.User)
                .Include(r => r.Replies)
                    .ThenInclude(rp => rp.ChildReplies).ThenInclude(cr => cr.User)
                .FirstOrDefaultAsync(r => r.ReviewId == id);

            if (review == null) return NotFound();

            var vm = new ReviewDetailViewModel
            {
                Review = MapToCard(review),
                Images = review.Images.OrderBy(i => i.DisplayOrder).Select(i => new ReviewImageViewModel
                {
                    ImageId = i.ImageId,
                    ImagePath = i.ImagePath,
                    DisplayOrder = i.DisplayOrder
                }).ToList(),
                Replies = BuildReplyTree(review.Replies.ToList()),
                LikeCount = review.Likes.Count(l => l.LikeType == "Like"),
                HelpfulCount = review.Likes.Count(l => l.LikeType == "Helpful"),
            };

            return PartialView("_ReviewDetail", vm);
        }

        // ══════════════════════════════════════════════════════════
        //  MODERATE — Approve / Reject / Hide / Feature
        // ══════════════════════════════════════════════════════════
        [HttpPost]
        public async Task<IActionResult> Moderate([FromBody] AdminModerateRequest req)
        {
            var review = await _context.CourtReviews.FindAsync(req.ReviewId);
            if (review == null)
                return Json(new { success = false, message = "Không tìm thấy đánh giá!" });

            var adminId = GetCurrentUserId();

            switch (req.Action)
            {
                case "Approve":
                    review.Status = "Approved";
                    review.ApprovedAt = DateTime.Now;
                    review.ApprovedByUserId = adminId;
                    review.RejectionReason = null;
                    break;

                case "Reject":
                    review.Status = "Rejected";
                    review.RejectionReason = req.RejectionReason ?? "Vi phạm quy định";
                    break;

                case "Hide":
                    review.Status = "Hidden";
                    break;

                case "Restore":
                    review.Status = "Pending";
                    break;

                case "Feature":
                    review.IsFeatured = true;
                    break;

                case "Unfeature":
                    review.IsFeatured = false;
                    break;

                default:
                    return Json(new { success = false, message = "Hành động không hợp lệ!" });
            }

            review.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = $"Đã {ActionLabel(req.Action)} đánh giá!",
                newStatus = review.Status,
                reviewId = review.ReviewId
            });
        }

        // ══════════════════════════════════════════════════════════
        //  BULK MODERATE
        // ══════════════════════════════════════════════════════════
        [HttpPost]
        public async Task<IActionResult> BulkModerate([FromBody] BulkModerateRequest req)
        {
            if (req.ReviewIds == null || !req.ReviewIds.Any())
                return Json(new { success = false, message = "Không có đánh giá nào được chọn!" });

            var reviews = await _context.CourtReviews
                .Where(r => req.ReviewIds.Contains(r.ReviewId))
                .ToListAsync();

            var adminId = GetCurrentUserId();

            foreach (var r in reviews)
            {
                switch (req.Action)
                {
                    case "Approve": r.Status = "Approved"; r.ApprovedAt = DateTime.Now; r.ApprovedByUserId = adminId; break;
                    case "Reject": r.Status = "Rejected"; r.RejectionReason = "Vi phạm quy định (bulk)"; break;
                    case "Hide": r.Status = "Hidden"; break;
                    case "Delete": _context.CourtReviews.Remove(r); break;
                }
                r.UpdatedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = $"Đã xử lý {reviews.Count} đánh giá!" });
        }

        // ══════════════════════════════════════════════════════════
        //  REPLY — admin trả lời đánh giá
        // ══════════════════════════════════════════════════════════
        [HttpPost]
        public async Task<IActionResult> Reply([FromBody] AdminReplyRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Content))
                return Json(new { success = false, message = "Nội dung không được để trống!" });

            var review = await _context.CourtReviews.FindAsync(req.ReviewId);
            if (review == null)
                return Json(new { success = false, message = "Không tìm thấy đánh giá!" });

            var adminId = GetCurrentUserId();
            var admin = await _context.Users.FindAsync(adminId);

            var reply = new ReviewReply
            {
                ReviewId = req.ReviewId,
                UserId = adminId,
                ParentReplyId = req.ParentReplyId,
                Content = req.Content.Trim(),
                AuthorRole = "Admin",
                IsAdminReply = true,
                CreatedAt = DateTime.Now
            };

            _context.ReviewReplies.Add(reply);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = "Đã gửi phản hồi!",
                replyId = reply.ReplyId,
                authorName = admin?.FullName ?? "Admin",
                content = reply.Content,
                createdAt = reply.CreatedAt.ToString("dd/MM/yyyy HH:mm")
            });
        }

        // ══════════════════════════════════════════════════════════
        //  DELETE REPLY
        // ══════════════════════════════════════════════════════════
        [HttpPost]
        public async Task<IActionResult> DeleteReply(int id)
        {
            var reply = await _context.ReviewReplies.FindAsync(id);
            if (reply == null) return Json(new { success = false, message = "Không tìm thấy!" });
            _context.ReviewReplies.Remove(reply);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã xóa phản hồi!" });
        }

        // ══════════════════════════════════════════════════════════
        //  DELETE REVIEW
        // ══════════════════════════════════════════════════════════
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var review = await _context.CourtReviews
                .Include(r => r.Images)
                .Include(r => r.Replies)
                .Include(r => r.Likes)
                .FirstOrDefaultAsync(r => r.ReviewId == id);

            if (review == null) return Json(new { success = false, message = "Không tìm thấy đánh giá!" });

            // Xóa ảnh vật lý
            foreach (var img in review.Images)
                DeleteFile(img.ImagePath);

            _context.CourtReviews.Remove(review);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã xóa đánh giá!" });
        }

        // ══════════════════════════════════════════════════════════
        //  DELETE IMAGE (ảnh đính kèm)
        // ══════════════════════════════════════════════════════════
        [HttpPost]
        public async Task<IActionResult> DeleteImage(int id)
        {
            var img = await _context.ReviewImages.FindAsync(id);
            if (img == null) return Json(new { success = false, message = "Không tìm thấy ảnh!" });
            DeleteFile(img.ImagePath);
            _context.ReviewImages.Remove(img);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // ══════════════════════════════════════════════════════════
        //  GET COURTS BY FACILITY (AJAX)
        // ══════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> GetCourtsByFacility(int facilityId)
        {
            var courts = await _context.Courts
                .Where(c => c.FacilityId == facilityId)
                .Select(c => new { c.CourtId, c.CourtNumber })
                .ToListAsync();
            return Json(courts);
        }

        // ══════════════════════════════════════════════════════════
        //  PRIVATE HELPERS
        // ══════════════════════════════════════════════════════════
        private static ReviewCardViewModel MapToCard(CourtReview r)
        {
            var adminReply = r.Replies
                .Where(rp => rp.IsAdminReply && rp.ParentReplyId == null)
                .OrderByDescending(rp => rp.CreatedAt)
                .FirstOrDefault();

            return new ReviewCardViewModel
            {
                ReviewId = r.ReviewId,
                CourtId = r.CourtId,
                CourtName = r.Court?.CourtNumber ?? $"Sân #{r.CourtId}",
                FacilityName = r.Court?.Facility?.FacilityName ?? "—",
                UserId = r.UserId,
                UserName = r.User?.FullName ?? $"User #{r.UserId}",
                UserAvatar = r.User?.AvatarUrl ?? "/images/default-avatar.jpg",
                Rating = r.Rating,
                Content = r.Content,
                Status = r.Status,
                IsVerifiedBooking = r.IsVerifiedBooking,
                IsFeatured = r.IsFeatured,
                LikeCount = r.Likes?.Count ?? 0,
                ReplyCount = r.Replies?.Count ?? 0,
                ImageCount = r.Images?.Count ?? 0,
                CreatedAt = r.CreatedAt,
                ApprovedAt = r.ApprovedAt,
                RejectionReason = r.RejectionReason,
                RatingCleanliness = r.RatingCleanliness,
                RatingCourtQuality = r.RatingCourtQuality,
                RatingService = r.RatingService,
                RatingValue = r.RatingValue,
                ThumbnailUrl = r.Images?.OrderBy(i => i.DisplayOrder).FirstOrDefault()?.ImagePath,
                AdminReply = adminReply == null ? null : new ReviewReplyViewModel
                {
                    ReplyId = adminReply.ReplyId,
                    Content = adminReply.Content,
                    UserName = adminReply.User?.FullName ?? "Admin",
                    CreatedAt = adminReply.CreatedAt,
                    IsAdminReply = true
                }
            };
        }

        private static List<ReviewReplyViewModel> BuildReplyTree(List<ReviewReply> replies)
        {
            var map = replies.ToDictionary(r => r.ReplyId);
            var roots = new List<ReviewReplyViewModel>();

            foreach (var r in replies.OrderBy(r => r.CreatedAt))
            {
                var vm = new ReviewReplyViewModel
                {
                    ReplyId = r.ReplyId,
                    ReviewId = r.ReviewId,
                    UserId = r.UserId,
                    UserName = r.User?.FullName ?? $"User #{r.UserId}",
                    UserAvatar = r.User?.AvatarUrl ?? "/images/default-avatar.jpg",
                    AuthorRole = r.AuthorRole,
                    Content = r.Content,
                    IsAdminReply = r.IsAdminReply,
                    IsEdited = r.IsEdited,
                    CreatedAt = r.CreatedAt,
                    ParentReplyId = r.ParentReplyId
                };

                if (r.ParentReplyId == null)
                    roots.Add(vm);
                else if (map.ContainsKey(r.ParentReplyId.Value))
                {
                    // tìm parent vm trong roots (shallow)
                    var parent = roots.FirstOrDefault(x => x.ReplyId == r.ParentReplyId.Value);
                    parent?.ChildReplies.Add(vm);
                }
            }

            return roots;
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out int id) ? id : 0;
        }

        private void DeleteFile(string? path)
        {
            if (string.IsNullOrEmpty(path)) return;
            var full = Path.Combine(_env.WebRootPath, path.TrimStart('/'));
            if (System.IO.File.Exists(full)) System.IO.File.Delete(full);
        }

        private static string ActionLabel(string action) => action switch
        {
            "Approve" => "duyệt",
            "Reject" => "từ chối",
            "Hide" => "ẩn",
            "Restore" => "khôi phục",
            "Feature" => "ghim",
            "Unfeature" => "bỏ ghim",
            _ => action.ToLower()
        };
    }
}
