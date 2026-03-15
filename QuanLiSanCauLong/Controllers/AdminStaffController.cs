using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLiSanCauLong.Data;
using QuanLiSanCauLong.Helpers;
using QuanLiSanCauLong.Models;
using QuanLiSanCauLong.ViewModels;
using System.Security.Claims;

namespace QuanLiSanCauLong.Controllers
{
    /// <summary>
    /// Quản lý nhân viên (Staff) — route: /AdminStaff
    /// Tách biệt hoàn toàn với AdminUserController (quản lý Customer/Admin)
    /// </summary>
    [Authorize(Roles = "Admin")]
    [Route("AdminStaff/{action=Index}/{id?}")]
    public class AdminStaffController : Controller
    {
        private readonly ApplicationDbContext _context;
        private const int PAGE_SIZE = 15;

        public AdminStaffController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ── INDEX ──────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Index(StaffFilterViewModel filter)
        {
            var query = _context.Users
                .Include(u => u.Facility)
                .Where(u => u.Role == "Staff" && u.Status != "Deleted")
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Search))
                query = query.Where(u =>
                    u.FullName.Contains(filter.Search) ||
                    u.Email.Contains(filter.Search) ||
                    (u.Phone != null && u.Phone.Contains(filter.Search)));

            if (filter.FacilityId.HasValue)
                query = query.Where(u => u.FacilityId == filter.FacilityId);

            if (!string.IsNullOrWhiteSpace(filter.Status))
                query = query.Where(u => u.Status == filter.Status);

            var total = await query.CountAsync();
            var page = Math.Max(1, filter.Page);
            var staff = await query
                .OrderBy(u => u.FullName)
                .Skip((page - 1) * PAGE_SIZE)
                .Take(PAGE_SIZE)
                .ToListAsync();

            var allFiltered = await query.ToListAsync();

            var vm = new StaffIndexViewModel
            {
                Staff = staff,
                Filter = filter,
                TotalCount = total,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(total / (double)PAGE_SIZE),
                PageSize = PAGE_SIZE,
                ActiveCount = allFiltered.Count(u => u.IsActive),
                LockedCount = allFiltered.Count(u => !u.IsActive),
                FacilityCount = allFiltered.Where(u => u.FacilityId.HasValue)
                                           .Select(u => u.FacilityId).Distinct().Count()
            };

            ViewBag.Facilities = await _context.Facilities
                .Where(f => f.IsActive).OrderBy(f => f.FacilityName).ToListAsync();

            return View(vm);
        }

        // ── DETAILS ────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            if (id <= 0) return BadRequest();

            var staff = await _context.Users
                .Include(u => u.Facility)
                .FirstOrDefaultAsync(u => u.UserId == id && u.Role == "Staff" && u.Status != "Deleted");

            if (staff == null) return NotFound();

            var now = DateOnly.FromDateTime(DateTime.Today);
            var monthStart = new DateOnly(now.Year, now.Month, 1);

            var allAssignments = await _context.ShiftAssignments
                .Include(a => a.Shift)
                .Include(a => a.Facility)
                .Where(a => a.UserId == id)
                .OrderByDescending(a => a.WorkDate)
                .ToListAsync();

            var monthAssignments = allAssignments.Where(a => a.WorkDate >= monthStart).ToList();

            var vm = new StaffDetailsViewModel
            {
                Staff = staff,
                TotalShiftsThisMonth = monthAssignments.Count,
                TotalHoursThisMonth = monthAssignments.Sum(a => a.ActualHours ?? 0),
                AbsentCount = monthAssignments.Count(a => a.Status == "Absent"),
                LateCount = monthAssignments.Count(a => a.Status == "Late"),
                RecentAssignments = allAssignments.Take(10).ToList(),
                TotalShiftsAllTime = allAssignments.Count,
                TotalHoursAllTime = allAssignments.Sum(a => a.ActualHours ?? 0)
            };

            return View(vm);
        }

        // ── CREATE GET ─────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var vm = new StaffCreateViewModel
            {
                Status = "Active",
                Facilities = await GetFacilitiesAsync()
            };
            return View(vm);
        }

        // ── CREATE POST ────────────────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StaffCreateViewModel vm)
        {
            // Email trùng lặp
            if (await _context.Users.AnyAsync(u => u.Email == vm.Email))
                ModelState.AddModelError("Email", "Email này đã được sử dụng!");

            if (!ModelState.IsValid)
            {
                vm.Facilities = await GetFacilitiesAsync();
                return View(vm);
            }

            var user = new User
            {
                FullName = vm.FullName.Trim(),
                Email = vm.Email.Trim().ToLower(),
                Phone = vm.Phone?.Trim(),
                FacilityId = vm.FacilityId,
                Role = "Staff",
                Status = vm.Status,
                IsActive = vm.Status == "Active",
                PasswordHash = PasswordHelper.HashPassword(vm.Password),
                Password = string.Empty,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã thêm nhân viên {user.FullName} thành công!";
            return RedirectToAction(nameof(Details), new { id = user.UserId });
        }

        // ── EDIT GET ───────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            if (id <= 0) return BadRequest();

            var staff = await _context.Users
                .Include(u => u.Facility)
                .FirstOrDefaultAsync(u => u.UserId == id && u.Role == "Staff" && u.Status != "Deleted");

            if (staff == null) return NotFound();

            var vm = new StaffEditViewModel
            {
                UserId = staff.UserId,
                FullName = staff.FullName,
                Email = staff.Email,
                Phone = staff.Phone,
                FacilityId = staff.FacilityId,
                Status = staff.Status,
                AvatarUrl = staff.AvatarUrl,
                FullNameDisplay = staff.FullName,
                CreatedAt = staff.CreatedAt,
                Facilities = await GetFacilitiesAsync(staff.FacilityId)
            };

            return View(vm);
        }

        // ── EDIT POST ──────────────────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, StaffEditViewModel vm)
        {
            if (id != vm.UserId) return BadRequest();

            // Xóa validation mật khẩu nếu không đổi
            if (string.IsNullOrEmpty(vm.NewPassword))
            {
                ModelState.Remove("NewPassword");
                ModelState.Remove("ConfirmNewPassword");
            }

            if (!ModelState.IsValid)
            {
                vm.Facilities = await GetFacilitiesAsync(vm.FacilityId);
                return View(vm);
            }

            var existing = await _context.Users.FindAsync(id);
            if (existing == null || existing.Status == "Deleted") return NotFound();

            existing.FullName = vm.FullName.Trim();
            existing.Email = vm.Email.Trim().ToLower();
            existing.Phone = vm.Phone?.Trim();
            existing.FacilityId = vm.FacilityId;
            existing.Status = vm.Status;
            existing.IsActive = vm.Status == "Active";
            existing.AvatarUrl = vm.AvatarUrl;
            existing.UpdatedAt = DateTime.Now;

            if (!string.IsNullOrWhiteSpace(vm.NewPassword))
                existing.PasswordHash = PasswordHelper.HashPassword(vm.NewPassword);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã cập nhật thông tin {existing.FullName}!";
            return RedirectToAction(nameof(Details), new { id });
        }

        // ── TOGGLE STATUS (AJAX) ───────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int userId)
        {
            var staff = await _context.Users.FindAsync(userId);
            if (staff == null || staff.Role != "Staff")
                return Json(new { success = false, message = "Không tìm thấy nhân viên!" });

            // Không được khóa chính mình
            if (userId == GetCurrentUserId())
                return Json(new { success = false, message = "Không thể tự khóa tài khoản của mình!" });

            staff.IsActive = !staff.IsActive;
            staff.Status = staff.IsActive ? "Active" : "Locked";
            staff.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                isActive = staff.IsActive,
                message = staff.IsActive ? "Đã mở khóa tài khoản!" : "Đã khóa tài khoản!"
            });
        }

        // ── DELETE (AJAX) ──────────────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int userId)
        {
            var staff = await _context.Users
                .Include(u => u.Bookings)
                .FirstOrDefaultAsync(u => u.UserId == userId && u.Role == "Staff");

            if (staff == null)
                return Json(new { success = false, message = "Không tìm thấy nhân viên!" });

            var hasAssignments = await _context.ShiftAssignments.AnyAsync(a => a.UserId == userId);
            var hasBookings = staff.Bookings != null && staff.Bookings.Any();

            if (hasAssignments || hasBookings)
            {
                staff.Status = "Deleted";
                staff.IsActive = false;
                staff.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                return Json(new { success = true, softDelete = true, message = "Đã ẩn nhân viên (còn dữ liệu lịch sử)." });
            }

            _context.Users.Remove(staff);
            await _context.SaveChangesAsync();
            return Json(new { success = true, softDelete = false, message = "Đã xóa nhân viên!" });
        }

        // ── RESET PASSWORD (AJAX) ──────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(int userId)
        {
            var staff = await _context.Users.FindAsync(userId);
            if (staff == null || staff.Role != "Staff")
                return Json(new { success = false, message = "Không tìm thấy nhân viên!" });

            staff.PasswordHash = PasswordHelper.HashPassword("123456");
            staff.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã reset mật khẩu về 123456!" });
        }

        // ── PRIVATE HELPERS ────────────────────────────────────────────────
        private async Task<List<Facility>> GetFacilitiesAsync(int? selectedId = null)
        {
            return await _context.Facilities
                .Where(f => f.IsActive)
                .OrderBy(f => f.FacilityName)
                .ToListAsync();
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out int id) ? id : 0;
        }
    }
}
