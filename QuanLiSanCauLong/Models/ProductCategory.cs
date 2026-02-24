// ===================================================================
// FILE: Models/ProductCategory.cs  (CẬP NHẬT)
// Thêm các thuộc tính mới để liên kết với Product & Inventory
// ===================================================================
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLiSanCauLong.Models
{
    public class ProductCategory
    {
        [Key]
        public int CategoryId { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Tên danh mục")]
        public string CategoryName { get; set; }

        /// <summary>Food | Beverage | Clothing | Equipment | Accessory | Other</summary>
        [Required]
        [StringLength(20)]
        [Display(Name = "Loại danh mục")]
        public string CategoryType { get; set; }

        [StringLength(500)]
        [Display(Name = "Mô tả")]
        public string Description { get; set; }

        [Display(Name = "Hình ảnh")]
        public string ImageUrl { get; set; }

        // ===== MỚI: Đơn vị mặc định của danh mục =====
        [StringLength(20)]
        [Display(Name = "Đơn vị mặc định")]
        public string DefaultUnit { get; set; } = "Cái";

        // ===== MỚI: Quy tắc quản lý kho =====

        /// <summary>Bật = sản phẩm trong danh mục này cần quản lý Hạn Sử Dụng</summary>
        [Display(Name = "Cần theo dõi HSD")]
        public bool RequiresExpiry { get; set; } = false;

        /// <summary>Bật = cần quản lý theo Số Lô (Batch/Lot)</summary>
        [Display(Name = "Quản lý theo lô")]
        public bool RequiresBatch { get; set; } = false;

        /// <summary>Bật = áp dụng nguyên tắc FIFO (Nhập trước - Xuất trước)</summary>
        [Display(Name = "Dùng FIFO")]
        public bool UseFIFO { get; set; } = false;

        /// <summary>Bật = sản phẩm có nhiều biến thể (màu, size)</summary>
        [Display(Name = "Có biến thể")]
        public bool HasVariants { get; set; } = false;

        // ===== MỚI: Ngưỡng kho mặc định =====
        [Display(Name = "Safety Stock mặc định")]
        public int DefaultMinStock { get; set; } = 0;

        [Display(Name = "Thứ tự hiển thị")]
        public int DisplayOrder { get; set; } = 0;
        // Thêm vào sau DisplayOrder, trước Navigation
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        // Navigation
        public virtual ICollection<Product> Products { get; set; }
    }
}


