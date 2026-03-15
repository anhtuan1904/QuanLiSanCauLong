// ===================================================================
// FILE: Controllers/AdminInventoryController.cs  (v3 — Modal AJAX)
// ===================================================================
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiSanCauLong.Data;
using QuanLiSanCauLong.Models;
using QuanLiSanCauLong.Services;
using QuanLiSanCauLong.ViewModels;

namespace QuanLiSanCauLong.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminInventoryController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly InventoryService _svc;

        public AdminInventoryController(ApplicationDbContext db, InventoryService svc)
        {
            _db = db;
            _svc = svc;
        }

        // ═══════════════════════════════════════════════════════
        //  INDEX
        // ═══════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Index(
            int? facilityId, int? categoryId,
            string? stockStatus, string? behaviorType)
        {
            var query = _db.Inventories
                .Include(i => i.Product).ThenInclude(p => p.Category)
                .Include(i => i.Facility)
                .AsQueryable();

            if (facilityId.HasValue) query = query.Where(i => i.FacilityId == facilityId.Value);
            if (categoryId.HasValue) query = query.Where(i => i.Product.CategoryId == categoryId.Value);
            if (!string.IsNullOrEmpty(behaviorType))
                query = query.Where(i => i.Product.Category.BehaviorType == behaviorType);

            var items = await query
                .OrderBy(i => i.Facility.FacilityName)
                .ThenBy(i => i.Product.ProductName)
                .ToListAsync();

            if (!string.IsNullOrEmpty(stockStatus))
                items = stockStatus switch
                {
                    "low" => items.Where(i => i.IsLowStock).ToList(),
                    "out" => items.Where(i => i.IsOutOfStock).ToList(),
                    "hold" => items.Where(i => i.HasHold).ToList(),
                    "rented" => items.Where(i => i.HasRented).ToList(),
                    "damaged" => items.Where(i => i.HasDamaged).ToList(),
                    _ => items
                };

            var model = new InventoryListViewModel
            {
                FacilityId = facilityId ?? 0,
                FacilityName = facilityId.HasValue
                    ? (await _db.Facilities.FindAsync(facilityId.Value))?.FacilityName
                    : "Tất cả cơ sở",
                Items = items.Select(i => new InventoryItemViewModel
                {
                    InventoryId = i.InventoryId,
                    ProductId = i.ProductId,
                    ProductName = i.Product.ProductName,
                    ProductCode = i.Product.ProductCode ?? i.Product.SKU,
                    CategoryName = i.Product.Category?.CategoryName,
                    CategoryType = i.Product.Category?.CategoryType,
                    BehaviorType = i.Product.Category?.BehaviorType ?? "Retail",
                    FacilityName = i.Facility?.FacilityName,
                    FacilityId = i.FacilityId,
                    Unit = i.Product.Unit,
                    Price = i.Product.Price,
                    LastUpdated = i.LastUpdated,
                    Quantity = i.Quantity,
                    HoldQuantity = i.HoldQuantity,
                    RentedQuantity = i.RentedQuantity,
                    DamagedQuantity = i.DamagedQuantity,
                    AvailableQuantity = i.AvailableQuantity,
                    MinQuantity = i.MinQuantity,
                    MaxQuantity = i.MaxQuantity,
                    IsLowStock = i.IsLowStock,
                }).ToList()
            };

            ViewBag.Facilities = await _db.Facilities.ToListAsync();
            ViewBag.Categories = await _db.ProductCategories.ToListAsync();
            ViewBag.LowStockCount = items.Count(i => i.IsLowStock);
            ViewBag.OutOfStockCount = items.Count(i => i.IsOutOfStock);
            ViewBag.HoldCount = items.Count(i => i.HasHold);
            ViewBag.RentedCount = items.Count(i => i.HasRented);
            ViewBag.DamagedCount = items.Count(i => i.HasDamaged);

            return View(model);
        }

        // ═══════════════════════════════════════════════════════
        //  STOCK IN — GET (partial view cho modal)
        // ═══════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> StockIn(int? facilityId)
        {
            await LoadDropdowns();
            return PartialView(new StockTransactionViewModel
            {
                FacilityId = facilityId ?? 0,
                TransactionType = TxType.StockIn,
                TransactionDate = DateTime.Now,
                Items = new List<StockTransactionItemViewModel> { new() }
            });
        }

        // ═══════════════════════════════════════════════════════
        //  STOCK IN — POST (JSON, hỗ trợ cả form-data lẫn JSON body)
        // ═══════════════════════════════════════════════════════
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> StockIn(StockTransactionViewModel model)
        {
            if (model.FacilityId <= 0)
                return Json(new { success = false, message = "Vui lòng chọn cơ sở!" });

            if (model.Items?.Any(i => i.Quantity > 0) != true)
                return Json(new { success = false, message = "Vui lòng nhập ít nhất một sản phẩm!" });

            var user = User.Identity?.Name ?? "system";
            foreach (var item in model.Items.Where(i => i.Quantity > 0 && i.ProductId > 0))
            {
                var (ok, msg) = await _svc.StockInAsync(
                    model.FacilityId, item.ProductId, item.Quantity,
                    item.CostPrice, model.BatchNumber, model.ExpiryDate,
                    model.DocumentReference, user, model.Note);
                if (!ok) return Json(new { success = false, message = msg });
            }
            return Json(new { success = true, message = "Nhập kho thành công!" });
        }

        // ═══════════════════════════════════════════════════════
        //  STOCK OUT — GET (partial view cho modal)
        // ═══════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> StockOut(int? facilityId)
        {
            await LoadDropdowns();
            return PartialView(new StockTransactionViewModel
            {
                FacilityId = facilityId ?? 0,
                TransactionType = TxType.StockOut,
                TransactionDate = DateTime.Now,
                Items = new List<StockTransactionItemViewModel> { new() }
            });
        }

        // ═══════════════════════════════════════════════════════
        //  STOCK OUT — POST
        // ═══════════════════════════════════════════════════════
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> StockOut(StockTransactionViewModel model)
        {
            if (model.FacilityId <= 0)
                return Json(new { success = false, message = "Vui lòng chọn cơ sở!" });

            if (model.Items?.Any(i => i.Quantity > 0) != true)
                return Json(new { success = false, message = "Vui lòng nhập ít nhất một sản phẩm!" });

            var user = User.Identity?.Name ?? "system";
            foreach (var item in model.Items.Where(i => i.Quantity > 0 && i.ProductId > 0))
            {
                var (ok, msg) = await _svc.StockOutAsync(
                    model.FacilityId, item.ProductId, item.Quantity,
                    model.Reason, user, model.Note);
                if (!ok) return Json(new { success = false, message = msg });
            }
            return Json(new { success = true, message = "Xuất kho thành công!" });
        }

        // ═══════════════════════════════════════════════════════
        //  ADJUSTMENT — GET (partial view)
        // ═══════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Adjustment(int inventoryId)
        {
            var inv = await _db.Inventories
                .Include(i => i.Product)
                .Include(i => i.Facility)
                .FirstOrDefaultAsync(i => i.InventoryId == inventoryId);
            if (inv == null) return NotFound();
            return PartialView(inv);
        }

        // ═══════════════════════════════════════════════════════
        //  ADJUSTMENT — POST
        // ═══════════════════════════════════════════════════════
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Adjustment(int inventoryId, int newQuantity, string note)
        {
            if (string.IsNullOrWhiteSpace(note))
                return Json(new { success = false, message = "Vui lòng nhập lý do điều chỉnh!" });

            var (ok, msg) = await _svc.AdjustAsync(inventoryId, newQuantity, User.Identity?.Name, note);
            return Json(new { success = ok, message = msg });
        }

        // ═══════════════════════════════════════════════════════
        //  TRANSFER — GET (partial view)
        // ═══════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Transfer(int? fromInventoryId)
        {
            await LoadDropdowns();
            ViewBag.FromInventoryId = fromInventoryId ?? 0;
            return PartialView();
        }

        // ═══════════════════════════════════════════════════════
        //  TRANSFER — POST (resolve inv ID from facilityId+productId)
        // ═══════════════════════════════════════════════════════
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Transfer(
            int fromInventoryId, int fromFacilityId, int targetFacilityId,
            int productId, int quantity, string? note)
        {
            if (fromFacilityId == targetFacilityId)
                return Json(new { success = false, message = "Cơ sở xuất và nhận phải khác nhau!" });

            // Resolve fromInventoryId if not provided
            if (fromInventoryId <= 0 && fromFacilityId > 0 && productId > 0)
            {
                var inv = await _db.Inventories
                    .FirstOrDefaultAsync(i => i.FacilityId == fromFacilityId && i.ProductId == productId);
                if (inv == null)
                    return Json(new { success = false, message = "Sản phẩm chưa có trong kho cơ sở xuất!" });
                fromInventoryId = inv.InventoryId;
            }

            var (ok, msg) = await _svc.TransferAsync(
                fromInventoryId, targetFacilityId, quantity, User.Identity?.Name, note);
            return Json(new { success = ok, message = msg });
        }

        // ═══════════════════════════════════════════════════════
        //  RENT OUT — POST
        // ═══════════════════════════════════════════════════════
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RentOut(
            int facilityId, int productId, int quantity,
            string? courtCode, string? customerName, string? customerPhone,
            string? size, int? maxHours, int? orderId, string? note)
        {
            var (ok, msg, rentalItemId) = await _svc.RentalOutAsync(
                facilityId, productId, quantity,
                courtCode, customerName, customerPhone,
                size, maxHours, orderId, User.Identity?.Name, note);
            return Json(new { success = ok, message = msg, rentalItemId });
        }

        // ═══════════════════════════════════════════════════════
        //  RENT RETURN — POST
        // ═══════════════════════════════════════════════════════
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RentReturn(
            int rentalItemId, int returnQuantity,
            bool isDamaged, decimal cleaningFee, string? note)
        {
            var (ok, msg) = await _svc.RentalReturnAsync(
                rentalItemId, returnQuantity, isDamaged, cleaningFee,
                User.Identity?.Name, note);
            return Json(new { success = ok, message = msg });
        }

        // ═══════════════════════════════════════════════════════
        //  RENT LOST — POST
        // ═══════════════════════════════════════════════════════
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RentLost(
            int rentalItemId, int lostQuantity, string? note)
        {
            var (ok, msg) = await _svc.RentalLostAsync(
                rentalItemId, lostQuantity, User.Identity?.Name, note);
            return Json(new { success = ok, message = msg });
        }

        // ═══════════════════════════════════════════════════════
        //  MARK DAMAGED — POST
        // ═══════════════════════════════════════════════════════
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkDamaged(int inventoryId, int quantity, string? note)
        {
            var (ok, msg) = await _svc.MarkDamagedAsync(inventoryId, quantity, User.Identity?.Name, note);
            return Json(new { success = ok, message = msg });
        }

        // ═══════════════════════════════════════════════════════
        //  WRITE OFF DAMAGED — POST
        // ═══════════════════════════════════════════════════════
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> WriteOffDamaged(int inventoryId, int quantity, string? note)
        {
            var (ok, msg) = await _svc.WriteOffDamagedAsync(inventoryId, quantity, User.Identity?.Name, note);
            return Json(new { success = ok, message = msg });
        }

        // ═══════════════════════════════════════════════════════
        //  HOLD — POST (gọi từ Order system)
        // ═══════════════════════════════════════════════════════
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Hold(
            int facilityId, int productId, int quantity, int orderId)
        {
            var (ok, msg) = await _svc.HoldAsync(facilityId, productId, quantity, orderId, User.Identity?.Name);
            return Json(new { success = ok, message = msg });
        }

        // ═══════════════════════════════════════════════════════
        //  RELEASE HOLD — POST
        // ═══════════════════════════════════════════════════════
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ReleaseHold(
            int facilityId, int productId, int quantity, int orderId, string? note)
        {
            var (ok, msg) = await _svc.ReleaseHoldAsync(
                facilityId, productId, quantity, orderId, User.Identity?.Name, note);
            return Json(new { success = ok, message = msg });
        }

        // ═══════════════════════════════════════════════════════
        //  CONFIRM SALE — POST (gọi từ Order system)
        // ═══════════════════════════════════════════════════════
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmSale(
            int facilityId, int productId, int quantity,
            decimal salePrice, decimal costPrice, int orderId)
        {
            var (ok, msg) = await _svc.ConfirmSaleAsync(
                facilityId, productId, quantity, salePrice, costPrice, orderId, User.Identity?.Name);
            return Json(new { success = ok, message = msg });
        }

        // ═══════════════════════════════════════════════════════
        //  LOW STOCK
        // ═══════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> LowStock()
        {
            var now = DateTime.Now;
            var soon = now.AddDays(30);

            var all = await _db.Inventories
                .Include(i => i.Product).ThenInclude(p => p.Category)
                .Include(i => i.Product).ThenInclude(p => p.Supplier)
                .Include(i => i.Product).ThenInclude(p => p.Variants)
                .Include(i => i.Facility)
                .ToListAsync();

            var flagged = all.Where(i =>
                i.IsLowStock || i.IsOutOfStock || i.HasDamaged ||
                (i.Product?.ExpiryDate.HasValue == true && i.Product.ExpiryDate <= soon))
                .OrderBy(i => i.AvailableQuantity)
                .ToList();

            var vm = new LowStockViewModel
            {
                OutOfStockCount = flagged.Count(i => i.IsOutOfStock),
                LowStockCount = flagged.Count(i => i.IsLowStock && !i.IsOutOfStock),
                DamagedCount = all.Count(i => i.HasDamaged),
                ExpiringCount = flagged.Count(i => { var ed = i.Product?.ExpiryDate; return ed.HasValue && ed >= now && ed <= soon; }),
                ExpiredCount = flagged.Count(i => { var ed = i.Product?.ExpiryDate; return ed.HasValue && ed < now; }),
                Items = flagged.Select(i =>
                {
                    var ed = i.Product?.ExpiryDate;
                    var isExp = ed.HasValue && ed < now;
                    var isExpS = ed.HasValue && !isExp && ed <= soon;
                    return new LowStockItemViewModel
                    {
                        InventoryId = i.InventoryId,
                        ProductId = i.ProductId,
                        ProductName = i.Product?.ProductName ?? "—",
                        ProductCode = i.Product?.ProductCode ?? i.Product?.SKU,
                        ImageUrl = i.Product?.ImageUrl,
                        CategoryName = i.Product?.Category?.CategoryName,
                        BehaviorType = i.Product?.Category?.BehaviorType ?? "Retail",
                        FacilityName = i.Facility?.FacilityName,
                        Unit = i.Product?.Unit,
                        SupplierName = i.Product?.Supplier?.SupplierName,
                        BatchNumber = i.Product?.BatchNumber,
                        StorageLocation = i.Product?.StorageLocation,
                        Quantity = i.Quantity,
                        AvailableQuantity = i.AvailableQuantity,
                        HoldQuantity = i.HoldQuantity,
                        RentedQuantity = i.RentedQuantity,
                        DamagedQuantity = i.DamagedQuantity,
                        MinQuantity = i.MinQuantity,
                        ExpiryDate = ed,
                        DaysUntilExpiry = ed.HasValue ? (int?)(ed.Value - now).Days : null,
                        IsOutOfStock = i.IsOutOfStock,
                        IsLowStock = i.IsLowStock,
                        IsExpired = isExp,
                        IsExpiringSoon = isExpS,
                        HasDamaged = i.HasDamaged,
                        LowVariants = i.Product?.Variants?
                            .Where(v => v.IsActive && v.AvailableQuantity <= v.MinStockLevel)
                            .Select(v => new LowStockVariantRow
                            {
                                DisplayName = v.DisplayName,
                                AvailableQty = v.AvailableQuantity
                            }).ToList() ?? new()
                    };
                }).ToList()
            };

            ViewBag.Facilities = await _db.Facilities.ToListAsync();
            return Request.Headers["X-Requested-With"] == "XMLHttpRequest"
                ? PartialView(vm) : View(vm);
        }

        // ═══════════════════════════════════════════════════════
        //  ACTIVE RENTALS
        // ═══════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> ActiveRentals(int? facilityId)
        {
            var today = DateTime.Today;
            var q = _db.RentalItems
                .Include(r => r.Inventory).ThenInclude(i => i.Product).ThenInclude(p => p.Category)
                .Include(r => r.Inventory).ThenInclude(i => i.Facility)
                .Where(r => r.Status == "Active"
                    || (r.Status == "Returned" && r.ReturnedAt >= today)
                    || ((r.Status == "Lost" || r.Status == "Damaged") && r.ReturnedAt >= DateTime.Now.AddDays(-7)))
                .AsQueryable();

            if (facilityId.HasValue)
                q = q.Where(r => r.Inventory.FacilityId == facilityId.Value);

            var list = await q
                .OrderByDescending(r => r.Status == "Active")
                .ThenBy(r => r.ExpectedReturnAt)
                .ThenByDescending(r => r.RentedAt)
                .ToListAsync();

            var vm = new ActiveRentalsViewModel
            {
                TotalActive = list.Count(r => r.Status == "Active"),
                TotalOverdue = list.Count(r => r.IsOverdue),
                TotalReturned = list.Count(r => r.Status == "Returned" && r.ReturnedAt?.Date == today),
                TotalLost = list.Count(r => r.Status == "Lost"),
                Items = list.Select(r => new RentalItemRowViewModel
                {
                    RentalItemId = r.RentalItemId,
                    InventoryId = r.InventoryId,
                    ProductId = r.Inventory?.ProductId ?? 0,
                    ProductName = r.Inventory?.Product?.ProductName ?? "—",
                    ProductCode = r.Inventory?.Product?.ProductCode ?? "",
                    CategoryName = r.Inventory?.Product?.Category?.CategoryName ?? "—",
                    FacilityName = r.Inventory?.Facility?.FacilityName ?? "—",
                    CourtCode = r.CourtCode,
                    CustomerName = r.CustomerName,
                    CustomerPhone = r.CustomerPhone,
                    Quantity = r.Quantity,
                    Size = r.Size,
                    Unit = r.Inventory?.Product?.Unit ?? "",
                    RentedAt = r.RentedAt,
                    ExpectedReturnAt = r.ExpectedReturnAt,
                    ReturnedAt = r.ReturnedAt,
                    Status = r.Status,
                    IsOverdue = r.IsOverdue,
                    DurationHours = r.Duration.TotalHours,
                    Note = r.Note,
                    CleaningFeeCharged = r.CleaningFeeCharged,
                    CreatedBy = r.CreatedBy,
                }).ToList()
            };

            ViewBag.Facilities = await _db.Facilities.ToListAsync();
            return Request.Headers["X-Requested-With"] == "XMLHttpRequest"
                ? PartialView(vm) : View(vm);
        }

        // ═══════════════════════════════════════════════════════
        //  HISTORY — GET (partial or full page)
        // ═══════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> History(int productId, int? facilityId)
        {
            var product = await _db.Products.FindAsync(productId);
            if (product == null) return NotFound();

            var q = _db.InventoryTransactions
                .Include(t => t.Facility)
                .Where(t => t.ProductId == productId);
            if (facilityId.HasValue)
                q = q.Where(t => t.FacilityId == facilityId.Value);

            var logs = await q.OrderByDescending(t => t.TransactionDate).Take(200).ToListAsync();

            var vm = new InventoryHistoryViewModel
            {
                ProductId = productId,
                ProductName = product.ProductName ?? "—",
                ProductCode = product.ProductCode ?? product.SKU ?? "",
                Transactions = logs.Select(t => new InventoryTransactionRowViewModel
                {
                    TransactionId = t.TransactionId,
                    Type = t.Type,
                    Quantity = t.Quantity,
                    QuantityAfter = t.QuantityAfter,
                    FacilityName = t.Facility?.FacilityName ?? "—",
                    OrderId = t.ReferenceId,
                    Note = t.Note,
                    UserEmail = t.UserEmail,
                    TransactionDate = t.TransactionDate,
                    CostPriceSnapshot = t.CostPrice > 0 ? t.CostPrice : null,
                    BatchNumber = t.BatchNumber,
                    ExpiryDate = t.ExpiryDate,
                    TargetFacilityId = t.TargetFacilityId,
                }).ToList()
            };

            return Request.Headers["X-Requested-With"] == "XMLHttpRequest"
                ? PartialView(vm) : View(vm);
        }

        // ═══════════════════════════════════════════════════════
        //  GET PRODUCT STOCK — AJAX API
        // ═══════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> GetProductStock(int facilityId, int productId)
        {
            var inv = await _db.Inventories
                .Include(i => i.Product)
                .FirstOrDefaultAsync(i => i.FacilityId == facilityId && i.ProductId == productId);

            if (inv == null)
                return Json(new { success = false, message = "Sản phẩm chưa có trong kho!" });

            return Json(new
            {
                success = true,
                inventoryId = inv.InventoryId,
                quantity = inv.Quantity,
                holdQuantity = inv.HoldQuantity,
                rentedQuantity = inv.RentedQuantity,
                damagedQuantity = inv.DamagedQuantity,
                availableQuantity = inv.AvailableQuantity,
                minQuantity = inv.MinQuantity,
                maxQuantity = inv.MaxQuantity,
                unit = inv.Product.Unit,
                isLowStock = inv.IsLowStock,
                isOutOfStock = inv.IsOutOfStock,
            });
        }

        // ═══════════════════════════════════════════════════════
        //  HOLD ALERTS — AJAX
        // ═══════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> HoldAlerts()
        {
            var stalled = await _svc.GetStalledHoldsAsync(120);
            return Json(stalled.Select(t => new
            {
                t.TransactionId,
                productName = t.Product?.ProductName ?? "—",
                t.Quantity,
                t.FacilityId,
                facilityName = t.Facility?.FacilityName ?? "—",
                orderId = t.ReferenceId,
                hoursAgo = (DateTime.Now - t.TransactionDate).TotalHours.ToString("F1"),
                t.Note
            }));
        }

        // ═══════════════════════════════════════════════════════
        //  HELPERS
        // ═══════════════════════════════════════════════════════
        private async Task LoadDropdowns()
        {
            ViewBag.Facilities = await _db.Facilities.Where(f => f.IsActive).ToListAsync();
            ViewBag.Products = await _db.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive)
                .OrderBy(p => p.ProductName)
                .ToListAsync();
        }
    }
}
