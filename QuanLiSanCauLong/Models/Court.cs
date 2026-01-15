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
        public string CourtNumber { get; set; }

        [Required]
        public string CourtType { get; set; } // Standard, VIP

        public string Status { get; set; } = "Available"; // Available, Maintenance, Occupied

        [StringLength(500)]
        public string Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("FacilityId")]
        public virtual Facility Facility { get; set; }
        public virtual ICollection<Booking> Bookings { get; set; }
    }
}
