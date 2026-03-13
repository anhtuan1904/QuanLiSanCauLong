// ===================================================================
// FILE: Controllers/AdminProductController.cs  (CẬP NHẬT v2)
// Hỗ trợ 3 nhóm BehaviorType: Retail | Rental | Service
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

        // ───────────────────────────────────────────────
        // 1. DANH SÁCH SẢN PHẨM
        // ───────────────────────────────────────────────
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

            // Lọc theo nhóm hành vi
            if (!string.IsNullOrWhiteSpace(behaviorType))
                query = query.Where(p => p.Category != null && p.Category.BehaviorType == behaviorType);

            var products = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();

            await LoadCategoriesToViewBag();
            ViewBag.Search = search;
            ViewBag.CurrentCategory = categoryId;
            ViewBag.CurrentStatus = status;
            ViewBag.CurrentBehavior = behaviorType;

            // Đếm theo nhóm để hiển thị tabs
            var allProds = await _context.Products
                .Include(p => p.Category)
                .ToListAsync();
            ViewBag.CountRetail = allProds.Count(p => p.Category?.BehaviorType == "Retail");
            ViewBag.CountRental = allProds.Count(p => p.Category?.BehaviorType == "Rental");
            ViewBag.CountService = allProds.Count(p => p.Category?.BehaviorType == "Service");

            return View("~/Views/Admin/AdminProduct/Index.cshtml", products);
        }

        // ───────────────────────────────────────────────
        // 2. CHI TIẾT SẢN PHẨM
        // ───────────────────────────────────────────────
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

        // ───────────────────────────────────────────────
        // 3. THÊM MỚI SẢN PHẨM
        // ───────────────────────────────────────────────
        [HttpGet("Create")]
        public async Task<IActionResult> Create(string? behaviorType)
        {
            await LoadCategoriesToViewBag();
            ViewBag.DefaultBehavior = behaviorType ?? "Retail";
            return View("~/Views/Admin/AdminProduct/Create.cshtml");
        }

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            Product model,
            IFormFile? ImageFile,
            List<string>? variantSize,
            List<string>? variantColor,
            List<int>? variantStock,
            List<int>? variantMinStock,
            // Trường mở rộng cho Rental
            decimal? depositAmount,
            decimal? cleaningFee,
            int? maxRentalHours,
            // Trường mở rộng cho Service
            string? laborUnit,
            decimal? laborPrice,
            decimal? materialPrice)
        {
            ModelState.Remove("Category");
            ModelState.Remove("Inventories");
            ModelState.Remove("OrderDetails");
            ModelState.Remove("ImageUrl");
            ModelState.Remove("Variants");

            if (!ModelState.IsValid)
            {
                await LoadCategoriesToViewBag();
                return View("~/Views/Admin/AdminProduct/Create.cshtml", model);
            }

            if (await _context.Products.AnyAsync(p => p.ProductCode == model.ProductCode))
            {
                ModelState.AddModelError("ProductCode", "Mã sản phẩm đã tồn tại!");
                await LoadCategoriesToViewBag();
                return View("~/Views/Admin/AdminProduct/Create.cshtml", model);
            }

            try
            {
                // Đọc BehaviorType từ danh mục được chọn
                var category = await _context.ProductCategories.FindAsync(model.CategoryId);
                var behavior = category?.BehaviorType ?? "Retail";

                model.ImageUrl = ImageFile != null ? await SaveImageAsync(ImageFile) : "/images/no-image.png";
                model.CreatedAt = DateTime.Now;
                model.UpdatedAt = DateTime.Now;
                model.IsActive = true;
                model.Status = "Active";

                // ── Áp dụng logic theo nhóm ──
                ApplyBehaviorDefaults(model, behavior, category,
                    depositAmount, cleaningFee, maxRentalHours,
                    laborUnit, laborPrice, materialPrice);

                // ── Variants (Retail + Rental có size/màu) ──
                if (behavior != "Service")
                {
                    var variants = BuildVariants(variantSize, variantColor, variantStock, variantMinStock, model.ProductCode);
                    model.Variants = variants;
                    if (variants.Any())
                        model.StockQuantity = variants.Sum(v => v.StockQuantity);
                }

                _context.Products.Add(model);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Tạo sản phẩm: ID={Id}, Name={Name}, Behavior={B}",
                    model.ProductId, model.ProductName, behavior);
                TempData["SuccessMessage"] = $"Thêm sản phẩm \"{model.ProductName}\" thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo sản phẩm");
                TempData["ErrorMessage"] = $"Có lỗi xảy ra: {ex.Message}";
                await LoadCategoriesToViewBag();
                return View("~/Views/Admin/AdminProduct/Create.cshtml", model);
            }
        }

        // ───────────────────────────────────────────────
        // 4. CHỈNH SỬA SẢN PHẨM
        // ───────────────────────────────────────────────
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
            return View("~/Views/Admin/AdminProduct/Edit.cshtml", product);
        }

        [HttpPost("Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
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
            if (id != model.ProductId) return NotFound();

            ModelState.Remove("Category");
            ModelState.Remove("Inventories");
            ModelState.Remove("OrderDetails");
            ModelState.Remove("Variants");

            if (!ModelState.IsValid)
            {
                await LoadCategoriesToViewBag();
                return View("~/Views/Admin/AdminProduct/Edit.cshtml", model);
            }

            try
            {
                if (await _context.Products.AnyAsync(p => p.ProductCode == model.ProductCode && p.ProductId != id))
                {
                    ModelState.AddModelError("ProductCode", "Mã sản phẩm đã tồn tại!");
                    await LoadCategoriesToViewBag();
                    return View("~/Views/Admin/AdminProduct/Edit.cshtml", model);
                }

                var category = await _context.ProductCategories.FindAsync(model.CategoryId);
                var behavior = category?.BehaviorType ?? "Retail";

                if (ImageFile != null)
                    model.ImageUrl = await SaveImageAsync(ImageFile);

                model.UpdatedAt = DateTime.Now;

                ApplyBehaviorDefaults(model, behavior, category,
                    depositAmount, cleaningFee, maxRentalHours,
                    laborUnit, laborPrice, materialPrice);

                // Variants chỉ áp dụng cho Retail & Rental
                if (behavior != "Service")
                {
                    await UpdateVariants(id, variantId, variantSize, variantColor,
                                        variantStock, variantMinStock, model.ProductCode);

                    var allVariants = await _context.ProductVariants
                        .Where(v => v.ProductId == id && v.IsActive)
                        .ToListAsync();

                    if (allVariants.Any())
                    {
                        model.StockQuantity = allVariants.Sum(v => v.StockQuantity);
                        model.ReservedQuantity = allVariants.Sum(v => v.ReservedQuantity);
                    }
                }

                _context.Update(model);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Cập nhật sản phẩm ID={Id}", id);
                TempData["SuccessMessage"] = $"Cập nhật \"{model.ProductName}\" thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(model.ProductId)) return NotFound();
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật sản phẩm ID={Id}", id);
                TempData["ErrorMessage"] = $"Có lỗi xảy ra: {ex.Message}";
                await LoadCategoriesToViewBag();
                return View("~/Views/Admin/AdminProduct/Edit.cshtml", model);
            }
        }

        // ───────────────────────────────────────────────
        // 5. XÓA SẢN PHẨM
        // ───────────────────────────────────────────────
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
            return Json(new { success = true, message = "Xóa sản phẩm thành công!" });
        }

        // ───────────────────────────────────────────────
        // 6. XÓA VARIANT (AJAX)
        // ───────────────────────────────────────────────
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
            {
                // Soft-delete nếu có đơn hàng liên quan
                variant.IsActive = false;
                variant.UpdatedAt = DateTime.Now;
            }
            else
            {
                _context.ProductVariants.Remove(variant);
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã xóa phân loại!" });
        }

        // ───────────────────────────────────────────────
        // 7. API: Lấy variants (dùng cho StockIn/StockOut)
        // ───────────────────────────────────────────────
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

        // ───────────────────────────────────────────────
        // 8. API: Lấy thông tin sản phẩm kèm BehaviorType
        // ───────────────────────────────────────────────
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
                behavior = behavior,
                unit = product.Unit,
                price = product.Price,
                // Rental fields
                depositRequired = product.Category?.DepositRequired ?? false,
                depositAmount = product.DepositAmount,
                cleaningFee = product.CleaningFee,
                maxRentalHours = product.MaxRentalHours,
                // Service fields
                laborUnit = product.LaborUnit,
                laborPrice = product.LaborPrice,
                materialPrice = product.MaterialPrice,
                // Variants
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

        // ───────────────────────────────────────────────
        // HELPERS
        // ───────────────────────────────────────────────

        /// <summary>
        /// Áp dụng giá trị mặc định / override theo BehaviorType của danh mục.
        /// Retail  → trừ kho thẳng, không cần deposit, không cần labor.
        /// Rental  → hoàn kho khi trả, có deposit + cleaningFee.
        /// Service → không có kho vật lý, chỉ tính công + vật liệu.
        /// </summary>
        private static void ApplyBehaviorDefaults(
            Product model,
            string behavior,
            ProductCategory? cat,
            decimal? depositAmount, decimal? cleaningFee, int? maxRentalHours,
            string? laborUnit, decimal? laborPrice, decimal? materialPrice)
        {
            switch (behavior)
            {
                case "Rental":
                    // Kế thừa default từ danh mục nếu form không truyền
                    model.DepositAmount = depositAmount ?? cat?.DefaultDepositAmount ?? 0;
                    model.CleaningFee = cleaningFee ?? cat?.DefaultCleaningFee ?? 0;
                    model.MaxRentalHours = maxRentalHours ?? cat?.MaxRentalHours;
                    model.RequiresDeposit = cat?.DepositRequired ?? false;
                    // Service fields không dùng
                    model.LaborUnit = null;
                    model.LaborPrice = 0;
                    model.MaterialPrice = 0;
                    break;

                case "Service":
                    // Không quản lý kho vật lý
                    model.StockQuantity = 0;
                    model.ReservedQuantity = 0;
                    model.LaborUnit = laborUnit ?? cat?.MaterialUnit ?? "bộ";
                    model.LaborPrice = laborPrice ?? 0;
                    model.MaterialPrice = materialPrice ?? 0;
                    // Rental fields không dùng
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

        private List<ProductVariant> BuildVariants(
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
            // Trả về dạng JSON để JS dùng khi switch danh mục
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
