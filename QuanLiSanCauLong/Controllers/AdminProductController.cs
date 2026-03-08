using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiSanCauLong.Data;
using QuanLiSanCauLong.Models;

namespace QuanLiSanCauLong.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminProductController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminProductController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ───────────────────────────────────────────────
        // 1. DANH SÁCH SẢN PHẨM
        // ───────────────────────────────────────────────
        public async Task<IActionResult> Index(int? categoryId, string search, string status)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Variants)
                .AsQueryable();

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(p => p.ProductName.Contains(search) || p.ProductCode.Contains(search));

            if (!string.IsNullOrEmpty(status))
                query = query.Where(p => p.Status == status);

            var products = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();

            ViewBag.Categories = await _context.ProductCategories.ToListAsync();
            return View(products);
        }

        // ───────────────────────────────────────────────
        // 2. CHI TIẾT SẢN PHẨM
        // ───────────────────────────────────────────────
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Variants.Where(v => v.IsActive))
                .FirstOrDefaultAsync(m => m.ProductId == id);

            if (product == null) return NotFound();

            return View(product);
        }

        // ───────────────────────────────────────────────
        // 3. THÊM MỚI SẢN PHẨM
        // ───────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadCategoriesToViewBag();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product model, IFormFile? ImageFile,
            List<string>? variantSize, List<string>? variantColor,
            List<int>? variantStock, List<int>? variantMinStock)
        {
            ModelState.Remove("Category");
            ModelState.Remove("Inventories");
            ModelState.Remove("OrderDetails");
            ModelState.Remove("ImageUrl");
            ModelState.Remove("Variants");

            if (ModelState.IsValid)
            {
                if (await _context.Products.AnyAsync(p => p.ProductCode == model.ProductCode))
                {
                    ModelState.AddModelError("ProductCode", "Mã sản phẩm đã tồn tại!");
                }
                else
                {
                    model.ImageUrl = ImageFile != null ? await SaveImage(ImageFile) : "/images/no-image.png";
                    model.CreatedAt = DateTime.Now;
                    model.UpdatedAt = DateTime.Now;
                    model.IsActive = true;

                    // ── Tạo variants từ form ──
                    var variants = BuildVariants(variantSize, variantColor, variantStock, variantMinStock, model.ProductCode);
                    model.Variants = variants;

                    // ── Tổng hợp tồn kho lên Product ──
                    if (variants.Any())
                    {
                        model.StockQuantity = variants.Sum(v => v.StockQuantity);
                    }

                    _context.Products.Add(model);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Thêm sản phẩm thành công!";
                    return RedirectToAction(nameof(Index));
                }
            }

            await LoadCategoriesToViewBag();
            return View(model);
        }

        // ───────────────────────────────────────────────
        // 4. CHỈNH SỬA SẢN PHẨM
        // ───────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products
                .Include(p => p.Variants.Where(v => v.IsActive))
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null) return NotFound();

            await LoadCategoriesToViewBag();
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product model, IFormFile? ImageFile,
            List<int?>? variantId, List<string>? variantSize, List<string>? variantColor,
            List<int>? variantStock, List<int>? variantMinStock)
        {
            if (id != model.ProductId) return NotFound();

            ModelState.Remove("Category");
            ModelState.Remove("Inventories");
            ModelState.Remove("OrderDetails");
            ModelState.Remove("Variants");

            if (ModelState.IsValid)
            {
                try
                {
                    if (await _context.Products.AnyAsync(p => p.ProductCode == model.ProductCode && p.ProductId != id))
                    {
                        ModelState.AddModelError("ProductCode", "Mã sản phẩm đã tồn tại!");
                        await LoadCategoriesToViewBag();
                        return View(model);
                    }

                    if (ImageFile != null)
                        model.ImageUrl = await SaveImage(ImageFile);

                    model.UpdatedAt = DateTime.Now;

                    // ── Xử lý variants ──
                    await UpdateVariants(id, variantId, variantSize, variantColor,
                                        variantStock, variantMinStock, model.ProductCode);

                    // ── Sync tổng tồn kho từ variants ──
                    var allVariants = await _context.ProductVariants
                        .Where(v => v.ProductId == id && v.IsActive)
                        .ToListAsync();

                    if (allVariants.Any())
                    {
                        model.StockQuantity = allVariants.Sum(v => v.StockQuantity);
                        model.ReservedQuantity = allVariants.Sum(v => v.ReservedQuantity);
                    }

                    _context.Update(model);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Cập nhật sản phẩm thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(model.ProductId)) return NotFound();
                    throw;
                }
            }

            await LoadCategoriesToViewBag();
            return View(model);
        }

        // ───────────────────────────────────────────────
        // 5. XÓA SẢN PHẨM
        // ───────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products
                .Include(p => p.OrderDetails)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
                return Json(new { success = false, message = "Không tìm thấy!" });

            if (product.OrderDetails != null && product.OrderDetails.Any())
                return Json(new { success = false, message = "Sản phẩm đã có đơn hàng, không thể xóa!" });

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Xóa thành công!" });
        }

        // ───────────────────────────────────────────────
        // 6. XÓA VARIANT (AJAX)
        // ───────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> DeleteVariant(int variantId)
        {
            var variant = await _context.ProductVariants
                .Include(v => v.OrderDetails)
                .FirstOrDefaultAsync(v => v.VariantId == variantId);

            if (variant == null)
                return Json(new { success = false, message = "Không tìm thấy phân loại!" });

            // Nếu có đơn hàng liên quan → soft-delete
            if (variant.OrderDetails != null && variant.OrderDetails.Any())
            {
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
        // 7. API: Lấy variants của product (dùng cho StockIn/StockOut)
        // GET /AdminProduct/GetVariants?productId=5
        // ───────────────────────────────────────────────
        [HttpGet]
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
        // HELPERS
        // ───────────────────────────────────────────────
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
                    // Cập nhật variant hiện có
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
                    // Thêm mới
                    var newVariant = new ProductVariant
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
                    };
                    _context.ProductVariants.Add(newVariant);
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
            ViewBag.Categories = await _context.ProductCategories.ToListAsync();
        }

        private async Task<string> SaveImage(IFormFile file)
        {
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products");

            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            var filePath = Path.Combine(folderPath, fileName);
            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            return "/images/products/" + fileName;
        }

        private bool ProductExists(int id) => _context.Products.Any(e => e.ProductId == id);
    }
}
