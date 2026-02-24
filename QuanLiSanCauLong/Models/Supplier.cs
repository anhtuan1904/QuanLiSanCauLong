using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

// QUAN TRỌNG: Supplier và Product phải cùng namespace
namespace QuanLiSanCauLong.Models
{
    public class Supplier
    {
        [Key]
        public int SupplierId { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Tên nhà cung cấp")]
        public string SupplierName { get; set; }

        [StringLength(100)]
        [Display(Name = "Người liên hệ")]
        public string ContactPerson { get; set; }

        [StringLength(20)]
        [Display(Name = "Số điện thoại")]
        public string Phone { get; set; }

        [StringLength(200)]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [StringLength(500)]
        [Display(Name = "Địa chỉ")]
        public string Address { get; set; }

        [StringLength(200)]
        [Display(Name = "Ghi chú")]
        public string Note { get; set; }

        // Navigation — ICollection<Product> resolve được vì cùng namespace Models
        public virtual ICollection<Product> Products { get; set; }
    }
}
