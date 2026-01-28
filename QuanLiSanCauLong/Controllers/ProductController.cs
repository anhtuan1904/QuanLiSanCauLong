using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiSanCauLong.Data;
using QuanLiSanCauLong.Models;
using QuanLiSanCauLong.ViewModels;

namespace QuanLiSanCauLong.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Product/Index (Trang quản lý sản phẩm cho Admin)
        [HttpGet]
        public async Task<IActionResult> Index(int? categoryId, string search, string sortBy)
        {
            // 1. Khởi tạo truy vấn cơ bản
            var query = _context.Products
                .Include(p => p.Category)
                .AsQueryable();

            // 2. Bộ lọc tìm kiếm
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.ProductName.Contains(search) ||
                                         p.ProductCode.Contains(search) ||
                                         p.Description.Contains(search));
            }

            // 3. Bộ lọc theo danh mục
            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            // 4. Thực hiện Sắp xếp
            query = sortBy switch
            {
                "price_asc" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                "name_asc" => query.OrderBy(p => p.ProductName),
                "name_desc" => query.OrderByDescending(p => p.ProductName),
                _ => query.OrderByDescending(p => p.CreatedAt) // Mặc định mới nhất lên đầu
            };

            var products = await query.ToListAsync();

            // 5. Chuẩn bị dữ liệu Group theo Category (nếu view dùng kiểu hiển thị theo nhóm)
            var categoriesGroup = products
                .GroupBy(p => p.Category)
                .Where(g => g.Key != null)
                .Select(g => new ProductCategoryGroupViewModel
                {
                    CategoryId = g.Key.CategoryId,
                    CategoryName = g.Key.CategoryName,
                    CategoryType = g.Key.CategoryType,
                    Products = g.Select(p => new ProductCardViewModel
                    {
                        ProductId = p.ProductId,
                        ProductName = p.ProductName,
                        Price = p.Price,
                        Unit = p.Unit,
                        ImageUrl = p.ImageUrl ?? "/images/default-product.jpg",
                        CategoryName = g.Key.CategoryName,
                        IsAvailable = p.IsActive
                    }).ToList()
                }).ToList();

            // 6. Khởi tạo Model và gán đầy đủ thuộc tính (Tránh lỗi Null Model.Products)
            var model = new ProductListViewModel
            {
                SearchKeyword = search,
                Categories = categoriesGroup, // Dùng cho hiển thị kiểu Group
                Products = products,           // Dùng cho hiển thị kiểu Bảng (Fix lỗi Null)
                TotalProducts = products.Count
            };

            return View(model);
        }

        // GET: Product/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Inventories).ThenInclude(i => i.Facility)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null) return NotFound();

            var model = new ProductDetailsViewModel
            {
                ProductId = product.ProductId,
                ProductCode = product.ProductCode,
                ProductName = product.ProductName,
                Description = product.Description,
                Price = product.Price,
                Unit = product.Unit,
                ImageUrls = new List<string> { product.ImageUrl ?? "/images/default-product.jpg" },
                CategoryName = product.Category?.CategoryName,
                CategoryType = product.Category?.CategoryType,
                IsAvailable = product.IsActive,
                StockQuantity = product.Inventories?.Sum(i => i.Quantity) ?? 0
            };

            // Lấy 4 sản phẩm liên quan cùng danh mục
            model.RelatedProducts = await _context.Products
                .Where(p => p.CategoryId == product.CategoryId && p.ProductId != id && p.IsActive)
                .Take(4)
                .Select(p => new ProductCardViewModel
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    Price = p.Price,
                    ImageUrl = p.ImageUrl ?? "/images/default-product.jpg",
                    CategoryName = p.Category.CategoryName
                }).ToListAsync();

            return View(model);
        }

        // API: Kiểm tra tồn kho tại cơ sở cụ thể
        [HttpGet]
        public async Task<IActionResult> CheckAvailability(int productId, int facilityId)
        {
            var inventory = await _context.Inventories
                .FirstOrDefaultAsync(i => i.ProductId == productId && i.FacilityId == facilityId);

            return Json(new
            {
                available = (inventory != null && inventory.Quantity > 0),
                quantity = inventory?.Quantity ?? 0
            });
        }
    }
}