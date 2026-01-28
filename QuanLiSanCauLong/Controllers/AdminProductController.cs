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

        [HttpGet]
        public async Task<IActionResult> Index(int? categoryId, string search, string status)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .AsQueryable();

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(p => p.ProductName.Contains(search) || p.ProductCode.Contains(search));

            if (!string.IsNullOrEmpty(status))
                query = query.Where(p => p.Status == status);

            var products = await query.OrderBy(p => p.ProductName).ToListAsync();

            ViewBag.Categories = await _context.ProductCategories.ToListAsync();
            return View(products);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = await _context.ProductCategories.ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product model)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra mã sản phẩm trùng
                if (await _context.Products.AnyAsync(p => p.ProductCode == model.ProductCode))
                {
                    ModelState.AddModelError("ProductCode", "Mã sản phẩm đã tồn tại!");
                    ViewBag.Categories = await _context.ProductCategories.ToListAsync();
                    return View(model);
                }

                model.CreatedAt = DateTime.Now;
                model.UpdatedAt = DateTime.Now;
                model.IsActive = true;

                _context.Products.Add(model);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Tạo sản phẩm thành công!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = await _context.ProductCategories.ToListAsync();
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Inventories)
                    .ThenInclude(i => i.Facility)
                .Include(p => p.OrderDetails)
                    .ThenInclude(od => od.Order)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
                return NotFound();

            return View(product);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound();

            ViewBag.Categories = await _context.ProductCategories.ToListAsync();
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Product model)
        {
            if (ModelState.IsValid)
            {
                var product = await _context.Products.FindAsync(model.ProductId);
                if (product != null)
                {
                    // Kiểm tra mã sản phẩm trùng (ngoại trừ chính nó)
                    if (await _context.Products.AnyAsync(p => p.ProductCode == model.ProductCode && p.ProductId != model.ProductId))
                    {
                        ModelState.AddModelError("ProductCode", "Mã sản phẩm đã tồn tại!");
                        ViewBag.Categories = await _context.ProductCategories.ToListAsync();
                        return View(model);
                    }

                    product.CategoryId = model.CategoryId;
                    product.ProductCode = model.ProductCode;
                    product.ProductName = model.ProductName;
                    product.Description = model.Description;
                    product.Price = model.Price;
                    product.Unit = model.Unit;
                    product.ImageUrl = model.ImageUrl;
                    product.Status = model.Status;
                    product.IsActive = model.IsActive;
                    product.UpdatedAt = DateTime.Now;

                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Cập nhật sản phẩm thành công!";
                    return RedirectToAction(nameof(Index));
                }
            }

            ViewBag.Categories = await _context.ProductCategories.ToListAsync();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
                return Json(new { success = false, message = "ID không hợp lệ!" });

            var product = await _context.Products
                .Include(p => p.OrderDetails)
                .Include(p => p.Inventories)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
                return Json(new { success = false, message = "Không tìm thấy sản phẩm!" });

            if (product.OrderDetails.Any())
                return Json(new { success = false, message = "Không thể xóa sản phẩm đã có trong đơn hàng!" });

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Xóa sản phẩm thành công!" });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int productId, bool isActive)
        {
            if (productId <= 0)
                return Json(new { success = false, message = "ID không hợp lệ!" });

            var product = await _context.Products.FindAsync(productId);
            if (product == null)
                return Json(new { success = false, message = "Không tìm thấy sản phẩm!" });

            product.IsActive = isActive;
            product.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = isActive ? "Kích hoạt sản phẩm thành công!" : "Vô hiệu hóa sản phẩm thành công!"
            });
        }
    }
}