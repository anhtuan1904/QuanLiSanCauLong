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

        [HttpGet]
        public async Task<IActionResult> Index(string status, string applicableFor)
        {
            var query = _context.Vouchers.AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                var now = DateTime.Now;
                if (status == "active")
                    query = query.Where(v => v.IsActive && v.StartDate <= now && v.EndDate >= now);
                else if (status == "expired")
                    query = query.Where(v => v.EndDate < now);
                else if (status == "upcoming")
                    query = query.Where(v => v.StartDate > now);
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

            var currentTime = DateTime.Now;
            ViewBag.TotalVouchers = model.Count;
            ViewBag.ActiveVouchers = model.Count(v => v.IsActive && v.StartDate <= currentTime && v.EndDate >= currentTime);
            ViewBag.ExpiredVouchers = model.Count(v => v.EndDate < currentTime);

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
                if (await _context.Vouchers.AnyAsync(v => v.VoucherCode.ToUpper() == model.VoucherCode.ToUpper()))
                {
                    ModelState.AddModelError("VoucherCode", "Mã voucher đã tồn tại!");
                }
                else if (model.StartDate >= model.EndDate)
                {
                    ModelState.AddModelError("EndDate", "Ngày kết thúc phải sau ngày bắt đầu!");
                }
                else if (model.DiscountType == "Percentage" && model.DiscountValue > 100)
                {
                    ModelState.AddModelError("DiscountValue", "Giảm giá % không được vượt quá 100%!");
                }
                else
                {
                    var voucher = new Voucher
                    {
                        VoucherCode = model.VoucherCode.ToUpper().Trim(),
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
            }

            // Cập nhật quan trọng: Nếu có lỗi khi tạo từ Modal ở Index, 
            // ta cần load lại danh sách để trả về View Index thay vì trả về View Create trống.
            var vouchers = await _context.Vouchers.OrderByDescending(v => v.CreatedAt).ToListAsync();
            var listModel = vouchers.Select(v => MapToViewModel(v)).ToList();
            TempData["ErrorMessage"] = "Có lỗi xảy ra, vui lòng kiểm tra lại thông tin!";
            return View("Index", listModel);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var voucher = await _context.Vouchers.FindAsync(id);
            if (voucher == null) return NotFound();

            var model = MapToViewModel(voucher);

            // FIX: Kiểm tra UsageLimit có null không trước khi trừ
            if (voucher.UsageLimit.HasValue)
            {
                ViewBag.RemainingUsage = voucher.UsageLimit.Value - voucher.UsedCount;
            }
            else
            {
                ViewBag.RemainingUsage = "Không giới hạn";
            }

            double percentage = 0;
            // FIX: Kiểm tra UsageLimit.HasValue để tránh lỗi chia cho null
            if (voucher.UsageLimit.HasValue && voucher.UsageLimit.Value > 0)
            {
                percentage = (double)voucher.UsedCount * 100.0 / (double)voucher.UsageLimit.Value;
            }

            ViewBag.UsagePercentage = percentage.ToString("F1");

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var voucher = await _context.Vouchers.FindAsync(id);
            if (voucher == null) return NotFound();
            return View(MapToViewModel(voucher));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(VoucherViewModel model)
        {
            if (ModelState.IsValid)
            {
                var voucher = await _context.Vouchers.FindAsync(model.VoucherId);
                if (voucher == null) return NotFound();

                if (model.StartDate >= model.EndDate)
                {
                    ModelState.AddModelError("EndDate", "Ngày kết thúc phải sau ngày bắt đầu!");
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
                TempData["SuccessMessage"] = "Cập nhật thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var voucher = await _context.Vouchers.FindAsync(id);
            if (voucher == null) return Json(new { success = false, message = "Không tìm thấy!" });

            if (voucher.UsedCount > 0)
                return Json(new { success = false, message = "Voucher đã dùng không thể xóa!" });

            _context.Vouchers.Remove(voucher);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Xóa thành công!" });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int voucherId, bool isActive)
        {
            var voucher = await _context.Vouchers.FindAsync(voucherId);
            if (voucher == null) return Json(new { success = false });

            voucher.IsActive = isActive;
            voucher.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> Usage(int id)
        {
            var voucher = await _context.Vouchers.FindAsync(id);
            if (voucher == null) return NotFound();
            ViewBag.Voucher = voucher;
            return View();
        }

        private VoucherViewModel MapToViewModel(Voucher v)
        {
            return new VoucherViewModel
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
}