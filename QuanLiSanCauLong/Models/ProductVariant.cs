using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLiSanCauLong.Models
{
    /// <summary>
    /// Phân loại sản phẩm theo Size / Màu sắc.
    /// Mỗi variant giữ số lượng tồn kho riêng.
    ///
    /// Hành vi theo BehaviorType của danh mục:
    ///   Retail  → StockQuantity giảm khi bán, không hoàn.
    ///   Rental  → StockQuantity không đổi; RentedQuantity tăng khi xuất, giảm khi khách trả.
    ///   Service → Variant không sử dụng (sản phẩm Service không có kho vật lý).
    /// </summary>
    public class ProductVariant
    {
        [Key]
        public int VariantId { get; set; }

        [Required]
        public int ProductId { get; set; }

        // ===== 1. ĐỊNH DANH =====

        /// <summary>Size chữ (S/M/L/XL/XXL) hoặc size số (38/39/40...)</summary>
        [StringLength(50)]
        public string SizeName { get; set; }

        /// <summary>Màu sắc (nếu có)</summary>
        [StringLength(50)]
        public string ColorName { get; set; }

        /// <summary>SKU riêng cho variant này (auto-generate hoặc nhập tay)</summary>
        [StringLength(100)]
        public string VariantSKU { get; set; }

        // ===== 2. TỒN KHO (RETAIL & RENTAL) =====

        /// <summary>
        /// Tổng số lượng nhập vào / đang sở hữu.
        /// - Retail : giảm khi bán.
        /// - Rental : KHÔNG giảm khi cho thuê — chỉ dùng để biết tổng tài sản.
        /// </summary>
        public int StockQuantity { get; set; } = 0;

        /// <summary>
        /// Số lượng đang bị treo bởi đơn hàng Retail chưa duyệt (Pending/Confirmed).
        /// AvailableQuantity = StockQuantity - ReservedQuantity - RentedQuantity
        /// </summary>
        public int ReservedQuantity { get; set; } = 0;

        /// <summary>
        /// Số lượng đang được khách thuê, chưa trả về.
        /// Chỉ có ý nghĩa khi Category.BehaviorType == "Rental".
        /// Tăng khi xuất cho thuê, giảm khi khách trả đồ.
        /// </summary>
        public int RentedQuantity { get; set; } = 0;

        /// <summary>
        /// Số lượng bị hỏng / mất, chưa xử lý.
        /// Không tính vào AvailableQuantity.
        /// </summary>
        public int DamagedQuantity { get; set; } = 0;

        /// <summary>Ngưỡng cảnh báo hàng sắp hết cho variant này</summary>
        public int MinStockLevel { get; set; } = 0;

        /// <summary>Số lượng tối đa khuyến nghị lưu kho (tuỳ chọn)</summary>
        public int MaxQuantity { get; set; } = 0;

        // ===== 3. TRẠNG THÁI =====
        public bool IsActive { get; set; } = true;

        // ===== 4. TIMESTAMPS =====
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // ===== 5. NAVIGATION =====
        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }

        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

        // ===== 6. COMPUTED (NOT MAPPED) =====

        /// <summary>
        /// Số lượng khả dụng để bán / cho thuê.
        /// - Retail : StockQuantity - ReservedQuantity
        /// - Rental : StockQuantity - RentedQuantity - DamagedQuantity
        /// </summary>
        [NotMapped]
        public int AvailableQuantity => StockQuantity - ReservedQuantity - RentedQuantity - DamagedQuantity;

        [NotMapped]
        public bool IsLowStock => MinStockLevel > 0 && AvailableQuantity <= MinStockLevel;

        [NotMapped]
        public bool IsOutOfStock => AvailableQuantity <= 0;

        [NotMapped]
        public bool HasRented => RentedQuantity > 0;

        [NotMapped]
        public bool HasDamaged => DamagedQuantity > 0;

        /// <summary>
        /// Tên hiển thị tổng hợp (dùng cho dropdown, label).
        /// Ví dụ: "Size: 40 / Đen" hoặc "Size: M" hoặc "Mặc định".
        /// </summary>
        [NotMapped]
        public string DisplayName
        {
            get
            {
                var parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(SizeName)) parts.Add($"Size: {SizeName}");
                if (!string.IsNullOrWhiteSpace(ColorName)) parts.Add(ColorName);
                return parts.Count > 0 ? string.Join(" / ", parts) : "Mặc định";
            }
        }
    }
}
