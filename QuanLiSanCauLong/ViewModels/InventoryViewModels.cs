// ===================================================================
// FILE: ViewModels/InventoryViewModels.cs
// ===================================================================
using System;
using System.Collections.Generic;

namespace QuanLiSanCauLong.ViewModels
{
    public class InventoryListViewModel
    {
        public int FacilityId { get; set; }
        public string? FacilityName { get; set; }
        public List<InventoryItemViewModel> Items { get; set; } = new();
    }

        public class InventoryItemViewModel
        {
            public int InventoryId { get; set; }
            public int ProductId { get; set; }
            public string ProductName { get; set; } = "";
            public string? ProductCode { get; set; }
            public string? ImageUrl { get; set; }   // ✅ THÊM — fix CS0117
            public string? CategoryName { get; set; }
            // CategoryType đã xóa — dùng BehaviorType
            public string BehaviorType { get; set; } = "Retail";
            public string? FacilityName { get; set; }
            public int FacilityId { get; set; }
            public string? Unit { get; set; }
            public decimal Price { get; set; }
            public DateTime LastUpdated { get; set; }

            public int Quantity { get; set; }
            public int HoldQuantity { get; set; }
            public int RentedQuantity { get; set; }
            public int DamagedQuantity { get; set; }
            public int AvailableQuantity { get; set; }
            public int MinQuantity { get; set; }
            public int MaxQuantity { get; set; }
            public bool IsLowStock { get; set; }
        }
    

    public class StockTransactionViewModel
    {
        public int FacilityId { get; set; }
        public int? SupplierId { get; set; }
        public string? TransactionType { get; set; }
        public DateTime TransactionDate { get; set; } = DateTime.Now;
        public string? DocumentReference { get; set; }
        public string? BatchNumber { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? Reason { get; set; }
        public string? Note { get; set; }
        public List<StockTransactionItemViewModel> Items { get; set; } = new();
    }

    public class StockTransactionItemViewModel
    {
        public int ProductId { get; set; }
        public int? VariantId { get; set; }
        public int Quantity { get; set; }
        public string? Unit { get; set; }
        public decimal CostPrice { get; set; }
    }

    public class ActiveRentalsViewModel
    {
        public List<RentalItemRowViewModel> Items { get; set; } = new();
        public int TotalActive { get; set; }
        public int TotalOverdue { get; set; }
        public int TotalReturned { get; set; }
        public int TotalLost { get; set; }
    }

    public class RentalItemRowViewModel
    {
        public int RentalItemId { get; set; }
        public int InventoryId { get; set; }
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? ProductCode { get; set; }
        public string? CategoryName { get; set; }
        public string? FacilityName { get; set; }
        public string? CourtCode { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public int Quantity { get; set; }
        public string? Size { get; set; }
        public string? Unit { get; set; }
        public DateTime RentedAt { get; set; }
        public DateTime? ExpectedReturnAt { get; set; }
        public DateTime? ReturnedAt { get; set; }
        public string? Status { get; set; }
        public bool IsOverdue { get; set; }
        public double DurationHours { get; set; }
        public string? Note { get; set; }
        public decimal CleaningFeeCharged { get; set; }
        public string? CreatedBy { get; set; }
    }

    public class LowStockViewModel
    {
        public List<LowStockItemViewModel> Items { get; set; } = new();
        public int OutOfStockCount { get; set; }
        public int LowStockCount { get; set; }
        public int ExpiringCount { get; set; }
        public int ExpiredCount { get; set; }
        public int DamagedCount { get; set; }
    }

    public class LowStockItemViewModel
    {
        public int InventoryId { get; set; }
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? ProductCode { get; set; }
        public string? ImageUrl { get; set; }
        public string? CategoryName { get; set; }
        public string? BehaviorType { get; set; }
        public string? FacilityName { get; set; }
        public string? Unit { get; set; }
        public string? SupplierName { get; set; }
        public string? BatchNumber { get; set; }
        public string? StorageLocation { get; set; }
        public int Quantity { get; set; }
        public int AvailableQuantity { get; set; }
        public int HoldQuantity { get; set; }
        public int RentedQuantity { get; set; }
        public int DamagedQuantity { get; set; }
        public int MinQuantity { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public int? DaysUntilExpiry { get; set; }
        public bool IsOutOfStock { get; set; }
        public bool IsLowStock { get; set; }
        public bool IsExpired { get; set; }
        public bool IsExpiringSoon { get; set; }
        public bool HasDamaged { get; set; }
        public List<LowStockVariantRow> LowVariants { get; set; } = new();
    }

    public class LowStockVariantRow
    {
        public string? DisplayName { get; set; }
        public int AvailableQty { get; set; }
    }

    public class InventoryHistoryViewModel
    {
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? ProductCode { get; set; }
        public List<InventoryTransactionRowViewModel> Transactions { get; set; } = new();
    }

    public class InventoryTransactionRowViewModel
    {
        public int TransactionId { get; set; }
        public string? Type { get; set; }
        public int Quantity { get; set; }

        // ✅ int (không nullable) — dùng > 0 để check, không dùng .HasValue
        public int QuantityAfter { get; set; }

        public string? FacilityName { get; set; }
        public int? OrderId { get; set; }
        public string? Note { get; set; }
        public string? UserEmail { get; set; }
        public DateTime TransactionDate { get; set; }
        public decimal? CostPriceSnapshot { get; set; }  // nullable decimal → dùng .HasValue OK
        public string? BatchNumber { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public int? TargetFacilityId { get; set; }

        public bool IsInbound => Type is "StockIn" or "RentReturn" or "ReleaseHold";
        public bool IsOutbound => Type is "StockOut" or "Sale" or "Hold"
                                       or "RentOut" or "RentLost" or "DamagedWrite";
        public bool IsNeutral => !IsInbound && !IsOutbound;
    }
}
