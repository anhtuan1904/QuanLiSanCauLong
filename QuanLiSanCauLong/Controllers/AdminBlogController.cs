using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiSanCauLong.Data;
using QuanLiSanCauLong.Models;

namespace QuanLiSanCauLong.Controllers
{
    /// <summary>
    /// Quản lý bài viết — toàn bộ yêu cầu role Admin.
    /// Route gốc: /AdminBlog/{action}
    /// Views:     Views/AdminBlog/
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class AdminBlogController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public AdminBlogController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // ══════════════════════════════════════════════
        // QUẢN LÝ BÀI VIẾT
        // ══════════════════════════════════════════════

        // GET /AdminBlog/Index
        [HttpGet]
        public async Task<IActionResult> Index(string search, string status)
        {
            var query = _context.BlogPosts.AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(p => p.Title.Contains(search) || p.Content.Contains(search));

            if (status == "Published" || status == "Archived")
                query = query.Where(p => p.Status == status);

            var posts = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
            return View(posts);
        }

        // GET  /AdminBlog/Create  → PartialView render trong modal
        [HttpGet]
        public IActionResult Create() => PartialView();

        // POST /AdminBlog/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BlogPost model, IFormFile? featuredImage)
        {
            ModelState.Remove("FeaturedImage");
            ModelState.Remove("Category");
            ModelState.Remove("Comments");
            ModelState.Remove("CategoryId");
            ModelState.Remove("Slug");

            if (ModelState.IsValid)
            {
                try
                {
                    model.Slug = GenerateSlug(model.Title);
                    model.CreatedAt = DateTime.Now;
                    model.UpdatedAt = DateTime.Now;
                    model.AllowComments = true;
                    model.CategoryId = 1;

                    if (model.Status == "Published")
                        model.PublishedAt = DateTime.Now;

                    if (featuredImage != null && featuredImage.Length > 0)
                        model.FeaturedImage = await SaveImageAsync(featuredImage);

                    _context.BlogPosts.Add(model);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Thêm bài viết thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex) { ModelState.AddModelError("", "Lỗi: " + ex.Message); }
            }

            return PartialView(model);
        }

        // GET  /AdminBlog/Edit/5  → PartialView render trong modal
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var post = await _context.BlogPosts.FindAsync(id);
            if (post == null) return NotFound();
            return PartialView(post);
        }

        // POST /AdminBlog/Edit
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(BlogPost model, IFormFile? featuredImage, bool removeExistingImage = false)
        {
            ModelState.Remove("FeaturedImage");
            ModelState.Remove("Category");
            ModelState.Remove("Comments");
            ModelState.Remove("CategoryId");
            ModelState.Remove("Slug");

            if (ModelState.IsValid)
            {
                var post = await _context.BlogPosts.FindAsync(model.PostId);
                if (post != null)
                {
                    try
                    {
                        post.Title = model.Title;
                        post.Slug = GenerateSlug(model.Title);
                        post.Excerpt = model.Excerpt;
                        post.Content = model.Content;
                        post.Status = model.Status;
                        post.IsFeatured = model.IsFeatured;
                        post.MetaTitle = model.MetaTitle;
                        post.MetaDescription = model.MetaDescription;
                        post.UpdatedAt = DateTime.Now;

                        if (model.Status == "Published" && !post.PublishedAt.HasValue)
                            post.PublishedAt = DateTime.Now;

                        if (removeExistingImage && !string.IsNullOrEmpty(post.FeaturedImage))
                        {
                            DeleteImage(post.FeaturedImage);
                            post.FeaturedImage = null;
                        }

                        if (featuredImage != null && featuredImage.Length > 0)
                        {
                            if (!string.IsNullOrEmpty(post.FeaturedImage))
                                DeleteImage(post.FeaturedImage);
                            post.FeaturedImage = await SaveImageAsync(featuredImage);
                        }

                        await _context.SaveChangesAsync();
                        TempData["SuccessMessage"] = "Cập nhật bài viết thành công!";
                        return RedirectToAction(nameof(Index));
                    }
                    catch (Exception ex) { ModelState.AddModelError("", "Lỗi: " + ex.Message); }
                }
            }

            return PartialView(model);
        }

        // POST /AdminBlog/Delete/5
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var post = await _context.BlogPosts
                .Include(p => p.Comments)
                .FirstOrDefaultAsync(p => p.PostId == id);

            if (post == null)
                return Json(new { success = false, message = "Không tìm thấy bài viết!" });

            try
            {
                if (!string.IsNullOrEmpty(post.FeaturedImage))
                    DeleteImage(post.FeaturedImage);

                _context.BlogPosts.Remove(post);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Xóa bài viết thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // GET /AdminBlog/Details/5  → PartialView xem nhanh trong modal
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var post = await _context.BlogPosts
                .Include(p => p.Comments)
                .FirstOrDefaultAsync(p => p.PostId == id);

            if (post == null) return NotFound();
            return PartialView(post);
        }

        // ══════════════════════════════════════════════
        // KHO LƯU TRỮ — chỉ Admin truy cập được
        // ══════════════════════════════════════════════

        // GET /AdminBlog/Archive
        [HttpGet]
        public async Task<IActionResult> Archive()
        {
            var posts = await _context.BlogPosts
                .Where(p => p.Status == "Archived")
                .OrderByDescending(p => p.UpdatedAt)
                .ToListAsync();
            return View(posts);
        }

        // GET /AdminBlog/ArchiveRead/5
        [HttpGet]
        public async Task<IActionResult> ArchiveRead(int id)
        {
            var post = await _context.BlogPosts
                .FirstOrDefaultAsync(p => p.PostId == id && p.Status == "Archived");

            if (post == null) return NotFound();
            return View(post);
        }

        // POST /AdminBlog/Restore/5  →  Archived → Published
        [HttpPost]
        public async Task<IActionResult> Restore(int id)
        {
            var post = await _context.BlogPosts.FindAsync(id);
            if (post == null)
                return Json(new { success = false, message = "Không tìm thấy bài viết!" });

            try
            {
                post.Status = "Published";
                post.PublishedAt ??= DateTime.Now;
                post.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Bài viết đã được khôi phục và xuất bản!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // ══════════════════════════════════════════════
        // UPLOAD ẢNH TINYMCE
        // ══════════════════════════════════════════════

        // POST /AdminBlog/UploadImage
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return Json(new { error = "Không có file" });
            try
            {
                return Json(new { location = await SaveImageAsync(file) });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        // ══════════════════════════════════════════════
        // PRIVATE HELPERS
        // ══════════════════════════════════════════════

        private async Task<string> SaveImageAsync(IFormFile file)
        {
            string folder = Path.Combine(_environment.WebRootPath, "images", "blog");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            string fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            using var stream = new FileStream(Path.Combine(folder, fileName), FileMode.Create);
            await file.CopyToAsync(stream);
            return $"/images/blog/{fileName}";
        }

        private void DeleteImage(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return;
            string full = Path.Combine(_environment.WebRootPath, relativePath.TrimStart('/'));
            if (System.IO.File.Exists(full)) System.IO.File.Delete(full);
        }

        private static string GenerateSlug(string text) =>
            text.ToLower()
                .Replace("đ", "d")
                .Replace("á", "a").Replace("à", "a").Replace("ả", "a").Replace("ã", "a").Replace("ạ", "a")
                .Replace("ă", "a").Replace("ắ", "a").Replace("ằ", "a").Replace("ẳ", "a").Replace("ẵ", "a").Replace("ặ", "a")
                .Replace("â", "a").Replace("ấ", "a").Replace("ầ", "a").Replace("ẩ", "a").Replace("ẫ", "a").Replace("ậ", "a")
                .Replace("é", "e").Replace("è", "e").Replace("ẻ", "e").Replace("ẽ", "e").Replace("ẹ", "e")
                .Replace("ê", "e").Replace("ế", "e").Replace("ề", "e").Replace("ể", "e").Replace("ễ", "e").Replace("ệ", "e")
                .Replace("í", "i").Replace("ì", "i").Replace("ỉ", "i").Replace("ĩ", "i").Replace("ị", "i")
                .Replace("ó", "o").Replace("ò", "o").Replace("ỏ", "o").Replace("õ", "o").Replace("ọ", "o")
                .Replace("ô", "o").Replace("ố", "o").Replace("ồ", "o").Replace("ổ", "o").Replace("ỗ", "o").Replace("ộ", "o")
                .Replace("ơ", "o").Replace("ớ", "o").Replace("ờ", "o").Replace("ở", "o").Replace("ỡ", "o").Replace("ợ", "o")
                .Replace("ú", "u").Replace("ù", "u").Replace("ủ", "u").Replace("ũ", "u").Replace("ụ", "u")
                .Replace("ư", "u").Replace("ứ", "u").Replace("ừ", "u").Replace("ử", "u").Replace("ữ", "u").Replace("ự", "u")
                .Replace("ý", "y").Replace("ỳ", "y").Replace("ỷ", "y").Replace("ỹ", "y").Replace("ỵ", "y")
                .Replace(" ", "-").Replace("---", "-").Replace("--", "-");
    }
}
