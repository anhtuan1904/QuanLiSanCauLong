using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiSanCauLong.Data;
using QuanLiSanCauLong.Helpers;
using QuanLiSanCauLong.Models;

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

        [HttpGet]
        public async Task<IActionResult> Index(string role, string search, bool? isActive)
        {
            var query = _context.Users.Include(u => u.Facility).AsQueryable();

            if (!string.IsNullOrEmpty(role))
                query = query.Where(u => u.Role == role);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(u => u.FullName.Contains(search) ||
                                        u.Email.Contains(search) ||
                                        u.Phone.Contains(search));

            if (isActive.HasValue)
                query = query.Where(u => u.IsActive == isActive.Value);

            var users = await query.OrderBy(u => u.FullName).ToListAsync();

            ViewBag.Roles = new[] { "Customer", "Staff", "Admin" };
            ViewBag.TotalUsers = users.Count;
            ViewBag.ActiveUsers = users.Count(u => u.IsActive);
            ViewBag.InactiveUsers = users.Count(u => !u.IsActive);

            return View(users);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Facilities = await _context.Facilities.Where(f => f.IsActive).ToListAsync();
            ViewBag.Roles = new[] { "Customer", "Staff", "Admin" };
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(User model, string password)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra email trùng
                if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Email đã tồn tại!");
                    ViewBag.Facilities = await _context.Facilities.Where(f => f.IsActive).ToListAsync();
                    ViewBag.Roles = new[] { "Customer", "Staff", "Admin" };
                    return View(model);
                }

                // Validate password
                if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
                {
                    ModelState.AddModelError("password", "Mật khẩu phải có ít nhất 6 ký tự!");
                    ViewBag.Facilities = await _context.Facilities.Where(f => f.IsActive).ToListAsync();
                    ViewBag.Roles = new[] { "Customer", "Staff", "Admin" };
                    return View(model);
                }

                model.PasswordHash = PasswordHelper.HashPassword(password);
                model.CreatedAt = DateTime.Now;
                model.UpdatedAt = DateTime.Now;
                model.IsActive = true;

                _context.Users.Add(model);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Tạo người dùng thành công!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Facilities = await _context.Facilities.Where(f => f.IsActive).ToListAsync();
            ViewBag.Roles = new[] { "Customer", "Staff", "Admin" };
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var user = await _context.Users
                .Include(u => u.Facility)
                .Include(u => u.Bookings)
                    .ThenInclude(b => b.Court)
                        .ThenInclude(c => c.Facility)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null)
                return NotFound();

            // Thống kê người dùng
            ViewBag.TotalBookings = user.Bookings.Count;
            ViewBag.CompletedBookings = user.Bookings.Count(b => b.Status == "Completed");
            ViewBag.TotalSpent = user.Bookings.Where(b => b.Status == "Completed").Sum(b => b.TotalPrice);
            ViewBag.LastBooking = user.Bookings.OrderByDescending(b => b.CreatedAt).FirstOrDefault();

            return View(user);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            ViewBag.Facilities = await _context.Facilities.Where(f => f.IsActive).ToListAsync();
            ViewBag.Roles = new[] { "Customer", "Staff", "Admin" };
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(User model, string newPassword)
        {
            if (ModelState.IsValid)
            {
                var user = await _context.Users.FindAsync(model.UserId);
                if (user != null)
                {
                    // Kiểm tra email trùng (ngoại trừ chính nó)
                    if (await _context.Users.AnyAsync(u => u.Email == model.Email && u.UserId != model.UserId))
                    {
                        ModelState.AddModelError("Email", "Email đã tồn tại!");
                        ViewBag.Facilities = await _context.Facilities.Where(f => f.IsActive).ToListAsync();
                        ViewBag.Roles = new[] { "Customer", "Staff", "Admin" };
                        return View(model);
                    }

                    user.FullName = model.FullName;
                    user.Email = model.Email;
                    user.Phone = model.Phone;
                    user.Role = model.Role;
                    user.FacilityId = model.FacilityId;
                    user.IsActive = model.IsActive;
                    user.UpdatedAt = DateTime.Now;

                    // Cập nhật mật khẩu nếu có
                    if (!string.IsNullOrEmpty(newPassword))
                    {
                        if (newPassword.Length < 6)
                        {
                            ModelState.AddModelError("newPassword", "Mật khẩu phải có ít nhất 6 ký tự!");
                            ViewBag.Facilities = await _context.Facilities.Where(f => f.IsActive).ToListAsync();
                            ViewBag.Roles = new[] { "Customer", "Staff", "Admin" };
                            return View(model);
                        }

                        user.PasswordHash = PasswordHelper.HashPassword(newPassword);
                    }

                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Cập nhật người dùng thành công!";
                    return RedirectToAction(nameof(Index));
                }
            }

            ViewBag.Facilities = await _context.Facilities.Where(f => f.IsActive).ToListAsync();
            ViewBag.Roles = new[] { "Customer", "Staff", "Admin" };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int userId, bool isActive)
        {
            if (userId <= 0)
                return Json(new { success = false, message = "ID không hợp lệ!" });

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return Json(new { success = false, message = "Không tìm thấy người dùng!" });

            user.IsActive = isActive;
            user.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = isActive ? "Mở khóa tài khoản thành công!" : "Khóa tài khoản thành công!"
            });
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(int userId)
        {
            if (userId <= 0)
                return Json(new { success = false, message = "ID không hợp lệ!" });

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return Json(new { success = false, message = "Không tìm thấy người dùng!" });

            // Reset password về mặc định
            string defaultPassword = "123456";
            user.PasswordHash = PasswordHelper.HashPassword(defaultPassword);
            user.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = $"Reset mật khẩu thành công! Mật khẩu mới: {defaultPassword}"
            });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
                return Json(new { success = false, message = "ID không hợp lệ!" });

            var user = await _context.Users
                .Include(u => u.Bookings)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null)
                return Json(new { success = false, message = "Không tìm thấy người dùng!" });

            if (user.Bookings.Any())
                return Json(new { success = false, message = "Không thể xóa người dùng đã có lịch đặt!" });

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Xóa người dùng thành công!" });
        }
    }
}