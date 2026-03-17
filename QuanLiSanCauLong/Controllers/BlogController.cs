using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiSanCauLong.Data;
using QuanLiSanCauLong.Models;
using QuanLiSanCauLong.ViewModels;

namespace QuanLiSanCauLong.Controllers
{
    /// <summary>
    /// Controller công khai cho người dùng đọc bài viết.
    /// Chỉ bài Status = "Published" mới hiển thị.
    /// Bài Status = "Archived" → 404 (chỉ Admin xem qua AdminBlogController).
    /// Route gốc: /Blog/{action}
    /// Views:     Views/Blog/
    /// </summary>
    public class BlogController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BlogController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET /Blog  — trang tin tức chính
        // ?category=ten-category  → lọc theo category (tên, không phân biệt hoa thường)
        [HttpGet]
        public async Task<IActionResult> Index(string? category)
        {
            var query = _context.BlogPosts
                .Include(p => p.Category)
                .Where(p => p.Status == "Published");

            if (!string.IsNullOrEmpty(category))
                query = query.Where(p => p.Category != null &&
                    p.Category.CategoryName.ToLower() == category.ToLower());

            var posts = await query
                .OrderByDescending(p => p.PublishedAt ?? p.CreatedAt)
                .ToListAsync();

            // Lấy danh sách tên category để render filter pills
            var categories = await _context.BlogPosts
                .Include(p => p.Category)
                .Where(p => p.Status == "Published" && p.Category != null)
                .Select(p => p.Category!.CategoryName)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            var vm = new BlogViewModel
            {
                FeaturedPosts = posts
                    .Where(p => p.IsFeatured)
                    .Take(4)
                    .Select(MapToItem)
                    .ToList(),

                OtherPosts = posts
                    .Where(p => !p.IsFeatured)
                    .Select(MapToItem)
                    .ToList(),

                Categories = categories,
                ActiveCategory = category
            };

            return View(vm);
        }

        // GET /Blog/Details/5  — đọc chi tiết bài viết (giữ route "Details" cho tương thích view)
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var post = await _context.BlogPosts
                .Include(p => p.Category)
                .Include(p => p.Comments!.Where(c => c.Status == "Approved"))
                .FirstOrDefaultAsync(p => p.PostId == id && p.Status == "Published");

            if (post == null) return NotFound();

            post.ViewCount++;
            await _context.SaveChangesAsync();

            return View(post);
        }

        // GET /Blog/Read/5  — alias cho Details (dùng trong Read.cshtml của AdminBlog)
        [HttpGet]
        public async Task<IActionResult> Read(int id) => await Details(id);

        // ── helper ──
        private static PostItemViewModel MapToItem(BlogPost p) => new()
        {
            PostId = p.PostId,
            Title = p.Title,
            Summary = p.Excerpt ?? string.Empty,
            ImageUrl = p.FeaturedImage ?? string.Empty,
            Category = p.Category?.CategoryName ?? string.Empty,
            Author = string.Empty,   // AuthorId không map sang tên, để trống hoặc hardcode
            PublishDate = (p.PublishedAt ?? p.CreatedAt).ToString("dd/MM/yyyy"),
            ViewCount = p.ViewCount,
            IsFeatured = p.IsFeatured
        };
    }
}
