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
        public string? Address { get; set; } // Thêm ? để cho phép Null

        [StringLength(100)]
        public string? District { get; set; } // Thêm ?

        [StringLength(100)]
        public string? City { get; set; } // Thêm ?

        [StringLength(20)]
        public string? Phone { get; set; } // Thêm ?

        // ImageUrl nên để Nullable vì chúng ta dùng bảng FacilityImages để lưu ảnh
        public string? ImageUrl { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; } // Thêm ? để hết lỗi "Description is required"

        public TimeSpan? OpenTime { get; set; }
        public TimeSpan? CloseTime { get; set; }

        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

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