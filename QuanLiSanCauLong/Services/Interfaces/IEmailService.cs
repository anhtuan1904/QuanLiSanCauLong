using Microsoft.AspNetCore.Mvc;
using QuanLiSanCauLong.Models;

namespace QuanLiSanCauLong.Services.Interfaces
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true);

        Task<bool> SendBookingConfirmationAsync(Booking booking);

        Task<bool> SendBookingCancellationAsync(Booking booking);

        Task<bool> SendPasswordResetAsync(string email, string resetToken);

        Task<bool> SendWelcomeEmailAsync(User user);

        Task<bool> SendOrderConfirmationAsync(Order order);
    }
}
