using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLiSanCauLong.Models
{
    public class ProductCategory
    {
        [Key]
        public int CategoryId { get; set; }

        [Required, StringLength(100)]
        [Display(Name = "Tên danh mục")]
        public string CategoryName { get; set; } = "";

        /// <summary>Retail | Rental | Service</summary>
        [Required, StringLength(10)]
        [Display(Name = "Loại nghiệp vụ")]
        public string BehaviorType { get; set; } = "Retail";

        [StringLength(500)]
        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [Display(Name = "Hình ảnh")]
        public string? ImageUrl { get; set; }

        [StringLength(20)]
        [Display(Name = "Đơn vị mặc định")]
        public string DefaultUnit { get; set; } = "Cái";

        // ── Retail ──────────────────────────────────────────────────
        /// <summary>Theo dõi hạn sử dụng (kéo theo FIFO tự động)</summary>
        [Display(Name = "Theo dõi hạn sử dụng")]
        public bool RequiresExpiry { get; set; } = false;

        /// <summary>Có biến thể màu/size</summary>
        [Display(Name = "Có biến thể")]
        public bool HasVariants { get; set; } = false;

        /// <summary>Tồn kho tối thiểu cảnh báo</summary>
        [Display(Name = "Tồn kho tối thiểu")]
        public int DefaultMinStock { get; set; } = 0;

        // ── Rental ──────────────────────────────────────────────────
        [Display(Name = "Cho phép trả từng phần")]
        public bool AllowPartialReturn { get; set; } = false;

        [Display(Name = "Yêu cầu đặt cọc")]
        public bool DepositRequired { get; set; } = false;

        [Column(TypeName = "decimal(18,0)")]
        [Display(Name = "Tiền đặt cọc mặc định")]
        public decimal DefaultDepositAmount { get; set; } = 0;

        // ── Meta ─────────────────────────────────────────────────────
        [Display(Name = "Thứ tự hiển thị")]
        public int DisplayOrder { get; set; } = 0;

        [Display(Name = "Kích hoạt")]
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Navigation
        public virtual ICollection<Product>? Products { get; set; }
    }
}
