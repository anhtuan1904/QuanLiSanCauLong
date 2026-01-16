using Microsoft.AspNetCore.Mvc;
using QuanLiSanCauLong.Models;

namespace QuanLiSanCauLong.Services.Interfaces
{
    public interface INotificationService
    {
        Task NotifyBookingCreatedAsync(Booking booking);

        Task NotifyBookingCancelledAsync(Booking booking);

        Task NotifyLowStockAsync(Inventory inventory);

        Task NotifyNewOrderAsync(Order order);

        Task<List<Notification>> GetUserNotificationsAsync(int userId, bool unreadOnly = false);

        Task<bool> MarkAsReadAsync(int notificationId);
    }
}
