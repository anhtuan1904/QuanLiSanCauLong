using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLiSanCauLong.Models
{
    public class Tournament
    {
        [Key]
        public int TournamentId { get; set; }

        [Required(ErrorMessage = "Tên giải đấu không được để trống")]
        [StringLength(200)]
        public string TournamentName { get; set; } = string.Empty;

        [Required]
        [StringLength(250)]
        public string Slug { get; set; } = string.Empty;

        [StringLength(50)]
        public string? TournamentType { get; set; }

        public string? ShortDesc { get; set; }

        [Required(ErrorMessage = "Mô tả chi tiết không được để trống")]
        public string Description { get; set; } = string.Empty;

        public string? FeaturedImage { get; set; }
        public string? VideoUrl { get; set; }

        // Thời gian
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? RegistrationDeadline { get; set; }

        // Địa điểm
        [StringLength(200)]
        public string? Venue { get; set; }

        [StringLength(300)]
        public string? VenueAddress { get; set; }

        public string? MapUrl { get; set; }

        // Người chơi
        public int? MaxPlayers { get; set; }
        public int CurrentPlayers { get; set; } = 0;

        // Giải thưởng
        [Column(TypeName = "decimal(18,2)")]
        public decimal? EntryFee { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? PrizeMoney { get; set; }

        public string? PrizeDescription { get; set; }

        // Nội dung thu hút
        public string? Rules { get; set; }
        public string? Schedule { get; set; }
        public string? Sponsors { get; set; }

        // Hiển thị
        public int DisplayOrder { get; set; } = 0;

        [StringLength(20)]
        public string Status { get; set; } = "Upcoming";

        public bool IsFeatured { get; set; } = false;

        public int ViewCount { get; set; } = 0;

        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public virtual ICollection<TournamentImage>? TournamentImages { get; set; }

        [NotMapped]
        public int AvailableSlots => (MaxPlayers ?? 0) - CurrentPlayers;

        [NotMapped]
        public bool IsFull => MaxPlayers.HasValue && CurrentPlayers >= MaxPlayers.Value;

        [NotMapped]
        public string StatusLabel => Status switch
        {
            "Upcoming" => "Sắp diễn ra",
            "Ongoing" => "Đang diễn ra",
            "Finished" => "Đã kết thúc",
            "Cancelled" => "Đã hủy",
            _ => Status
        };

        [NotMapped]
        public string StatusBadge => Status switch
        {
            "Upcoming" => "badge-upcoming",
            "Ongoing" => "badge-ongoing",
            "Finished" => "badge-finished",
            "Cancelled" => "badge-cancelled",
            _ => "badge-secondary"
        };
    }

    public class TournamentImage
    {
        [Key]
        public int ImageId { get; set; }

        [Required]
        public int TournamentId { get; set; }

        [Required]
        public string ImagePath { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Caption { get; set; }

        public bool IsPrimary { get; set; } = false;
        public int DisplayOrder { get; set; } = 0;
        public DateTime UploadedAt { get; set; } = DateTime.Now;

        public virtual Tournament? Tournament { get; set; }
    }
}
