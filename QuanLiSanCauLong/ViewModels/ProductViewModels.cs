using Microsoft.AspNetCore.Mvc;
using QuanLiSanCauLong.Models;
using System.ComponentModel.DataAnnotations;

namespace QuanLiSanCauLong.ViewModels
{
    /// <summary>
    /// ViewModel cho danh sách sản phẩm (khách hàng xem menu)
    /// </summary>
    public class ProductListViewModel
    {
        public List<Product> Products { get; set; }

        public int? FacilityId { get; set; }
        public string FacilityName { get; set; }
        public string CategoryType { get; set; }
        public string SearchKeyword { get; set; }
        public List<ProductCategoryGroupViewModel> Categories { get; set; }
        public int TotalProducts { get; set; }
    }

    /// <summary>
    /// ViewModel cho nhóm sản phẩm theo danh mục
    /// </summary>
    public class ProductCategoryGroupViewModel
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public string CategoryType { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
        public List<ProductCardViewModel> Products { get; set; }
        public int ProductCount => Products?.Count ?? 0;
    }

    /// <summary>
    /// ViewModel cho thẻ hiển thị sản phẩm
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
        public string CategoryName { get; set; }
        public bool IsAvailable { get; set; }
        public int StockQuantity { get; set; }
        public bool IsPopular { get; set; }
        public bool IsNew { get; set; }
        public decimal? DiscountPercent { get; set; }
        public decimal? DiscountedPrice { get; set; }
        public string PriceDisplay => Price.ToString("N0") + "đ";
    }

    /// <summary>
    /// ViewModel cho chi tiết sản phẩm
    /// </summary>
    public class ProductDetailsViewModel
    {
        public int ProductId { get; set; }
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string Unit { get; set; }
        public List<string> ImageUrls { get; set; }
        public string CategoryName { get; set; }
        public string CategoryType { get; set; }
        public bool IsAvailable { get; set; }
        public int StockQuantity { get; set; }

        // Thông tin bổ sung
        public string Ingredients { get; set; }
        public string NutritionalInfo { get; set; }
        public List<string> Tags { get; set; }

        // Sản phẩm liên quan
        public List<ProductCardViewModel> RelatedProducts { get; set; }
    }

    /// <summary>
    /// ViewModel cho quản lý sản phẩm (Admin)
    /// </summary>
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
        [Range(0, double.MaxValue, ErrorMessage = "Giá phải lớn hơn 0")]
        [Display(Name = "Giá")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Đơn vị không được để trống")]
        [Display(Name = "Đơn vị")]
        public string Unit { get; set; }

        [Display(Name = "Hình ảnh")]
        public List<string> ImageUrls { get; set; }

        [Display(Name = "Thành phần")]
        public string Ingredients { get; set; }

        [Display(Name = "Thông tin dinh dưỡng")]
        public string NutritionalInfo { get; set; }

        [Display(Name = "Hoạt động")]
        public bool IsActive { get; set; }

        [Display(Name = "Nổi bật")]
        public bool IsFeatured { get; set; }

        // Dropdown data
        public List<CategoryOptionViewModel> AvailableCategories { get; set; }
    }

    /// <summary>
    /// ViewModel cho option danh mục
    /// </summary>
    public class CategoryOptionViewModel
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public string CategoryType { get; set; }
    }

    /// <summary>
    /// ViewModel cho menu theo cơ sở
    /// </summary>
    public class FacilityMenuViewModel
    {
        public int FacilityId { get; set; }
        public string FacilityName { get; set; }
        public string FacilityAddress { get; set; }
        public string FacilityPhone { get; set; }

        // Menu items grouped by category
        public List<MenuCategoryViewModel> MenuCategories { get; set; }

        // Promotions
        public List<MenuPromotionViewModel> ActivePromotions { get; set; }
    }

    /// <summary>
    /// ViewModel cho danh mục trong menu
    /// </summary>
    public class MenuCategoryViewModel
    {
        public string CategoryName { get; set; }
        public string CategoryType { get; set; }
        public string Icon { get; set; }
        public List<MenuItemViewModel> Items { get; set; }
    }

    /// <summary>
    /// ViewModel cho món trong menu
    /// </summary>
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

    /// <summary>
    /// ViewModel cho khuyến mãi trong menu
    /// </summary>
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
