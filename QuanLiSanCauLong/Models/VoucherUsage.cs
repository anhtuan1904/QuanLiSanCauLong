using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLiSanCauLong.Models
{
    public class VoucherUsage
    {
        [Key]
        public int UsageId { get; set; }

        [Required]
        public int VoucherId { get; set; }

        [Required]
        public int UserId { get; set; }

        public int? BookingId { get; set; }
        public int? OrderId { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal DiscountAmount { get; set; }

        public DateTime UsedAt { get; set; } = DateTime.Now;

        [ForeignKey("VoucherId")]
        public virtual Voucher Voucher { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [ForeignKey("BookingId")]
        public virtual Booking Booking { get; set; }

        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; }
    }

}
