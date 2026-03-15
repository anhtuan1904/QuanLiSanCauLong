using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiSanCauLong.Data;
using QuanLiSanCauLong.Models;
using QuanLiSanCauLong.ViewModels;
using System.Security.Claims;

namespace QuanLiSanCauLong.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminVoucherController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminVoucherController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ═══════════════════════════════════════════════════════════
        // INDEX
        // ═══════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Index(string? status, string? applicableFor, string? search)
        {
            var query = _context.Vouchers.AsQueryable();
            var now = DateTime.Now;

            if (!string.IsNullOrEmpty(status))
            {
                query = status switch
                {
                    "active" => query.Where(v => v.IsActive && v.StartDate <= now && v.EndDate >= now),
                    "expired" => query.Where(v => v.EndDate < now),
                    "upcoming" => query.Where(v => v.StartDate > now),
                    "inactive" => query.Where(v => !v.IsActive),
                    _ => query
                };
            }

            if (!string.IsNullOrEmpty(applicableFor))
                query = query.Where(v => v.ApplicableFor == applicableFor);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(v => v.VoucherCode.Contains(search) || v.VoucherName.Contains(search));

            var vouchers = await query.OrderByDescending(v => v.CreatedAt).ToListAsync();

            var model = vouchers.Select(MapToViewModel).ToList();

            ViewBag.TotalVouchers = model.Count;
            ViewBag.ActiveVouchers = model.Count(v => v.IsActive && v.StartDate <= now && v.EndDate >= now);
            ViewBag.ExpiredVouchers = model.Count(v => v.EndDate < now);
            ViewBag.TotalUsage = model.Sum(v => v.UsedCount);
            ViewBag.Status = status;
            ViewBag.ApplicableFor = applicableFor;
            ViewBag.Search = search;

            return View(model);
        }

        // ═══════════════════════════════════════════════════════════
        // CREATE GET
        // ═══════════════════════════════════════════════════════════
        [HttpGet]
        public IActionResult Create()
        {
            var model = new VoucherViewModel
            {
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddMonths(1),
                IsActive = true,
                DiscountType = "Percentage",
                ApplicableFor = "All"
            };
            return View(model);
        }

        // ═══════════════════════════════════════════════════════════
        // CREATE POST
        // ═══════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VoucherViewModel model)
        {
            // Validate
            if (await _context.Vouchers.AnyAsync(v => v.VoucherCode.ToUpper() == model.VoucherCode.ToUpper()))
                ModelState.AddModelError("VoucherCode", "Mã voucher đã tồn tại!");

            if (model.StartDate >= model.EndDate)
                ModelState.AddModelError("EndDate", "Ngày kết thúc phải sau ngày bắt đầu!");

            if (model.DiscountType == "Percentage" && model.DiscountValue > 100)
                ModelState.AddModelError("DiscountValue", "Giảm giá % không được vượt quá 100%!");

            if (!ModelState.IsValid)
                return View(model);

            var voucher = new Voucher
            {
                VoucherCode = model.VoucherCode.ToUpper().Trim(),
                VoucherName = model.VoucherName,
                Description = model.Description,
                DiscountType = model.DiscountType,
                DiscountValue = model.DiscountValue,
                MinOrderAmount = model.MinOrderAmount,
                MaxDiscount = model.MaxDiscount,
                ApplicableFor = model.ApplicableFor ?? "All",
                StartDate = model.StartDate ?? DateTime.Now,
                EndDate = model.EndDate ?? DateTime.Now.AddMonths(1),
                UsageLimit = model.UsageLimit,
                UsageLimitPerUser = model.UsageLimitPerUser,
                UsedCount = 0,
                IsActive = model.IsActive,
                CreatedAt = DateTime.Now,
                CreatedBy = GetCurrentUserId()
            };

            _context.Vouchers.Add(voucher);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã tạo voucher <strong>{voucher.VoucherCode}</strong> thành công!";
            return RedirectToAction(nameof(Index));
        }

        // ═══════════════════════════════════════════════════════════
        // EDIT GET
        // ═══════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var voucher = await _context.Vouchers.FindAsync(id);
            if (voucher == null) return NotFound();
            return View(MapToViewModel(voucher));
        }

        // ═══════════════════════════════════════════════════════════
        // EDIT POST
        // ═══════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(VoucherViewModel model)
        {
            if (model.StartDate >= model.EndDate)
                ModelState.AddModelError("EndDate", "Ngày kết thúc phải sau ngày bắt đầu!");

            if (!ModelState.IsValid)
                return View(model);

            var voucher = await _context.Vouchers.FindAsync(model.VoucherId);
            if (voucher == null) return NotFound();

            voucher.VoucherName = model.VoucherName;
            voucher.Description = model.Description;
            voucher.DiscountType = model.DiscountType;
            voucher.DiscountValue = model.DiscountValue;
            voucher.MinOrderAmount = model.MinOrderAmount;
            voucher.MaxDiscount = model.MaxDiscount;
            voucher.ApplicableFor = model.ApplicableFor ?? "All";
            voucher.StartDate = model.StartDate ?? voucher.StartDate;
            voucher.EndDate = model.EndDate ?? voucher.EndDate;
            voucher.UsageLimit = model.UsageLimit;
            voucher.UsageLimitPerUser = model.UsageLimitPerUser;
            voucher.IsActive = model.IsActive;
            voucher.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Cập nhật voucher thành công!";
            return RedirectToAction(nameof(Index));
        }

        // ═══════════════════════════════════════════════════════════
        // DETAILS
        // ═══════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var voucher = await _context.Vouchers.FindAsync(id);
            if (voucher == null) return NotFound();

            var model = MapToViewModel(voucher);

            ViewBag.RemainingUsage = voucher.UsageLimit.HasValue
                ? (object)(voucher.UsageLimit.Value - voucher.UsedCount)
                : "Không giới hạn";

            ViewBag.UsagePercentage = voucher.UsageLimit.HasValue && voucher.UsageLimit.Value > 0
                ? ((double)voucher.UsedCount / voucher.UsageLimit.Value * 100).ToString("F1")
                : "0";

            return View(model);
        }

        // ═══════════════════════════════════════════════════════════
        // USAGE — lịch sử sử dụng thật từ VoucherUsages
        // ═══════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Usage(int id)
        {
            var voucher = await _context.Vouchers.FindAsync(id);
            if (voucher == null) return NotFound();

            // Query VoucherUsages thật, join User + Booking + Order
            var usages = await _context.VoucherUsages
                .Include(vu => vu.User)
                .Include(vu => vu.Booking)
                .Include(vu => vu.Order)
                .Where(vu => vu.VoucherId == id)
                .OrderByDescending(vu => vu.Booking != null
                    ? vu.Booking.CreatedAt
                    : (vu.Order != null ? vu.Order.CreatedAt : DateTime.MinValue))
                .ToListAsync();

            var model = usages.Select(vu =>
            {
                // Xác định ngày sử dụng từ Booking hoặc Order
                var usedAt = vu.Booking?.CreatedAt ?? vu.Order?.CreatedAt ?? DateTime.MinValue;
                // Mã đơn: BookingCode hoặc OrderCode
                var orderCode = vu.Booking?.BookingCode ?? vu.Order?.OrderCode ?? "—";
                var orderType = vu.Booking != null ? "Đặt sân" : "Đơn hàng";
                var orderTotal = vu.Booking?.TotalPrice ?? vu.Order?.TotalAmount ?? 0m;

                return new VoucherUsageViewModel
                {
                    CustomerName = vu.User?.FullName ?? vu.User?.Email ?? "Khách",
                    CustomerPhone = vu.User?.Phone ?? "—",
                    OrderCode = orderCode,
                    OrderType = orderType,
                    UsedAt = usedAt,
                    OrderTotal = orderTotal,
                    DiscountAmount = vu.DiscountAmount
                };
            }).ToList();

            var totalDiscounted = model.Sum(u => u.DiscountAmount);

            ViewBag.Voucher = MapToViewModel(voucher);
            ViewBag.TotalDiscounted = totalDiscounted;

            return View(model);
        }

        // ═══════════════════════════════════════════════════════════
        // DELETE
        // ═══════════════════════════════════════════════════════════
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var voucher = await _context.Vouchers.FindAsync(id);
            if (voucher == null)
                return Json(new { success = false, message = "Không tìm thấy voucher!" });

            if (voucher.UsedCount > 0)
                return Json(new { success = false, message = "Voucher đã được sử dụng, không thể xóa!" });

            _context.Vouchers.Remove(voucher);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã xóa voucher thành công!" });
        }

        // ═══════════════════════════════════════════════════════════
        // TOGGLE STATUS
        // ═══════════════════════════════════════════════════════════
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int voucherId, bool isActive)
        {
            var voucher = await _context.Vouchers.FindAsync(voucherId);
            if (voucher == null)
                return Json(new { success = false, message = "Không tìm thấy!" });

            voucher.IsActive = isActive;
            voucher.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = isActive ? "Đã kích hoạt!" : "Đã tắt!" });
        }

        // ═══════════════════════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════════════════════
        private static VoucherViewModel MapToViewModel(Voucher v) => new()
        {
            VoucherId = v.VoucherId,
            VoucherCode = v.VoucherCode,
            VoucherName = v.VoucherName,
            Description = v.Description,
            DiscountType = v.DiscountType,
            DiscountValue = v.DiscountValue,
            MinOrderAmount = v.MinOrderAmount,
            MaxDiscount = v.MaxDiscount,
            ApplicableFor = v.ApplicableFor,
            StartDate = v.StartDate,
            EndDate = v.EndDate,
            UsageLimit = v.UsageLimit,
            UsageLimitPerUser = v.UsageLimitPerUser,
            UsedCount = v.UsedCount,
            IsActive = v.IsActive
        };

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out int uid) ? uid : 0;
        }
    }
}
