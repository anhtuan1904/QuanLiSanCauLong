using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiSanCauLong.Data;
using QuanLiSanCauLong.ViewModels;

namespace QuanLiSanCauLong.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminDashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        private async Task<AdminDashboardViewModel> BuildDashboardData(DateTime from, DateTime to)
        {
            var model = new AdminDashboardViewModel { FromDate = from, ToDate = to };

            var bookings = await _context.Bookings
                .Include(b => b.Court).ThenInclude(c => c.Facility)
                .Include(b => b.User)
                .Where(b => b.BookingDate >= from && b.BookingDate <= to)
                .ToListAsync();

            var orders = await _context.Orders
                .Include(o => o.OrderDetails).ThenInclude(od => od.Product)
                .Include(o => o.Facility)
                .Where(o => o.CreatedAt.Date >= from && o.CreatedAt.Date <= to)
                .ToListAsync();

            model.TotalBookings = bookings.Count;
            model.CompletedBookings = bookings.Count(b => b.Status == "Completed");
            model.CancelledBookings = bookings.Count(b => b.Status == "Cancelled");
            model.PendingBookings = bookings.Count(b => b.Status == "Pending");
            model.BookingRevenue = bookings.Where(b => b.Status != "Cancelled").Sum(b => b.TotalPrice);
            model.ProductRevenue = orders.Where(o => o.OrderStatus == "Completed").Sum(o => o.TotalAmount);
            model.TotalRevenue = model.BookingRevenue + model.ProductRevenue;
            model.TotalOrders = orders.Count;
            model.TotalCustomers = await _context.Users.CountAsync(u => u.Role == "Customer" && u.IsActive);
            model.NewCustomers = await _context.Users.CountAsync(u => u.Role == "Customer" && u.CreatedAt.Date >= from);
            model.TotalFacilities = await _context.Facilities.CountAsync(f => f.IsActive);
            model.TotalCourts = await _context.Courts.CountAsync(c => c.Status == "Available");
            model.LowStockCount = await _context.Inventories.CountAsync(i => i.Quantity <= i.MinQuantity && i.MinQuantity > 0);

            var allRatingData = await _context.CourtReviews
                .Where(r => r.Status == "Approved")
                .GroupBy(r => 1)
                .Select(g => new { Avg = g.Average(r => (double)r.Rating), Count = g.Count() })
                .FirstOrDefaultAsync();
            model.OverallAverageRating = allRatingData?.Avg ?? 0;
            model.OverallReviewCount = allRatingData?.Count ?? 0;

            var bookingIdsInRange = bookings.Select(b => b.BookingId).ToList();
            var orderIdsInRange = orders.Select(o => o.OrderId).ToList();

            model.VouchersUsed = await _context.VoucherUsages
                .CountAsync(vu =>
                    (vu.BookingId != null && bookingIdsInRange.Contains(vu.BookingId.Value)) ||
                    (vu.OrderId != null && orderIdsInRange.Contains(vu.OrderId.Value)));

            model.VoucherDiscount = await _context.VoucherUsages
                .Where(vu =>
                    (vu.BookingId != null && bookingIdsInRange.Contains(vu.BookingId.Value)) ||
                    (vu.OrderId != null && orderIdsInRange.Contains(vu.OrderId.Value)))
                .SumAsync(vu => (decimal?)vu.DiscountAmount) ?? 0;

            var now = DateTime.Now;
            var todayDate = now.Date;
            var nowTime = now.TimeOfDay;

            var liveBookings = await _context.Bookings
                .Include(b => b.Court).Include(b => b.User)
                .Where(b => b.BookingDate.Date == todayDate
                         && b.StartTime <= nowTime && b.EndTime > nowTime
                         && b.Status != "Cancelled")
                .ToListAsync();

            var upcomingToday = await _context.Bookings
                .Include(b => b.Court).Include(b => b.User)
                .Where(b => b.BookingDate.Date == todayDate
                         && b.StartTime > nowTime && b.Status != "Cancelled")
                .ToListAsync();

            var liveCids = liveBookings.Select(b => b.CourtId).ToHashSet();
            var upcomingCids = upcomingToday.Select(b => b.CourtId).ToHashSet();
            var maintCids = (await _context.Courts.Where(c => c.Status == "Maintenance")
                                   .Select(c => c.CourtId).ToListAsync()).ToHashSet();

            model.LiveCourtStatuses = await _context.Courts
                .Include(c => c.Facility)
                .Where(c => c.Facility != null && c.Facility.IsActive)
                .OrderBy(c => c.CourtNumber)
                .Take(12)
                .Select(c => new LiveCourtStatusViewModel
                {
                    CourtId = c.CourtId,
                    CourtNumber = c.CourtNumber,
                    FacilityName = c.Facility != null ? c.Facility.FacilityName : "",
                    Status = c.Status
                })
                .ToListAsync();

            foreach (var cs in model.LiveCourtStatuses)
            {
                if (maintCids.Contains(cs.CourtId))
                {
                    cs.LiveStatus = "Maintenance";
                }
                else if (liveCids.Contains(cs.CourtId))
                {
                    var b = liveBookings.First(x => x.CourtId == cs.CourtId);
                    cs.LiveStatus = "Occupied";
                    cs.CurrentPlayer = b.User?.FullName ?? "Khách";
                    cs.RemainingMinutes = (int)(b.EndTime - nowTime).TotalMinutes;
                }
                else if (upcomingCids.Contains(cs.CourtId))
                {
                    var b = upcomingToday.Where(x => x.CourtId == cs.CourtId)
                                         .OrderBy(x => x.StartTime).First();
                    cs.LiveStatus = "Reserved";
                    cs.NextPlayer = b.User?.FullName ?? "Khách";
                    cs.NextBookingTime = $"{b.StartTime:hh\\:mm} – {b.EndTime:hh\\:mm}";
                }
                else cs.LiveStatus = "Available";
            }

            model.CourtsAvailable = model.LiveCourtStatuses.Count(c => c.LiveStatus == "Available");
            model.CourtsOccupied = model.LiveCourtStatuses.Count(c => c.LiveStatus == "Occupied");
            model.CourtsReserved = model.LiveCourtStatuses.Count(c => c.LiveStatus == "Reserved");
            model.CourtsOnMaintenance = model.LiveCourtStatuses.Count(c => c.LiveStatus == "Maintenance");

            var totalLive = model.LiveCourtStatuses.Count;
            model.OccupancyRate = totalLive > 0
                ? Math.Round((double)(model.CourtsOccupied + model.CourtsReserved) / totalLive * 100, 1) : 0;

            model.BookingRevenuePercent = model.TotalRevenue > 0
                ? Math.Round((double)(model.BookingRevenue / model.TotalRevenue) * 100, 1) : 0;
            model.ProductRevenuePercent = model.TotalRevenue > 0
                ? Math.Round((double)(model.ProductRevenue / model.TotalRevenue) * 100, 1) : 0;

            var recentActs = new List<ActivityFeedItem>();
            var feedBookings = await _context.Bookings
                .Include(b => b.User).Include(b => b.Court)
                .Where(b => b.User != null)
                .OrderByDescending(b => b.CreatedAt).Take(4).ToListAsync();
            recentActs.AddRange(feedBookings.Select(b => new ActivityFeedItem
            {
                ActorName = b.User!.FullName ?? b.User.Email ?? "Khách",
                Description = $"Đặt sân {b.Court?.CourtNumber} – {b.StartTime:hh\\:mm}",
                CreatedAt = b.CreatedAt,
                Type = "Booking",
                TypeClass = "p-pl"
            }));

            var feedOrders = await _context.Orders
                .Include(o => o.User)
                .Where(o => o.User != null)
                .OrderByDescending(o => o.CreatedAt).Take(3).ToListAsync();
            recentActs.AddRange(feedOrders.Select(o => new ActivityFeedItem
            {
                ActorName = o.User!.FullName ?? o.User.Email ?? "Khách",
                Description = $"Đặt hàng ₫{o.TotalAmount:N0}",
                CreatedAt = o.CreatedAt,
                Type = "Đơn hàng",
                TypeClass = "p-ok"
            }));

            var feedReviews = await _context.CourtReviews
                .Include(r => r.User).Include(r => r.Court)
                .Where(r => r.User != null && r.Status == "Approved")
                .OrderByDescending(r => r.CreatedAt).Take(2).ToListAsync();
            recentActs.AddRange(feedReviews.Select(r => new ActivityFeedItem
            {
                ActorName = r.User!.FullName ?? r.User.Email ?? "Khách",
                Description = $"Đánh giá {r.Rating}★ sân {r.Court?.CourtNumber}",
                CreatedAt = r.CreatedAt,
                Type = "Review",
                TypeClass = "p-ok"
            }));

            model.ActivityFeed = recentActs.OrderByDescending(a => a.CreatedAt).Take(6).ToList();

            var heatmapFrom = DateTime.Today.AddDays(-28);
            var heatmapRaw = await _context.Bookings
                .Where(b => b.BookingDate >= heatmapFrom && b.Status != "Cancelled")
                .Select(b => new { Dow = (int)b.BookingDate.DayOfWeek, Hour = b.StartTime.Hours })
                .ToListAsync();

            model.HeatmapData = new int[7, 24];
            foreach (var h in heatmapRaw)
            {
                var dayIdx = h.Dow == 0 ? 6 : h.Dow - 1;
                var hourIdx = Math.Clamp(h.Hour, 0, 23);
                model.HeatmapData[dayIdx, hourIdx]++;
            }

            model.ForecastDays = new List<ForecastDayViewModel>();
            for (int i = 0; i < 7; i++)
            {
                var d = DateTime.Today.AddDays(i + 1);
                var bookedCount = await _context.Bookings.CountAsync(b => b.BookingDate.Date == d && b.Status != "Cancelled");
                var bookedRev = await _context.Bookings
                    .Where(b => b.BookingDate.Date == d && b.Status != "Cancelled")
                    .SumAsync(b => (decimal?)b.TotalPrice) ?? 0;
                model.ForecastDays.Add(new ForecastDayViewModel { Date = d, BookingCount = bookedCount, Revenue = bookedRev });
            }

            model.RevenueByDate = bookings
                .GroupBy(b => b.BookingDate.Date)
                .Select(g => new RevenueByDateViewModel
                {
                    Date = g.Key,
                    BookingRevenue = g.Where(b => b.Status != "Cancelled").Sum(b => b.TotalPrice),
                    ProductRevenue = orders.Where(o => o.CreatedAt.Date == g.Key && o.OrderStatus == "Completed").Sum(o => o.TotalAmount)
                })
                .OrderBy(r => r.Date).ToList();
            model.RevenueByDate.ForEach(r => r.TotalRevenue = r.BookingRevenue + r.ProductRevenue);

            model.RevenueByFacility = bookings
                .Where(b => b.Status != "Cancelled" && b.Court?.Facility != null)
                .GroupBy(b => new { b.Court.Facility.FacilityId, b.Court.Facility.FacilityName })
                .Select(g => new RevenueByFacilityViewModel
                {
                    FacilityName = g.Key.FacilityName,
                    BookingCount = g.Count(),
                    BookingRevenue = g.Sum(b => b.TotalPrice),
                    ProductRevenue = orders.Where(o => o.FacilityId == g.Key.FacilityId && o.OrderStatus == "Completed").Sum(o => o.TotalAmount)
                })
                .Select(r => { r.TotalRevenue = r.BookingRevenue + r.ProductRevenue; return r; })
                .OrderByDescending(r => r.TotalRevenue).ToList();

            model.PopularTimeSlots = bookings
                .Where(b => b.Status != "Cancelled")
                .GroupBy(b => b.StartTime.Hours)
                .Select(g => new PopularTimeSlotViewModel
                {
                    TimeSlot = $"{g.Key:D2}:00 – {g.Key + 1:D2}:00",
                    BookingCount = g.Count(),
                    Revenue = g.Sum(b => b.TotalPrice)
                })
                .OrderByDescending(t => t.BookingCount).Take(10).ToList();

            // ✅ FIX line 344: Xóa CategoryType → dùng BehaviorType
            model.TopProducts = await _context.OrderDetails
                .Include(od => od.Product).ThenInclude(p => p.Category)
                .Include(od => od.Order)
                .Where(od => od.Order.CreatedAt.Date >= from
                          && od.Order.CreatedAt.Date <= to
                          && od.Order.OrderStatus == "Completed"
                          && od.Product != null)
                .GroupBy(od => new
                {
                    od.Product.ProductId,
                    od.Product.ProductName,
                    // ✅ Đổi CategoryType → BehaviorType
                    BehaviorType = od.Product.Category != null ? od.Product.Category.BehaviorType : "Retail"
                })
                .Select(g => new TopProductViewModel
                {
                    ProductName = g.Key.ProductName,
                    CategoryType = g.Key.BehaviorType, // map vào property CategoryType của ViewModel (giữ tên VM cũ)
                    QuantitySold = g.Sum(od => od.Quantity),
                    Revenue = g.Sum(od => od.TotalPrice)
                })
                .OrderByDescending(p => p.Revenue)
                .Take(10)
                .ToListAsync();

            model.TopCustomers = bookings
                .Where(b => b.Status != "Cancelled" && b.User != null)
                .GroupBy(b => new { b.UserId, b.User.FullName, Email = b.User.Email ?? "", Phone = b.User.Phone ?? "" })
                .Select(g => new TopCustomerViewModel
                {
                    CustomerName = g.Key.FullName,
                    Email = g.Key.Email,
                    Phone = g.Key.Phone,
                    BookingCount = g.Count(),
                    TotalSpent = g.Sum(b => b.TotalPrice)
                })
                .OrderByDescending(c => c.TotalSpent).Take(10).ToList();

            model.RecentBookings = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Court).ThenInclude(c => c.Facility)
                .Where(b => b.User != null && b.Court != null)
                .OrderByDescending(b => b.CreatedAt)
                .Take(10)
                .Select(b => new RecentBookingViewModel
                {
                    BookingId = b.BookingId,
                    BookingCode = b.BookingCode,
                    CustomerName = b.User.FullName,
                    CourtNumber = b.Court.CourtNumber,
                    FacilityName = b.Court.Facility != null ? b.Court.Facility.FacilityName : "",
                    BookingDate = b.BookingDate,
                    StartTime = b.StartTime,
                    EndTime = b.EndTime,
                    TotalAmount = b.TotalPrice,
                    Status = b.Status,
                    PaymentStatus = b.PaymentStatus ?? ""
                })
                .ToListAsync();

            var facilityIds = bookings
                .Where(b => b.Court?.Facility != null)
                .Select(b => b.Court.Facility.FacilityId).Distinct().ToList();

            var reviewStats = await _context.CourtReviews
                .Include(r => r.Court)
                .Where(r => r.Court != null && facilityIds.Contains(r.Court.FacilityId))
                .GroupBy(r => r.Court.FacilityId)
                .Select(g => new { FacilityId = g.Key, AverageRating = g.Average(r => (double)r.Rating), ReviewCount = g.Count() })
                .ToListAsync();

            model.FacilityStats = bookings
                .Where(b => b.Court?.Facility != null)
                .GroupBy(b => b.Court.Facility)
                .Select(g =>
                {
                    var total = g.Count();
                    var completed = g.Count(b => b.Status == "Completed");
                    var review = reviewStats.FirstOrDefault(r => r.FacilityId == g.Key.FacilityId);
                    return new FacilityStatsViewModel
                    {
                        FacilityId = g.Key.FacilityId,
                        FacilityName = g.Key.FacilityName,
                        Address = g.Key.Address ?? "",
                        TotalBookings = total,
                        TotalRevenue = g.Where(b => b.Status != "Cancelled").Sum(b => b.TotalPrice),
                        OccupancyRate = total > 0 ? Math.Round((double)completed / total * 100, 1) : 0,
                        AverageRating = review?.AverageRating ?? 0,
                        ReviewCount = review?.ReviewCount ?? 0
                    };
                })
                .OrderByDescending(f => f.TotalRevenue).ToList();

            return model;
        }

        [HttpGet]
        public async Task<IActionResult> Index(DateTime? fromDate, DateTime? toDate)
        {
            var from = fromDate ?? DateTime.Today.AddDays(-30);
            var to = toDate ?? DateTime.Today;
            return View(await BuildDashboardData(from, to));
        }

        [HttpGet]
        public async Task<IActionResult> Analytics(DateTime? fromDate, DateTime? toDate)
        {
            var from = fromDate ?? DateTime.Today.AddDays(-30);
            var to = toDate ?? DateTime.Today;
            return View(await BuildDashboardData(from, to));
        }
    }
}
