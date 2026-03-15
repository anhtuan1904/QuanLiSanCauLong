using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiSanCauLong.Data;
using QuanLiSanCauLong.Models;
using QuanLiSanCauLong.ViewModels;
using System.Security.Claims;

namespace QuanLiSanCauLong.Controllers
{
    [Authorize(Roles = "Staff")]
    public class StaffController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StaffController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ══════════════════════════════════════════
        // HELPERS
        // ══════════════════════════════════════════
        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out int id) ? id : 0;
        }

        private async Task<int> GetStaffFacilityId()
        {
            var userId = GetCurrentUserId();
            var user = await _context.Users.FindAsync(userId);
            return user?.FacilityId ?? 0;
        }

        private async Task SetStaffViewBag()
        {
            var facilityId = await GetStaffFacilityId();
            var facility = await _context.Facilities.FindAsync(facilityId);
            ViewBag.StaffFacilityId = facilityId;
            ViewBag.StaffFacilityName = facility?.FacilityName ?? "Chưa phân cơ sở";
        }

        private async Task<string> GenerateBookingCode()
        {
            var count = await _context.Bookings.CountAsync(b => b.CreatedAt >= DateTime.Today);
            return "BK" + DateTime.Now.ToString("yyyyMMdd") + (count + 1).ToString("D4");
        }

        private async Task<int> GetOrCreateGuestUser(string phone, string name)
        {
            var existing = await _context.Users.FirstOrDefaultAsync(u => u.Phone == phone);
            if (existing != null) return existing.UserId;

            var guest = new User
            {
                FullName = name,
                Phone = phone,
                Email = $"guest_{phone}@walkin.local",
                Role = "Customer",
                IsActive = true,
                CreatedAt = DateTime.Now
            };
            _context.Users.Add(guest);
            await _context.SaveChangesAsync();
            return guest.UserId;
        }

        // ══════════════════════════════════════════
        // DASHBOARD
        // ══════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            await SetStaffViewBag();
            ViewData["Title"] = "Dashboard";

            var facilityId = await GetStaffFacilityId();
            var today = DateTime.Today;
            var now = DateTime.Now;

            var todayBookings = await _context.Bookings
                .Include(b => b.Court)
                .Include(b => b.User)
                .Where(b => b.Court.FacilityId == facilityId
                         && b.BookingDate == today
                         && b.Status != "Cancelled")
                .OrderBy(b => b.StartTime)
                .ToListAsync();

            var upcoming = todayBookings
                .Where(b => b.Status == "Confirmed"
                         && b.StartTime >= now.TimeOfDay
                         && b.StartTime <= now.TimeOfDay.Add(TimeSpan.FromHours(2)))
                .Take(5).ToList();

            var playing = todayBookings.Where(b => b.Status == "Playing").ToList();

            ViewBag.TodayCount = todayBookings.Count;
            ViewBag.PlayingCount = playing.Count;
            ViewBag.UpcomingCount = upcoming.Count;
            ViewBag.ShiftRevenue = todayBookings
                .Where(b => b.Status is "Completed" or "Playing")
                .Sum(b => b.TotalPrice);
            ViewBag.UpcomingList = upcoming;
            ViewBag.PlayingList = playing;

            return View("~/Views/Staff/Dashboard.cshtml");
        }

        // ── Quick Stats API (sidebar polling) ──
        [HttpGet]
        public async Task<IActionResult> QuickStats()
        {
            var facilityId = await GetStaffFacilityId();
            var today = DateTime.Today;
            var now = DateTime.Now;

            var courts = await _context.Courts
                .Where(c => c.FacilityId == facilityId && c.Status == "Available")
                .CountAsync();

            var bookings = await _context.Bookings
                .Include(b => b.Court)
                .Where(b => b.Court.FacilityId == facilityId
                         && b.BookingDate == today
                         && b.Status != "Cancelled")
                .ToListAsync();

            var playing = bookings.Count(b => b.Status == "Playing");
            var pending = bookings.Count(b => b.Status == "Confirmed"
                             && b.StartTime <= now.TimeOfDay.Add(TimeSpan.FromMinutes(30))
                             && b.StartTime > now.TimeOfDay.Subtract(TimeSpan.FromMinutes(15)));
            var revenue = bookings.Where(b => b.Status is "Completed" or "Playing")
                                   .Sum(b => b.TotalPrice);

            return Json(new
            {
                freeCourts = Math.Max(0, courts - playing),
                playingCourts = playing,
                pendingCheckin = pending,
                todayBookings = bookings.Count,
                shiftRevenue = revenue.ToString("N0") + "đ"
            });
        }

        // ══════════════════════════════════════════
        // SƠ ĐỒ SÂN
        // ══════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> CourtMap()
        {
            await SetStaffViewBag();
            ViewData["Title"] = "Sơ đồ sân";

            var facilityId = await GetStaffFacilityId();
            var today = DateTime.Today;
            var now = DateTime.Now;

            var courts = await _context.Courts
                .Where(c => c.FacilityId == facilityId)
                .OrderBy(c => c.CourtNumber)
                .ToListAsync();

            var bookings = await _context.Bookings
                .Include(b => b.User)
                .Where(b => b.Court.FacilityId == facilityId
                         && b.BookingDate == today
                         && b.Status != "Cancelled")
                .ToListAsync();

            var courtStatusList = courts.Select(c =>
            {
                var active = bookings.FirstOrDefault(b =>
                    b.CourtId == c.CourtId &&
                    (b.Status == "Playing" ||
                    (b.Status == "Confirmed" && b.StartTime <= now.TimeOfDay && b.EndTime > now.TimeOfDay)));

                var next = bookings
                    .Where(b => b.CourtId == c.CourtId
                             && b.Status == "Confirmed"
                             && b.StartTime > now.TimeOfDay)
                    .OrderBy(b => b.StartTime)
                    .FirstOrDefault();

                string statusKey = c.Status != "Available" ? "maintenance"
                    : active != null ? "playing"
                    : next != null && next.StartTime <= now.TimeOfDay.Add(TimeSpan.FromMinutes(30)) ? "incoming"
                    : "free";

                string? countdown = null;
                if (active != null)
                {
                    var rem = today.Add(active.EndTime) - now;
                    countdown = rem.TotalMinutes > 0
                        ? $"{(int)rem.TotalMinutes}p {rem.Seconds:D2}s" : "Hết giờ";
                }

                return new StaffCourtStatusViewModel
                {
                    CourtId = c.CourtId,
                    CourtNumber = c.CourtNumber,
                    CourtType = c.CourtType,
                    Status = statusKey,
                    Countdown = countdown,
                    CustomerName = active?.User?.FullName,
                    CustomerPhone = active?.User?.Phone,
                    StartTime = active?.StartTime.ToString(@"hh\:mm"),
                    EndTime = active?.EndTime.ToString(@"hh\:mm"),
                    BookingId = active?.BookingId,
                    NextTime = next?.StartTime.ToString(@"hh\:mm"),
                    NextName = next?.User?.FullName
                };
            }).ToList();

            return View("~/Views/Staff/CourtMap.cshtml", courtStatusList);
        }

        // ══════════════════════════════════════════
        // QUẢN LÝ ĐƠN
        // ══════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> BookingManagement(string? status, string? search)
        {
            await SetStaffViewBag();
            ViewData["Title"] = "Quản lý đơn hôm nay";

            var facilityId = await GetStaffFacilityId();
            var today = DateTime.Today;

            var query = _context.Bookings
                .Include(b => b.Court)
                .Include(b => b.User)
                .Where(b => b.Court.FacilityId == facilityId && b.BookingDate == today)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(b => b.Status == status);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(b => b.BookingCode.Contains(search)
                                      || b.User.FullName.Contains(search)
                                      || b.User.Phone.Contains(search));

            var list = await query.OrderBy(b => b.StartTime).ToListAsync();

            ViewBag.Status = status;
            ViewBag.Search = search;
            return View("~/Views/Staff/BookingManagement.cshtml", list);
        }

        // ══════════════════════════════════════════
        // WALK-IN BOOKING
        // ══════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> WalkIn()
        {
            await SetStaffViewBag();
            ViewData["Title"] = "Đặt sân tại chỗ";

            var facilityId = await GetStaffFacilityId();
            ViewBag.Courts = await _context.Courts
                .Where(c => c.FacilityId == facilityId && c.Status == "Available")
                .OrderBy(c => c.CourtNumber).ToListAsync();

            return View("~/Views/Staff/WalkIn.cshtml", new WalkInBookingViewModel { BookingDate = DateTime.Today });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> WalkIn(WalkInBookingViewModel model)
        {
            await SetStaffViewBag();
            ViewData["Title"] = "Đặt sân tại chỗ";
            var facilityId = await GetStaffFacilityId();

            if (!ModelState.IsValid)
            {
                ViewBag.Courts = await _context.Courts
                    .Where(c => c.FacilityId == facilityId && c.Status == "Available")
                    .ToListAsync();
                return View("~/Views/Staff/WalkIn.cshtml", model);
            }

            var conflict = await _context.Bookings.AnyAsync(b =>
                b.CourtId == model.CourtId &&
                b.BookingDate == model.BookingDate &&
                b.Status != "Cancelled" &&
                b.StartTime < model.EndTime &&
                b.EndTime > model.StartTime);

            if (conflict)
            {
                ModelState.AddModelError("", "Sân đã có người đặt trong khung giờ này!");
                ViewBag.Courts = await _context.Courts
                    .Where(c => c.FacilityId == facilityId && c.Status == "Available")
                    .ToListAsync();
                return View("~/Views/Staff/WalkIn.cshtml", model);
            }

            var guestId = await GetOrCreateGuestUser(model.CustomerPhone, model.CustomerName);
            var booking = new Booking
            {
                BookingCode = await GenerateBookingCode(),
                UserId = guestId,
                CourtId = model.CourtId,
                BookingDate = model.BookingDate,
                StartTime = model.StartTime,
                EndTime = model.EndTime,
                Duration = (int)(model.EndTime - model.StartTime).TotalMinutes,
                CourtPrice = model.Price,
                ServiceFee = 0,
                DiscountAmount = 0,
                TotalPrice = model.Price,
                Status = "Confirmed",
                PaymentMethod = model.PaymentMethod ?? "Cash",
                PaymentStatus = model.PaymentMethod == "Cash" ? "Paid" : "Unpaid",
                Note = $"[Walk-in] Staff: {User.Identity?.Name}. {model.Note}",
                CancelReason = null,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đặt sân thành công! Mã: {booking.BookingCode}";
            return RedirectToAction(nameof(BookingManagement));
        }

        // ══════════════════════════════════════════
        // CHECK-IN / CHECK-OUT
        // ══════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> CheckIn()
        {
            await SetStaffViewBag();
            ViewData["Title"] = "Check-in / Check-out";
            return View("~/Views/Staff/CheckIn.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> FindBooking(string keyword)
        {
            var facilityId = await GetStaffFacilityId();
            var today = DateTime.Today;

            var b = await _context.Bookings
                .Include(b => b.Court)
                .Include(b => b.User)
                .FirstOrDefaultAsync(b =>
                    b.Court.FacilityId == facilityId &&
                    b.BookingDate == today &&
                    b.Status != "Cancelled" &&
                    (b.BookingCode == keyword || b.User.Phone == keyword));

            if (b == null)
                return Json(new { success = false, message = "Không tìm thấy đơn!" });

            return Json(new
            {
                success = true,
                bookingId = b.BookingId,
                bookingCode = b.BookingCode,
                customerName = b.User?.FullName ?? "Khách",
                phone = b.User?.Phone,
                courtNumber = b.Court?.CourtNumber,
                startTime = b.StartTime.ToString(@"hh\:mm"),
                endTime = b.EndTime.ToString(@"hh\:mm"),
                status = b.Status,
                totalPrice = b.TotalPrice.ToString("N0")
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DoCheckIn(int bookingId)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null)
                return Json(new { success = false, message = "Không tìm thấy đơn!" });
            if (booking.Status != "Confirmed")
                return Json(new { success = false, message = "Đơn không ở trạng thái Confirmed!" });

            booking.Status = "Playing";
            booking.CheckInTime = DateTime.Now;
            booking.CheckInBy = GetCurrentUserId();
            booking.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Check-in thành công! Chúc khách chơi vui 🏸" });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DoCheckOut(int bookingId)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null)
                return Json(new { success = false, message = "Không tìm thấy đơn!" });
            if (booking.Status != "Playing")
                return Json(new { success = false, message = "Sân chưa check-in!" });

            booking.Status = "Completed";
            booking.CheckOutTime = DateTime.Now;
            booking.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Check-out thành công!" });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ExtendBooking(int bookingId, int extraMinutes)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null)
                return Json(new { success = false, message = "Không tìm thấy đơn!" });
            if (booking.Status != "Playing")
                return Json(new { success = false, message = "Sân chưa check-in!" });

            var newEnd = booking.EndTime.Add(TimeSpan.FromMinutes(extraMinutes));
            var conflict = await _context.Bookings.AnyAsync(b =>
                b.CourtId == booking.CourtId &&
                b.BookingDate == booking.BookingDate &&
                b.BookingId != bookingId &&
                b.Status != "Cancelled" &&
                b.StartTime < newEnd &&
                b.EndTime > booking.EndTime);

            if (conflict)
                return Json(new { success = false, message = "Sân đã có người đặt sau khung giờ này!" });

            booking.EndTime = newEnd;
            booking.Duration += extraMinutes;
            booking.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = $"Đã gia hạn thêm {extraMinutes} phút!", newEndTime = newEnd.ToString(@"hh\:mm") });
        }

        // ══════════════════════════════════════════
        // BÁN HÀNG POS
        // ══════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Sales()
        {
            await SetStaffViewBag();
            ViewData["Title"] = "Bán hàng POS";

            var facilityId = await GetStaffFacilityId();
            var today = DateTime.Today;

            ViewBag.Products = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive)
                .OrderBy(p => p.Category.CategoryType)
                .ThenBy(p => p.ProductName)
                .ToListAsync();

            ViewBag.ActiveBookings = await _context.Bookings
                .Include(b => b.Court)
                .Include(b => b.User)
                .Where(b => b.Court.FacilityId == facilityId
                         && b.BookingDate == today
                         && (b.Status == "Playing" || b.Status == "Confirmed"))
                .OrderBy(b => b.StartTime)
                .ToListAsync();

            return View("~/Views/Staff/Sales.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> OrderHistory()
        {
            await SetStaffViewBag();
            ViewData["Title"] = "Lịch sử bán hàng";

            var facilityId = await GetStaffFacilityId();
            var orders = await _context.Orders
                .Include(o => o.OrderDetails).ThenInclude(od => od.Product)
                .Include(o => o.User)
                .Where(o => o.FacilityId == facilityId && o.CreatedAt.Date == DateTime.Today)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return View("~/Views/Staff/OrderHistory.cshtml", orders);
        }

        // ══════════════════════════════════════════
        // TỒN KHO (chỉ đọc)
        // ══════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> StockView()
        {
            await SetStaffViewBag();
            ViewData["Title"] = "Xem tồn kho";

            var facilityId = await GetStaffFacilityId();
            var inventories = await _context.Inventories
                .Include(i => i.Product).ThenInclude(p => p.Category)
                .Where(i => i.FacilityId == facilityId)
                .OrderBy(i => i.Product.Category.CategoryName)
                .ThenBy(i => i.Product.ProductName)
                .ToListAsync();

            return View("~/Views/Staff/StockView.cshtml", inventories);
        }

        // ══════════════════════════════════════════
        // LỊCH CA — dùng ShiftAssignments (DB có Shifts + ShiftAssignments)
        // ══════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> MyShift()
        {
            await SetStaffViewBag();
            ViewData["Title"] = "Lịch ca của tôi";

            var userId = GetCurrentUserId();
            var from = DateTime.Today;
            var to = DateTime.Today.AddDays(14);

            // Load tất cả ca của user, filter in-memory để tránh lỗi type mismatch
            var assignments = await _context.ShiftAssignments
                .Include(sa => sa.Shift)
                .Where(sa => sa.UserId == userId && sa.Shift != null)
                .ToListAsync();

            // Filter 14 ngày tới in-memory
            assignments = assignments
                .Where(sa => sa.Shift != null)
                .OrderBy(sa => sa.Shift.StartTime)
                .ToList();

            return View("~/Views/Staff/MyShift.cshtml", assignments);
        }

        // ══════════════════════════════════════════
        // BÀN GIAO CA
        // ══════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> ShiftHandover()
        {
            await SetStaffViewBag();
            ViewData["Title"] = "Bàn giao ca";

            var facilityId = await GetStaffFacilityId();
            var today = DateTime.Today;

            var bookings = await _context.Bookings
                .Include(b => b.Court)
                .Where(b => b.Court.FacilityId == facilityId && b.BookingDate == today)
                .ToListAsync();

            ViewBag.TotalBookings = bookings.Count(b => b.Status != "Cancelled");
            ViewBag.CompletedCount = bookings.Count(b => b.Status == "Completed");
            ViewBag.TotalRevenue = bookings.Where(b => b.Status is "Completed" or "Playing").Sum(b => b.TotalPrice);
            ViewBag.CashRevenue = bookings.Where(b => b.PaymentMethod == "Cash" && b.PaymentStatus == "Paid").Sum(b => b.TotalPrice);

            return View("~/Views/Staff/ShiftHandover.cshtml", new ShiftHandoverViewModel());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ShiftHandover(ShiftHandoverViewModel model)
        {
            // TODO: lưu vào bảng ShiftHandover sau
            TempData["SuccessMessage"] = "Bàn giao ca thành công!";
            return RedirectToAction(nameof(Dashboard));
        }

        // ══════════════════════════════════════════
        // BÁO CÁO CA
        // ══════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> ShiftReport()
        {
            await SetStaffViewBag();
            ViewData["Title"] = "Báo cáo ca";

            var facilityId = await GetStaffFacilityId();
            var bookings = await _context.Bookings
                .Include(b => b.Court)
                .Include(b => b.User)
                .Where(b => b.Court.FacilityId == facilityId && b.BookingDate == DateTime.Today)
                .OrderBy(b => b.StartTime)
                .ToListAsync();

            ViewBag.TotalBookings = bookings.Count(b => b.Status != "Cancelled");
            ViewBag.CompletedCount = bookings.Count(b => b.Status == "Completed");
            ViewBag.CancelledCount = bookings.Count(b => b.Status == "Cancelled");
            ViewBag.Revenue = bookings.Where(b => b.Status is "Completed" or "Playing").Sum(b => b.TotalPrice);

            return View("~/Views/Staff/ShiftReport.cshtml", bookings);
        }

        // ══════════════════════════════════════════
        // BÁO SỰ CỐ
        // ══════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Incident()
        {
            await SetStaffViewBag();
            ViewData["Title"] = "Báo sự cố sân";

            var facilityId = await GetStaffFacilityId();
            ViewBag.Courts = await _context.Courts
                .Where(c => c.FacilityId == facilityId)
                .OrderBy(c => c.CourtNumber).ToListAsync();

            return View("~/Views/Staff/Incident.cshtml", new IncidentReportViewModel());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Incident(IncidentReportViewModel model)
        {
            await SetStaffViewBag();
            var facilityId = await GetStaffFacilityId();

            if (!ModelState.IsValid)
            {
                ViewBag.Courts = await _context.Courts
                    .Where(c => c.FacilityId == facilityId).ToListAsync();
                return View("~/Views/Staff/Incident.cshtml", model);
            }

            // Sự cố nghiêm trọng → tự set bảo trì
            if (model.Severity == "Critical" && model.CourtId.HasValue)
            {
                var court = await _context.Courts.FindAsync(model.CourtId.Value);
                if (court != null) { court.Status = "Maintenance"; await _context.SaveChangesAsync(); }
            }

            TempData["SuccessMessage"] = "Đã gửi báo cáo sự cố! Admin sẽ xử lý sớm.";
            return RedirectToAction(nameof(Dashboard));
        }
    }

    // ── Request model cho SubmitOrder ──
    public class SubmitOrderRequest
    {
        public int? BookingId { get; set; }
        public string PaymentMethod { get; set; } = "Cash";
        public List<SubmitOrderItem> Items { get; set; } = new();
    }
    public class SubmitOrderItem
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }

}