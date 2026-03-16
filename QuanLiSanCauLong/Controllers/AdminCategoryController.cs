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

        // ── INDEX ─────────────────────────────────────────────────
        [HttpGet(""), HttpGet("Index")]
        public async Task<IActionResult> Index()
        {
            var list = await _context.ProductCategories
                .Include(c => c.Products)
                .OrderBy(c => c.DisplayOrder).ThenBy(c => c.CategoryName)
                .ToListAsync();
            return View("~/Views/Admin/AdminCategory/Index.cshtml", list);
        }

        // ── DETAILS ───────────────────────────────────────────────
        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var cat = await _context.ProductCategories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (cat == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy danh mục!";
                return RedirectToAction(nameof(Index));
            }
            return View("~/Views/Admin/AdminCategory/Details.cshtml", cat);
        }

        // ── CREATE PARTIAL (modal — Index gọi /Admin/AdminCategory/CreatePartial) ──
        [HttpGet("CreatePartial")]
        public IActionResult CreatePartial()
        {
            return PartialView("~/Views/Admin/AdminCategory/_FormPartial.cshtml",
                new ProductCategory
                {
                    IsActive = true,
                    DefaultUnit = "Cái",
                    BehaviorType = "Retail",
                    DisplayOrder = 0
                });
        }

        // ── EDIT PARTIAL (modal) ──────────────────────────────────
        [HttpGet("EditPartial/{id}")]
        public async Task<IActionResult> EditPartial(int id)
        {
            var cat = await _context.ProductCategories.FindAsync(id);
            if (cat == null) return NotFound();
            return PartialView("~/Views/Admin/AdminCategory/_FormPartial.cshtml", cat);
        }

        // ── CREATE AJAX ───────────────────────────────────────────
        [HttpPost("CreateAjax")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAjax(ProductCategory model, IFormFile? CategoryImage)
        {
            var err = Validate(model);
            if (err != null) return Json(new { success = false, message = err });

            if (await NameExists(model.CategoryName))
                return Json(new { success = false, message = $"Tên \"{model.CategoryName}\" đã tồn tại!" });

            try
            {
                var cat = new ProductCategory { CreatedAt = DateTime.Now };
                ApplyFields(cat, model);
                if (CategoryImage?.Length > 0) cat.ImageUrl = await SaveImageAsync(CategoryImage);

                _context.ProductCategories.Add(cat);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Tạo danh mục ID={Id} Name={Name}", cat.CategoryId, cat.CategoryName);
                return Json(new { success = true, message = $"Tạo \"{cat.CategoryName}\" thành công!", categoryId = cat.CategoryId });
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "DB error CreateAjax");
                return Json(new { success = false, message = $"Lỗi DB: {dbEx.InnerException?.Message ?? dbEx.Message}" });
            }
            catch (InvalidOperationException ioEx)
            {
                return Json(new { success = false, message = ioEx.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateAjax failed");
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        // ── EDIT AJAX ─────────────────────────────────────────────
        [HttpPost("EditAjax/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAjax(int id, ProductCategory model, IFormFile? CategoryImage)
        {
            var err = Validate(model);
            if (err != null) return Json(new { success = false, message = err });

            var cat = await _context.ProductCategories.FindAsync(id);
            if (cat == null) return Json(new { success = false, message = "Không tìm thấy danh mục!" });

            if (await NameExists(model.CategoryName, id))
                return Json(new { success = false, message = $"Tên \"{model.CategoryName}\" đã tồn tại!" });

            try
            {
                ApplyFields(cat, model);
                if (CategoryImage?.Length > 0)
                {
                    DeleteImage(cat.ImageUrl);
                    cat.ImageUrl = await SaveImageAsync(CategoryImage);
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Cập nhật danh mục ID={Id}", id);
                return Json(new { success = true, message = $"Cập nhật \"{cat.CategoryName}\" thành công!" });
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "DB error EditAjax ID={Id}", id);
                return Json(new { success = false, message = $"Lỗi DB: {dbEx.InnerException?.Message ?? dbEx.Message}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EditAjax failed ID={Id}", id);
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        // ── DELETE ────────────────────────────────────────────────
        [HttpPost("Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var cat = await _context.ProductCategories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (cat == null) return Json(new { success = false, message = "Không tìm thấy!" });
            if (cat.Products?.Any() == true)
                return Json(new { success = false, message = $"Không thể xóa — đang có {cat.Products.Count} sản phẩm." });

            try
            {
                DeleteImage(cat.ImageUrl);
                _context.ProductCategories.Remove(cat);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = $"Đã xóa \"{cat.CategoryName}\"." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Delete failed ID={Id}", id);
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        // ── TOGGLE ACTIVE ─────────────────────────────────────────
        [HttpPost("ToggleActive/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var cat = await _context.ProductCategories.FindAsync(id);
            if (cat == null) return Json(new { success = false, message = "Không tìm thấy!" });

            cat.IsActive = !cat.IsActive;
            cat.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                isActive = cat.IsActive,
                message = cat.IsActive ? "Đã kích hoạt." : "Đã ẩn danh mục."
            });
        }

        // ── PRIVATE HELPERS ───────────────────────────────────────

        private static string? Validate(ProductCategory m)
        {
            if (string.IsNullOrWhiteSpace(m.CategoryName)) return "Tên danh mục không được để trống!";
            if (string.IsNullOrWhiteSpace(m.BehaviorType)) return "Vui lòng chọn loại nghiệp vụ!";
            return null;
        }

        private Task<bool> NameExists(string name, int excludeId = 0) =>
            _context.ProductCategories.AnyAsync(c =>
                c.CategoryName.ToLower().Trim() == name.ToLower().Trim() && c.CategoryId != excludeId);

        private static void ApplyFields(ProductCategory cat, ProductCategory m)
        {
            cat.CategoryName = m.CategoryName.Trim();
            cat.BehaviorType = m.BehaviorType.Trim();
            cat.Description = string.IsNullOrWhiteSpace(m.Description) ? null : m.Description.Trim();
            cat.DefaultUnit = string.IsNullOrWhiteSpace(m.DefaultUnit) ? "Cái" : m.DefaultUnit.Trim();
            cat.RequiresExpiry = m.RequiresExpiry;
            cat.HasVariants = m.HasVariants;
            cat.DefaultMinStock = m.DefaultMinStock;
            cat.AllowPartialReturn = m.AllowPartialReturn;
            cat.DepositRequired = m.DepositRequired;
            cat.DefaultDepositAmount = m.DefaultDepositAmount;
            cat.DisplayOrder = m.DisplayOrder;
            cat.IsActive = m.IsActive;
            cat.UpdatedAt = DateTime.Now;
        }

        private async Task<string> SaveImageAsync(IFormFile file)
        {
            var allowed = new[] { "image/jpeg", "image/png", "image/webp" };
            if (!allowed.Contains(file.ContentType.ToLower()))
                throw new InvalidOperationException("Chỉ chấp nhận JPG, PNG, WEBP.");
            if (file.Length > 5 * 1024 * 1024)
                throw new InvalidOperationException("Ảnh tối đa 5MB.");

            var dir = Path.Combine(_env.WebRootPath, ImageFolder);
            Directory.CreateDirectory(dir);
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var name = $"cat_{Guid.NewGuid():N}{ext}";
            await using var s = new FileStream(Path.Combine(dir, name), FileMode.Create);
            await file.CopyToAsync(s);
            return $"/{ImageFolder}/{name}";
        }

        private void DeleteImage(string? url)
        {
            if (string.IsNullOrEmpty(url)) return;
            try
            {
                var p = Path.Combine(_env.WebRootPath, url.TrimStart('/'));
                if (System.IO.File.Exists(p)) System.IO.File.Delete(p);
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Không xóa được ảnh {Url}", url); }
        }
    }
}
