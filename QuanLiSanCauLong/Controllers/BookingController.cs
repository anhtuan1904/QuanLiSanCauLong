using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiSanCauLong.Data;
using QuanLiSanCauLong.Models;
using QuanLiSanCauLong.ViewModels;
using System.Security.Claims;

namespace QuanLiSanCauLong.Controllers
{
    [Authorize]
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _context;

        // ── Cấu hình thanh toán ──
        private const string BankCode = "MB";
        private const string BankAccount = "0123456789";
        private const string BankAccountName = "CONG TY QUAN LI SAN CAU LONG";
        private const string BankName = "MB Bank";

        public BookingController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ─────────────────────────────────────────────────────────────────────
        // HELPER: ViewBag ngân hàng + QR
        // ─────────────────────────────────────────────────────────────────────
        private void SetBankViewBag(string? bookingCode = null)
        {
            ViewBag.BankCode = BankCode;
            ViewBag.BankAccount = BankAccount;
            ViewBag.BankAccountName = BankAccountName;
            ViewBag.BankName = BankName;

            if (!string.IsNullOrEmpty(bookingCode))
            {
                string content = Uri.EscapeDataString(bookingCode);
                string name = Uri.EscapeDataString(BankAccountName);
                ViewBag.QrUrl = $"https://img.vietqr.io/image/{BankCode}-{BankAccount}-compact2.png" +
                                  $"?addInfo={content}&accountName={name}";
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        // SEARCH — tìm sân theo cơ sở
        // ═════════════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Search(int facilityId)
        {
            var facility = await _context.Facilities
                .Include(f => f.FacilityImages)
                .FirstOrDefaultAsync(f => f.FacilityId == facilityId && f.IsActive);

            if (facility == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy cơ sở!";
                return RedirectToAction("Index", "Facility");
            }

            // Lấy rating thật từ CourtReviews
            var ratingData = await _context.CourtReviews
                .Where(r => r.Court != null && r.Court.FacilityId == facilityId && r.Status == "Approved")
                .GroupBy(r => r.Court!.FacilityId)
                .Select(g => new { Avg = g.Average(r => (double)r.Rating), Count = g.Count() })
                .FirstOrDefaultAsync();

            // Amenities list
            var amenities = string.IsNullOrEmpty(facility.Amenities)
                ? new List<string>()
                : facility.Amenities.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                    .Select(a => a.Trim()).ToList();

            // Gallery images
            var images = facility.FacilityImages.Any()
                ? facility.FacilityImages.OrderBy(i => i.DisplayOrder).Select(i => i.ImagePath).ToList()
                : new List<string> { facility.ImageUrl ?? "/images/default-facility.jpg" };

            ViewBag.FacilityId = facility.FacilityId;
            ViewBag.FacilityName = facility.FacilityName;
            ViewBag.FacilityAddress = $"{facility.Address}, {facility.District}, {facility.City}";
            ViewBag.FacilityPhone = facility.Phone;
            ViewBag.FacilityImages = images;
            ViewBag.FacilityImage = images.FirstOrDefault() ?? "/images/default-facility.jpg";
            ViewBag.OpenTime = facility.OpenTime?.ToString(@"hh\:mm") ?? "06:00";
            ViewBag.CloseTime = facility.CloseTime?.ToString(@"hh\:mm") ?? "23:00";
            ViewBag.Description = facility.Description;
            ViewBag.Amenities = amenities;
            ViewBag.AverageRating = ratingData?.Avg ?? 0;
            ViewBag.ReviewCount = ratingData?.Count ?? 0;
            ViewBag.TotalCourts = await _context.Courts
                .CountAsync(c => c.FacilityId == facilityId && c.Status == "Available");

            return View();
        }

        // ─────────────────────────────────────────────────────────────────────
        // API: Tìm sân trống (Ajax POST)
        // ─────────────────────────────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> SearchAvailableCourts([FromBody] SearchCourtRequest request)
        {
            try
            {
                if (request.FacilityId <= 0 || request.BookingDate == default)
                    return Json(new { success = false, message = "Vui lòng chọn đầy đủ thông tin!" });

                if (!TimeSpan.TryParse(request.StartTime, out TimeSpan searchTime))
                    return Json(new { success = false, message = "Khung giờ không hợp lệ!" });

                var courts = await _context.Courts
                    .Include(c => c.CourtImages)
                    .Where(c => c.FacilityId == request.FacilityId && c.Status == "Available")
                    .ToListAsync();

                var availableCourts = new List<object>();
                foreach (var court in courts)
                {
                    var timeSlots = await GetAvailableTimeSlots(court, request.BookingDate);
                    var matchingSlot = timeSlots.FirstOrDefault(t =>
                        t.IsAvailable && t.StartTime <= searchTime && t.EndTime > searchTime);

                    if (matchingSlot != null)
                    {
                        // Lấy ảnh sân
                        var courtImg = court.CourtImages?.OrderBy(i => i.IsPrimary ? 0 : 1)
                                           .FirstOrDefault()?.ImagePath
                                    ?? court.ImagePath;

                        availableCourts.Add(new
                        {
                            courtId = court.CourtId,
                            courtNumber = court.CourtNumber,
                            courtType = court.CourtType,
                            courtTypeLabel = court.CourtTypeLabel,
                            startTime = matchingSlot.StartTime.ToString(@"hh\:mm"),
                            endTime = matchingSlot.EndTime.ToString(@"hh\:mm"),
                            price = matchingSlot.Price,
                            isPeakHour = matchingSlot.IsPeakHour,
                            hasLighting = court.HasLighting,
                            hasAC = court.HasAC,
                            floorNumber = court.FloorNumber,
                            description = court.Description,
                            imageUrl = courtImg,
                            // Đếm số slot trống còn lại trong ngày
                            availableSlots = timeSlots.Count(t => t.IsAvailable)
                        });
                    }
                }

                return Json(new
                {
                    success = true,
                    courts = availableCourts,
                    message = $"Tìm thấy {availableCourts.Count} sân trống"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // API: Lấy tất cả khung giờ của 1 sân trong 1 ngày (Ajax GET)
        // NEW — dùng cho calendar picker và hiển thị slot trên Search
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetTimeSlots(int courtId, DateTime date)
        {
            var court = await _context.Courts.FindAsync(courtId);
            if (court == null)
                return Json(new { success = false, message = "Không tìm thấy sân!" });

            var slots = await GetAvailableTimeSlots(court, date);

            return Json(new
            {
                success = true,
                slots = slots.Select(s => new
                {
                    startTime = s.StartTime.ToString(@"hh\:mm"),
                    endTime = s.EndTime.ToString(@"hh\:mm"),
                    price = s.Price,
                    isPeakHour = s.IsPeakHour,
                    isAvailable = s.IsAvailable
                })
            });
        }

        // ═════════════════════════════════════════════════════════════════════
        // CREATE — xác nhận & tạo booking
        // ═════════════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Create(int courtId, DateTime bookingDate, string startTime, string endTime)
        {
            var court = await _context.Courts
                .Include(c => c.Facility)
                .Include(c => c.CourtImages)
                .FirstOrDefaultAsync(c => c.CourtId == courtId);

            if (court == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy sân!";
                return RedirectToAction("Index", "Facility");
            }

            var start = TimeSpan.Parse(startTime);
            var end = TimeSpan.Parse(endTime);
            var duration = (int)(end - start).TotalMinutes;

            var courtPrice = await CalculateCourtPrice(court.FacilityId, court.CourtType, start, end);
            var serviceFee = await GetSystemSetting("DefaultServiceFee", 0);
            var serviceFeeAmt = courtPrice * serviceFee / 100;

            // Ảnh sân: ưu tiên CourtImages, fallback ImagePath
            var courtImg = court.CourtImages?.OrderBy(i => i.IsPrimary ? 0 : 1)
                               .FirstOrDefault()?.ImagePath
                         ?? court.ImagePath;

            var model = new CreateBookingViewModel
            {
                CourtId = courtId,
                BookingDate = bookingDate,
                StartTime = start,
                EndTime = end,
                Duration = duration,
                FacilityName = court.Facility!.FacilityName,
                FacilityAddress = $"{court.Facility.Address}, {court.Facility.District}, {court.Facility.City}",
                CourtNumber = court.CourtNumber,
                CourtType = court.CourtType,
                CourtDescription = court.Description,
                CourtImageUrl = courtImg,
                CourtPrice = courtPrice,
                ServiceFee = serviceFeeAmt,
                DiscountAmount = 0,
                TotalPrice = courtPrice + serviceFeeAmt,
                OrderItems = new List<OrderItemViewModel>()
            };

            await LoadProductsForBooking(court.FacilityId);
            SetBankViewBag();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateBookingViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var courtInfo = await _context.Courts.Include(c => c.Facility)
                    .FirstOrDefaultAsync(c => c.CourtId == model.CourtId);
                if (courtInfo != null) await LoadProductsForBooking(courtInfo.FacilityId);
                SetBankViewBag();
                return View(model);
            }

            int userId = GetCurrentUserId();
            var court = await _context.Courts.FindAsync(model.CourtId);

            if (court == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy sân!";
                return RedirectToAction("Index", "Facility");
            }

            if (!await IsCourtAvailable(model.CourtId, model.BookingDate, model.StartTime, model.EndTime))
            {
                TempData["ErrorMessage"] = "Rất tiếc, sân vừa mới được người khác đặt trong khung giờ này!";
                return RedirectToAction(nameof(Search), new { facilityId = court.FacilityId });
            }

            decimal serverCourtPrice = await CalculateCourtPrice(court.FacilityId, court.CourtType, model.StartTime, model.EndTime);
            decimal serverServiceFee = serverCourtPrice * (await GetSystemSetting("DefaultServiceFee", 0)) / 100;
            decimal baseTotal = serverCourtPrice + serverServiceFee;

            var strategy = _context.Database.CreateExecutionStrategy();
            IActionResult actionResult = RedirectToAction(nameof(MyBookings));

            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    decimal voucherDiscount = 0;
                    int? voucherId = null;
                    if (!string.IsNullOrEmpty(model.VoucherCode))
                    {
                        var vRes = await ApplyVoucher(model.VoucherCode, userId, baseTotal, "Booking");
                        if (vRes.IsValid)
                        {
                            voucherDiscount = vRes.DiscountAmount;
                            voucherId = vRes.VoucherId;
                        }
                        else
                        {
                            TempData["WarnMessage"] = "Mã voucher không hợp lệ hoặc không áp dụng được, đơn vẫn được tạo.";
                        }
                    }

                    string paymentMethod = model.PaymentMethod ?? "Cash";

                    var booking = new Booking
                    {
                        BookingCode = await GenerateBookingCode(),
                        UserId = userId,
                        CourtId = model.CourtId,
                        BookingDate = model.BookingDate,
                        StartTime = model.StartTime,
                        EndTime = model.EndTime,
                        Duration = model.Duration,
                        CourtPrice = serverCourtPrice,
                        ServiceFee = serverServiceFee,
                        DiscountAmount = voucherDiscount,
                        TotalPrice = baseTotal - voucherDiscount,
                        Status = "Pending",
                        PaymentMethod = paymentMethod,
                        PaymentStatus = "Unpaid",
                        Note = model.Note,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };

                    _context.Bookings.Add(booking);
                    await _context.SaveChangesAsync();

                    if (voucherId.HasValue)
                        await SaveVoucherUsage(voucherId.Value, userId, booking.BookingId, null, voucherDiscount);

                    if (model.OrderItems != null && model.OrderItems.Any(i => i.Quantity > 0))
                        await CreateProductOrder(booking, userId, model.OrderItems);

                    await LogActivity(userId, "Create", "Bookings", booking.BookingId, null,
                        $"Mã: {booking.BookingCode} | PT: {paymentMethod}");

                    await transaction.CommitAsync();

                    TempData["SuccessMessage"] = paymentMethod == "Transfer"
                        ? $"Đặt sân thành công! Vui lòng chuyển khoản nội dung: <strong>{booking.BookingCode}</strong>"
                        : "Đặt sân thành công!";

                    actionResult = RedirectToAction(nameof(Details), new { id = booking.BookingId });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "Lỗi khi tạo đặt sân: " + (ex.InnerException?.Message ?? ex.Message);
                    actionResult = RedirectToAction(nameof(MyBookings));
                }
            });

            return actionResult;
        }

        // ═════════════════════════════════════════════════════════════════════
        // MY BOOKINGS — lịch sử đặt sân
        // ═════════════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> MyBookings(string? status, DateTime? fromDate, DateTime? toDate, int page = 1)
        {
            int userId = GetCurrentUserId();
            int pageSize = 8;
            var cancelHours = await GetSystemSetting("BookingCancellationHours", 2);

            var query = _context.Bookings
                .Include(b => b.Court).ThenInclude(c => c.Facility)
                .Include(b => b.Orders).ThenInclude(o => o.OrderDetails).ThenInclude(od => od.Product)
                .Where(b => b.UserId == userId);

            if (!string.IsNullOrEmpty(status)) query = query.Where(b => b.Status == status);
            if (fromDate.HasValue) query = query.Where(b => b.BookingDate >= fromDate.Value);
            if (toDate.HasValue) query = query.Where(b => b.BookingDate <= toDate.Value);

            var total = await query.CountAsync();
            var bookings = await query.OrderByDescending(b => b.CreatedAt)
                                      .Skip((page - 1) * pageSize)
                                      .Take(pageSize)
                                      .ToListAsync();

            var model = bookings.Select(b => MapToHistoryViewModel(b, cancelHours)).ToList();

            // ── Stats tổng hợp ──
            var allUserBookings = await _context.Bookings
                .Where(b => b.UserId == userId)
                .Select(b => new { b.Status, b.TotalPrice })
                .ToListAsync();

            ViewBag.TotalCount = allUserBookings.Count;
            ViewBag.CompletedCount = allUserBookings.Count(b => b.Status == "Completed");
            ViewBag.PendingCount = allUserBookings.Count(b => b.Status is "Pending" or "Confirmed");
            ViewBag.TotalSpent = allUserBookings.Where(b => b.Status != "Cancelled").Sum(b => b.TotalPrice);

            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPage = (int)Math.Ceiling((double)total / pageSize);
            ViewBag.Status = status;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

            return View(model);
        }

        // ═════════════════════════════════════════════════════════════════════
        // DETAILS
        // ═════════════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Court).ThenInclude(c => c.Facility)
                .Include(b => b.User)
                .Include(b => b.Orders).ThenInclude(o => o.OrderDetails).ThenInclude(od => od.Product)
                .Include(b => b.VoucherUsages).ThenInclude(v => v.Voucher)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null) return NotFound();

            int userId = GetCurrentUserId();
            string role = GetCurrentUserRole();
            if (role != "Admin" && role != "Staff" && booking.UserId != userId) return Forbid();

            // Kiểm tra xem đã đánh giá chưa
            var existingReview = await _context.CourtReviews
                .FirstOrDefaultAsync(r => r.CourtId == booking.CourtId && r.UserId == userId);

            var cancelHours = await GetSystemSetting("BookingCancellationHours", 2);
            SetBankViewBag(booking.BookingCode);

            var vm = MapToHistoryViewModel(booking, cancelHours);
            ViewBag.HasReview = existingReview != null;
            ViewBag.ReviewId = existingReview?.ReviewId;
            ViewBag.CanReview = booking.Status == "Completed" && existingReview == null;

            return View(vm);
        }

        // ═════════════════════════════════════════════════════════════════════
        // INVOICE
        // ═════════════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Invoice(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Court).ThenInclude(c => c.Facility)
                .Include(b => b.User)
                .Include(b => b.Orders).ThenInclude(o => o.OrderDetails).ThenInclude(od => od.Product)
                .Include(b => b.VoucherUsages).ThenInclude(v => v.Voucher)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null) return NotFound();

            int userId = GetCurrentUserId();
            string role = GetCurrentUserRole();
            if (role != "Admin" && role != "Staff" && booking.UserId != userId) return Forbid();

            var cancelHours = await GetSystemSetting("BookingCancellationHours", 2);
            SetBankViewBag(booking.BookingCode);
            return View(MapToHistoryViewModel(booking, cancelHours));
        }

        // ═════════════════════════════════════════════════════════════════════
        // CANCEL
        // ═════════════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Cancel(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Court).ThenInclude(c => c.Facility)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null) return NotFound();
            if (booking.UserId != GetCurrentUserId()) return Forbid();

            var cancelHours = await GetSystemSetting("BookingCancellationHours", 2);
            if (!CanCancelBooking(booking, cancelHours))
            {
                TempData["ErrorMessage"] = "Không thể hủy đơn này do đã quá thời hạn!";
                return RedirectToAction(nameof(Details), new { id });
            }

            return View(MapToHistoryViewModel(booking, cancelHours));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, string cancelReason)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();
            if (booking.UserId != GetCurrentUserId()) return Forbid();

            var cancelHours = await GetSystemSetting("BookingCancellationHours", 2);
            if (!CanCancelBooking(booking, cancelHours))
            {
                TempData["ErrorMessage"] = "Không thể hủy đơn này!";
                return RedirectToAction(nameof(MyBookings));
            }

            booking.Status = "Cancelled";
            booking.CancelReason = cancelReason;
            booking.CancelledAt = DateTime.Now;
            booking.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            await LogActivity(GetCurrentUserId(), "Cancel", "Bookings", booking.BookingId,
                booking.Status, $"Hủy: {cancelReason}");

            TempData["SuccessMessage"] = "Đã hủy đặt sân thành công!";
            return RedirectToAction(nameof(MyBookings));
        }

        // ═════════════════════════════════════════════════════════════════════
        // CONFIRM PAYMENT — NEW
        // Khách xác nhận đã chuyển khoản → booking chuyển sang Processing
        // ═════════════════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmPayment(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();
            if (booking.UserId != GetCurrentUserId()) return Forbid();

            if (booking.PaymentStatus != "Unpaid")
                return Json(new { success = false, message = "Đơn đã được xử lý!" });

            booking.PaymentStatus = "Processing"; // Admin xác nhận sau
            booking.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            await LogActivity(GetCurrentUserId(), "ConfirmPayment", "Bookings", booking.BookingId,
                "Unpaid", "Processing");

            TempData["SuccessMessage"] = "Đã xác nhận chuyển khoản! Chúng tôi sẽ xác nhận đơn trong thời gian sớm nhất.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // ═════════════════════════════════════════════════════════════════════
        // RESCHEDULE — NEW
        // Cho phép đổi lịch nếu booking còn trong trạng thái Pending/Confirmed
        // ═════════════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Reschedule(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Court).ThenInclude(c => c.Facility)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null) return NotFound();
            if (booking.UserId != GetCurrentUserId()) return Forbid();

            if (booking.Status != "Pending" && booking.Status != "Confirmed")
            {
                TempData["ErrorMessage"] = "Chỉ có thể đổi lịch đơn đang Chờ xác nhận hoặc Đã xác nhận!";
                return RedirectToAction(nameof(Details), new { id });
            }

            var cancelHours = await GetSystemSetting("BookingCancellationHours", 2);
            var bookingVM = MapToHistoryViewModel(booking, cancelHours);

            ViewBag.OpenTime = booking.Court?.Facility?.OpenTime?.ToString(@"hh\:mm") ?? "06:00";
            ViewBag.CloseTime = booking.Court?.Facility?.CloseTime?.ToString(@"hh\:mm") ?? "23:00";

            return View(bookingVM);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reschedule(int id, DateTime newDate, string newStartTime, string newEndTime)
        {
            var booking = await _context.Bookings
                .Include(b => b.Court)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null) return NotFound();
            if (booking.UserId != GetCurrentUserId()) return Forbid();

            if (!TimeSpan.TryParse(newStartTime, out TimeSpan start) ||
                !TimeSpan.TryParse(newEndTime, out TimeSpan end))
            {
                TempData["ErrorMessage"] = "Khung giờ không hợp lệ!";
                return RedirectToAction(nameof(Reschedule), new { id });
            }

            // Kiểm tra ngày mới phải trong tương lai
            var newDateTime = newDate.Date.Add(start);
            if (newDateTime <= DateTime.Now.AddHours(2))
            {
                TempData["ErrorMessage"] = "Phải đổi lịch ít nhất 2 giờ trước giờ chơi!";
                return RedirectToAction(nameof(Reschedule), new { id });
            }

            // Kiểm tra sân trống ở lịch mới (bỏ qua booking hiện tại)
            var conflict = await _context.Bookings.AnyAsync(b =>
                b.CourtId == booking.CourtId
                && b.BookingId != booking.BookingId
                && b.BookingDate.Date == newDate.Date
                && b.Status != "Cancelled"
                && b.StartTime < end
                && b.EndTime > start);

            if (conflict)
            {
                TempData["ErrorMessage"] = "Khung giờ mới đã có người đặt, vui lòng chọn lại!";
                return RedirectToAction(nameof(Reschedule), new { id });
            }

            // Tính lại giá theo lịch mới
            var newPrice = await CalculateCourtPrice(booking.Court!.FacilityId, booking.Court.CourtType, start, end);
            var svcFee = await GetSystemSetting("DefaultServiceFee", 0);

            string oldInfo = $"{booking.BookingDate:dd/MM/yyyy} {booking.StartTime:hh\\:mm}-{booking.EndTime:hh\\:mm}";

            booking.BookingDate = newDate;
            booking.StartTime = start;
            booking.EndTime = end;
            booking.Duration = (int)(end - start).TotalMinutes;
            booking.CourtPrice = newPrice;
            booking.ServiceFee = newPrice * svcFee / 100;
            booking.TotalPrice = booking.CourtPrice + booking.ServiceFee - booking.DiscountAmount;
            booking.Status = "Pending"; // reset về Pending để admin xác nhận lại
            booking.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            await LogActivity(GetCurrentUserId(), "Reschedule", "Bookings", booking.BookingId,
                oldInfo, $"{newDate:dd/MM/yyyy} {start:hh\\:mm}-{end:hh\\:mm}");

            TempData["SuccessMessage"] = $"Đã đổi lịch thành công sang {newDate:dd/MM/yyyy} {start:hh\\:mm}-{end:hh\\:mm}!";
            return RedirectToAction(nameof(Details), new { id });
        }

        // ═════════════════════════════════════════════════════════════════════
        // RATE — NEW
        // Đánh giá sau khi hoàn thành
        // ═════════════════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rate(int bookingId, int rating, string comment)
        {
            var booking = await _context.Bookings
                .Include(b => b.Court)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId);

            if (booking == null) return NotFound();

            int userId = GetCurrentUserId();
            if (booking.UserId != userId) return Forbid();

            if (booking.Status != "Completed")
            {
                TempData["ErrorMessage"] = "Chỉ có thể đánh giá sau khi hoàn thành buổi chơi!";
                return RedirectToAction(nameof(Details), new { id = bookingId });
            }

            // Kiểm tra đã đánh giá chưa
            var exists = await _context.CourtReviews
                .AnyAsync(r => r.CourtId == booking.CourtId && r.UserId == userId);

            if (exists)
            {
                TempData["ErrorMessage"] = "Bạn đã đánh giá sân này rồi!";
                return RedirectToAction(nameof(Details), new { id = bookingId });
            }

            if (rating < 1 || rating > 5)
            {
                TempData["ErrorMessage"] = "Điểm đánh giá phải từ 1 đến 5!";
                return RedirectToAction(nameof(Details), new { id = bookingId });
            }

            _context.CourtReviews.Add(new CourtReview
            {
                CourtId = booking.CourtId,
                UserId = userId,
                Rating = rating,
                Content = comment?.Trim(),
                Status = "Approved", // Auto-approve hoặc "Pending" nếu cần duyệt
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
            await LogActivity(userId, "Rate", "CourtReviews", booking.CourtId, null,
                $"Rating {rating}★ cho sân bookingId={bookingId}");

            TempData["SuccessMessage"] = $"Cảm ơn bạn đã đánh giá {rating}★!";
            return RedirectToAction(nameof(Details), new { id = bookingId });
        }

        // ═════════════════════════════════════════════════════════════════════
        // GET BOOKING STATS — NEW (Ajax)
        // Trả về stats tổng hợp cho dashboard khách hàng
        // ═════════════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> GetStats()
        {
            int userId = GetCurrentUserId();

            var bookings = await _context.Bookings
                .Where(b => b.UserId == userId)
                .Select(b => new { b.Status, b.TotalPrice, b.BookingDate, b.CourtId })
                .ToListAsync();

            // Favourite court
            var favCourtId = bookings
                .Where(b => b.Status != "Cancelled")
                .GroupBy(b => b.CourtId)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault()?.Key;

            string? favCourt = null;
            if (favCourtId.HasValue)
            {
                var court = await _context.Courts
                    .Include(c => c.Facility)
                    .FirstOrDefaultAsync(c => c.CourtId == favCourtId.Value);
                favCourt = court != null ? $"{court.Facility?.FacilityName} – {court.CourtNumber}" : null;
            }

            return Json(new
            {
                total = bookings.Count,
                completed = bookings.Count(b => b.Status == "Completed"),
                pending = bookings.Count(b => b.Status is "Pending" or "Confirmed"),
                cancelled = bookings.Count(b => b.Status == "Cancelled"),
                totalSpent = bookings.Where(b => b.Status != "Cancelled").Sum(b => b.TotalPrice),
                favCourt
            });
        }

        // ═════════════════════════════════════════════════════════════════════
        // INDEX — redirect về MyBookings
        // ═════════════════════════════════════════════════════════════════════
        [HttpGet]
        public IActionResult Index() => RedirectToAction(nameof(MyBookings));

        // ─────────────────────────────────────────────────────────────────────
        // PRIVATE HELPERS
        // ─────────────────────────────────────────────────────────────────────

        private BookingHistoryViewModel MapToHistoryViewModel(Booking b, decimal cancelHours)
        {
            return new BookingHistoryViewModel
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
                CanCancel = CanCancelBooking(b, cancelHours),
                VoucherCode = b.VoucherUsages?.FirstOrDefault()?.Voucher?.VoucherCode,
                RelatedOrders = b.Orders?.Select(o => new OrderViewModel
                {
                    OrderId = o.OrderId,
                    OrderCode = o.OrderCode,
                    OrderType = o.OrderType,
                    TotalAmount = o.TotalAmount,
                    OrderStatus = o.OrderStatus,
                    OrderDetails = o.OrderDetails?.Select(od => new OrderDetailViewModel
                    {
                        ProductName = od.Product?.ProductName ?? "Sản phẩm không xác định",
                        Quantity = od.Quantity,
                        Unit = od.Product?.Unit ?? "",
                        UnitPrice = od.UnitPrice,
                        TotalPrice = od.TotalPrice
                    }).ToList() ?? new()
                }).ToList() ?? new()
            };
        }

        private async Task<List<TimeSlotViewModel>> GetAvailableTimeSlots(Court court, DateTime bookingDate)
        {
            var timeSlots = new List<TimeSlotViewModel>();
            var dayOfWeekEnum = bookingDate.DayOfWeek;

            var priceSlots = await _context.PriceSlots
                .Where(p => p.FacilityId == court.FacilityId
                         && p.CourtType == court.CourtType
                         && p.IsActive
                         && (p.DayOfWeek == null || p.DayOfWeek == dayOfWeekEnum))
                .OrderBy(p => p.StartTime)
                .ToListAsync();

            var bookedSlots = await _context.Bookings
                .Where(b => b.CourtId == court.CourtId
                         && b.BookingDate.Date == bookingDate.Date
                         && b.Status != "Cancelled")
                .ToListAsync();

            foreach (var slot in priceSlots)
            {
                bool isBooked = bookedSlots.Any(b =>
                    b.StartTime < slot.EndTime && b.EndTime > slot.StartTime);

                timeSlots.Add(new TimeSlotViewModel
                {
                    StartTime = slot.StartTime,
                    EndTime = slot.EndTime,
                    Price = slot.Price,
                    IsPeakHour = slot.IsPeakHour,
                    IsAvailable = !isBooked
                });
            }

            return timeSlots;
        }

        private async Task<bool> IsCourtAvailable(int courtId, DateTime date, TimeSpan start, TimeSpan end)
        {
            return !await _context.Bookings.AnyAsync(b =>
                b.CourtId == courtId
                && b.BookingDate == date
                && b.Status != "Cancelled"
                && b.StartTime < end
                && b.EndTime > start);
        }

        private async Task<decimal> CalculateCourtPrice(int facilityId, string courtType, TimeSpan start, TimeSpan end)
        {
            var priceSlots = await _context.PriceSlots
                .Where(p => p.FacilityId == facilityId
                         && p.CourtType == courtType
                         && p.IsActive
                         && p.StartTime < end
                         && p.EndTime > start)
                .ToListAsync();

            decimal totalPrice = 0;
            foreach (var slot in priceSlots)
            {
                var overlapStart = start > slot.StartTime ? start : slot.StartTime;
                var overlapEnd = end < slot.EndTime ? end : slot.EndTime;
                double overlapMins = (overlapEnd - overlapStart).TotalMinutes;
                if (overlapMins <= 0) continue;

                double slotMins = (slot.EndTime - slot.StartTime).TotalMinutes;
                totalPrice += slot.Price * (decimal)(overlapMins / slotMins);
            }

            return Math.Round(totalPrice, 0);
        }

        private bool CanCancelBooking(Booking booking, decimal cancelHours)
        {
            if (booking.Status != "Confirmed" && booking.Status != "Pending") return false;
            var bookingDateTime = booking.BookingDate.Date.Add(booking.StartTime);
            return bookingDateTime > DateTime.Now.AddHours((double)cancelHours);
        }

        private async Task CreateProductOrder(Booking booking, int userId, List<OrderItemViewModel> items)
        {
            var court = await _context.Courts.FindAsync(booking.CourtId);
            var orderCode = await GenerateOrderCode();

            var order = new Order
            {
                OrderCode = orderCode,
                BookingId = booking.BookingId,
                UserId = userId,
                FacilityId = court!.FacilityId,
                OrderType = "Product",
                SubTotal = items.Sum(i => i.Price * i.Quantity),
                DiscountAmount = 0,
                TotalAmount = items.Sum(i => i.Price * i.Quantity),
                OrderStatus = "Pending",
                PaymentStatus = "Paid",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            foreach (var item in items.Where(i => i.Quantity > 0))
            {
                _context.OrderDetails.Add(new OrderDetail
                {
                    OrderId = order.OrderId,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.Price,
                    DiscountAmount = 0,
                    TotalPrice = item.Price * item.Quantity
                });
            }

            await _context.SaveChangesAsync();
        }

        private async Task LoadProductsForBooking(int facilityId)
        {
            var allProds = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive)
                .ToListAsync();

            var mapped = allProds.Select(p => new
            {
                vm = new BookingProductItemViewModel
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    Price = p.SalePrice > 0 ? p.SalePrice : p.CostPrice,
                    ImageUrl = p.ImageUrl,
                    Unit = p.Unit,
                    Description = p.Description
                },
                catType = p.Category?.CategoryType
            }).ToList();

            ViewBag.FoodItems = mapped.Where(x => x.catType == "Food").Select(x => x.vm).ToList();
            ViewBag.BeverageItems = mapped.Where(x => x.catType == "Beverage").Select(x => x.vm).ToList();
            ViewBag.EquipmentItems = mapped.Where(x => x.catType == "Equipment").Select(x => x.vm).ToList();
        }

        private async Task<string> GenerateBookingCode()
        {
            var today = DateTime.Today;
            var count = await _context.Bookings.CountAsync(b => b.CreatedAt >= today);
            return "BK" + DateTime.Now.ToString("yyyyMMdd") + (count + 1).ToString("D4");
        }

        private async Task<string> GenerateOrderCode()
        {
            var today = DateTime.Today;
            var count = await _context.Orders.CountAsync(o => o.CreatedAt >= today);
            return "OD" + DateTime.Now.ToString("yyyyMMdd") + (count + 1).ToString("D4");
        }

        private async Task<decimal> GetSystemSetting(string key, decimal defaultValue)
        {
            var setting = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.SettingKey == key && s.IsActive);
            if (setting != null && decimal.TryParse(setting.SettingValue, out decimal value))
                return value;
            return defaultValue;
        }

        private async Task<(bool IsValid, int? VoucherId, decimal DiscountAmount)> ApplyVoucher(
            string code, int userId, decimal orderAmount, string applicableFor)
        {
            var voucher = await _context.Vouchers
                .FirstOrDefaultAsync(v => v.VoucherCode == code.ToUpper().Trim() && v.IsActive);

            if (voucher == null || voucher.EndDate < DateTime.Now || voucher.StartDate > DateTime.Now)
                return (false, null, 0);

            if (voucher.ApplicableFor != "All" && voucher.ApplicableFor != applicableFor)
                return (false, null, 0);

            if (voucher.MinOrderAmount.HasValue && orderAmount < voucher.MinOrderAmount.Value)
                return (false, null, 0);

            if (voucher.UsageLimit.HasValue && voucher.UsedCount >= voucher.UsageLimit.Value)
                return (false, null, 0);

            if (voucher.UsageLimitPerUser > 0)
            {
                var used = await _context.VoucherUsages
                    .CountAsync(v => v.VoucherId == voucher.VoucherId && v.UserId == userId);
                if (used >= voucher.UsageLimitPerUser) return (false, null, 0);
            }

            decimal discount = voucher.DiscountType == "Percentage"
                ? orderAmount * voucher.DiscountValue / 100
                : voucher.DiscountValue;

            if (voucher.MaxDiscount.HasValue && discount > voucher.MaxDiscount.Value)
                discount = voucher.MaxDiscount.Value;

            return (true, voucher.VoucherId, discount);
        }

        private async Task SaveVoucherUsage(int voucherId, int userId, int? bookingId, int? orderId, decimal discountAmount)
        {
            _context.VoucherUsages.Add(new VoucherUsage
            {
                VoucherId = voucherId,
                UserId = userId,
                BookingId = bookingId,
                OrderId = orderId,
                DiscountAmount = discountAmount
            });

            var voucher = await _context.Vouchers.FindAsync(voucherId);
            if (voucher != null) voucher.UsedCount++;

            await _context.SaveChangesAsync();
        }

        private async Task LogActivity(int userId, string action, string tableName,
            int? recordId, string? oldValue, string newValue)
        {
            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = userId,
                Action = action,
                TableName = tableName,
                RecordId = recordId,
                OldValue = oldValue ?? "",
                NewValue = newValue ?? "",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                UserAgent = HttpContext.Request.Headers["User-Agent"].ToString(),
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim != null && int.TryParse(claim.Value, out int userId)) return userId;
            return HttpContext.Session.GetInt32("UserId") ?? 0;
        }

        private string GetCurrentUserRole() =>
            User.FindFirst(ClaimTypes.Role)?.Value ?? "Customer";
    }

    // ── Request model ──
    public class SearchCourtRequest
    {
        public int FacilityId { get; set; }
        public DateTime BookingDate { get; set; }
        public string StartTime { get; set; } = "";
    }
}
