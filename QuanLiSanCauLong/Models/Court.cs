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
        [StringLength(50)]
        [Display(Name = "Số sân")]
        public string CourtNumber { get; set; }

        [Required]
        [Display(Name = "Loại sân")]
        public string CourtType { get; set; } // "Indoor" hoặc "Outdoor"

        [Display(Name = "Trạng thái")]
        public string Status { get; set; } = "Available";

        [StringLength(500)]
        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [Display(Name = "Vị trí tầng")]
        public string? FloorNumber { get; set; }

        [Display(Name = "Loại mặt sân")]
        public string? SurfaceType { get; set; }

        // Tiện ích
        [Display(Name = "Hệ thống đèn")]
        public bool HasLighting { get; set; }

        [Display(Name = "Điều hòa")]
        public bool HasAC { get; set; }

        [Display(Name = "Wifi")]
        public bool HasWifi { get; set; }

        [Display(Name = "Bãi đậu xe")]
        public bool HasParking { get; set; }

        [Display(Name = "Căn tin")]
        public bool HasCanteen { get; set; }

        [Display(Name = "Phòng tắm")]
        public bool HasShower { get; set; }

        // Giá
        [Display(Name = "Giá giờ thường")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal RegularPrice { get; set; }

        [Display(Name = "Giá cuối tuần")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal? WeekendPrice { get; set; }

        [Display(Name = "Giá giờ cao điểm")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal? PeakHourPrice { get; set; }

        // Legacy - giữ để backward compatible
        [Display(Name = "Giá thuê mỗi giờ")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal HourlyRate { get; set; }

        [Display(Name = "Hình ảnh sân")]
        public string? ImagePath { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("FacilityId")]
        public virtual Facility? Facility { get; set; }

        public virtual ICollection<Booking>? Bookings { get; set; }
        public virtual ICollection<PriceSlot>? PriceSlots { get; set; }
        public virtual ICollection<CourtImage>? CourtImages { get; set; }
    }
}
