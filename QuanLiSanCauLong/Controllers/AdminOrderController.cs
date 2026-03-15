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
                .AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(o => o.CreatedAt >= fromDate.Value.Date);
            if (toDate.HasValue)
                query = query.Where(o => o.CreatedAt < toDate.Value.Date.AddDays(1));
            if (!string.IsNullOrEmpty(status))
                query = query.Where(o => o.OrderStatus == status);
            if (orderType == "booking")
                query = query.Where(o => o.BookingId != null);
            else if (orderType == "product")
                query = query.Where(o => o.BookingId == null);
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(o => o.OrderCode.ToLower().Contains(s) ||
                    (o.User != null && o.User.FullName.ToLower().Contains(s)));
            }

            int total = await query.CountAsync();
            var orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Badge counts — 1 query
            var counts = await _context.Orders
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    Pending = g.Count(o => o.OrderStatus == "Pending"),
                    Confirmed = g.Count(o => o.OrderStatus == "Confirmed"),
                    Shipping = g.Count(o => o.OrderStatus == "Shipping"),
                    Completed = g.Count(o => o.OrderStatus == "Completed"),
                    Revenue = g.Where(o => o.OrderStatus == "Completed").Sum(o => o.TotalAmount)
                })
                .FirstOrDefaultAsync();

            ViewBag.TotalOrders = counts?.Total ?? 0;
            ViewBag.PendingOrders = counts?.Pending ?? 0;
            ViewBag.ShippingOrders = counts?.Shipping ?? 0;
            ViewBag.CompletedOrders = counts?.Completed ?? 0;
            ViewBag.TotalRevenue = counts?.Revenue ?? 0m;
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
        // SET STATUS  ← gộp Confirm + Complete + Cancel
        //
        //  newStatus  | Hành động kho
        // ────────────┼────────────────────────────────────────────────────────
        //  Confirmed  | Reserve tồn kho (kiểm tra đủ hàng trước)
        //  Completed  | Trừ thật StockQty + giải phóng Reserved + Paid
        //  Cancelled  | Giải phóng Reserved nếu đang Confirmed
        //  Shipping   | Chỉ đổi status (pick-up / giao ngoài)
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
                return Json(new
                {
                    success = false,
                    message = $"Không thể thay đổi đơn đã {order.OrderStatus}!"
                });

            string old = order.OrderStatus;

            switch (newStatus)
            {
                // ── Xác nhận: Pending → Confirmed, reserve kho ──
                case "Confirmed":
                    if (old != "Pending")
                        return Json(new
                        {
                            success = false,
                            message = "Chỉ xác nhận được đơn đang Chờ xử lý!"
                        });

                    var stockErr = CheckAndReserve(order.OrderDetails, dryRun: true);
                    if (stockErr != null)
                        return Json(new { success = false, message = stockErr });

                    CheckAndReserve(order.OrderDetails, dryRun: false); // thực hiện reserve
                    order.OrderStatus = "Confirmed";
                    break;

                // ── Đang giao (không thay đổi kho) ──
                case "Shipping":
                    if (old != "Confirmed")
                        return Json(new
                        {
                            success = false,
                            message = "Chỉ chuyển sang Đang giao từ Đã xác nhận!"
                        });
                    order.OrderStatus = "Shipping";
                    break;

                // ── Hoàn thành: trừ kho thật + giải Reserved ──
                case "Completed":
                    if (old is not ("Confirmed" or "Shipping"))
                        return Json(new
                        {
                            success = false,
                            message = "Chỉ hoàn thành được đơn đã Xác nhận hoặc Đang giao!"
                        });

                    DeductStock(order.OrderDetails);
                    order.OrderStatus = "Completed";
                    order.PaymentStatus = "Paid";
                    order.CompletedAt = DateTime.Now;
                    break;

                // ── Hủy: giải Reserved nếu đang Confirmed/Shipping ──
                case "Cancelled":
                    if (string.IsNullOrWhiteSpace(reason))
                        return Json(new
                        {
                            success = false,
                            message = "Vui lòng nhập lý do hủy!"
                        });

                    if (old is "Confirmed" or "Shipping")
                        ReleaseReserved(order.OrderDetails);

                    order.OrderStatus = "Cancelled";
                    order.Note = reason.Trim();
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

            return Json(new
            {
                success = true,
                message = $"Đơn {order.OrderCode} → {label}."
            });
        }

        // ═════════════════════════════════════════════════════════════════════
        // API: Lấy variants của 1 product (cho dropdown tạo đơn)
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
        // INVENTORY HELPERS (tách ra để SetStatus không bị lồng nhau)
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Kiểm tra đủ hàng. dryRun=true → chỉ validate, trả về error string hoặc null.
        /// dryRun=false → thực hiện reserve luôn.
        /// </summary>
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
                        return $"'{od.Product.ProductName}' không đủ hàng. " +
                               $"Khả dụng: {avail}, cần: {od.Quantity}";

                    if (!dryRun)
                    {
                        od.Product.ReservedQuantity += od.Quantity;
                        od.Product.UpdatedAt = DateTime.Now;
                    }
                }
            }
            return null;
        }

        /// <summary>Trừ StockQuantity thật + giải phóng ReservedQuantity sau khi hoàn thành.</summary>
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

        /// <summary>Giải phóng ReservedQuantity khi hủy đơn đang Confirmed/Shipping.</summary>
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
                {
                    od.Product.ReservedQuantity = Math.Max(0, od.Product.ReservedQuantity - od.Quantity);
                }
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
                .Include(o => o.Booking)
                    .ThenInclude(b => b!.User)
                .Include(o => o.Booking)
                    .ThenInclude(b => b!.Court)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Variant)
                .FirstOrDefaultAsync(o => o.OrderId == id);

        private static OrderViewModel Map(Order o)
        {
            // Khách hàng: ưu tiên User trực tiếp, fallback User từ Booking
            var user = o.User ?? o.Booking?.User;

            return new OrderViewModel
            {
                OrderId = o.OrderId,
                OrderCode = o.OrderCode,
                Note = o.Note,
                // Loại đơn: liên kết booking hay đơn sản phẩm độc lập
                OrderType = o.BookingId.HasValue ? "booking" : "product",
                BookingId = o.BookingId,
                BookingCode = o.Booking?.BookingCode,
                FacilityName = o.Facility?.FacilityName ?? o.Booking?.Court?.Facility?.FacilityName,
                // Thông tin khách hàng
                CustomerName = user?.FullName ?? "Khách vãng lai",
                CustomerPhone = user?.Phone ?? "",
                CustomerEmail = user?.Email ?? "",
                SubTotal = o.SubTotal,
                DiscountAmount = o.DiscountAmount,
                TotalAmount = o.TotalAmount,
                OrderStatus = o.OrderStatus,
                PaymentStatus = o.PaymentStatus,
                CreatedAt = o.CreatedAt,
                UpdatedAt = o.UpdatedAt,
                CompletedAt = o.CompletedAt,
                OrderDetails = o.OrderDetails.Select(od => new OrderDetailViewModel
                {
                    ProductName = od.Product?.ProductName ?? "Sản phẩm không xác định",
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
