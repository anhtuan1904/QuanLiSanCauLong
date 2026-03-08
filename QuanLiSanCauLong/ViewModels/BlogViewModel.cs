using System.Collections.Generic;

namespace QuanLiSanCauLong.ViewModels
{
    public class BlogViewModel
    {
        // Danh sách tin nổi bật (Hiển thị phần trên cùng)
        public List<PostItemViewModel> FeaturedPosts { get; set; } = new();
        // Danh sách tin tức khác (Hiển thị phần dưới)
        public List<PostItemViewModel> OtherPosts { get; set; } = new();
        // Danh sách các danh mục để lọc (Giải đấu, Sức khỏe...)
        public List<string> Categories { get; set; } = new();
    }

    public class PostItemViewModel
    {
        public int PostId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string PublishDate { get; set; } = string.Empty; // Định dạng: 28 Tháng 1, 2026
    }
}
