using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiSanCauLong.Data;
using QuanLiSanCauLong.Helpers;
using QuanLiSanCauLong.Models;
using System.Security.Claims;

namespace QuanLiSanCauLong.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        // Thư mục lưu avatar (tương đối với wwwroot)
        private const string AvatarFolder = "images/avatars";

        // Kích thước file tối đa: 10 MB (khớp với _AvatarCropper.cshtml)
        private const long MaxFileBytes = 10L * 1024 * 1024;

        public ProfileController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ══════════════════════════════════════════════════════════════
        // GET  /Profile  — trang thông tin cá nhân
        // ══════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return RedirectToAction("Login", "Account");

            // Truyền avatar xuống layout sidebar (_LayoutAdmin / _LayoutStaff)
            ViewData["UserAvatar"] = user.AvatarUrl;

            // Thống kê cho Customer
            if (user.Role == "Customer")
            {
                ViewBag.TotalBookings = await _context.Bookings.CountAsync(b => b.UserId == user.UserId);
                ViewBag.CompletedBookings = await _context.Bookings.CountAsync(b => b.UserId == user.UserId && b.Status == "Completed");
                ViewBag.TotalSpent = await _context.Bookings
                    .Where(b => b.UserId == user.UserId && b.Status == "Completed")
                    .SumAsync(b => (decimal?)b.TotalPrice) ?? 0;
            }

            return View("~/Views/Account/Profile.cshtml", user);
        }

        // ══════════════════════════════════════════════════════════════
        // POST /Profile  — cập nhật họ tên, số điện thoại
        // ══════════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(User model)
        {
            var user = await _context.Users.FindAsync(model.UserId);
            if (user == null) return NotFound();

            // Chỉ cho phép sửa đúng user của mình
            if (user.UserId != GetCurrentUserId())
                return Forbid();

            user.FullName = model.FullName;
            user.Phone = model.Phone;
            user.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            HttpContext.Session.SetString("UserName", user.FullName);
            TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";

            ViewData["UserAvatar"] = user.AvatarUrl;
            return View("~/Views/Account/Profile.cshtml", user);
        }

        // ══════════════════════════════════════════════════════════════
        // POST /Profile/UploadAvatar
        // Nhận: IFormFile avatar  (file thô từ input)
        //   OR  string  croppedBase64 (base64 WebP từ Cropper.js)
        // Trả:  { success, avatarUrl }
        // ══════════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAvatar(IFormFile? avatar, string? croppedBase64)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Json(new { success = false, message = "Phiên đăng nhập đã hết." });

            try
            {
                string savedPath;

                // ── Nhánh 1: Cropper.js gửi base64 WebP ──────────────────────
                if (!string.IsNullOrEmpty(croppedBase64))
                {
                    savedPath = await SaveBase64ImageAsync(croppedBase64, user.UserId);
                }
                // ── Nhánh 2: Upload file thông thường ────────────────────────
                else if (avatar != null && avatar.Length > 0)
                {
                    if (avatar.Length > MaxFileBytes)
                        return Json(new { success = false, message = "File quá lớn. Tối đa 10MB." });

                    var allowed = new[] { "image/jpeg", "image/jpg", "image/png",
                                          "image/webp", "image/gif", "image/bmp" };
                    if (!allowed.Contains(avatar.ContentType.ToLower()))
                        return Json(new { success = false, message = "Chỉ chấp nhận JPG, PNG, WEBP, GIF." });

                    savedPath = await SaveFormFileAsync(avatar, user.UserId);
                }
                else
                {
                    return Json(new { success = false, message = "Không có file nào được gửi lên." });
                }

                // ── Xóa ảnh cũ (nếu tồn tại) ────────────────────────────────
                DeleteOldAvatar(user.AvatarUrl);

                // ── Cập nhật DB ──────────────────────────────────────────────
                user.AvatarUrl = savedPath;
                user.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                // ── Cập nhật cookie claim AvatarUrl để layout hiển thị ngay ─
                await RefreshAvatarClaimAsync(user.AvatarUrl);

                return Json(new { success = true, avatarUrl = user.AvatarUrl });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi server: " + ex.Message });
            }
        }

        // ══════════════════════════════════════════════════════════════
        // POST /Profile/DeleteAvatar
        // Xóa ảnh đại diện, về lại initials
        // ══════════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAvatar()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Json(new { success = false, message = "Phiên đăng nhập đã hết." });

            try
            {
                DeleteOldAvatar(user.AvatarUrl);

                user.AvatarUrl = null;
                user.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                await RefreshAvatarClaimAsync(null);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // ══════════════════════════════════════════════════════════════
        // POST /Profile/ChangePassword
        // ══════════════════════════════════════════════════════════════
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(
            string currentPassword, string newPassword, string confirmPassword)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return NotFound();

            if (!PasswordHelper.VerifyPassword(currentPassword, user.PasswordHash))
                return Json(new { success = false, message = "Mật khẩu hiện tại không đúng." });

            if (newPassword != confirmPassword)
                return Json(new { success = false, message = "Mật khẩu mới không khớp." });

            if (newPassword.Length < 6)
                return Json(new { success = false, message = "Mật khẩu phải có ít nhất 6 ký tự." });

            user.PasswordHash = PasswordHelper.HashPassword(newPassword);
            user.Password = string.Empty; // không lưu plain text
            user.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đổi mật khẩu thành công!" });
        }

        // ══════════════════════════════════════════════════════════════
        // GET /Profile/GetAvatarLibrary  — danh sách avatar mẫu
        // Trả JSON cho _AvatarCropper.cshtml → showAvatarLibrary()
        // ══════════════════════════════════════════════════════════════
        [HttpGet]
        public IActionResult GetAvatarLibrary()
        {
            // Đặt ảnh mẫu vào wwwroot/images/avatars/library/*.webp
            var libraryDir = Path.Combine(_env.WebRootPath, "images", "avatars", "library");
            var items = new List<object>();

            if (Directory.Exists(libraryDir))
            {
                var files = Directory.GetFiles(libraryDir, "*.webp")
                    .Concat(Directory.GetFiles(libraryDir, "*.png"))
                    .Concat(Directory.GetFiles(libraryDir, "*.jpg"))
                    .Take(24); // tối đa 24 ảnh mẫu

                foreach (var f in files)
                {
                    var name = Path.GetFileNameWithoutExtension(f);
                    items.Add(new
                    {
                        url = $"/images/avatars/library/{Path.GetFileName(f)}",
                        label = name
                    });
                }
            }

            // Nếu thư mục trống → trả avatar màu gradient tạo sẵn
            if (!items.Any())
            {
                var colors = new[] {
                    new { bg = "#d4a017", fg = "#1a1209" },
                    new { bg = "#4f46e5", fg = "#ffffff" },
                    new { bg = "#16a34a", fg = "#ffffff" },
                    new { bg = "#c94b2a", fg = "#ffffff" },
                    new { bg = "#0e7490", fg = "#ffffff" },
                    new { bg = "#7c3aed", fg = "#ffffff" },
                };
                var user = GetCurrentUserAsync().Result;
                var initial = user?.Initials ?? "U";
                items = colors.Select(c => (object)new
                {
                    url = (string?)null,
                    label = initial,
                    bgColor = c.bg,
                    fgColor = c.fg
                }).ToList();
            }

            return Json(items);
        }

        // ══════════════════════════════════════════════════════════════
        // PRIVATE HELPERS
        // ══════════════════════════════════════════════════════════════

        /// <summary>Lưu base64 image (từ Cropper.js) thành file WebP trên disk.</summary>
        private async Task<string> SaveBase64ImageAsync(string base64, int userId)
        {
            // Dạng: "data:image/webp;base64,XXXX..."
            var parts = base64.Split(',', 2);
            var data = parts.Length == 2 ? parts[1] : parts[0];
            var bytes = Convert.FromBase64String(data);

            var folder = EnsureAvatarFolder();
            var fileName = $"user_{userId}_{Guid.NewGuid():N}.webp";
            var filePath = Path.Combine(folder, fileName);

            await System.IO.File.WriteAllBytesAsync(filePath, bytes);
            return $"/{AvatarFolder}/{fileName}";
        }

        /// <summary>Lưu IFormFile thành file trên disk.</summary>
        private async Task<string> SaveFormFileAsync(IFormFile file, int userId)
        {
            var folder = EnsureAvatarFolder();
            var ext = Path.GetExtension(file.FileName).ToLower();
            var fileName = $"user_{userId}_{Guid.NewGuid():N}{ext}";
            var filePath = Path.Combine(folder, fileName);

            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);
            return $"/{AvatarFolder}/{fileName}";
        }

        /// <summary>Xóa file ảnh cũ nếu nằm trong thư mục avatars (tránh xóa ảnh ngoài).</summary>
        private void DeleteOldAvatar(string? avatarUrl)
        {
            if (string.IsNullOrEmpty(avatarUrl)) return;
            if (!avatarUrl.StartsWith($"/{AvatarFolder}/")) return; // bảo vệ: chỉ xóa trong thư mục avatars

            var oldPath = Path.Combine(
                _env.WebRootPath,
                avatarUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

            if (System.IO.File.Exists(oldPath))
                System.IO.File.Delete(oldPath);
        }

        /// <summary>Tạo thư mục avatars nếu chưa tồn tại.</summary>
        private string EnsureAvatarFolder()
        {
            var folder = Path.Combine(_env.WebRootPath, AvatarFolder.Replace('/', Path.DirectorySeparatorChar));
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            return folder;
        }

        /// <summary>
        /// Cập nhật claim AvatarUrl trong cookie để sidebar layout hiển thị ảnh mới
        /// ngay lập tức mà không cần logout/login lại.
        /// </summary>
        private async Task RefreshAvatarClaimAsync(string? newAvatarUrl)
        {
            // Lấy toàn bộ claims hiện tại, bỏ claim AvatarUrl cũ
            var claims = User.Claims
                .Where(c => c.Type != "AvatarUrl")
                .ToList();

            claims.Add(new Claim("AvatarUrl", newAvatarUrl ?? ""));

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            // Giữ nguyên IsPersistent của cookie cũ
            var existingProps = (await HttpContext.AuthenticateAsync(
                CookieAuthenticationDefaults.AuthenticationScheme))?.Properties
                ?? new AuthenticationProperties { IsPersistent = false };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                existingProps);
        }

        private async Task<User?> GetCurrentUserAsync()
        {
            int id = GetCurrentUserId();
            return id > 0 ? await _context.Users.FindAsync(id) : null;
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(claim, out int id)) return id;
            return HttpContext.Session.GetInt32("UserId") ?? 0;
        }
    }
}
