using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLiSanCauLong.Models
{
    public class InventoryBatch
    {
        [Key]
        public int BatchId { get; set; }

        public int InventoryId { get; set; }

        [ForeignKey("InventoryId")]
        public virtual Inventory Inventory { get; set; }

        [StringLength(50)]
        public string BatchNumber { get; set; }

        public int OriginalQuantity { get; set; }
        public int RemainingQuantity { get; set; }

        [Column(TypeName = "decimal(18,0)")]
        public decimal CostPrice { get; set; }

        public DateTime? ExpiryDate { get; set; }
        public DateTime ReceivedDate { get; set; } = DateTime.Now;

        [StringLength(50)]
        public string DocumentReference { get; set; }

        // Active | Expired | Depleted
        [StringLength(10)]
        public string Status { get; set; } = "Active";

        [NotMapped] public bool IsDepleted => RemainingQuantity <= 0;
        [NotMapped] public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value.Date < DateTime.Today;

        [NotMapped]
        public int? DaysUntilExpiry => ExpiryDate.HasValue
            ? (int?)(ExpiryDate.Value.Date - DateTime.Today).TotalDays
            : null;
    }
}
