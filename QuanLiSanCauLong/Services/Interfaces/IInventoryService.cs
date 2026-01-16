using Microsoft.AspNetCore.Mvc;
using QuanLiSanCauLong.Models;
using QuanLiSanCauLong.ViewModels;

namespace QuanLiSanCauLong.Services.Interfaces
{
    public interface IInventoryService
    {
        Task<bool> UpdateStockAsync(int productId, int facilityId, int quantity, string transactionType);

        Task<StockTransaction> CreateStockTransactionAsync(
            int facilityId, string transactionType, int createdBy,
            List<StockTransactionItemViewModel> items, string note = null);

        Task<List<InventoryItemViewModel>> GetLowStockItemsAsync(int? facilityId = null);

        Task<int> GetStockQuantityAsync(int productId, int facilityId);

        Task<bool> CheckStockAsync(int productId, int facilityId, int requiredQuantity);

        Task<List<StockTransaction>> GetTransactionHistoryAsync(
            int? facilityId, DateTime? fromDate, DateTime? toDate);
    }
}
