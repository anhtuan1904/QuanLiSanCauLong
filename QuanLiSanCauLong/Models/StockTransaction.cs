using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLiSanCauLong.Models
{
    public class StockTransaction
    {
        [Key]
        public int TransactionId { get; set; }

        [Required]
        public int FacilityId { get; set; }

        [Required]
        public string TransactionType { get; set; } // Import, Export, Adjustment

        public DateTime TransactionDate { get; set; } = DateTime.Now;

        [Required]
        public int CreatedBy { get; set; }

        [StringLength(500)]
        public string Note { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? TotalAmount { get; set; }

        [ForeignKey("FacilityId")]
        public virtual Facility Facility { get; set; }

        [ForeignKey("CreatedBy")]
        public virtual User Creator { get; set; }

        public virtual ICollection<StockTransactionDetail> Details { get; set; }
    }
}
