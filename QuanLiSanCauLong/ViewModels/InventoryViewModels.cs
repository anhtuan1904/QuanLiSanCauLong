using Microsoft.AspNetCore.Mvc;
using QuanLiSanCauLong.Models;
using System.ComponentModel.DataAnnotations;

namespace QuanLiSanCauLong.ViewModels
{
    /// <summary>
    /// ViewModel cho danh sách kho hàng
    /// </summary>
    public class InventoryListViewModel
    {
        public int FacilityId { get; set; }
        public string FacilityName { get; set; }
        public List<InventoryItemViewModel> Items { get; set; }

        // Thêm 2 dòng này để tính toán tự động dựa trên danh sách Items
        public int TotalProducts => Items?.Count ?? 0;
        public int LowStockCount => Items?.Count(i => i.IsLowStock) ?? 0;
    }

    /// <summary>
    /// ViewModel cho từng sản phẩm trong kho
    /// </summary>
    public class InventoryItemViewModel
    {
        public int InventoryId { get; set; }

        public string FacilityName { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductCode { get; set; }
        public string CategoryName { get; set; }
        public string CategoryType { get; set; }
        public int Quantity { get; set; }
        public int MinQuantity { get; set; }
        public int MaxQuantity { get; set; }
        public bool IsLowStock => Quantity <= MinQuantity;
        public string Unit { get; set; }
        public decimal Price { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    /// <summary>
    /// ViewModel cho giao dịch nhập/xuất kho
    /// </summary>
    public class StockTransactionViewModel
    {
        [Required]
        public int FacilityId { get; set; }

        [Required]
        [Display(Name = "Loại giao dịch")]
        public string TransactionType { get; set; } // Import, Export, Adjustment

        // Bổ sung thuộc tính này để hết lỗi
        [Display(Name = "Ngày giao dịch")]
        public DateTime TransactionDate { get; set; } = DateTime.Now;

        [Display(Name = "Ghi chú")]
        public string Note { get; set; }

        public List<StockTransactionItemViewModel> Items { get; set; }
        public decimal TotalAmount => Items?.Sum(i => i.TotalPrice) ?? 0;
    }

    /// <summary>
    /// ViewModel cho chi tiết sản phẩm trong giao dịch kho
    /// </summary>
    public class StockTransactionItemViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice => Quantity * UnitPrice;
    }

}
