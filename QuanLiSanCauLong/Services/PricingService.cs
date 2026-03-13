using Microsoft.EntityFrameworkCore;
using QuanLiSanCauLong.Data;
using QuanLiSanCauLong.Models;

namespace QuanLiSanCauLong.Services
{
    /// <summary>
    /// Service tính giá thông minh — xử lý TẤT CẢ ngoại lệ:
    /// 
    /// 1. OVERLAP PRICING: Khách đặt 16:00–18:00 qua 2 khung giá khác nhau
    ///    → Tính cộng dồn từng block thay vì lấy giá đầu
    /// 
    /// 2. WEEKEND vs WEEKDAY: Giá khác nhau theo ngày
    ///    → Check AppliedDays / DayOfWeek trong PriceSlot
    /// 
    /// 3. MEMBER vs RETAIL: Khách cố định được giảm giá
    ///    → Check CustomerType + MemberDiscount
    /// 
    /// 4. FLEX SLOT: Đặt 1.5h, 2h tự do
    ///    → Chia theo block nhỏ nhất (RoundingMinutes) rồi cộng dồn
    /// 
    /// 5. NGÀY LỄ: Override giá bằng PriceCalendar
    ///    → Check PriceCalendar trước, fallback PriceSlot
    /// 
    /// 6. MINIMUM DURATION & BUFFER: Tránh slot mồ côi
    ///    → Validate trước khi cho phép đặt
    /// </summary>
    public class PricingService
    {
        private readonly ApplicationDbContext _context;

        public PricingService(ApplicationDbContext context)
        {
            _context = context;
        }

        // ══════════════════════════════════════════════════════════
        // TÍNH GIÁ CHÍNH (Entry Point)
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Tính tổng tiền cho 1 booking.
        /// Xử lý overlap: nếu đặt 16:00–18:00 và có 2 khung giá (16-17: 80k, 17-18: 120k)
        /// → Tổng = 80k + 120k = 200k (KHÔNG phải 80k × 2 = 160k)
        /// </summary>
        public async Task<PricingResult> CalculatePrice(
            int courtId,
            DateTime bookingDate,
            TimeSpan startTime,
            TimeSpan endTime,
            bool isMember = false,
            string customerType = "Retail")
        {
            var result = new PricingResult
            {
                CourtId = courtId,
                BookingDate = bookingDate,
                StartTime = startTime,
                EndTime = endTime,
                IsMember = isMember
            };

            // 1. Lấy court info
            var court = await _context.Courts
                .Include(c => c.Facility)
                .FirstOrDefaultAsync(c => c.CourtId == courtId);

            if (court == null)
            {
                result.IsValid = false;
                result.ErrorMessage = "Không tìm thấy sân!";
                return result;
            }

            // 2. Check ngày lễ — có bị đóng cửa không?
            var calendarOverride = await GetCalendarOverride(court.FacilityId, courtId, bookingDate);
            if (calendarOverride != null && calendarOverride.OverrideType == "Closed")
            {
                result.IsValid = false;
                result.ErrorMessage = $"Sân đóng cửa ngày {bookingDate:dd/MM} — {calendarOverride.EventName}";
                return result;
            }

            // 3. Check giờ mở/đóng cửa đặc biệt
            if (calendarOverride != null)
            {
                if (calendarOverride.SpecialOpenTime.HasValue && startTime < calendarOverride.SpecialOpenTime.Value)
                {
                    result.IsValid = false;
                    result.ErrorMessage = $"Ngày {bookingDate:dd/MM} mở cửa lúc {calendarOverride.SpecialOpenTime.Value:hh\\:mm}";
                    return result;
                }
                if (calendarOverride.SpecialCloseTime.HasValue && endTime > calendarOverride.SpecialCloseTime.Value)
                {
                    result.IsValid = false;
                    result.ErrorMessage = $"Ngày {bookingDate:dd/MM} đóng cửa lúc {calendarOverride.SpecialCloseTime.Value:hh\\:mm}";
                    return result;
                }
            }

            // 4. Lấy tất cả PriceSlot áp dụng cho sân này, ngày này
            var dayOfWeek = bookingDate.DayOfWeek;
            var applicableSlots = await GetApplicableSlots(courtId, dayOfWeek, customerType);

            if (!applicableSlots.Any())
            {
                result.IsValid = false;
                result.ErrorMessage = "Chưa cấu hình giá cho sân này vào ngày này!";
                return result;
            }

            // 5. Validate minimum duration
            var totalMinutes = (endTime - startTime).TotalMinutes;
            var minDuration = applicableSlots.Min(s => s.MinDurationMinutes);
            if (totalMinutes < minDuration)
            {
                result.IsValid = false;
                result.ErrorMessage = $"Thời gian đặt tối thiểu là {minDuration} phút!";
                return result;
            }

            // 6. ═══ CORE: Tính giá cộng dồn theo từng block ═══
            var rounding = applicableSlots.First().RoundingMinutes;
            var currentTime = startTime;
            decimal totalPrice = 0;
            var breakdown = new List<PriceBreakdownItem>();

            while (currentTime < endTime)
            {
                var blockEnd = currentTime.Add(TimeSpan.FromMinutes(rounding));
                if (blockEnd > endTime) blockEnd = endTime;

                // Tìm slot có giá cao nhất priority match block này
                var matchingSlot = applicableSlots
                    .Where(s => s.OverlapsWith(currentTime, blockEnd))
                    .OrderByDescending(s => s.Priority)
                    .ThenByDescending(s => s.IsPeakHour) // Peak hour ưu tiên
                    .FirstOrDefault();

                if (matchingSlot != null)
                {
                    // Tính phần overlap thực tế
                    var overlapMinutes = matchingSlot.GetOverlapMinutes(currentTime, blockEnd);
                    var hours = (decimal)overlapMinutes / 60m;
                    var blockPrice = matchingSlot.GetEffectivePrice(isMember) * hours;

                    // Apply calendar override nếu có
                    if (calendarOverride != null)
                        blockPrice = calendarOverride.ApplyOverride(blockPrice);

                    totalPrice += blockPrice;

                    breakdown.Add(new PriceBreakdownItem
                    {
                        StartTime = currentTime,
                        EndTime = blockEnd,
                        SlotName = matchingSlot.SlotName ?? (matchingSlot.IsPeakHour ? "Cao điểm" : "Thường"),
                        BasePrice = matchingSlot.Price,
                        Surcharge = matchingSlot.Surcharge,
                        MemberDiscount = isMember ? matchingSlot.MemberDiscount : 0,
                        IsPeakHour = matchingSlot.IsPeakHour,
                        Minutes = overlapMinutes,
                        SubTotal = blockPrice,
                        CalendarOverride = calendarOverride?.EventName
                    });
                }
                else
                {
                    // Không có slot giá cho khoảng này → cảnh báo
                    breakdown.Add(new PriceBreakdownItem
                    {
                        StartTime = currentTime,
                        EndTime = blockEnd,
                        SlotName = "⚠ Chưa có giá",
                        Minutes = (blockEnd - currentTime).TotalMinutes,
                        SubTotal = 0
                    });
                }

                currentTime = blockEnd;
            }

            result.IsValid = true;
            result.TotalPrice = Math.Round(totalPrice, 0); // Làm tròn VNĐ
            result.Breakdown = breakdown;
            result.CalendarEvent = calendarOverride?.EventName;
            result.TotalMinutes = totalMinutes;
            result.RoundingMinutes = rounding;

            return result;
        }

        // ══════════════════════════════════════════════════════════
        // VALIDATE BOOKING (Kiểm tra trước khi cho đặt)
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Kiểm tra xem booking có hợp lệ không:
        /// - Thời gian >= minimum duration
        /// - Không trùng buffer time với booking trước/sau
        /// - Không tạo slot mồ côi
        /// </summary>
        public async Task<ValidationResult> ValidateBooking(
            int courtId,
            DateTime bookingDate,
            TimeSpan startTime,
            TimeSpan endTime)
        {
            var result = new ValidationResult { IsValid = true };

            // 1. Check minimum duration
            var slots = await GetApplicableSlots(courtId, bookingDate.DayOfWeek, "All");
            if (slots.Any())
            {
                var minDuration = slots.Min(s => s.MinDurationMinutes);
                var duration = (endTime - startTime).TotalMinutes;
                if (duration < minDuration)
                {
                    result.IsValid = false;
                    result.Errors.Add($"Thời gian đặt tối thiểu là {minDuration} phút");
                }
            }

            // 2. Check buffer time với booking trước/sau
            var bufferMinutes = slots.FirstOrDefault()?.BufferMinutes ?? 0;
            if (bufferMinutes > 0)
            {
                var buffer = TimeSpan.FromMinutes(bufferMinutes);
                var existingBookings = await _context.Bookings
                    .Where(b => b.CourtId == courtId &&
                               b.BookingDate == bookingDate &&
                               b.Status != "Cancelled")
                    .ToListAsync();

                foreach (var existing in existingBookings)
                {
                    // Booking mới kết thúc quá gần booking cũ bắt đầu
                    if (endTime > existing.StartTime.Subtract(buffer) && endTime <= existing.StartTime)
                    {
                        result.IsValid = false;
                        result.Errors.Add($"Cần buffer {bufferMinutes} phút trước ca {existing.StartTime:hh\\:mm}");
                    }
                    // Booking mới bắt đầu quá gần booking cũ kết thúc
                    if (startTime >= existing.EndTime && startTime < existing.EndTime.Add(buffer))
                    {
                        result.IsValid = false;
                        result.Errors.Add($"Cần buffer {bufferMinutes} phút sau ca {existing.EndTime:hh\\:mm}");
                    }
                }
            }

            // 3. Check slot mồ côi: nếu booking tạo ra khoảng trống < minDuration
            if (slots.Any())
            {
                var minDuration = slots.Min(s => s.MinDurationMinutes);
                var existingBookings = await _context.Bookings
                    .Where(b => b.CourtId == courtId &&
                               b.BookingDate == bookingDate &&
                               b.Status != "Cancelled")
                    .OrderBy(b => b.StartTime)
                    .ToListAsync();

                // Check khoảng trống phía trước
                var prevBooking = existingBookings.LastOrDefault(b => b.EndTime <= startTime);
                if (prevBooking != null)
                {
                    var gapBefore = (startTime - prevBooking.EndTime).TotalMinutes;
                    if (gapBefore > 0 && gapBefore < minDuration)
                    {
                        result.Warnings.Add($"Khoảng trống {gapBefore:0} phút trước ca (< {minDuration}p tối thiểu) — có thể khó bán");
                    }
                }

                // Check khoảng trống phía sau
                var nextBooking = existingBookings.FirstOrDefault(b => b.StartTime >= endTime);
                if (nextBooking != null)
                {
                    var gapAfter = (nextBooking.StartTime - endTime).TotalMinutes;
                    if (gapAfter > 0 && gapAfter < minDuration)
                    {
                        result.Warnings.Add($"Khoảng trống {gapAfter:0} phút sau ca (< {minDuration}p tối thiểu) — có thể khó bán");
                    }
                }
            }

            return result;
        }

        // ══════════════════════════════════════════════════════════
        // PRIVATE HELPERS
        // ══════════════════════════════════════════════════════════

        /// <summary>Lấy tất cả PriceSlot áp dụng cho court + ngày + loại khách</summary>
        private async Task<List<PriceSlot>> GetApplicableSlots(
            int courtId, DayOfWeek dayOfWeek, string customerType)
        {
            var allSlots = await _context.PriceSlots
                .Where(s => s.CourtId == courtId && s.IsActive)
                .ToListAsync();

            return allSlots
                .Where(s => s.AppliesToDay(dayOfWeek))
                .Where(s => s.CustomerType == "All" || s.CustomerType == customerType)
                .OrderBy(s => s.StartTime)
                .ToList();
        }

        /// <summary>Lấy PriceCalendar override cho ngày cụ thể (nếu có)</summary>
        private async Task<PriceCalendar?> GetCalendarOverride(
            int facilityId, int courtId, DateTime date)
        {
            var overrides = await _context.Set<PriceCalendar>()
                .Where(c => c.IsActive &&
                            c.StartDate <= date.Date &&
                            c.EndDate >= date.Date)
                .ToListAsync();

            // Tìm override cụ thể nhất (ưu tiên: court > facility > global)
            return overrides
                .Where(c => c.AppliesToCourt(facilityId, courtId))
                .OrderByDescending(c => c.Priority)
                .ThenByDescending(c => c.CourtId.HasValue) // Court-specific trước
                .ThenByDescending(c => c.FacilityId.HasValue) // Facility-specific kế
                .FirstOrDefault();
        }
    }

    // ══════════════════════════════════════════════════════════
    // RESULT MODELS
    // ══════════════════════════════════════════════════════════

    /// <summary>Kết quả tính giá</summary>
    public class PricingResult
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }

        public int CourtId { get; set; }
        public DateTime BookingDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public bool IsMember { get; set; }

        public decimal TotalPrice { get; set; }
        public double TotalMinutes { get; set; }
        public int RoundingMinutes { get; set; }
        public string? CalendarEvent { get; set; }

        /// <summary>Chi tiết từng block giá (cho hiển thị breakdown)</summary>
        public List<PriceBreakdownItem> Breakdown { get; set; } = new();
    }

    /// <summary>Chi tiết 1 block giá trong breakdown</summary>
    public class PriceBreakdownItem
    {
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string SlotName { get; set; } = "";
        public decimal BasePrice { get; set; }
        public decimal Surcharge { get; set; }
        public decimal MemberDiscount { get; set; }
        public bool IsPeakHour { get; set; }
        public double Minutes { get; set; }
        public decimal SubTotal { get; set; }
        public string? CalendarOverride { get; set; }

        public string TimeDisplay => $"{StartTime:hh\\:mm}–{EndTime:hh\\:mm}";
    }

    /// <summary>Kết quả validate booking</summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }
}
