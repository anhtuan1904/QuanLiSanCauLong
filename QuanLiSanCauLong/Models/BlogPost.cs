    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    namespace QuanLiSanCauLong.Models
    {
        /// <summary>
        /// Model cho bài viết/tin tức
        /// </summary>
        public class BlogPost
        {
            [Key]
            public int PostId { get; set; }

            [Required(ErrorMessage = "Tiêu đề không được để trống")]
            [StringLength(200)]
            public string Title { get; set; } = string.Empty;

            [Required(ErrorMessage = "Slug không được để trống")]
            [StringLength(250)]
            public string Slug { get; set; } = string.Empty;

            public string? Excerpt { get; set; }

            [Required(ErrorMessage = "Nội dung không được để trống")]
            public string Content { get; set; } = string.Empty;

            public string? FeaturedImage { get; set; }

            [Required]
            public int CategoryId { get; set; }

            public string? Tags { get; set; }

            public string? MetaTitle { get; set; }
            public string? MetaDescription { get; set; }
            public string? MetaKeywords { get; set; }

            public int ViewCount { get; set; } = 0;

            [StringLength(50)]
            public string Status { get; set; } = "Draft"; // Draft, Published, Archived

            public bool IsFeatured { get; set; } = false;
            public bool AllowComments { get; set; } = true;

            public string? AuthorId { get; set; }
            public DateTime CreatedAt { get; set; } = DateTime.Now;
            public DateTime? PublishedAt { get; set; }
            public DateTime UpdatedAt { get; set; } = DateTime.Now;

            // Navigation
            public virtual BlogCategory? Category { get; set; }
            public virtual ICollection<BlogComment>? Comments { get; set; }
        }

        /// <summary>
        /// Danh mục bài viết
        /// </summary>
        public class BlogCategory
        {
            [Key]
            public int CategoryId { get; set; }

            [Required]
            [StringLength(100)]
            public string CategoryName { get; set; } = string.Empty;

            [Required]
            [StringLength(150)]
            public string Slug { get; set; } = string.Empty;

            public string? Description { get; set; }
            public string? Icon { get; set; }
            public int DisplayOrder { get; set; } = 0;
            public bool IsActive { get; set; } = true;

            public DateTime CreatedAt { get; set; } = DateTime.Now;

            // Navigation
            public virtual ICollection<BlogPost>? Posts { get; set; }
        }

        /// <summary>
        /// Bình luận bài viết
        /// </summary>
        public class BlogComment
        {
            [Key]
            public int CommentId { get; set; }

            [Required]
            public int PostId { get; set; }

            public string? UserId { get; set; }

            [Required]
            [StringLength(100)]
            public string AuthorName { get; set; } = string.Empty;

            [StringLength(100)]
            public string? AuthorEmail { get; set; }

            [Required]
            public string Content { get; set; } = string.Empty;

            public int? ParentCommentId { get; set; }

            [StringLength(20)]
            public string Status { get; set; } = "Pending"; // Pending, Approved, Spam

            public DateTime CreatedAt { get; set; } = DateTime.Now;

            // Navigation
            public virtual BlogPost? Post { get; set; }
            public virtual BlogComment? ParentComment { get; set; }
            public virtual ICollection<BlogComment>? Replies { get; set; }
        }
    }
