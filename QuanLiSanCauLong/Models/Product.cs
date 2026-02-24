using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLiSanCauLong.Models
{
    public class Product
    {
        [Key]
        public int ProductId { get; set; }

        // ===== 1. ĐỊNH DANH & THÔNG TIN CƠ BẢN =====
        [Required(ErrorMessage = "Tên sản phẩm không được để trống")]
        [StringLength(200)]
        public string ProductName { get; set; }

        [StringLength(50)]
        public string ProductCode { get; set; }   // Dùng trong Controller/View

        [StringLength(50)]
        public string SKU { get; set; }           // Giữ lại để tương thích

        [StringLength(100)]
        public string Barcode { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [StringLength(100)]
        public string Brand { get; set; }

        public string ImageUrl { get; set; }

        public string Metadata { get; set; }      // SEO / extra info

        // ===== 2. THUỘC TÍNH CHI TIẾT =====
        [StringLength(100)] public string Origin { get; set; }
        [StringLength(100)] public string Color { get; set; }
        [StringLength(50)] public string Size { get; set; }
        [StringLength(100)] public string Material { get; set; }
        public decimal? Weight { get; set; }
        [StringLength(5)] public string WeightUnit { get; set; } = "g";
        [StringLength(300)] public string ExtraAttributes { get; set; }
        public string TechnicalSpecs { get; set; }

        // ===== 3. TỒN KHO & VỊ TRÍ =====
        public int StockQuantity { get; set; } = 0;
        public int MinStockLevel { get; set; } = 0;

        [Required]
        [StringLength(30)]
        public string Unit { get; set; } = "Cái";

        [StringLength(100)]
        public string StorageLocation { get; set; }

        // ===== 4. LÔ HÀNG & HẠN SỬ DỤNG =====
        [StringLength(100)]
        public string BatchNumber { get; set; }

        [DataType(DataType.Date)]
        public DateTime? ImportDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? ExpiryDate { get; set; }

        // ===== 5. GIÁ TRỊ TÀI CHÍNH =====
        // Precision cấu hình trong DbContext - KHÔNG dùng [Column(TypeName)] ở đây
        public decimal CostPrice { get; set; } = 0;   // Giá nhập
        public decimal COGSPrice { get; set; } = 0;   // Giá vốn
        public decimal SalePrice { get; set; }         // Giá bán lẻ
        public decimal Price { get; set; }             // Alias SalePrice - dùng trong View/Controller

        // ===== 6. QUẢN LÝ TRẠNG THÁI =====
        [StringLength(20)]
        public string Status { get; set; } = "Active"; // Active|Inactive|Defective|Returned|Liquidating

        public bool IsActive { get; set; } = true;

        [StringLength(100)]
        public string DocumentReference { get; set; }

        // ===== 7. TIMESTAMPS =====
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // ===== 8. KHÓA NGOẠI =====
        public int? CategoryId { get; set; }
        [ForeignKey("CategoryId")]
        public virtual ProductCategory Category { get; set; }

        public int? SupplierId { get; set; }
        [ForeignKey("SupplierId")]
        public virtual Supplier Supplier { get; set; }

        // ===== 9. NAVIGATION PROPERTIES =====
        public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

        // ===== 10. COMPUTED (NOT MAPPED) =====
        [NotMapped]
        public bool IsLowStock => MinStockLevel > 0 && StockQuantity <= MinStockLevel;

        [NotMapped]
        public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value < DateTime.Now;

        [NotMapped]
        public bool IsExpiringSoon => ExpiryDate.HasValue
                                      && !IsExpired
                                      && ExpiryDate.Value <= DateTime.Now.AddDays(30);
    }
}