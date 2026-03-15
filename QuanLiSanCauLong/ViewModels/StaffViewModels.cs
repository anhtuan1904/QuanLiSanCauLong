using System.ComponentModel.DataAnnotations;

namespace QuanLiSanCauLong.ViewModels
{
    // ── Sơ đồ sân ──
    public class StaffCourtStatusViewModel
    {
        public int CourtId { get; set; }
        public string CourtNumber { get; set; } = "";
        public string CourtType { get; set; } = "";
        public string Status { get; set; } = "free"; // free / playing / incoming / maintenance
        public string? Countdown { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? StartTime { get; set; }
        public string? EndTime { get; set; }
        public int? BookingId { get; set; }
        public string? NextTime { get; set; }
        public string? NextName { get; set; }
    }

    // ── Walk-in Booking ──
    public class WalkInBookingViewModel
    {
        [Required(ErrorMessage = "Vui lòng chọn sân")]
        public int CourtId { get; set; }

        [Required]
        public DateTime BookingDate { get; set; } = DateTime.Today;

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên khách")]
        [StringLength(100)]
        public string CustomerName { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [StringLength(20)]
        public string CustomerPhone { get; set; } = "";

        [Range(0, 10000000)]
        public decimal Price { get; set; }

        public string? PaymentMethod { get; set; } = "Cash";

        [StringLength(500)]
        public string? Note { get; set; }
    }

    // ── Bàn giao ca ──
    public class ShiftHandoverViewModel
    {
        public DateTime ShiftDate { get; set; } = DateTime.Today;
        public string ShiftType { get; set; } = "Morning";
        public decimal CashReceived { get; set; }
        public decimal TransferReceived { get; set; }
        public int TotalBookings { get; set; }
        public int TotalOrders { get; set; }

        [StringLength(1000)]
        public string? Issues { get; set; }

        [StringLength(1000)]
        public string? HandoverNote { get; set; }

        [StringLength(100)]
        public string? HandoverTo { get; set; }
    }

    // ── Báo sự cố ──
    public class IncidentReportViewModel
    {
        public int? CourtId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn loại sự cố")]
        public string IncidentType { get; set; } = "";
        // Light / Net / Floor / Equipment / Customer / Other

        [Required(ErrorMessage = "Vui lòng mô tả sự cố")]
        [StringLength(1000)]
        public string Description { get; set; } = "";

        public string Severity { get; set; } = "Normal";
        // Low / Normal / High / Critical

        [StringLength(500)]
        public string? ActionTaken { get; set; }
    }
}


// ── Sales / POS ──
public class SalesViewModel
{
    public int? BookingId { get; set; }   // Gắn vào booking nếu có
    public int FacilityId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string PaymentMethod { get; set; } = "Cash";
    public string? Note { get; set; }
    public List<SalesItemViewModel> Items { get; set; } = new();
}

public class SalesItemViewModel
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}
