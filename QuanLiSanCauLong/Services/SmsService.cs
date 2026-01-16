using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiSanCauLong.Data;
using QuanLiSanCauLong.Models;
using QuanLiSanCauLong.Services.Interfaces;

namespace QuanLiSanCauLong.Services
{
    // ===================================
    // SMS SERVICE IMPLEMENTATION
    // ===================================

    public class SmsService : ISmsService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmsService> _logger;

        public SmsService(IConfiguration configuration, ILogger<SmsService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendSmsAsync(string phoneNumber, string message)
        {
            try
            {
                var enabled = bool.Parse(_configuration["SmsSettings:Enabled"] ?? "false");
                if (!enabled)
                {
                    _logger.LogWarning("SMS service is disabled");
                    return false;
                }

                // TODO: Implement SMS provider (Twilio, VNPT, Viettel, etc.)
                // Example with Twilio:
                // var accountSid = _configuration["SmsSettings:AccountSid"];
                // var authToken = _configuration["SmsSettings:AuthToken"];
                // var fromNumber = _configuration["SmsSettings:PhoneNumber"];

                _logger.LogInformation($"SMS would be sent to {phoneNumber}: {message}");

                await Task.CompletedTask; // Placeholder
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send SMS to {phoneNumber}");
                return false;
            }
        }

        public async Task<bool> SendBookingReminderAsync(Booking booking)
        {
            var message = $"Nhắc nhở: Bạn có lịch chơi cầu lông vào {booking.BookingDate:dd/MM/yyyy} lúc {booking.StartTime:hh\\:mm}. Mã đơn: {booking.BookingCode}";
            // Get phone from user
            return await SendSmsAsync("phone_number", message);
        }

        public async Task<bool> SendOtpAsync(string phoneNumber, string otp)
        {
            var message = $"Mã OTP của bạn là: {otp}. Có hiệu lực trong 5 phút.";
            return await SendSmsAsync(phoneNumber, message);
        }

        public async Task<bool> SendBookingConfirmationSmsAsync(Booking booking)
        {
            var message = $"Đặt sân thành công! Mã: {booking.BookingCode}. Ngày: {booking.BookingDate:dd/MM/yyyy} {booking.StartTime:hh\\:mm}-{booking.EndTime:hh\\:mm}";
            // Get phone from user
            return await SendSmsAsync("phone_number", message);
        }
    }

    // ===================================
    // NOTIFICATION SERVICE IMPLEMENTATION
    // ===================================

    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            ApplicationDbContext context,
            IEmailService emailService,
            ILogger<NotificationService> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task NotifyBookingCreatedAsync(Booking booking)
        {
            await _emailService.SendBookingConfirmationAsync(booking);
        }

        public async Task NotifyBookingCancelledAsync(Booking booking)
        {
            await _emailService.SendBookingCancellationAsync(booking);
        }

        public async Task NotifyLowStockAsync(Inventory inventory)
        {
            try
            {
                var product = await _context.Products.FindAsync(inventory.ProductId);
                var facility = await _context.Facilities.FindAsync(inventory.FacilityId);

                var adminEmails = await _context.Users
                    .Where(u => u.Role == "Admin" && u.IsActive)
                    .Select(u => u.Email)
                    .ToListAsync();

                var subject = $"Cảnh báo tồn kho thấp - {product?.ProductName}";
                var body = $@"
                    <p>Sản phẩm <strong>{product?.ProductName}</strong> tại <strong>{facility?.FacilityName}</strong> 
                    còn <strong>{inventory.Quantity}</strong> (ngưỡng tối thiểu: {inventory.MinQuantity})</p>
                    <p>Vui lòng nhập thêm hàng.</p>
                ";

                foreach (var email in adminEmails)
                {
                    await _emailService.SendEmailAsync(email, subject, body);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send low stock notification");
            }
        }

        public async Task NotifyNewOrderAsync(Order order)
        {
            await _emailService.SendOrderConfirmationAsync(order);
        }

        public Task<List<Notification>> GetUserNotificationsAsync(int userId, bool unreadOnly = false)
        {
            // TODO: Implement notification storage if needed
            return Task.FromResult(new List<Notification>());
        }

        public Task<bool> MarkAsReadAsync(int notificationId)
        {
            // TODO: Implement notification read status
            return Task.FromResult(true);
        }
    }
}
