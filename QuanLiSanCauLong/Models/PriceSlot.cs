using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLiSanCauLong.Models
{
    public class PriceSlot
    {
        [Key]
        public int PriceSlotId { get; set; }

        [Required]
        public int CourtId { get; set; }

        // THÊM: Để hàm CalculateCourtPrice có thể lọc theo cơ sở
        [Required]
        public int FacilityId { get; set; }

        // THÊM: Để phân biệt giá sân VIP/Thường (Sửa lỗi image_56092f.png)
        [Required]
        [StringLength(50)]
        public string CourtType { get; set; }

        // THÊM: Để tính giá theo ngày trong tuần (Sửa lỗi image_55b353.png)
        [Required]
        public DayOfWeek DayOfWeek { get; set; }

        [Required]
        [Display(Name = "Giờ bắt đầu")]
        [DataType(DataType.Time)]
        public TimeSpan StartTime { get; set; }

        [Required]
        [Display(Name = "Giờ kết thúc")]
        [DataType(DataType.Time)]
        public TimeSpan EndTime { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Giá thuê (VNĐ)")]
        public decimal Price { get; set; }

        [Display(Name = "Giờ cao điểm?")]
        public bool IsPeakHour { get; set; }

        // THÊM: Để logic kiểm tra p.IsActive hoạt động
        public bool IsActive { get; set; } = true;

        [ForeignKey("CourtId")]
        public virtual Court? Court { get; set; }

        [ForeignKey("FacilityId")]
        public virtual Facility? Facility { get; set; }
    }
}