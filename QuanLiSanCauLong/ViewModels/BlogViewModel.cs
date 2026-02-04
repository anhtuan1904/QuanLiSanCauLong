using Microsoft.AspNetCore.Mvc;

namespace QuanLiSanCauLong.ViewModels
{
    public class BlogViewModel
    {
        // Danh sách tin nổi bật (Hiển thị phần trên cùng)
        public List<PostItemViewModel> FeaturedPosts { get; set; }

        // Danh sách tin tức khác (Hiển thị phần dưới)
        public List<PostItemViewModel> OtherPosts { get; set; }

        // Danh sách các danh mục để lọc (Giải đấu, Sức khỏe...)
        public List<string> Categories { get; set; }
    }

    public class PostItemViewModel
    {
        public int PostId { get; set; }
        public string Title { get; set; }
        public string Summary { get; set; }
        public string ImageUrl { get; set; }
        public string Category { get; set; }
        public string Author { get; set; }
        public string PublishDate { get; set; } // Định dạng: 28 Tháng 1, 2026
    }
}
