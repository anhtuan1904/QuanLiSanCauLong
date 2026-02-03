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

        public AdminCategoryController(ApplicationDbContext context, ILogger<AdminCategoryController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index()
        {
            var categories = await _context.ProductCategories
                .Include(c => c.Products)
                .OrderBy(c => c.CategoryName)
                .ToListAsync();
            return View("~/Views/Admin/AdminCategory/Index.cshtml", categories);
        }

        [HttpGet("Create")]
        public IActionResult Create()
        {
            return View("~/Views/Admin/AdminCategory/Create.cshtml");
        }

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductCategory model)
        {
            try
            {
                // Log model state để debug
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .Select(x => new
                        {
                            Field = x.Key,
                            Messages = x.Value.Errors.Select(e => e.ErrorMessage ?? e.Exception?.Message).ToList()
                        })
                        .ToList();

                    _logger.LogWarning("ModelState is invalid:");
                    foreach (var error in errors)
                    {
                        _logger.LogWarning($"Field: {error.Field}");
                        foreach (var msg in error.Messages)
                        {
                            _logger.LogWarning($"  - {msg}");
                        }
                    }

                    var errorMessages = string.Join("; ", errors.SelectMany(e => e.Messages));
                    TempData["ErrorMessage"] = $"Vui lòng kiểm tra lại thông tin: {errorMessages}";
                    return View("~/Views/Admin/AdminCategory/Create.cshtml", model);
                }

                // Kiểm tra các giá trị null/empty
                if (string.IsNullOrWhiteSpace(model.CategoryName))
                {
                    ModelState.AddModelError("CategoryName", "Tên danh mục không được để trống");
                    TempData["ErrorMessage"] = "Tên danh mục không được để trống!";
                    return View("~/Views/Admin/AdminCategory/Create.cshtml", model);
                }

                if (string.IsNullOrWhiteSpace(model.CategoryType))
                {
                    ModelState.AddModelError("CategoryType", "Loại danh mục không được để trống");
                    TempData["ErrorMessage"] = "Loại danh mục không được để trống!";
                    return View("~/Views/Admin/AdminCategory/Create.cshtml", model);
                }

                // Kiểm tra tên danh mục trùng
                var existingCategory = await _context.ProductCategories
                    .FirstOrDefaultAsync(c => c.CategoryName.ToLower().Trim() == model.CategoryName.ToLower().Trim());

                if (existingCategory != null)
                {
                    ModelState.AddModelError("CategoryName", "Tên danh mục đã tồn tại!");
                    TempData["ErrorMessage"] = "Tên danh mục đã tồn tại!";
                    return View("~/Views/Admin/AdminCategory/Create.cshtml", model);
                }

                // Trim dữ liệu
                model.CategoryName = model.CategoryName.Trim();
                model.CategoryType = model.CategoryType.Trim();
                if (!string.IsNullOrWhiteSpace(model.Description))
                {
                    model.Description = model.Description.Trim();
                }

                // Set thời gian
                model.CreatedAt = DateTime.Now;
                model.UpdatedAt = DateTime.Now;

                _logger.LogInformation($"Creating category: {model.CategoryName}, Type: {model.CategoryType}");

                _context.ProductCategories.Add(model);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Category created successfully with ID: {model.CategoryId}");

                TempData["SuccessMessage"] = "Tạo danh mục thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error while creating category");
                TempData["ErrorMessage"] = $"Lỗi cơ sở dữ liệu: {dbEx.InnerException?.Message ?? dbEx.Message}";
                return View("~/Views/Admin/AdminCategory/Create.cshtml", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while creating category");
                TempData["ErrorMessage"] = $"Có lỗi xảy ra: {ex.Message}";
                return View("~/Views/Admin/AdminCategory/Create.cshtml", model);
            }
        }

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

        [HttpPost("Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProductCategory model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    TempData["ErrorMessage"] = string.Join("; ", errors);
                    return View("~/Views/Admin/AdminCategory/Edit.cshtml", model);
                }

                var category = await _context.ProductCategories.FindAsync(model.CategoryId);
                if (category == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy danh mục!";
                    return RedirectToAction(nameof(Index));
                }

                // Kiểm tra tên danh mục trùng (ngoại trừ chính nó)
                var existingCategory = await _context.ProductCategories
                    .FirstOrDefaultAsync(c => c.CategoryName.ToLower().Trim() == model.CategoryName.ToLower().Trim()
                                            && c.CategoryId != model.CategoryId);

                if (existingCategory != null)
                {
                    ModelState.AddModelError("CategoryName", "Tên danh mục đã tồn tại!");
                    TempData["ErrorMessage"] = "Tên danh mục đã tồn tại!";
                    return View("~/Views/Admin/AdminCategory/Edit.cshtml", model);
                }

                category.CategoryName = model.CategoryName.Trim();
                category.CategoryType = model.CategoryType.Trim();
                category.Description = model.Description?.Trim();
                category.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Cập nhật danh mục thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while updating category");
                TempData["ErrorMessage"] = $"Có lỗi xảy ra: {ex.Message}";
                return View("~/Views/Admin/AdminCategory/Edit.cshtml", model);
            }
        }

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

        [HttpPost("Delete/{id}")]
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
                    return Json(new { success = false, message = "Không thể xóa danh mục có sản phẩm!" });

                _context.ProductCategories.Remove(category);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Xóa danh mục thành công!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while deleting category");
                return Json(new { success = false, message = $"Có lỗi xảy ra: {ex.Message}" });
            }
        }
    }
}