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
        public string ProductCode { get; set; }

        [StringLength(50)]
        public string SKU { get; set; }

        [StringLength(100)]
        public string Barcode { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [StringLength(100)]
        public string Brand { get; set; }

        public string ImageUrl { get; set; }

        public string Metadata { get; set; }

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
        /// <summary>
        /// Tổng tồn kho (tổng các Variant.StockQuantity).
        /// Cập nhật bằng cách gọi SyncStockFromVariants() sau mỗi thay đổi variant.
        /// Với nhóm Service: luôn = 0 (không quản lý kho vật lý).
        /// </summary>
        public int StockQuantity { get; set; } = 0;

        /// <summary>
        /// Tổng số lượng đang treo trên toàn sản phẩm.
        /// Với nhóm Rental: đây là số lượng đang được khách thuê chưa trả.
        /// </summary>
        public int ReservedQuantity { get; set; } = 0;

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
        public decimal CostPrice { get; set; } = 0;
        public decimal COGSPrice { get; set; } = 0;
        public decimal SalePrice { get; set; } = 0;

        /// <summary>
        /// Giá niêm yết:
        /// - Retail  → giá bán lẻ
        /// - Rental  → giá thuê mỗi lần / giờ
        /// - Service → giá trọn gói (nếu có), hoặc 0 nếu tính theo LaborPrice + MaterialPrice
        /// </summary>
        public decimal Price { get; set; } = 0;

        // ===== 6. RENTAL — Cho thuê =====
        /// <summary>
        /// Tiền đặt cọc khi thuê đồ. Mặc định kế thừa từ ProductCategory.DefaultDepositAmount.
        /// Chỉ có ý nghĩa khi Category.BehaviorType == "Rental".
        /// </summary>
        public decimal DepositAmount { get; set; } = 0;

        /// <summary>
        /// Phí vệ sinh / giặt ủi thu khi khách trả đồ.
        /// Chỉ có ý nghĩa khi Category.BehaviorType == "Rental".
        /// </summary>
        public decimal CleaningFee { get; set; } = 0;

        /// <summary>
        /// Thời gian thuê tối đa (giờ). Null = không giới hạn.
        /// Chỉ có ý nghĩa khi Category.BehaviorType == "Rental".
        /// </summary>
        public int? MaxRentalHours { get; set; }

        /// <summary>
        /// Sản phẩm yêu cầu đặt cọc khi thuê.
        /// Kế thừa từ ProductCategory.DepositRequired nhưng có thể override theo từng sản phẩm.
        /// </summary>
        public bool RequiresDeposit { get; set; } = false;

        // ===== 7. SERVICE — Dịch vụ kỹ thuật =====
        /// <summary>
        /// Đơn vị tính công (ví dụ: "bộ", "chiếc", "cặp", "m").
        /// Chỉ có ý nghĩa khi Category.BehaviorType == "Service".
        /// </summary>
        [StringLength(30)]
        public string LaborUnit { get; set; }

        /// <summary>
        /// Tiền công / đơn vị (ví dụ: 50.000đ / bộ căng vợt).
        /// Chỉ có ý nghĩa khi Category.BehaviorType == "Service".
        /// </summary>
        public decimal LaborPrice { get; set; } = 0;

        /// <summary>
        /// Giá vật tư / đơn vị (ví dụ: giá cước theo bộ, giá keo dán đế).
        /// = 0 nếu khách tự mang vật tư.
        /// Chỉ có ý nghĩa khi Category.BehaviorType == "Service".
        /// </summary>
        public decimal MaterialPrice { get; set; } = 0;

        // ===== 8. QUẢN LÝ TRẠNG THÁI =====
        [StringLength(20)]
        public string Status { get; set; } = "Active";

        public bool IsActive { get; set; } = true;

        [StringLength(100)]
        public string DocumentReference { get; set; }

        // ===== 9. TIMESTAMPS =====
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // ===== 10. KHÓA NGOẠI =====
        public int? CategoryId { get; set; }
        [ForeignKey("CategoryId")]
        public virtual ProductCategory Category { get; set; }

        public int? SupplierId { get; set; }
        [ForeignKey("SupplierId")]
        public virtual Supplier Supplier { get; set; }

        // ===== 11. NAVIGATION PROPERTIES =====
        public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
        public virtual ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();

        // ===== 12. COMPUTED (NOT MAPPED) =====
        [NotMapped]
        public int AvailableQuantity => StockQuantity - ReservedQuantity;

        [NotMapped]
        public bool IsLowStock => MinStockLevel > 0 && AvailableQuantity <= MinStockLevel;

        [NotMapped]
        public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value < DateTime.Now;

        [NotMapped]
        public bool IsExpiringSoon => ExpiryDate.HasValue
                                      && !IsExpired
                                      && ExpiryDate.Value <= DateTime.Now.AddDays(30);

        /// <summary>
        /// Tổng doanh thu dịch vụ ước tính mỗi đơn vị (công + vật tư).
        /// Chỉ có ý nghĩa khi Category.BehaviorType == "Service".
        /// </summary>
        [NotMapped]
        public decimal ServiceTotalPerUnit => LaborPrice + MaterialPrice;

        /// <summary>
        /// Đồng bộ StockQuantity và ReservedQuantity từ tổng các Variants active.
        /// Gọi sau mỗi thao tác nhập / xuất / điều chỉnh variant.
        /// Không gọi với nhóm Service (không có kho vật lý).
        /// </summary>
        public void SyncStockFromVariants()
        {
            if (Variants != null && Variants.Any(v => v.IsActive))
            {
                var active = Variants.Where(v => v.IsActive);
                StockQuantity = active.Sum(v => v.StockQuantity);
                ReservedQuantity = active.Sum(v => v.ReservedQuantity);
            }
        }
    }
}
