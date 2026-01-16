using Microsoft.AspNetCore.Mvc;
using QuanLiSanCauLong.Models;
using QuanLiSanCauLong.ViewModels;

namespace QuanLiSanCauLong.Services.Interfaces
{
    public interface IBookingService
    {
        Task<(bool Success, string Message, Booking Booking)> CreateBookingAsync(
            CreateBookingViewModel model, int userId);

        Task<bool> CancelBookingAsync(int bookingId, int userId, string reason);

        Task<bool> IsCourtAvailableAsync(int courtId, DateTime date, TimeSpan start, TimeSpan end);

        Task<decimal> CalculatePriceAsync(int facilityId, string courtType,
            TimeSpan start, TimeSpan end, DateTime bookingDate);

        Task<List<TimeSlotViewModel>> GetAvailableTimeSlotsAsync(int courtId, DateTime date);

        Task<bool> CanCancelBookingAsync(Booking booking);

        Task<string> GenerateBookingCodeAsync();
    }
}
