using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLiSanCauLong.Models
{
    [Table("Tournaments")]
    public class Tournament
    {
        [Key]
        public int TournamentId { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Tên Giải Đấu")]
        public string TournamentName { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Loại Giải")]
        public string? TournamentType { get; set; } // Đơn Nam/Nữ, Đôi Nam/Nữ/Mix

        [StringLength(500)]
        [Display(Name = "Mô Tả Ngắn")]
        public string? ShortDesc { get; set; }

        [Display(Name = "Mô Tả Chi Tiết")]
        public string? Description { get; set; }

        [Display(Name = "Ngày Bắt Đầu")]
        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [Display(Name = "Ngày Kết Thúc")]
        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        [Display(Name = "Hạn Đăng Ký")]
        [DataType(DataType.Date)]
        public DateTime? RegistrationDeadline { get; set; }

        [StringLength(300)]
        [Display(Name = "Địa Điểm Tổ Chức")]
        public string? Venue { get; set; }

        [Display(Name = "Số VĐV Tối Đa")]
        public int? MaxPlayers { get; set; }

        [Display(Name = "Số VĐV Hiện Tại")]
        public int CurrentPlayers { get; set; } = 0;

        [Display(Name = "Phí Đăng Ký (VNĐ)")]
        [Column(TypeName = "decimal(18,0)")]
        public decimal? EntryFee { get; set; }

        [Display(Name = "Tổng Giải Thưởng (VNĐ)")]
        [Column(TypeName = "decimal(18,0)")]
        public decimal? PrizeMoney { get; set; }

        [Display(Name = "Mô Tả Giải Thưởng")]
        public string? PrizeDescription { get; set; }

        [Display(Name = "Thể Lệ Giải Đấu")]
        public string? Rules { get; set; }

        [StringLength(500)]
        [Display(Name = "Ảnh Đại Diện")]
        public string? FeaturedImage { get; set; }

        [StringLength(20)]
        [Display(Name = "Trạng Thái")]
        public string Status { get; set; } = "Upcoming"; // Upcoming / Ongoing / Completed / Cancelled

        [Display(Name = "Nổi Bật")]
        public bool IsFeatured { get; set; } = false;

        [Display(Name = "Thứ Tự Hiển Thị")]
        public int DisplayOrder { get; set; } = 0;

        [Display(Name = "Lượt Xem")]
        public int ViewCount { get; set; } = 0;

        [StringLength(300)]
        public string? Slug { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        // Computed
        [NotMapped]
        public int AvailableSlots => (MaxPlayers ?? 0) - CurrentPlayers;

        [NotMapped]
        public bool IsFull => MaxPlayers.HasValue && CurrentPlayers >= MaxPlayers;

        [NotMapped]
        public string StatusLabel => Status switch
        {
            "Upcoming" => "Sắp Diễn Ra",
            "Ongoing" => "Đang Hoạt Động",
            "Completed" => "Đã Đầy",
            "Cancelled" => "Đã Hủy",
            _ => Status
        };

        [NotMapped]
        public string StatusBadge => Status switch
        {
            "Upcoming" => "warning",
            "Ongoing" => "success",
            "Completed" => "primary",
            "Cancelled" => "danger",
            _ => "secondary"
        };
    }
}
