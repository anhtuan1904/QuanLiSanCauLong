using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiSanCauLong.Data;
using QuanLiSanCauLong.Models;
using QuanLiSanCauLong.Services.Interfaces;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace QuanLiSanCauLong.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private readonly ApplicationDbContext _context;

        public EmailService(
            IConfiguration configuration,
            ILogger<EmailService> logger,
            ApplicationDbContext context)
        {
            _configuration = configuration;
            _logger = logger;
            _context = context;
        }

        public async Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true)
        {
            try
            {
                var smtpServer = _configuration["EmailSettings:SmtpServer"];
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"]);
                var smtpUsername = _configuration["EmailSettings:SmtpUsername"];
                var smtpPassword = _configuration["EmailSettings:SmtpPassword"];
                var fromEmail = _configuration["EmailSettings:FromEmail"];
                var fromName = _configuration["EmailSettings:FromName"];
                var enableSsl = bool.Parse(_configuration["EmailSettings:EnableSsl"]);

                using (var message = new MailMessage())
                {
                    message.From = new MailAddress(fromEmail, fromName);
                    message.To.Add(new MailAddress(to));
                    message.Subject = subject;
                    message.Body = body;
                    message.IsBodyHtml = isHtml;
                    message.BodyEncoding = Encoding.UTF8;
                    message.SubjectEncoding = Encoding.UTF8;

                    using (var smtpClient = new SmtpClient(smtpServer, smtpPort))
                    {
                        smtpClient.UseDefaultCredentials = false;
                        smtpClient.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
                        smtpClient.EnableSsl = enableSsl;
                        smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;

                        await smtpClient.SendMailAsync(message);
                    }
                }

                _logger.LogInformation($"Email sent successfully to {to}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email to {to}");
                return false;
            }
        }

        public async Task<bool> SendBookingConfirmationAsync(Booking booking)
        {
            try
            {
                var user = await _context.Users.FindAsync(booking.UserId);
                var court = await _context.Courts
                    .Include(c => c.Facility)
                    .FirstOrDefaultAsync(c => c.CourtId == booking.CourtId);

                if (user == null || court == null)
                    return false;

                var subject = $"Xác nhận đặt sân - {booking.BookingCode}";
                var body = BuildBookingConfirmationEmail(booking, user, court);

                return await SendEmailAsync(user.Email, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send booking confirmation for {booking.BookingCode}");
                return false;
            }
        }

        public async Task<bool> SendBookingCancellationAsync(Booking booking)
        {
            try
            {
                var user = await _context.Users.FindAsync(booking.UserId);
                var court = await _context.Courts
                    .Include(c => c.Facility)
                    .FirstOrDefaultAsync(c => c.CourtId == booking.CourtId);

                if (user == null || court == null)
                    return false;

                var subject = $"Hủy đặt sân - {booking.BookingCode}";
                var body = BuildBookingCancellationEmail(booking, user, court);

                return await SendEmailAsync(user.Email, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send booking cancellation for {booking.BookingCode}");
                return false;
            }
        }

        public async Task<bool> SendPasswordResetAsync(string email, string resetToken)
        {
            try
            {
                var resetLink = $"{_configuration["AppSettings:SiteUrl"]}/Account/ResetPassword?token={resetToken}";

                var subject = "Đặt lại mật khẩu";
                var body = BuildPasswordResetEmail(email, resetLink);

                return await SendEmailAsync(email, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send password reset email to {email}");
                return false;
            }
        }

        public async Task<bool> SendWelcomeEmailAsync(User user)
        {
            try
            {
                var subject = "Chào mừng bạn đến với Hệ thống Đặt sân Cầu lông";
                var body = BuildWelcomeEmail(user);

                return await SendEmailAsync(user.Email, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send welcome email to {user.Email}");
                return false;
            }
        }

        public async Task<bool> SendOrderConfirmationAsync(Order order)
        {
            try
            {
                var user = await _context.Users.FindAsync(order.UserId);
                var facility = await _context.Facilities.FindAsync(order.FacilityId);
                var orderDetails = await _context.OrderDetails
                    .Include(od => od.Product)
                    .Where(od => od.OrderId == order.OrderId)
                    .ToListAsync();

                if (user == null || facility == null)
                    return false;

                var subject = $"Xác nhận đơn hàng - {order.OrderCode}";
                var body = BuildOrderConfirmationEmail(order, user, facility, orderDetails);

                return await SendEmailAsync(user.Email, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send order confirmation for {order.OrderCode}");
                return false;
            }
        }

        // ===================================
        // EMAIL TEMPLATES
        // ===================================

        private string BuildBookingConfirmationEmail(Booking booking, User user, Court court)
        {
            return $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background: #4CAF50; color: white; padding: 20px; text-align: center; }}
                        .content {{ background: #f9f9f9; padding: 20px; border-radius: 5px; margin-top: 20px; }}
                        .info-row {{ margin: 10px 0; padding: 10px; background: white; border-radius: 3px; }}
                        .label {{ font-weight: bold; color: #555; }}
                        .footer {{ margin-top: 20px; padding: 20px; text-align: center; color: #777; font-size: 12px; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h2>Xác nhận đặt sân thành công!</h2>
                        </div>
                        <div class='content'>
                            <p>Xin chào <strong>{user.FullName}</strong>,</p>
                            <p>Đặt sân của bạn đã được xác nhận. Dưới đây là thông tin chi tiết:</p>
                            
                            <div class='info-row'>
                                <span class='label'>Mã đơn:</span> {booking.BookingCode}
                            </div>
                            <div class='info-row'>
                                <span class='label'>Cơ sở:</span> {court.Facility.FacilityName}
                            </div>
                            <div class='info-row'>
                                <span class='label'>Địa chỉ:</span> {court.Facility.Address}
                            </div>
                            <div class='info-row'>
                                <span class='label'>Sân:</span> {court.CourtNumber} ({court.CourtType})
                            </div>
                            <div class='info-row'>
                                <span class='label'>Ngày chơi:</span> {booking.BookingDate:dd/MM/yyyy}
                            </div>
                            <div class='info-row'>
                                <span class='label'>Giờ chơi:</span> {booking.StartTime:hh\\:mm} - {booking.EndTime:hh\\:mm}
                            </div>
                            <div class='info-row'>
                                <span class='label'>Tổng tiền:</span> <strong style='color: #4CAF50;'>{booking.TotalPrice:N0}đ</strong>
                            </div>
                            
                            <p style='margin-top: 20px;'>
                                <strong>Lưu ý:</strong> Vui lòng có mặt trước giờ chơi 15 phút để làm thủ tục check-in.
                            </p>
                        </div>
                        <div class='footer'>
                            <p>Cảm ơn bạn đã sử dụng dịch vụ của chúng tôi!</p>
                            <p>Mọi thắc mắc vui lòng liên hệ: {_configuration["AppSettings:SupportPhone"]}</p>
                        </div>
                    </div>
                </body>
                </html>
            ";
        }

        private string BuildBookingCancellationEmail(Booking booking, User user, Court court)
        {
            return $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background: #f44336; color: white; padding: 20px; text-align: center; }}
                        .content {{ background: #f9f9f9; padding: 20px; border-radius: 5px; margin-top: 20px; }}
                        .info-row {{ margin: 10px 0; padding: 10px; background: white; border-radius: 3px; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h2>Thông báo hủy đặt sân</h2>
                        </div>
                        <div class='content'>
                            <p>Xin chào <strong>{user.FullName}</strong>,</p>
                            <p>Đơn đặt sân <strong>{booking.BookingCode}</strong> đã được hủy.</p>
                            
                            <div class='info-row'>
                                <strong>Lý do hủy:</strong> {booking.CancelReason ?? "Không có"}
                            </div>
                            
                            <p>Nếu có thắc mắc, vui lòng liên hệ hotline: {_configuration["AppSettings:SupportPhone"]}</p>
                        </div>
                    </div>
                </body>
                </html>
            ";
        }

        private string BuildPasswordResetEmail(string email, string resetLink)
        {
            return $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <h2>Đặt lại mật khẩu</h2>
                        <p>Bạn nhận được email này vì đã yêu cầu đặt lại mật khẩu.</p>
                        <p>Click vào link dưới đây để đặt lại mật khẩu:</p>
                        <p><a href='{resetLink}' style='background: #4CAF50; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Đặt lại mật khẩu</a></p>
                        <p>Link này có hiệu lực trong 24 giờ.</p>
                        <p>Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này.</p>
                    </div>
                </body>
                </html>
            ";
        }

        private string BuildWelcomeEmail(User user)
        {
            return $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <h2>Chào mừng {user.FullName}!</h2>
                        <p>Cảm ơn bạn đã đăng ký tài khoản tại Hệ thống Đặt sân Cầu lông.</p>
                        <p>Bạn có thể bắt đầu đặt sân ngay bây giờ!</p>
                        <p><a href='{_configuration["AppSettings:SiteUrl"]}/Booking/Search'>Đặt sân ngay</a></p>
                    </div>
                </body>
                </html>
            ";
        }

        private string BuildOrderConfirmationEmail(Order order, User user, Facility facility, List<OrderDetail> details)
        {
            var itemsHtml = string.Join("", details.Select(d => $@"
                <tr>
                    <td>{d.Product.ProductName}</td>
                    <td style='text-align: center;'>{d.Quantity}</td>
                    <td style='text-align: right;'>{d.UnitPrice:N0}đ</td>
                    <td style='text-align: right;'>{d.TotalPrice:N0}đ</td>
                </tr>
            "));

            return $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                        <h2>Đơn hàng {order.OrderCode}</h2>
                        <p>Xin chào <strong>{user.FullName}</strong>,</p>
                        <p>Đơn hàng của bạn đã được xác nhận.</p>
                        
                        <table style='width: 100%; border-collapse: collapse; margin: 20px 0;'>
                            <thead>
                                <tr style='background: #f0f0f0;'>
                                    <th style='padding: 10px; text-align: left;'>Sản phẩm</th>
                                    <th style='padding: 10px; text-align: center;'>SL</th>
                                    <th style='padding: 10px; text-align: right;'>Đơn giá</th>
                                    <th style='padding: 10px; text-align: right;'>Thành tiền</th>
                                </tr>
                            </thead>
                            <tbody>
                                {itemsHtml}
                            </tbody>
                            <tfoot>
                                <tr>
                                    <td colspan='3' style='padding: 10px; text-align: right;'><strong>Tổng cộng:</strong></td>
                                    <td style='padding: 10px; text-align: right;'><strong>{order.TotalAmount:N0}đ</strong></td>
                                </tr>
                            </tfoot>
                        </table>
                    </div>
                </body>
                </html>
            ";
        }
    }
}
