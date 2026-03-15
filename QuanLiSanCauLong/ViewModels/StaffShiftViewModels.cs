using System.ComponentModel.DataAnnotations;
using QuanLiSanCauLong.Models;

namespace QuanLiSanCauLong.ViewModels
{
    // ═══════════════════════════════════════════════════════════════════════
    // STAFF VIEW MODELS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Dùng cho trang Index — filter + pagination
    /// </summary>
    public class StaffIndexViewModel
    {
        public List<User> Staff { get; set; } = new();
        public StaffFilterViewModel Filter { get; set; } = new();
        public int TotalCount { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 15;

        // Stats
        public int ActiveCount { get; set; }
        public int LockedCount { get; set; }
        public int FacilityCount { get; set; }

        public bool HasPrevPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }

    /// <summary>
    /// Bộ lọc trang Index
    /// </summary>
    public class StaffFilterViewModel
    {
        public string? Search { get; set; }
        public int? FacilityId { get; set; }
        public string? Status { get; set; }
        public int Page { get; set; } = 1;
    }

    /// <summary>
    /// Form Tạo nhân viên mới
    /// </summary>
    public class StaffCreateViewModel
    {
        [Required(ErrorMessage = "Họ tên không được trống")]
        [StringLength(100, ErrorMessage = "Tối đa 100 ký tự")]
        [Display(Name = "Họ và Tên")]
        public string FullName { get; set; } = "";

        [Required(ErrorMessage = "Email không được trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(100)]
        [Display(Name = "Email")]
        public string Email { get; set; } = "";

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [StringLength(20)]
        [Display(Name = "Số điện thoại")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn cơ sở")]
        [Display(Name = "Cơ sở")]
        public int? FacilityId { get; set; }

        [Display(Name = "Trạng thái")]
        public string Status { get; set; } = "Active";

        [Required(ErrorMessage = "Mật khẩu không được trống")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6 ký tự trở lên")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu")]
        [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu")]
        public string ConfirmPassword { get; set; } = "";

        // Dropdowns — populated by controller
        public List<Facility> Facilities { get; set; } = new();
    }

    /// <summary>
    /// Form Sửa nhân viên
    /// </summary>
    public class StaffEditViewModel
    {
        public int UserId { get; set; }

        [Required(ErrorMessage = "Họ tên không được trống")]
        [StringLength(100)]
        [Display(Name = "Họ và Tên")]
        public string FullName { get; set; } = "";

        [Required(ErrorMessage = "Email không được trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(100)]
        [Display(Name = "Email")]
        public string Email { get; set; } = "";

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [StringLength(20)]
        [Display(Name = "Số điện thoại")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn cơ sở")]
        [Display(Name = "Cơ sở")]
        public int? FacilityId { get; set; }

        [Display(Name = "Trạng thái")]
        public string Status { get; set; } = "Active";

        public string? AvatarUrl { get; set; }

        // Đổi mật khẩu — không bắt buộc
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6 ký tự trở lên")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới")]
        public string? NewPassword { get; set; }

        [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu mới")]
        public string? ConfirmNewPassword { get; set; }

        // Dropdowns
        public List<Facility> Facilities { get; set; } = new();

        // Readonly display
        public string FullNameDisplay { get; set; } = "";
        public string RoleDisplay { get; set; } = "Staff";
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Trang hồ sơ chi tiết nhân viên
    /// </summary>
    public class StaffDetailsViewModel
    {
        public User Staff { get; set; } = null!;

        // Thống kê tháng này
        public int TotalShiftsThisMonth { get; set; }
        public double TotalHoursThisMonth { get; set; }
        public int AbsentCount { get; set; }
        public int LateCount { get; set; }

        // Lịch sử ca gần nhất
        public List<ShiftAssignment> RecentAssignments { get; set; } = new();

        // Tổng toàn thời gian
        public int TotalShiftsAllTime { get; set; }
        public double TotalHoursAllTime { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // SHIFT VIEW MODELS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Lịch phân ca theo tuần
    /// </summary>
    public class ShiftScheduleViewModel
    {
        public DateOnly WeekStart { get; set; }
        public DateOnly WeekEnd { get; set; }
        public int WeekOffset { get; set; }
        public int? FacilityId { get; set; }

        public List<Facility> Facilities { get; set; } = new();
        public List<User> StaffList { get; set; } = new();
        public List<Shift> Shifts { get; set; } = new();
        public List<ShiftAssignment> Assignments { get; set; } = new();

        // Helper: lấy assignments của 1 nhân viên trong 1 ngày
        public List<ShiftAssignment> GetAssignments(int userId, DateOnly date)
            => Assignments.Where(a => a.UserId == userId && a.WorkDate == date).ToList();

        // Tháng đang hiển thị (cho month view)
        public int CurrentMonth => WeekStart.Month;
        public int CurrentYear => WeekStart.Year;

        // Quick stats tuần
        public int TotalAssignments => Assignments.Count;
        public int CheckedOutCount => Assignments.Count(a => a.Status == "CheckedOut");
        public int AbsentCount => Assignments.Count(a => a.Status == "Absent");
    }

    /// <summary>
    /// Báo cáo chấm công tháng — 1 dòng / nhân viên
    /// </summary>
    public class ShiftReportViewModel
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public int? FacilityId { get; set; }

        public List<Facility> Facilities { get; set; } = new();
        public List<StaffMonthReportRow> Rows { get; set; } = new();

        // Summary
        public int TotalStaff => Rows.Count;
        public int TotalShifts => Rows.Sum(r => r.TotalShifts);
        public int TotalPresent => Rows.Sum(r => r.PresentShifts);
        public int TotalAbsent => Rows.Sum(r => r.AbsentShifts);
        public double TotalHours => Rows.Sum(r => r.TotalHours);
        public double AvgAttendRate => TotalShifts > 0 ? (double)TotalPresent / TotalShifts * 100 : 0;

        public string MonthLabel => new DateTime(Year, Month, 1).ToString("MMMM yyyy");
    }

    public class StaffMonthReportRow
    {
        public User Staff { get; set; } = null!;
        public int TotalShifts { get; set; }
        public int PresentShifts { get; set; }
        public int AbsentShifts { get; set; }
        public double TotalHours { get; set; }
        public double ScheduledHours { get; set; }

        public double AttendanceRate =>
            TotalShifts > 0 ? (double)PresentShifts / TotalShifts * 100 : 0;

        public string AttendanceLabel =>
            AttendanceRate >= 90 ? "Xuất sắc" :
            AttendanceRate >= 70 ? "Đạt" : "Cần cải thiện";

        public List<ShiftAssignment> Assignments { get; set; } = new();
    }

    /// <summary>
    /// AJAX response khi phân ca
    /// </summary>
    public class AssignShiftResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public int AssignmentId { get; set; }
        public string ShiftName { get; set; } = "";
        public string TimeRange { get; set; } = "";
        public string Color { get; set; } = "#d4a017";
    }
}
