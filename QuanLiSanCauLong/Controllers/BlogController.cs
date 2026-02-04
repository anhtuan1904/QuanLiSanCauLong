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

        // Trang danh sách tin tức (khớp với file Blog/Index.cshtml)
        public async Task<IActionResult> Index()
        {
            var posts = await _context.Posts.Where(p => p.IsActive).ToListAsync();

            var viewModel = new BlogViewModel
            {
                FeaturedPosts = posts.Where(p => p.IsFeatured).Select(p => MapToPostItem(p)).ToList(),
                OtherPosts = posts.Where(p => !p.IsFeatured).Select(p => MapToPostItem(p)).ToList()
            };
            return View(viewModel); // ASP.NET sẽ tự tìm Views/Blog/Index.cshtml
        }

        // Trang chi tiết bài viết
        public async Task<IActionResult> Details(int id)
        {
            var post = await _context.Posts.FirstOrDefaultAsync(p => p.PostId == id);
            if (post == null) return NotFound();

            return View(post); // Sẽ tạo file Views/Blog/Details.cshtml
        }

        private PostItemViewModel MapToPostItem(Models.Post p)
        {
            return new PostItemViewModel
            {
                PostId = p.PostId,
                Title = p.Title,
                Summary = p.Summary,
                ImageUrl = p.ImageUrl ?? "/images/blog-default.jpg",
                Category = p.Category,
                Author = p.Author,
                PublishDate = p.CreatedAt.ToString("dd 'Tháng' MM, yyyy")
            };
        }
    }
}