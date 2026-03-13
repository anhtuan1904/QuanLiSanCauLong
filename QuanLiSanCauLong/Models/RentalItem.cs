using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLiSanCauLong.Models
{
    public class RentalItem
    {
        [Key]
        public int RentalItemId { get; set; }

        public int InventoryId { get; set; }

        [ForeignKey("InventoryId")]
        public virtual Inventory Inventory { get; set; }

        // Đơn hàng liên quan (nếu có)
        public int? OrderId { get; set; }

        // Sân đang sử dụng
        [StringLength(20)]
        public string CourtCode { get; set; }

        // Thông tin khách
        [StringLength(100)]
        public string CustomerName { get; set; }

        [StringLength(20)]
        public string CustomerPhone { get; set; }

        // Số lượng thuê
        public int Quantity { get; set; }

        // Size (giày, quần áo...)
        [StringLength(10)]
        public string Size { get; set; }

        // Thời gian
        public DateTime RentedAt { get; set; } = DateTime.Now;
        public DateTime? ReturnedAt { get; set; }
        public DateTime? ExpectedReturnAt { get; set; }

        // Active | Returned | Lost | Damaged
        [StringLength(15)]
        public string Status { get; set; } = "Active";

        [Column(TypeName = "decimal(18,0)")]
        public decimal CleaningFeeCharged { get; set; } = 0;

        [StringLength(300)]
        public string Note { get; set; }

        [StringLength(100)]
        public string CreatedBy { get; set; }

        // Computed
        [NotMapped]
        public bool IsOverdue =>
            Status == "Active" && ExpectedReturnAt.HasValue && DateTime.Now > ExpectedReturnAt.Value;

        [NotMapped]
        public TimeSpan Duration =>
            ReturnedAt.HasValue ? ReturnedAt.Value - RentedAt : DateTime.Now - RentedAt;
    }
}
