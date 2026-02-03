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

        // 1. DANH SÁCH SẢN PHẨM
        public async Task<IActionResult> Index(int? categoryId, string search, string status)
        {
            var query = _context.Products.Include(p => p.Category).AsQueryable();

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

        // 2. CHI TIẾT SẢN PHẨM
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.ProductId == id);

            if (product == null) return NotFound();

            return View(product);
        }

        // 3. THÊM MỚI SẢN PHẨM (SỬA LẠI MODEL Ở ĐÂY)
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadCategoriesToViewBag();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product model, IFormFile? ImageFile)
        {
            // Bỏ qua các thuộc tính không nhập từ Form để tránh lỗi Validation
            ModelState.Remove("Category");
            ModelState.Remove("Inventories");
            ModelState.Remove("OrderDetails");
            ModelState.Remove("ImageUrl"); // Vì ImageUrl sẽ được gán sau khi lưu file

            if (ModelState.IsValid)
            {
                // Kiểm tra mã sản phẩm trùng
                if (await _context.Products.AnyAsync(p => p.ProductCode == model.ProductCode))
                {
                    ModelState.AddModelError("ProductCode", "Mã sản phẩm đã tồn tại!");
                }
                else
                {
                    // Xử lý lưu ảnh
                    if (ImageFile != null)
                    {
                        model.ImageUrl = await SaveImage(ImageFile);
                    }
                    else
                    {
                        // Bạn có thể gán ảnh mặc định nếu không chọn ảnh
                        model.ImageUrl = "/images/no-image.png";
                    }

                    model.CreatedAt = DateTime.Now;
                    model.UpdatedAt = DateTime.Now;
                    model.IsActive = true;

                    _context.Products.Add(model);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Thêm sản phẩm thành công!";
                    return RedirectToAction(nameof(Index));
                }
            }

            // Nếu dữ liệu không hợp lệ, nạp lại danh mục và trả về View
            await LoadCategoriesToViewBag();
            return View(model);
        }
        // 4. CHỈNH SỬA SẢN PHẨM
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            await LoadCategoriesToViewBag();
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product model, IFormFile? ImageFile)
        {
            if (id != model.ProductId) return NotFound();

            ModelState.Remove("Category");

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

        // 5. XÓA SẢN PHẨM
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products
                .Include(p => p.OrderDetails)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null) return Json(new { success = false, message = "Không tìm thấy!" });
            if (product.OrderDetails != null && product.OrderDetails.Any())
                return Json(new { success = false, message = "Sản phẩm đã có đơn hàng, không thể xóa!" });

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Xóa thành công!" });
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
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            return "/images/products/" + fileName;
        }

        private bool ProductExists(int id) => _context.Products.Any(e => e.ProductId == id);
    }
}