using Microsoft.AspNetCore.Mvc;

namespace QuanLiSanCauLong.ViewModels
{
    public class InventoryListViewModel
    {
        public int FacilityId { get; set; }
        public string FacilityName { get; set; }
        public List<InventoryItemViewModel> Items { get; set; }
        public int LowStockCount => Items?.Count(i => i.IsLowStock) ?? 0;
    }
}
