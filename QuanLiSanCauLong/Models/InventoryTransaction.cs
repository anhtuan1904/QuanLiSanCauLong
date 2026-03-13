// ===================================================================
// FILE: Models/InventoryTransaction.cs  (Final – Phase 1+2)
// ===================================================================
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLiSanCauLong.Models
{
    /// <summary>
    /// Constants cho tất cả loại giao dịch kho.
    /// Dùng TxType.StockIn thay vì "StockIn" để tránh typo.
    /// </summary>
    public static class TxType
    {
        public const string StockIn = "StockIn";       // Nhập kho thủ công
        public const string StockOut = "StockOut";      // Xuất kho thủ công
        public const string SaleHold = "SaleHold";      // Treo hàng (đơn chưa TT)
        public const string SaleConfirmed = "SaleConfirmed"; // Bán đứt (đã thanh toán)
        public const string SaleCancel = "SaleCancel";    // Huỷ đơn → release hold
        public const string RentalOut = "RentalOut";     // Xuất cho thuê
        public const string RentalReturn = "RentalReturn";  // Khách trả đồ thuê
        public const string RentalLost = "RentalLost";    // Mất / không trả
        public const string Adjustment = "Adjustment";    // Điều chỉnh kiểm kê
        public const string Damaged = "Damaged";       // Ghi nhận hàng hỏng
        public const string DamagedWrite = "DamagedWrite";  // Xoá sổ hàng hỏng
        public const string Transfer = "Transfer";      // Chuyển kho giữa cơ sở
    }

    public class InventoryTransaction
    {
        [Key]
        public int TransactionId { get; set; }

        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }

        [Required]
        [StringLength(20)]
        public string Type { get; set; }

        [Required]
        public int Quantity { get; set; }

        // Snapshot tồn kho SAU giao dịch – dùng cho audit trail
        public int QuantityAfter { get; set; }

        [Column(TypeName = "decimal(18,0)")]
        public decimal CostPrice { get; set; } = 0;

        [Column(TypeName = "decimal(18,0)")]
        public decimal SalePrice { get; set; } = 0;

        // Liên kết đến đơn hàng hoặc phiên thuê
        public int? ReferenceId { get; set; }

        [StringLength(20)]
        public string ReferenceType { get; set; } // "Order" | "Rental"

        public int FacilityId { get; set; }

        [ForeignKey("FacilityId")]
        public virtual Facility Facility { get; set; }

        // Cơ sở đích (chỉ dùng cho Transfer)
        public int? TargetFacilityId { get; set; }

        public DateTime TransactionDate { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string UserEmail { get; set; }

        [StringLength(500)]
        public string Note { get; set; }

        // FIFO tracking
        [StringLength(50)]
        public string BatchNumber { get; set; }

        public DateTime? ExpiryDate { get; set; }

        // Helpers (không lưu DB)
        [NotMapped]
        public bool IsInbound => Type is TxType.StockIn or TxType.RentalReturn or TxType.SaleCancel;

        [NotMapped]
        public bool IsOutbound => Type is TxType.StockOut or TxType.SaleConfirmed
                                       or TxType.SaleHold or TxType.RentalOut
                                       or TxType.RentalLost or TxType.DamagedWrite;
    }
}
