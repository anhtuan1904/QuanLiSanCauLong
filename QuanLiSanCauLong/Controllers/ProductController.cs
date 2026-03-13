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

        // GET: Product/Index
        [HttpGet]
        public async Task<IActionResult> Index(int? categoryId, string search, string sortBy,
                                               int page = 1, int pageSize = 12)
        {
            // 1. Truy vấn cơ bản
            var query = _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive)
                .AsQueryable();

            // 2. Tìm kiếm
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.ProductName.Contains(search) ||
                                         p.ProductCode.Contains(search) ||
                                         p.Description.Contains(search));
            }

            // 3. Lọc danh mục
            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            // 4. Sắp xếp
            query = sortBy switch
            {
                "price_asc" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                "name_asc" => query.OrderBy(p => p.ProductName),
                "name_desc" => query.OrderByDescending(p => p.ProductName),
                "popular" => query.OrderByDescending(p => p.StockQuantity),
                _ => query.OrderByDescending(p => p.CreatedAt)
            };

            // 5. Tổng số & phân trang
            var totalCount = await query.CountAsync();
            var products = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // 6. Nhóm theo danh mục cho sidebar
            var allProducts = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive)
                .ToListAsync();

            var categoriesGroup = allProducts
                .GroupBy(p => p.Category)
                .Where(g => g.Key != null)
                .Select(g => new ProductCategoryGroupViewModel
                {
                    CategoryId = g.Key!.CategoryId,
                    CategoryName = g.Key.CategoryName,
                    CategoryType = g.Key.CategoryType,
                    Products = g.Select(p => new ProductCardViewModel
                    {
                        ProductId = p.ProductId,
                        ProductName = p.ProductName,
                        Price = p.Price,
                        Unit = p.Unit,
                        ImageUrl = p.ImageUrl ?? "/images/default-product.jpg",
                        CategoryId = p.CategoryId,
                        CategoryName = g.Key.CategoryName,
                        IsAvailable = p.IsActive,
                        StockQuantity = p.StockQuantity,
                        MinStockLevel = p.MinStockLevel,
                        BehaviorType = p.Category?.BehaviorType ?? "Retail",
                        IsNew = p.CreatedAt >= DateTime.Now.AddDays(-14)
                    }).ToList()
                }).ToList();

            // 7. Gán model
            var model = new ProductListViewModel
            {
                SearchKeyword = search,
                CurrentSort = sortBy ?? "newest",
                Categories = categoriesGroup,
                Products = products,
                TotalCount = totalCount,   // <-- dùng TotalCount, không phải TotalProducts
                CurrentPage = page,
                PageSize = pageSize
            };

            return View(model);
        }

        // GET: Product/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Variants)
                .Include(p => p.Inventories).ThenInclude(i => i.Facility)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null) return NotFound();

            var stockQty = product.StockQuantity > 0
                ? product.StockQuantity
                : product.Inventories?.Sum(i => i.Quantity) ?? 0;

            var imageUrls = new List<string>();
            if (!string.IsNullOrEmpty(product.ImageUrl))
                imageUrls.Add(product.ImageUrl);
            if (imageUrls.Count == 0)
                imageUrls.Add("/images/default-product.jpg");

            var model = new ProductDetailsViewModel
            {
                ProductId = product.ProductId,
                ProductCode = product.ProductCode,
                SKU = product.SKU,
                Barcode = product.Barcode,
                ProductName = product.ProductName,
                Description = product.Description,
                TechnicalSpecs = product.TechnicalSpecs,
                Brand = product.Brand,
                Origin = product.Origin,
                Color = product.Color,
                Size = product.Size,
                Material = product.Material,
                Weight = product.Weight,
                WeightUnit = product.WeightUnit ?? "g",
                Price = product.Price,
                Unit = product.Unit ?? "Cái",
                ImageUrls = imageUrls,
                CategoryId = product.CategoryId,
                CategoryName = product.Category?.CategoryName,
                CategoryType = product.Category?.CategoryType,
                BehaviorType = product.Category?.BehaviorType ?? "Retail",
                IsAvailable = product.IsActive && stockQty > 0,
                StockQuantity = stockQty,
                MinStockLevel = product.MinStockLevel,
                ExpiryDate = product.ExpiryDate,
                Variants = product.Variants?.ToList() ?? new(),

                // Rental
                DepositAmount = product.DepositAmount,
                CleaningFee = product.CleaningFee,
                MaxRentalHours = product.MaxRentalHours,

                // Service
                LaborUnit = product.LaborUnit,
                LaborPrice = product.LaborPrice,
                MaterialPrice = product.MaterialPrice,
            };

            // Sản phẩm liên quan cùng danh mục
            model.RelatedProducts = await _context.Products
                .Where(p => p.CategoryId == product.CategoryId
                         && p.ProductId != id
                         && p.IsActive)
                .Take(4)
                .Select(p => new ProductCardViewModel
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    Price = p.Price,
                    Unit = p.Unit,
                    ImageUrl = p.ImageUrl ?? "/images/default-product.jpg",
                    CategoryName = p.Category!.CategoryName,
                    IsAvailable = p.IsActive,
                    StockQuantity = p.StockQuantity,
                    BehaviorType = p.Category.BehaviorType ?? "Retail"
                }).ToListAsync();

            return View(model);
        }

        // API: Kiểm tra tồn kho tại cơ sở
        [HttpGet]
        public async Task<IActionResult> CheckAvailability(int productId, int facilityId)
        {
            var inventory = await _context.Inventories
                .FirstOrDefaultAsync(i => i.ProductId == productId && i.FacilityId == facilityId);

            return Json(new
            {
                available = inventory != null && inventory.Quantity > 0,
                quantity = inventory?.Quantity ?? 0
            });
        }
    }
}
