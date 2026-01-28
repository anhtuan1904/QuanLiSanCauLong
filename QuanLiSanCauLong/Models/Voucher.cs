using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLiSanCauLong.Models
{
    public class Voucher
    {
        [Key]
        public int VoucherId { get; set; }

        [Required]
        [StringLength(50)]
        public string VoucherCode { get; set; }

        [Required]
        [StringLength(200)]
        public string VoucherName { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [Required]
        public string DiscountType { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal DiscountValue { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? MinOrderAmount { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? MaxDiscount { get; set; }

        public string ApplicableFor { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        public int? UsageLimit { get; set; }
        public int UsageLimitPerUser { get; set; } = 1;
        public int UsedCount { get; set; } = 0;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public int? CreatedBy { get; set; }

        [ForeignKey("CreatedBy")]
        public virtual User Creator { get; set; }

        public DateTime? UpdatedAt { get; set; } = DateTime.Now;
        public virtual ICollection<VoucherUsage> VoucherUsages { get; set; }
    }
}
