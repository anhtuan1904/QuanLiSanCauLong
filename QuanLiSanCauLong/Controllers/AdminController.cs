using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLiSanCauLong.Data;
using QuanLiSanCauLong.Helpers;
using QuanLiSanCauLong.Models;
using QuanLiSanCauLong.ViewModels;
using System.Security.Claims;

namespace QuanLiSanCauLong.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ===================================
        // DASHBOARD & ANALYTICS
        // ===================================

        [HttpGet]
        public async Task<IActionResult> Index(DateTime? fromDate, DateTime? toDate)
        {
            DateTime from = fromDate ?? DateTime.Today.AddDays(-30);
            DateTime to = toDate ?? DateTime.Today;

            var model = new AdminDashboardViewModel
            {
                FromDate = from,
                ToDate = to
            };

            // Lấy dữ liệu bookings và orders
            var bookings = await _context.Bookings
                .Include(b => b.Court.Facility)
                .Where(b => b.BookingDate >= from && b.BookingDate <= to)
                .ToListAsync();

            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                .Where(o => o.CreatedAt >= from && o.CreatedAt <= to.AddDays(1))
                .ToListAsync();

            // Thống kê tổng quan
            model.TotalBookings = bookings.Count;
            model.CompletedBookings = bookings.Count(b => b.Status == "Completed");
            model.CancelledBookings = bookings.Count(b => b.Status == "Cancelled");
            model.BookingRevenue = bookings.Where(b => b.Status != "Cancelled").Sum(b => b.TotalPrice);
            model.ProductRevenue = orders.Where(o => o.OrderStatus == "Completed").Sum(o => o.TotalAmount);
            model.TotalRevenue = model.BookingRevenue + model.ProductRevenue;

            model.TotalCustomers = await _context.Users.CountAsync(u => u.Role == "Customer");
            model.NewCustomers = await _context.Users.CountAsync(u => u.Role == "Customer" && u.CreatedAt >= from);
            model.TotalOrders = orders.Count;

            // Doanh thu theo ngày
            model.RevenueByDate = bookings
                .GroupBy(b => b.BookingDate)
                .Select(g => new RevenueByDateViewModel
                {
                    Date = g.Key,
                    BookingRevenue = g.Where(b => b.Status != "Cancelled").Sum(b => b.TotalPrice),
                    ProductRevenue = orders
                        .Where(o => o.CreatedAt.Date == g.Key && o.OrderStatus == "Completed")
                        .Sum(o => o.TotalAmount)
                })
                .OrderBy(r => r.Date)
                .ToList();

            foreach (var item in model.RevenueByDate)
            {
                item.TotalRevenue = item.BookingRevenue + item.ProductRevenue;
            }

            // Doanh thu theo cơ sở
            model.RevenueByFacility = bookings
                .Where(b => b.Status != "Cancelled")
                .GroupBy(b => b.Court.Facility.FacilityName)
                .Select(g => new RevenueByFacilityViewModel
                {
                    FacilityName = g.Key,
                    BookingCount = g.Count(),
                    BookingRevenue = g.Sum(b => b.TotalPrice),
                    ProductRevenue = orders
                        .Where(o => o.Facility.FacilityName == g.Key && o.OrderStatus == "Completed")
                        .Sum(o => o.TotalAmount)
                })
                .ToList();

            foreach (var item in model.RevenueByFacility)
            {
                item.TotalRevenue = item.BookingRevenue + item.ProductRevenue;
            }

            // Khung giờ phổ biến
            model.PopularTimeSlots = bookings
                .Where(b => b.Status != "Cancelled")
                .GroupBy(b => new { Hour = b.StartTime.Hours })
                .Select(g => new PopularTimeSlotViewModel
                {
                    TimeSlot = g.Key.Hour + ":00 - " + (g.Key.Hour + 1) + ":00",
                    BookingCount = g.Count(),
                    Revenue = g.Sum(b => b.TotalPrice)
                })
                .OrderByDescending(t => t.BookingCount)
                .Take(10)
                .ToList();

            // Sản phẩm bán chạy
            var productStats = await _context.OrderDetails
                .Include(od => od.Product)
                .Include(od => od.Order)
                .Where(od => od.Order.CreatedAt >= from && od.Order.CreatedAt <= to.AddDays(1)
                        && od.Order.OrderStatus == "Completed")
                .GroupBy(od => new { od.Product.ProductName, od.Product.Category.CategoryType })
                .Select(g => new TopProductViewModel
                {
                    ProductName = g.Key.ProductName,
                    CategoryType = g.Key.CategoryType,
                    QuantitySold = g.Sum(od => od.Quantity),
                    Revenue = g.Sum(od => od.TotalPrice)
                })
                .OrderByDescending(p => p.Revenue)
                .Take(10)
                .ToListAsync();

            model.TopProducts = productStats;

            // Khách hàng thân thiết
            model.TopCustomers = bookings
                .Where(b => b.Status != "Cancelled")
                .GroupBy(b => new { b.User.FullName, b.User.Email, b.User.Phone })
                .Select(g => new TopCustomerViewModel
                {
                    CustomerName = g.Key.FullName,
                    Email = g.Key.Email,
                    Phone = g.Key.Phone,
                    BookingCount = g.Count(),
                    TotalSpent = g.Sum(b => b.TotalPrice)
                })
                .OrderByDescending(c => c.TotalSpent)
                .Take(10)
                .ToList();

            return View("Dashboard/Index", model);
        }

        // ===================================
        // FACILITY MANAGEMENT
        // ===================================

        [HttpGet]
        public async Task<IActionResult> Facilities()
        {
            var facilities = await _context.Facilities.OrderBy(f => f.FacilityName).ToListAsync();
            return View(facilities);
        }

        [HttpGet]
        public IActionResult CreateFacility()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFacility(Facility model)
        {
            if (ModelState.IsValid)
            {
                model.CreatedAt = DateTime.Now;
                model.UpdatedAt = DateTime.Now;
                model.IsActive = true;

                _context.Facilities.Add(model);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Tạo cơ sở thành công!";
                return RedirectToAction(nameof(Facilities));
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> EditFacility(int id)
        {
            var facility = await _context.Facilities.FindAsync(id);
            if (facility == null)
                return NotFound();

            return View(facility);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditFacility(Facility model)
        {
            if (ModelState.IsValid)
            {
                var facility = await _context.Facilities.FindAsync(model.FacilityId);
                if (facility != null)
                {
                    facility.FacilityName = model.FacilityName;
                    facility.Address = model.Address;
                    facility.District = model.District;
                    facility.City = model.City;
                    facility.Phone = model.Phone;
                    facility.Email = model.Email;
                    facility.Description = model.Description;
                    facility.OpenTime = model.OpenTime;
                    facility.CloseTime = model.CloseTime;
                    facility.IsActive = model.IsActive;
                    facility.UpdatedAt = DateTime.Now;

                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Cập nhật cơ sở thành công!";
                    return RedirectToAction(nameof(Facilities));
                }
            }

            return View(model);
        }

        // ===================================
        // COURT MANAGEMENT
        // ===================================

        [HttpGet]
        public async Task<IActionResult> Courts(int? facilityId)
        {
            var query = _context.Courts.Include(c => c.Facility).AsQueryable();

            if (facilityId.HasValue)
                query = query.Where(c => c.FacilityId == facilityId.Value);

            var courts = await query
                .OrderBy(c => c.Facility.FacilityName)
                .ThenBy(c => c.CourtNumber)
                .ToListAsync();

            ViewBag.Facilities = await _context.Facilities.ToListAsync();
            return View(courts);
        }

        [HttpGet]
        public async Task<IActionResult> CreateCourt()
        {
            ViewBag.Facilities = await _context.Facilities.Where(f => f.IsActive).ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCourt(Court model)
        {
            if (ModelState.IsValid)
            {
                model.CreatedAt = DateTime.Now;
                model.Status = "Available";

                _context.Courts.Add(model);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Tạo sân thành công!";
                return RedirectToAction(nameof(Courts), new { facilityId = model.FacilityId });
            }

            ViewBag.Facilities = await _context.Facilities.Where(f => f.IsActive).ToListAsync();
            return View(model);
        }

        // ===================================
        // PRICE SLOT MANAGEMENT
        // ===================================

        [HttpGet]
        public async Task<IActionResult> PriceSlots(int? facilityId)
        {
            var query = _context.PriceSlots.Include(p => p.Facility).AsQueryable();

            if (facilityId.HasValue)
                query = query.Where(p => p.FacilityId == facilityId.Value);

            var priceSlots = await query
                .OrderBy(p => p.Facility.FacilityName)
                .ThenBy(p => p.StartTime)
                .ToListAsync();

            ViewBag.Facilities = await _context.Facilities.ToListAsync();
            return View(priceSlots);
        }

        [HttpGet]
        public async Task<IActionResult> CreatePriceSlot()
        {
            ViewBag.Facilities = await _context.Facilities.Where(f => f.IsActive).ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePriceSlot(PriceSlot model)
        {
            if (ModelState.IsValid)
            {
                model.IsActive = true;
                _context.PriceSlots.Add(model);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Tạo khung giờ thành công!";
                return RedirectToAction(nameof(PriceSlots), new { facilityId = model.FacilityId });
            }

            ViewBag.Facilities = await _context.Facilities.Where(f => f.IsActive).ToListAsync();
            return View(model);
        }

        // ===================================
        // USER MANAGEMENT
        // ===================================

        [HttpGet]
        public async Task<IActionResult> Users(string role)
        {
            var query = _context.Users.Include(u => u.Facility).AsQueryable();

            if (!string.IsNullOrEmpty(role))
                query = query.Where(u => u.Role == role);

            var users = await query.OrderBy(u => u.FullName).ToListAsync();

            ViewBag.Roles = new[] { "Customer", "Staff", "Admin" };
            return View(users);
        }

        [HttpGet]
        public async Task<IActionResult> CreateUser()
        {
            ViewBag.Facilities = await _context.Facilities.Where(f => f.IsActive).ToListAsync();
            ViewBag.Roles = new[] { "Customer", "Staff", "Admin" };
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(User model, string password)
        {
            if (ModelState.IsValid)
            {
                if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Email đã tồn tại!");
                }
                else
                {
                    model.PasswordHash = PasswordHelper.HashPassword(password);
                    model.CreatedAt = DateTime.Now;
                    model.UpdatedAt = DateTime.Now;
                    model.IsActive = true;

                    _context.Users.Add(model);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Tạo người dùng thành công!";
                    return RedirectToAction(nameof(Users));
                }
            }

            ViewBag.Facilities = await _context.Facilities.Where(f => f.IsActive).ToListAsync();
            ViewBag.Roles = new[] { "Customer", "Staff", "Admin" };
            return View(model);
        }

        // ===================================
        // PRODUCT MANAGEMENT
        // ===================================

        [HttpGet]
        public async Task<IActionResult> Products()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .OrderBy(p => p.ProductName)
                .ToListAsync();

            return View(products);
        }

        [HttpGet]
        public async Task<IActionResult> CreateProduct()
        {
            ViewBag.Categories = await _context.ProductCategories.ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProduct(Product model)
        {
            if (ModelState.IsValid)
            {
                model.CreatedAt = DateTime.Now;
                model.UpdatedAt = DateTime.Now;
                model.IsActive = true;

                _context.Products.Add(model);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Tạo sản phẩm thành công!";
                return RedirectToAction(nameof(Products));
            }

            ViewBag.Categories = await _context.ProductCategories.ToListAsync();
            return View(model);
        }

        // ===================================
        // INVENTORY MANAGEMENT
        // ===================================

        [HttpGet]
        public async Task<IActionResult> Inventory(int? facilityId)
        {
            var query = _context.Inventories
                .Include(i => i.Product)
                    .ThenInclude(p => p.Category)
                .Include(i => i.Facility)
                .AsQueryable();

            if (facilityId.HasValue)
                query = query.Where(i => i.FacilityId == facilityId.Value);

            var items = await query.ToListAsync();

            var model = new InventoryListViewModel
            {
                FacilityId = facilityId ?? 0,
                FacilityName = facilityId.HasValue ?
                    (await _context.Facilities.FindAsync(facilityId.Value))?.FacilityName : "Tất cả cơ sở",
                Items = items.Select(i => new InventoryItemViewModel
                {
                    InventoryId = i.InventoryId,
                    ProductId = i.ProductId,
                    ProductName = i.Product.ProductName,
                    ProductCode = i.Product.ProductCode,
                    CategoryName = i.Product.Category.CategoryName,
                    CategoryType = i.Product.Category.CategoryType,
                    Quantity = i.Quantity,
                    MinQuantity = i.MinQuantity,
                    MaxQuantity = i.MaxQuantity,
                    Unit = i.Product.Unit,
                    Price = i.Product.Price,
                    LastUpdated = i.LastUpdated
                }).ToList()
            };

            ViewBag.Facilities = await _context.Facilities.ToListAsync();
            return View(model);
        }

        // ===================================
        // VOUCHER MANAGEMENT
        // ===================================

        [HttpGet]
        public async Task<IActionResult> Vouchers()
        {
            var vouchers = await _context.Vouchers.OrderByDescending(v => v.CreatedAt).ToListAsync();
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

            return View(model);
        }

        [HttpGet]
        public IActionResult CreateVoucher()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateVoucher(VoucherViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (await _context.Vouchers.AnyAsync(v => v.VoucherCode == model.VoucherCode))
                {
                    ModelState.AddModelError("VoucherCode", "Mã voucher đã tồn tại!");
                    return View(model);
                }

                var voucher = new Voucher
                {
                    VoucherCode = model.VoucherCode,
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
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    CreatedBy = GetCurrentUserId()
                };

                _context.Vouchers.Add(voucher);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Tạo voucher thành công!";
                return RedirectToAction(nameof(Vouchers));
            }

            return View(model);
        }

        // ===================================
        // SYSTEM SETTINGS
        // ===================================

        [HttpGet]
        public async Task<IActionResult> Settings()
        {
            var settings = await _context.SystemSettings
                .OrderBy(s => s.Category)
                .ThenBy(s => s.SettingKey)
                .ToListAsync();

            return View(settings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSetting(int settingId, string value)
        {
            var setting = await _context.SystemSettings.FindAsync(settingId);
            if (setting != null)
            {
                setting.SettingValue = value;
                setting.UpdatedAt = DateTime.Now;
                setting.UpdatedBy = GetCurrentUserId();
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Cập nhật thành công!" });
            }

            return Json(new { success = false, message = "Không tìm thấy cài đặt!" });
        }

        // ===================================
        // HELPERS
        // ===================================

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
