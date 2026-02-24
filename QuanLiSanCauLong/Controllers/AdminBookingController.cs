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

        // ================================================================
        // INDEX
        // ================================================================
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
                CustomerName = b.User?.FullName ?? "Khách vãng lai",
                Phone = b.User?.Phone ?? "N/A",
                Email = b.User?.Email ?? "N/A",
                FacilityName = b.Court?.Facility?.FacilityName ?? "N/A",
                CourtNumber = b.Court?.CourtNumber?.ToString() ?? "N/A",
                CourtType = b.Court?.CourtType ?? "N/A",
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

            // ✅ FIX: Thêm ConfirmedBookings vào ViewBag (view cần)
            ViewBag.Facilities = await _context.Facilities.ToListAsync();
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.TotalBookings = model.Count;
            ViewBag.PendingBookings = model.Count(b => b.Status == "Pending");
            ViewBag.ConfirmedBookings = model.Count(b => b.Status == "Confirmed");
            ViewBag.CompletedBookings = model.Count(b => b.Status == "Completed");
            ViewBag.TotalRevenue = model.Where(b => b.Status != "Cancelled")
                                             .Sum(b => b.TotalPrice);

            return View(model);
        }

        // ================================================================
        // DETAILS
        // ================================================================
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
                // ✅ FIX: Set customer info (view cần CustomerName, Phone, Email)
                CustomerName = booking.User?.FullName ?? "Khách vãng lai",
                Phone = booking.User?.Phone ?? "N/A",
                Email = booking.User?.Email ?? "N/A",
                FacilityName = booking.Court?.Facility?.FacilityName ?? "N/A",
                CourtNumber = booking.Court?.CourtNumber?.ToString() ?? "N/A",
                CourtType = booking.Court?.CourtType ?? "N/A",
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
                        ProductName = od.Product?.ProductName ?? "Không xác định",
                        Quantity = od.Quantity,
                        Unit = od.Product?.Unit ?? "",
                        UnitPrice = od.UnitPrice,
                        TotalPrice = od.TotalPrice
                    }).ToList()
                }).ToList()
            };

            return View(model);
        }

        // ================================================================
        // CONFIRM — Xác nhận đơn Pending → Confirmed
        // ================================================================
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

        // ================================================================
        // CHECKIN — ✅ FIX: Thêm action này (Details.cshtml gọi checkInBooking)
        // Confirmed → Playing
        // ================================================================
        [HttpPost]
        public async Task<IActionResult> CheckIn(int bookingId)
        {
            if (bookingId <= 0)
                return Json(new { success = false, message = "ID không hợp lệ!" });

            var booking = await _context.Bookings.FindAsync(bookingId);

            if (booking == null)
                return Json(new { success = false, message = "Không tìm thấy đơn đặt sân!" });

            if (booking.Status != "Confirmed")
                return Json(new { success = false, message = "Chỉ có thể check-in đơn đã xác nhận!" });

            booking.Status = "Playing";
            booking.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Check-in thành công! Chúc khách hàng chơi vui vẻ." });
        }

        // ================================================================
        // COMPLETE — Playing/Confirmed → Completed
        // ================================================================
        [HttpPost]
        public async Task<IActionResult> Complete(int bookingId)
        {
            if (bookingId <= 0)
                return Json(new { success = false, message = "ID không hợp lệ!" });

            var booking = await _context.Bookings.FindAsync(bookingId);

            if (booking == null)
                return Json(new { success = false, message = "Không tìm thấy đơn đặt sân!" });

            if (booking.Status != "Confirmed" && booking.Status != "Playing")
                return Json(new { success = false, message = "Không thể hoàn thành đơn này!" });

            booking.Status = "Completed";
            booking.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đánh dấu hoàn thành thành công!" });
        }

        // ================================================================
        // CANCEL — Admin huỷ đơn bất kỳ
        // ================================================================
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
            booking.CancelReason = reason;
            booking.CancelledAt = DateTime.Now;
            booking.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Hủy đơn đặt sân thành công!" });
        }

        // ================================================================
        // UPDATE PAYMENT — Admin cập nhật trạng thái thanh toán
        // ================================================================
        [HttpPost]
        public async Task<IActionResult> UpdatePayment(int bookingId, string paymentStatus)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);

            if (booking == null)
                return Json(new { success = false, message = "Không tìm thấy đơn!" });

            booking.PaymentStatus = paymentStatus;
            booking.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Cập nhật thanh toán thành công!" });
        }

        // ================================================================
        // EXPORT — Xuất Excel (placeholder)
        // ================================================================
        [HttpGet]
        public IActionResult Export()
        {
            // TODO: Implement Excel export với EPPlus hoặc ClosedXML
            TempData["InfoMessage"] = "Tính năng xuất Excel đang được phát triển.";
            return RedirectToAction(nameof(Index));
        }
    }
}
