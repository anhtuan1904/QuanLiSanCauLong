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

        [HttpGet]
        public async Task<IActionResult> Index(DateTime? fromDate, DateTime? toDate)
        {
            DateTime from = fromDate ?? DateTime.Today.AddDays(-30);
            DateTime to = toDate ?? DateTime.Today;

            var model = new AdminDashboardViewModel
            {
                FromDate = from,
                ToDate = to
            };

            // Lấy dữ liệu bookings và orders
            var bookings = await _context.Bookings
                .Include(b => b.Court.Facility)
                .Include(b => b.User)
                .Where(b => b.BookingDate >= from && b.BookingDate <= to)
                .ToListAsync();

            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                .Include(o => o.Facility)
                .Where(o => o.CreatedAt >= from && o.CreatedAt <= to.AddDays(1))
                .ToListAsync();

            // Thống kê tổng quan
            model.TotalBookings = bookings.Count;
            model.CompletedBookings = bookings.Count(b => b.Status == "Completed");
            model.CancelledBookings = bookings.Count(b => b.Status == "Cancelled");
            model.BookingRevenue = bookings.Where(b => b.Status != "Cancelled").Sum(b => b.TotalPrice);
            model.ProductRevenue = orders.Where(o => o.OrderStatus == "Completed").Sum(o => o.TotalAmount);
            model.TotalRevenue = model.BookingRevenue + model.ProductRevenue;

            model.TotalCustomers = await _context.Users.CountAsync(u => u.Role == "Customer");
            model.NewCustomers = await _context.Users.CountAsync(u => u.Role == "Customer" && u.CreatedAt >= from);
            model.TotalOrders = orders.Count;

            // Doanh thu theo ngày
            model.RevenueByDate = bookings
                .GroupBy(b => b.BookingDate)
                .Select(g => new RevenueByDateViewModel
                {
                    Date = g.Key,
                    BookingRevenue = g.Where(b => b.Status != "Cancelled").Sum(b => b.TotalPrice),
                    ProductRevenue = orders
                        .Where(o => o.CreatedAt.Date == g.Key && o.OrderStatus == "Completed")
                        .Sum(o => o.TotalAmount)
                })
                .OrderBy(r => r.Date)
                .ToList();

            foreach (var item in model.RevenueByDate)
            {
                item.TotalRevenue = item.BookingRevenue + item.ProductRevenue;
            }

            // Doanh thu theo cơ sở
            model.RevenueByFacility = bookings
                .Where(b => b.Status != "Cancelled")
                .GroupBy(b => b.Court.Facility.FacilityName)
                .Select(g => new RevenueByFacilityViewModel
                {
                    FacilityName = g.Key,
                    BookingCount = g.Count(),
                    BookingRevenue = g.Sum(b => b.TotalPrice),
                    ProductRevenue = orders
                        .Where(o => o.Facility.FacilityName == g.Key && o.OrderStatus == "Completed")
                        .Sum(o => o.TotalAmount)
                })
                .ToList();

            foreach (var item in model.RevenueByFacility)
            {
                item.TotalRevenue = item.BookingRevenue + item.ProductRevenue;
            }

            // Khung giờ phổ biến
            model.PopularTimeSlots = bookings
                .Where(b => b.Status != "Cancelled")
                .GroupBy(b => new { Hour = b.StartTime.Hours })
                .Select(g => new PopularTimeSlotViewModel
                {
                    TimeSlot = g.Key.Hour + ":00 - " + (g.Key.Hour + 1) + ":00",
                    BookingCount = g.Count(),
                    Revenue = g.Sum(b => b.TotalPrice)
                })
                .OrderByDescending(t => t.BookingCount)
                .Take(10)
                .ToList();

            // Sản phẩm bán chạy
            var productStats = await _context.OrderDetails
                .Include(od => od.Product)
                .Include(od => od.Order)
                .Where(od => od.Order.CreatedAt >= from && od.Order.CreatedAt <= to.AddDays(1)
                        && od.Order.OrderStatus == "Completed")
                .GroupBy(od => new { od.Product.ProductName, od.Product.Category.CategoryType })
                .Select(g => new TopProductViewModel
                {
                    ProductName = g.Key.ProductName,
                    CategoryType = g.Key.CategoryType,
                    QuantitySold = g.Sum(od => od.Quantity),
                    Revenue = g.Sum(od => od.TotalPrice)
                })
                .OrderByDescending(p => p.Revenue)
                .Take(10)
                .ToListAsync();

            model.TopProducts = productStats;

            // Khách hàng thân thiết
            model.TopCustomers = bookings
                .Where(b => b.Status != "Cancelled")
                .GroupBy(b => new { b.User.FullName, b.User.Email, b.User.Phone })
                .Select(g => new TopCustomerViewModel
                {
                    CustomerName = g.Key.FullName,
                    Email = g.Key.Email,
                    Phone = g.Key.Phone,
                    BookingCount = g.Count(),
                    TotalSpent = g.Sum(b => b.TotalPrice)
                })
                .OrderByDescending(c => c.TotalSpent)
                .Take(10)
                .ToList();

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Analytics(DateTime? fromDate, DateTime? toDate)
        {
            // Có thể tái sử dụng logic từ Index hoặc tạo view riêng
            return await Index(fromDate, toDate);
        }
    }
}