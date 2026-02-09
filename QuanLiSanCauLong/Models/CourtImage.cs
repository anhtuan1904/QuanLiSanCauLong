using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLiSanCauLong.Models
{
    public class CourtImage
    {
        [Key]
        public int ImageId { get; set; }

        [Required]
        public int CourtId { get; set; }

        [Required]
        [StringLength(500)]
        public string ImagePath { get; set; }

        public bool IsPrimary { get; set; } = false;

        public int DisplayOrder { get; set; } = 0;

        public DateTime UploadedAt { get; set; } = DateTime.Now;

        [ForeignKey("CourtId")]
        public virtual Court Court { get; set; }
    }
}