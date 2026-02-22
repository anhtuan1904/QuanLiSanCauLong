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
        public decimal? HourlyRate { get; set; }

        // ── Thông tin cơ bản ─────────────────────────────────────
        [Required(ErrorMessage = "Vui lòng nhập tên / số sân")]
        [StringLength(50)]
        [Display(Name = "Tên / Số sân")]
        public string CourtNumber { get; set; } = string.Empty;
        // VD: "Sân số 1", "Sân VIP", "Sân A"

        [Required(ErrorMessage = "Vui lòng chọn vị trí sân")]
        [Display(Name = "Vị trí")]
        public string CourtType { get; set; } = "Indoor";
        // "Indoor" = Trong nhà  |  "Outdoor" = Ngoài trời

        [Required(ErrorMessage = "Vui lòng chọn loại mặt sân")]
        [Display(Name = "Loại mặt sân")]
        public string SurfaceType { get; set; } = "PVC";
        // "PVC" = Thảm cao su PVC  |  "Wood" = Sàn gỗ  |  "Silicon" = Sơn Silicon

        [Required(ErrorMessage = "Vui lòng chọn tình trạng")]
        [Display(Name = "Tình trạng")]
        public string Status { get; set; } = "Available";
        // "Available" = Đang trống  |  "Active" = Đang hoạt động  |  "Maintenance" = Đang bảo trì

        // ── Vị trí trong cơ sở ───────────────────────────────────
        [Display(Name = "Vị trí tầng")]
        [StringLength(30)]
        public string? FloorNumber { get; set; }
        // VD: "Tầng 1", "Tầng 2" — tuỳ chọn

        // ── Tiện ích của sân ─────────────────────────────────────
        [Display(Name = "Đèn chiếu sáng")]
        public bool HasLighting { get; set; }

        [Display(Name = "Điều hoà")]
        public bool HasAC { get; set; }

        // ── Mô tả ────────────────────────────────────────────────
        [StringLength(1000)]
        [Display(Name = "Mô tả thêm")]
        public string? Description { get; set; }
        // VD: "Gần cửa sổ, ánh sáng tự nhiên tốt nhất, vị trí góc yên tĩnh"

        // ── Hình ảnh ─────────────────────────────────────────────
        [Display(Name = "Hình ảnh sân")]
        public string? ImagePath { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // ── Navigation ───────────────────────────────────────────
        [ForeignKey("FacilityId")]
        public virtual Facility? Facility { get; set; }

        public virtual ICollection<Booking>? Bookings { get; set; }
        public virtual ICollection<PriceSlot>? PriceSlots { get; set; }
        public virtual ICollection<CourtImage>? CourtImages { get; set; }

        // ── Computed (NotMapped) ─────────────────────────────────
        [NotMapped]
        public string StatusLabel => Status switch
        {
            "Available" => "Đang trống",
            "Active" => "Đang hoạt động",
            "Maintenance" => "Đang bảo trì",
            _ => Status
        };

        [NotMapped]
        public string CourtTypeLabel => CourtType switch
        {
            "Indoor" => "Trong nhà",
            "Outdoor" => "Ngoài trời",
            _ => CourtType
        };

        [NotMapped]
        public string SurfaceTypeLabel => SurfaceType switch
        {
            "PVC" => "Thảm cao su (PVC)",
            "Wood" => "Sàn gỗ",
            "Silicon" => "Sơn Silicon",
            _ => SurfaceType
        };
    }
}
