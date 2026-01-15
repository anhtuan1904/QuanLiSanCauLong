using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLiSanCauLong.Data;
using QuanLiSanCauLong.Models;
using QuanLiSanCauLong.ViewModels;
using System.Security.Claims;

namespace QuanLiSanCauLong.Controllers
{
    [Authorize(Roles = "Staff,Admin")]
    public class StaffController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StaffController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Staff/Dashboard
        [HttpGet]
        public async Task<IActionResult> Dashboard(DateTime? selectedDate)
        {
            int staffId = GetCurrentUserId();
            var staff = await _context.Users.FindAsync(staffId);

            if (staff == null || !staff.FacilityId.HasValue)
            {
                TempData["ErrorMessage"] = "Bạn chưa được gán cơ sở!";
                return RedirectToAction("Index", "Home");
            }

            int facilityId = staff.FacilityId.Value;
            DateTime date = selectedDate ?? DateTime.Today;

            var facility = await _context.Facilities.FindAsync(facilityId);

            var model = new StaffDashboardViewModel
            {
                SelectedDate = date,
                FacilityId = facilityId,
                FacilityName = facility.FacilityName,
                CourtSchedules = new List<CourtScheduleViewModel>(),
                PendingBookings = new List<BookingItemViewModel>(),
                UpcomingBookings = new List<BookingItemViewModel>(),
                PlayingBookings = new List<BookingItemViewModel>(),
                Statistics = await GetDayStatistics(facilityId, date)
            };

            // Lấy danh sách sân
            var courts = await _context.Courts
                .Where(c => c.FacilityId == facilityId && c.Status == "Available")
                .OrderBy(c => c.CourtNumber)
                .ToListAsync();

            var todayBookings = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Court)
                .Include(b => b.Orders)
                .Where(b => b.Court.FacilityId == facilityId
                       && b.BookingDate == date
                       && b.Status != "Cancelled")
                .ToListAsync();

            // Xây dựng lịch từng sân
            foreach (var court in courts)
            {
                var courtBookings = todayBookings
                    .Where(b => b.CourtId == court.CourtId)
                    .OrderBy(b => b.StartTime)
                    .ToList();

                var now = DateTime.Now.TimeOfDay;

                BookingItemViewModel currentBooking = null;
                BookingItemViewModel nextBooking = null;
                string currentStatus = "Empty";

                foreach (var booking in courtBookings)
                {
                    if (booking.StartTime <= now && booking.EndTime > now)
                    {
                        currentBooking = MapToBookingItem(booking);
                        currentStatus = booking.Status == "Playing" ? "Playing" : "Upcoming";
                    }
                    else if (booking.StartTime > now && nextBooking == null)
                    {
                        nextBooking = MapToBookingItem(booking);
                    }
                }

                if (court.Status == "Maintenance")
                    currentStatus = "Maintenance";

                model.CourtSchedules.Add(new CourtScheduleViewModel
                {
                    CourtId = court.CourtId,
                    CourtNumber = court.CourtNumber,
                    CourtType = court.CourtType,
                    CurrentStatus = currentStatus,
                    CurrentBooking = currentBooking,
                    NextBooking = nextBooking,
                    TimeSlots = GenerateTimeSlots(court.CourtId, date, todayBookings)
                });
            }

            // Phân loại booking
            model.PendingBookings = todayBookings
                .Where(b => b.Status == "Pending")
                .Select(b => MapToBookingItem(b))
                .ToList();

            var now2 = DateTime.Now.TimeOfDay;
            model.UpcomingBookings = todayBookings
                .Where(b => b.Status == "Confirmed" && b.StartTime > now2)
                .OrderBy(b => b.StartTime)
                .Select(b => MapToBookingItem(b))
                .ToList();

            model.PlayingBookings = todayBookings
                .Where(b => b.Status == "Playing" ||
                       (b.Status == "Confirmed" && b.StartTime <= now2 && b.EndTime > now2))
                .Select(b => MapToBookingItem(b))
                .ToList();

            return View(model);
        }

        // POST: Staff/CheckIn
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckIn(int bookingId)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);

            if (booking == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn đặt sân!" });
            }

            int staffId = GetCurrentUserId();

            if (booking.Status != "Confirmed" && booking.Status != "Pending")
            {
                return Json(new { success = false, message = "Trạng thái đơn không hợp lệ!" });
            }

            booking.Status = "Playing";
            booking.CheckInTime = DateTime.Now;
            booking.CheckInBy = staffId;
            booking.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            await LogActivity(staffId, "CheckIn", "Bookings", bookingId,
                booking.Status, $"Check-in sân {booking.Court?.CourtNumber}");

            return Json(new { success = true, message = "Check-in thành công!" });
        }

        // POST: Staff/CheckOut
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckOut(int bookingId)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);

            if (booking == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn đặt sân!" });
            }

            if (booking.Status != "Playing")
            {
                return Json(new { success = false, message = "Sân chưa được check-in!" });
            }

            booking.Status = "Completed";
            booking.CheckOutTime = DateTime.Now;
            booking.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            await LogActivity(GetCurrentUserId(), "CheckOut", "Bookings", bookingId,
                "Playing", "Hoàn thành");

            return Json(new { success = true, message = "Check-out thành công!" });
        }

        // GET: Staff/Sales
        [HttpGet]
        public async Task<IActionResult> Sales(int? bookingId)
        {
            int staffId = GetCurrentUserId();
            var staff = await _context.Users.FindAsync(staffId);

            if (staff == null || !staff.FacilityId.HasValue)
            {
                TempData["ErrorMessage"] = "Bạn chưa được gán cơ sở!";
                return RedirectToAction(nameof(Dashboard));
            }

            int facilityId = staff.FacilityId.Value;

            var model = new SalesViewModel
            {
                BookingId = bookingId,
                FacilityId = facilityId,
                UserId = bookingId.HasValue ?
                    (await _context.Bookings.FindAsync(bookingId.Value))?.UserId ?? 0 : 0,
                ProductCategories = new List<ProductCategoryViewModel>(),
                CartItems = new List<OrderItemViewModel>()
            };

            // Load sản phẩm theo category
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Inventories)
                .Where(p => p.IsActive)
                .ToListAsync();

            var inventory = await _context.Inventories
                .Where(i => i.FacilityId == facilityId)
                .ToDictionaryAsync(i => i.ProductId, i => i.Quantity);

            var categories = products
                .GroupBy(p => p.Category.CategoryType)
                .Select(g => new ProductCategoryViewModel
                {
                    CategoryName = g.First().Category.CategoryName,
                    CategoryType = g.Key,
                    Products = g.Select(p => new ProductItemViewModel
                    {
                        ProductId = p.ProductId,
                        ProductName = p.ProductName,
                        Price = p.Price,
                        Unit = p.Unit,
                        ImageUrl = p.ImageUrl,
                        StockQuantity = inventory.ContainsKey(p.ProductId) ? inventory[p.ProductId] : 0
                    }).ToList()
                }).ToList();

            model.ProductCategories = categories;

            if (bookingId.HasValue)
            {
                var booking = await _context.Bookings
                    .Include(b => b.User)
                    .Include(b => b.Court)
                    .FirstOrDefaultAsync(b => b.BookingId == bookingId.Value);

                if (booking != null)
                {
                    ViewBag.Booking = booking;
                }
            }

            return View(model);
        }

        // POST: Staff/CreateOrder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOrder(SalesViewModel model)
        {
            if (model.CartItems == null || !model.CartItems.Any())
            {
                return Json(new { success = false, message = "Giỏ hàng trống!" });
            }

            int staffId = GetCurrentUserId();

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Kiểm tra tồn kho
                    foreach (var item in model.CartItems)
                    {
                        var inventory = await _context.Inventories
                            .FirstOrDefaultAsync(i => i.ProductId == item.ProductId && i.FacilityId == model.FacilityId);

                        if (inventory == null || inventory.Quantity < item.Quantity)
                        {
                            return Json(new
                            {
                                success = false,
                                message = $"Sản phẩm {item.ProductName} không đủ số lượng!"
                            });
                        }
                    }

                    // Tạo order
                    string orderCode = await GenerateOrderCode();

                    var order = new Order
                    {
                        OrderCode = orderCode,
                        BookingId = model.BookingId,
                        UserId = model.UserId > 0 ? model.UserId : staffId,
                        FacilityId = model.FacilityId,
                        OrderType = "Product",
                        SubTotal = model.SubTotal,
                        DiscountAmount = model.DiscountAmount,
                        TotalAmount = model.TotalAmount,
                        OrderStatus = "Completed",
                        PaymentStatus = "Paid",
                        PaymentMethod = "Cash",
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now,
                        CompletedAt = DateTime.Now,
                        CreatedBy = staffId
                    };

                    _context.Orders.Add(order);
                    await _context.SaveChangesAsync();

                    // Tạo order details và cập nhật kho
                    foreach (var item in model.CartItems)
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

                        // Trừ kho
                        var inventory = await _context.Inventories
                            .FirstAsync(i => i.ProductId == item.ProductId && i.FacilityId == model.FacilityId);

                        inventory.Quantity -= item.Quantity;
                        inventory.LastUpdated = DateTime.Now;
                    }

                    await _context.SaveChangesAsync();

                    await LogActivity(staffId, "Create", "Orders", order.OrderId,
                        null, $"Tạo đơn hàng {orderCode}");

                    await transaction.CommitAsync();

                    return Json(new
                    {
                        success = true,
                        message = $"Tạo đơn hàng thành công! Mã: {orderCode}",
                        orderId = order.OrderId
                    });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return Json(new { success = false, message = "Lỗi: " + ex.Message });
                }
            }
        }

        // GET: Staff/BookingManagement
        [HttpGet]
        public async Task<IActionResult> BookingManagement(string status, DateTime? date)
        {
            int staffId = GetCurrentUserId();
            var staff = await _context.Users.FindAsync(staffId);

            if (staff == null || !staff.FacilityId.HasValue)
            {
                TempData["ErrorMessage"] = "Bạn chưa được gán cơ sở!";
                return RedirectToAction(nameof(Dashboard));
            }

            int facilityId = staff.FacilityId.Value;
            DateTime searchDate = date ?? DateTime.Today;

            var query = _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Court)
                .Include(b => b.Orders)
                .Where(b => b.Court.FacilityId == facilityId);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(b => b.Status == status);

            if (date.HasValue)
                query = query.Where(b => b.BookingDate == searchDate);
            else
                query = query.Where(b => b.BookingDate >= DateTime.Today);

            var bookings = await query
                .OrderBy(b => b.BookingDate)
                .ThenBy(b => b.StartTime)
                .Take(100)
                .ToListAsync();

            var model = bookings.Select(b => MapToBookingItem(b)).ToList();

            ViewBag.Statuses = new[] { "Pending", "Confirmed", "Playing", "Completed", "Cancelled" };
            return View(model);
        }

        // POST: Staff/ConfirmBooking
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmBooking(int bookingId)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);

            if (booking == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn!" });
            }

            if (booking.Status != "Pending")
            {
                return Json(new { success = false, message = "Đơn đã được xử lý!" });
            }

            booking.Status = "Confirmed";
            booking.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            await LogActivity(GetCurrentUserId(), "Confirm", "Bookings", bookingId,
                "Pending", "Confirmed");

            return Json(new { success = true, message = "Xác nhận đơn thành công!" });
        }

        // POST: Staff/RejectBooking
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectBooking(int bookingId, string reason)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);

            if (booking == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn!" });
            }

            booking.Status = "Cancelled";
            booking.CancelReason = reason;
            booking.CancelledAt = DateTime.Now;
            booking.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            await LogActivity(GetCurrentUserId(), "Reject", "Bookings", bookingId,
                booking.Status, $"Từ chối: {reason}");

            return Json(new { success = true, message = "Đã từ chối đơn!" });
        }

        // HELPER METHODS

        private BookingItemViewModel MapToBookingItem(Booking booking)
        {
            return new BookingItemViewModel
            {
                BookingId = booking.BookingId,
                BookingCode = booking.BookingCode,
                CustomerName = booking.User.FullName,
                CustomerPhone = booking.User.Phone,
                CourtNumber = booking.Court.CourtNumber,
                CourtType = booking.Court.CourtType,
                BookingDate = booking.BookingDate,
                StartTime = booking.StartTime,
                EndTime = booking.EndTime,
                Duration = booking.Duration,
                Status = booking.Status,
                TotalPrice = booking.TotalPrice,
                CheckInTime = booking.CheckInTime,
                HasOrders = booking.Orders != null && booking.Orders.Any(),
                OrderCount = booking.Orders?.Count ?? 0
            };
        }

        private List<TimeSlotBookingViewModel> GenerateTimeSlots(int courtId, DateTime date, List<Booking> bookings)
        {
            var slots = new List<TimeSlotBookingViewModel>();
            var courtBookings = bookings.Where(b => b.CourtId == courtId).ToList();

            for (int hour = 6; hour < 23; hour++)
            {
                var startTime = new TimeSpan(hour, 0, 0);
                var endTime = new TimeSpan(hour + 1, 0, 0);

                var booking = courtBookings.FirstOrDefault(b =>
                    b.StartTime <= startTime && b.EndTime > startTime);

                slots.Add(new TimeSlotBookingViewModel
                {
                    StartTime = startTime,
                    EndTime = endTime,
                    IsBooked = booking != null,
                    CustomerName = booking?.User?.FullName,
                    BookingId = booking?.BookingId
                });
            }

            return slots;
        }

        private async Task<StatisticSummaryViewModel> GetDayStatistics(int facilityId, DateTime date)
        {
            var bookings = await _context.Bookings
                .Include(b => b.Court)
                .Where(b => b.Court.FacilityId == facilityId && b.BookingDate == date)
                .ToListAsync();

            return new StatisticSummaryViewModel
            {
                TodayBookings = bookings.Count,
                PendingBookings = bookings.Count(b => b.Status == "Pending"),
                PlayingBookings = bookings.Count(b => b.Status == "Playing"),
                CompletedBookings = bookings.Count(b => b.Status == "Completed"),
                TodayRevenue = bookings.Where(b => b.Status != "Cancelled").Sum(b => b.TotalPrice)
            };
        }

        private async Task<string> GenerateOrderCode()
        {
            var today = DateTime.Today;
            var count = await _context.Orders.CountAsync(o => o.CreatedAt >= today);
            return "OD" + DateTime.Now.ToString("yyyyMMdd") + (count + 1).ToString("D4");
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
    }
}
