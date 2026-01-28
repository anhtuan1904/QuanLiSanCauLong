using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLiSanCauLong.Models
{
    public class Product
    {
        [Key]
        public int ProductId { get; set; }

        [Required]
        public int CategoryId { get; set; }

        [StringLength(50)]
        public string ProductCode { get; set; }

        [Required]
        [StringLength(200)]
        public string ProductName { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? CostPrice { get; set; }

        public string ImageUrl { get; set; }

        [StringLength(20)]
        public string Unit { get; set; }

        // Thêm thuộc tính này để khớp với View
        public string Status { get; set; } = "InStock";

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [ForeignKey("CategoryId")]
        public virtual ProductCategory Category { get; set; }
        public virtual ICollection<Inventory> Inventories { get; set; }
        public virtual ICollection<OrderDetail> OrderDetails { get; set; }
    }
}
