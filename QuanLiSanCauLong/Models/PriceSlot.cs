using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLiSanCauLong.Models
{
    public class PriceSlot
    {
        [Key]
        public int PriceSlotId { get; set; }

        [Required]
        public int FacilityId { get; set; }

        [Required]
        public string CourtType { get; set; }

        public int? DayOfWeek { get; set; } // 0=CN, 1=T2, ... 6=T7, NULL=all days

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

        public bool IsPeakHour { get; set; } = false;
        public bool IsActive { get; set; } = true;

        [ForeignKey("FacilityId")]
        public virtual Facility Facility { get; set; }
    }
}
