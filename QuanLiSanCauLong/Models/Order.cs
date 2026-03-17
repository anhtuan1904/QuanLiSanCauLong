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
        public string OrderCode { get; set; } = string.Empty;

        public int? BookingId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int FacilityId { get; set; }

        /// <summary>
        /// Product | Service_Course | Service_Stringing | Service_Tournament
        /// </summary>
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
        public string PaymentMethod { get; set; } = "Cash";
        public string PaymentStatus { get; set; } = "Unpaid";

        [StringLength(500)]
        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public DateTime? CompletedAt { get; set; }
        public int? CreatedBy { get; set; }

        [ForeignKey("BookingId")]
        public virtual Booking? Booking { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        [ForeignKey("FacilityId")]
        public virtual Facility Facility { get; set; } = null!;

        [ForeignKey("CreatedBy")]
        public virtual User? Creator { get; set; }

        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
        public virtual ICollection<VoucherUsage>? VoucherUsages { get; set; }

        // ── Dịch vụ đăng ký (Course / Stringing / Tournament) ───────
        public virtual ICollection<ServiceEnrollment>? ServiceEnrollments { get; set; }

        // ── Helpers ──────────────────────────────────────────────────
        [NotMapped]
        public bool IsServiceOrder =>
            OrderType.StartsWith("Service_", StringComparison.OrdinalIgnoreCase);

        [NotMapped]
        public string ServiceTypeLabel => OrderType switch
        {
            "Service_Course" => "Khóa học",
            "Service_Stringing" => "Căng vợt",
            "Service_Tournament" => "Giải đấu",
            "Product" => "Sản phẩm",
            _ => OrderType
        };
    }
}
