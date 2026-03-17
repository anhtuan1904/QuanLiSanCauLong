namespace QuanLiSanCauLong.ViewModels
{
    /// <summary>
    /// ViewModel cho trang Blog công khai (Views/Blog/Index.cshtml).
    /// </summary>
    public class BlogViewModel
    {
        public List<PostItemViewModel> FeaturedPosts { get; set; } = new();
        public List<PostItemViewModel> OtherPosts { get; set; } = new();

        // Danh sách tên category để render filter pills trong Index.cshtml
        public List<string> Categories { get; set; } = new();

        // Category đang được filter (để highlight pill active)
        public string? ActiveCategory { get; set; }
    }

    /// <summary>
    /// ViewModel đại diện một bài viết trong danh sách công khai.
    /// Được map từ BlogPost trong BlogController.MapToItem().
    /// </summary>
    public class PostItemViewModel
    {
        public int PostId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;

        // FeaturedImage của BlogPost được map vào đây
        public string ImageUrl { get; set; } = string.Empty;

        // Tên danh mục (hiển thị badge trên card)
        public string Category { get; set; } = string.Empty;

        // Tên tác giả
        public string Author { get; set; } = string.Empty;

        public string PublishDate { get; set; } = string.Empty;
        public int ViewCount { get; set; }
        public bool IsFeatured { get; set; }
    }
}
