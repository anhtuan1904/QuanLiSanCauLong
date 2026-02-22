using QuanLiSanCauLong.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("BlogReviewLikes")]
public class BlogReviewLike
{
    [Key]
    public int LikeId { get; set; }

    [Required]
    public int ReviewId { get; set; }

    [StringLength(450)]
    public string? UserId { get; set; }

    [StringLength(45)]
    public string? IpAddress { get; set; }

    // true = like, false = dislike
    public bool IsLike { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public virtual BlogReview? Review { get; set; }
}

