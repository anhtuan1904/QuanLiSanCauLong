// ===================================================================
// FILE: Controllers/AdminProductController.cs  (HOÀN CHỈNH v3)
// Modal AJAX + Wizard form
// ===================================================================
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiSanCauLong.Data;
using QuanLiSanCauLong.Models;

namespace QuanLiSanCauLong.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("Admin/[controller]")]
    public class AdminProductController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminProductController> _logger;
        private readonly IWebHostEnvironment _env;

        private const string ImageFolder = "images/products";

        public AdminProductController(
            ApplicationDbContext context,
            ILogger<AdminProductController> logger,
            IWebHostEnvironment env)
        {
            _context = context;
            _logger = logger;
            _env = env;
        }

        // ═══════════════════════════════════════════════════════
        //  1. INDEX
        // ═══════════════════════════════════════════════════════
        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index(
            int? categoryId, string? search,
            string? status, string? behaviorType)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Variants)
                .AsQueryable();

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p =>
                    p.ProductName.Contains(search) ||
                    (p.ProductCode != null && p.ProductCode.Contains(search)) ||
                    (p.SKU != null && p.SKU.Contains(search)));

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(p => p.Status == status);

            if (!string.IsNullOrWhiteSpace(behaviorType))
                query = query.Where(p => p.Category != null && p.Category.BehaviorType == behaviorType);

            var products = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();

            await LoadCategoriesToViewBag();

            // Stats for sidebar
            var all = await _context.Products.Include(p => p.Category).ToListAsync();
            ViewBag.CountAll = all.Count;
            ViewBag.CountRetail = all.Count(p => p.Category?.BehaviorType == "Retail");
            ViewBag.CountRental = all.Count(p => p.Category?.BehaviorType == "Rental");
            ViewBag.CountService = all.Count(p => p.Category?.BehaviorType == "Service");
            ViewBag.Search = search;
            ViewBag.CurrentCategory = categoryId;
            ViewBag.CurrentStatus = status;
            ViewBag.CurrentBehavior = behaviorType;

            return View("~/Views/Admin/AdminProduct/Index.cshtml", products);
        }

        // ═══════════════════════════════════════════════════════
        //  2. DETAILS
        // ═══════════════════════════════════════════════════════
        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Variants.Where(v => v.IsActive))
                .FirstOrDefaultAsync(m => m.ProductId == id);

            if (product == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy sản phẩm!";
                return RedirectToAction(nameof(Index));
            }

            return View("~/Views/Admin/AdminProduct/Details.cshtml", product);
        }

        // ═══════════════════════════════════════════════════════
        //  3. CREATE — GET (partial view, no layout, cho modal)
        // ═══════════════════════════════════════════════════════
        [HttpGet("Create")]
        public async Task<IActionResult> Create(string? behaviorType)
        {
            await LoadCategoriesToViewBag();
            ViewBag.DefaultBehavior = behaviorType ?? "Retail";
            return View("~/Views/Admin/AdminProduct/Create.cshtml", new Product());
        }

        // ═══════════════════════════════════════════════════════
        //  4. CREATE AJAX — POST từ modal (JSON response)
        // ═══════════════════════════════════════════════════════
        [HttpPost("CreateAjax")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAjax(
            Product model,
            IFormFile? ImageFile,
            List<string>? variantSize,
            List<string>? variantColor,
            List<int>? variantStock,
            List<int>? variantMinStock,
            decimal? depositAmount,
            decimal? cleaningFee,
            int? maxRentalHours,
            string? laborUnit,
            decimal? laborPrice,
            decimal? materialPrice)
        {
            if (string.IsNullOrWhiteSpace(model.ProductName))
                return Json(new { success = false, message = "Tên sản phẩm không được để trống!" });

            if (string.IsNullOrWhiteSpace(model.ProductCode))
                return Json(new { success = false, message = "Mã SKU không được để trống!" });

            if (model.CategoryId <= 0)
                return Json(new { success = false, message = "Vui lòng chọn danh mục!" });

            if (await _context.Products.AnyAsync(p => p.ProductCode == model.ProductCode))
                return Json(new { success = false, message = $"Mã SKU \"{model.ProductCode}\" đã tồn tại!" });

            try
            {
                var category = await _context.ProductCategories.FindAsync(model.CategoryId);
                var behavior = category?.BehaviorType ?? "Retail";

                model.ImageUrl = ImageFile != null ? await SaveImageAsync(ImageFile) : "/images/no-image.png";
                model.CreatedAt = DateTime.Now;
                model.UpdatedAt = DateTime.Now;
                model.IsActive = model.Status != "Inactive";
                model.Status ??= "Active";

                ApplyBehaviorDefaults(model, behavior, category,
                    depositAmount, cleaningFee, maxRentalHours,
                    laborUnit, laborPrice, materialPrice);

                if (behavior != "Service")
                {
                    var variants = BuildVariants(variantSize, variantColor, variantStock, variantMinStock, model.ProductCode);
                    model.Variants = variants;
                    if (variants.Any())
                        model.StockQuantity = variants.Sum(v => v.StockQuantity);
                }

                _context.Products.Add(model);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Tạo SP (AJAX): ID={Id}, Name={Name}, BH={B}",
                    model.ProductId, model.ProductName, behavior);

                return Json(new { success = true, message = $"Tạo \"{model.ProductName}\" thành công!", productId = model.ProductId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi CreateAjax");
                return Json(new { success = false, message = $"Có lỗi xảy ra: {ex.Message}" });
            }
        }

        // ═══════════════════════════════════════════════════════
        //  5. EDIT — GET (partial view, no layout, cho modal)
        // ═══════════════════════════════════════════════════════
        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Variants.Where(v => v.IsActive))
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy sản phẩm!";
                return RedirectToAction(nameof(Index));
            }

            await LoadCategoriesToViewBag();
            return View("~/Views/Admin/AdminProduct/Create.cshtml", product); // Dùng chung Create.cshtml
        }

        // ═══════════════════════════════════════════════════════
        //  6. EDIT AJAX — POST từ modal (JSON response)
        // ═══════════════════════════════════════════════════════
        [HttpPost("EditAjax/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAjax(
            int id,
            Product model,
            IFormFile? ImageFile,
            List<int?>? variantId,
            List<string>? variantSize,
            List<string>? variantColor,
            List<int>? variantStock,
            List<int>? variantMinStock,
            decimal? depositAmount,
            decimal? cleaningFee,
            int? maxRentalHours,
            string? laborUnit,
            decimal? laborPrice,
            decimal? materialPrice)
        {
            if (string.IsNullOrWhiteSpace(model.ProductName))
                return Json(new { success = false, message = "Tên sản phẩm không được để trống!" });

            if (await _context.Products.AnyAsync(p => p.ProductCode == model.ProductCode && p.ProductId != id))
                return Json(new { success = false, message = $"Mã SKU \"{model.ProductCode}\" đã tồn tại!" });

            var product = await _context.Products
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
                return Json(new { success = false, message = "Không tìm thấy sản phẩm!" });

            try
            {
                var category = await _context.ProductCategories.FindAsync(model.CategoryId);
                var behavior = category?.BehaviorType ?? "Retail";

                // Map basic fields
                product.ProductName = model.ProductName.Trim();
                product.ProductCode = model.ProductCode?.Trim() ?? product.ProductCode;
                product.SKU = model.ProductCode?.Trim() ?? product.SKU;
                product.CategoryId = model.CategoryId;
                product.Unit = model.Unit;
                product.Status = model.Status ?? "Active";
                product.IsActive = model.Status != "Inactive";
                product.Description = model.Description;
                product.Price = model.Price;
                product.CostPrice = model.CostPrice;
                product.SalePrice = model.SalePrice;
                product.StockQuantity = model.StockQuantity;
                product.MinStockLevel = model.MinStockLevel;
                product.UpdatedAt = DateTime.Now;

                if (ImageFile != null)
                    product.ImageUrl = await SaveImageAsync(ImageFile);

                ApplyBehaviorDefaults(product, behavior, category,
                    depositAmount, cleaningFee, maxRentalHours,
                    laborUnit, laborPrice, materialPrice);

                if (behavior != "Service")
                {
                    await UpdateVariants(id, variantId, variantSize, variantColor,
                                         variantStock, variantMinStock, product.ProductCode);

                    var activeVars = await _context.ProductVariants
                        .Where(v => v.ProductId == id && v.IsActive).ToListAsync();
                    if (activeVars.Any())
                    {
                        product.StockQuantity = activeVars.Sum(v => v.StockQuantity);
                        product.ReservedQuantity = activeVars.Sum(v => v.ReservedQuantity);
                    }
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Cập nhật SP (AJAX): ID={Id}", id);

                return Json(new { success = true, message = $"Cập nhật \"{product.ProductName}\" thành công!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi EditAjax ID={Id}", id);
                return Json(new { success = false, message = $"Có lỗi xảy ra: {ex.Message}" });
            }
        }

        // ═══════════════════════════════════════════════════════
        //  7. DELETE
        // ═══════════════════════════════════════════════════════
        [HttpPost("Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products
                .Include(p => p.OrderDetails)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
                return Json(new { success = false, message = "Không tìm thấy sản phẩm!" });

            if (product.OrderDetails != null && product.OrderDetails.Any())
                return Json(new { success = false, message = "Sản phẩm đã có đơn hàng, không thể xóa!" });

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = $"Đã xóa \"{product.ProductName}\"!" });
        }

        // ═══════════════════════════════════════════════════════
        //  8. DELETE VARIANT (AJAX)
        // ═══════════════════════════════════════════════════════
        [HttpPost("DeleteVariant/{variantId}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteVariant(int variantId)
        {
            var variant = await _context.ProductVariants
                .Include(v => v.OrderDetails)
                .FirstOrDefaultAsync(v => v.VariantId == variantId);

            if (variant == null)
                return Json(new { success = false, message = "Không tìm thấy phân loại!" });

            if (variant.OrderDetails != null && variant.OrderDetails.Any())
            { variant.IsActive = false; variant.UpdatedAt = DateTime.Now; }
            else
            { _context.ProductVariants.Remove(variant); }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã xóa phân loại!" });
        }

        // ═══════════════════════════════════════════════════════
        //  9. GET VARIANTS (API)
        // ═══════════════════════════════════════════════════════
        [HttpGet("GetVariants")]
        public async Task<IActionResult> GetVariants(int productId)
        {
            var variants = await _context.ProductVariants
                .Where(v => v.ProductId == productId && v.IsActive)
                .OrderBy(v => v.SizeName)
                .Select(v => new
                {
                    v.VariantId,
                    DisplayName = v.DisplayName,
                    v.SizeName,
                    v.ColorName,
                    v.StockQuantity,
                    v.ReservedQuantity,
                    AvailableQuantity = v.StockQuantity - v.ReservedQuantity
                })
                .ToListAsync();

            return Json(variants);
        }

        // ═══════════════════════════════════════════════════════
        //  10. GET PRODUCT INFO (API)
        // ═══════════════════════════════════════════════════════
        [HttpGet("GetProductInfo")]
        public async Task<IActionResult> GetProductInfo(int productId)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Variants.Where(v => v.IsActive))
                .FirstOrDefaultAsync(p => p.ProductId == productId);

            if (product == null)
                return Json(new { success = false, message = "Không tìm thấy sản phẩm!" });

            var behavior = product.Category?.BehaviorType ?? "Retail";

            return Json(new
            {
                success = true,
                productId = product.ProductId,
                productName = product.ProductName,
                behavior,
                unit = product.Unit,
                price = product.Price,
                depositRequired = product.Category?.DepositRequired ?? false,
                depositAmount = product.DepositAmount,
                cleaningFee = product.CleaningFee,
                maxRentalHours = product.MaxRentalHours,
                laborUnit = product.LaborUnit,
                laborPrice = product.LaborPrice,
                materialPrice = product.MaterialPrice,
                hasVariants = product.Variants?.Any() ?? false,
                variants = product.Variants?.Select(v => new
                {
                    v.VariantId,
                    v.SizeName,
                    v.ColorName,
                    displayName = v.DisplayName,
                    availableQuantity = v.AvailableQuantity,
                    rentedQuantity = v.RentedQuantity
                })
            });
        }

        // ═══════════════════════════════════════════════════════
        //  PRIVATE HELPERS
        // ═══════════════════════════════════════════════════════

        private static void ApplyBehaviorDefaults(
            Product model, string behavior, ProductCategory? cat,
            decimal? depositAmount, decimal? cleaningFee, int? maxRentalHours,
            string? laborUnit, decimal? laborPrice, decimal? materialPrice)
        {
            switch (behavior)
            {
                case "Rental":
                    model.DepositAmount = depositAmount ?? cat?.DefaultDepositAmount ?? 0;
                    model.CleaningFee = cleaningFee ?? cat?.DefaultCleaningFee ?? 0;
                    model.MaxRentalHours = maxRentalHours ?? cat?.MaxRentalHours;
                    model.RequiresDeposit = model.DepositAmount > 0;
                    model.LaborUnit = null;
                    model.LaborPrice = 0;
                    model.MaterialPrice = 0;
                    break;

                case "Service":
                    model.StockQuantity = 0;
                    model.ReservedQuantity = 0;
                    model.LaborUnit = laborUnit ?? cat?.MaterialUnit ?? "lần";
                    model.LaborPrice = laborPrice ?? 0;
                    model.MaterialPrice = materialPrice ?? 0;
                    // If total price not set, compute from labor + material
                    if (model.Price <= 0)
                        model.Price = model.LaborPrice + model.MaterialPrice;
                    model.DepositAmount = 0;
                    model.CleaningFee = 0;
                    model.MaxRentalHours = null;
                    model.RequiresDeposit = false;
                    break;

                default: // Retail
                    model.DepositAmount = 0;
                    model.CleaningFee = 0;
                    model.MaxRentalHours = null;
                    model.RequiresDeposit = false;
                    model.LaborUnit = null;
                    model.LaborPrice = 0;
                    model.MaterialPrice = 0;
                    break;
            }
        }

        private static List<ProductVariant> BuildVariants(
            List<string>? sizes, List<string>? colors,
            List<int>? stocks, List<int>? minStocks, string? productCode)
        {
            var result = new List<ProductVariant>();
            if (sizes == null || !sizes.Any()) return result;

            for (int i = 0; i < sizes.Count; i++)
            {
                var size = sizes.ElementAtOrDefault(i)?.Trim();
                var color = colors?.ElementAtOrDefault(i)?.Trim();
                var stock = stocks?.ElementAtOrDefault(i) ?? 0;
                var minStock = minStocks?.ElementAtOrDefault(i) ?? 0;

                if (string.IsNullOrEmpty(size) && string.IsNullOrEmpty(color)) continue;

                result.Add(new ProductVariant
                {
                    SizeName = size,
                    ColorName = color,
                    VariantSKU = $"{productCode}-{size}{(string.IsNullOrEmpty(color) ? "" : $"-{color}")}".ToUpper(),
                    StockQuantity = stock,
                    MinStockLevel = minStock,
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                });
            }
            return result;
        }

        private async Task UpdateVariants(
            int productId,
            List<int?>? ids, List<string>? sizes, List<string>? colors,
            List<int>? stocks, List<int>? minStocks, string? productCode)
        {
            if (sizes == null) return;

            var existingVariants = await _context.ProductVariants
                .Where(v => v.ProductId == productId)
                .ToListAsync();

            var keptIds = new HashSet<int>();

            for (int i = 0; i < sizes.Count; i++)
            {
                var size = sizes.ElementAtOrDefault(i)?.Trim();
                var color = colors?.ElementAtOrDefault(i)?.Trim();
                var stock = stocks?.ElementAtOrDefault(i) ?? 0;
                var minStock = minStocks?.ElementAtOrDefault(i) ?? 0;
                var vid = ids?.ElementAtOrDefault(i);

                if (string.IsNullOrEmpty(size) && string.IsNullOrEmpty(color)) continue;

                if (vid.HasValue && vid.Value > 0)
                {
                    var existing = existingVariants.FirstOrDefault(v => v.VariantId == vid.Value);
                    if (existing != null)
                    {
                        existing.SizeName = size;
                        existing.ColorName = color;
                        existing.StockQuantity = stock;
                        existing.MinStockLevel = minStock;
                        existing.IsActive = true;
                        existing.UpdatedAt = DateTime.Now;
                        keptIds.Add(existing.VariantId);
                    }
                }
                else
                {
                    _context.ProductVariants.Add(new ProductVariant
                    {
                        ProductId = productId,
                        SizeName = size,
                        ColorName = color,
                        VariantSKU = $"{productCode}-{size}{(string.IsNullOrEmpty(color) ? "" : $"-{color}")}".ToUpper(),
                        StockQuantity = stock,
                        MinStockLevel = minStock,
                        IsActive = true,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    });
                }
            }

            // Soft-delete variants không còn trong form
            foreach (var v in existingVariants.Where(v => v.IsActive && !keptIds.Contains(v.VariantId)))
            {
                v.IsActive = false;
                v.UpdatedAt = DateTime.Now;
            }
        }

        private async Task LoadCategoriesToViewBag()
        {
            var cats = await _context.ProductCategories
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.CategoryName)
                .ToListAsync();

            ViewBag.Categories = cats;
            ViewBag.CategoriesJson = System.Text.Json.JsonSerializer.Serialize(
                cats.Select(c => new
                {
                    c.CategoryId,
                    c.CategoryName,
                    c.BehaviorType,
                    c.CategoryType,
                    c.DefaultUnit,
                    c.DepositRequired,
                    c.DefaultDepositAmount,
                    c.DefaultCleaningFee,
                    c.MaxRentalHours,
                    c.MaterialUnit,
                    c.HasVariants,
                    c.RequiresSize
                }));
        }

        private async Task<string> SaveImageAsync(IFormFile file)
        {
            var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp", "image/gif" };
            if (!allowedTypes.Contains(file.ContentType.ToLower()))
                throw new InvalidOperationException("Chỉ chấp nhận ảnh JPG, PNG, WEBP hoặc GIF.");
            if (file.Length > 5 * 1024 * 1024)
                throw new InvalidOperationException("Ảnh không được vượt quá 5MB.");

            var uploadDir = Path.Combine(_env.WebRootPath, ImageFolder);
            Directory.CreateDirectory(uploadDir);

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName = $"prod_{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(uploadDir, fileName);

            await using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);
            return $"/{ImageFolder}/{fileName}";
        }

        private bool ProductExists(int id) =>
            _context.Products.Any(e => e.ProductId == id);
    }
}
