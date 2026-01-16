using Microsoft.EntityFrameworkCore;
using QuanLiSanCauLong.Data;
using QuanLiSanCauLong.Models;
using QuanLiSanCauLong.Services.Interfaces;
using QuanLiSanCauLong.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuanLiSanCauLong.Services
{
    public class BookingService : IBookingService
    {
        private readonly ApplicationDbContext _context;
        private readonly IVoucherService _voucherService;
        private readonly IActivityLogService _activityLogService;
        private readonly IEmailService _emailService;

        public BookingService(
            ApplicationDbContext context,
            IVoucherService voucherService,
            IActivityLogService activityLogService,
            IEmailService emailService)
        {
            _context = context;
            _voucherService = voucherService;
            _activityLogService = activityLogService;
            _emailService = emailService;
        }

        public async Task<(bool Success, string Message, Booking Booking)> CreateBookingAsync(
            CreateBookingViewModel model, int userId)
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // 1. Validate court availability
                    if (!await IsCourtAvailableAsync(model.CourtId, model.BookingDate, model.StartTime, model.EndTime))
                    {
                        return (false, "Sân đã được đặt trong khung giờ này!", null);
                    }

                    // 2. Validate booking date
                    var maxDaysInAdvance = await GetSystemSettingAsync("MaxBookingDaysInAdvance", 30);
                    if (model.BookingDate > DateTime.Today.AddDays((double)maxDaysInAdvance))
                    {
                        return (false, $"Chỉ có thể đặt sân trước tối đa {maxDaysInAdvance} ngày!", null);
                    }

                    if (model.BookingDate < DateTime.Today)
                    {
                        return (false, "Không thể đặt sân trong quá khứ!", null);
                    }

                    // 3. Calculate price
                    var courtPrice = await CalculatePriceAsync(
                        model.CourtId, model.CourtType,
                        model.StartTime, model.EndTime, model.BookingDate);

                    var serviceFeePercent = await GetSystemSettingAsync("DefaultServiceFee", 0);
                    var serviceFee = courtPrice * serviceFeePercent / 100;

                    // 4. Apply voucher if provided
                    decimal voucherDiscount = 0;
                    int? voucherId = null;

                    if (!string.IsNullOrEmpty(model.VoucherCode))
                    {
                        var voucherResult = await _voucherService.ValidateVoucherAsync(
                            model.VoucherCode, userId, courtPrice + serviceFee, "Booking");

                        if (voucherResult.IsValid)
                        {
                            voucherDiscount = voucherResult.DiscountAmount;
                            voucherId = voucherResult.VoucherId;
                        }
                        else
                        {
                            return (false, voucherResult.Message, null);
                        }
                    }

                    // 5. Create booking
                    var bookingCode = await GenerateBookingCodeAsync();
                    var duration = (int)(model.EndTime - model.StartTime).TotalMinutes;

                    var booking = new Booking
                    {
                        BookingCode = bookingCode,
                        UserId = userId,
                        CourtId = model.CourtId,
                        BookingDate = model.BookingDate,
                        StartTime = model.StartTime,
                        EndTime = model.EndTime,
                        Duration = duration,
                        CourtPrice = courtPrice,
                        ServiceFee = serviceFee,
                        DiscountAmount = voucherDiscount,
                        TotalPrice = courtPrice + serviceFee - voucherDiscount,
                        Status = "Confirmed",
                        PaymentMethod = model.PaymentMethod ?? "Cash",
                        PaymentStatus = "Paid",
                        Note = model.Note,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };

                    _context.Bookings.Add(booking);
                    await _context.SaveChangesAsync();

                    // 6. Apply voucher usage
                    if (voucherId.HasValue)
                    {
                        await _voucherService.ApplyVoucherAsync(
                            voucherId.Value, userId, booking.BookingId, null, voucherDiscount);
                    }

                    // 7. Send confirmation email
                    await _emailService.SendBookingConfirmationAsync(booking);

                    await transaction.CommitAsync();

                    return (true, $"Đặt sân thành công! Mã đơn: {bookingCode}", booking);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return (false, $"Lỗi: {ex.Message}", null);
                }
            }
        }

        public async Task<bool> CancelBookingAsync(int bookingId, int userId, string reason)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);

            if (booking == null)
                return false;

            if (booking.UserId != userId)
                return false;

            if (!await CanCancelBookingAsync(booking))
                return false;

            booking.Status = "Cancelled";
            booking.CancelReason = reason;
            booking.CancelledAt = DateTime.Now;
            booking.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            // Send cancellation email
            await _emailService.SendBookingCancellationAsync(booking);

            return true;
        }

        public async Task<bool> IsCourtAvailableAsync(int courtId, DateTime date, TimeSpan start, TimeSpan end)
        {
            return !await _context.Bookings.AnyAsync(b =>
                b.CourtId == courtId
                && b.BookingDate == date
                && b.Status != "Cancelled"
                && b.StartTime < end
                && b.EndTime > start);
        }

        public async Task<decimal> CalculatePriceAsync(int courtId, string courtType,
            TimeSpan start, TimeSpan end, DateTime bookingDate)
        {
            var court = await _context.Courts
                .Include(c => c.Facility)
                .FirstOrDefaultAsync(c => c.CourtId == courtId);

            if (court == null)
                return 0;

            int dayOfWeek = (int)bookingDate.DayOfWeek;

            var priceSlots = await _context.PriceSlots
                .Where(p => p.FacilityId == court.FacilityId
                       && p.CourtType == courtType
                       && p.IsActive
                       && (p.DayOfWeek == null || p.DayOfWeek == dayOfWeek)
                       && p.StartTime >= start
                       && p.EndTime <= end)
                .ToListAsync();

            return priceSlots.Sum(p => p.Price);
        }

        public async Task<List<TimeSlotViewModel>> GetAvailableTimeSlotsAsync(int courtId, DateTime date)
        {
            var court = await _context.Courts
                .Include(c => c.Facility)
                .FirstOrDefaultAsync(c => c.CourtId == courtId);

            if (court == null)
                return new List<TimeSlotViewModel>();

            int dayOfWeek = (int)date.DayOfWeek;

            var priceSlots = await _context.PriceSlots
                .Where(p => p.FacilityId == court.FacilityId
                       && p.CourtType == court.CourtType
                       && p.IsActive
                       && (p.DayOfWeek == null || p.DayOfWeek == dayOfWeek))
                .OrderBy(p => p.StartTime)
                .ToListAsync();

            var bookedSlots = await _context.Bookings
                .Where(b => b.CourtId == courtId
                       && b.BookingDate == date
                       && b.Status != "Cancelled")
                .ToListAsync();

            var timeSlots = new List<TimeSlotViewModel>();

            foreach (var slot in priceSlots)
            {
                bool isBooked = bookedSlots.Any(b =>
                    b.StartTime < slot.EndTime && b.EndTime > slot.StartTime);

                timeSlots.Add(new TimeSlotViewModel
                {
                    StartTime = slot.StartTime,
                    EndTime = slot.EndTime,
                    Price = slot.Price,
                    IsPeakHour = slot.IsPeakHour,
                    IsAvailable = !isBooked
                });
            }

            return timeSlots;
        }

        public async Task<bool> CanCancelBookingAsync(Booking booking)
        {
            if (booking.Status != "Confirmed" && booking.Status != "Pending")
                return false;

            var bookingDateTime = booking.BookingDate.Add(booking.StartTime);
            var cancelHours = await GetSystemSettingAsync("BookingCancellationHours", 2);

            return bookingDateTime > DateTime.Now.AddHours((double)cancelHours);
        }

        public async Task<string> GenerateBookingCodeAsync()
        {
            var today = DateTime.Today;
            var count = await _context.Bookings.CountAsync(b => b.CreatedAt >= today);
            return "BK" + DateTime.Now.ToString("yyyyMMdd") + (count + 1).ToString("D4");
        }

        // Helper method
        private async Task<decimal> GetSystemSettingAsync(string key, decimal defaultValue)
        {
            var setting = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.SettingKey == key && s.IsActive);

            if (setting != null && decimal.TryParse(setting.SettingValue, out decimal value))
                return value;

            return defaultValue;
        }
    }
}