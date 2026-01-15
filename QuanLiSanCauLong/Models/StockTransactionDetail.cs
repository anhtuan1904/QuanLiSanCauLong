using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLiSanCauLong.Models
{
    public class StockTransactionDetail
    {
        [Key]
        public int DetailId { get; set; }

        [Required]
        public int TransactionId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? UnitPrice { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? TotalPrice { get; set; }

        [ForeignKey("TransactionId")]
        public virtual StockTransaction Transaction { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }
    }
}
