using System.ComponentModel.DataAnnotations;

namespace QuanLiSanCauLong.Models
{
    public class Facility
    {
        [Key]
        public int FacilityId { get; set; }

        [Required(ErrorMessage = "Tên cơ sở không được để trống")]
        [StringLength(200)]
        public string FacilityName { get; set; }

        [StringLength(300)]
        public string? Address { get; set; }

        [StringLength(100)]
        public string? District { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(20)]
        public string? Phone { get; set; }

        // ImageUrl dùng để lưu ảnh đại diện nhanh (primary image path)
        public string? ImageUrl { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        public TimeSpan? OpenTime { get; set; }
        public TimeSpan? CloseTime { get; set; }

        // Tọa độ Google Maps
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        // Tiện ích đi kèm: lưu dạng comma-separated, vd: "parking,wifi,canteen,shower,rental"
        [StringLength(500)]
        public string? Amenities { get; set; }

        public string Status { get; set; } = "Active";
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
        public virtual ICollection<Court> Courts { get; set; } = new List<Court>();
        public virtual ICollection<PriceSlot> PriceSlots { get; set; } = new List<PriceSlot>();
        public virtual ICollection<FacilityImage> FacilityImages { get; set; } = new List<FacilityImage>();
    }
}
