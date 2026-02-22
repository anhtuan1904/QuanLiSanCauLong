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

        [Required]
        public int FacilityId { get; set; }

        // Tên bảng giá, ví dụ: "Giá ngày thường", "Giá cuối tuần", "Giá lễ"
        [StringLength(100)]
        [Display(Name = "Tên bảng giá")]
        public string? SlotName { get; set; }

        // Loại sân áp dụng: Standard / Professional / Practice
        [Required]
        [StringLength(50)]
        public string CourtType { get; set; }

        // NULL = áp dụng tất cả các ngày trong tuần
        // Có giá trị cụ thể = chỉ áp dụng ngày đó
        [Display(Name = "Ngày trong tuần")]
        public DayOfWeek? DayOfWeek { get; set; }

        // Lưu danh sách ngày áp dụng dạng comma-separated: "1,2,3,4,5" (Thứ 2–6)
        // Dùng khi muốn chọn nhiều ngày cùng lúc
        [StringLength(20)]
        [Display(Name = "Ngày áp dụng (nhiều ngày)")]
        public string? AppliedDays { get; set; }

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
        [Display(Name = "Đơn giá (VNĐ/giờ)")]
        public decimal Price { get; set; }

        // Loại khách: "Retail" = vãng lai, "Member" = cố định/tháng
        [StringLength(20)]
        [Display(Name = "Loại khách hàng")]
        public string CustomerType { get; set; } = "Retail";

        // Phụ phí (đèn, nước...), tính theo giờ
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Phụ phí/giờ (VNĐ)")]
        public decimal Surcharge { get; set; } = 0;

        // Mô tả phụ phí, ví dụ: "Phí bật đèn"
        [StringLength(100)]
        [Display(Name = "Mô tả phụ phí")]
        public string? SurchargeNote { get; set; }

        // Đơn vị làm tròn: 15, 30, hoặc 60 phút
        [Display(Name = "Đơn vị làm tròn (phút)")]
        public int RoundingMinutes { get; set; } = 60;

        [Display(Name = "Giờ cao điểm?")]
        public bool IsPeakHour { get; set; }

        public bool IsActive { get; set; } = true;

        [ForeignKey("CourtId")]
        public virtual Court? Court { get; set; }

        [ForeignKey("FacilityId")]
        public virtual Facility? Facility { get; set; }
    }
}
