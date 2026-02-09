using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLiSanCauLong.Models
{
    public class InventoryTransaction
    {
        [Key]
        public int TransactionId { get; set; }

        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }

        // View đang gọi log.Type (StockIn, StockOut)
        [Required]
        public string Type { get; set; }

        [Required]
        public int Quantity { get; set; }

        public DateTime TransactionDate { get; set; } = DateTime.Now;

        public string Note { get; set; }

        // View đang gọi log.UserEmail
        public string UserEmail { get; set; }

        // View đang gọi log.Facility.FacilityName
        public int FacilityId { get; set; }

        [ForeignKey("FacilityId")]
        public virtual Facility Facility { get; set; }
    }
}