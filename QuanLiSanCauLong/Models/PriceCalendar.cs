using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLiSanCauLong.Models
{
    /// <summary>
    /// Bảng ghi đè giá cho ngày đặc biệt (Lễ, Tết, sự kiện...)
    /// Override giá mặc định từ PriceSlot vào những ngày cụ thể.
    /// 
    /// VÍ DỤ:
    /// - Tết Nguyên Đán (29/01 – 05/02): giá tăng 50%
    /// - Ngày 30/4, 1/5: giá x1.3
    /// - Event giải đấu 15/03: đóng cửa sớm 18:00
    /// 
    /// LOGIC:
    /// 1. Khi tính giá → check PriceCalendar trước
    /// 2. Nếu có override cho ngày đó → dùng giá override
    /// 3. Nếu không → fallback về PriceSlot mặc định
    /// </summary>
    public class PriceCalendar
    {
        [Key]
        public int PriceCalendarId { get; set; }

        /// <summary>Áp dụng cho cơ sở nào (NULL = tất cả cơ sở)</summary>
        public int? FacilityId { get; set; }

        /// <summary>Áp dụng cho sân nào (NULL = tất cả sân trong cơ sở)</summary>
        public int? CourtId { get; set; }

        // ══════════════════════════════════════════════════════════
        // KHOẢNG NGÀY ÁP DỤNG
        // ══════════════════════════════════════════════════════════

        [Required]
        [Display(Name = "Tên sự kiện/ngày lễ")]
        [StringLength(100)]
        public string EventName { get; set; } = "";

        [Required]
        [Display(Name = "Ngày bắt đầu")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required]
        [Display(Name = "Ngày kết thúc")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        // ══════════════════════════════════════════════════════════
        // CÁCH GHI ĐÈ GIÁ
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Loại override:
        /// "Multiplier" = nhân hệ số (VD: 1.5 = tăng 50%)
        /// "FixedPrice" = giá cố định thay thế hoàn toàn
        /// "Surcharge"  = cộng thêm phụ phí
        /// "Closed"     = đóng cửa / không nhận đặt
        /// </summary>
        [Required]
        [StringLength(20)]
        [Display(Name = "Loại ghi đè")]
        public string OverrideType { get; set; } = "Multiplier";

        /// <summary>
        /// Giá trị ghi đè:
        /// - Nếu OverrideType = "Multiplier": 1.5 = tăng 50%, 0.8 = giảm 20%
        /// - Nếu OverrideType = "FixedPrice": 150000 = giá cố định 150k/giờ
        /// - Nếu OverrideType = "Surcharge": 30000 = cộng thêm 30k/giờ
        /// - Nếu OverrideType = "Closed": không cần giá trị
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Giá trị")]
        public decimal OverrideValue { get; set; } = 1;

        // ══════════════════════════════════════════════════════════
        // GIỜ ÁP DỤNG (tuỳ chọn — NULL = cả ngày)
        // ══════════════════════════════════════════════════════════

        /// <summary>Giờ mở cửa đặc biệt (NULL = giữ nguyên)</summary>
        [Display(Name = "Giờ mở cửa đặc biệt")]
        public TimeSpan? SpecialOpenTime { get; set; }

        /// <summary>Giờ đóng cửa đặc biệt (NULL = giữ nguyên)</summary>
        [Display(Name = "Giờ đóng cửa đặc biệt")]
        public TimeSpan? SpecialCloseTime { get; set; }

        // ══════════════════════════════════════════════════════════
        // METADATA
        // ══════════════════════════════════════════════════════════

        [StringLength(200)]
        [Display(Name = "Ghi chú")]
        public string? Notes { get; set; }

        /// <summary>Độ ưu tiên: cao hơn sẽ override thấp hơn</summary>
        public int Priority { get; set; } = 0;

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // ══════════════════════════════════════════════════════════
        // NAVIGATION
        // ══════════════════════════════════════════════════════════

        [ForeignKey("FacilityId")]
        public virtual Facility? Facility { get; set; }

        [ForeignKey("CourtId")]
        public virtual Court? Court { get; set; }

        // ══════════════════════════════════════════════════════════
        // HELPERS
        // ══════════════════════════════════════════════════════════

        /// <summary>Kiểm tra ngày có nằm trong khoảng override không</summary>
        public bool AppliesToDate(DateTime date)
        {
            return IsActive && date.Date >= StartDate.Date && date.Date <= EndDate.Date;
        }

        /// <summary>Kiểm tra có áp dụng cho sân/cơ sở cụ thể không</summary>
        public bool AppliesToCourt(int facilityId, int courtId)
        {
            if (FacilityId.HasValue && FacilityId.Value != facilityId) return false;
            if (CourtId.HasValue && CourtId.Value != courtId) return false;
            return true;
        }

        /// <summary>Tính giá sau override</summary>
        public decimal ApplyOverride(decimal originalPrice)
        {
            return OverrideType switch
            {
                "Multiplier" => originalPrice * OverrideValue,
                "FixedPrice" => OverrideValue,
                "Surcharge" => originalPrice + OverrideValue,
                "Closed" => 0,
                _ => originalPrice
            };
        }
    }
}
