using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiSanCauLong.Data;
using QuanLiSanCauLong.ViewModels;

namespace QuanLiSanCauLong.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminBookingController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminBookingController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(DateTime? fromDate, DateTime? toDate,
            int? facilityId, string status, string paymentStatus)
        {
            var query = _context.Bookings
                .Include(b => b.Court)
                    .ThenInclude(c => c.Facility)
                .Include(b => b.User)
                .Include(b => b.Orders)
                .AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(b => b.BookingDate >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(b => b.BookingDate <= toDate.Value);

            if (facilityId.HasValue)
                query = query.Where(b => b.Court.FacilityId == facilityId.Value);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(b => b.Status == status);

            if (!string.IsNullOrEmpty(paymentStatus))
                query = query.Where(b => b.PaymentStatus == paymentStatus);

            var bookings = await query
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            var model = bookings.Select(b => new BookingHistoryViewModel
            {
                BookingId = b.BookingId,
                BookingCode = b.BookingCode,

                // ĐÃ THÊM: Gán dữ liệu khách hàng từ navigation property b.User
                CustomerName = b.User?.FullName ?? "Khách vãng lai",
                Phone = b.User?.Phone ?? "N/A",
                Email = b.User?.Email ?? "N/A",

                FacilityName = b.Court.Facility.FacilityName,
                CourtNumber = b.Court.CourtNumber,
                CourtType = b.Court.CourtType,
                BookingDate = b.BookingDate,
                StartTime = b.StartTime,
                EndTime = b.EndTime,
                Duration = (int)(b.EndTime - b.StartTime).TotalMinutes,
                TotalPrice = b.TotalPrice,
                Status = b.Status,
                PaymentStatus = b.PaymentStatus,
                PaymentMethod = b.PaymentMethod,
                CreatedAt = b.CreatedAt,
                RelatedOrders = b.Orders.Select(o => new OrderViewModel
                {
                    OrderId = o.OrderId,
                    TotalAmount = o.TotalAmount,
                    OrderStatus = o.OrderStatus
                }).ToList()
            }).ToList();
            ViewBag.Facilities = await _context.Facilities.ToListAsync();
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.TotalBookings = model.Count;
            ViewBag.PendingBookings = model.Count(b => b.Status == "Pending");
            ViewBag.CompletedBookings = model.Count(b => b.Status == "Completed");
            ViewBag.TotalRevenue = model.Where(b => b.Status != "Cancelled").Sum(b => b.TotalPrice);

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Court)
                    .ThenInclude(c => c.Facility)
                .Include(b => b.User)
                .Include(b => b.Orders)
                    .ThenInclude(o => o.OrderDetails)
                        .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null)
                return NotFound();

            var model = new BookingHistoryViewModel
            {
                BookingId = booking.BookingId,
                BookingCode = booking.BookingCode,
                FacilityName = booking.Court.Facility.FacilityName,
                CourtNumber = booking.Court.CourtNumber,
                CourtType = booking.Court.CourtType,
                BookingDate = booking.BookingDate,
                StartTime = booking.StartTime,
                EndTime = booking.EndTime,
                Duration = (int)(booking.EndTime - booking.StartTime).TotalMinutes,
                TotalPrice = booking.TotalPrice,
                Status = booking.Status,
                PaymentStatus = booking.PaymentStatus,
                PaymentMethod = booking.PaymentMethod,
                Note = booking.Note,
                CreatedAt = booking.CreatedAt,
                RelatedOrders = booking.Orders.Select(o => new OrderViewModel
                {
                    OrderId = o.OrderId,
                    OrderCode = o.OrderCode,
                    TotalAmount = o.TotalAmount,
                    OrderStatus = o.OrderStatus,
                    OrderDetails = o.OrderDetails.Select(od => new OrderDetailViewModel
                    {
                        ProductName = od.Product.ProductName,
                        Quantity = od.Quantity,
                        Unit = od.Product.Unit,
                        UnitPrice = od.UnitPrice,
                        TotalPrice = od.TotalPrice
                    }).ToList()
                }).ToList()
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Confirm(int bookingId)
        {
            if (bookingId <= 0)
                return Json(new { success = false, message = "ID không hợp lệ!" });

            var booking = await _context.Bookings.FindAsync(bookingId);

            if (booking == null)
                return Json(new { success = false, message = "Không tìm thấy đơn đặt sân!" });

            if (booking.Status != "Pending")
                return Json(new { success = false, message = "Chỉ có thể xác nhận đơn đang chờ!" });

            booking.Status = "Confirmed";
            booking.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Xác nhận đơn đặt sân thành công!" });
        }

        [HttpPost]
        public async Task<IActionResult> Cancel(int bookingId, string reason)
        {
            if (bookingId <= 0)
                return Json(new { success = false, message = "ID không hợp lệ!" });

            var booking = await _context.Bookings.FindAsync(bookingId);

            if (booking == null)
                return Json(new { success = false, message = "Không tìm thấy đơn đặt sân!" });

            if (booking.Status == "Cancelled")
                return Json(new { success = false, message = "Đơn đã được hủy trước đó!" });

            if (booking.Status == "Completed")
                return Json(new { success = false, message = "Không thể hủy đơn đã hoàn thành!" });

            booking.Status = "Cancelled";
            booking.Note = reason;
            booking.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Hủy đơn đặt sân thành công!" });
        }

        [HttpPost]
        public async Task<IActionResult> Complete(int bookingId)
        {
            if (bookingId <= 0)
                return Json(new { success = false, message = "ID không hợp lệ!" });

            var booking = await _context.Bookings.FindAsync(bookingId);

            if (booking == null)
                return Json(new { success = false, message = "Không tìm thấy đơn đặt sân!" });

            if (booking.Status != "Confirmed")
                return Json(new { success = false, message = "Chỉ có thể hoàn thành đơn đã xác nhận!" });

            booking.Status = "Completed";
            booking.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Hoàn thành đơn đặt sân thành công!" });
        }
    }
}