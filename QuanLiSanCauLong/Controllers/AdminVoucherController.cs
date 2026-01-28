/*using Microsoft.AspNetCore.Authorization;
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

        [HttpGet]
        public async Task<IActionResult> Index(string status, string applicableFor)
        {
            var query = _context.Vouchers.AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                if (status == "active")
                    query = query.Where(v => v.IsActive && v.StartDate <= DateTime.Now && v.EndDate >= DateTime.Now);
                else if (status == "expired")
                    query = query.Where(v => v.EndDate < DateTime.Now);
                else if (status == "upcoming")
                    query = query.Where(v => v.StartDate > DateTime.Now);
                else if (status == "inactive")
                    query = query.Where(v => !v.IsActive);
            }

            if (!string.IsNullOrEmpty(applicableFor))
                query = query.Where(v => v.ApplicableFor == applicableFor);

            var vouchers = await query.OrderByDescending(v => v.CreatedAt).ToListAsync();

            var model = vouchers.Select(v => new VoucherViewModel
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
            }).ToList();

            ViewBag.TotalVouchers = model.Count;
            ViewBag.ActiveVouchers = model.Count(v => v.IsActive && v.StartDate <= DateTime.Now && v.EndDate >= DateTime.Now);
            ViewBag.ExpiredVouchers = model.Count(v => v.EndDate < DateTime.Now);
            ViewBag.TotalUsage = model.Sum(v => v.UsedCount);

            return View(model);
        }

        [HttpGet]
        public IActionResult Create()
        {
            var model = new VoucherViewModel
            {
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddMonths(1),
                IsActive = true
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VoucherViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra mã voucher trùng
                if (await _context.Vouchers.AnyAsync(v => v.VoucherCode == model.VoucherCode))
                {
                    ModelState.AddModelError("VoucherCode", "Mã voucher đã tồn tại!");
                    return View(model);
                }

                // Validate dates
                if (model.StartDate >= model.EndDate)
                {
                    ModelState.AddModelError("EndDate", "Ngày kết thúc phải sau ngày bắt đầu!");
                    return View(model);
                }

                // Validate discount value
                if (model.DiscountType == "Percentage" && model.DiscountValue > 100)
                {
                    ModelState.AddModelError("DiscountValue", "Giảm giá theo phần trăm không được vượt quá 100%!");
                    return View(model);
                }

                var voucher = new Voucher
                {
                    VoucherCode = model.VoucherCode.ToUpper(),
                    VoucherName = model.VoucherName,
                    Description = model.Description,
                    DiscountType = model.DiscountType,
                    DiscountValue = model.DiscountValue,
                    MinOrderAmount = model.MinOrderAmount,
                    MaxDiscount = model.MaxDiscount,
                    ApplicableFor = model.ApplicableFor,
                    StartDate = model.StartDate,
                    EndDate = model.EndDate,
                    UsageLimit = model.UsageLimit,
                    UsageLimitPerUser = model.UsageLimitPerUser,
                    UsedCount = 0,
                    IsActive = model.IsActive,
                    CreatedAt = DateTime.Now,
                    CreatedBy = GetCurrentUserId()
                };

                _context.Vouchers.Add(voucher);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Tạo voucher thành công!";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var voucher = await _context.Vouchers.FindAsync(id);
            if (voucher == null)
                return NotFound();

            var model = new VoucherViewModel
            {
                VoucherId = voucher.VoucherId,
                VoucherCode = voucher.VoucherCode,
                VoucherName = voucher.VoucherName,
                Description = voucher.Description,
                DiscountType = voucher.DiscountType,
                DiscountValue = voucher.DiscountValue,
                MinOrderAmount = voucher.MinOrderAmount,
                MaxDiscount = voucher.MaxDiscount,
                ApplicableFor = voucher.ApplicableFor,
                StartDate = voucher.StartDate,
                EndDate = voucher.EndDate,
                UsageLimit = voucher.UsageLimit,
                UsageLimitPerUser = voucher.UsageLimitPerUser,
                UsedCount = voucher.UsedCount,
                IsActive = voucher.IsActive
            };

            // TODO: Lấy thống kê sử dụng voucher
            ViewBag.RemainingUsage = voucher.UsageLimit - voucher.UsedCount;
            ViewBag.UsagePercentage = (voucher.UsageLimit > 0)
                ? ((voucher.UsedCount ?? 0) * 100.0 / voucher.UsageLimit).ToString("F1")
                : "0";

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var voucher = await _context.Vouchers.FindAsync(id);
            if (voucher == null)
                return NotFound();

            var model = new VoucherViewModel
            {
                VoucherId = voucher.VoucherId,
                VoucherCode = voucher.VoucherCode,
                VoucherName = voucher.VoucherName,
                Description = voucher.Description,
                DiscountType = voucher.DiscountType,
                DiscountValue = voucher.DiscountValue,
                MinOrderAmount = voucher.MinOrderAmount,
                MaxDiscount = voucher.MaxDiscount,
                ApplicableFor = voucher.ApplicableFor,
                StartDate = voucher.StartDate,
                EndDate = voucher.EndDate,
                UsageLimit = voucher.UsageLimit,
                UsageLimitPerUser = voucher.UsageLimitPerUser,
                IsActive = voucher.IsActive
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(VoucherViewModel model)
        {
            if (ModelState.IsValid)
            {
                var voucher = await _context.Vouchers.FindAsync(model.VoucherId);
                if (voucher != null)
                {
                    // Validate dates
                    if (model.StartDate >= model.EndDate)
                    {
                        ModelState.AddModelError("EndDate", "Ngày kết thúc phải sau ngày bắt đầu!");
                        return View(model);
                    }

                    // Validate discount value
                    if (model.DiscountType == "Percentage" && model.DiscountValue > 100)
                    {
                        ModelState.AddModelError("DiscountValue", "Giảm giá theo phần trăm không được vượt quá 100%!");
                        return View(model);
                    }

                    voucher.VoucherName = model.VoucherName;
                    voucher.Description = model.Description;
                    voucher.DiscountType = model.DiscountType;
                    voucher.DiscountValue = model.DiscountValue;
                    voucher.MinOrderAmount = model.MinOrderAmount;
                    voucher.MaxDiscount = model.MaxDiscount;
                    voucher.ApplicableFor = model.ApplicableFor;
                    voucher.StartDate = model.StartDate;
                    voucher.EndDate = model.EndDate;
                    voucher.UsageLimit = model.UsageLimit;
                    voucher.UsageLimitPerUser = model.UsageLimitPerUser;
                    voucher.IsActive = model.IsActive;
                    voucher.UpdatedAt = DateTime.Now;

                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Cập nhật voucher thành công!";
                    return RedirectToAction(nameof(Index));
                }
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
                return Json(new { success = false, message = "ID không hợp lệ!" });

            var voucher = await _context.Vouchers.FindAsync(id);
            if (voucher == null)
                return Json(new { success = false, message = "Không tìm thấy voucher!" });

            // Kiểm tra voucher đã được sử dụng chưa
            if (voucher.UsedCount > 0)
                return Json(new { success = false, message = "Không thể xóa voucher đã được sử dụng!" });

            _context.Vouchers.Remove(voucher);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Xóa voucher thành công!" });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int voucherId, bool isActive)
        {
            if (voucherId <= 0)
                return Json(new { success = false, message = "ID không hợp lệ!" });

            var voucher = await _context.Vouchers.FindAsync(voucherId);
            if (voucher == null)
                return Json(new { success = false, message = "Không tìm thấy voucher!" });

            voucher.IsActive = isActive;
            voucher.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = isActive ? "Kích hoạt voucher thành công!" : "Vô hiệu hóa voucher thành công!"
            });
        }

        [HttpGet]
        public async Task<IActionResult> Usage(int id)
        {
            var voucher = await _context.Vouchers.FindAsync(id);
            if (voucher == null)
                return NotFound();

            // TODO: Lấy lịch sử sử dụng voucher từ bảng VoucherUsage hoặc Orders
            // var usageHistory = await _context.VoucherUsages
            //     .Where(vu => vu.VoucherId == id)
            //     .Include(vu => vu.User)
            //     .Include(vu => vu.Order)
            //     .OrderByDescending(vu => vu.UsedAt)
            //     .ToListAsync();

            ViewBag.Voucher = voucher;
            return View();
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
            return 0;
        }
    }
}*/