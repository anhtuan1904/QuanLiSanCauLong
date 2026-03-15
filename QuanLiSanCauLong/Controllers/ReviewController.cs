// ═══════════════════════════════════════════════════════════════════
//  FILE: Controllers/ReviewController.cs
//  Sửa lỗi: ToHashSet async, using thiếu, tính lại rating cơ sở
// ═══════════════════════════════════════════════════════════════════
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiSanCauLong.Data;
using QuanLiSanCauLong.Models;
using QuanLiSanCauLong.ViewModels;
using System.Security.Claims;

namespace QuanLiSanCauLong.Controllers
{
    public class ReviewController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private const int PAGE_SIZE = 8;

        public ReviewController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ══════════════════════════════════════════════════════════
        //  HELPER: Tạo ReviewSectionViewModel cho một Facility
        //  Gọi từ FacilityController.Details()
        // ══════════════════════════════════════════════════════════
        public async Task<ReviewSectionViewModel> BuildSectionVm(int facilityId, int? currentUserId)
        {
            var allReviews = await _context.CourtReviews
                .Include(r => r.User)
                .Include(r => r.Court)
                .Include(r => r.Images)
                .Include(r => r.Likes)
                .Include(r => r.Replies).ThenInclude(rp => rp.User)
                .Where(r => r.Court!.FacilityId == facilityId && r.Status == "Approved")
                .OrderByDescending(r => r.IsFeatured)
                .ThenByDescending(r => r.CreatedAt)
                .ToListAsync();

            var vm = new ReviewSectionViewModel { FacilityId = facilityId };

            if (allReviews.Any())
            {
                vm.TotalCount = allReviews.Count;
                vm.AverageRating = Math.Round(allReviews.Average(r => r.Rating), 1);
                vm.FiveStars = allReviews.Count(r => r.Rating == 5);
                vm.FourStars = allReviews.Count(r => r.Rating == 4);
                vm.ThreeStars = allReviews.Count(r => r.Rating == 3);
                vm.TwoStars = allReviews.Count(r => r.Rating == 2);
                vm.OneStar = allReviews.Count(r => r.Rating == 1);
                vm.WithImages = allReviews.Count(r => r.Images.Any());
                vm.Verified = allReviews.Count(r => r.IsVerifiedBooking);

                var withSub = allReviews.Where(r => r.RatingCleanliness.HasValue).ToList();
                if (withSub.Any())
                {
                    vm.AvgCleanliness = Math.Round(withSub.Average(r => r.RatingCleanliness!.Value), 1);
                    vm.AvgCourtQuality = Math.Round(withSub.Average(r => r.RatingCourtQuality!.Value), 1);
                    vm.AvgService = Math.Round(withSub.Average(r => r.RatingService!.Value), 1);
                    vm.AvgValue = Math.Round(withSub.Average(r => r.RatingValue!.Value), 1);
                }
            }

            var firstPage = allReviews.Take(PAGE_SIZE).ToList();
            vm.Reviews = firstPage.Select(MapToReviewCard).ToList();
            vm.LoadedCount = firstPage.Count;
            vm.HasMore = allReviews.Count > PAGE_SIZE;

            if (currentUserId.HasValue)
            {
                var uid = currentUserId.Value;

                var existReview = allReviews.FirstOrDefault(r => r.UserId == uid);
                if (existReview != null)
                    vm.UserExistingReview = MapToReviewCard(existReview);

                var completedBookings = await _context.Bookings
                    .Include(b => b.Court)
                    .Where(b => b.UserId == uid
                             && b.Court.FacilityId == facilityId
                             && (b.Status == "Completed" || b.Status == "Playing"))
                    .ToListAsync();

                // FIX: ToHashSet() trên in-memory list — không cần async
                var reviewedBookingIds = allReviews
                    .Where(r => r.UserId == uid && r.BookingId.HasValue)
                    .Select(r => r.BookingId!.Value)
                    .ToHashSet();

                vm.BookedCourts = completedBookings
                    .Where(b => !reviewedBookingIds.Contains(b.BookingId))
                    .Select(b => new BookedCourtItem
                    {
                        CourtId = b.CourtId,
                        CourtNumber = b.Court?.CourtNumber ?? "",
                        BookingId = b.BookingId,
                        BookingDate = b.BookingDate
                    })
                    .ToList();

                vm.CanWriteReview = vm.BookedCourts.Any() && vm.UserExistingReview == null;

                // FIX: ToListAsync() rồi mới ToHashSet() — không có ToHashSetAsync trong EF
                var likedIds = await _context.ReviewLikes
                    .Where(l => l.UserId == uid
                             && allReviews.Select(r => r.ReviewId).Contains(l.ReviewId))
                    .Select(l => l.ReviewId)
                    .ToListAsync();                    // ← ToListAsync, không phải ToHashSet trực tiếp

                vm.MyLikedReviewIds = likedIds.ToHashSet();  // ← sau đó mới ToHashSet
            }

            return vm;
        }

        // ══════════════════════════════════════════════════════════
        //  TÍNH LẠI RATING CƠ SỞ — gọi sau mỗi review Approve/Submit
        //  Rating cơ sở = trung bình cộng TẤT CẢ review của các sân thuộc cơ sở
        // ══════════════════════════════════════════════════════════
        public async Task RecalculateFacilityRating(int facilityId)
        {
            // Lấy tất cả review đã duyệt của mọi sân trong cơ sở
            var approvedRatings = await _context.CourtReviews
                .Where(r => r.Court!.FacilityId == facilityId && r.Status == "Approved")
                .Select(r => (double)r.Rating)
                .ToListAsync();

            var facility = await _context.Facilities.FindAsync(facilityId);
            if (facility == null) return;

            if (approvedRatings.Any())
            {
                // Trung bình cộng, làm tròn 1 chữ số thập phân
                facility.AverageRating = Math.Round(approvedRatings.Average(), 1);
                facility.TotalReviews = approvedRatings.Count;
            }
            else
            {
                facility.AverageRating = null;
                facility.TotalReviews = 0;
            }

            await _context.SaveChangesAsync();
        }

        // ══════════════════════════════════════════════════════════
        //  POST: /Review/Submit
        // ══════════════════════════════════════════════════════════
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Submit(
            int facilityId, int courtId, int bookingId,
            int rating, string? content,
            int ratingCleanliness, int ratingCourtQuality, int ratingService, int ratingValue,
            List<IFormFile>? images)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Json(new { success = false, message = "Chưa đăng nhập!" });

            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.BookingId == bookingId
                                       && b.UserId == userId
                                       && b.CourtId == courtId);
            if (booking == null)
                return Json(new { success = false, message = "Không tìm thấy đơn đặt sân!" });

            var exists = await _context.CourtReviews.AnyAsync(r =>
                r.UserId == userId && r.BookingId == bookingId);
            if (exists)
                return Json(new { success = false, message = "Bạn đã đánh giá đơn đặt sân này rồi!" });

            var review = new CourtReview
            {
                CourtId = courtId,
                UserId = userId,
                BookingId = bookingId,
                Rating = Math.Clamp(rating, 1, 5),
                Content = content?.Trim(),
                RatingCleanliness = ratingCleanliness > 0 ? ratingCleanliness : null,
                RatingCourtQuality = ratingCourtQuality > 0 ? ratingCourtQuality : null,
                RatingService = ratingService > 0 ? ratingService : null,
                RatingValue = ratingValue > 0 ? ratingValue : null,
                IsVerifiedBooking = true,
                Status = "Pending",   // chờ admin duyệt
                CreatedAt = DateTime.Now
            };

            _context.CourtReviews.Add(review);
            await _context.SaveChangesAsync();

            // Lưu ảnh
            if (images?.Count > 0)
            {
                string folder = Path.Combine(_env.WebRootPath, "images", "reviews");
                Directory.CreateDirectory(folder);
                int order = 0;
                foreach (var img in images.Take(5))
                {
                    if (img.Length <= 0 || img.Length > 5 * 1024 * 1024) continue;
                    var ext = Path.GetExtension(img.FileName).ToLower();
                    if (ext != ".jpg" && ext != ".jpeg" && ext != ".png" && ext != ".webp") continue;
                    var fileName = $"{Guid.NewGuid()}{ext}";
                    using var stream = new FileStream(Path.Combine(folder, fileName), FileMode.Create);
                    await img.CopyToAsync(stream);
                    _context.ReviewImages.Add(new ReviewImage
                    {
                        ReviewId = review.ReviewId,
                        ImagePath = $"/images/reviews/{fileName}",
                        DisplayOrder = order++,
                        UploadedAt = DateTime.Now
                    });
                }
                await _context.SaveChangesAsync();
            }

            // KHÔNG tính lại rating ngay vì review còn Pending — admin duyệt xong mới tính

            return Json(new { success = true, message = "Đánh giá đã được ghi nhận! Chờ kiểm duyệt." });
        }

        // ══════════════════════════════════════════════════════════
        //  POST: /Review/Like
        // ══════════════════════════════════════════════════════════
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Like([FromBody] LikeRequest req)
        {
            var userId = GetCurrentUserId();

            // Tìm bất kỳ tương tác nào của User này trên Review này (không phân biệt Type)
            // Để nếu họ đổi từ Like sang Dislike thì mình cập nhật, thay vì thêm mới
            var existing = await _context.ReviewLikes.FirstOrDefaultAsync(l =>
                l.ReviewId == req.ReviewId && l.UserId == userId);

            bool isLiked = false;

            if (existing != null)
            {
                // Nếu bấm đúng cái Type cũ -> Hủy (Unlike)
                if (existing.LikeType.ToString() == req.LikeType.ToString())
                {
                    _context.ReviewLikes.Remove(existing);
                    isLiked = false;
                }
                else
                {
                    // Nếu bấm Type khác (đang Like mà bấm Dislike) -> Cập nhật Type
                    existing.LikeType = req.LikeType.ToString(); // Đảm bảo gán đúng kiểu
                    existing.CreatedAt = DateTime.Now;
                    isLiked = true;
                }
            }
            else
            {
                // Thêm mới
                _context.ReviewLikes.Add(new ReviewLike
                {
                    ReviewId = req.ReviewId,
                    UserId = userId,
                    LikeType = req.LikeType.ToString(),
                    CreatedAt = DateTime.Now
                });
                isLiked = true;
            }

            await _context.SaveChangesAsync();

            // Đếm lại
            int newCount = await _context.ReviewLikes.CountAsync(l =>
                l.ReviewId == req.ReviewId && l.LikeType == req.LikeType.ToString());

            return Json(new { success = true, isLiked, newCount });
        }

        // ══════════════════════════════════════════════════════════
        //  POST: /Review/Reply
        // ══════════════════════════════════════════════════════════
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Reply([FromBody] AdminReplyRequest req)
        {
            var userId = GetCurrentUserId();
            var user = await _context.Users.FindAsync(userId);
            if (string.IsNullOrWhiteSpace(req.Content))
                return Json(new { success = false, message = "Nội dung không được để trống!" });

            _context.ReviewReplies.Add(new ReviewReply
            {
                ReviewId = req.ReviewId,
                UserId = userId,
                ParentReplyId = req.ParentReplyId,
                Content = req.Content.Trim(),
                AuthorRole = "Customer",
                CreatedAt = DateTime.Now
            });
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                userName = user?.FullName ?? "Bạn",
                userAvatar = user?.AvatarUrl ?? "/images/default-avatar.jpg"
            });
        }

        // ══════════════════════════════════════════════════════════
        //  GET: /Review/LoadMore
        // ══════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> LoadMore(int facilityId, int offset = 0, int limit = 5)
        {
            var userId = GetCurrentUserId();

            var reviews = await _context.CourtReviews
                .Include(r => r.User)
                .Include(r => r.Court)
                .Include(r => r.Images)
                .Include(r => r.Likes)
                .Include(r => r.Replies).ThenInclude(rp => rp.User)
                .Where(r => r.Court!.FacilityId == facilityId && r.Status == "Approved")
                .OrderByDescending(r => r.IsFeatured)
                .ThenByDescending(r => r.CreatedAt)
                .Skip(offset).Take(limit)
                .ToListAsync();

            // FIX: ToListAsync() trước rồi ToHashSet() sau
            var myLikes = userId > 0
                ? (await _context.ReviewLikes.Where(l => l.UserId == userId).Select(l => l.ReviewId).ToListAsync()).ToHashSet()
                : new HashSet<int>();

            var total = await _context.CourtReviews
                .CountAsync(r => r.Court!.FacilityId == facilityId && r.Status == "Approved");

            var html = string.Join("", reviews.Select(r => RenderReviewCard(r, myLikes, userId)));

            return Json(new { success = true, html, hasMore = (offset + limit) < total });
        }

        // ══════════════════════════════════════════════════════════
        //  PRIVATE HELPERS
        // ══════════════════════════════════════════════════════════
        private static ReviewCardViewModel MapToReviewCard(CourtReview r)
        {
            var adminReply = r.Replies?
                .Where(rp => rp.IsAdminReply && rp.ParentReplyId == null)
                .OrderByDescending(rp => rp.CreatedAt)
                .FirstOrDefault();

            return new ReviewCardViewModel
            {
                ReviewId = r.ReviewId,
                CourtId = r.CourtId,
                CourtName = r.Court?.CourtNumber ?? "",
                FacilityName = r.Court?.Facility?.FacilityName ?? "",
                UserId = r.UserId,
                UserName = r.User?.FullName ?? $"Người dùng #{r.UserId}",
                UserAvatar = r.User?.AvatarUrl ?? "/images/default-avatar.jpg",
                Rating = r.Rating,
                Content = r.Content,
                Status = r.Status,
                IsVerifiedBooking = r.IsVerifiedBooking,
                IsFeatured = r.IsFeatured,
                LikeCount = r.Likes?.Count(l => l.LikeType == "Like") ?? 0,
                ReplyCount = r.Replies?.Count ?? 0,
                ImageCount = r.Images?.Count ?? 0,
                CreatedAt = r.CreatedAt,
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

        private string RenderReviewCard(CourtReview r, HashSet<int> myLikes, int userId)
        {
            var vm = MapToReviewCard(r);
            var liked = myLikes.Contains(r.ReviewId);
            var stars = string.Concat(Enumerable.Range(1, 5).Select(s =>
                $"<i class=\"bi bi-star-fill rv-star {(s <= vm.Rating ? "on" : "off")}\"></i>"));
            var verified = vm.IsVerifiedBooking
                ? "<span class=\"rv-verified\"><i class=\"bi bi-patch-check-fill\"></i>Đã chơi</span>" : "";
            var content = !string.IsNullOrEmpty(vm.Content)
                ? $"<div class=\"rv-card-body\"><div class=\"rv-content\">{System.Web.HttpUtility.HtmlEncode(vm.Content)}</div></div>" : "";
            var adminR = vm.AdminReply != null
                ? $"<div class=\"rv-admin-reply\"><div class=\"rv-ar-hd\"><span class=\"rv-ar-badge\"><i class=\"bi bi-shield-check me-1\"></i>Phản hồi từ ban quản lý</span></div><div class=\"rv-ar-content\">{System.Web.HttpUtility.HtmlEncode(vm.AdminReply.Content)}</div></div>" : "";

            return $@"<div class='rv-card' data-rating='{vm.Rating}' data-has-img='{(vm.ImageCount > 0 ? "1" : "0")}' data-verified='{(vm.IsVerifiedBooking ? "1" : "0")}' data-helpful='{vm.LikeCount}'>
              <div class='rv-card-hd'><img src='{vm.UserAvatar}' class='rv-avatar' onerror=""this.src='/images/default-avatar.jpg'"" />
              <div class='rv-author'><div class='rv-author-name'>{System.Web.HttpUtility.HtmlEncode(vm.UserName)}{verified}</div>
              <div class='rv-date'>{vm.CreatedAt:dd/MM/yyyy}</div><div class='rv-stars-row'>{stars}</div></div></div>
              {content}<div class='rv-card-ft'><button class='rv-like-btn {(liked ? "liked" : "")}' onclick=""likeReview({vm.ReviewId},'Like',this)""><i class='bi bi-hand-thumbs-up{(liked ? "-fill" : "")}'></i> Hữu ích <span class='like-cnt'>{vm.LikeCount}</span></button></div>
              {adminR}</div>";
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out int id) ? id : 0;
        }
    }
}
