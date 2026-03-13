// ===================================================================
// FILE: Controllers/AdminCategoryController.cs  (CẬP NHẬT v2)
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
    public class AdminCategoryController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminCategoryController> _logger;
        private readonly IWebHostEnvironment _env;

        private const string ImageFolder = "uploads/categories";

        public AdminCategoryController(
            ApplicationDbContext context,
            ILogger<AdminCategoryController> logger,
            IWebHostEnvironment env)
        {
            _context = context;
            _logger = logger;
            _env = env;
        }

        // ─────────────────────────────────────────────
        // INDEX
        // ─────────────────────────────────────────────
        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index()
        {
            var categories = await _context.ProductCategories
                .Include(c => c.Products)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.CategoryName)
                .ToListAsync();
            return View("~/Views/Admin/AdminCategory/Index.cshtml", categories);
        }

        // ─────────────────────────────────────────────
        // CREATE – GET
        // ─────────────────────────────────────────────
        [HttpGet("Create")]
        public IActionResult Create()
        {
            return View("~/Views/Admin/AdminCategory/Create.cshtml");
        }

        // ─────────────────────────────────────────────
        // CREATE – POST
        // ─────────────────────────────────────────────
        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductCategory model, IFormFile? CategoryImage)
        {
            if (string.IsNullOrWhiteSpace(model.CategoryName))
            {
                TempData["ErrorMessage"] = "Tên danh mục không được để trống!";
                return View("~/Views/Admin/AdminCategory/Create.cshtml", model);
            }

            if (string.IsNullOrWhiteSpace(model.CategoryType))
            {
                TempData["ErrorMessage"] = "Loại hàng hoá không được để trống!";
                return View("~/Views/Admin/AdminCategory/Create.cshtml", model);
            }

            if (string.IsNullOrWhiteSpace(model.BehaviorType))
            {
                TempData["ErrorMessage"] = "Hành vi nghiệp vụ không được để trống!";
                return View("~/Views/Admin/AdminCategory/Create.cshtml", model);
            }

            var exists = await _context.ProductCategories
                .AnyAsync(c => c.CategoryName.ToLower().Trim() == model.CategoryName.ToLower().Trim());

            if (exists)
            {
                TempData["ErrorMessage"] = "Tên danh mục đã tồn tại!";
                return View("~/Views/Admin/AdminCategory/Create.cshtml", model);
            }

            try
            {
                var category = MapFromModel(model);
                category.CreatedAt = DateTime.Now;
                category.UpdatedAt = DateTime.Now;
                category.ImageUrl = "";

                if (CategoryImage != null && CategoryImage.Length > 0)
                    category.ImageUrl = await SaveImageAsync(CategoryImage);

                _context.ProductCategories.Add(category);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Tạo danh mục: ID={Id}, Name={Name}, Behavior={B}",
                    category.CategoryId, category.CategoryName, category.BehaviorType);
                TempData["SuccessMessage"] = $"Tạo danh mục \"{category.CategoryName}\" thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ioEx)
            {
                TempData["ErrorMessage"] = ioEx.Message;
                return View("~/Views/Admin/AdminCategory/Create.cshtml", model);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Lỗi DB khi tạo danh mục");
                TempData["ErrorMessage"] = $"Lỗi cơ sở dữ liệu: {dbEx.InnerException?.Message ?? dbEx.Message}";
                return View("~/Views/Admin/AdminCategory/Create.cshtml", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo danh mục");
                TempData["ErrorMessage"] = $"Có lỗi xảy ra: {ex.Message}";
                return View("~/Views/Admin/AdminCategory/Create.cshtml", model);
            }
        }

        // ─────────────────────────────────────────────
        // EDIT – GET
        // ─────────────────────────────────────────────
        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _context.ProductCategories.FindAsync(id);
            if (category == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy danh mục!";
                return RedirectToAction(nameof(Index));
            }
            return View("~/Views/Admin/AdminCategory/Edit.cshtml", category);
        }

        // ─────────────────────────────────────────────
        // EDIT – POST
        // ─────────────────────────────────────────────
        [HttpPost("Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductCategory model, IFormFile? CategoryImage)
        {
            if (id != model.CategoryId)
            {
                TempData["ErrorMessage"] = "Dữ liệu không hợp lệ!";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(model.CategoryName))
            {
                TempData["ErrorMessage"] = "Tên danh mục không được để trống!";
                return View("~/Views/Admin/AdminCategory/Edit.cshtml", model);
            }

            if (string.IsNullOrWhiteSpace(model.CategoryType))
            {
                TempData["ErrorMessage"] = "Loại hàng hoá không được để trống!";
                return View("~/Views/Admin/AdminCategory/Edit.cshtml", model);
            }

            var category = await _context.ProductCategories.FindAsync(model.CategoryId);
            if (category == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy danh mục!";
                return RedirectToAction(nameof(Index));
            }

            var exists = await _context.ProductCategories
                .AnyAsync(c => c.CategoryName.ToLower().Trim() == model.CategoryName.ToLower().Trim()
                            && c.CategoryId != model.CategoryId);
            if (exists)
            {
                TempData["ErrorMessage"] = "Tên danh mục đã tồn tại!";
                return View("~/Views/Admin/AdminCategory/Edit.cshtml", model);
            }

            try
            {
                // Map all fields
                category.CategoryName = model.CategoryName.Trim();
                category.CategoryType = model.CategoryType.Trim();
                category.BehaviorType = model.BehaviorType?.Trim() ?? "Retail";
                category.Description = string.IsNullOrWhiteSpace(model.Description) ? "" : model.Description.Trim();
                category.DefaultUnit = model.DefaultUnit;
                category.RequiresExpiry = model.RequiresExpiry;
                category.UseFIFO = model.UseFIFO;
                category.RequiresBatch = model.RequiresBatch;
                category.HasVariants = model.HasVariants;
                category.RequiresSize = model.RequiresSize;
                category.DefaultMinStock = model.DefaultMinStock;
                // Rental
                category.AllowPartialReturn = model.AllowPartialReturn;
                category.DepositRequired = model.DepositRequired;
                category.DefaultDepositAmount = model.DefaultDepositAmount;
                category.DefaultCleaningFee = model.DefaultCleaningFee;
                category.MaxRentalHours = model.MaxRentalHours;
                category.ChargeOvertime = model.ChargeOvertime;
                // Service
                category.PricingModel = model.PricingModel ?? "Fixed";
                category.SeparateLaborAndMaterial = model.SeparateLaborAndMaterial;
                category.MaterialUnit = model.MaterialUnit;
                category.AllowCustomerMaterial = model.AllowCustomerMaterial;
                // Meta
                category.DisplayOrder = model.DisplayOrder;
                category.IsActive = model.IsActive;
                category.UpdatedAt = DateTime.Now;

                if (CategoryImage != null && CategoryImage.Length > 0)
                {
                    DeleteImageIfExists(category.ImageUrl);
                    category.ImageUrl = await SaveImageAsync(CategoryImage);
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Cập nhật danh mục: ID={Id}", category.CategoryId);
                TempData["SuccessMessage"] = $"Cập nhật danh mục \"{category.CategoryName}\" thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ioEx)
            {
                TempData["ErrorMessage"] = ioEx.Message;
                return View("~/Views/Admin/AdminCategory/Edit.cshtml", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật danh mục ID={Id}", model.CategoryId);
                TempData["ErrorMessage"] = $"Có lỗi xảy ra: {ex.Message}";
                return View("~/Views/Admin/AdminCategory/Edit.cshtml", model);
            }
        }

        // ─────────────────────────────────────────────
        // DETAILS
        // ─────────────────────────────────────────────
        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var category = await _context.ProductCategories
                .Include(c => c.Products)
                    .ThenInclude(p => p.Inventories)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy danh mục!";
                return RedirectToAction(nameof(Index));
            }

            return View("~/Views/Admin/AdminCategory/Details.cshtml", category);
        }

        // ─────────────────────────────────────────────
        // DELETE
        // ─────────────────────────────────────────────
        [HttpPost("Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                if (id <= 0)
                    return Json(new { success = false, message = "ID không hợp lệ!" });

                var category = await _context.ProductCategories
                    .Include(c => c.Products)
                    .FirstOrDefaultAsync(c => c.CategoryId == id);

                if (category == null)
                    return Json(new { success = false, message = "Không tìm thấy danh mục!" });

                if (category.Products != null && category.Products.Any())
                    return Json(new { success = false, message = $"Không thể xóa! Danh mục đang có {category.Products.Count} sản phẩm." });

                DeleteImageIfExists(category.ImageUrl);
                _context.ProductCategories.Remove(category);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Xóa danh mục: ID={Id}, Name={Name}", id, category.CategoryName);
                return Json(new { success = true, message = $"Đã xóa danh mục \"{category.CategoryName}\" thành công!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa danh mục ID={Id}", id);
                return Json(new { success = false, message = $"Có lỗi xảy ra: {ex.Message}" });
            }
        }

        // ─────────────────────────────────────────────
        // TOGGLE ACTIVE (AJAX)
        // ─────────────────────────────────────────────
        [HttpPost("ToggleActive/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var category = await _context.ProductCategories.FindAsync(id);
            if (category == null)
                return Json(new { success = false, message = "Không tìm thấy danh mục!" });

            category.IsActive = !category.IsActive;
            category.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                isActive = category.IsActive,
                message = category.IsActive ? "Đã kích hoạt danh mục." : "Đã ẩn danh mục."
            });
        }

        // ─────────────────────────────────────────────
        // HELPERS
        // ─────────────────────────────────────────────
        private ProductCategory MapFromModel(ProductCategory model) => new ProductCategory
        {
            CategoryName = model.CategoryName.Trim(),
            CategoryType = model.CategoryType.Trim(),
            BehaviorType = model.BehaviorType?.Trim() ?? "Retail",
            Description = string.IsNullOrWhiteSpace(model.Description) ? "" : model.Description.Trim(),
            DefaultUnit = model.DefaultUnit,
            RequiresExpiry = model.RequiresExpiry,
            UseFIFO = model.UseFIFO,
            RequiresBatch = model.RequiresBatch,
            HasVariants = model.HasVariants,
            RequiresSize = model.RequiresSize,
            DefaultMinStock = model.DefaultMinStock,
            AllowPartialReturn = model.AllowPartialReturn,
            DepositRequired = model.DepositRequired,
            DefaultDepositAmount = model.DefaultDepositAmount,
            DefaultCleaningFee = model.DefaultCleaningFee,
            MaxRentalHours = model.MaxRentalHours,
            ChargeOvertime = model.ChargeOvertime,
            PricingModel = model.PricingModel ?? "Fixed",
            SeparateLaborAndMaterial = model.SeparateLaborAndMaterial,
            MaterialUnit = model.MaterialUnit,
            AllowCustomerMaterial = model.AllowCustomerMaterial,
            DisplayOrder = model.DisplayOrder,
            IsActive = model.IsActive,
        };

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
            var fileName = $"cat_{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(uploadDir, fileName);

            await using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);

            return $"/{ImageFolder}/{fileName}";
        }

        private void DeleteImageIfExists(string? imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl)) return;
            try
            {
                var fullPath = Path.Combine(_env.WebRootPath, imageUrl.TrimStart('/'));
                if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không thể xóa ảnh: {Url}", imageUrl);
            }
        }
    }
}
