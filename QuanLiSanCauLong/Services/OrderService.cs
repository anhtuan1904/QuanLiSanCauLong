using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiSanCauLong.Data;
using QuanLiSanCauLong.Models;
using QuanLiSanCauLong.Services.Interfaces;
using QuanLiSanCauLong.ViewModels;

namespace QuanLiSanCauLong.Services
{
    public class OrderService : IOrderService
    {
        private readonly ApplicationDbContext _context;
        private readonly IInventoryService _inventoryService;
        private readonly IEmailService _emailService;

        public OrderService(
            ApplicationDbContext context,
            IInventoryService inventoryService,
            IEmailService emailService)
        {
            _context = context;
            _inventoryService = inventoryService;
            _emailService = emailService;
        }

        public async Task<(bool Success, string Message, Order Order)> CreateOrderAsync(
            int userId, int facilityId, List<OrderItemViewModel> items, int? bookingId = null)
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // 1. Check stock availability
                    if (!await CheckStockAvailabilityAsync(facilityId, items))
                    {
                        return (false, "Một số sản phẩm không đủ số lượng trong kho!", null);
                    }

                    // 2. Calculate total
                    var subTotal = await CalculateOrderTotalAsync(items);

                    // 3. Create order
                    var orderCode = await GenerateOrderCodeAsync();

                    var order = new Order
                    {
                        OrderCode = orderCode,
                        BookingId = bookingId,
                        UserId = userId,
                        FacilityId = facilityId,
                        OrderType = "Product",
                        SubTotal = subTotal,
                        DiscountAmount = 0,
                        TotalAmount = subTotal,
                        OrderStatus = "Pending",
                        PaymentStatus = "Unpaid",
                        PaymentMethod = "Cash",
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };

                    _context.Orders.Add(order);
                    await _context.SaveChangesAsync();

                    // 4. Create order details
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

                    // 5. Update inventory
                    await UpdateInventoryAsync(facilityId, items);

                    // 6. Send confirmation email
                    await _emailService.SendOrderConfirmationAsync(order);

                    await transaction.CommitAsync();

                    return (true, $"Tạo đơn hàng thành công! Mã: {orderCode}", order);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return (false, $"Lỗi: {ex.Message}", null);
                }
            }
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderId, string status)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
                return false;

            order.OrderStatus = status;
            order.UpdatedAt = DateTime.Now;

            if (status == "Completed")
            {
                order.CompletedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ProcessPaymentAsync(int orderId, string paymentMethod)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
                return false;

            order.PaymentMethod = paymentMethod;
            order.PaymentStatus = "Paid";
            order.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<decimal> CalculateOrderTotalAsync(List<OrderItemViewModel> items)
        {
            return items.Sum(i => i.TotalPrice);
        }

        public async Task<string> GenerateOrderCodeAsync()
        {
            var today = DateTime.Today;
            var count = await _context.Orders.CountAsync(o => o.CreatedAt >= today);
            return "OD" + DateTime.Now.ToString("yyyyMMdd") + (count + 1).ToString("D4");
        }

        public async Task<bool> CheckStockAvailabilityAsync(int facilityId, List<OrderItemViewModel> items)
        {
            foreach (var item in items.Where(i => i.Quantity > 0))
            {
                var inventory = await _context.Inventories
                    .FirstOrDefaultAsync(i => i.ProductId == item.ProductId && i.FacilityId == facilityId);

                if (inventory == null || inventory.Quantity < item.Quantity)
                {
                    return false;
                }
            }

            return true;
        }

        public async Task<bool> UpdateInventoryAsync(int facilityId, List<OrderItemViewModel> items)
        {
            foreach (var item in items.Where(i => i.Quantity > 0))
            {
                var inventory = await _context.Inventories
                    .FirstOrDefaultAsync(i => i.ProductId == item.ProductId && i.FacilityId == facilityId);

                if (inventory != null)
                {
                    inventory.Quantity -= item.Quantity;
                    inventory.LastUpdated = DateTime.Now;
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
