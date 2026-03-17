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
            ViewBag.Courses = await _context.Courses
                .Include(c => c.CourseImages)
                .OrderBy(c => c.DisplayOrder).ToListAsync();
            ViewBag.Stringings = await _context.StringingServices
                .Include(s => s.StringingImages)
                .OrderBy(s => s.DisplayOrder).ToListAsync();
            ViewBag.Tournaments = await _context.Tournaments
                .Include(t => t.TournamentImages)
                .OrderBy(t => t.DisplayOrder).ToListAsync();
            ViewBag.ActiveTab = activeTab;
            return View("Index");
        }


        // ============================================================
        // KHOA HOC - COURSE
        // ============================================================

        [HttpGet]
        public IActionResult CourseCreate()
        {
            return PartialView("CourseCreate", new Course());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CourseCreate(
            Course model,
            IFormFile? featuredImage,
            IFormFile? InstructorAvatarFile,
            List<IFormFile>? galleryImages)
        {
            ModelState.Remove("AvailableSlots");
            ModelState.Remove("IsFull");
            ModelState.Remove("EffectivePrice");
            ModelState.Remove("CourseImages");

            if (!ModelState.IsValid)
                return PartialView("CourseCreate", model);

            model.FeaturedImage = await SaveImageAsync(featuredImage, "courses");

            var avatarPath = await SaveImageAsync(InstructorAvatarFile, "courses/instructors");
            if (avatarPath != null) model.InstructorAvatar = avatarPath;

            model.CreatedAt = DateTime.Now;
            model.UpdatedAt = DateTime.Now;
            model.Slug = GenerateSlug(model.CourseName);

            _context.Courses.Add(model);
            await _context.SaveChangesAsync();

            if (galleryImages != null && galleryImages.Count > 0)
            {
                int order = 0;
                foreach (var img in galleryImages)
                {
                    var path = await SaveImageAsync(img, "courses/gallery");
                    if (path != null)
                    {
                        _context.CourseImages.Add(new CourseImage
                        {
                            CourseId = model.CourseId,
                            ImagePath = path,
                            IsPrimary = order == 0 && model.FeaturedImage == null,
                            DisplayOrder = order++,
                            UploadedAt = DateTime.Now
                        });
                    }
                }
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> CourseEdit(int id)
        {
            var course = await _context.Courses
                .Include(c => c.CourseImages)
                .FirstOrDefaultAsync(c => c.CourseId == id);
            if (course == null) return Content("<p class='text-danger p-4'>Khong tim thay khoa hoc</p>");
            return PartialView("CourseEdit", course);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CourseEdit(
            Course model,
            IFormFile? featuredImage,
            IFormFile? InstructorAvatarFile,
            List<IFormFile>? galleryImages)
        {
            ModelState.Remove("AvailableSlots");
            ModelState.Remove("IsFull");
            ModelState.Remove("EffectivePrice");
            ModelState.Remove("CourseImages");

            var existing = await _context.Courses
                .Include(c => c.CourseImages)
                .FirstOrDefaultAsync(c => c.CourseId == model.CourseId);
            if (existing == null)
                return Json(new { success = false, message = "Khong tim thay khoa hoc" });

            existing.CourseName = model.CourseName;
            existing.ShortDesc = model.ShortDesc;
            existing.Description = model.Description;
            existing.Instructor = model.Instructor;
            existing.InstructorTitle = model.InstructorTitle;
            existing.VideoUrl = model.VideoUrl;
            existing.Level = model.Level;
            existing.DurationWeeks = model.DurationWeeks;
            existing.Schedule = model.Schedule;
            existing.MaxStudents = model.MaxStudents;
            existing.CurrentStudents = model.CurrentStudents;
            existing.TuitionFee = model.TuitionFee;
            existing.DiscountFee = model.DiscountFee;
            existing.Features = model.Features;
            existing.Highlights = model.Highlights;
            existing.Requirements = model.Requirements;
            existing.Outcomes = model.Outcomes;
            existing.Status = model.Status;
            existing.IsFeatured = model.IsFeatured;
            existing.IsPopular = model.IsPopular;
            existing.DisplayOrder = model.DisplayOrder;
            existing.UpdatedAt = DateTime.Now;

            // Anh dai dien: uu tien file upload, sau do URL nhap tay
            if (featuredImage != null && featuredImage.Length > 0)
                existing.FeaturedImage = await SaveImageAsync(featuredImage, "courses");
            else if (!string.IsNullOrEmpty(model.FeaturedImage))
                existing.FeaturedImage = model.FeaturedImage;

            // Avatar HLV
            if (InstructorAvatarFile != null && InstructorAvatarFile.Length > 0)
                existing.InstructorAvatar = await SaveImageAsync(InstructorAvatarFile, "courses/instructors");
            else if (!string.IsNullOrEmpty(model.InstructorAvatar))
                existing.InstructorAvatar = model.InstructorAvatar;

            // Gallery moi
            if (galleryImages != null && galleryImages.Count > 0)
            {
                int order = existing.CourseImages?.Count ?? 0;
                foreach (var img in galleryImages)
                {
                    var path = await SaveImageAsync(img, "courses/gallery");
                    if (path != null)
                    {
                        _context.CourseImages.Add(new CourseImage
                        {
                            CourseId = existing.CourseId,
                            ImagePath = path,
                            IsPrimary = false,
                            DisplayOrder = order++,
                            UploadedAt = DateTime.Now
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> CourseDetails(int id)
        {
            var course = await _context.Courses
                .Include(c => c.CourseImages)
                .FirstOrDefaultAsync(c => c.CourseId == id);
            if (course == null) return NotFound();
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
                return Json(new { success = false, message = "Khong tim thay" });
            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Da xoa khoa hoc" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CourseImageDelete(int imageId)
        {
            var img = await _context.CourseImages.FindAsync(imageId);
            if (img == null) return Json(new { success = false });
            DeletePhysicalFile(img.ImagePath);
            _context.CourseImages.Remove(img);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CourseImageSetPrimary(int imageId, int courseId)
        {
            var images = await _context.CourseImages
                .Where(i => i.CourseId == courseId).ToListAsync();
            foreach (var img in images)
                img.IsPrimary = (img.ImageId == imageId);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }


        // ============================================================
        // CANG VOT - STRINGING
        // ============================================================

        [HttpGet]
        public IActionResult StringingCreate()
        {
            return PartialView("StringingCreate", new StringingService());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StringingCreate(
            StringingService model,
            IFormFile? featuredImage,
            List<IFormFile>? galleryImages)
        {
            ModelState.Remove("StringingImages");

            if (!ModelState.IsValid)
                return PartialView("StringingCreate", model);

            model.FeaturedImage = await SaveImageAsync(featuredImage, "stringings");
            model.CreatedAt = DateTime.Now;
            model.UpdatedAt = DateTime.Now;
            model.Slug = GenerateSlug(model.ServiceName);

            _context.StringingServices.Add(model);
            await _context.SaveChangesAsync();

            if (galleryImages != null && galleryImages.Count > 0)
            {
                int order = 0;
                foreach (var img in galleryImages)
                {
                    var path = await SaveImageAsync(img, "stringings/gallery");
                    if (path != null)
                    {
                        _context.StringingImages.Add(new StringingImage
                        {
                            StringingId = model.StringingId,
                            ImagePath = path,
                            IsPrimary = order == 0 && model.FeaturedImage == null,
                            DisplayOrder = order++,
                            UploadedAt = DateTime.Now
                        });
                    }
                }
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> StringingEdit(int id)
        {
            var s = await _context.StringingServices
                .Include(s => s.StringingImages)
                .FirstOrDefaultAsync(s => s.StringingId == id);
            if (s == null) return Content("<p class='text-danger p-4'>Khong tim thay dich vu</p>");
            return PartialView("StringingEdit", s);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StringingEdit(
            StringingService model,
            IFormFile? featuredImage,
            List<IFormFile>? galleryImages)
        {
            ModelState.Remove("StringingImages");

            var existing = await _context.StringingServices
                .Include(s => s.StringingImages)
                .FirstOrDefaultAsync(s => s.StringingId == model.StringingId);
            if (existing == null)
                return Json(new { success = false, message = "Khong tim thay" });

            existing.ServiceName = model.ServiceName;
            existing.Brand = model.Brand;
            existing.StringModel = model.StringModel;
            existing.ShortDesc = model.ShortDesc;
            existing.Description = model.Description;
            existing.Price = model.Price;
            existing.DiscountPrice = model.DiscountPrice;
            existing.DurationMinutes = model.DurationMinutes;
            existing.WarrantyDays = model.WarrantyDays;
            existing.Tension = model.Tension;
            existing.TensionMin = model.TensionMin;
            existing.TensionMax = model.TensionMax;
            existing.StringColor = model.StringColor;
            existing.StringGauge = model.StringGauge;
            existing.Features = model.Features;
            existing.Benefits = model.Benefits;
            existing.VideoUrl = model.VideoUrl;
            existing.Status = model.Status;
            existing.IsFeatured = model.IsFeatured;
            existing.IsPopular = model.IsPopular;
            existing.DisplayOrder = model.DisplayOrder;
            existing.UpdatedAt = DateTime.Now;

            if (featuredImage != null && featuredImage.Length > 0)
                existing.FeaturedImage = await SaveImageAsync(featuredImage, "stringings");
            else if (!string.IsNullOrEmpty(model.FeaturedImage))
                existing.FeaturedImage = model.FeaturedImage;

            if (galleryImages != null && galleryImages.Count > 0)
            {
                int order = existing.StringingImages?.Count ?? 0;
                foreach (var img in galleryImages)
                {
                    var path = await SaveImageAsync(img, "stringings/gallery");
                    if (path != null)
                    {
                        _context.StringingImages.Add(new StringingImage
                        {
                            StringingId = existing.StringingId,
                            ImagePath = path,
                            IsPrimary = false,
                            DisplayOrder = order++,
                            UploadedAt = DateTime.Now
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> StringingDetails(int id)
        {
            var stringing = await _context.StringingServices
                .Include(s => s.StringingImages)
                .FirstOrDefaultAsync(s => s.StringingId == id);
            if (stringing == null) return NotFound();
            stringing.ViewCount++;
            await _context.SaveChangesAsync();
            return PartialView("StringingDetails", stringing);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StringingDelete(int id)
        {
            var s = await _context.StringingServices.FindAsync(id);
            if (s == null)
                return Json(new { success = false, message = "Khong tim thay" });
            _context.StringingServices.Remove(s);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Da xoa dich vu cang vot" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StringingImageDelete(int imageId)
        {
            var img = await _context.StringingImages.FindAsync(imageId);
            if (img == null) return Json(new { success = false });
            DeletePhysicalFile(img.ImagePath);
            _context.StringingImages.Remove(img);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StringingImageSetPrimary(int imageId, int stringingId)
        {
            var images = await _context.StringingImages
                .Where(i => i.StringingId == stringingId).ToListAsync();
            foreach (var img in images)
                img.IsPrimary = (img.ImageId == imageId);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }


        // ============================================================
        // GIAI DAU - TOURNAMENT
        // ============================================================

        [HttpGet]
        public IActionResult TournamentCreate()
        {
            return PartialView("TournamentCreate", new Tournament());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TournamentCreate(
            Tournament model,
            IFormFile? featuredImage,
            List<IFormFile>? galleryImages)
        {
            ModelState.Remove("AvailableSlots");
            ModelState.Remove("IsFull");
            ModelState.Remove("StatusLabel");
            ModelState.Remove("StatusBadge");
            ModelState.Remove("TournamentImages");

            if (!ModelState.IsValid)
                return PartialView("TournamentCreate", model);

            model.FeaturedImage = await SaveImageAsync(featuredImage, "tournaments");
            model.CreatedAt = DateTime.Now;
            model.UpdatedAt = DateTime.Now;
            model.Slug = GenerateSlug(model.TournamentName);

            _context.Tournaments.Add(model);
            await _context.SaveChangesAsync();

            if (galleryImages != null && galleryImages.Count > 0)
            {
                int order = 0;
                foreach (var img in galleryImages)
                {
                    var path = await SaveImageAsync(img, "tournaments/gallery");
                    if (path != null)
                    {
                        _context.TournamentImages.Add(new TournamentImage
                        {
                            TournamentId = model.TournamentId,
                            ImagePath = path,
                            IsPrimary = order == 0 && model.FeaturedImage == null,
                            DisplayOrder = order++,
                            UploadedAt = DateTime.Now
                        });
                    }
                }
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> TournamentEdit(int id)
        {
            var t = await _context.Tournaments
                .Include(t => t.TournamentImages)
                .FirstOrDefaultAsync(t => t.TournamentId == id);
            if (t == null) return Content("<p class='text-danger p-4'>Khong tim thay giai dau</p>");
            return PartialView("TournamentEdit", t);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TournamentEdit(
            Tournament model,
            IFormFile? featuredImage,
            List<IFormFile>? galleryImages)
        {
            ModelState.Remove("AvailableSlots");
            ModelState.Remove("IsFull");
            ModelState.Remove("StatusLabel");
            ModelState.Remove("StatusBadge");
            ModelState.Remove("TournamentImages");

            var existing = await _context.Tournaments
                .Include(t => t.TournamentImages)
                .FirstOrDefaultAsync(t => t.TournamentId == model.TournamentId);
            if (existing == null)
                return Json(new { success = false, message = "Khong tim thay" });

            existing.TournamentName = model.TournamentName;
            existing.TournamentType = model.TournamentType;
            existing.ShortDesc = model.ShortDesc;
            existing.Description = model.Description;
            existing.StartDate = model.StartDate;
            existing.EndDate = model.EndDate;
            existing.RegistrationDeadline = model.RegistrationDeadline;
            existing.Venue = model.Venue;
            existing.VenueAddress = model.VenueAddress;
            existing.MapUrl = model.MapUrl;
            existing.MaxPlayers = model.MaxPlayers;
            existing.EntryFee = model.EntryFee;
            existing.PrizeMoney = model.PrizeMoney;
            existing.PrizeDescription = model.PrizeDescription;
            existing.Rules = model.Rules;
            existing.Schedule = model.Schedule;
            existing.Sponsors = model.Sponsors;
            existing.VideoUrl = model.VideoUrl;
            existing.Status = model.Status;
            existing.IsFeatured = model.IsFeatured;
            existing.DisplayOrder = model.DisplayOrder;
            existing.UpdatedAt = DateTime.Now;

            if (featuredImage != null && featuredImage.Length > 0)
                existing.FeaturedImage = await SaveImageAsync(featuredImage, "tournaments");
            else if (!string.IsNullOrEmpty(model.FeaturedImage))
                existing.FeaturedImage = model.FeaturedImage;

            if (galleryImages != null && galleryImages.Count > 0)
            {
                int order = existing.TournamentImages?.Count ?? 0;
                foreach (var img in galleryImages)
                {
                    var path = await SaveImageAsync(img, "tournaments/gallery");
                    if (path != null)
                    {
                        _context.TournamentImages.Add(new TournamentImage
                        {
                            TournamentId = existing.TournamentId,
                            ImagePath = path,
                            IsPrimary = false,
                            DisplayOrder = order++,
                            UploadedAt = DateTime.Now
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> TournamentDetails(int id)
        {
            var tournament = await _context.Tournaments
                .Include(t => t.TournamentImages)
                .FirstOrDefaultAsync(t => t.TournamentId == id);
            if (tournament == null) return NotFound();
            tournament.ViewCount++;
            await _context.SaveChangesAsync();
            return PartialView("TournamentDetails", tournament);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TournamentDelete(int id)
        {
            var t = await _context.Tournaments.FindAsync(id);
            if (t == null)
                return Json(new { success = false, message = "Khong tim thay" });
            _context.Tournaments.Remove(t);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Da xoa giai dau" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TournamentImageDelete(int imageId)
        {
            var img = await _context.TournamentImages.FindAsync(imageId);
            if (img == null) return Json(new { success = false });
            DeletePhysicalFile(img.ImagePath);
            _context.TournamentImages.Remove(img);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TournamentImageSetPrimary(int imageId, int tournamentId)
        {
            var images = await _context.TournamentImages
                .Where(i => i.TournamentId == tournamentId).ToListAsync();
            foreach (var img in images)
                img.IsPrimary = (img.ImageId == imageId);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }


        // ─── HELPERS ─────────────────────────────────────────────

        private async Task<string?> SaveImageAsync(IFormFile? file, string folder)
        {
            if (file == null || file.Length == 0) return null;

            var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/webp", "image/gif" };
            if (!allowedTypes.Contains(file.ContentType.ToLower())) return null;
            if (file.Length > 5 * 1024 * 1024) return null;

            var uploadsPath = Path.Combine(_env.WebRootPath, "uploads", folder);
            Directory.CreateDirectory(uploadsPath);
            var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(uploadsPath, fileName);
            await using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);
            return $"/uploads/{folder}/{fileName}";
        }

        private void DeletePhysicalFile(string? imagePath)
        {
            if (string.IsNullOrEmpty(imagePath)) return;
            if (imagePath.StartsWith("/uploads/"))
            {
                var fullPath = Path.Combine(_env.WebRootPath, imagePath.TrimStart('/'));
                if (System.IO.File.Exists(fullPath))
                    System.IO.File.Delete(fullPath);
            }
        }

        private static string GenerateSlug(string text)
        {
            var slug = text.ToLower()
                .Replace("a\u0300", "a").Replace("a\u0301", "a").Replace("a\u0309", "a").Replace("a\u0303", "a").Replace("a\u0323", "a")
                .Replace("\u0103", "a").Replace("\u1eaf", "a").Replace("\u1eb7", "a").Replace("\u1eb1", "a").Replace("\u1eb3", "a").Replace("\u1eb5", "a")
                .Replace("\u00e2", "a").Replace("\u1ea5", "a").Replace("\u1ead", "a").Replace("\u1ea7", "a").Replace("\u1ea9", "a").Replace("\u1eab", "a")
                .Replace("\u0111", "d")
                .Replace("e\u0300", "e").Replace("e\u0301", "e").Replace("e\u0309", "e").Replace("e\u0303", "e").Replace("e\u0323", "e")
                .Replace("\u00ea", "e").Replace("\u1ebf", "e").Replace("\u1ec7", "e").Replace("\u1ec1", "e").Replace("\u1ec3", "e").Replace("\u1ec5", "e")
                .Replace("i\u0300", "i").Replace("i\u0301", "i").Replace("i\u0309", "i").Replace("i\u0303", "i").Replace("i\u0323", "i")
                .Replace("o\u0300", "o").Replace("o\u0301", "o").Replace("o\u0309", "o").Replace("o\u0303", "o").Replace("o\u0323", "o")
                .Replace("\u00f4", "o").Replace("\u1ed1", "o").Replace("\u1ed9", "o").Replace("\u1ed3", "o").Replace("\u1ed5", "o").Replace("\u1ed7", "o")
                .Replace("\u01a1", "o").Replace("\u1edb", "o").Replace("\u1ee3", "o").Replace("\u1edd", "o").Replace("\u1edf", "o").Replace("\u1ee1", "o")
                .Replace("u\u0300", "u").Replace("u\u0301", "u").Replace("u\u0309", "u").Replace("u\u0303", "u").Replace("u\u0323", "u")
                .Replace("\u01b0", "u").Replace("\u1ee9", "u").Replace("\u1ef1", "u").Replace("\u1eeb", "u").Replace("\u1eed", "u").Replace("\u1eef", "u")
                .Replace("y\u0300", "y").Replace("y\u0301", "y").Replace("y\u0309", "y").Replace("y\u0303", "y").Replace("y\u0323", "y")
                .Replace(" ", "-");
            // fallback for literal Vietnamese chars that might remain
            slug = slug
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
                .Replace("ỳ", "y").Replace("ý", "y").Replace("ỷ", "y").Replace("ỹ", "y").Replace("ỵ", "y");
            return System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\-]", "");
        }
    }
}
