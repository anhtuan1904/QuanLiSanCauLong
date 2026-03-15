using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiSanCauLong.Data;
using QuanLiSanCauLong.Models;

namespace QuanLiSanCauLong.Controllers
{
    public class CourtController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CourtController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Court/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var court = await _context.Courts
                .Include(c => c.Facility)
                    .ThenInclude(f => f.FacilityImages)   // ảnh gallery cơ sở
                .Include(c => c.PriceSlots)                // bảng giá khung giờ
                .Include(c => c.CourtReviews
                    .Where(r => r.Status == "Approved"))   // đánh giá đã duyệt
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(c => c.CourtId == id);

            if (court == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy sân!";
                ViewBag.Court = null;
                return View();
            }

            // ── Danh sách ảnh ────────────────────────────────────────────
            // Ưu tiên ảnh riêng của sân; fallback sang ảnh gallery cơ sở
            var images = new List<string>();
            if (!string.IsNullOrEmpty(court.ImagePath))
                images.Add(court.ImagePath);

            var facilityImgs = court.Facility?.FacilityImages?
                .OrderBy(i => i.DisplayOrder)
                .Select(i => i.ImagePath)
                .Where(p => !images.Contains(p))
                .ToList() ?? new List<string>();
            images.AddRange(facilityImgs);

            // Nếu vẫn không có ảnh, dùng ImageUrl của cơ sở làm fallback
            if (!images.Any() && !string.IsNullOrEmpty(court.Facility?.ImageUrl))
                images.Add(court.Facility.ImageUrl);

            // ── Review summary ────────────────────────────────────────────
            var approvedReviews = court.CourtReviews?.ToList() ?? new List<CourtReview>();
            double avgRating = approvedReviews.Any() ? Math.Round(approvedReviews.Average(r => (double)r.Rating), 1) : 0;
            int reviewCount = approvedReviews.Count;

            // ── Tổng quan cơ sở: số sân + rating toàn cơ sở ─────────────
            int facilityTotalCourts = 0;
            double facilityAvgRating = 0;
            int facilityReviewCount = 0;
            if (court.FacilityId > 0)
            {
                facilityTotalCourts = await _context.Courts
                    .CountAsync(c => c.FacilityId == court.FacilityId);

                var facRatingData = await _context.CourtReviews
                    .Where(r => r.Status == "Approved" && r.Court != null && r.Court.FacilityId == court.FacilityId)
                    .GroupBy(r => r.Court!.FacilityId)
                    .Select(g => new { Avg = g.Average(r => (double)r.Rating), Count = g.Count() })
                    .FirstOrDefaultAsync();

                if (facRatingData != null)
                {
                    facilityAvgRating = Math.Round(facRatingData.Avg, 1);
                    facilityReviewCount = facRatingData.Count;
                }
            }

            // ── Truyền ViewBag ────────────────────────────────────────────
            ViewBag.Court = court;
            ViewBag.CourtId = court.CourtId;
            ViewBag.CourtImages = images;
            ViewBag.PriceSlots = court.PriceSlots?
                                       .Where(p => p.IsActive)
                                       .OrderBy(p => p.StartTime)
                                       .ToList() ?? new List<PriceSlot>();
            ViewBag.Reviews = approvedReviews
                                       .OrderByDescending(r => r.CreatedAt)
                                       .Take(10)
                                       .ToList();
            ViewBag.AverageRating = avgRating;
            ViewBag.ReviewCount = reviewCount;
            ViewBag.FacilityTotalCourts = facilityTotalCourts;
            ViewBag.FacilityAvgRating = facilityAvgRating;
            ViewBag.FacilityReviewCount = facilityReviewCount;

            return View();
        }
    }
}
