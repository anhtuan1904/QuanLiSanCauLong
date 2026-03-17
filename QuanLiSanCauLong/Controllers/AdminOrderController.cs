using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiSanCauLong.Data;
using QuanLiSanCauLong.Models;
using QuanLiSanCauLong.ViewModels;

namespace QuanLiSanCauLong.Controllers
{
    [Authorize(Roles = "Admin,Staff")]
    public class AdminOrderController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminOrderController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ═════════════════════════════════════════════════════════════════════
        // INDEX
        // ═════════════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Index(
            DateTime? fromDate, DateTime? toDate,
            string? status, string? orderType,
            string? search, int page = 1)
        {
            const int pageSize = 30;

            var query = _context.Orders
                .Include(o => o.Facility)
                .Include(o => o.Booking).ThenInclude(b => b!.User)
                .Include(o => o.User)
                .Include(o => o.OrderDetails).ThenInclude(od => od.Product)
                // ── MỚI: Include ServiceEnrollments cho đơn dịch vụ ──
                .Include(o => o.ServiceEnrollments!).ThenInclude(e => e.Course)
                .Include(o => o.ServiceEnrollments!).ThenInclude(e => e.StringingService)
                .Include(o => o.ServiceEnrollments!).ThenInclude(e => e.Tournament)
                .AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(o => o.CreatedAt >= fromDate.Value.Date);
            if (toDate.HasValue)
                query = query.Where(o => o.CreatedAt < toDate.Value.Date.AddDays(1));
            if (!string.IsNullOrEmpty(status))
                query = query.Where(o => o.OrderStatus == status);

            // orderType filter mở rộng: product | booking | service
            if (orderType == "booking")
                query = query.Where(o => o.BookingId != null);
            else if (orderType == "product")
                query = query.Where(o => o.OrderType == "Product");
            else if (orderType == "service")
                query = query.Where(o => o.OrderType.StartsWith("Service_"));

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(o =>
                    o.OrderCode.ToLower().Contains(s) ||
                    (o.User != null && o.User.FullName.ToLower().Contains(s)) ||
                    (o.ServiceEnrollments!.Any(e =>
                        e.CustomerName.ToLower().Contains(s) ||
                        e.Phone.Contains(s))));
            }

            int total = await query.CountAsync();
            var orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Badge counts
            var counts = await _context.Orders
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    Pending = g.Count(o => o.OrderStatus == "Pending"),
                    Confirmed = g.Count(o => o.OrderStatus == "Confirmed"),
                    Shipping = g.Count(o => o.OrderStatus == "Shipping"),
                    Completed = g.Count(o => o.OrderStatus == "Completed"),
                    Revenue = g.Where(o => o.OrderStatus == "Completed" || o.PaymentStatus == "Paid")
                                 .Sum(o => o.TotalAmount),
                    ServiceOrders = g.Count(o => o.OrderType.StartsWith("Service_"))
                })
                .FirstOrDefaultAsync();

            ViewBag.TotalOrders = counts?.Total ?? 0;
            ViewBag.PendingOrders = counts?.Pending ?? 0;
            ViewBag.ShippingOrders = counts?.Shipping ?? 0;
            ViewBag.CompletedOrders = counts?.Completed ?? 0;
            ViewBag.TotalRevenue = counts?.Revenue ?? 0m;
            ViewBag.ServiceOrders = counts?.ServiceOrders ?? 0;
            ViewBag.Page = page;
            ViewBag.TotalPage = (int)Math.Ceiling((double)total / pageSize);
            ViewBag.Search = search;

            return View(orders.Select(Map).ToList());
        }

        // ═════════════════════════════════════════════════════════════════════
        // DETAILS
        // ═════════════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var order = await LoadFull(id);
            if (order == null) return NotFound();
            return View(Map(order));
        }

        // ═════════════════════════════════════════════════════════════════════
        // SET STATUS
        // ═════════════════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetStatus(int id, string newStatus, string? reason)
        {
            var allowed = new[] { "Confirmed", "Shipping", "Completed", "Cancelled" };
            if (!allowed.Contains(newStatus))
                return Json(new { success = false, message = "Trạng thái không hợp lệ!" });

            var order = await LoadFull(id);
            if (order == null)
                return Json(new { success = false, message = "Không tìm thấy đơn hàng!" });

            if (order.OrderStatus is "Cancelled" or "Completed")
                return Json(new { success = false, message = $"Không thể thay đổi đơn đã {order.OrderStatus}!" });

            string old = order.OrderStatus;

            switch (newStatus)
            {
                case "Confirmed":
                    if (old != "Pending")
                        return Json(new { success = false, message = "Chỉ xác nhận được đơn đang Chờ xử lý!" });
                    // Đơn sản phẩm → kiểm tra kho
                    if (!order.IsServiceOrder)
                    {
                        var stockErr = CheckAndReserve(order.OrderDetails, dryRun: true);
                        if (stockErr != null) return Json(new { success = false, message = stockErr });
                        CheckAndReserve(order.OrderDetails, dryRun: false);
                    }
                    order.OrderStatus = "Confirmed";
                    break;

                case "Shipping":
                    if (old != "Confirmed")
                        return Json(new { success = false, message = "Chỉ chuyển sang Đang giao từ Đã xác nhận!" });
                    order.OrderStatus = "Shipping";
                    break;

                case "Completed":
                    if (old is not ("Confirmed" or "Shipping"))
                        return Json(new { success = false, message = "Chỉ hoàn thành được đơn đã Xác nhận hoặc Đang giao!" });
                    if (!order.IsServiceOrder)
                        DeductStock(order.OrderDetails);
                    order.OrderStatus = "Completed";
                    order.PaymentStatus = "Paid";
                    order.CompletedAt = DateTime.Now;

                    // Xác nhận enrollment nếu là đơn dịch vụ
                    if (order.ServiceEnrollments != null)
                    {
                        foreach (var e in order.ServiceEnrollments)
                        {
                            e.Status = "Confirmed";
                            e.UpdatedAt = DateTime.Now;
                        }
                    }
                    break;

                case "Cancelled":
                    if (string.IsNullOrWhiteSpace(reason))
                        return Json(new { success = false, message = "Vui lòng nhập lý do hủy!" });
                    if (old is "Confirmed" or "Shipping" && !order.IsServiceOrder)
                        ReleaseReserved(order.OrderDetails);
                    if (order.ServiceEnrollments != null)
                    {
                        foreach (var e in order.ServiceEnrollments)
                        {
                            e.Status = "Cancelled";
                            e.UpdatedAt = DateTime.Now;
                        }
                    }
                    order.OrderStatus = "Cancelled";
                    order.Note = string.IsNullOrEmpty(order.Note)
                        ? $"Lý do hủy: {reason}"
                        : order.Note + $" | Hủy: {reason}";
                    break;
            }

            order.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            string label = newStatus switch
            {
                "Confirmed" => "Đã xác nhận",
                "Shipping" => "Đang giao",
                "Completed" => "Hoàn thành",
                "Cancelled" => "Đã hủy",
                _ => newStatus
            };

            return Json(new { success = true, message = $"Đơn {order.OrderCode} → {label}." });
        }

        // ═════════════════════════════════════════════════════════════════════
        // API: Variants của product
        // ═════════════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> GetVariants(int productId)
        {
            var variants = await _context.ProductVariants
                .Where(v => v.ProductId == productId && v.IsActive)
                .Select(v => new
                {
                    v.VariantId,
                    v.DisplayName,
                    v.SizeName,
                    v.ColorName,
                    v.AvailableQuantity,
                    v.StockQuantity,
                    v.ReservedQuantity
                })
                .ToListAsync();
            return Json(variants);
        }

        // ─────────────────────────────────────────────────────────────────────
        // INVENTORY HELPERS
        // ─────────────────────────────────────────────────────────────────────

        private static string? CheckAndReserve(IEnumerable<OrderDetail> details, bool dryRun)
        {
            foreach (var od in details)
            {
                if (od.VariantId.HasValue && od.Variant != null)
                {
                    if (od.Variant.AvailableQuantity < od.Quantity)
                        return $"'{od.Product.ProductName}' ({od.Variant.DisplayName}) " +
                               $"không đủ hàng. Khả dụng: {od.Variant.AvailableQuantity}, cần: {od.Quantity}";
                    if (!dryRun)
                    {
                        od.Variant.ReservedQuantity += od.Quantity;
                        od.Variant.UpdatedAt = DateTime.Now;
                        od.Product.ReservedQuantity += od.Quantity;
                        od.Product.UpdatedAt = DateTime.Now;
                    }
                }
                else
                {
                    int avail = od.Product.StockQuantity - od.Product.ReservedQuantity;
                    if (avail < od.Quantity)
                        return $"'{od.Product.ProductName}' không đủ hàng. Khả dụng: {avail}, cần: {od.Quantity}";
                    if (!dryRun)
                    {
                        od.Product.ReservedQuantity += od.Quantity;
                        od.Product.UpdatedAt = DateTime.Now;
                    }
                }
            }
            return null;
        }

        private static void DeductStock(IEnumerable<OrderDetail> details)
        {
            foreach (var od in details)
            {
                if (od.VariantId.HasValue && od.Variant != null)
                {
                    od.Variant.StockQuantity = Math.Max(0, od.Variant.StockQuantity - od.Quantity);
                    od.Variant.ReservedQuantity = Math.Max(0, od.Variant.ReservedQuantity - od.Quantity);
                    od.Variant.UpdatedAt = DateTime.Now;
                    od.Product.StockQuantity = Math.Max(0, od.Product.StockQuantity - od.Quantity);
                    od.Product.ReservedQuantity = Math.Max(0, od.Product.ReservedQuantity - od.Quantity);
                }
                else
                {
                    od.Product.StockQuantity = Math.Max(0, od.Product.StockQuantity - od.Quantity);
                    od.Product.ReservedQuantity = Math.Max(0, od.Product.ReservedQuantity - od.Quantity);
                }
                od.Product.UpdatedAt = DateTime.Now;
            }
        }

        private static void ReleaseReserved(IEnumerable<OrderDetail> details)
        {
            foreach (var od in details)
            {
                if (od.VariantId.HasValue && od.Variant != null)
                {
                    od.Variant.ReservedQuantity = Math.Max(0, od.Variant.ReservedQuantity - od.Quantity);
                    od.Variant.UpdatedAt = DateTime.Now;
                    od.Product.ReservedQuantity = Math.Max(0, od.Product.ReservedQuantity - od.Quantity);
                }
                else
                    od.Product.ReservedQuantity = Math.Max(0, od.Product.ReservedQuantity - od.Quantity);
                od.Product.UpdatedAt = DateTime.Now;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // DATA HELPERS
        // ─────────────────────────────────────────────────────────────────────

        private Task<Order?> LoadFull(int id) =>
            _context.Orders
                .Include(o => o.Facility)
                .Include(o => o.User)
                .Include(o => o.Booking).ThenInclude(b => b!.User)
                .Include(o => o.Booking).ThenInclude(b => b!.Court)
                .Include(o => o.OrderDetails).ThenInclude(od => od.Product)
                .Include(o => o.OrderDetails).ThenInclude(od => od.Variant)
                .Include(o => o.ServiceEnrollments!).ThenInclude(e => e.Course)
                .Include(o => o.ServiceEnrollments!).ThenInclude(e => e.StringingService)
                .Include(o => o.ServiceEnrollments!).ThenInclude(e => e.Tournament)
                .FirstOrDefaultAsync(o => o.OrderId == id);

        private static OrderViewModel Map(Order o)
        {
            var user = o.User ?? o.Booking?.User;
            var enrollment = o.ServiceEnrollments?.FirstOrDefault();

            // Với đơn dịch vụ: lấy tên khách từ enrollment
            var customerName = enrollment?.CustomerName
                ?? user?.FullName
                ?? "Khách vãng lai";
            var customerPhone = enrollment?.Phone ?? user?.Phone ?? "";
            var customerEmail = enrollment?.Email ?? user?.Email ?? "";

            // Tên dịch vụ cho display
            string serviceInfo = "";
            if (enrollment != null)
            {
                serviceInfo = enrollment.ServiceType switch
                {
                    "Course" => enrollment.Course?.CourseName ?? "Khóa học",
                    "Stringing" => enrollment.StringingService?.ServiceName ?? "Căng vợt",
                    "Tournament" => enrollment.Tournament?.TournamentName ?? "Giải đấu",
                    _ => ""
                };
            }

            return new OrderViewModel
            {
                OrderId = o.OrderId,
                OrderCode = o.OrderCode,
                Note = o.Note,
                OrderType = o.IsServiceOrder ? "service" : (o.BookingId.HasValue ? "booking" : "product"),
                ServiceType = o.ServiceTypeLabel,
                ServiceInfo = serviceInfo,
                BookingId = o.BookingId,
                BookingCode = o.Booking?.BookingCode,
                FacilityName = o.Facility?.FacilityName,
                CustomerName = customerName,
                CustomerPhone = customerPhone,
                CustomerEmail = customerEmail,
                SubTotal = o.SubTotal,
                DiscountAmount = o.DiscountAmount,
                TotalAmount = o.TotalAmount,
                OrderStatus = o.OrderStatus,
                PaymentStatus = o.PaymentStatus,
                PaymentMethod = o.PaymentMethod,
                CreatedAt = o.CreatedAt,
                UpdatedAt = o.UpdatedAt,
                CompletedAt = o.CompletedAt,
                OrderDetails = o.OrderDetails.Select(od => new OrderDetailViewModel
                {
                    ProductName = od.Product?.ProductName ?? "—",
                    VariantDisplay = od.Variant?.DisplayName,
                    Quantity = od.Quantity,
                    Unit = od.Product?.Unit ?? "",
                    UnitPrice = od.UnitPrice,
                    DiscountAmount = od.DiscountAmount,
                    TotalPrice = od.TotalPrice
                }).ToList()
            };
        }
    }
}
