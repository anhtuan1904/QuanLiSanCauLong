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
        // ===== FIX: Thêm FacilityId, FacilityName =====
        public int FacilityId { get; set; }
        public string FacilityName { get; set; }

        public List<InventoryItemViewModel> Items { get; set; } = new();
        public int LowStockCount { get; set; }
        public int ExpiringCount { get; set; }
        public int OutOfStockCount { get; set; }
    }

    /// <summary>
    /// ViewModel cho từng sản phẩm trong kho
    /// </summary>
    public class InventoryItemViewModel
    {
        public int InventoryId { get; set; }
        public int ProductId { get; set; }

        // Product info
        public string ProductName { get; set; }
        public string ProductCode { get; set; }     // FIX: thêm (dùng trong Controller & InventoryService)
        public string SKU { get; set; }
        public string Barcode { get; set; }
        public string BatchNumber { get; set; }
        public string ImageUrl { get; set; }
        public string Unit { get; set; }
        public string StorageLocation { get; set; }
        public decimal Price { get; set; }          // FIX: thêm

        // Category
        public string CategoryName { get; set; }
        public string CategoryType { get; set; }    // FIX: thêm

        // Facility
        public int FacilityId { get; set; }
        public string FacilityName { get; set; }

        // Stock numbers
        public int Quantity { get; set; }
        public int MinQuantity { get; set; }
        public int MaxQuantity { get; set; }

        // --- FIX LỖI: Thêm bí danh MinStockLevel để khớp với code ở View/Controller ---
        public int MinStockLevel { get => MinQuantity; set => MinQuantity = value; }

        // Timestamps
        public DateTime LastUpdated { get; set; }   // FIX: thêm

        // Expiry
        public DateTime? ExpiryDate { get; set; }

        // Supplier
        public string SupplierName { get; set; }

        // Computed
        public bool IsLowStock => MinQuantity > 0 && Quantity <= MinQuantity;
        public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value < DateTime.Now;
        public bool IsExpiringSoon => ExpiryDate.HasValue && !IsExpired && ExpiryDate.Value <= DateTime.Now.AddDays(30);
    }
    public class StockTransactionViewModel
    {
        public int FacilityId { get; set; }
        public string TransactionType { get; set; }     // FIX: thêm (Import / Export)
        public DateTime TransactionDate { get; set; } = DateTime.Now;

        public int? SupplierId { get; set; }

        [StringLength(100)]
        public string BatchNumber { get; set; }

        [DataType(DataType.Date)]
        public DateTime? ExpiryDate { get; set; }

        [StringLength(100)]
        public string DocumentReference { get; set; }

        [StringLength(20)]
        public string OutReason { get; set; } = "Sale";

        public string Note { get; set; }

        public List<StockTransactionItemViewModel> Items { get; set; } = new();
    }

    public class StockTransactionItemViewModel
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }

        public string Unit { get; set; }

        public decimal CostPrice { get; set; }
        public decimal UnitPrice { get; set; }      // FIX: thêm (dùng trong InventoryService)
        public decimal TotalPrice { get; set; }     // FIX: thêm (dùng trong InventoryService)
    }
}
