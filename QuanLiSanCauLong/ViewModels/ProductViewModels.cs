using QuanLiSanCauLong.Models;
using System.ComponentModel.DataAnnotations;

namespace QuanLiSanCauLong.ViewModels
{
    // ══════════════════════════════════════════════════════════════
    //  SHARED / UTILITY
    // ══════════════════════════════════════════════════════════════

    public class ProductCategoryViewModel
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public string CategoryType { get; set; }
        public List<ProductItemViewModel> Products { get; set; } = new();
    }

    /// <summary>Item nhỏ nhất — dùng trong ProductCategoryViewModel</summary>
/*    public class ProductItemViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public string ImageUrl { get; set; }
        public bool IsAvailable { get; set; }
    }*/

    public class CategoryOptionViewModel
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public string CategoryType { get; set; }
    }

    // ══════════════════════════════════════════════════════════════
    //  PRODUCT LIST (trang cửa hàng Index)
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// ViewModel cho trang danh sách sản phẩm — người dùng xem cửa hàng.
    /// Hỗ trợ phân trang, lọc, sắp xếp.
    /// </summary>
    public class ProductListViewModel
    {
        // Danh sách sản phẩm trên trang hiện tại
        public List<Product> Products { get; set; } = new();

        // Metadata cửa hàng / cơ sở
        public int? FacilityId { get; set; }
        public string FacilityName { get; set; }

        // Filter state
        public string CategoryType { get; set; }
        public string SearchKeyword { get; set; }
        public string CurrentSort { get; set; } = "newest";
        public int? MinPrice { get; set; }
        public int? MaxPrice { get; set; }

        // Nhóm danh mục (sidebar)
        public List<ProductCategoryGroupViewModel> Categories { get; set; } = new();

        // Tổng và phân trang
        public int TotalCount { get; set; }
        public int TotalProducts => TotalCount;   // alias tương thích cũ
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 12;
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }

    /// <summary>Nhóm sản phẩm theo danh mục — dùng trong sidebar và nav pills</summary>
    public class ProductCategoryGroupViewModel
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = "";

        // ✅ FIX: CategoryType đã xóa — dùng BehaviorType: "Retail" | "Rental" | "Service"
        // Nếu view cũ đang dùng .CategoryType thì đổi sang .BehaviorType
        public string BehaviorType { get; set; } = "Retail";

        public List<ProductCardViewModel> Products { get; set; } = new();
    }
    /// <summary>
    /// ViewModel cho thẻ sản phẩm (card) trong grid / list.
    /// </summary>
    public class ProductCardViewModel
    {
        public int ProductId { get; set; }
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string Unit { get; set; }
        public string ImageUrl { get; set; }
        public int? CategoryId { get; set; }
        public string CategoryName { get; set; }

        // Tồn kho
        public bool IsAvailable { get; set; }
        public int StockQuantity { get; set; }
        public int MinStockLevel { get; set; }

        // Badge flags
        public bool IsPopular { get; set; }
        public bool IsNew { get; set; }

        // Giá khuyến mãi
        public decimal? DiscountPercent { get; set; }
        public decimal? DiscountedPrice { get; set; }
        public decimal? DiscountPrice => DiscountedPrice;

        // Behavior (Retail / Rental / Service)
        public string BehaviorType { get; set; } = "Retail";

        // Computed
        public string PriceDisplay => Price.ToString("N0") + "đ";
        public bool HasDiscount => DiscountPercent.HasValue && DiscountPercent > 0;
        public string DiscountedPriceDisplay => DiscountedPrice?.ToString("N0") + "đ";
    }

    // ══════════════════════════════════════════════════════════════
    //  PRODUCT DETAILS (trang chi tiết)
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// ViewModel đầy đủ cho trang chi tiết sản phẩm.
    /// Bao gồm tất cả field mà Details.cshtml sử dụng.
    /// </summary>
    public class ProductDetailsViewModel
    {
        // ── 1. Định danh ──────────────────────────────────────────
        public int ProductId { get; set; }
        public string ProductCode { get; set; }
        public string SKU { get; set; }
        public string Barcode { get; set; }
        public string ProductName { get; set; }

        // ── 2. Danh mục & hành vi ──────────────────────────────────
        public int? CategoryId { get; set; }
        public string CategoryName { get; set; }
        public string CategoryType { get; set; }

        /// <summary>"Retail" | "Rental" | "Service"</summary>
        public string BehaviorType { get; set; } = "Retail";

        // ── 3. Mô tả & thông số ────────────────────────────────────
        public string Description { get; set; }
        public string TechnicalSpecs { get; set; }

        // ── 4. Thuộc tính vật lý ───────────────────────────────────
        public string Brand { get; set; }
        public string Origin { get; set; }
        public string Color { get; set; }
        public string Size { get; set; }
        public string Material { get; set; }
        public decimal? Weight { get; set; }
        public string WeightUnit { get; set; } = "g";

        // ── 5. Giá ─────────────────────────────────────────────────
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public string Unit { get; set; } = "Cái";

        // ── 6. Tồn kho ─────────────────────────────────────────────
        public bool IsAvailable { get; set; }
        public int StockQuantity { get; set; }
        public int MinStockLevel { get; set; }

        // ── 7. Ảnh ─────────────────────────────────────────────────
        public List<string> ImageUrls { get; set; } = new();

        // ── 8. Rental ──────────────────────────────────────────────
        /// <summary>Tiền đặt cọc khi thuê. Chỉ dùng khi BehaviorType == "Rental".</summary>
        public decimal DepositAmount { get; set; } = 0;

        /// <summary>Phí vệ sinh khi trả đồ. Chỉ dùng khi BehaviorType == "Rental".</summary>
        public decimal CleaningFee { get; set; } = 0;

        /// <summary>Thời gian thuê tối đa (giờ). Null = không giới hạn.</summary>
        public int? MaxRentalHours { get; set; }

        // ── 9. Service ─────────────────────────────────────────────
        /// <summary>Đơn vị tính công (bộ, chiếc…). Chỉ dùng khi BehaviorType == "Service".</summary>
        public string LaborUnit { get; set; }

        /// <summary>Tiền công / đơn vị. Chỉ dùng khi BehaviorType == "Service".</summary>
        public decimal LaborPrice { get; set; } = 0;

        /// <summary>Giá vật tư / đơn vị. = 0 nếu khách tự mang.</summary>
        public decimal MaterialPrice { get; set; } = 0;

        // ── 10. Variants ───────────────────────────────────────────
        public List<ProductVariant> Variants { get; set; } = new();

        // ── 11. Thông tin thêm (legacy) ────────────────────────────
        public string Ingredients { get; set; }
        public string NutritionalInfo { get; set; }
        public List<string> Tags { get; set; } = new();
        public DateTime? ExpiryDate { get; set; }

        // ── 12. Sản phẩm liên quan ─────────────────────────────────
        public List<ProductCardViewModel> RelatedProducts { get; set; } = new();

        // ── 13. Computed ───────────────────────────────────────────
        public bool HasDiscount => DiscountPrice.HasValue && DiscountPrice < Price;

        public int? DiscountPercent => HasDiscount
            ? (int)Math.Round((1 - (double)DiscountPrice!.Value / (double)Price) * 100)
            : null;

        public bool IsLowStock => MinStockLevel > 0
                                  && StockQuantity <= MinStockLevel
                                  && StockQuantity > 0;

        public decimal ServiceTotalPerUnit => LaborPrice + MaterialPrice;
    }

    // ══════════════════════════════════════════════════════════════
    //  ADMIN
    // ══════════════════════════════════════════════════════════════

    /// <summary>ViewModel cho quản lý sản phẩm (Admin CRUD)</summary>
    public class ProductManageViewModel
    {
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Mã sản phẩm không được để trống")]
        [Display(Name = "Mã sản phẩm")]
        public string ProductCode { get; set; }

        [Required(ErrorMessage = "Tên sản phẩm không được để trống")]
        [Display(Name = "Tên sản phẩm")]
        public string ProductName { get; set; }

        [Display(Name = "Mô tả")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Danh mục không được để trống")]
        [Display(Name = "Danh mục")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Giá không được để trống")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá phải >= 0")]
        [Display(Name = "Giá")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Đơn vị không được để trống")]
        [Display(Name = "Đơn vị")]
        public string Unit { get; set; }

        [Display(Name = "Hình ảnh")]
        public List<string> ImageUrls { get; set; } = new();

        [Display(Name = "Thành phần")]
        public string Ingredients { get; set; }

        [Display(Name = "Thông tin dinh dưỡng")]
        public string NutritionalInfo { get; set; }

        [Display(Name = "Hoạt động")]
        public bool IsActive { get; set; }

        [Display(Name = "Nổi bật")]
        public bool IsFeatured { get; set; }

        // Dropdown data
        public List<CategoryOptionViewModel> AvailableCategories { get; set; } = new();
    }

    // ══════════════════════════════════════════════════════════════
    //  MENU (legacy — giữ lại tương thích)
    // ══════════════════════════════════════════════════════════════

    public class MenuCategoryViewModel
    {
        public string CategoryName { get; set; }
        public string CategoryType { get; set; }
        public string Icon { get; set; }
        public List<MenuItemViewModel> Items { get; set; } = new();
    }

    public class MenuItemViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string Unit { get; set; }
        public string ImageUrl { get; set; }
        public bool IsAvailable { get; set; }
        public bool IsPopular { get; set; }
        public bool IsSpicy { get; set; }
        public bool IsVegetarian { get; set; }
        public string Allergens { get; set; }
    }

    public class MenuPromotionViewModel
    {
        public string PromotionName { get; set; }
        public string Description { get; set; }
        public decimal DiscountPercent { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string ImageUrl { get; set; }
    }
}
