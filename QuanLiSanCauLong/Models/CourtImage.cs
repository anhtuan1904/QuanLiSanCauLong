using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLiSanCauLong.Models
{
    public class CourtImage
    {
        [Key]
        public int ImageId { get; set; }

        // ✅ Alias NotMapped để controller/ViewModel có thể dùng CourtImageId
        // mà không cần sửa DB migration
        [NotMapped]
        public int CourtImageId { get => ImageId; set => ImageId = value; }

        [Required]
        public int CourtId { get; set; }

        [Required]
        [StringLength(500)]
        public string ImagePath { get; set; }
        public string? Caption { get; set; }

        public bool IsPrimary { get; set; } = false;

        public int DisplayOrder { get; set; } = 0;

        public DateTime UploadedAt { get; set; } = DateTime.Now;

        [ForeignKey("CourtId")]
        public virtual Court Court { get; set; }
    }
}