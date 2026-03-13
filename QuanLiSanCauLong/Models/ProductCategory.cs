// ===================================================================
// FILE: Models/ProductCategory.cs  (CẬP NHẬT v2)
// Thêm BehaviorType, Rental/Service logic, Pricing model
// ===================================================================
using System;
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

        // ─────────────────────────────────────────────────────────────
        // PHÂN LOẠI HÀNG HÓA (CategoryType) – dùng cho giao diện / icon
        // Food | Beverage | Clothing | Equipment | Accessory | Other
        // ─────────────────────────────────────────────────────────────
        [Required]
        [StringLength(20)]
        [Display(Name = "Loại hàng hoá")]
        public string CategoryType { get; set; }

        // ─────────────────────────────────────────────────────────────
        // HÀNH VI NGHIỆP VỤ (BehaviorType) – quyết định logic tính tiền & kho
        // Retail   = Bán đứt, trừ kho thẳng, không hoàn lại
        // Rental   = Cho thuê, xuất kho khi thuê – nhập lại khi trả
        // Service  = Dịch vụ kỹ thuật, không có kho vật lý
        // ─────────────────────────────────────────────────────────────
        [Required]
        [StringLength(10)]
        [Display(Name = "Hành vi nghiệp vụ")]
        public string BehaviorType { get; set; } = "Retail";

        [StringLength(500)]
        [Display(Name = "Mô tả")]
        public string Description { get; set; }

        [Display(Name = "Hình ảnh")]
        public string ImageUrl { get; set; }

        [StringLength(20)]
        [Display(Name = "Đơn vị mặc định")]
        public string DefaultUnit { get; set; } = "Cái";

        // ─────────────────────────────────────────────────────────────
        // QUY TẮC QUẢN LÝ KHO  (chỉ áp dụng cho Retail & Rental)
        // ─────────────────────────────────────────────────────────────

        /// <summary>Bật = sản phẩm cần quản lý Hạn Sử Dụng</summary>
        [Display(Name = "Theo dõi HSD")]
        public bool RequiresExpiry { get; set; } = false;

        /// <summary>Bật = quản lý theo Số Lô (Batch/Lot)</summary>
        [Display(Name = "Quản lý theo lô")]
        public bool RequiresBatch { get; set; } = false;

        /// <summary>Bật = áp dụng FIFO (Nhập trước – Xuất trước)</summary>
        [Display(Name = "Dùng FIFO")]
        public bool UseFIFO { get; set; } = false;

        /// <summary>Bật = sản phẩm có nhiều biến thể (màu, size)</summary>
        [Display(Name = "Có biến thể")]
        public bool HasVariants { get; set; } = false;

        /// <summary>Bật = quản lý theo Size (giày cho thuê, quần áo)</summary>
        [Display(Name = "Quản lý theo Size")]
        public bool RequiresSize { get; set; } = false;

        [Display(Name = "Safety Stock mặc định")]
        public int DefaultMinStock { get; set; } = 0;

        // ─────────────────────────────────────────────────────────────
        // CẤU HÌNH RENTAL (BehaviorType = "Rental")
        // ─────────────────────────────────────────────────────────────

        /// <summary>Cho phép trả từng phần (VD: thuê 3 vợt, trả 1 trước)</summary>
        [Display(Name = "Cho phép trả từng phần")]
        public bool AllowPartialReturn { get; set; } = false;

        /// <summary>Yêu cầu đặt cọc khi thuê</summary>
        [Display(Name = "Yêu cầu đặt cọc")]
        public bool DepositRequired { get; set; } = false;

        /// <summary>Tiền đặt cọc mặc định (VNĐ)</summary>
        [Column(TypeName = "decimal(18,0)")]
        [Display(Name = "Tiền đặt cọc mặc định")]
        public decimal DefaultDepositAmount { get; set; } = 0;

        /// <summary>Phí vệ sinh/giặt ủi mặc định khi trả đồ (VNĐ)</summary>
        [Column(TypeName = "decimal(18,0)")]
        [Display(Name = "Phí vệ sinh mặc định")]
        public decimal DefaultCleaningFee { get; set; } = 0;

        /// <summary>Số giờ thuê tối đa (0 = không giới hạn)</summary>
        [Display(Name = "Giờ thuê tối đa")]
        public int MaxRentalHours { get; set; } = 0;

        /// <summary>Tính phí theo giờ vượt (phụ thu overtime)</summary>
        [Display(Name = "Tính phí overtime")]
        public bool ChargeOvertime { get; set; } = false;

        // ─────────────────────────────────────────────────────────────
        // CẤU HÌNH SERVICE (BehaviorType = "Service")
        // ─────────────────────────────────────────────────────────────

        /// <summary>Mô hình tính giá: Fixed | PerUnit | Labor+Material | Bundle</summary>
        [StringLength(20)]
        [Display(Name = "Mô hình giá")]
        public string PricingModel { get; set; } = "Fixed";

        /// <summary>Bật = tách riêng tiền công và tiền vật liệu (căng vợt: công + cước)</summary>
        [Display(Name = "Tách tiền công & vật liệu")]
        public bool SeparateLaborAndMaterial { get; set; } = false;

        /// <summary>Tên đơn vị vật liệu (VD: "bộ cước", "đôi đế")</summary>
        [StringLength(30)]
        [Display(Name = "Đơn vị vật liệu")]
        public string MaterialUnit { get; set; }

        /// <summary>Bật = cho phép khách tự mang vật liệu (căng vợt bằng cước khách)</summary>
        [Display(Name = "Khách tự mang vật liệu")]
        public bool AllowCustomerMaterial { get; set; } = false;

        // ─────────────────────────────────────────────────────────────
        // HIỂN THỊ & AUDIT
        // ─────────────────────────────────────────────────────────────
        [Display(Name = "Thứ tự hiển thị")]
        public int DisplayOrder { get; set; } = 0;

        [Display(Name = "Trạng thái")]
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Navigation
        public virtual ICollection<Product> Products { get; set; }
    }
}
