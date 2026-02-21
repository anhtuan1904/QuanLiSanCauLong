using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdfSharp.Logging;
using QuanLiSanCauLong.Data;
using QuanLiSanCauLong.Models;

namespace QuanLiSanCauLong.Controllers
{
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

        // ============ BLOG POSTS ============
        [HttpGet]
        public async Task<IActionResult> Index(string search, int? categoryId, string status)
        {
            var query = _context.BlogPosts
                .Include(p => p.Category)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(p => p.Title.Contains(search) || p.Content.Contains(search));

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(p => p.Status == status);

            var posts = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();

            ViewBag.Categories = await _context.BlogCategories.Where(c => c.IsActive).ToListAsync();
            return View(posts);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = await _context.BlogCategories.Where(c => c.IsActive).ToListAsync();
            return PartialView();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BlogPost model, IFormFile? featuredImage)
        {
            ModelState.Remove("FeaturedImage");
            ModelState.Remove("Category");
            ModelState.Remove("Comments");

            if (ModelState.IsValid)
            {
                try
                {
                    // Generate slug from title
                    model.Slug = GenerateSlug(model.Title);
                    model.CreatedAt = DateTime.Now;
                    model.UpdatedAt = DateTime.Now;

                    if (model.Status == "Published" && !model.PublishedAt.HasValue)
                        model.PublishedAt = DateTime.Now;

                    // Upload featured image
                    if (featuredImage != null && featuredImage.Length > 0)
                    {
                        string folder = Path.Combine(_environment.WebRootPath, "images", "blog");
                        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                        string fileName = $"{Guid.NewGuid()}{Path.GetExtension(featuredImage.FileName)}";
                        string filePath = Path.Combine(folder, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await featuredImage.CopyToAsync(stream);
                        }

                        model.FeaturedImage = $"/images/blog/{fileName}";
                    }

                    _context.BlogPosts.Add(model);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Thêm bài viết thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi: " + ex.Message);
                }
            }

            ViewBag.Categories = await _context.BlogCategories.Where(c => c.IsActive).ToListAsync();
            return PartialView(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var post = await _context.BlogPosts.FindAsync(id);
            if (post == null) return NotFound();

            ViewBag.Categories = await _context.BlogCategories.Where(c => c.IsActive).ToListAsync();
            return PartialView(post);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(BlogPost model, IFormFile? featuredImage)
        {
            ModelState.Remove("FeaturedImage");
            ModelState.Remove("Category");
            ModelState.Remove("Comments");

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
                        post.CategoryId = model.CategoryId;
                        post.Tags = model.Tags;
                        post.Status = model.Status;
                        post.IsFeatured = model.IsFeatured;
                        post.MetaTitle = model.MetaTitle;
                        post.MetaDescription = model.MetaDescription;
                        post.UpdatedAt = DateTime.Now;

                        if (model.Status == "Published" && !post.PublishedAt.HasValue)
                            post.PublishedAt = DateTime.Now;

                        // Upload new image
                        if (featuredImage != null && featuredImage.Length > 0)
                        {
                            // Delete old image
                            if (!string.IsNullOrEmpty(post.FeaturedImage))
                            {
                                string oldPath = Path.Combine(_environment.WebRootPath, post.FeaturedImage.TrimStart('/'));
                                if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                            }

                            string folder = Path.Combine(_environment.WebRootPath, "images", "blog");
                            string fileName = $"{Guid.NewGuid()}{Path.GetExtension(featuredImage.FileName)}";
                            string filePath = Path.Combine(folder, fileName);

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await featuredImage.CopyToAsync(stream);
                            }

                            post.FeaturedImage = $"/images/blog/{fileName}";
                        }

                        await _context.SaveChangesAsync();
                        TempData["SuccessMessage"] = "Cập nhật bài viết thành công!";
                        return RedirectToAction(nameof(Index));
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("", "Lỗi: " + ex.Message);
                    }
                }
            }

            ViewBag.Categories = await _context.BlogCategories.Where(c => c.IsActive).ToListAsync();
            return PartialView(model);
        }

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
                // Delete image
                if (!string.IsNullOrEmpty(post.FeaturedImage))
                {
                    string path = Path.Combine(_environment.WebRootPath, post.FeaturedImage.TrimStart('/'));
                    if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
                }

                _context.BlogPosts.Remove(post);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Xóa bài viết thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // ============ CATEGORIES ============
        [HttpGet]
        public async Task<IActionResult> Categories()
        {
            var categories = await _context.BlogCategories.OrderBy(c => c.DisplayOrder).ToListAsync();
            return View(categories);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCategory([FromBody] BlogCategory model)
        {
            try
            {
                model.Slug = GenerateSlug(model.CategoryName);
                model.CreatedAt = DateTime.Now;
                _context.BlogCategories.Add(model);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Thêm danh mục thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.BlogCategories
                .Include(c => c.Posts)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
                return Json(new { success = false, message = "Không tìm thấy danh mục!" });

            if (category.Posts != null && category.Posts.Any())
                return Json(new { success = false, message = "Không thể xóa danh mục đang có bài viết!" });

            try
            {
                _context.BlogCategories.Remove(category);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Xóa danh mục thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // Helper method
        private string GenerateSlug(string text)
        {
            return text.ToLower()
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
                .Replace(" ", "-")
                .Replace("---", "-").Replace("--", "-");
        }
    }
}
