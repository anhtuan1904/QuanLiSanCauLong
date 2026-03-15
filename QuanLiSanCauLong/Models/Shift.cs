using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLiSanCauLong.Models
{
    /// <summary>
    /// Định nghĩa một ca làm việc (VD: Ca Sáng 6:00–12:00)
    /// </summary>
    public class Shift
    {
        [Key]
        public int ShiftId { get; set; }

        [Required, StringLength(100)]
        public string ShiftName { get; set; } = "";          // "Ca Sáng", "Ca Chiều", "Ca Tối"

        [Required]
        public TimeSpan StartTime { get; set; }              // 06:00

        [Required]
        public TimeSpan EndTime { get; set; }                // 12:00

        [StringLength(7)]
        public string Color { get; set; } = "#d4a017";       // Màu hiển thị trên lịch

        [StringLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        public virtual ICollection<ShiftAssignment>? Assignments { get; set; }

        // Helper
        [NotMapped]
        public string TimeRange =>
            $"{StartTime:hh\\:mm} – {EndTime:hh\\:mm}";

        [NotMapped]
        public double HoursPerShift =>
            (EndTime - StartTime).TotalHours;
    }

    /// <summary>
    /// Phân ca: 1 nhân viên — 1 ca — 1 ngày cụ thể
    /// </summary>
    public class ShiftAssignment
    {
        [Key]
        public int AssignmentId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int ShiftId { get; set; }

        [Required]
        public int? FacilityId { get; set; }

        [Required]
        public DateOnly WorkDate { get; set; }

        // Trạng thái: Scheduled | CheckedIn | CheckedOut | Absent | Late
        [StringLength(20)]
        public string Status { get; set; } = "Scheduled";

        public DateTime? CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }

        [StringLength(500)]
        public string? Note { get; set; }

        public int CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Navigation
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [ForeignKey("ShiftId")]
        public virtual Shift? Shift { get; set; }

        [ForeignKey("FacilityId")]
        public virtual Facility? Facility { get; set; }

        // Helper
        [NotMapped]
        public double? ActualHours =>
            (CheckInTime.HasValue && CheckOutTime.HasValue)
            ? (CheckOutTime.Value - CheckInTime.Value).TotalHours
            : null;
    }
}
