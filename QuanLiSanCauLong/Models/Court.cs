using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLiSanCauLong.Models
{
    public class Court
    {
        [Key]
        public int CourtId { get; set; }

        [Required]
        public int FacilityId { get; set; }

        [Required]
        [StringLength(10)]
        [Display(Name = "Số sân")]
        public string CourtNumber { get; set; }

        [Required]
        [Display(Name = "Loại sân")]
        public string CourtType { get; set; } // Standard, VIP

        [Display(Name = "Trạng thái")]
        public string Status { get; set; } = "Available"; // Available, Maintenance, Occupied

        [StringLength(500)]
        [Display(Name = "Mô tả")]
        public string Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Quan hệ với Cơ sở
        [ForeignKey("FacilityId")]
        public virtual Facility? Facility { get; set; }

        // Quan hệ với Đặt sân
        public virtual ICollection<Booking>? Bookings { get; set; }

        // QUAN TRỌNG: Thêm dòng này để sửa lỗi View Details
        public virtual ICollection<PriceSlot>? PriceSlots { get; set; }
    }
}
