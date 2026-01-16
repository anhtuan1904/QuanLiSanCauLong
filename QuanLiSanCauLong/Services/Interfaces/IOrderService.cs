using Microsoft.AspNetCore.Mvc;
using QuanLiSanCauLong.Models;
using QuanLiSanCauLong.ViewModels;

namespace QuanLiSanCauLong.Services.Interfaces
{
    public interface IOrderService
    {
        Task<(bool Success, string Message, Order Order)> CreateOrderAsync(
            int userId, int facilityId, List<OrderItemViewModel> items, int? bookingId = null);

        Task<bool> UpdateOrderStatusAsync(int orderId, string status);

        Task<bool> ProcessPaymentAsync(int orderId, string paymentMethod);

        Task<decimal> CalculateOrderTotalAsync(List<OrderItemViewModel> items);

        Task<string> GenerateOrderCodeAsync();

        Task<bool> CheckStockAvailabilityAsync(int facilityId, List<OrderItemViewModel> items);

        Task<bool> UpdateInventoryAsync(int facilityId, List<OrderItemViewModel> items);
    }
}
