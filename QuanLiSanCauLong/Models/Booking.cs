using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLiSanCauLong.Models
{
    public class Booking
    {
        [Key]
        public int BookingId { get; set; }

        [Required]
        [StringLength(20)]
        public string BookingCode { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int CourtId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime BookingDate { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        [Required]
        public int Duration { get; set; } // Phút

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal CourtPrice { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal ServiceFee { get; set; } = 0;

        [Column(TypeName = "decimal(10,2)")]
        public decimal DiscountAmount { get; set; } = 0;

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalPrice { get; set; }

        public string Status { get; set; } = "Pending";
        public string PaymentMethod { get; set; }
        public string PaymentStatus { get; set; } = "Unpaid";

        [StringLength(500)]
        public string Note { get; set; }

        [StringLength(500)]
        public string CancelReason { get; set; }

        public DateTime? CancelledAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public DateTime? CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
        public int? CheckInBy { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [ForeignKey("CourtId")]
        public virtual Court Court { get; set; }

        [ForeignKey("CheckInBy")]
        public virtual User CheckInStaff { get; set; }

        public virtual ICollection<Order> Orders { get; set; }
        public virtual ICollection<VoucherUsage> VoucherUsages { get; set; }
    }
}
