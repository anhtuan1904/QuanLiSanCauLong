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

        public BookingController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Booking/Search - Trang tìm kiếm sân
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

            // Truyền thông tin facility vào ViewBag
            ViewBag.FacilityId = facility.FacilityId;
            ViewBag.FacilityName = facility.FacilityName;
            ViewBag.FacilityAddress = facility.Address + ", " + facility.District + ", " + facility.City;
            ViewBag.FacilityPhone = facility.Phone;
            ViewBag.FacilityImage = facility.ImageUrl ?? "/images/default-facility.jpg";
            ViewBag.OpenTime = facility.OpenTime?.ToString(@"hh\:mm") ?? "06:00";
            ViewBag.CloseTime = facility.CloseTime?.ToString(@"hh\:mm") ?? "23:00";

            // Đếm số sân
            ViewBag.TotalCourts = await _context.Courts
                .CountAsync(c => c.FacilityId == facilityId && c.Status == "Available");

            return View();
        }

        // POST: API tìm sân trống (Ajax)
        [HttpPost]
        public async Task<IActionResult> SearchAvailableCourts([FromBody] SearchCourtRequest request)
        {
            try
            {
                if (request.FacilityId <= 0 || request.BookingDate == default)
                {
                    return Json(new { success = false, message = "Vui lòng chọn đầy đủ thông tin!" });
                }

                // Parse time
                TimeSpan searchTime;
                if (!TimeSpan.TryParse(request.StartTime, out searchTime))
                {
                    return Json(new { success = false, message = "Khung giờ không hợp lệ!" });
                }

                // Get all courts of facility
                var courts = await _context.Courts
                    .Where(c => c.FacilityId == request.FacilityId && c.Status == "Available")
                    .ToListAsync();

                var availableCourts = new List<object>();

                foreach (var court in courts)
                {
                    // Get available time slots for this court
                    var timeSlots = await GetAvailableTimeSlots(court, request.BookingDate);

                    // Find the slot that matches search time
                    var matchingSlot = timeSlots.FirstOrDefault(t =>
                        t.IsAvailable &&
                        t.StartTime <= searchTime &&
                        t.EndTime > searchTime);

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

        // GET: Booking/Create - Form đặt sân
        [HttpGet]
        public async Task<IActionResult> Create(int courtId, DateTime bookingDate, string startTime, string endTime)
        {
            var court = await _context.Courts
                .Include(c => c.Facility)
                .FirstOrDefaultAsync(c => c.CourtId == courtId);

            if (court == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy sân!";
                return RedirectToAction(nameof(Search));
            }

            var start = TimeSpan.Parse(startTime);
            var end = TimeSpan.Parse(endTime);
            var duration = (int)(end - start).TotalMinutes;

            // Tính giá sân
            var courtPrice = await CalculateCourtPrice(court.FacilityId, court.CourtType, start, end);
            var serviceFee = await GetSystemSetting("DefaultServiceFee", 0);
            var serviceFeeAmount = courtPrice * serviceFee / 100;

            var model = new CreateBookingViewModel
            {
                CourtId = courtId,
                BookingDate = bookingDate,
                StartTime = start,
                EndTime = end,
                Duration = duration,
                FacilityName = court.Facility.FacilityName,
                CourtNumber = court.CourtNumber,
                CourtType = court.CourtType,
                CourtPrice = courtPrice,
                ServiceFee = serviceFeeAmount,
                DiscountAmount = 0,
                TotalPrice = courtPrice + serviceFeeAmount,
                OrderItems = new List<OrderItemViewModel>()
            };

            await LoadProductsForBooking(court.FacilityId);
            return View(model);
        }

        // POST: Booking/Create - Xử lý đặt sân
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateBookingViewModel model)
        {
            // 1. Kiểm tra tính hợp lệ của Model
            if (!ModelState.IsValid)
            {
                var courtInfo = await _context.Courts.Include(c => c.Facility)
                    .FirstOrDefaultAsync(c => c.CourtId == model.CourtId);
                if (courtInfo != null) await LoadProductsForBooking(courtInfo.FacilityId);
                return View(model);
            }

            // 2. Kiểm tra sân còn trống (Double Check)
            if (!await IsCourtAvailable(model.CourtId, model.BookingDate, model.StartTime, model.EndTime))
            {
                TempData["ErrorMessage"] = "Rất tiếc, sân vừa mới được người khác đặt trong khung giờ này!";
                return RedirectToAction(nameof(Search));
            }

            int userId = GetCurrentUserId();

            // 3. Tính toán lại giá trị thực tế tại Server
            var court = await _context.Courts.FindAsync(model.CourtId);
            decimal serverCourtPrice = await CalculateCourtPrice(court.FacilityId, court.CourtType, model.StartTime, model.EndTime);
            decimal serverServiceFee = serverCourtPrice * (await GetSystemSetting("DefaultServiceFee", 0)) / 100;
            decimal baseTotal = serverCourtPrice + serverServiceFee;

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // 4. Xử lý Voucher
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

                    // 5. Lưu Booking
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
                        Status = "Confirmed",
                        PaymentMethod = model.PaymentMethod ?? "Cash",
                        PaymentStatus = (model.PaymentMethod == "Cash") ? "Unpaid" : "Paid",
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };

                    _context.Bookings.Add(booking);
                    await _context.SaveChangesAsync();

                    // 6. Xử lý Voucher và Order đi kèm
                    if (voucherId.HasValue)
                        await SaveVoucherUsage(voucherId.Value, userId, booking.BookingId, null, voucherDiscount);

                    if (model.OrderItems != null && model.OrderItems.Any(i => i.Quantity > 0))
                        await CreateProductOrder(booking, userId, model.OrderItems);

                    await LogActivity(userId, "Create", "Bookings", booking.BookingId, null, $"Mã: {booking.BookingCode}");

                    await transaction.CommitAsync();
                    TempData["SuccessMessage"] = "Đặt sân thành công!";
                    return RedirectToAction(nameof(Details), new { id = booking.BookingId });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "Lỗi hệ thống: " + ex.Message;
                    return RedirectToAction(nameof(Search));
                }
            }
        }

        // GET: Booking/MyBookings - Lịch sử đặt sân
        [HttpGet]
        public async Task<IActionResult> MyBookings(string status, DateTime? fromDate, DateTime? toDate)
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

            var model = bookings.Select(b => new BookingHistoryViewModel
            {
                BookingId = b.BookingId,
                BookingCode = b.BookingCode,
                FacilityName = b.Court?.Facility?.FacilityName ?? "N/A",
                CourtNumber = b.Court != null ? b.Court.CourtNumber.ToString() : "N/A",
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
                CanCancel = CanCancelBooking(b, cancelHoursSetting),
                RelatedOrders = b.Orders.Select(o => new OrderViewModel
                {
                    OrderId = o.OrderId,
                    OrderCode = o.OrderCode,
                    OrderType = o.OrderType,
                    TotalAmount = o.TotalAmount,
                    OrderStatus = o.OrderStatus,
                    OrderDetails = o.OrderDetails.Select(od => new OrderDetailViewModel
                    {
                        ProductName = od.Product?.ProductName ?? "Sản phẩm không xác định",
                        Quantity = od.Quantity,
                        Unit = od.Product?.Unit ?? "",
                        UnitPrice = od.UnitPrice,
                        TotalPrice = od.TotalPrice
                    }).ToList()
                }).ToList()
            }).ToList();

            ViewBag.Statuses = new[] { "Pending", "Confirmed", "Playing", "Completed", "Cancelled" };
            return View(model);
        }

        // GET: Booking/Details - Chi tiết booking
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

            if (booking == null)
                return NotFound();

            int userId = GetCurrentUserId();
            string userRole = GetCurrentUserRole();

            if (userRole != "Admin" && userRole != "Staff" && booking.UserId != userId)
                return Forbid();

            var cancelHours = await GetSystemSetting("BookingCancellationHours", 2);

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
                Duration = booking.Duration,
                TotalPrice = booking.TotalPrice,
                Status = booking.Status,
                PaymentStatus = booking.PaymentStatus,
                PaymentMethod = booking.PaymentMethod,
                CreatedAt = booking.CreatedAt,
                Note = booking.Note,
                CanCancel = CanCancelBooking(booking, cancelHours),
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

        // POST: Booking/Cancel - Hủy booking
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, string cancelReason)
        {
            var booking = await _context.Bookings.FindAsync(id);

            if (booking == null)
                return NotFound();

            if (booking.UserId != GetCurrentUserId())
                return Forbid();

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

        // HELPER METHODS

        private async Task<List<TimeSlotViewModel>> GetAvailableTimeSlots(Court court, DateTime bookingDate)
        {
            var timeSlots = new List<TimeSlotViewModel>();
            DayOfWeek dayOfWeekEnum = bookingDate.DayOfWeek;

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

            return priceSlots.Sum(p => p.Price);
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
                FacilityId = court.FacilityId,
                OrderType = "Product",
                SubTotal = items.Sum(i => i.TotalPrice),
                DiscountAmount = 0,
                TotalAmount = items.Sum(i => i.TotalPrice),
                OrderStatus = "Pending",
                PaymentStatus = "Paid",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            foreach (var item in items.Where(i => i.Quantity > 0))
            {
                var orderDetail = new OrderDetail
                {
                    OrderId = order.OrderId,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.Price,
                    DiscountAmount = 0,
                    TotalPrice = item.TotalPrice
                };

                _context.OrderDetails.Add(orderDetail);
            }

            await _context.SaveChangesAsync();
        }

        private async Task LoadProductsForBooking(int facilityId)
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Inventories)
                .Where(p => p.IsActive)
                .ToListAsync();

            ViewBag.FoodItems = products.Where(p => p.Category.CategoryType == "Food").ToList();
            ViewBag.BeverageItems = products.Where(p => p.Category.CategoryType == "Beverage").ToList();
            ViewBag.EquipmentItems = products.Where(p => p.Category.CategoryType == "Equipment").ToList();
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
            var usage = new VoucherUsage
            {
                VoucherId = voucherId,
                UserId = userId,
                BookingId = bookingId,
                OrderId = orderId,
                DiscountAmount = discountAmount,
                UsedAt = DateTime.Now
            };

            _context.VoucherUsages.Add(usage);

            var voucher = await _context.Vouchers.FindAsync(voucherId);
            if (voucher != null)
            {
                voucher.UsedCount++;
            }

            await _context.SaveChangesAsync();
        }

        private async Task LogActivity(int userId, string action, string tableName, int? recordId,
            string oldValue, string newValue)
        {
            var log = new ActivityLog
            {
                UserId = userId,
                Action = action,
                TableName = tableName,
                RecordId = recordId,
                OldValue = oldValue,
                NewValue = newValue,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = HttpContext.Request.Headers["User-Agent"].ToString(),
                CreatedAt = DateTime.Now
            };

            _context.ActivityLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
            return HttpContext.Session.GetInt32("UserId") ?? 0;
        }

        private string GetCurrentUserRole()
        {
            var roleClaim = User.FindFirst(ClaimTypes.Role);
            return roleClaim?.Value ?? "Customer";
        }
    }

    // Request model for search
    public class SearchCourtRequest
    {
        public int FacilityId { get; set; }
        public DateTime BookingDate { get; set; }
        public string StartTime { get; set; }
    }
}
