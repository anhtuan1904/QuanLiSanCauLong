using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiSanCauLong.Data;
using QuanLiSanCauLong.Models;
using QuanLiSanCauLong.ViewModels;

namespace QuanLiSanCauLong.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminInventoryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminInventoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int? facilityId, int? categoryId, string stockStatus)
        {
            var query = _context.Inventories
                .Include(i => i.Product)
                    .ThenInclude(p => p.Category)
                .Include(i => i.Facility)
                .AsQueryable();

            if (facilityId.HasValue)
                query = query.Where(i => i.FacilityId == facilityId.Value);

            if (categoryId.HasValue)
                query = query.Where(i => i.Product.CategoryId == categoryId.Value);

            if (!string.IsNullOrEmpty(stockStatus))
            {
                if (stockStatus == "low")
                    query = query.Where(i => i.Quantity <= i.MinQuantity);
                else if (stockStatus == "normal")
                    query = query.Where(i => i.Quantity > i.MinQuantity);
                else if (stockStatus == "out")
                    query = query.Where(i => i.Quantity == 0);
            }

            var items = await query.OrderBy(i => i.Facility.FacilityName).ToListAsync();

            var model = new InventoryListViewModel
            {
                FacilityId = facilityId ?? 0,
                FacilityName = facilityId.HasValue
                    ? (await _context.Facilities.FindAsync(facilityId.Value))?.FacilityName
                    : "Tất cả cơ sở",
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
            ViewBag.Categories = await _context.ProductCategories.ToListAsync();
            ViewBag.LowStockCount = items.Count(i => i.Quantity <= i.MinQuantity);
            ViewBag.OutOfStockCount = items.Count(i => i.Quantity == 0);

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> StockIn(int? facilityId)
        {
            ViewBag.Facilities = await _context.Facilities.Where(f => f.IsActive).ToListAsync();
            ViewBag.Products = await _context.Products.Where(p => p.IsActive).ToListAsync();

            var model = new StockTransactionViewModel
            {
                FacilityId = facilityId ?? 0,
                TransactionType = "Import",
                TransactionDate = DateTime.Now,
                Items = new List<StockTransactionItemViewModel> { new StockTransactionItemViewModel() }
            };

            // Trả về PartialView để không kèm Layout Nav/Footer
            return PartialView(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StockIn(StockTransactionViewModel model)
        {
            if (ModelState.IsValid && model.Items != null && model.Items.Any())
            {
                foreach (var item in model.Items)
                {
                    if (item.Quantity <= 0) continue;

                    var inventory = await _context.Inventories
                        .FirstOrDefaultAsync(i => i.FacilityId == model.FacilityId && i.ProductId == item.ProductId);

                    if (inventory == null)
                    {
                        inventory = new Inventory
                        {
                            FacilityId = model.FacilityId,
                            ProductId = item.ProductId,
                            Quantity = item.Quantity,
                            MinQuantity = 10,
                            MaxQuantity = 1000,
                            LastUpdated = DateTime.Now
                        };
                        _context.Inventories.Add(inventory);
                    }
                    else
                    {
                        inventory.Quantity += item.Quantity;
                        inventory.LastUpdated = DateTime.Now;
                    }
                }

                await _context.SaveChangesAsync();
                // Trả về JSON để AJAX ở Index.cshtml xử lý đóng Modal
                return Json(new { success = true, message = "Nhập kho thành công!" });
            }

            ViewBag.Facilities = await _context.Facilities.Where(f => f.IsActive).ToListAsync();
            ViewBag.Products = await _context.Products.Where(p => p.IsActive).ToListAsync();
            return PartialView(model);
        }

        [HttpGet]
        public async Task<IActionResult> StockOut(int? facilityId)
        {
            ViewBag.Facilities = await _context.Facilities.Where(f => f.IsActive).ToListAsync();
            ViewBag.Products = await _context.Products.Where(p => p.IsActive).ToListAsync();

            var model = new StockTransactionViewModel
            {
                FacilityId = facilityId ?? 0,
                TransactionType = "Export",
                TransactionDate = DateTime.Now,
                Items = new List<StockTransactionItemViewModel> { new StockTransactionItemViewModel() }
            };

            return PartialView(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StockOut(StockTransactionViewModel model)
        {
            if (ModelState.IsValid && model.Items != null && model.Items.Any())
            {
                foreach (var item in model.Items)
                {
                    if (item.Quantity <= 0) continue;

                    var inventory = await _context.Inventories
                        .FirstOrDefaultAsync(i => i.FacilityId == model.FacilityId && i.ProductId == item.ProductId);

                    if (inventory == null)
                    {
                        return Json(new { success = false, message = $"Sản phẩm ID {item.ProductId} không có trong kho!" });
                    }

                    if (inventory.Quantity < item.Quantity)
                    {
                        return Json(new { success = false, message = $"Sản phẩm {item.ProductId} không đủ số lượng tồn!" });
                    }

                    inventory.Quantity -= item.Quantity;
                    inventory.LastUpdated = DateTime.Now;
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Xuất kho thành công!" });
            }

            ViewBag.Facilities = await _context.Facilities.Where(f => f.IsActive).ToListAsync();
            ViewBag.Products = await _context.Products.Where(p => p.IsActive).ToListAsync();
            return PartialView(model);
        }

        [HttpGet]
        public async Task<IActionResult> Adjustment(int inventoryId)
        {
            var inventory = await _context.Inventories
                .Include(i => i.Product)
                .Include(i => i.Facility)
                .FirstOrDefaultAsync(i => i.InventoryId == inventoryId);

            if (inventory == null) return NotFound();

            return PartialView(inventory);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Adjustment(int inventoryId, int newQuantity, string note)
        {
            if (newQuantity < 0)
            {
                return Json(new { success = false, message = "Số lượng không hợp lệ!" });
            }

            var inventory = await _context.Inventories.FindAsync(inventoryId);
            if (inventory != null)
            {
                inventory.Quantity = newQuantity;
                inventory.LastUpdated = DateTime.Now;
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Điều chỉnh kho thành công!" });
            }

            return Json(new { success = false, message = "Không tìm thấy bản ghi kho!" });
        }

        [HttpGet]
        public async Task<IActionResult> LowStock()
        {
            var lowStockItems = await _context.Inventories
                .Include(i => i.Product)
                    .ThenInclude(p => p.Category)
                .Include(i => i.Facility)
                .Where(i => i.Quantity <= i.MinQuantity)
                .OrderBy(i => i.Quantity)
                .ToListAsync();

            // Nếu trang này mở bằng trang riêng thì dùng View, nếu mở Modal thì dùng Partial
            return Request.Headers["X-Requested-With"] == "XMLHttpRequest"
                ? PartialView(lowStockItems)
                : View(lowStockItems);
        }

        [HttpGet]
        public async Task<IActionResult> GetProductStock(int facilityId, int productId)
        {
            var inventory = await _context.Inventories
                .Include(i => i.Product)
                .FirstOrDefaultAsync(i => i.FacilityId == facilityId && i.ProductId == productId);

            if (inventory == null)
                return Json(new { success = false, message = "Sản phẩm chưa có trong kho!" });

            return Json(new
            {
                success = true,
                quantity = inventory.Quantity,
                minQuantity = inventory.MinQuantity,
                maxQuantity = inventory.MaxQuantity,
                unit = inventory.Product.Unit
            });
        }
    }
}