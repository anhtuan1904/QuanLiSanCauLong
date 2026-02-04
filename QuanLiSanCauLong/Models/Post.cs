using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace QuanLiSanCauLong.Models
{
    public class Post
    {
        [Key]
        public int PostId { get; set; }

        [Required]
        [StringLength(250)]
        public string Title { get; set; }

        [Required]
        public string Content { get; set; } // Nội dung bài viết (HTML)

        [StringLength(500)]
        public string Summary { get; set; } // Mô tả ngắn hiển thị ở trang danh sách

        public string ImageUrl { get; set; } // Ảnh đại diện bài viết

        [StringLength(100)]
        public string Category { get; set; } // Ví dụ: Giải đấu, Hướng dẫn, Thiết bị, Sức khỏe

        public string Author { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsFeatured { get; set; } = false; // Tin nổi bật

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
