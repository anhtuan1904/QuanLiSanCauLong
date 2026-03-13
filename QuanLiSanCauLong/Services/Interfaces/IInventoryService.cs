using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using QuanLiSanCauLong.Models;

namespace QuanLiSanCauLong.Services.Interfaces
{
    public interface IInventoryService
    {
        // 1. Nhập kho
        Task<(bool ok, string msg)> StockInAsync(int facilityId, int productId, int quantity, decimal costPrice = 0, string batchNumber = null, DateTime? expiryDate = null, string docRef = null, string userEmail = null, string note = null);

        // 2. Treo hàng (đơn chưa thanh toán)
        Task<(bool ok, string msg)> HoldAsync(int facilityId, int productId, int quantity, int orderId, string userEmail = null);

        // 3. Xác nhận bán (đã thanh toán)
        Task<(bool ok, string msg)> ConfirmSaleAsync(int facilityId, int productId, int quantity, decimal salePrice, decimal costPrice, int orderId, string userEmail = null);

        // 4. Huỷ đơn / Release Hold
        Task<(bool ok, string msg)> ReleaseHoldAsync(int facilityId, int productId, int quantity, int orderId, string userEmail = null, string note = null);

        // 5. Xuất cho thuê
        Task<(bool ok, string msg, int rentalItemId)> RentalOutAsync(int facilityId, int productId, int quantity, string courtCode = null, string customerName = null, string customerPhone = null, string size = null, int? maxHours = null, int? orderId = null, string userEmail = null, string note = null);

        // 6. Trả đồ thuê
        Task<(bool ok, string msg)> RentalReturnAsync(int rentalItemId, int returnQty, bool isDamaged = false, decimal cleaningFee = 0, string userEmail = null, string note = null);

        // 7. Mất đồ thuê
        Task<(bool ok, string msg)> RentalLostAsync(int rentalItemId, int lostQty, string userEmail = null, string note = null);

        // 8. Đánh dấu hàng hỏng từ kho
        Task<(bool ok, string msg)> MarkDamagedAsync(int inventoryId, int quantity, string userEmail = null, string note = null);

        // 9. Xoá sổ hàng hỏng
        Task<(bool ok, string msg)> WriteOffDamagedAsync(int inventoryId, int quantity, string userEmail = null, string note = null);

        // 10. Điều chỉnh kiểm kê (Audit)
        Task<(bool ok, string msg)> AdjustAsync(int inventoryId, int newQuantity, string userEmail = null, string note = null);

        // 11. Xuất kho thủ công
        Task<(bool ok, string msg)> StockOutAsync(int facilityId, int productId, int quantity, string reason = null, string userEmail = null, string note = null);

        // 12. Chuyển kho giữa các cơ sở
        Task<(bool ok, string msg)> TransferAsync(int fromInventoryId, int targetFacilityId, int quantity, string userEmail = null, string note = null);

        // 13. Lấy danh sách đơn treo quá lâu
        Task<List<InventoryTransaction>> GetStalledHoldsAsync(int olderThanMinutes = 120);
    }
}