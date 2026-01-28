using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiSanCauLong.Data;
using QuanLiSanCauLong.ViewModels;

namespace QuanLiSanCauLong.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminReportController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminReportController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ===================================
        // REVENUE REPORT (Báo cáo doanh thu)
        // ===================================

        [HttpGet]
        public async Task<IActionResult> Revenue(DateTime? fromDate, DateTime? toDate, int? facilityId)
        {
            fromDate ??= DateTime.Today.AddDays(-30);
            toDate ??= DateTime.Today;

            var bookings = await _context.Bookings
                .Include(b => b.Court.Facility)
                .Where(b => b.BookingDate >= fromDate && b.BookingDate <= toDate)
                .ToListAsync();

            var orders = await _context.Orders
                .Include(o => o.Facility)
                .Where(o => o.CreatedAt >= fromDate && o.CreatedAt <= toDate.Value.AddDays(1))
                .ToListAsync();

            if (facilityId.HasValue)
            {
                bookings = bookings.Where(b => b.Court.FacilityId == facilityId.Value).ToList();
                orders = orders.Where(o => o.FacilityId == facilityId.Value).ToList();
            }

            // Tính toán các chỉ số
            var bookingRevenue = bookings.Where(b => b.Status != "Cancelled").Sum(b => b.TotalPrice);
            var productRevenue = orders.Where(o => o.OrderStatus == "Completed").Sum(o => o.TotalAmount);
            var totalRevenue = bookingRevenue + productRevenue;

            var daysDiff = (toDate.Value - fromDate.Value).Days;
            var dailyAverage = daysDiff > 0 ? totalRevenue / daysDiff : 0;

            // Doanh thu theo ngày
            var revenueByDate = bookings
                .GroupBy(b => b.BookingDate)
                .Select(g => new
                {
                    Date = g.Key,
                    BookingRevenue = g.Where(b => b.Status != "Cancelled").Sum(b => b.TotalPrice),
                    ProductRevenue = orders
                        .Where(o => o.CreatedAt.Date == g.Key && o.OrderStatus == "Completed")
                        .Sum(o => o.TotalAmount)
                })
                .OrderBy(r => r.Date)
                .ToList();

            // Doanh thu theo cơ sở
            var revenueByFacility = bookings
                .Where(b => b.Status != "Cancelled")
                .GroupBy(b => new { b.Court.Facility.FacilityId, b.Court.Facility.FacilityName })
                .Select(g => new
                {
                    FacilityId = g.Key.FacilityId,
                    FacilityName = g.Key.FacilityName,
                    BookingRevenue = g.Sum(b => b.TotalPrice),
                    ProductRevenue = orders
                        .Where(o => o.FacilityId == g.Key.FacilityId && o.OrderStatus == "Completed")
                        .Sum(o => o.TotalAmount),
                    TotalRevenue = g.Sum(b => b.TotalPrice) + orders
                        .Where(o => o.FacilityId == g.Key.FacilityId && o.OrderStatus == "Completed")
                        .Sum(o => o.TotalAmount)
                })
                .OrderByDescending(r => r.TotalRevenue)
                .ToList();

            ViewBag.Facilities = await _context.Facilities.ToListAsync();
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.BookingRevenue = bookingRevenue;
            ViewBag.ProductRevenue = productRevenue;
            ViewBag.DailyAverage = dailyAverage;
            ViewBag.RevenueByDate = revenueByDate;
            ViewBag.RevenueByFacility = revenueByFacility;

            return View();
        }

        // ===================================
        // BOOKING REPORT (Báo cáo đặt sân)
        // ===================================

        [HttpGet]
        public async Task<IActionResult> Booking(DateTime? fromDate, DateTime? toDate, int? facilityId)
        {
            fromDate ??= DateTime.Today.AddDays(-30);
            toDate ??= DateTime.Today;

            var query = _context.Bookings
                .Include(b => b.Court.Facility)
                .Include(b => b.User)
                .Where(b => b.BookingDate >= fromDate && b.BookingDate <= toDate)
                .AsQueryable();

            if (facilityId.HasValue)
                query = query.Where(b => b.Court.FacilityId == facilityId.Value);

            var bookings = await query.ToListAsync();

            // Thống kê tổng quan
            var totalBookings = bookings.Count;
            var completedBookings = bookings.Count(b => b.Status == "Completed");
            var cancelledBookings = bookings.Count(b => b.Status == "Cancelled");
            var pendingBookings = bookings.Count(b => b.Status == "Pending");
            var confirmedBookings = bookings.Count(b => b.Status == "Confirmed");

            var completionRate = totalBookings > 0
                ? (completedBookings * 100.0 / totalBookings).ToString("F1")
                : "0";

            var cancellationRate = totalBookings > 0
                ? (cancelledBookings * 100.0 / totalBookings).ToString("F1")
                : "0";

            // Đặt sân theo ngày
            var bookingsByDate = bookings
                .GroupBy(b => b.BookingDate)
                .Select(g => new
                {
                    Date = g.Key,
                    Total = g.Count(),
                    Completed = g.Count(b => b.Status == "Completed"),
                    Cancelled = g.Count(b => b.Status == "Cancelled"),
                    Pending = g.Count(b => b.Status == "Pending")
                })
                .OrderBy(r => r.Date)
                .ToList();

            // Đặt sân theo cơ sở
            var bookingsByFacility = bookings
                .GroupBy(b => b.Court.Facility.FacilityName)
                .Select(g => new
                {
                    FacilityName = g.Key,
                    Total = g.Count(),
                    Completed = g.Count(b => b.Status == "Completed"),
                    Cancelled = g.Count(b => b.Status == "Cancelled"),
                    Revenue = g.Where(b => b.Status == "Completed").Sum(b => b.TotalPrice)
                })
                .OrderByDescending(r => r.Total)
                .ToList();

            // Khung giờ phổ biến
            var popularTimeSlots = bookings
                .Where(b => b.Status != "Cancelled")
                .GroupBy(b => b.StartTime.Hours)
                .Select(g => new
                {
                    Hour = g.Key,
                    TimeSlot = $"{g.Key}:00 - {g.Key + 1}:00",
                    Count = g.Count(),
                    Revenue = g.Sum(b => b.TotalPrice)
                })
                .OrderByDescending(t => t.Count)
                .Take(10)
                .ToList();

            ViewBag.Facilities = await _context.Facilities.ToListAsync();
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.TotalBookings = totalBookings;
            ViewBag.CompletedBookings = completedBookings;
            ViewBag.CancelledBookings = cancelledBookings;
            ViewBag.PendingBookings = pendingBookings;
            ViewBag.ConfirmedBookings = confirmedBookings;
            ViewBag.CompletionRate = completionRate;
            ViewBag.CancellationRate = cancellationRate;
            ViewBag.BookingsByDate = bookingsByDate;
            ViewBag.BookingsByFacility = bookingsByFacility;
            ViewBag.PopularTimeSlots = popularTimeSlots;

            return View();
        }

        // ===================================
        // PRODUCT REPORT (Báo cáo sản phẩm)
        // ===================================

        [HttpGet]
        public async Task<IActionResult> Product(DateTime? fromDate, DateTime? toDate, int? categoryId)
        {
            fromDate ??= DateTime.Today.AddDays(-30);
            toDate ??= DateTime.Today;

            var query = _context.OrderDetails
                .Include(od => od.Product)
                    .ThenInclude(p => p.Category)
                .Include(od => od.Order)
                .Where(od => od.Order.CreatedAt >= fromDate &&
                            od.Order.CreatedAt <= toDate.Value.AddDays(1) &&
                            od.Order.OrderStatus == "Completed")
                .AsQueryable();

            if (categoryId.HasValue)
                query = query.Where(od => od.Product.CategoryId == categoryId.Value);

            var orderDetails = await query.ToListAsync();

            // Thống kê tổng quan
            var totalProducts = orderDetails.Select(od => od.ProductId).Distinct().Count();
            var totalQuantitySold = orderDetails.Sum(od => od.Quantity);
            var totalRevenue = orderDetails.Sum(od => od.TotalPrice);

            // Sản phẩm bán chạy
            var topProducts = orderDetails
                .GroupBy(od => new { od.Product.ProductId, od.Product.ProductName, od.Product.Category.CategoryName })
                .Select(g => new
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.ProductName,
                    CategoryName = g.Key.CategoryName,
                    QuantitySold = g.Sum(od => od.Quantity),
                    Revenue = g.Sum(od => od.TotalPrice),
                    OrderCount = g.Select(od => od.OrderId).Distinct().Count()
                })
                .OrderByDescending(p => p.Revenue)
                .Take(20)
                .ToList();

            // Doanh thu theo danh mục
            var revenueByCategory = orderDetails
                .GroupBy(od => od.Product.Category.CategoryName)
                .Select(g => new
                {
                    CategoryName = g.Key,
                    QuantitySold = g.Sum(od => od.Quantity),
                    Revenue = g.Sum(od => od.TotalPrice),
                    ProductCount = g.Select(od => od.ProductId).Distinct().Count()
                })
                .OrderByDescending(c => c.Revenue)
                .ToList();

            // Xu hướng bán hàng theo ngày
            var salesTrend = orderDetails
                .GroupBy(od => od.Order.CreatedAt.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Quantity = g.Sum(od => od.Quantity),
                    Revenue = g.Sum(od => od.TotalPrice),
                    OrderCount = g.Select(od => od.OrderId).Distinct().Count()
                })
                .OrderBy(t => t.Date)
                .ToList();

            ViewBag.Categories = await _context.ProductCategories.ToListAsync();
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.TotalProducts = totalProducts;
            ViewBag.TotalQuantitySold = totalQuantitySold;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.TopProducts = topProducts;
            ViewBag.RevenueByCategory = revenueByCategory;
            ViewBag.SalesTrend = salesTrend;

            return View();
        }

        // ===================================
        // CUSTOMER REPORT (Báo cáo khách hàng)
        // ===================================

        [HttpGet]
        public async Task<IActionResult> Customer(DateTime? fromDate, DateTime? toDate)
        {
            fromDate ??= DateTime.Today.AddDays(-30);
            toDate ??= DateTime.Today;

            // Thống kê tổng quan
            var totalCustomers = await _context.Users.CountAsync(u => u.Role == "Customer");
            var newCustomers = await _context.Users
                .CountAsync(u => u.Role == "Customer" &&
                               u.CreatedAt >= fromDate &&
                               u.CreatedAt <= toDate.Value.AddDays(1));

            var activeCustomers = await _context.Bookings
                .Where(b => b.BookingDate >= fromDate && b.BookingDate <= toDate)
                .Select(b => b.UserId)
                .Distinct()
                .CountAsync();

            // Khách hàng thân thiết (chi tiêu >= 5 triệu)
            var vipCustomers = await _context.Bookings
                .Where(b => b.Status == "Completed")
                .GroupBy(b => b.UserId)
                .Where(g => g.Sum(b => b.TotalPrice) >= 5000000)
                .CountAsync();

            // Top khách hàng
            var topCustomers = await _context.Bookings
                .Include(b => b.User)
                .Where(b => b.Status == "Completed" &&
                           b.BookingDate >= fromDate &&
                           b.BookingDate <= toDate)
                .GroupBy(b => new { b.User.UserId, b.User.FullName, b.User.Email, b.User.Phone })
                .Select(g => new
                {
                    UserId = g.Key.UserId,
                    CustomerName = g.Key.FullName,
                    Email = g.Key.Email,
                    Phone = g.Key.Phone,
                    BookingCount = g.Count(),
                    TotalSpent = g.Sum(b => b.TotalPrice),
                    LastBooking = g.Max(b => b.BookingDate)
                })
                .OrderByDescending(c => c.TotalSpent)
                .Take(20)
                .ToListAsync();

            // Khách hàng mới theo tháng
            var newCustomersTrend = await _context.Users
                .Where(u => u.Role == "Customer" &&
                           u.CreatedAt >= fromDate &&
                           u.CreatedAt <= toDate.Value.AddDays(1))
                .GroupBy(u => new { u.CreatedAt.Year, u.CreatedAt.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Count = g.Count()
                })
                .OrderBy(t => t.Year)
                .ThenBy(t => t.Month)
                .ToListAsync();

            // Phân tích hành vi khách hàng
            var customerBehavior = await _context.Users
                .Where(u => u.Role == "Customer")
                .Select(u => new
                {
                    UserId = u.UserId,
                    FullName = u.FullName,
                    BookingCount = u.Bookings.Count(b => b.Status == "Completed"),
                    TotalSpent = u.Bookings.Where(b => b.Status == "Completed").Sum(b => b.TotalPrice),
                    FirstBooking = u.Bookings.Min(b => b.CreatedAt),
                    LastBooking = u.Bookings.Max(b => b.CreatedAt)
                })
                .Where(c => c.BookingCount > 0)
                .OrderByDescending(c => c.BookingCount)
                .Take(10)
                .ToListAsync();

            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.TotalCustomers = totalCustomers;
            ViewBag.NewCustomers = newCustomers;
            ViewBag.ActiveCustomers = activeCustomers;
            ViewBag.VIPCustomers = vipCustomers;
            ViewBag.TopCustomers = topCustomers;
            ViewBag.NewCustomersTrend = newCustomersTrend;
            ViewBag.CustomerBehavior = customerBehavior;

            return View();
        }

        // ===================================
        // COMPARISON REPORT (Báo cáo so sánh)
        // ===================================

        [HttpGet]
        public async Task<IActionResult> Comparison(DateTime? fromDate, DateTime? toDate)
        {
            fromDate ??= DateTime.Today.AddDays(-30);
            toDate ??= DateTime.Today;

            // Tính kỳ trước
            var daysDiff = (toDate.Value - fromDate.Value).Days;
            var previousFromDate = fromDate.Value.AddDays(-daysDiff);
            var previousToDate = fromDate.Value.AddDays(-1);

            // Dữ liệu kỳ hiện tại
            var currentBookings = await _context.Bookings
                .Where(b => b.BookingDate >= fromDate && b.BookingDate <= toDate)
                .ToListAsync();

            var currentOrders = await _context.Orders
                .Where(o => o.CreatedAt >= fromDate && o.CreatedAt <= toDate.Value.AddDays(1))
                .ToListAsync();

            // Dữ liệu kỳ trước
            var previousBookings = await _context.Bookings
                .Where(b => b.BookingDate >= previousFromDate && b.BookingDate <= previousToDate)
                .ToListAsync();

            var previousOrders = await _context.Orders
                .Where(o => o.CreatedAt >= previousFromDate && o.CreatedAt <= previousToDate.AddDays(1))
                .ToListAsync();

            // Tính toán so sánh
            var currentRevenue = currentBookings.Where(b => b.Status != "Cancelled").Sum(b => b.TotalPrice) +
                                currentOrders.Where(o => o.OrderStatus == "Completed").Sum(o => o.TotalAmount);

            var previousRevenue = previousBookings.Where(b => b.Status != "Cancelled").Sum(b => b.TotalPrice) +
                                 previousOrders.Where(o => o.OrderStatus == "Completed").Sum(o => o.TotalAmount);

            var revenueGrowth = previousRevenue > 0
                ? ((currentRevenue - previousRevenue) / previousRevenue * 100).ToString("F1")
                : "0";

            var currentBookingCount = currentBookings.Count;
            var previousBookingCount = previousBookings.Count;
            var bookingGrowth = previousBookingCount > 0
                ? ((currentBookingCount - previousBookingCount) * 100.0 / previousBookingCount).ToString("F1")
                : "0";

            var currentOrderCount = currentOrders.Count;
            var previousOrderCount = previousOrders.Count;
            var orderGrowth = previousOrderCount > 0
                ? ((currentOrderCount - previousOrderCount) * 100.0 / previousOrderCount).ToString("F1")
                : "0";

            // So sánh theo cơ sở
            var facilities = await _context.Facilities.ToListAsync();
            var facilityComparison = facilities.Select(f => new
            {
                FacilityName = f.FacilityName,
                CurrentRevenue = currentBookings
                    .Where(b => b.Court.FacilityId == f.FacilityId && b.Status != "Cancelled")
                    .Sum(b => b.TotalPrice),
                PreviousRevenue = previousBookings
                    .Where(b => b.Court.FacilityId == f.FacilityId && b.Status != "Cancelled")
                    .Sum(b => b.TotalPrice)
            }).ToList();

            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.PreviousFromDate = previousFromDate.ToString("yyyy-MM-dd");
            ViewBag.PreviousToDate = previousToDate.ToString("yyyy-MM-dd");

            ViewBag.CurrentRevenue = currentRevenue;
            ViewBag.PreviousRevenue = previousRevenue;
            ViewBag.RevenueGrowth = revenueGrowth;

            ViewBag.CurrentBookingCount = currentBookingCount;
            ViewBag.PreviousBookingCount = previousBookingCount;
            ViewBag.BookingGrowth = bookingGrowth;

            ViewBag.CurrentOrderCount = currentOrderCount;
            ViewBag.PreviousOrderCount = previousOrderCount;
            ViewBag.OrderGrowth = orderGrowth;

            ViewBag.FacilityComparison = facilityComparison;

            return View();
        }

        // ===================================
        // EXPORT REPORTS
        // ===================================

        [HttpGet]
        public async Task<IActionResult> ExportRevenue(DateTime? fromDate, DateTime? toDate, int? facilityId)
        {
            // TODO: Implement export to Excel/PDF
            TempData["InfoMessage"] = "Chức năng xuất báo cáo đang được phát triển.";
            return RedirectToAction(nameof(Revenue), new { fromDate, toDate, facilityId });
        }

        [HttpGet]
        public async Task<IActionResult> ExportBooking(DateTime? fromDate, DateTime? toDate, int? facilityId)
        {
            // TODO: Implement export to Excel/PDF
            TempData["InfoMessage"] = "Chức năng xuất báo cáo đang được phát triển.";
            return RedirectToAction(nameof(Booking), new { fromDate, toDate, facilityId });
        }

        [HttpGet]
        public async Task<IActionResult> ExportProduct(DateTime? fromDate, DateTime? toDate, int? categoryId)
        {
            // TODO: Implement export to Excel/PDF
            TempData["InfoMessage"] = "Chức năng xuất báo cáo đang được phát triển.";
            return RedirectToAction(nameof(Product), new { fromDate, toDate, categoryId });
        }

        [HttpGet]
        public async Task<IActionResult> ExportCustomer(DateTime? fromDate, DateTime? toDate)
        {
            // TODO: Implement export to Excel/PDF
            TempData["InfoMessage"] = "Chức năng xuất báo cáo đang được phát triển.";
            return RedirectToAction(nameof(Customer), new { fromDate, toDate });
        }
    }
}