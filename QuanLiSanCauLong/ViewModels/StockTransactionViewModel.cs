using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace QuanLiSanCauLong.ViewModels
{
    public class StockTransactionViewModel
    {
        [Required]
        public int FacilityId { get; set; }

        [Required]
        [Display(Name = "Loại giao dịch")]
        public string TransactionType { get; set; } // Import, Export, Adjustment

        [Display(Name = "Ghi chú")]
        public string Note { get; set; }

        public List<StockTransactionItemViewModel> Items { get; set; }
        public decimal TotalAmount => Items?.Sum(i => i.TotalPrice) ?? 0;
    }
}
