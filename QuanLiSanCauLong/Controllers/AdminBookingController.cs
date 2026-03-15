using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiSanCauLong.Data;
using QuanLiSanCauLong.Models;
using QuanLiSanCauLong.ViewModels;
using System.Security.Claims;

namespace QuanLiSanCauLong.Controllers
{
    [Authorize(Roles = "Admin,Staff")]
    public class AdminBookingController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminBookingController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ═════════════════════════════════════════════════════════════════════
        // INDEX
        // ═════════════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Index(
            string? filter, string? search,
            string? paymentMethod,
            DateTime? fromDate, DateTime? toDate,
            int page = 1)
        {
            const int pageSize = 25;

            var query = _context.Bookings
                .Include(b => b.Court).ThenInclude(c => c!.Facility)
                .Include(b => b.User)
                .AsQueryable();

            // Tab filter
            if (filter == "awaiting-transfer")
                query = query.Where(b =>
                    b.PaymentMethod == "Transfer" &&
                    b.PaymentStatus != "Paid" &&
                    b.Status != "Cancelled");
            else if (!string.IsNullOrEmpty(filter))
                query = query.Where(b => b.Status == filter);

            // Search: ma don / ten / SDT
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(b =>
                    b.BookingCode.ToLower().Contains(s) ||
                    (b.User != null && b.User.FullName.ToLower().Contains(s)) ||
                    (b.User != null && b.User.Phone != null && b.User.Phone.Contains(s)));
            }

            if (!string.IsNullOrEmpty(paymentMethod))
                query = query.Where(b => b.PaymentMethod == paymentMethod);
            if (fromDate.HasValue)
                query = query.Where(b => b.BookingDate >= fromDate.Value.Date);
            if (toDate.HasValue)
                query = query.Where(b => b.BookingDate <= toDate.Value.Date);

            // Badge counts cho tab (1 query duy nhat)
            var counts = await _context.Bookings
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    Pending = g.Count(b => b.Status == "Pending"),
                    Confirmed = g.Count(b => b.Status == "Confirmed"),
                    AwaitCk = g.Count(b =>
                        b.PaymentMethod == "Transfer" &&
                        b.PaymentStatus != "Paid" &&
                        b.Status != "Cancelled")
                })
                .FirstOrDefaultAsync();

            ViewBag.TotalCount = counts?.Total ?? 0;
            ViewBag.PendingCount = counts?.Pending ?? 0;
            ViewBag.ConfirmedCount = counts?.Confirmed ?? 0;
            ViewBag.AwaitingTransferCount = counts?.AwaitCk ?? 0;

            int total = await query.CountAsync();
            var bookings = await query
                .OrderByDescending(b => b.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.FilterType = filter ?? "";
            ViewBag.Search = search;
            ViewBag.PaymentMethod = paymentMethod;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.Page = page;
            ViewBag.TotalPage = (int)Math.Ceiling((double)total / pageSize);

            var cancelHours = await GetSetting("BookingCancellationHours", 2m);
            return View(bookings.Select(b => Map(b, cancelHours)).ToList());
        }

        // ═════════════════════════════════════════════════════════════════════
        // DETAILS
        // ═════════════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var b = await LoadFull(id);
            if (b == null) return NotFound();

            // Load VoucherCode riêng — tránh EF join bảng VoucherUsages
            // (bảng này không có cột CreatedAt nên gây SqlException nếu Include thẳng)
            string? voucherCode = await _context.VoucherUsages
                .Where(v => v.BookingId == id)
                .Select(v => v.Voucher != null ? v.Voucher.VoucherCode : null)
                .FirstOrDefaultAsync();

            SetBankViewBag(b.BookingCode);
            var cancelHours = await GetSetting("BookingCancellationHours", 2m);
            var vm = Map(b, cancelHours);
            vm.VoucherCode = voucherCode;
            return View(vm);
        }

        // ═════════════════════════════════════════════════════════════════════
        // SET STATUS  <-- gop: ConfirmPayment + ConfirmOrder + UpdateStatus
        //
        //  newStatus  | Hanh dong
        // ────────────┼──────────────────────────────────────────────────────
        //  ConfirmCK  | PaymentStatus -> Paid (chua doi Status don)
        //  Confirmed  | Pending -> Confirmed, PaymentStatus -> Paid
        //  Playing    | Confirmed -> Playing
        //  Completed  | Playing -> Completed, tu Paid neu chua
        // ═════════════════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetStatus(int id, string newStatus, string? note)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();

            if (booking.Status is "Cancelled" or "Completed")
            {
                TempData["ErrorMessage"] = $"Khong the thay doi don da {booking.Status}.";
                return RedirectToAction(nameof(Details), new { id });
            }

            string old = booking.Status;

            switch (newStatus)
            {
                case "ConfirmCK":
                    if (booking.PaymentStatus == "Paid")
                    {
                        TempData["ErrorMessage"] = "Don nay da xac nhan CK truoc do!";
                        return RedirectToAction(nameof(Details), new { id });
                    }
                    booking.PaymentStatus = "Paid";
                    break;

                case "Confirmed":
                    if (booking.Status != "Pending")
                    {
                        TempData["ErrorMessage"] = "Chi xac nhan duoc don dang Cho xac nhan!";
                        return RedirectToAction(nameof(Details), new { id });
                    }
                    if (booking.PaymentMethod == "Transfer" && booking.PaymentStatus != "Paid")
                    {
                        TempData["ErrorMessage"] = "Vui long xac nhan CK truoc!";
                        return RedirectToAction(nameof(Details), new { id });
                    }
                    booking.Status = "Confirmed";
                    booking.PaymentStatus = "Paid";
                    break;

                case "Playing":
                case "Completed":
                    booking.Status = newStatus;
                    if (newStatus == "Completed" && booking.PaymentStatus != "Paid")
                        booking.PaymentStatus = "Paid";
                    break;

                default:
                    TempData["ErrorMessage"] = "Trang thai khong hop le!";
                    return RedirectToAction(nameof(Details), new { id });
            }

            if (!string.IsNullOrWhiteSpace(note))
                booking.Note = note.Trim();

            booking.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            await Log(GetAdminId(), "SetStatus", booking.BookingId, old,
                newStatus + (note != null ? $"|{note}" : ""));

            string label = newStatus switch
            {
                "ConfirmCK" => "Xac nhan CK",
                "Confirmed" => "Da xac nhan",
                "Playing" => "Dang choi",
                "Completed" => "Hoan thanh",
                _ => newStatus
            };
            TempData["SuccessMessage"] =
                $"Don <strong>{booking.BookingCode}</strong> &rarr; <strong>{label}</strong>.";

            return RedirectToAction(nameof(Details), new { id });
        }

        // ═════════════════════════════════════════════════════════════════════
        // CANCEL  (admin khong bi gioi han gio nhu khach)
        // ═════════════════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, string cancelReason)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();

            if (booking.Status is "Cancelled" or "Completed")
            {
                TempData["ErrorMessage"] = $"Khong the huy don dang o trang thai {booking.Status}!";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (string.IsNullOrWhiteSpace(cancelReason))
            {
                TempData["ErrorMessage"] = "Vui long nhap ly do huy!";
                return RedirectToAction(nameof(Details), new { id });
            }

            string old = booking.Status;
            booking.Status = "Cancelled";
            booking.CancelReason = $"[Admin] {cancelReason.Trim()}";
            booking.CancelledAt = DateTime.Now;
            booking.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            await Log(GetAdminId(), "Cancel", booking.BookingId, old, cancelReason);

            TempData["SuccessMessage"] = $"Da huy don <strong>{booking.BookingCode}</strong>.";
            return RedirectToAction(nameof(Index));
        }

        // ═════════════════════════════════════════════════════════════════════
        // EXPORT CSV
        // ═════════════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> ExportCsv(
            string? filter, string? search,
            string? paymentMethod,
            DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.Bookings
                .Include(b => b.Court).ThenInclude(c => c!.Facility)
                .Include(b => b.User)
                .AsQueryable();

            if (filter == "awaiting-transfer")
                query = query.Where(b =>
                    b.PaymentMethod == "Transfer" &&
                    b.PaymentStatus != "Paid" &&
                    b.Status != "Cancelled");
            else if (!string.IsNullOrEmpty(filter))
                query = query.Where(b => b.Status == filter);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(b =>
                    b.BookingCode.ToLower().Contains(s) ||
                    (b.User != null && b.User.FullName.ToLower().Contains(s)));
            }

            if (!string.IsNullOrEmpty(paymentMethod))
                query = query.Where(b => b.PaymentMethod == paymentMethod);
            if (fromDate.HasValue) query = query.Where(b => b.BookingDate >= fromDate.Value.Date);
            if (toDate.HasValue) query = query.Where(b => b.BookingDate <= toDate.Value.Date);

            var rows = await query.OrderByDescending(b => b.CreatedAt).ToListAsync();

            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Ma don,Khach hang,Dien thoai,Co so,San,Ngay choi,Gio,Phuong thuc,Thanh toan,Trang thai,Tong tien");

            foreach (var b in rows)
            {
                csv.AppendLine(string.Join(",",
                    b.BookingCode,
                    $"\"{b.User?.FullName ?? "N/A"}\"",
                    b.User?.Phone ?? "",
                    $"\"{b.Court?.Facility?.FacilityName ?? "N/A"}\"",
                    b.Court?.CourtNumber ?? "",
                    b.BookingDate.ToString("dd/MM/yyyy"),
                    $"{b.StartTime:hh\\:mm}-{b.EndTime:hh\\:mm}",
                    b.PaymentMethod,
                    b.PaymentStatus,
                    b.Status,
                    b.TotalPrice.ToString("N0")));
            }

            byte[] bytes = System.Text.Encoding.UTF8.GetPreamble()
                .Concat(System.Text.Encoding.UTF8.GetBytes(csv.ToString()))
                .ToArray();

            return File(bytes, "text/csv; charset=utf-8",
                $"bookings_{DateTime.Now:yyyyMMdd_HHmm}.csv");
        }

        // ─────────────────────────────────────────────────────────────────────
        // HELPERS
        // ─────────────────────────────────────────────────────────────────────

        private Task<Booking?> LoadFull(int id) =>
            _context.Bookings
                .Include(b => b.Court).ThenInclude(c => c!.Facility)
                .Include(b => b.User)
                .Include(b => b.Orders)
                    .ThenInclude(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                // VoucherUsages được load riêng trong Details() vì bảng không có CreatedAt
                .FirstOrDefaultAsync(b => b.BookingId == id);

        private static BookingHistoryViewModel Map(Booking b, decimal cancelHours) => new()
        {
            BookingId = b.BookingId,
            BookingCode = b.BookingCode,
            CustomerName = b.User?.FullName ?? "N/A",
            Phone = b.User?.Phone ?? "N/A",
            Email = b.User?.Email ?? "N/A",
            FacilityName = b.Court?.Facility?.FacilityName ?? "N/A",
            FacilityAddress = $"{b.Court?.Facility?.Address}, {b.Court?.Facility?.District}, {b.Court?.Facility?.City}",
            CourtNumber = b.Court?.CourtNumber ?? "N/A",
            CourtType = b.Court?.CourtType ?? "N/A",
            CourtTypeLabel = b.Court?.CourtTypeLabel,
            CourtImageUrl = b.Court?.ImagePath,
            BookingDate = b.BookingDate,
            StartTime = b.StartTime,
            EndTime = b.EndTime,
            Duration = b.Duration,
            CourtPrice = b.CourtPrice,
            ServiceFee = b.ServiceFee,
            DiscountAmount = b.DiscountAmount,
            TotalPrice = b.TotalPrice,
            Status = b.Status,
            PaymentStatus = b.PaymentStatus,
            PaymentMethod = b.PaymentMethod,
            Note = b.Note,
            CancelReason = b.CancelReason,
            CreatedAt = b.CreatedAt,
            CancelledAt = b.CancelledAt,
            CanCancel = b.Status is not ("Cancelled" or "Completed"),
            VoucherCode = null, // set riêng trong Details() sau khi query tách
            RelatedOrders = b.Orders?.Select(o => new OrderViewModel
            {
                OrderId = o.OrderId,
                OrderCode = o.OrderCode,
                OrderType = o.OrderType,
                TotalAmount = o.TotalAmount,
                OrderStatus = o.OrderStatus,
                OrderDetails = o.OrderDetails?.Select(od => new OrderDetailViewModel
                {
                    ProductName = od.Product?.ProductName ?? "Khong xac dinh",
                    Quantity = od.Quantity,
                    Unit = od.Product?.Unit ?? "",
                    UnitPrice = od.UnitPrice,
                    TotalPrice = od.TotalPrice
                }).ToList() ?? new()
            }).ToList() ?? new()
        };

        private void SetBankViewBag(string? bookingCode = null)
        {
            const string code = "MB", acc = "0123456789",
                         name = "CONG TY QUAN LI SAN CAU LONG";
            ViewBag.BankCode = code;
            ViewBag.BankAccount = acc;
            ViewBag.BankAccountName = name;
            ViewBag.BankName = "MB Bank";
            if (!string.IsNullOrEmpty(bookingCode))
                ViewBag.QrUrl =
                    $"https://img.vietqr.io/image/{code}-{acc}-compact2.png" +
                    $"?addInfo={Uri.EscapeDataString(bookingCode)}" +
                    $"&accountName={Uri.EscapeDataString(name)}";
        }

        private async Task<decimal> GetSetting(string key, decimal def)
        {
            var s = await _context.SystemSettings
                .FirstOrDefaultAsync(x => x.SettingKey == key && x.IsActive);
            return s != null && decimal.TryParse(s.SettingValue, out var v) ? v : def;
        }

        private async Task Log(int userId, string action, int? recordId,
            string? oldVal, string newVal)
        {
            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = userId,
                Action = action,
                TableName = "Bookings",
                RecordId = recordId,
                OldValue = oldVal ?? "",
                NewValue = newVal,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                UserAgent = HttpContext.Request.Headers["User-Agent"].ToString(),
                CreatedAt = DateTime.Now
            });
            await _context.SaveChangesAsync();
        }

        private int GetAdminId()
        {
            var c = User.FindFirst(ClaimTypes.NameIdentifier);
            return c != null && int.TryParse(c.Value, out var id)
                ? id
                : HttpContext.Session.GetInt32("UserId") ?? 0;
        }
    }
}
