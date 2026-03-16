// Controllers/BlogController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiSanCauLong.Data;
using QuanLiSanCauLong.ViewModels;

namespace QuanLiSanCauLong.Controllers
{
    public class BlogController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BlogController(ApplicationDbContext context)
        {
            _context = context;
        }

        // FIX 1: Dùng BlogPosts thay vì Posts
        // FIX 3: Lọc Status == "Published" thay vì IsActive
        // FIX 5: Include Category để tránh NullReferenceException
        // FIX 6: Set Categories cho ViewModel để filter hiện đúng
        // FIX 7: Nhận và xử lý query param ?category=
        public async Task<IActionResult> Index(string? category = null)
        {
            var query = _context.BlogPosts
                .Include(p => p.Category)
                .Where(p => p.Status == "Published")
                .AsQueryable();

            // FIX 7: Lọc theo category nếu có
            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(p => p.Category != null && p.Category.CategoryName == category);
            }

            var posts = await query
                .OrderByDescending(p => p.PublishedAt ?? p.CreatedAt)
                .ToListAsync();

            // FIX 6: Lấy danh sách category từ các bài đã Published để hiện filter pills
            var categories = await _context.BlogPosts
                .Include(p => p.Category)
                .Where(p => p.Status == "Published" && p.Category != null)
                .Select(p => p.Category!.CategoryName)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            var viewModel = new BlogViewModel
            {
                FeaturedPosts = posts.Where(p => p.IsFeatured).Select(MapToPostItem).ToList(),
                OtherPosts = posts.Where(p => !p.IsFeatured).Select(MapToPostItem).ToList(),
                Categories = categories,
                ActiveCategory = category
            };

            return View(viewModel);
        }

        // FIX 4: Tăng ViewCount mỗi lần xem chi tiết
        // FIX 5: Include Category để tránh null
        public async Task<IActionResult> Details(int id)
        {
            var post = await _context.BlogPosts
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.PostId == id && p.Status == "Published");

            if (post == null) return NotFound();

            // Tăng lượt xem
            post.ViewCount++;
            await _context.SaveChangesAsync();

            return View(post);
        }

        // FIX 2: Dùng FeaturedImage thay vì ImageUrl
        private static PostItemViewModel MapToPostItem(Models.BlogPost p)
        {
            return new PostItemViewModel
            {
                PostId = p.PostId,
                Title = p.Title,
                Summary = p.Excerpt ?? string.Empty,
                ImageUrl = p.FeaturedImage ?? "/images/blog-default.jpg",
                Category = p.Category?.CategoryName ?? string.Empty,
                Author = string.Empty,
                PublishDate = (p.PublishedAt ?? p.CreatedAt).ToString("dd 'Tháng' MM, yyyy")
            };
        }
    }
}
