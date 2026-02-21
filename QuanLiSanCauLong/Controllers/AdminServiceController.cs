using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiSanCauLong.Models;
using QuanLiSanCauLong.Data;

namespace QuanLiSanCauLong.Controllers
{
    public class AdminServiceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public AdminServiceController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ─── INDEX ───────────────────────────────────────────────
        public async Task<IActionResult> Index(string activeTab = "courses")
        {
            ViewBag.Courses = await _context.Courses.OrderBy(c => c.DisplayOrder).ToListAsync();
            ViewBag.Stringings = await _context.StringingServices.OrderBy(s => s.DisplayOrder).ToListAsync();
            ViewBag.Tournaments = await _context.Tournaments.OrderBy(t => t.DisplayOrder).ToListAsync();
            ViewBag.ActiveTab = activeTab;
            return View("Index");
        }


        [HttpGet]
        public IActionResult CourseCreate()
        {
            return PartialView("CourseCreate", new Course());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CourseCreate(Course model, IFormFile? featuredImage)
        {
            // Bỏ qua các field [NotMapped] khỏi ModelState
            ModelState.Remove("AvailableSlots");
            ModelState.Remove("IsFull");
            ModelState.Remove("EffectivePrice");

            if (!ModelState.IsValid)
                return PartialView("_CourseCreate", model);

            model.FeaturedImage = await SaveImageAsync(featuredImage, "courses");
            model.CreatedAt = DateTime.Now;
            model.Slug = GenerateSlug(model.CourseName);

            _context.Courses.Add(model);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> CourseEdit(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null) return Content("<p class='text-danger p-4'>Không tìm thấy khóa học</p>");
            return PartialView("_CourseEdit", course);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CourseEdit(Course model, IFormFile? featuredImage)
        {
            ModelState.Remove("AvailableSlots");
            ModelState.Remove("IsFull");
            ModelState.Remove("EffectivePrice");

            var existing = await _context.Courses.FindAsync(model.CourseId);
            if (existing == null)
                return Json(new { success = false, message = "Không tìm thấy khóa học" });

            existing.CourseName = model.CourseName;
            existing.ShortDesc = model.ShortDesc;
            existing.Description = model.Description;
            existing.Instructor = model.Instructor;
            existing.Level = model.Level;
            existing.DurationWeeks = model.DurationWeeks;
            existing.Schedule = model.Schedule;
            existing.MaxStudents = model.MaxStudents;
            existing.TuitionFee = model.TuitionFee;
            existing.DiscountFee = model.DiscountFee;
            existing.Features = model.Features;
            existing.Status = model.Status;
            existing.IsFeatured = model.IsFeatured;
            existing.DisplayOrder = model.DisplayOrder;
            existing.UpdatedAt = DateTime.Now;

            if (featuredImage != null && featuredImage.Length > 0)
                existing.FeaturedImage = await SaveImageAsync(featuredImage, "courses");

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> CourseDetails(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null) return Content("<p class='text-danger p-4'>Không tìm thấy khóa học</p>");
            course.ViewCount++;
            await _context.SaveChangesAsync();
            return PartialView("CourseDetails", course);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CourseDelete(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null)
                return Json(new { success = false, message = "Không tìm thấy" });
            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // ============================================================
        // CĂNG VỢT - Partial Views phải đặt tại:
        // Views/AdminService/_StringingCreate.cshtml
        // Views/AdminService/_StringingEdit.cshtml
        // Views/AdminService/_StringingDetails.cshtml
        // ============================================================

        [HttpGet]
        public IActionResult StringingCreate()
        {
            return PartialView("StringingCreate", new StringingService());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StringingCreate(StringingService model, IFormFile? featuredImage)
        {
            if (!ModelState.IsValid)
                return PartialView("StringingCreate", model);

            model.FeaturedImage = await SaveImageAsync(featuredImage, "stringings");
            model.CreatedAt = DateTime.Now;
            model.Slug = GenerateSlug(model.ServiceName);

            _context.StringingServices.Add(model);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> StringingEdit(int id)
        {
            var s = await _context.StringingServices.FindAsync(id);
            if (s == null) return Content("<p class='text-danger p-4'>Không tìm thấy dịch vụ</p>");
            return PartialView("StringingEdit", s);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StringingEdit(StringingService model, IFormFile? featuredImage)
        {
            var existing = await _context.StringingServices.FindAsync(model.StringingId);
            if (existing == null)
                return Json(new { success = false, message = "Không tìm thấy" });

            existing.ServiceName = model.ServiceName;
            existing.Brand = model.Brand;
            existing.StringModel = model.StringModel;
            existing.ShortDesc = model.ShortDesc;
            existing.Description = model.Description;
            existing.Price = model.Price;
            existing.DurationMinutes = model.DurationMinutes;
            existing.WarrantyDays = model.WarrantyDays;
            existing.Tension = model.Tension;
            existing.Features = model.Features;
            existing.Status = model.Status;
            existing.IsFeatured = model.IsFeatured;
            existing.IsPopular = model.IsPopular;
            existing.DisplayOrder = model.DisplayOrder;
            existing.UpdatedAt = DateTime.Now;

            if (featuredImage != null && featuredImage.Length > 0)
                existing.FeaturedImage = await SaveImageAsync(featuredImage, "stringings");

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> StringingDetails(int id)
        {
            var s = await _context.StringingServices.FindAsync(id);
            if (s == null) return Content("<p class='text-danger p-4'>Không tìm thấy dịch vụ</p>");
            s.ViewCount++;
            await _context.SaveChangesAsync();
            return PartialView("StringingDetails", s);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StringingDelete(int id)
        {
            var s = await _context.StringingServices.FindAsync(id);
            if (s == null)
                return Json(new { success = false, message = "Không tìm thấy" });
            _context.StringingServices.Remove(s);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // ============================================================
        // GIẢI ĐẤU - Partial Views phải đặt tại:
        // Views/AdminService/_TournamentCreate.cshtml
        // Views/AdminService/_TournamentEdit.cshtml
        // Views/AdminService/_TournamentDetails.cshtml
        // ============================================================

        [HttpGet]
        public IActionResult TournamentCreate()
        {
            return PartialView("TournamentCreate", new Tournament());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TournamentCreate(Tournament model, IFormFile? featuredImage)
        {
            // Bỏ qua computed properties
            ModelState.Remove("AvailableSlots");
            ModelState.Remove("IsFull");
            ModelState.Remove("StatusLabel");
            ModelState.Remove("StatusBadge");

            if (!ModelState.IsValid)
                return PartialView("TournamentCreate", model);

            model.FeaturedImage = await SaveImageAsync(featuredImage, "tournaments");
            model.CreatedAt = DateTime.Now;
            model.Slug = GenerateSlug(model.TournamentName);

            _context.Tournaments.Add(model);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> TournamentEdit(int id)
        {
            var t = await _context.Tournaments.FindAsync(id);
            if (t == null) return Content("<p class='text-danger p-4'>Không tìm thấy giải đấu</p>");
            return PartialView("TournamentEdit", t);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TournamentEdit(Tournament model, IFormFile? featuredImage)
        {
            ModelState.Remove("AvailableSlots");
            ModelState.Remove("IsFull");
            ModelState.Remove("StatusLabel");
            ModelState.Remove("StatusBadge");

            var existing = await _context.Tournaments.FindAsync(model.TournamentId);
            if (existing == null)
                return Json(new { success = false, message = "Không tìm thấy" });

            existing.TournamentName = model.TournamentName;
            existing.TournamentType = model.TournamentType;
            existing.ShortDesc = model.ShortDesc;
            existing.Description = model.Description;
            existing.StartDate = model.StartDate;
            existing.EndDate = model.EndDate;
            existing.RegistrationDeadline = model.RegistrationDeadline;
            existing.Venue = model.Venue;
            existing.MaxPlayers = model.MaxPlayers;
            existing.EntryFee = model.EntryFee;
            existing.PrizeMoney = model.PrizeMoney;
            existing.PrizeDescription = model.PrizeDescription;
            existing.Rules = model.Rules;
            existing.Status = model.Status;
            existing.IsFeatured = model.IsFeatured;
            existing.DisplayOrder = model.DisplayOrder;
            existing.UpdatedAt = DateTime.Now;

            if (featuredImage != null && featuredImage.Length > 0)
                existing.FeaturedImage = await SaveImageAsync(featuredImage, "tournaments");

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> TournamentDetails(int id)
        {
            var t = await _context.Tournaments.FindAsync(id);
            if (t == null) return Content("<p class='text-danger p-4'>Không tìm thấy giải đấu</p>");
            t.ViewCount++;
            await _context.SaveChangesAsync();
            return PartialView("TournamentDetails", t);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TournamentDelete(int id)
        {
            var t = await _context.Tournaments.FindAsync(id);
            if (t == null)
                return Json(new { success = false, message = "Không tìm thấy" });
            _context.Tournaments.Remove(t);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // ─── HELPERS ─────────────────────────────────────────────

        private async Task<string?> SaveImageAsync(IFormFile? file, string folder)
        {
            if (file == null || file.Length == 0) return null;
            var uploadsPath = Path.Combine(_env.WebRootPath, "uploads", folder);
            Directory.CreateDirectory(uploadsPath);
            var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(uploadsPath, fileName);
            await using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);
            return $"/uploads/{folder}/{fileName}";
        }

        private static string GenerateSlug(string text)
        {
            var slug = text.ToLower()
                .Replace("à", "a").Replace("á", "a").Replace("ả", "a").Replace("ã", "a").Replace("ạ", "a")
                .Replace("ă", "a").Replace("ắ", "a").Replace("ặ", "a").Replace("ằ", "a").Replace("ẳ", "a").Replace("ẵ", "a")
                .Replace("â", "a").Replace("ấ", "a").Replace("ậ", "a").Replace("ầ", "a").Replace("ẩ", "a").Replace("ẫ", "a")
                .Replace("đ", "d")
                .Replace("è", "e").Replace("é", "e").Replace("ẻ", "e").Replace("ẽ", "e").Replace("ẹ", "e")
                .Replace("ê", "e").Replace("ế", "e").Replace("ệ", "e").Replace("ề", "e").Replace("ể", "e").Replace("ễ", "e")
                .Replace("ì", "i").Replace("í", "i").Replace("ỉ", "i").Replace("ĩ", "i").Replace("ị", "i")
                .Replace("ò", "o").Replace("ó", "o").Replace("ỏ", "o").Replace("õ", "o").Replace("ọ", "o")
                .Replace("ô", "o").Replace("ố", "o").Replace("ộ", "o").Replace("ồ", "o").Replace("ổ", "o").Replace("ỗ", "o")
                .Replace("ơ", "o").Replace("ớ", "o").Replace("ợ", "o").Replace("ờ", "o").Replace("ở", "o").Replace("ỡ", "o")
                .Replace("ù", "u").Replace("ú", "u").Replace("ủ", "u").Replace("ũ", "u").Replace("ụ", "u")
                .Replace("ư", "u").Replace("ứ", "u").Replace("ự", "u").Replace("ừ", "u").Replace("ử", "u").Replace("ữ", "u")
                .Replace("ỳ", "y").Replace("ý", "y").Replace("ỷ", "y").Replace("ỹ", "y").Replace("ỵ", "y")
                .Replace(" ", "-");
            return System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\-]", "");
        }
    }
}
