// ════════════════════════════════════════════════════════════
//  File: ViewModels/BulkPriceSlotRequest.cs
// ════════════════════════════════════════════════════════════
namespace QuanLiSanCauLong.ViewModels
{
    public class BulkPriceSlotRequest
    {
        /// <summary>Danh sách CourtId (int)</summary>
        public List<int> CourtIds { get; set; } = new();

        /// <summary>Danh sách ngày áp dụng (0=CN, 1=T2...6=T7). Null/rỗng = tất cả ngày.</summary>
        public List<int>? Days { get; set; }

        /// <summary>Danh sách khung giờ cần tạo</summary>
        public List<TimeSlotItem> TimeSlots { get; set; } = new();
    }

    public class TimeSlotItem
    {
        // ── Nhận từ JS dưới dạng string "HH:mm" ──────────────────────
        public string? StartTimeStr { get; set; }
        public string? EndTimeStr { get; set; }

        // Alias camelCase — JS gửi key startTime / endTime
        public string? StartTime
        {
            get => StartTimeStr;
            set => StartTimeStr = value;
        }
        public string? EndTime
        {
            get => EndTimeStr;
            set => EndTimeStr = value;
        }

        // ── Computed: parse string → TimeSpan để Controller dùng trực tiếp ──
        /// <summary>Parse StartTimeStr "HH:mm" → TimeSpan. Trả về TimeSpan.Zero nếu lỗi.</summary>
        public TimeSpan StartTimeParsed =>
            TimeSpan.TryParse(StartTimeStr, out var t) ? t : TimeSpan.Zero;

        /// <summary>Parse EndTimeStr "HH:mm" → TimeSpan. Trả về TimeSpan.Zero nếu lỗi.</summary>
        public TimeSpan EndTimeParsed =>
            TimeSpan.TryParse(EndTimeStr, out var t) ? t : TimeSpan.Zero;

        // ── Các trường khác ────────────────────────────────────────────
        public decimal Price { get; set; }
        public decimal Surcharge { get; set; }
        public string? SlotName { get; set; }
        public string CustType { get; set; } = "All";
        public string? SurNote { get; set; }
        public bool IsPeakHour { get; set; }
    }
}
