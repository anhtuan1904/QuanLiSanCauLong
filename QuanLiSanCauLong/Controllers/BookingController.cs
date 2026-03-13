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

        // ── Cấu hình ngân hàng ──
        private const string BankCode = "MB";
        private const string BankAccount = "0123456789";
        private const string BankAccountName = "CONG TY QUAN LI SAN CAU LONG";
        private const string BankName = "MB Bank";

        public BookingController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ── HELPER: ViewBag thông tin ngân hàng + VietQR ──
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

        // ─────────────────────────────────────────────────────────────────────
        // GET: Booking/Search
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Search(int facilityId)
        {
            var facility = await _context.Facilities
                .FirstOrDefaultAsync(f => f.FacilityId == facilityId);

            if (facility == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy cơ sở!";
                return RedirectToAction("Index", "Facility");
            }

            ViewBag.FacilityId = facility.FacilityId;
            ViewBag.FacilityName = facility.FacilityName;
            ViewBag.FacilityAddress = facility.Address + ", " + facility.District + ", " + facility.City;
            ViewBag.FacilityPhone = facility.Phone;
            ViewBag.FacilityImage = facility.ImageUrl ?? "/images/default-facility.jpg";
            ViewBag.OpenTime = facility.OpenTime?.ToString(@"hh\:mm") ?? "06:00";
            ViewBag.CloseTime = facility.CloseTime?.ToString(@"hh\:mm") ?? "23:00";
            ViewBag.TotalCourts = await _context.Courts
                .CountAsync(c => c.FacilityId == facilityId && c.Status == "Available");

            return View();
        }

        // ─────────────────────────────────────────────────────────────────────
        // POST: API tìm sân trống (Ajax)
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
                        availableCourts.Add(new
                        {
                            courtId = court.CourtId,
                            courtNumber = court.CourtNumber,
                            courtType = court.CourtType,
                            startTime = matchingSlot.StartTime.ToString(@"hh\:mm"),
                            endTime = matchingSlot.EndTime.ToString(@"hh\:mm"),
                            price = matchingSlot.Price,
                            isPeakHour = matchingSlot.IsPeakHour,
                            hasLighting = court.HasLighting,
                            hasAC = court.HasAC
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
        // GET: Booking/Create
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Create(int courtId, DateTime bookingDate, string startTime, string endTime)
        {
            var court = await _context.Courts
                .Include(c => c.Facility)
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
                // FIX: map Court.ImagePath (alias ImageUrl) → ViewModel.CourtImageUrl
                CourtImageUrl = court.ImagePath,
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

        // ─────────────────────────────────────────────────────────────────────
        // POST: Booking/Create
        // ─────────────────────────────────────────────────────────────────────
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

            // FIX: dùng CreateExecutionStrategy thay vì BeginTransactionAsync trực tiếp
            // SqlServerRetryingExecutionStrategy không cho phép mở transaction thủ công
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
                        ? $"Đặt sân thành công! Vui lòng chuyển khoản với nội dung: {booking.BookingCode}"
                        : "Đặt sân thành công!";

                    actionResult = RedirectToAction(nameof(Details), new { id = booking.BookingId });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    var innerMsg = ex.InnerException?.Message ?? ex.Message;
                    TempData["ErrorMessage"] = "Lỗi khi lưu đặt sân: " + innerMsg;
                    actionResult = RedirectToAction(nameof(MyBookings));
                }
            });

            return actionResult;
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET: Booking/MyBookings
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> MyBookings(string? status, DateTime? fromDate, DateTime? toDate)
        {
            int userId = GetCurrentUserId();
            var cancelHoursSetting = await GetSystemSetting("BookingCancellationHours", 2);

            var query = _context.Bookings
                .Include(b => b.Court.Facility)
                .Include(b => b.Orders)
                    .ThenInclude(o => o.OrderDetails)
                        .ThenInclude(od => od.Product)
                .Where(b => b.UserId == userId);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(b => b.Status == status);
            if (fromDate.HasValue)
                query = query.Where(b => b.BookingDate >= fromDate.Value);
            if (toDate.HasValue)
                query = query.Where(b => b.BookingDate <= toDate.Value);

            var bookings = await query.OrderByDescending(b => b.CreatedAt).ToListAsync();
            var model = bookings.Select(b => MapToHistoryViewModel(b, cancelHoursSetting)).ToList();

            ViewBag.Statuses = new[] { "Pending", "Confirmed", "Playing", "Completed", "Cancelled" };
            return View(model);
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET: Booking/Details
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Court.Facility)
                .Include(b => b.User)
                .Include(b => b.Orders)
                    .ThenInclude(o => o.OrderDetails)
                        .ThenInclude(od => od.Product)
                .Include(b => b.VoucherUsages)
                    .ThenInclude(v => v.Voucher)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null) return NotFound();

            int userId = GetCurrentUserId();
            string userRole = GetCurrentUserRole();

            if (userRole != "Admin" && userRole != "Staff" && booking.UserId != userId)
                return Forbid();

            var cancelHours = await GetSystemSetting("BookingCancellationHours", 2);
            return View(MapToHistoryViewModel(booking, cancelHours));
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET: Booking/Invoice
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Invoice(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Court.Facility)
                .Include(b => b.Orders)
                    .ThenInclude(o => o.OrderDetails)
                        .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null) return NotFound();

            int userId = GetCurrentUserId();
            string userRole = GetCurrentUserRole();

            if (userRole != "Admin" && userRole != "Staff" && booking.UserId != userId)
                return Forbid();

            var cancelHours = await GetSystemSetting("BookingCancellationHours", 2);
            SetBankViewBag(booking.BookingCode);
            return View(MapToHistoryViewModel(booking, cancelHours));
        }

        // ─────────────────────────────────────────────────────────────────────
        // POST: Booking/Cancel
        // ─────────────────────────────────────────────────────────────────────
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
                TempData["ErrorMessage"] = "Không thể hủy đơn này do đã quá thời gian quy định!";
                return RedirectToAction(nameof(MyBookings));
            }

            booking.Status = "Cancelled";
            booking.CancelReason = cancelReason;
            booking.CancelledAt = DateTime.Now;
            booking.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            await LogActivity(GetCurrentUserId(), "Cancel", "Bookings", booking.BookingId,
                "Confirmed", $"Hủy đơn {booking.BookingCode}: {cancelReason}");

            TempData["SuccessMessage"] = "Hủy đặt sân thành công!";
            return RedirectToAction(nameof(MyBookings));
        }

        // ─────────────────────────────────────────────────────────────────────
        // HELPER: Map Booking → BookingHistoryViewModel
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
                CourtNumber = b.Court?.CourtNumber ?? "N/A",
                CourtType = b.Court?.CourtType ?? "N/A",
                // FIX: map Court.ImagePath → CourtImageUrl
                CourtImageUrl = b.Court?.ImagePath,
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
                CanCancel = CanCancelBooking(b, cancelHours),
                // FIX: use OrderViewModel (has OrderCode, OrderType, OrderStatus)
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

        // ─────────────────────────────────────────────────────────────────────
        // PRIVATE HELPERS
        // ─────────────────────────────────────────────────────────────────────
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
            if (booking.Status != "Confirmed" && booking.Status != "Pending")
                return false;

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

        // FIX: project Product → BookingProductItemViewModel
        // View dùng dynamic (item.ImageUrl, item.ProductName, item.Price, item.Unit, item.Description)
        // Nếu truyền List<Product> thì item.ImageUrl sẽ ném RuntimeBinderException
        private async Task LoadProductsForBooking(int facilityId)
        {
            // FIX: project sang BookingProductItemViewModel
            // View dùng dynamic binding (item.ImageUrl, item.Price...) — truyền List<Product> thô
            // sẽ ném RuntimeBinderException vì Product model không có các property này.
            var allProds = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive)
                .ToListAsync();

            var mapped = allProds
                .Select(p => new
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
                })
                .ToList();

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
                .FirstOrDefaultAsync(v => v.VoucherCode == code && v.IsActive);

            if (voucher == null || voucher.EndDate < DateTime.Now || voucher.StartDate > DateTime.Now)
                return (false, null, 0);

            if (voucher.ApplicableFor != "All" && voucher.ApplicableFor != applicableFor)
                return (false, null, 0);

            if (voucher.MinOrderAmount.HasValue && orderAmount < voucher.MinOrderAmount.Value)
                return (false, null, 0);

            var usageCount = await _context.VoucherUsages
                .CountAsync(v => v.VoucherId == voucher.VoucherId && v.UserId == userId);

            if (usageCount >= voucher.UsageLimitPerUser)
                return (false, null, 0);

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
                DiscountAmount = discountAmount,
                UsedAt = DateTime.Now
            });

            var voucher = await _context.Vouchers.FindAsync(voucherId);
            if (voucher != null) voucher.UsedCount++;

            await _context.SaveChangesAsync();
        }

        private async Task LogActivity(int userId, string action, string tableName, int? recordId,
            string? oldValue, string newValue)
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
                UserAgent = HttpContext.Request.Headers["User-Agent"].ToString() ?? "",
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim != null && int.TryParse(claim.Value, out int userId))
                return userId;
            return HttpContext.Session.GetInt32("UserId") ?? 0;
        }

        private string GetCurrentUserRole()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value ?? "Customer";
        }
    }

    // ── Request model dùng chung ──
    public class SearchCourtRequest
    {
        public int FacilityId { get; set; }
        public DateTime BookingDate { get; set; }
        public string StartTime { get; set; } = "";
    }
}
