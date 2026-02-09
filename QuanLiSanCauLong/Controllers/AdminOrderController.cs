using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiSanCauLong.Data;
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
                .AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(o => o.CreatedAt >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(o => o.CreatedAt <= toDate.Value.AddDays(1));

            if (!string.IsNullOrEmpty(status))
                query = query.Where(o => o.OrderStatus == status);

            if (!string.IsNullOrEmpty(orderType))
            {
                if (orderType == "WithBooking")
                    query = query.Where(o => o.BookingId != null);
                else if (orderType == "Standalone")
                    query = query.Where(o => o.BookingId == null);
            }

            var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();

            var model = orders.Select(o => new OrderViewModel
            {
                OrderId = o.OrderId,
                OrderCode = o.OrderCode,
                OrderType = o.BookingId.HasValue ? "WithBooking" : "Standalone",
                SubTotal = o.SubTotal,
                DiscountAmount = o.DiscountAmount,
                TotalAmount = o.TotalAmount,
                OrderStatus = o.OrderStatus,
                PaymentStatus = o.PaymentStatus,
                CreatedAt = o.CreatedAt,
                OrderDetails = o.OrderDetails.Select(od => new OrderDetailViewModel
                {
                    ProductName = od.Product.ProductName,
                    Quantity = od.Quantity,
                    Unit = od.Product.Unit,
                    UnitPrice = od.UnitPrice,
                    DiscountAmount = od.DiscountAmount,
                    TotalPrice = od.TotalPrice
                }).ToList()
            }).ToList();

            ViewBag.TotalOrders = model.Count;
            ViewBag.PendingOrders = model.Count(o => o.OrderStatus == "Pending");
            ViewBag.CompletedOrders = model.Count(o => o.OrderStatus == "Completed");
            ViewBag.TotalRevenue = model.Where(o => o.OrderStatus == "Completed").Sum(o => o.TotalAmount);

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Facility)
                .Include(o => o.Booking)
                    .ThenInclude(b => b.Court)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
                return NotFound();

            var model = new OrderViewModel
            {
                OrderId = order.OrderId,
                OrderCode = order.OrderCode,
                Note = order.Note,
                OrderType = order.BookingId.HasValue ? "WithBooking" : "Standalone",
                SubTotal = order.SubTotal,
                DiscountAmount = order.DiscountAmount,
                TotalAmount = order.TotalAmount,
                OrderStatus = order.OrderStatus,
                PaymentStatus = order.PaymentStatus,
                CreatedAt = order.CreatedAt,
                OrderDetails = order.OrderDetails.Select(od => new OrderDetailViewModel
                {
                    ProductName = od.Product.ProductName,
                    Quantity = od.Quantity,
                    Unit = od.Product.Unit,
                    UnitPrice = od.UnitPrice,
                    DiscountAmount = od.DiscountAmount,
                    TotalPrice = od.TotalPrice
                }).ToList()
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Confirm(int orderId)
        {
            if (orderId <= 0)
                return Json(new { success = false, message = "ID không hợp lệ!" });

            var order = await _context.Orders.FindAsync(orderId);

            if (order == null)
                return Json(new { success = false, message = "Không tìm thấy đơn hàng!" });

            if (order.OrderStatus != "Pending")
                return Json(new { success = false, message = "Chỉ có thể xác nhận đơn đang chờ!" });

            order.OrderStatus = "Confirmed";
            order.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Xác nhận đơn hàng thành công!" });
        }

        [HttpPost]
        public async Task<IActionResult> Complete(int orderId)
        {
            if (orderId <= 0)
                return Json(new { success = false, message = "ID không hợp lệ!" });

            var order = await _context.Orders.FindAsync(orderId);

            if (order == null)
                return Json(new { success = false, message = "Không tìm thấy đơn hàng!" });

            if (order.OrderStatus != "Confirmed")
                return Json(new { success = false, message = "Chỉ có thể hoàn thành đơn đã xác nhận!" });

            order.OrderStatus = "Completed";
            order.PaymentStatus = "Paid";
            order.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Hoàn thành đơn hàng thành công!" });
        }

        [HttpPost]
        public async Task<IActionResult> Cancel(int orderId, string reason)
        {
            if (orderId <= 0)
                return Json(new { success = false, message = "ID không hợp lệ!" });

            var order = await _context.Orders.FindAsync(orderId);

            if (order == null)
                return Json(new { success = false, message = "Không tìm thấy đơn hàng!" });

            if (order.OrderStatus == "Cancelled")
                return Json(new { success = false, message = "Đơn đã được hủy trước đó!" });

            if (order.OrderStatus == "Completed")
                return Json(new { success = false, message = "Không thể hủy đơn đã hoàn thành!" });

            order.OrderStatus = "Cancelled";
            order.Note = reason;
            order.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Hủy đơn hàng thành công!" });
        }
    }
}