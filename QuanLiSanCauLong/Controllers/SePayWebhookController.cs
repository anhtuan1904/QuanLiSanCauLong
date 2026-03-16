using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiSanCauLong.Data;
using QuanLiSanCauLong.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace QuanLiSanCauLong.Controllers
{
    [ApiController]
    [Route("api/sepay")]
    public class SePayWebhookController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;
        private readonly ILogger<SePayWebhookController> _logger;

        public SePayWebhookController(
            ApplicationDbContext context,
            IConfiguration config,
            ILogger<SePayWebhookController> logger)
        {
            _context = context;
            _config = config;
            _logger = logger;
        }

        // ─────────────────────────────────────────────────────────────────
        // POST /api/sepay/webhook
        // ─────────────────────────────────────────────────────────────────
        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook([FromBody] SePayPayload payload)
        {
            // ── 1. Xác thực API key ──
            string? secret = _config["SePay:WebhookSecret"];
            if (!string.IsNullOrEmpty(secret))
            {
                Request.Headers.TryGetValue("Authorization", out var authHeader);
                // SePay gửi: Authorization: Apikey <your_secret>
                string headerVal = authHeader.ToString().Replace("Apikey ", "").Trim();
                if (headerVal != secret)
                {
                    _logger.LogWarning("SePay webhook: sai API key từ IP {IP}",
                        HttpContext.Connection.RemoteIpAddress);
                    return Unauthorized(new { success = false, message = "Unauthorized" });
                }
            }

            // ── 2. Chỉ xử lý giao dịch tiền vào (credit) ──
            if (payload == null)
                return BadRequest(new { success = false, message = "Empty payload" });

            _logger.LogInformation(
                "SePay webhook nhận: id={Id} | content={Content} | amount={Amount} | account={Acc}",
                payload.Id, payload.TransferContent, payload.TransferAmount, payload.AccountNumber);

            // Bỏ qua giao dịch tiền ra (debit)
            if (payload.TransferType == "out")
                return Ok(new { success = true, message = "Ignored outgoing" });

            // ── 3. Tách BookingCode từ nội dung CK ──
            // Nội dung CK thường là: "BK20260314001" hoặc "thanh toan BK20260314001"
            // Regex tìm chuỗi bắt đầu bằng BK + 14 ký tự số
            string? bookingCode = ExtractBookingCode(payload.TransferContent);

            if (string.IsNullOrEmpty(bookingCode))
            {
                _logger.LogInformation("SePay webhook: không tìm thấy mã đặt sân trong nội dung '{Content}'",
                    payload.TransferContent);
                return Ok(new { success = true, message = "No booking code found" });
            }

            // ── 4. Tìm booking ──
            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.BookingCode == bookingCode);

            if (booking == null)
            {
                _logger.LogWarning("SePay webhook: không tìm thấy booking '{Code}'", bookingCode);
                return Ok(new { success = true, message = $"Booking {bookingCode} not found" });
            }

            // ── 5. Bỏ qua nếu đã thanh toán ──
            if (booking.PaymentStatus == "Paid")
            {
                _logger.LogInformation("SePay webhook: booking '{Code}' đã Paid, bỏ qua", bookingCode);
                return Ok(new { success = true, message = "Already paid" });
            }

            // ── 6. Kiểm tra số tiền (cho phép lệch ±1% do làm tròn) ──
            decimal minAcceptable = booking.TotalPrice * 0.99m;
            if (payload.TransferAmount < minAcceptable)
            {
                _logger.LogWarning(
                    "SePay webhook: booking '{Code}' số tiền {Amount} < yêu cầu {Required}",
                    bookingCode, payload.TransferAmount, booking.TotalPrice);

                // Ghi nhận nhưng không tự duyệt — admin xem xét thủ công
                booking.Note = $"[SePay] Nhận {payload.TransferAmount:N0}đ / cần {booking.TotalPrice:N0}đ — Chờ xác nhận thủ công";
                booking.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Amount insufficient, flagged for manual review" });
            }

            // ── 7. Tự động duyệt ──
            string oldStatus = booking.Status;
            booking.PaymentStatus = "Paid";
            booking.Status = "Confirmed";
            booking.UpdatedAt = DateTime.Now;

            // Lưu reference ID từ SePay vào Note để đối chiếu
            booking.Note = string.IsNullOrEmpty(booking.Note)
                ? $"[SePay] TxID:{payload.Id} | {payload.TransferAmount:N0}đ | {payload.TransactionDate}"
                : booking.Note + $" | [SePay] TxID:{payload.Id}";

            await _context.SaveChangesAsync();

            // Ghi log
            _context.ActivityLogs.Add(new ActivityLog
            {
                UserId = booking.UserId,
                Action = "SePayWebhook",
                TableName = "Bookings",
                RecordId = booking.BookingId,
                OldValue = $"Pay:Unpaid|Status:{oldStatus}",
                NewValue = $"Pay:Paid|Status:Confirmed|SePay:{payload.Id}",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "webhook",
                UserAgent = "SePay-Webhook",
                CreatedAt = DateTime.Now
            });
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "✅ SePay webhook: booking '{Code}' tự động Confirmed. TxID={TxId} Amount={Amount}đ",
                bookingCode, payload.Id, payload.TransferAmount);

            return Ok(new { success = true, message = $"Booking {bookingCode} confirmed" });
        }

        // ─────────────────────────────────────────────────────────────────
        // CHECK STATUS — Khách polling kiểm tra booking đã được duyệt chưa
        // GET /api/sepay/check/{bookingCode}
        // ─────────────────────────────────────────────────────────────────
        [HttpGet("check/{bookingCode}")]
        public async Task<IActionResult> CheckStatus(string bookingCode)
        {
            var booking = await _context.Bookings
                .Where(b => b.BookingCode == bookingCode)
                .Select(b => new { b.BookingCode, b.PaymentStatus, b.Status, b.TotalPrice })
                .FirstOrDefaultAsync();

            if (booking == null)
                return NotFound(new { success = false });

            return Ok(new
            {
                success = true,
                bookingCode = booking.BookingCode,
                paymentStatus = booking.PaymentStatus,
                status = booking.Status,
                isPaid = booking.PaymentStatus == "Paid"
            });
        }

        // ─────────────────────────────────────────────────────────────────
        // HELPER: tách BookingCode từ nội dung chuyển khoản
        // ─────────────────────────────────────────────────────────────────
        private static string? ExtractBookingCode(string? content)
        {
            if (string.IsNullOrWhiteSpace(content)) return null;

            // BookingCode format: BK + yyyyMMdd + 4 số  (vd: BK202603140001)
            var match = System.Text.RegularExpressions.Regex.Match(
                content.ToUpper(),
                @"BK\d{12}");   // BK + 12 ký tự số (8 ngày + 4 seq)

            return match.Success ? match.Value : null;
        }
    }

    // ── SePay Webhook Payload ──
    // Tham khảo: https://my.sepay.vn/userapi/document
    public class SePayPayload
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("gateway")]
        public string Gateway { get; set; } = "";

        [JsonPropertyName("transactionDate")]
        public string TransactionDate { get; set; } = "";

        [JsonPropertyName("accountNumber")]
        public string AccountNumber { get; set; } = "";

        /// <summary>Số tiền giao dịch (VND)</summary>
        [JsonPropertyName("transferAmount")]
        public decimal TransferAmount { get; set; }

        /// <summary>Nội dung chuyển khoản — chứa BookingCode</summary>
        [JsonPropertyName("transferContent")]
        public string TransferContent { get; set; } = "";

        /// <summary>"in" = tiền vào, "out" = tiền ra</summary>
        [JsonPropertyName("transferType")]
        public string TransferType { get; set; } = "in";

        [JsonPropertyName("referenceCode")]
        public string? ReferenceCode { get; set; }

        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }
}
