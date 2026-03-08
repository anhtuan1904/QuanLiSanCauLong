using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiSanCauLong.Data;
using QuanLiSanCauLong.Models;
using QuanLiSanCauLong.ViewModels;
using System.Security.Claims;

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
        // INDEX — Danh sách đặt sân + Tab filter "Chờ Duyệt CK"
        // ================================================================
        [HttpGet]
        public async Task<IActionResult> Index(
            string? filter = "",
            string? search = "",
            string? paymentMethod = "",
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int? facilityId = null,
            string? status = null,
            string? paymentStatus = null)
        {
            var query = _context.Bookings
                .Include(b => b.Court)
                    .ThenInclude(c => c.Facility)
                .Include(b => b.User)
                .Include(b => b.Orders)
                    .ThenInclude(o => o.OrderDetails)
                        .ThenInclude(od => od.Product)
                .AsQueryable();

            // ── Tab filter (ưu tiên hơn status param) ──
            switch (filter)
            {
                case "awaiting-transfer":
                    // Đơn Transfer chưa được xác nhận thanh toán
                    query = query.Where(b =>
                        b.PaymentMethod == "Transfer" &&
                        (b.PaymentStatus == "Unpaid" || b.PaymentStatus == "Pending") &&
                        b.Status == "Pending");
                    break;
                case "Pending":
                case "Confirmed":
                case "Playing":
                case "Completed":
                case "Cancelled":
                    query = query.Where(b => b.Status == filter);
                    break;
                default:
                    // Fallback: dùng status param cũ nếu không có filter tab
                    if (!string.IsNullOrEmpty(status))
                        query = query.Where(b => b.Status == status);
                    break;
            }

            // ── Search theo mã đơn ──
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(b => b.BookingCode.Contains(search));

            // ── Phương thức thanh toán ──
            if (!string.IsNullOrEmpty(paymentMethod))
                query = query.Where(b => b.PaymentMethod == paymentMethod);
            else if (!string.IsNullOrEmpty(paymentStatus))
                query = query.Where(b => b.PaymentStatus == paymentStatus);

            // ── Cơ sở ──
            if (facilityId.HasValue)
                query = query.Where(b => b.Court.FacilityId == facilityId.Value);

            // ── Date range ──
            if (fromDate.HasValue)
                query = query.Where(b => b.BookingDate >= fromDate.Value);
            if (toDate.HasValue)
                query = query.Where(b => b.BookingDate <= toDate.Value);

            var bookings = await query.OrderByDescending(b => b.CreatedAt).ToListAsync();

            // ── ViewBag counters cho tab badges ──
            ViewBag.FilterType = filter;
            ViewBag.Search = search;
            ViewBag.PaymentMethod = paymentMethod;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.Facilities = await _context.Facilities.ToListAsync();
            ViewBag.TotalCount = await _context.Bookings.CountAsync();
            ViewBag.TotalBookings = bookings.Count;
            ViewBag.PendingCount = await _context.Bookings.CountAsync(b => b.Status == "Pending");
            ViewBag.PendingBookings = bookings.Count(b => b.Status == "Pending");
            ViewBag.ConfirmedCount = await _context.Bookings.CountAsync(b => b.Status == "Confirmed");
            ViewBag.ConfirmedBookings = bookings.Count(b => b.Status == "Confirmed");
            ViewBag.CompletedBookings = bookings.Count(b => b.Status == "Completed");
            ViewBag.TotalRevenue = bookings.Where(b => b.Status != "Cancelled").Sum(b => b.TotalPrice);
            ViewBag.AwaitingTransferCount = await _context.Bookings.CountAsync(b =>
                b.PaymentMethod == "Transfer" &&
                (b.PaymentStatus == "Unpaid" || b.PaymentStatus == "Pending") &&
                b.Status == "Pending");

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
                Duration = b.Duration,
                TotalPrice = b.TotalPrice,
                Status = b.Status,
                PaymentStatus = b.PaymentStatus,
                PaymentMethod = b.PaymentMethod,
                CreatedAt = b.CreatedAt,
                Note = b.Note,
                RelatedOrders = b.Orders?.Select(o => new OrderViewModel
                {
                    OrderId = o.OrderId,
                    TotalAmount = o.TotalAmount,
                    OrderStatus = o.OrderStatus,
                    OrderDetails = o.OrderDetails?.Select(od => new OrderDetailViewModel
                    {
                        ProductName = od.Product?.ProductName ?? "",
                        Quantity = od.Quantity,
                        Unit = od.Product?.Unit ?? "",
                        UnitPrice = od.UnitPrice,
                        TotalPrice = od.TotalPrice
                    }).ToList() ?? new()
                }).ToList() ?? new()
            }).ToList();

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

            if (booking == null) return NotFound();

            var model = new BookingHistoryViewModel
            {
                BookingId = booking.BookingId,
                BookingCode = booking.BookingCode,
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
                CanCancel = booking.Status != "Cancelled" && booking.Status != "Completed",
                RelatedOrders = booking.Orders?.Select(o => new OrderViewModel
                {
                    OrderId = o.OrderId,
                    OrderCode = o.OrderCode,
                    TotalAmount = o.TotalAmount,
                    OrderStatus = o.OrderStatus,
                    OrderDetails = o.OrderDetails?.Select(od => new OrderDetailViewModel
                    {
                        ProductName = od.Product?.ProductName ?? "Không xác định",
                        Quantity = od.Quantity,
                        Unit = od.Product?.Unit ?? "",
                        UnitPrice = od.UnitPrice,
                        TotalPrice = od.TotalPrice
                    }).ToList() ?? new()
                }).ToList() ?? new()
            };

            return View(model);
        }

        // ================================================================
        // CONFIRM PAYMENT — Xác nhận đã nhận tiền chuyển khoản (MỚI)
        // ================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmPayment(int id, string? confirmNote)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();

            if (booking.PaymentMethod != "Transfer")
            {
                TempData["ErrorMessage"] = "Chỉ áp dụng cho đơn chuyển khoản.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (booking.PaymentStatus == "Paid")
            {
                TempData["ErrorMessage"] = "Đơn này đã được xác nhận thanh toán trước đó.";
                return RedirectToAction(nameof(Details), new { id });
            }

            booking.PaymentStatus = "Paid";
            booking.UpdatedAt = DateTime.Now;

            // Tự động chuyển sang Confirmed sau khi nhận tiền
            if (booking.Status == "Pending")
                booking.Status = "Confirmed";

            // Append ghi chú xác nhận
            if (!string.IsNullOrEmpty(confirmNote))
                booking.Note = string.IsNullOrEmpty(booking.Note)
                    ? $"[Admin CK] {confirmNote}"
                    : $"{booking.Note}\n[Admin CK] {confirmNote}";

            await _context.SaveChangesAsync();

            // Log activity
            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = GetCurrentAdminId(),
                Action = "ConfirmPayment",
                TableName = "Bookings",
                RecordId = booking.BookingId,
                OldValue = "PaymentStatus=Unpaid",
                NewValue = $"PaymentStatus=Paid | Status=Confirmed | Note: {confirmNote}",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = HttpContext.Request.Headers["User-Agent"].ToString(),
                CreatedAt = DateTime.Now
            });
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"✅ Đã xác nhận nhận tiền CK cho đơn {booking.BookingCode}. Đơn đã chuyển sang Confirmed.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // ================================================================
        // CONFIRM ORDER — Xác nhận đơn Pending → Confirmed (MỚI, thay Confirm cũ)
        // Bao gồm guard: Transfer phải xác nhận CK trước
        // ================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmOrder(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();

            if (booking.Status != "Pending")
            {
                TempData["ErrorMessage"] = "Đơn không ở trạng thái Pending.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Guard: không confirm đơn Transfer chưa xác nhận thanh toán
            if (booking.PaymentMethod == "Transfer" && booking.PaymentStatus != "Paid")
            {
                TempData["ErrorMessage"] = "Vui lòng xác nhận đã nhận tiền CK trước khi xác nhận đơn.";
                return RedirectToAction(nameof(Details), new { id });
            }

            booking.Status = "Confirmed";
            booking.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"✅ Đã xác nhận đơn {booking.BookingCode}.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // ================================================================
        // CONFIRM (JSON) — giữ lại để tương thích với JS cũ gọi Ajax
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

            // Guard: Transfer chưa nhận tiền → block
            if (booking.PaymentMethod == "Transfer" && booking.PaymentStatus != "Paid")
                return Json(new { success = false, message = "Đơn chuyển khoản chưa được xác nhận thanh toán. Vui lòng dùng nút 'Xác nhận đã nhận tiền CK' trước." });

            booking.Status = "Confirmed";
            booking.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Xác nhận đơn đặt sân thành công!" });
        }

        // ================================================================
        // UPDATE STATUS — Cập nhật trạng thái (Playing / Completed) (MỚI)
        // ================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string newStatus)
        {
            var allowed = new[] { "Playing", "Completed" };
            if (!allowed.Contains(newStatus))
            {
                TempData["ErrorMessage"] = "Trạng thái không hợp lệ.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();

            booking.Status = newStatus;
            booking.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"✅ Đã cập nhật trạng thái đơn {booking.BookingCode} → {newStatus}.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // ================================================================
        // CHECKIN (JSON) — Confirmed → Playing, tương thích JS cũ
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
        // COMPLETE (JSON) — Playing/Confirmed → Completed, tương thích JS cũ
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
        // CANCEL — Admin huỷ đơn (hỗ trợ cả JSON lẫn form POST)
        // ================================================================
        [HttpPost]
        public async Task<IActionResult> Cancel(int? bookingId, int? id, string? reason, string? cancelReason)
        {
            // Hỗ trợ cả 2 cách gọi: Ajax (bookingId, reason) và form (id, cancelReason)
            int targetId = bookingId ?? id ?? 0;
            string? theReason = reason ?? cancelReason;

            if (targetId <= 0)
                return Request.Headers["X-Requested-With"] == "XMLHttpRequest"
                    ? Json(new { success = false, message = "ID không hợp lệ!" })
                    : BadRequest();

            var booking = await _context.Bookings.FindAsync(targetId);
            if (booking == null)
                return Request.Headers["X-Requested-With"] == "XMLHttpRequest"
                    ? Json(new { success = false, message = "Không tìm thấy đơn đặt sân!" })
                    : NotFound();

            if (booking.Status == "Cancelled")
                return Request.Headers["X-Requested-With"] == "XMLHttpRequest"
                    ? Json(new { success = false, message = "Đơn đã được hủy trước đó!" })
                    : RedirectToAction(nameof(Details), new { id = targetId });

            if (booking.Status == "Completed")
                return Request.Headers["X-Requested-With"] == "XMLHttpRequest"
                    ? Json(new { success = false, message = "Không thể hủy đơn đã hoàn thành!" })
                    : RedirectToAction(nameof(Details), new { id = targetId });

            booking.Status = "Cancelled";
            booking.CancelReason = theReason;
            booking.CancelledAt = DateTime.Now;
            booking.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { success = true, message = "Hủy đơn đặt sân thành công!" });

            TempData["SuccessMessage"] = $"Đã hủy đơn {booking.BookingCode}.";
            return RedirectToAction(nameof(Index));
        }

        // ================================================================
        // UPDATE PAYMENT (JSON) — Admin cập nhật trạng thái thanh toán thủ công
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
            TempData["InfoMessage"] = "Tính năng xuất Excel đang được phát triển.";
            return RedirectToAction(nameof(Index));
        }

        // ── Helper ──
        private int GetCurrentAdminId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return (claim != null && int.TryParse(claim.Value, out int id)) ? id : 0;
        }
    }
}
