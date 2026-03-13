using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiSanCauLong.Data;
using QuanLiSanCauLong.Helpers;
using QuanLiSanCauLong.Models;
using System.Security.Claims;

namespace QuanLiSanCauLong.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminUserController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminUserController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ─── Helper: lấy ID Admin đang đăng nhập ───────────────────────────────
        private int GetCurrentAdminId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out int id) ? id : 0;
        }

        // ─── INDEX ──────────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Index(string role, string search, bool? isActive)
        {
            // ✅ FIX: Chỉ lấy user chưa bị xóa mềm (Status != "Deleted")
            var query = _context.Users
                .Include(u => u.Facility)
                .Where(u => u.Status != "Deleted")
                .AsQueryable();

            if (!string.IsNullOrEmpty(role))
                query = query.Where(u => u.Role == role);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(u => u.FullName.Contains(search) ||
                                         u.Email.Contains(search) ||
                                         (u.Phone != null && u.Phone.Contains(search)));

            if (isActive.HasValue)
                query = query.Where(u => u.IsActive == isActive.Value);

            var users = await query.OrderBy(u => u.FullName).ToListAsync();

            ViewBag.TotalUsers = users.Count;
            ViewBag.ActiveUsers = users.Count(u => u.IsActive);
            ViewBag.InactiveUsers = users.Count(u => !u.IsActive);
            ViewBag.CurrentRole = role;
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentIsActive = isActive;

            return View(users);
        }

        // ─── CREATE GET ─────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Facilities = await _context.Facilities.Where(f => f.IsActive).ToListAsync();
            ViewBag.Roles = new[] { "Customer", "Staff", "Admin" };
            return View();
        }

        // ─── CREATE POST ────────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(User model, string password)
        {
            ModelState.Remove("Password");
            ModelState.Remove("PasswordHash");
            ModelState.Remove("Facility");
            ModelState.Remove("Bookings");
            ModelState.Remove("Orders");
            ModelState.Remove("Status");

            // ✅ FIX MỚI: Validate FacilityId bắt buộc khi Role = Staff
            if (model.Role == "Staff" && (model.FacilityId == null || model.FacilityId == 0))
            {
                ModelState.AddModelError("FacilityId", "Staff phải được gán vào một cơ sở!");
            }

            if (ModelState.IsValid)
            {
                if (await _context.Users.AnyAsync(u => u.Email == model.Email && u.Status != "Deleted"))
                {
                    ModelState.AddModelError("Email", "Email đã tồn tại!");
                    await ReloadCreateViewBag();
                    return View(model);
                }

                if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
                {
                    ModelState.AddModelError("password", "Mật khẩu phải có ít nhất 6 ký tự!");
                    await ReloadCreateViewBag();
                    return View(model);
                }

                // ✅ FIX: Không lưu password thuần vào DB — chỉ lưu hash
                model.Password = string.Empty; // Không lưu plain text
                model.PasswordHash = PasswordHelper.HashPassword(password);
                model.Status = "Active";
                model.CreatedAt = DateTime.Now;
                model.UpdatedAt = DateTime.Now;
                model.IsActive = true;

                // ✅ FIX: Reset FacilityId về null nếu không phải Staff
                if (model.Role != "Staff")
                    model.FacilityId = null;

                _context.Users.Add(model);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Tạo tài khoản \"{model.FullName}\" thành công!";
                return RedirectToAction(nameof(Index));
            }

            await ReloadCreateViewBag();
            return View(model);
        }

        // ─── DETAILS ────────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            if (id <= 0) return BadRequest();

            var user = await _context.Users
                .Include(u => u.Facility)
                .Include(u => u.Bookings)
                    .ThenInclude(b => b.Court)
                        .ThenInclude(c => c.Facility)
                .FirstOrDefaultAsync(u => u.UserId == id && u.Status != "Deleted");

            if (user == null) return NotFound();

            ViewBag.TotalBookings = user.Bookings?.Count ?? 0;
            ViewBag.CompletedBookings = user.Bookings?.Count(b => b.Status == "Completed") ?? 0;
            ViewBag.TotalSpent = user.Bookings?.Where(b => b.Status == "Completed").Sum(b => b.TotalPrice) ?? 0;
            ViewBag.LastBooking = user.Bookings?.OrderByDescending(b => b.CreatedAt).FirstOrDefault();

            return View("Details", user);
        }

        // ─── EDIT GET ───────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            if (id <= 0) return BadRequest();

            var user = await _context.Users.FindAsync(id);
            if (user == null || user.Status == "Deleted") return NotFound();

            ViewBag.Facilities = await _context.Facilities.Where(f => f.IsActive).ToListAsync();
            ViewBag.Roles = new[] { "Customer", "Staff", "Admin" };
            return View(user);
        }

        // ─── EDIT POST ──────────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(User model, string newPassword)
        {
            ModelState.Remove("Password");
            ModelState.Remove("PasswordHash");
            ModelState.Remove("Facility");
            ModelState.Remove("Bookings");
            ModelState.Remove("Orders");
            ModelState.Remove("Status");

            // ✅ FIX MỚI: Validate FacilityId bắt buộc khi Role = Staff
            if (model.Role == "Staff" && (model.FacilityId == null || model.FacilityId == 0))
            {
                ModelState.AddModelError("FacilityId", "Staff phải được gán vào một cơ sở!");
            }

            if (ModelState.IsValid)
            {
                var user = await _context.Users.FindAsync(model.UserId);
                if (user == null || user.Status == "Deleted")
                    return NotFound();

                // ✅ FIX: Kiểm tra trùng email, bỏ qua user bị xóa mềm
                if (await _context.Users.AnyAsync(u => u.Email == model.Email
                    && u.UserId != model.UserId
                    && u.Status != "Deleted"))
                {
                    ModelState.AddModelError("Email", "Email đã tồn tại!");
                    await ReloadEditViewBag();
                    return View(model);
                }

                user.FullName = model.FullName;
                user.Email = model.Email;
                user.Phone = model.Phone;
                user.Role = model.Role;
                // ✅ FIX: Reset FacilityId về null nếu không phải Staff
                user.FacilityId = model.Role == "Staff" ? model.FacilityId : null;
                user.IsActive = model.IsActive;
                user.Status = model.IsActive ? "Active" : "Locked";
                user.UpdatedAt = DateTime.Now;

                if (!string.IsNullOrEmpty(newPassword))
                {
                    if (newPassword.Length < 6)
                    {
                        ModelState.AddModelError("newPassword", "Mật khẩu phải có ít nhất 6 ký tự!");
                        await ReloadEditViewBag();
                        return View(model);
                    }
                    user.Password = string.Empty; // Không lưu plain text
                    user.PasswordHash = PasswordHelper.HashPassword(newPassword);
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Cập nhật \"{user.FullName}\" thành công!";
                return RedirectToAction(nameof(Index));
            }

            await ReloadEditViewBag();
            return View(model);
        }

        // ─── TOGGLE STATUS ──────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int userId, bool isActive)
        {
            if (userId <= 0)
                return Json(new { success = false, message = "ID không hợp lệ!" });

            var user = await _context.Users.FindAsync(userId);
            if (user == null || user.Status == "Deleted")
                return Json(new { success = false, message = "Không tìm thấy người dùng!" });

            // ✅ FIX MỚI: Không cho phép tự khóa tài khoản Admin đang đăng nhập
            if (userId == GetCurrentAdminId() && !isActive)
                return Json(new { success = false, message = "Bạn không thể tự khóa tài khoản của mình!" });

            user.IsActive = isActive;
            user.Status = isActive ? "Active" : "Locked";
            user.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = isActive ? "Mở khóa tài khoản thành công!" : "Khóa tài khoản thành công!"
            });
        }

        // ─── RESET PASSWORD ─────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(int userId)
        {
            if (userId <= 0)
                return Json(new { success = false, message = "ID không hợp lệ!" });

            var user = await _context.Users.FindAsync(userId);
            if (user == null || user.Status == "Deleted")
                return Json(new { success = false, message = "Không tìm thấy người dùng!" });

            string defaultPassword = "123456";
            user.Password = string.Empty;
            user.PasswordHash = PasswordHelper.HashPassword(defaultPassword);
            user.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = $"Reset thành công! Mật khẩu mới: {defaultPassword}"
            });
        }

        // ─── DELETE (Soft Delete) ───────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
                return Json(new { success = false, message = "ID không hợp lệ!" });

            // ✅ FIX MỚI: Không cho phép tự xóa tài khoản đang đăng nhập
            if (id == GetCurrentAdminId())
                return Json(new { success = false, message = "Bạn không thể xóa tài khoản của chính mình!" });

            var user = await _context.Users
                .Include(u => u.Bookings)
                .FirstOrDefaultAsync(u => u.UserId == id && u.Status != "Deleted");

            if (user == null)
                return Json(new { success = false, message = "Không tìm thấy người dùng!" });

            if (user.Bookings != null && user.Bookings.Any())
            {
                // ✅ NÂNG CẤP: Soft Delete thay vì hard delete khi có booking
                user.IsActive = false;
                user.Status = "Deleted";
                user.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Đã ẩn người dùng (có lịch đặt, không xóa vĩnh viễn)." });
            }

            // Hard delete nếu chưa có booking nào
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Xóa người dùng thành công!" });
        }

        // ─── PERMISSIONS GET ────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Permissions(int id)
        {
            if (id <= 0) return BadRequest();

            var user = await _context.Users
                .Include(u => u.Facility)
                .FirstOrDefaultAsync(u => u.UserId == id && u.Status != "Deleted");

            if (user == null) return NotFound();

            ViewBag.Permissions = new List<string>();
            return View(user);
        }

        // ─── SAVE PERMISSIONS ───────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SavePermissions(int UserId, List<string> permissions)
        {
            // TODO: Lưu permissions vào DB (bảng UserPermissions hoặc cột JSON)
            // Hiện tại placeholder — tích hợp thêm khi có bảng Permission
            TempData["SuccessMessage"] = "Cập nhật quyền thành công!";
            return RedirectToAction(nameof(Details), new { id = UserId });
        }

        // ─── PRIVATE HELPERS ────────────────────────────────────────────────────
        private async Task ReloadCreateViewBag()
        {
            ViewBag.Facilities = await _context.Facilities.Where(f => f.IsActive).ToListAsync();
            ViewBag.Roles = new[] { "Customer", "Staff", "Admin" };
        }

        private async Task ReloadEditViewBag()
        {
            ViewBag.Facilities = await _context.Facilities.Where(f => f.IsActive).ToListAsync();
            ViewBag.Roles = new[] { "Customer", "Staff", "Admin" };
        }
    }
}
