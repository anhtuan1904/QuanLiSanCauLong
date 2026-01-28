using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiSanCauLong.Data;
using QuanLiSanCauLong.Models;
using System.Security.Claims;

namespace QuanLiSanCauLong.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminSettingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminSettingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string category)
        {
            var query = _context.SystemSettings.AsQueryable();

            if (!string.IsNullOrEmpty(category))
                query = query.Where(s => s.Category == category);

            var settings = await query
                .OrderBy(s => s.Category)
                .ThenBy(s => s.SettingKey)
                .ToListAsync();

            // Lấy danh sách các category
            var categories = await _context.SystemSettings
                .Select(s => s.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            ViewBag.Categories = categories;
            ViewBag.CurrentCategory = category;

            return View(settings);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SystemSetting model)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra key trùng
                if (await _context.SystemSettings.AnyAsync(s => s.SettingKey == model.SettingKey))
                {
                    ModelState.AddModelError("SettingKey", "Key này đã tồn tại!");
                    return View(model);
                }

                model.CreatedAt = DateTime.Now;
                model.UpdatedAt = DateTime.Now;
                model.CreatedBy = GetCurrentUserId();

                _context.SystemSettings.Add(model);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Tạo cài đặt thành công!";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var setting = await _context.SystemSettings.FindAsync(id);
            if (setting == null)
                return NotFound();

            return View(setting);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(SystemSetting model)
        {
            if (ModelState.IsValid)
            {
                var setting = await _context.SystemSettings.FindAsync(model.SettingId);
                if (setting != null)
                {
                    setting.SettingValue = model.SettingValue;
                    setting.Description = model.Description;
                    setting.UpdatedAt = DateTime.Now;
                    setting.UpdatedBy = GetCurrentUserId();

                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Cập nhật cài đặt thành công!";
                    return RedirectToAction(nameof(Index));
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSetting(int settingId, string value)
        {
            if (settingId <= 0)
                return Json(new { success = false, message = "ID không hợp lệ!" });

            var setting = await _context.SystemSettings.FindAsync(settingId);
            if (setting != null)
            {
                setting.SettingValue = value;
                setting.UpdatedAt = DateTime.Now;
                setting.UpdatedBy = GetCurrentUserId();
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Cập nhật thành công!" });
            }

            return Json(new { success = false, message = "Không tìm thấy cài đặt!" });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
                return Json(new { success = false, message = "ID không hợp lệ!" });

            var setting = await _context.SystemSettings.FindAsync(id);
            if (setting == null)
                return Json(new { success = false, message = "Không tìm thấy cài đặt!" });

            _context.SystemSettings.Remove(setting);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Xóa cài đặt thành công!" });
        }

        [HttpGet]
        public async Task<IActionResult> General()
        {
            var settings = await _context.SystemSettings
                .Where(s => s.Category == "General")
                .ToListAsync();

            return View(settings);
        }

        [HttpGet]
        public async Task<IActionResult> Email()
        {
            var settings = await _context.SystemSettings
                .Where(s => s.Category == "Email")
                .ToListAsync();

            return View(settings);
        }

        [HttpGet]
        public async Task<IActionResult> Payment()
        {
            var settings = await _context.SystemSettings
                .Where(s => s.Category == "Payment")
                .ToListAsync();

            return View(settings);
        }

        [HttpGet]
        public async Task<IActionResult> Notification()
        {
            var settings = await _context.SystemSettings
                .Where(s => s.Category == "Notification")
                .ToListAsync();

            return View(settings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBulk(Dictionary<string, string> settings)
        {
            if (settings == null || !settings.Any())
                return Json(new { success = false, message = "Không có dữ liệu để cập nhật!" });

            try
            {
                foreach (var item in settings)
                {
                    if (int.TryParse(item.Key, out int settingId))
                    {
                        var setting = await _context.SystemSettings.FindAsync(settingId);
                        if (setting != null)
                        {
                            setting.SettingValue = item.Value;
                            setting.UpdatedAt = DateTime.Now;
                            setting.UpdatedBy = GetCurrentUserId();
                        }
                    }
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Cập nhật thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetToDefault()
        {
            // TODO: Implement reset settings to default values
            TempData["InfoMessage"] = "Chức năng khôi phục cài đặt mặc định đang được phát triển.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Backup()
        {
            // TODO: Implement backup settings
            TempData["InfoMessage"] = "Chức năng sao lưu cài đặt đang được phát triển.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore()
        {
            // TODO: Implement restore settings from backup
            TempData["InfoMessage"] = "Chức năng khôi phục cài đặt đang được phát triển.";
            return RedirectToAction(nameof(Index));
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
            return 0;
        }
    }
}