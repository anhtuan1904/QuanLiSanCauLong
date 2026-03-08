using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiSanCauLong.Data;
using QuanLiSanCauLong.Models;
using QuanLiSanCauLong.ViewModels;

namespace QuanLiSanCauLong.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminOrderController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminOrderController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(DateTime? fromDate, DateTime? toDate,
            string status, string orderType)
        {
            var query = _context.Orders
                .Include(o => o.Facility)
                .Include(o => o.Booking)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Variant)
                .AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(o => o.CreatedAt >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(o => o.CreatedAt <= toDate.Value.AddDays(1));

            if (!string.IsNullOrEmpty(status))
                query = query.Where(o => o.OrderStatus == status);

            if (!string.IsNullOrEmpty(orderType))
            {
                if (orderType == "booking")
                    query = query.Where(o => o.BookingId != null);
                else if (orderType == "product")
                    query = query.Where(o => o.BookingId == null);
            }

            var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();

            var model = orders.Select(o => new OrderViewModel
            {
                OrderId = o.OrderId,
                OrderCode = o.OrderCode,
                // "booking" = có BookingId, "product" = đơn sản phẩm thường
                // Khớp với data-filter trong tab buttons ở View
                OrderType = o.BookingId.HasValue ? "booking" : "product",
                SubTotal = o.SubTotal,
                DiscountAmount = o.DiscountAmount,
                TotalAmount = o.TotalAmount,
                OrderStatus = o.OrderStatus,
                PaymentStatus = o.PaymentStatus,
                CreatedAt = o.CreatedAt,
                OrderDetails = o.OrderDetails.Select(od => new OrderDetailViewModel
                {
                    ProductName = od.Product.ProductName,
                    VariantDisplay = od.Variant?.DisplayName,
                    Quantity = od.Quantity,
                    Unit = od.Product.Unit,
                    UnitPrice = od.UnitPrice,
                    DiscountAmount = od.DiscountAmount,
                    TotalPrice = od.TotalPrice
                }).ToList()
            }).ToList();

            ViewBag.TotalOrders = model.Count;
            ViewBag.PendingOrders = model.Count(o => o.OrderStatus == "Pending");
            ViewBag.ShippingOrders = model.Count(o => o.OrderStatus == "Shipping");
            ViewBag.CompletedOrders = model.Count(o => o.OrderStatus == "Completed");
            ViewBag.TotalRevenue = model.Where(o => o.OrderStatus == "Completed").Sum(o => o.TotalAmount);

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Facility)
                .Include(o => o.Booking).ThenInclude(b => b.Court)
                .Include(o => o.OrderDetails).ThenInclude(od => od.Product)
                .Include(o => o.OrderDetails).ThenInclude(od => od.Variant)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null) return NotFound();

            var model = new OrderViewModel
            {
                OrderId = order.OrderId,
                OrderCode = order.OrderCode,
                Note = order.Note,
                OrderType = order.BookingId.HasValue ? "booking" : "product",
                SubTotal = order.SubTotal,
                DiscountAmount = order.DiscountAmount,
                TotalAmount = order.TotalAmount,
                OrderStatus = order.OrderStatus,
                PaymentStatus = order.PaymentStatus,
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt,
                CompletedAt = order.CompletedAt,
                OrderDetails = order.OrderDetails.Select(od => new OrderDetailViewModel
                {
                    ProductName = od.Product.ProductName,
                    VariantDisplay = od.Variant?.DisplayName,
                    Quantity = od.Quantity,
                    Unit = od.Product.Unit,
                    UnitPrice = od.UnitPrice,
                    DiscountAmount = od.DiscountAmount,
                    TotalPrice = od.TotalPrice
                }).ToList()
            };

            return View(model);
        }

        /// <summary>
        /// Xác nhận đơn hàng: Pending → Confirmed
        /// → Treo (reserve) số lượng tồn kho theo từng variant (hoặc product nếu không có variant).
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Confirm(int orderId)
        {
            if (orderId <= 0)
                return Json(new { success = false, message = "ID không hợp lệ!" });

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Variant)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                        .ThenInclude(p => p.Variants)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                return Json(new { success = false, message = "Không tìm thấy đơn hàng!" });

            if (order.OrderStatus != "Pending")
                return Json(new { success = false, message = "Chỉ có thể xác nhận đơn đang chờ!" });

            // Kiểm tra tồn khả dụng trước khi reserve
            foreach (var od in order.OrderDetails)
            {
                if (od.VariantId.HasValue && od.Variant != null)
                {
                    if (od.Variant.AvailableQuantity < od.Quantity)
                        return Json(new
                        {
                            success = false,
                            message = $"Sản phẩm '{od.Product.ProductName}' ({od.Variant.DisplayName}) " +
                                      $"không đủ hàng. Khả dụng: {od.Variant.AvailableQuantity}, cần: {od.Quantity}"
                        });
                }
                else
                {
                    // Sản phẩm không có variant — kiểm tra Product.AvailableQuantity
                    var availableQty = od.Product.StockQuantity - od.Product.ReservedQuantity;
                    if (availableQty < od.Quantity)
                        return Json(new
                        {
                            success = false,
                            message = $"Sản phẩm '{od.Product.ProductName}' " +
                                      $"không đủ hàng. Khả dụng: {availableQty}, cần: {od.Quantity}"
                        });
                }
            }

            // Thực hiện reserve
            foreach (var od in order.OrderDetails)
            {
                if (od.VariantId.HasValue && od.Variant != null)
                {
                    od.Variant.ReservedQuantity += od.Quantity;
                    od.Variant.UpdatedAt = DateTime.Now;

                    // Sync lên Product
                    od.Product.ReservedQuantity += od.Quantity;
                    od.Product.UpdatedAt = DateTime.Now;
                }
                else
                {
                    od.Product.ReservedQuantity += od.Quantity;
                    od.Product.UpdatedAt = DateTime.Now;
                }
            }

            order.OrderStatus = "Confirmed";
            order.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Xác nhận đơn hàng thành công! Đã treo tồn kho." });
        }

        /// <summary>
        /// Hoàn thành đơn hàng: Confirmed → Completed
        /// → Trừ thật StockQuantity và bỏ ReservedQuantity.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Complete(int orderId)
        {
            if (orderId <= 0)
                return Json(new { success = false, message = "ID không hợp lệ!" });

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Variant)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                return Json(new { success = false, message = "Không tìm thấy đơn hàng!" });

            if (order.OrderStatus != "Confirmed")
                return Json(new { success = false, message = "Chỉ có thể hoàn thành đơn đã xác nhận!" });

            // Trừ tồn thật + giải phóng reserved
            foreach (var od in order.OrderDetails)
            {
                if (od.VariantId.HasValue && od.Variant != null)
                {
                    od.Variant.StockQuantity -= od.Quantity;
                    od.Variant.ReservedQuantity -= od.Quantity;
                    if (od.Variant.StockQuantity < 0) od.Variant.StockQuantity = 0;
                    if (od.Variant.ReservedQuantity < 0) od.Variant.ReservedQuantity = 0;
                    od.Variant.UpdatedAt = DateTime.Now;

                    // Sync lên Product
                    od.Product.StockQuantity -= od.Quantity;
                    od.Product.ReservedQuantity -= od.Quantity;
                    if (od.Product.StockQuantity < 0) od.Product.StockQuantity = 0;
                    if (od.Product.ReservedQuantity < 0) od.Product.ReservedQuantity = 0;
                    od.Product.UpdatedAt = DateTime.Now;
                }
                else
                {
                    od.Product.StockQuantity -= od.Quantity;
                    od.Product.ReservedQuantity -= od.Quantity;
                    if (od.Product.StockQuantity < 0) od.Product.StockQuantity = 0;
                    if (od.Product.ReservedQuantity < 0) od.Product.ReservedQuantity = 0;
                    od.Product.UpdatedAt = DateTime.Now;
                }
            }

            order.OrderStatus = "Completed";
            order.PaymentStatus = "Paid";
            order.CompletedAt = DateTime.Now;
            order.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Hoàn thành đơn hàng! Đã xuất kho." });
        }

        /// <summary>
        /// Hủy đơn hàng.
        /// Nếu đơn đã ở trạng thái Confirmed → giải phóng reserved.
        /// Nếu đơn chưa Confirmed (Pending) → không có reserved, chỉ đổi status.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Cancel(int orderId, string reason)
        {
            if (orderId <= 0)
                return Json(new { success = false, message = "ID không hợp lệ!" });

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Variant)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                return Json(new { success = false, message = "Không tìm thấy đơn hàng!" });

            if (order.OrderStatus == "Cancelled")
                return Json(new { success = false, message = "Đơn đã được hủy trước đó!" });

            if (order.OrderStatus == "Completed")
                return Json(new { success = false, message = "Không thể hủy đơn đã hoàn thành!" });

            // Nếu đơn đã Confirmed → phải giải phóng reserved
            if (order.OrderStatus == "Confirmed")
            {
                foreach (var od in order.OrderDetails)
                {
                    if (od.VariantId.HasValue && od.Variant != null)
                    {
                        od.Variant.ReservedQuantity -= od.Quantity;
                        if (od.Variant.ReservedQuantity < 0) od.Variant.ReservedQuantity = 0;
                        od.Variant.UpdatedAt = DateTime.Now;

                        od.Product.ReservedQuantity -= od.Quantity;
                        if (od.Product.ReservedQuantity < 0) od.Product.ReservedQuantity = 0;
                        od.Product.UpdatedAt = DateTime.Now;
                    }
                    else
                    {
                        od.Product.ReservedQuantity -= od.Quantity;
                        if (od.Product.ReservedQuantity < 0) od.Product.ReservedQuantity = 0;
                        od.Product.UpdatedAt = DateTime.Now;
                    }
                }
            }

            order.OrderStatus = "Cancelled";
            order.Note = reason;
            order.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Hủy đơn hàng thành công!" });
        }

        /// <summary>
        /// API: Lấy danh sách variants của 1 product (dùng cho dropdown khi tạo đơn).
        /// GET /AdminOrder/GetVariants?productId=5
        /// </summary>
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
    }
}
