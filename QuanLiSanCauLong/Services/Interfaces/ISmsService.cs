using Microsoft.AspNetCore.Mvc;
using QuanLiSanCauLong.Models;

namespace QuanLiSanCauLong.Services.Interfaces
{
    public interface ISmsService
    {
        Task<bool> SendSmsAsync(string phoneNumber, string message);

        Task<bool> SendBookingReminderAsync(Booking booking);

        Task<bool> SendOtpAsync(string phoneNumber, string otp);

        Task<bool> SendBookingConfirmationSmsAsync(Booking booking);
    }
}
