using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace QuanLiSanCauLong.ViewModels
{
    /// <summary>
    /// ViewModel cho danh sách danh mục
    /// </summary>
    public class CategoryListViewModel
    {
        public List<CategoryItemViewModel> Categories { get; set; }
        public string FilterType { get; set; }
        public int TotalCount { get; set; }
    }

    /// <summary>
    /// ViewModel cho từng danh mục trong danh sách
    /// </summary>
    public class CategoryItemViewModel
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public string CategoryType { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
        public int ProductCount { get; set; }
        public bool IsActive { get; set; }
        public int DisplayOrder { get; set; }
    }

    /// <summary>
    /// ViewModel cho quản lý danh mục (Admin)
    /// </summary>
    public class CategoryManageViewModel
    {
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Tên danh mục không được để trống")]
        [Display(Name = "Tên danh mục")]
        public string CategoryName { get; set; }

        [Required(ErrorMessage = "Loại danh mục không được để trống")]
        [Display(Name = "Loại danh mục")]
        public string CategoryType { get; set; } // Food, Beverage, Equipment, Accessory

        [Display(Name = "Mô tả")]
        public string Description { get; set; }

        [Display(Name = "Icon")]
        public string Icon { get; set; }

        [Display(Name = "Thứ tự hiển thị")]
        public int DisplayOrder { get; set; }

        [Display(Name = "Hoạt động")]
        public bool IsActive { get; set; }

        // Dropdown options
        public List<CategoryTypeOption> CategoryTypeOptions { get; set; }
    }

    /// <summary>
    /// ViewModel cho option loại danh mục
    /// </summary>
    public class CategoryTypeOption
    {
        public string Value { get; set; }
        public string Text { get; set; }
    }
}
