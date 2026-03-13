using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLiSanCauLong.Models
{
    public class Inventory
    {
        [Key]
        public int InventoryId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        public int FacilityId { get; set; }

        // ── Tồn kho THỰC TẾ – số lượng vật lý đang trong kho ──
        [Required]
        public int Quantity { get; set; } = 0;

        // ── Đang bị TREO bởi đơn chưa thanh toán ──
        // Khách đang đánh sân gọi 2 chai nước → HoldQuantity += 2
        // Chưa trừ Quantity, chỉ trừ AvailableQuantity
        public int HoldQuantity { get; set; } = 0;

        // ── Đang CHO THUÊ – khách đang cầm trên sân ──
        // Khi trả: RentedQuantity -= n, Quantity không đổi (hàng về kho)
        // Khi mất: RentedQuantity -= n VÀ Quantity -= n (mất vĩnh viễn)
        public int RentedQuantity { get; set; } = 0;

        // ── HÀNG HỎNG – tách khỏi Available, chờ xử lý ──
        public int DamagedQuantity { get; set; } = 0;

        // ── Ngưỡng kho ──
        public int MinQuantity { get; set; } = 10;
        public int MaxQuantity { get; set; } = 1000;

        public DateTime LastUpdated { get; set; } = DateTime.Now;

        // ── Tồn KHẢ DỤNG (computed – không lưu DB) ──
        // Số có thể bán / cho thuê ngay
        [NotMapped]
        public int AvailableQuantity =>
            Math.Max(0, Quantity - HoldQuantity - RentedQuantity - DamagedQuantity);

        // ── Flags ──
        [NotMapped] public bool IsOutOfStock => AvailableQuantity == 0;
        [NotMapped] public bool IsLowStock => AvailableQuantity > 0 && AvailableQuantity <= MinQuantity;
        [NotMapped] public bool HasHold => HoldQuantity > 0;
        [NotMapped] public bool HasRented => RentedQuantity > 0;
        [NotMapped] public bool HasDamaged => DamagedQuantity > 0;

        // Navigation
        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }

        [ForeignKey("FacilityId")]
        public virtual Facility Facility { get; set; }

        public virtual ICollection<InventoryBatch> Batches { get; set; }
        public virtual ICollection<RentalItem> RentalItems { get; set; }
    }
}