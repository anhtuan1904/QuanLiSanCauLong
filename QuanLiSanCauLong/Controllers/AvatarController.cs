using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiSanCauLong.Data;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Webp;

namespace QuanLiSanCauLong.Controllers
{
    /// <summary>
    /// Xử lý upload + crop ảnh đại diện người dùng.
    /// Yêu cầu NuGet: SixLabors.ImageSharp (dotnet add package SixLabors.ImageSharp)
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class AvatarController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        // Kích thước ảnh đại diện lưu cuối cùng
        private const int OUTPUT_SIZE = 256;
        // Thư mục lưu ảnh (tương đối với wwwroot)
        private const string AVATAR_FOLDER = "uploads/avatars";

        public AvatarController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        /// <summary>
        /// Nhận file ảnh gốc, trả về base64 để hiển thị trong cropper.
        /// KHÔNG lưu file tại bước này — chỉ validate.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Preview(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return Json(new { success = false, message = "Chưa chọn file!" });

            if (file.Length > 10 * 1024 * 1024) // 10MB max
                return Json(new { success = false, message = "File quá lớn! Tối đa 10MB." });

            var allowed = new[] { "image/jpeg", "image/png", "image/webp", "image/gif" };
            if (!allowed.Contains(file.ContentType.ToLower()))
                return Json(new { success = false, message = "Chỉ hỗ trợ JPG, PNG, WebP, GIF." });

            try
            {
                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                var base64 = Convert.ToBase64String(ms.ToArray());
                return Json(new
                {
                    success = true,
                    dataUrl = $"data:{file.ContentType};base64,{base64}",
                    originalName = file.FileName
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi đọc file: {ex.Message}" });
            }
        }

        /// <summary>
        /// Nhận crop data (x, y, width, height, rotate, scaleX, scaleY từ Cropper.js),
        /// crop ảnh bằng ImageSharp, lưu WebP 256×256, cập nhật DB.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveCrop(
            [FromForm] int userId,
            [FromForm] IFormFile sourceFile,
            [FromForm] double cropX,
            [FromForm] double cropY,
            [FromForm] double cropWidth,
            [FromForm] double cropHeight,
            [FromForm] double rotate,
            [FromForm] double scaleX,
            [FromForm] double scaleY)
        {
            if (userId <= 0)
                return Json(new { success = false, message = "User ID không hợp lệ!" });

            var user = await _context.Users.FindAsync(userId);
            if (user == null || user.Status == "Deleted")
                return Json(new { success = false, message = "Không tìm thấy người dùng!" });

            if (sourceFile == null || sourceFile.Length == 0)
                return Json(new { success = false, message = "Không có file ảnh!" });

            if (sourceFile.Length > 10 * 1024 * 1024)
                return Json(new { success = false, message = "File quá lớn! Tối đa 10MB." });

            try
            {
                // 1. Đảm bảo thư mục tồn tại
                var avatarDir = Path.Combine(_env.WebRootPath, AVATAR_FOLDER);
                Directory.CreateDirectory(avatarDir);

                // 2. Xoá ảnh cũ nếu có (tránh tích file rác)
                if (!string.IsNullOrEmpty(user.AvatarUrl))
                {
                    var oldPath = Path.Combine(_env.WebRootPath,
                        user.AvatarUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(oldPath))
                        System.IO.File.Delete(oldPath);
                }

                // 3. Load ảnh với ImageSharp
                using var stream = sourceFile.OpenReadStream();
                using var image = await Image.LoadAsync(stream);

                // 4. Apply transform (rotate + flip từ Cropper.js)
                image.Mutate(ctx =>
                {
                    // Áp dụng flip nếu scale âm
                    if (scaleX < 0) ctx.Flip(FlipMode.Horizontal);
                    if (scaleY < 0) ctx.Flip(FlipMode.Vertical);

                    // Rotate
                    if (Math.Abs(rotate) > 0.01)
                        ctx.Rotate((float)rotate);
                });

                // 5. Crop vùng đã chọn
                // Đảm bảo crop rect nằm trong bounds của ảnh sau transform
                var imgW = image.Width;
                var imgH = image.Height;

                int x = Math.Max(0, (int)Math.Round(cropX));
                int y = Math.Max(0, (int)Math.Round(cropY));
                int w = (int)Math.Round(cropWidth);
                int h = (int)Math.Round(cropHeight);

                // Clamp để không vượt ra ngoài
                w = Math.Min(w, imgW - x);
                h = Math.Min(h, imgH - y);

                if (w > 0 && h > 0)
                {
                    image.Mutate(ctx => ctx.Crop(new Rectangle(x, y, w, h)));
                }

                // 6. Resize về 256×256 (vuông)
                image.Mutate(ctx => ctx.Resize(new ResizeOptions
                {
                    Size = new Size(OUTPUT_SIZE, OUTPUT_SIZE),
                    Mode = ResizeMode.Pad, // Pad để không méo nếu crop không vuông
                    PadColor = Color.Transparent
                }));

                // 7. Lưu file WebP (nén tốt, hỗ trợ transparency)
                var fileName = $"user_{userId}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}.webp";
                var filePath = Path.Combine(avatarDir, fileName);

                await image.SaveAsync(filePath, new WebpEncoder { Quality = 85 });

                // 8. Cập nhật DB
                var relativeUrl = $"/{AVATAR_FOLDER}/{fileName}";
                user.AvatarUrl = relativeUrl;
                user.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    avatarUrl = relativeUrl,
                    message = "Cập nhật ảnh đại diện thành công!"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi xử lý ảnh: {ex.Message}" });
            }
        }

        /// <summary>
        /// Xóa ảnh đại diện (reset về initials avatar)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int userId)
        {
            if (userId <= 0)
                return Json(new { success = false, message = "ID không hợp lệ!" });

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return Json(new { success = false, message = "Không tìm thấy người dùng!" });

            if (!string.IsNullOrEmpty(user.AvatarUrl))
            {
                var fullPath = Path.Combine(_env.WebRootPath,
                    user.AvatarUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(fullPath))
                    System.IO.File.Delete(fullPath);
            }

            user.AvatarUrl = null;
            user.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã xóa ảnh đại diện!" });
        }
    }
}
