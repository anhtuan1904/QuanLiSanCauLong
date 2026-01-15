using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLiSanCauLong.Models
{
    public class Order
    {
        [Key]
        public int OrderId { get; set; }

        [Required]
        [StringLength(20)]
        public string OrderCode { get; set; }

        public int? BookingId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int FacilityId { get; set; }

        public string OrderType { get; set; } = "Product";

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal SubTotal { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal DiscountAmount { get; set; } = 0;

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalAmount { get; set; }

        public string OrderStatus { get; set; } = "Pending";
        public string PaymentMethod { get; set; }
        public string PaymentStatus { get; set; } = "Unpaid";

        [StringLength(500)]
        public string Note { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public DateTime? CompletedAt { get; set; }
        public int? CreatedBy { get; set; }

        [ForeignKey("BookingId")]
        public virtual Booking Booking { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [ForeignKey("FacilityId")]
        public virtual Facility Facility { get; set; }

        [ForeignKey("CreatedBy")]
        public virtual User Creator { get; set; }

        public virtual ICollection<OrderDetail> OrderDetails { get; set; }
        public virtual ICollection<VoucherUsage> VoucherUsages { get; set; }
    }
}
