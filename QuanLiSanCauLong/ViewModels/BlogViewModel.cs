// ViewModels/BlogViewModel.cs
namespace QuanLiSanCauLong.ViewModels
{
    public class BlogViewModel
    {
        public List<PostItemViewModel> FeaturedPosts { get; set; } = new();
        public List<PostItemViewModel> OtherPosts { get; set; } = new();

        // FIX 6: Danh sách category để render filter pills trong Index.cshtml
        public List<string> Categories { get; set; } = new();

        // FIX 7: Category đang được filter (để highlight pill active)
        public string? ActiveCategory { get; set; }
    }

    public class PostItemViewModel
    {
        public int PostId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;

        // FIX 2: Tên field đồng nhất — controller map FeaturedImage → ImageUrl
        public string ImageUrl { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string PublishDate { get; set; } = string.Empty;
    }
}
