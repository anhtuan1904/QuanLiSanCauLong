using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiSanCauLong.Data;
using QuanLiSanCauLong.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace QuanLiSanCauLong.Controllers
{
    /// <summary>
    /// Luồng thanh toán dịch vụ (Khóa học / Căng vợt / Giải đấu) qua SePay.
    ///
    ///   POST /ServicePayment/Checkout       — Tạo đơn + redirect sang trang QR
    ///   GET  /ServicePayment/Pay/{orderId}  — Hiển thị QR SePay + polling
    ///   GET  /ServicePayment/Status/{id}    — AJAX poll trạng thái thanh toán
    ///   POST /ServicePayment/SePayWebhook   — SePay gọi về sau khi chuyển khoản xong
    ///   GET  /ServicePayment/Success/{id}   — Trang hóa đơn thành công
    ///   GET  /ServicePayment/Cancel/{id}    — Hủy đơn chờ thanh toán
    /// </summary>
    public class ServicePaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;

        // Bank info — chỉnh trong appsettings nếu cần
        private const string BANK_ID = "MB";         // Mã ngân hàng SePay
        private const string ACCOUNT_NO = "0123456789"; // Số tài khoản nhận tiền
        private const string ACCOUNT_NAME = "QUAN LY SAN CAU LONG"; // Tên tài khoản

        // Hệ thống ghost user cho giao dịch khách vãng lai
        private const int GUEST_USER_ID = 1; // UserId fallback
        private const int DEFAULT_FACILITY = 1; // FacilityId mặc định

        public ServicePaymentController(ApplicationDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // ═══════════════════════════════════════════════════════════════
        // BƯỚC 1: Tạo đơn hàng và redirect sang trang QR
        // POST /ServicePayment/Checkout
        // ═══════════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(
            string serviceType,   // Course | Stringing | Tournament
            int serviceId,
            string customerName,
            string phone,
            string? email,
            string? note)
        {
            if (string.IsNullOrWhiteSpace(customerName) || string.IsNullOrWhiteSpace(phone))
                return BadRequest("Vui lòng điền đầy đủ họ tên và số điện thoại.");

            // ── 1. Lấy thông tin dịch vụ và tính giá ──────────────────
            decimal price = 0;
            string serviceName = "";
            string orderType = $"Service_{serviceType}";

            switch (serviceType)
            {
                case "Course":
                    var course = await _context.Courses.FindAsync(serviceId);
                    if (course == null) return NotFound("Không tìm thấy khóa học.");
                    if (course.Status == "Full" || course.IsFull)
                        return BadRequest("Khóa học đã đầy.");
                    if (course.Status == "Inactive")
                        return BadRequest("Khóa học chưa mở đăng ký.");
                    price = course.DiscountFee ?? course.TuitionFee ?? 0;
                    serviceName = course.CourseName;
                    break;

                case "Stringing":
                    var svc = await _context.StringingServices.FindAsync(serviceId);
                    if (svc == null) return NotFound("Không tìm thấy dịch vụ căng vợt.");
                    price = svc.DiscountPrice ?? svc.Price ?? 0;
                    serviceName = svc.ServiceName;
                    break;

                case "Tournament":
                    var t = await _context.Tournaments.FindAsync(serviceId);
                    if (t == null) return NotFound("Không tìm thấy giải đấu.");
                    if (t.IsFull) return BadRequest("Giải đấu đã đủ VĐV.");
                    price = t.EntryFee ?? 0;
                    serviceName = t.TournamentName;
                    break;

                default:
                    return BadRequest("Loại dịch vụ không hợp lệ.");
            }

            // ── 2. Tạo Order ───────────────────────────────────────────
            var orderCode = GenerateOrderCode(serviceType);

            var order = new Order
            {
                OrderCode = orderCode,
                UserId = GUEST_USER_ID,
                FacilityId = DEFAULT_FACILITY,
                OrderType = orderType,
                SubTotal = price,
                DiscountAmount = 0,
                TotalAmount = price,
                OrderStatus = "Pending",
                PaymentMethod = "BankTransfer",
                PaymentStatus = "Unpaid",
                Note = $"Đăng ký {serviceType}: {serviceName} — {customerName} — {phone}",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                OrderDetails = new List<OrderDetail>() // Dịch vụ không có sản phẩm vật lý
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // ── 3. Tạo ServiceEnrollment liên kết Order ────────────────
            var enrollment = new ServiceEnrollment
            {
                ServiceType = serviceType,
                CourseId = serviceType == "Course" ? serviceId : null,
                StringingId = serviceType == "Stringing" ? serviceId : null,
                TournamentId = serviceType == "Tournament" ? serviceId : null,
                OrderId = order.OrderId,
                CustomerName = customerName,
                Phone = phone,
                Email = email,
                Note = note,
                Status = "Pending",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.ServiceEnrollments.Add(enrollment);
            await _context.SaveChangesAsync();

            // ── 4. Redirect sang trang thanh toán QR ───────────────────
            return RedirectToAction("Pay", new { orderId = order.OrderId });
        }

        // ═══════════════════════════════════════════════════════════════
        // BƯỚC 2: Hiển thị QR thanh toán
        // GET /ServicePayment/Pay/{orderId}
        // ═══════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Pay(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.ServiceEnrollments)
                    .ThenInclude(e => e.Course)
                .Include(o => o.ServiceEnrollments)
                    .ThenInclude(e => e.StringingService)
                .Include(o => o.ServiceEnrollments)
                    .ThenInclude(e => e.Tournament)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null) return NotFound();

            if (order.PaymentStatus == "Paid")
                return RedirectToAction("Success", new { orderId });

            var enrollment = order.ServiceEnrollments?.FirstOrDefault();

            // SePay QR URL theo chuẩn VietQR
            // Format: https://qr.sepay.vn/img?bank=BANK&acc=ACC&template=compact&amount=AMT&des=DESC&download=false
            var desc = Uri.EscapeDataString(order.OrderCode);
            var qrUrl = $"https://qr.sepay.vn/img?bank={BANK_ID}&acc={ACCOUNT_NO}" +
                          $"&template=compact2&amount={order.TotalAmount:0}" +
                          $"&des={desc}&download=false";

            ViewBag.Order = order;
            ViewBag.Enrollment = enrollment;
            ViewBag.QrUrl = qrUrl;
            ViewBag.BankId = BANK_ID;
            ViewBag.AccountNo = ACCOUNT_NO;
            ViewBag.AccountName = ACCOUNT_NAME;
            ViewBag.Amount = order.TotalAmount;
            ViewBag.OrderCode = order.OrderCode;
            ViewBag.ExpiresAt = order.CreatedAt.AddMinutes(30);

            return View();
        }

        // ═══════════════════════════════════════════════════════════════
        // AJAX POLLING: Kiểm tra trạng thái thanh toán
        // GET /ServicePayment/Status/{orderId}
        // ═══════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Status(int orderId)
        {
            var order = await _context.Orders
                .Select(o => new { o.OrderId, o.OrderStatus, o.PaymentStatus, o.UpdatedAt })
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                return Json(new { paid = false, status = "NotFound" });

            return Json(new
            {
                paid = order.PaymentStatus == "Paid",
                status = order.PaymentStatus,
                orderStatus = order.OrderStatus,
                updatedAt = order.UpdatedAt
            });
        }

        // ═══════════════════════════════════════════════════════════════
        // BƯỚC 3: SePay Webhook — nhận callback sau khi chuyển khoản
        // POST /ServicePayment/SePayWebhook
        //
        // SePay gửi POST với body JSON, header X-Sepay-Signature (HMAC-SHA256).
        // Ta verify signature, sau đó tìm Order theo nội dung chuyển khoản
        // (transferContent chứa orderCode), rồi xác nhận thanh toán.
        // ═══════════════════════════════════════════════════════════════
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> SePayWebhook()
        {
            // ── Đọc raw body ───────────────────────────────────────────
            string rawBody;
            using (var reader = new System.IO.StreamReader(Request.Body, Encoding.UTF8))
                rawBody = await reader.ReadToEndAsync();

            // ── Verify HMAC-SHA256 signature ───────────────────────────
            var secret = _config["SePay:WebhookSecret"] ?? "";
            var signature = Request.Headers["X-Sepay-Signature"].FirstOrDefault() ?? "";

            if (!VerifySignature(rawBody, secret, signature))
            {
                // Log để debug nhưng không expose lý do ra ngoài
                return Unauthorized(new { success = false, message = "Invalid signature" });
            }

            // ── Parse JSON payload ─────────────────────────────────────
            SePayWebhookPayload? payload;
            try
            {
                payload = JsonSerializer.Deserialize<SePayWebhookPayload>(rawBody,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch
            {
                return BadRequest(new { success = false, message = "Invalid JSON" });
            }

            if (payload == null || payload.TransferAmount <= 0)
                return Ok(new { success = true, message = "No action" });

            // ── Tìm Order theo nội dung chuyển khoản ──────────────────
            // SePay điền orderCode vào trường "content" của giao dịch
            var content = payload.Content?.Trim().ToUpper() ?? "";

            var order = await _context.Orders
                .Include(o => o.ServiceEnrollments)
                .FirstOrDefaultAsync(o =>
                    content.Contains(o.OrderCode.ToUpper()) &&
                    o.PaymentStatus == "Unpaid" &&
                    o.OrderStatus == "Pending");

            if (order == null)
                // Order không tìm thấy hoặc đã thanh toán — OK để SePay không retry
                return Ok(new { success = true, message = "Order not found or already paid" });

            // ── Kiểm tra số tiền ──────────────────────────────────────
            if (payload.TransferAmount < order.TotalAmount)
                return Ok(new { success = true, message = "Amount mismatch — ignored" });

            // ── Cập nhật trạng thái ────────────────────────────────────
            order.PaymentStatus = "Paid";
            order.OrderStatus = "Confirmed";
            order.UpdatedAt = DateTime.Now;
            order.CompletedAt = DateTime.Now;

            // Cập nhật enrollment
            foreach (var enrollment in order.ServiceEnrollments ?? new List<ServiceEnrollment>())
            {
                enrollment.Status = "Confirmed";
                enrollment.UpdatedAt = DateTime.Now;
            }

            // Nếu là đăng ký khóa học → tăng CurrentStudents và auto-Full
            await ProcessEnrollmentConfirmation(order);

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Payment confirmed" });
        }

        // ═══════════════════════════════════════════════════════════════
        // BƯỚC 4: Trang hóa đơn thành công
        // GET /ServicePayment/Success/{orderId}
        // ═══════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Success(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.ServiceEnrollments)
                    .ThenInclude(e => e.Course)
                .Include(o => o.ServiceEnrollments)
                    .ThenInclude(e => e.StringingService)
                .Include(o => o.ServiceEnrollments)
                    .ThenInclude(e => e.Tournament)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null) return NotFound();

            return View(order);
        }

        // ═══════════════════════════════════════════════════════════════
        // HỦY ĐƠN chờ thanh toán
        // GET /ServicePayment/Cancel/{orderId}
        // ═══════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Cancel(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.ServiceEnrollments)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null) return NotFound();

            if (order.PaymentStatus != "Paid")
            {
                order.OrderStatus = "Cancelled";
                order.PaymentStatus = "Cancelled";
                order.UpdatedAt = DateTime.Now;

                foreach (var e in order.ServiceEnrollments ?? new List<ServiceEnrollment>())
                {
                    e.Status = "Cancelled";
                    e.UpdatedAt = DateTime.Now;
                }
                await _context.SaveChangesAsync();
            }

            TempData["CancelMsg"] = "Đã hủy đơn đăng ký.";
            return Redirect(Request.Headers["Referer"].ToString() is { Length: > 0 } r ? r : "/");
        }

        // ═══════════════════════════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════════════════════════

        private static string GenerateOrderCode(string serviceType)
        {
            var prefix = serviceType switch
            {
                "Course" => "KH",
                "Stringing" => "CV",
                "Tournament" => "GD",
                _ => "SV"
            };
            return $"{prefix}{DateTime.Now:yyMMddHHmm}{Random.Shared.Next(100, 999)}";
        }

        private static bool VerifySignature(string body, string secret, string signature)
        {
            if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(signature))
                return false;

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var computed = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(body))).ToLower();
            // Constant-time compare
            return CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(computed),
                Encoding.ASCII.GetBytes(signature.ToLower()));
        }

        /// <summary>
        /// Sau khi thanh toán xác nhận: tăng CurrentStudents cho Course,
        /// tăng CurrentPlayers cho Tournament, và tự chuyển Full nếu đầy.
        /// </summary>
        private async Task ProcessEnrollmentConfirmation(Order order)
        {
            var enrollments = order.ServiceEnrollments ?? new List<ServiceEnrollment>();

            foreach (var e in enrollments)
            {
                if (e.ServiceType == "Course" && e.CourseId.HasValue)
                {
                    var course = await _context.Courses.FindAsync(e.CourseId.Value);
                    if (course != null)
                    {
                        course.CurrentStudents++;
                        if (course.MaxStudents.HasValue && course.CurrentStudents >= course.MaxStudents.Value)
                            course.Status = "Full";
                        course.UpdatedAt = DateTime.Now;
                    }
                }
                else if (e.ServiceType == "Tournament" && e.TournamentId.HasValue)
                {
                    var tourn = await _context.Tournaments.FindAsync(e.TournamentId.Value);
                    if (tourn != null)
                    {
                        tourn.CurrentPlayers++;
                        tourn.UpdatedAt = DateTime.Now;
                    }
                }
            }
        }

        // ─── SePay Webhook Payload model ─────────────────────────────
        private sealed class SePayWebhookPayload
        {
            public string? Gateway { get; set; }
            public string? TransactionDate { get; set; }
            public string? AccountNumber { get; set; }
            public string? SubAccount { get; set; }
            public string? Code { get; set; }
            public string? Content { get; set; }   // nội dung CK chứa OrderCode
            public decimal TransferAmount { get; set; }
            public string? Description { get; set; }
            public string? TransferType { get; set; }   // "in" | "out"
            public string? ReferenceCode { get; set; }
            public bool Accumulated { get; set; }
            public int Id { get; set; }
        }
    }
}
