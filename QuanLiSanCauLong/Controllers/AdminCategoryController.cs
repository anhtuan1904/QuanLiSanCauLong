using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiSanCauLong.Data;
using QuanLiSanCauLong.Models;

namespace QuanLiSanCauLong.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminCategoryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminCategoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var categories = await _context.ProductCategories
                .Include(c => c.Products)
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            return View(categories);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductCategory model)
        {
            if (ModelState.IsValid)
            {
                _context.ProductCategories.Add(model);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Tạo danh mục thành công!";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _context.ProductCategories.FindAsync(id);
            if (category == null)
                return NotFound();

            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProductCategory model)
        {
            if (ModelState.IsValid)
            {
                var category = await _context.ProductCategories.FindAsync(model.CategoryId);
                if (category != null)
                {
                    category.CategoryName = model.CategoryName;
                    category.CategoryType = model.CategoryType;
                    category.Description = model.Description;

                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Cập nhật danh mục thành công!";
                    return RedirectToAction(nameof(Index));
                }
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
                return Json(new { success = false, message = "ID không hợp lệ!" });

            var category = await _context.ProductCategories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
                return Json(new { success = false, message = "Không tìm thấy danh mục!" });

            if (category.Products.Any())
                return Json(new { success = false, message = "Không thể xóa danh mục có sản phẩm!" });

            _context.ProductCategories.Remove(category);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Xóa danh mục thành công!" });
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var category = await _context.ProductCategories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
                return NotFound();

            return View(category);
        }
    }
}