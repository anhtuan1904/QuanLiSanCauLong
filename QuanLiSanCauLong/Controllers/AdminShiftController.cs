using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiSanCauLong.Data;
using QuanLiSanCauLong.Models;
using QuanLiSanCauLong.ViewModels;
using System.Security.Claims;

namespace QuanLiSanCauLong.Controllers
{
    /// <summary>
    /// Quản lý ca làm việc & phân ca — route: /AdminShift
    /// </summary>
    [Authorize(Roles = "Admin")]
    [Route("AdminShift/{action=Schedule}/{id?}")]
    public class AdminShiftController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminShiftController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ── LỊCH PHÂN CA (tuần + tháng) ────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Schedule(int? facilityId, int? weekOffset)
        {
            weekOffset ??= 0;

            var today = DateTime.Today;
            var dow = (int)today.DayOfWeek;
            var daysToMon = dow == 0 ? -6 : 1 - dow;
            var weekStart = DateOnly.FromDateTime(today.AddDays(daysToMon + weekOffset.Value * 7));
            var weekEnd = weekStart.AddDays(6);

            var facilities = await _context.Facilities
                .Where(f => f.IsActive).OrderBy(f => f.FacilityName).ToListAsync();

            facilityId ??= facilities.FirstOrDefault()?.FacilityId;

            var staffList = await _context.Users
                .Include(u => u.Facility)
                .Where(u => u.Role == "Staff" && u.IsActive &&
                            u.Status == "Active" &&
                            (!facilityId.HasValue || u.FacilityId == facilityId))
                .OrderBy(u => u.FullName)
                .ToListAsync();

            var shifts = await _context.Shifts
                .Where(s => s.IsActive)
                .OrderBy(s => s.StartTime)
                .ToListAsync();

            var assignments = await _context.ShiftAssignments
                .Include(a => a.Shift)
                .Include(a => a.User)
                .Where(a => a.WorkDate >= weekStart && a.WorkDate <= weekEnd &&
                            (!facilityId.HasValue || a.FacilityId == facilityId))
                .ToListAsync();

            var vm = new ShiftScheduleViewModel
            {
                WeekStart = weekStart,
                WeekEnd = weekEnd,
                WeekOffset = weekOffset.Value,
                FacilityId = facilityId,
                Facilities = facilities,
                StaffList = staffList,
                Shifts = shifts,
                Assignments = assignments
            };

            return View(vm);
        }

        // ── DANH SÁCH CA ──────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Shifts()
        {
            var shifts = await _context.Shifts
                .Where(s => s.IsActive)
                .OrderBy(s => s.StartTime)
                .ToListAsync();
            return View(shifts);
        }

        // ── TẠO CA (AJAX) ──────────────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateShift(
            string shiftName, string startTime, string endTime,
            string color, string? description)
        {
            if (string.IsNullOrWhiteSpace(shiftName))
                return Json(new { success = false, message = "Tên ca không được trống!" });

            if (!TimeSpan.TryParse(startTime, out var start) ||
                !TimeSpan.TryParse(endTime, out var end))
                return Json(new { success = false, message = "Giờ không hợp lệ!" });

            if (end <= start)
                return Json(new { success = false, message = "Giờ kết thúc phải sau giờ bắt đầu!" });

            var shift = new Shift
            {
                ShiftName = shiftName.Trim(),
                StartTime = start,
                EndTime = end,
                Color = string.IsNullOrWhiteSpace(color) ? "#d4a017" : color,
                Description = description?.Trim(),
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.Shifts.Add(shift);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = "Đã tạo ca thành công!",
                shiftId = shift.ShiftId,
                shiftName = shift.ShiftName,
                timeRange = shift.TimeRange,
                color = shift.Color,
                hours = shift.HoursPerShift.ToString("0.0")
            });
        }

        // ── CẬP NHẬT CA (AJAX) ────────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateShift(
            int shiftId, string shiftName, string startTime,
            string endTime, string color, string? description)
        {
            var shift = await _context.Shifts.FindAsync(shiftId);
            if (shift == null)
                return Json(new { success = false, message = "Không tìm thấy ca!" });

            if (!TimeSpan.TryParse(startTime, out var start) ||
                !TimeSpan.TryParse(endTime, out var end))
                return Json(new { success = false, message = "Giờ không hợp lệ!" });

            if (end <= start)
                return Json(new { success = false, message = "Giờ kết thúc phải sau giờ bắt đầu!" });

            shift.ShiftName = shiftName.Trim();
            shift.StartTime = start;
            shift.EndTime = end;
            shift.Color = color;
            shift.Description = description?.Trim();
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = "Đã cập nhật ca!",
                timeRange = shift.TimeRange,
                hours = shift.HoursPerShift.ToString("0.0")
            });
        }

        // ── XÓA CA (AJAX) ─────────────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteShift(int shiftId)
        {
            var shift = await _context.Shifts
                .Include(s => s.Assignments)
                .FirstOrDefaultAsync(s => s.ShiftId == shiftId);

            if (shift == null)
                return Json(new { success = false, message = "Không tìm thấy ca!" });

            if (shift.Assignments != null && shift.Assignments.Any())
            {
                shift.IsActive = false;
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Đã ẩn ca (còn lịch sử phân công)." });
            }

            _context.Shifts.Remove(shift);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã xóa ca!" });
        }

        // ── PHÂN CA (AJAX) ────────────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(
            int userId, int shiftId, int facilityId, string workDate)
        {
            if (!DateOnly.TryParse(workDate, out var date))
                return Json(new { success = false, message = "Ngày không hợp lệ!" });

            var user = await _context.Users.FindAsync(userId);
            var shift = await _context.Shifts.FindAsync(shiftId);

            if (user == null || shift == null)
                return Json(new { success = false, message = "Nhân viên hoặc ca không tồn tại!" });

            // Kiểm tra trùng ca
            var exists = await _context.ShiftAssignments.AnyAsync(a =>
                a.UserId == userId && a.WorkDate == date && a.ShiftId == shiftId);

            if (exists)
                return Json(new { success = false, message = "Nhân viên đã được phân ca này trong ngày!" });

            var assignment = new ShiftAssignment
            {
                UserId = userId,
                ShiftId = shiftId,
                FacilityId = facilityId,
                WorkDate = date,
                Status = "Scheduled",
                CreatedByUserId = GetCurrentUserId(),
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.ShiftAssignments.Add(assignment);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                assignmentId = assignment.AssignmentId,
                shiftName = shift.ShiftName,
                timeRange = shift.TimeRange,
                color = shift.Color,
                message = "Đã phân ca!"
            });
        }

        // ── XÓA PHÂN CA (AJAX) ────────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveAssignment(int assignmentId)
        {
            var a = await _context.ShiftAssignments.FindAsync(assignmentId);
            if (a == null)
                return Json(new { success = false, message = "Không tìm thấy phân công!" });

            if (a.Status is "CheckedIn" or "CheckedOut")
                return Json(new { success = false, message = "Không thể xóa ca đã chấm công!" });

            _context.ShiftAssignments.Remove(a);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã xóa phân ca!" });
        }

        // ── CHẤM CÔNG VÀO (AJAX) ──────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckIn(int assignmentId)
        {
            var a = await _context.ShiftAssignments
                .Include(x => x.Shift)
                .FirstOrDefaultAsync(x => x.AssignmentId == assignmentId);

            if (a == null) return Json(new { success = false, message = "Không tìm thấy!" });
            if (a.Status == "CheckedIn")
                return Json(new { success = false, message = "Đã check-in rồi!" });

            a.CheckInTime = DateTime.Now;
            a.Status = "CheckedIn";
            a.UpdatedAt = DateTime.Now;

            // Kiểm tra đi trễ
            if (a.Shift != null)
            {
                var scheduledStart = a.WorkDate.ToDateTime(TimeOnly.FromTimeSpan(a.Shift.StartTime));
                if (DateTime.Now > scheduledStart.AddMinutes(15))
                    a.Status = "Late";
            }

            await _context.SaveChangesAsync();
            return Json(new
            {
                success = true,
                status = a.Status,
                checkInTime = a.CheckInTime?.ToString("HH:mm"),
                message = a.Status == "Late" ? "Chấm công — Đi trễ!" : "Chấm công vào thành công!"
            });
        }

        // ── CHẤM CÔNG RA (AJAX) ───────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckOut(int assignmentId)
        {
            var a = await _context.ShiftAssignments.FindAsync(assignmentId);
            if (a == null) return Json(new { success = false, message = "Không tìm thấy!" });

            a.CheckOutTime = DateTime.Now;
            a.Status = "CheckedOut";
            a.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                checkOutTime = a.CheckOutTime?.ToString("HH:mm"),
                actualHours = a.ActualHours?.ToString("0.0"),
                message = $"Chấm công ra — Làm {a.ActualHours:0.0} giờ!"
            });
        }

        // ── BÁO CÁO CHẤM CÔNG ────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Report(int? month, int? year, int? facilityId)
        {
            var now = DateTime.Today;
            month ??= now.Month;
            year ??= now.Year;

            var monthStart = new DateOnly(year.Value, month.Value, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            var assignments = await _context.ShiftAssignments
                .Include(a => a.User).ThenInclude(u => u!.Facility)
                .Include(a => a.Shift)
                .Where(a => a.WorkDate >= monthStart && a.WorkDate <= monthEnd &&
                            (!facilityId.HasValue || a.FacilityId == facilityId) &&
                            a.User != null && a.User.Role == "Staff" && a.User.Status != "Deleted")
                .OrderBy(a => a.User!.FullName).ThenBy(a => a.WorkDate)
                .ToListAsync();

            var rows = assignments
                .GroupBy(a => a.User!)
                .Select(g => new StaffMonthReportRow
                {
                    Staff = g.Key,
                    TotalShifts = g.Count(),
                    PresentShifts = g.Count(a => a.Status is "CheckedIn" or "CheckedOut" or "Late"),
                    AbsentShifts = g.Count(a => a.Status == "Absent"),
                    TotalHours = g.Sum(a => a.ActualHours ?? 0),
                    ScheduledHours = g.Sum(a => a.Shift?.HoursPerShift ?? 0),
                    Assignments = g.ToList()
                })
                .OrderBy(r => r.Staff.FullName)
                .ToList();

            var vm = new ShiftReportViewModel
            {
                Month = month.Value,
                Year = year.Value,
                FacilityId = facilityId,
                Facilities = await _context.Facilities
                    .Where(f => f.IsActive).OrderBy(f => f.FacilityName).ToListAsync(),
                Rows = rows
            };

            return View(vm);
        }

        // ── CA CỦA TÔI (Staff xem lịch cá nhân) ──────────────────────────
        [Authorize(Roles = "Admin,Staff")]
        [HttpGet]
        public async Task<IActionResult> MySchedule()
        {
            var userId = GetCurrentUserId();
            var start = DateOnly.FromDateTime(DateTime.Today);
            var end = start.AddDays(13);

            var assignments = await _context.ShiftAssignments
                .Include(a => a.Shift)
                .Include(a => a.Facility)
                .Where(a => a.UserId == userId && a.WorkDate >= start && a.WorkDate <= end)
                .OrderBy(a => a.WorkDate)
                .ToListAsync();

            return View(assignments);
        }

        // ── HELPER ────────────────────────────────────────────────────────
        private int GetCurrentUserId()
        {
            var c = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(c, out int id) ? id : 0;
        }
    }
}
