using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLiSanCauLong.Models
{
    /// <summary>
    /// Bảng giá theo khung giờ — hỗ trợ đầy đủ:
    /// ✅ Overlap pricing (tính giá cộng dồn khi đặt qua nhiều khung giờ)
    /// ✅ Weekend vs Weekday (giá riêng theo ngày)
    /// ✅ Retail vs Member (giá riêng theo loại khách)
    /// ✅ Flex slot (đơn vị tối thiểu 15/30/60 phút)
    /// ✅ Phụ phí (đèn, nước, AC...)
    /// ✅ Minimum duration (thời gian đặt tối thiểu)
    /// ✅ Buffer time (thời gian chờ giữa 2 ca)
    /// </summary>
    public class PriceSlot
    {
        [Key]
        public int PriceSlotId { get; set; }

        [Required]
        public int CourtId { get; set; }

        [Required]
        public int FacilityId { get; set; }

        // ══════════════════════════════════════════════════════════
        // THÔNG TIN CƠ BẢN
        // ══════════════════════════════════════════════════════════

        /// <summary>Tên bảng giá: "Giá ngày thường", "Giá cuối tuần", "Giá lễ"</summary>
        [StringLength(100)]
        [Display(Name = "Tên bảng giá")]
        public string? SlotName { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>Loại sân áp dụng: Standard / Professional / Practice</summary>
        [Required]
        [StringLength(50)]
        public string CourtType { get; set; } = "Standard";

        // ══════════════════════════════════════════════════════════
        // NGÀY ÁP DỤNG (FIX #2: Weekend vs Weekday)
        // ══════════════════════════════════════════════════════════

        /// <summary>NULL = tất cả ngày, có giá trị = chỉ ngày đó</summary>
        [Display(Name = "Ngày trong tuần")]
        public DayOfWeek? DayOfWeek { get; set; }

        /// <summary>
        /// Comma-separated: "1,2,3,4,5" (T2-T6), "6,0" (T7,CN), "0,1,2,3,4,5,6" (tất cả)
        /// Ưu tiên hơn DayOfWeek nếu có giá trị
        /// </summary>
        [StringLength(20)]
        [Display(Name = "Ngày áp dụng (nhiều ngày)")]
        public string? AppliedDays { get; set; }

        // ══════════════════════════════════════════════════════════
        // KHUNG GIỜ & GIÁ (FIX #1: Overlap sẽ xử lý ở Service)
        // ══════════════════════════════════════════════════════════

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

        // ══════════════════════════════════════════════════════════
        // LOẠI KHÁCH (FIX #3: Retail vs Member)
        // ══════════════════════════════════════════════════════════

        /// <summary>"All" = mọi khách, "Retail" = vãng lai, "Member" = cố định/tháng</summary>
        [StringLength(20)]
        [Display(Name = "Loại khách hàng")]
        public string CustomerType { get; set; } = "All";

        /// <summary>% chiết khấu cho khách Member (0-100). VD: 15 = giảm 15%</summary>
        [Column(TypeName = "decimal(5,2)")]
        [Display(Name = "Chiết khấu Member (%)")]
        public decimal MemberDiscount { get; set; } = 0;

        // ══════════════════════════════════════════════════════════
        // PHỤ PHÍ
        // ══════════════════════════════════════════════════════════

        /// <summary>Phụ phí cộng thêm mỗi giờ (đèn, nước, AC...)</summary>
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Phụ phí/giờ (VNĐ)")]
        public decimal Surcharge { get; set; } = 0;

        [StringLength(100)]
        [Display(Name = "Mô tả phụ phí")]
        public string? SurchargeNote { get; set; }

        // ══════════════════════════════════════════════════════════
        // FLEX SLOT (FIX #4: Bước nhảy thời gian linh hoạt)
        // ══════════════════════════════════════════════════════════

        /// <summary>Đơn vị tính giá tối thiểu: 15, 30, hoặc 60 phút</summary>
        [Display(Name = "Đơn vị làm tròn (phút)")]
        public int RoundingMinutes { get; set; } = 60;

        /// <summary>
        /// Thời gian đặt tối thiểu (phút). VD: 60 = ít nhất 1 tiếng.
        /// Ngăn slot "mồ côi" 30 phút khó bán (FIX #6)
        /// </summary>
        [Display(Name = "Thời gian đặt tối thiểu (phút)")]
        public int MinDurationMinutes { get; set; } = 60;

        /// <summary>
        /// Thời gian buffer giữa 2 ca (phút). VD: 15 = nghỉ 15p giữa ca.
        /// Giúp dọn sân, tránh xung đột (FIX #6)
        /// </summary>
        [Display(Name = "Buffer giữa 2 ca (phút)")]
        public int BufferMinutes { get; set; } = 0;

        // ══════════════════════════════════════════════════════════
        // TRẠNG THÁI & FLAGS
        // ══════════════════════════════════════════════════════════

        [Display(Name = "Giờ cao điểm?")]
        public bool IsPeakHour { get; set; }

        public bool IsActive { get; set; } = true;

        /// <summary>Độ ưu tiên: slot có priority cao hơn sẽ override slot thấp hơn</summary>
        [Display(Name = "Độ ưu tiên")]
        public int Priority { get; set; } = 0;

        // ══════════════════════════════════════════════════════════
        // NAVIGATION
        // ══════════════════════════════════════════════════════════

        [ForeignKey("CourtId")]
        public virtual Court? Court { get; set; }

        [ForeignKey("FacilityId")]
        public virtual Facility? Facility { get; set; }

        // ══════════════════════════════════════════════════════════
        // HELPER METHODS
        // ══════════════════════════════════════════════════════════

        /// <summary>Kiểm tra slot có áp dụng cho ngày cụ thể không</summary>
        public bool AppliesToDay(DayOfWeek day)
        {
            // Nếu có AppliedDays → check trong danh sách
            if (!string.IsNullOrEmpty(AppliedDays))
            {
                var dayNums = AppliedDays.Split(',')
                    .Select(d => int.TryParse(d.Trim(), out var n) ? n : -1)
                    .Where(n => n >= 0);
                return dayNums.Contains((int)day);
            }
            // Nếu có DayOfWeek cụ thể → check match
            if (DayOfWeek.HasValue)
                return DayOfWeek.Value == day;
            // Không set gì = áp dụng tất cả ngày
            return true;
        }

        /// <summary>Kiểm tra slot có overlap với khoảng thời gian không</summary>
        public bool OverlapsWith(TimeSpan start, TimeSpan end)
        {
            return StartTime < end && EndTime > start;
        }

        /// <summary>Tính giá thực tế cho Member (sau chiết khấu)</summary>
        public decimal GetEffectivePrice(bool isMember)
        {
            var basePrice = Price + Surcharge;
            if (isMember && MemberDiscount > 0)
                return basePrice * (1 - MemberDiscount / 100);
            return basePrice;
        }

        /// <summary>Tính số phút overlap giữa slot và khoảng thời gian đặt</summary>
        public double GetOverlapMinutes(TimeSpan bookStart, TimeSpan bookEnd)
        {
            var overlapStart = StartTime > bookStart ? StartTime : bookStart;
            var overlapEnd = EndTime < bookEnd ? EndTime : bookEnd;
            var overlap = (overlapEnd - overlapStart).TotalMinutes;
            return overlap > 0 ? overlap : 0;
        }
    }
}
