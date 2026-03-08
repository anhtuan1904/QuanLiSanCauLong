using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLiSanCauLong.Models
{
    /// <summary>
    /// Phân loại sản phẩm theo Size / Màu sắc.
    /// Mỗi variant giữ số lượng tồn kho riêng.
    /// </summary>
    public class ProductVariant
    {
        [Key]
        public int VariantId { get; set; }

        [Required]
        public int ProductId { get; set; }

        /// <summary>Size chữ (S/M/L/XL/XXL) hoặc size số (38/39/40...)</summary>
        [StringLength(50)]
        public string SizeName { get; set; }

        /// <summary>Màu sắc (nếu có)</summary>
        [StringLength(50)]
        public string ColorName { get; set; }

        /// <summary>SKU riêng cho variant này (auto-generate hoặc nhập tay)</summary>
        [StringLength(100)]
        public string VariantSKU { get; set; }

        /// <summary>Tồn kho thực tế của variant</summary>
        public int StockQuantity { get; set; } = 0;

        /// <summary>
        /// Số lượng đang bị treo bởi đơn hàng chưa duyệt (Pending/Confirmed).
        /// AvailableQuantity = StockQuantity - ReservedQuantity
        /// </summary>
        public int ReservedQuantity { get; set; } = 0;

        /// <summary>Ngưỡng cảnh báo hàng sắp hết cho variant này</summary>
        public int MinStockLevel { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // ===== NAVIGATION =====
        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }

        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

        // ===== COMPUTED =====
        [NotMapped]
        public int AvailableQuantity => StockQuantity - ReservedQuantity;

        [NotMapped]
        public bool IsLowStock => MinStockLevel > 0 && AvailableQuantity <= MinStockLevel;

        [NotMapped]
        public string DisplayName
        {
            get
            {
                var parts = new List<string>();
                if (!string.IsNullOrEmpty(SizeName)) parts.Add($"Size: {SizeName}");
                if (!string.IsNullOrEmpty(ColorName)) parts.Add(ColorName);
                return parts.Any() ? string.Join(" / ", parts) : "Mặc định";
            }
        }
    }
}
