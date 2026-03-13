// ===================================================================
// FILE: Services/InventoryService.cs  (Final – complete)
// Mọi thay đổi kho ĐỀU qua đây. Controller chỉ gọi service.
// ===================================================================
using Microsoft.EntityFrameworkCore;
using QuanLiSanCauLong.Data;
using QuanLiSanCauLong.Models;
using QuanLiSanCauLong.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuanLiSanCauLong.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly ApplicationDbContext _db;
        public InventoryService(ApplicationDbContext db) => _db = db;

        // ─────────────────────────────────────────────────────────────
        // 1. NHẬP KHO
        // ─────────────────────────────────────────────────────────────
        public async Task<(bool ok, string msg)> StockInAsync(
            int facilityId, int productId, int quantity,
            decimal costPrice = 0, string batchNumber = null,
            DateTime? expiryDate = null, string docRef = null,
            string userEmail = null, string note = null)
        {
            if (quantity <= 0) return (false, "Số lượng phải lớn hơn 0.");
            var inv = await GetOrCreateAsync(facilityId, productId);
            inv.Quantity += quantity;
            inv.LastUpdated = DateTime.Now;

            var bNum = batchNumber ?? $"AUTO-{DateTime.Now:yyyyMMddHHmm}";
            _db.InventoryBatches.Add(new InventoryBatch
            {
                InventoryId = inv.InventoryId,
                BatchNumber = bNum,
                OriginalQuantity = quantity,
                RemainingQuantity = quantity,
                CostPrice = costPrice,
                ExpiryDate = expiryDate,
                ReceivedDate = DateTime.Now,
                DocumentReference = docRef,
                Status = "Active"
            });
            Log(productId, facilityId, TxType.StockIn, quantity, inv.Quantity,
                costPrice: costPrice, batchNumber: bNum, expiryDate: expiryDate,
                userEmail: userEmail, note: note);

            await _db.SaveChangesAsync();
            return (true, $"Nhập kho thành công: +{quantity}.");
        }

        // ─────────────────────────────────────────────────────────────
        // 2. TREO HÀNG (đơn chưa thanh toán)
        // ─────────────────────────────────────────────────────────────
        public async Task<(bool ok, string msg)> HoldAsync(
            int facilityId, int productId, int quantity,
            int orderId, string userEmail = null)
        {
            if (quantity <= 0) return (false, "Số lượng phải > 0.");
            var inv = await GetAsync(facilityId, productId);
            if (inv == null) return (false, "Sản phẩm chưa có trong kho.");
            if (inv.AvailableQuantity < quantity) return (false, $"Không đủ khả dụng ({inv.AvailableQuantity}).");

            inv.HoldQuantity += quantity;
            inv.LastUpdated = DateTime.Now;
            Log(productId, facilityId, TxType.SaleHold, quantity, inv.Quantity,
                refId: orderId, refType: "Order", userEmail: userEmail,
                note: $"Treo cho đơn #{orderId}");

            await _db.SaveChangesAsync();
            return (true, "Đã tạm giữ hàng.");
        }

        // ─────────────────────────────────────────────────────────────
        // 3. XÁC NHẬN BÁN (đã thanh toán)
        // ─────────────────────────────────────────────────────────────
        public async Task<(bool ok, string msg)> ConfirmSaleAsync(
            int facilityId, int productId, int quantity,
            decimal salePrice, decimal costPrice,
            int orderId, string userEmail = null)
        {
            if (quantity <= 0) return (false, "Số lượng phải > 0.");
            var inv = await GetAsync(facilityId, productId);
            if (inv == null) return (false, "Không tìm thấy kho.");

            inv.HoldQuantity = Math.Max(0, inv.HoldQuantity - quantity);
            inv.Quantity = Math.Max(0, inv.Quantity - quantity);
            inv.LastUpdated = DateTime.Now;
            await DeductBatchFIFOAsync(inv.InventoryId, quantity);
            Log(productId, facilityId, TxType.SaleConfirmed, quantity, inv.Quantity,
                costPrice: costPrice, salePrice: salePrice,
                refId: orderId, refType: "Order",
                userEmail: userEmail, note: $"Bán đơn #{orderId}");

            await _db.SaveChangesAsync();
            return (true, "Xác nhận bán thành công.");
        }

        // ─────────────────────────────────────────────────────────────
        // 4. HUỶ ĐƠN / RELEASE HOLD
        // ─────────────────────────────────────────────────────────────
        public async Task<(bool ok, string msg)> ReleaseHoldAsync(
            int facilityId, int productId, int quantity,
            int orderId, string userEmail = null, string note = null)
        {
            var inv = await GetAsync(facilityId, productId);
            if (inv == null) return (false, "Không tìm thấy kho.");
            inv.HoldQuantity = Math.Max(0, inv.HoldQuantity - quantity);
            inv.LastUpdated = DateTime.Now;
            Log(productId, facilityId, TxType.SaleCancel, quantity, inv.Quantity,
                refId: orderId, refType: "Order",
                userEmail: userEmail, note: note ?? $"Huỷ treo đơn #{orderId}");

            await _db.SaveChangesAsync();
            return (true, "Đã huỷ treo hàng.");
        }

        // ─────────────────────────────────────────────────────────────
        // 5. XUẤT CHO THUÊ
        // ─────────────────────────────────────────────────────────────
        public async Task<(bool ok, string msg, int rentalItemId)> RentalOutAsync(
            int facilityId, int productId, int quantity,
            string courtCode = null, string customerName = null,
            string customerPhone = null, string size = null,
            int? maxHours = null, int? orderId = null,
            string userEmail = null, string note = null)
        {
            if (quantity <= 0) return (false, "Số lượng phải > 0.", 0);
            var inv = await GetAsync(facilityId, productId);
            if (inv == null) return (false, "Sản phẩm chưa có trong kho.", 0);
            if (inv.AvailableQuantity < quantity) return (false, $"Không đủ khả dụng ({inv.AvailableQuantity}).", 0);

            inv.RentedQuantity += quantity;
            inv.LastUpdated = DateTime.Now;

            var rental = new RentalItem
            {
                InventoryId = inv.InventoryId,
                OrderId = orderId,
                CourtCode = courtCode,
                CustomerName = customerName,
                CustomerPhone = customerPhone,
                Quantity = quantity,
                Size = size,
                RentedAt = DateTime.Now,
                Status = "Active",
                CreatedBy = userEmail,
                ExpectedReturnAt = maxHours > 0 ? DateTime.Now.AddHours(maxHours.Value) : null,
                Note = note
            };
            _db.RentalItems.Add(rental);
            Log(productId, facilityId, TxType.RentalOut, quantity, inv.Quantity,
                refId: orderId, refType: "Rental", userEmail: userEmail,
                note: $"Cho thuê – {courtCode} – {customerName}");

            await _db.SaveChangesAsync();
            return (true, $"Xuất {quantity} cho thuê.", rental.RentalItemId);
        }

        // ─────────────────────────────────────────────────────────────
        // 6. KHÁCH TRẢ ĐỒ THUÊ
        // ─────────────────────────────────────────────────────────────
        public async Task<(bool ok, string msg)> RentalReturnAsync(
            int rentalItemId, int returnQty,
            bool isDamaged = false, decimal cleaningFee = 0,
            string userEmail = null, string note = null)
        {
            var rental = await _db.RentalItems
                .Include(r => r.Inventory)
                .FirstOrDefaultAsync(r => r.RentalItemId == rentalItemId);
            if (rental == null) return (false, "Không tìm thấy phiên thuê.");
            if (rental.Status != "Active") return (false, "Phiên thuê đã kết thúc.");

            var qty = Math.Min(returnQty, rental.Quantity);
            var inv = rental.Inventory;

            inv.RentedQuantity = Math.Max(0, inv.RentedQuantity - qty);
            if (isDamaged) inv.DamagedQuantity += qty;
            inv.LastUpdated = DateTime.Now;

            rental.ReturnedAt = DateTime.Now;
            rental.Status = isDamaged ? "Damaged" : "Returned";
            rental.CleaningFeeCharged = cleaningFee;
            if (!string.IsNullOrEmpty(note))
                rental.Note = string.IsNullOrEmpty(rental.Note) ? note : $"{rental.Note} | {note}";

            Log(inv.ProductId, inv.FacilityId, TxType.RentalReturn, qty, inv.Quantity,
                refId: rentalItemId, refType: "Rental", userEmail: userEmail,
                note: $"Trả thuê{(isDamaged ? " – hỏng" : "")}. VS: {cleaningFee:N0}₫. {note}");
            if (isDamaged)
                Log(inv.ProductId, inv.FacilityId, TxType.Damaged, qty, inv.Quantity,
                    userEmail: userEmail, note: "Hàng thuê trả về hỏng.");

            await _db.SaveChangesAsync();
            return (true, "Ghi nhận trả thuê thành công.");
        }

        // ─────────────────────────────────────────────────────────────
        // 7. MẤT ĐỒ THUÊ
        // ─────────────────────────────────────────────────────────────
        public async Task<(bool ok, string msg)> RentalLostAsync(
            int rentalItemId, int lostQty,
            string userEmail = null, string note = null)
        {
            var rental = await _db.RentalItems
                .Include(r => r.Inventory)
                .FirstOrDefaultAsync(r => r.RentalItemId == rentalItemId);
            if (rental == null || rental.Status != "Active")
                return (false, "Phiên thuê không hợp lệ.");

            var qty = Math.Min(lostQty, rental.Quantity);
            var inv = rental.Inventory;
            inv.Quantity = Math.Max(0, inv.Quantity - qty);
            inv.RentedQuantity = Math.Max(0, inv.RentedQuantity - qty);
            inv.LastUpdated = DateTime.Now;
            rental.Status = "Lost"; rental.ReturnedAt = DateTime.Now;

            Log(inv.ProductId, inv.FacilityId, TxType.RentalLost, qty, inv.Quantity,
                refId: rentalItemId, refType: "Rental",
                userEmail: userEmail, note: $"Mất hàng thuê. {note}");

            await _db.SaveChangesAsync();
            return (true, "Đã ghi nhận mất hàng.");
        }

        // ─────────────────────────────────────────────────────────────
        // 8. HÀNG HỎNG từ kho
        // ─────────────────────────────────────────────────────────────
        public async Task<(bool ok, string msg)> MarkDamagedAsync(
            int inventoryId, int quantity, string userEmail = null, string note = null)
        {
            var inv = await _db.Inventories.FindAsync(inventoryId);
            if (inv == null) return (false, "Không tìm thấy kho.");
            if (inv.AvailableQuantity < quantity) return (false, $"Không đủ khả dụng ({inv.AvailableQuantity}).");
            inv.DamagedQuantity += quantity;
            inv.LastUpdated = DateTime.Now;
            Log(inv.ProductId, inv.FacilityId, TxType.Damaged, quantity, inv.Quantity,
                userEmail: userEmail, note: note);
            await _db.SaveChangesAsync();
            return (true, "Đã đánh dấu hàng hỏng.");
        }

        // ─────────────────────────────────────────────────────────────
        // 9. XOÁ SỔ HÀNG HỎNG
        // ─────────────────────────────────────────────────────────────
        public async Task<(bool ok, string msg)> WriteOffDamagedAsync(
            int inventoryId, int quantity, string userEmail = null, string note = null)
        {
            var inv = await _db.Inventories.FindAsync(inventoryId);
            if (inv == null) return (false, "Không tìm thấy kho.");
            var writeQty = Math.Min(quantity, inv.DamagedQuantity);
            inv.DamagedQuantity -= writeQty;
            inv.Quantity -= writeQty;
            inv.LastUpdated = DateTime.Now;
            Log(inv.ProductId, inv.FacilityId, TxType.DamagedWrite, writeQty, inv.Quantity,
                userEmail: userEmail, note: $"Xoá sổ hàng hỏng. {note}");
            await _db.SaveChangesAsync();
            return (true, "Đã xoá sổ hàng hỏng.");
        }

        // ─────────────────────────────────────────────────────────────
        // 10. ĐIỀU CHỈNH KIỂM KÊ
        // ─────────────────────────────────────────────────────────────
        public async Task<(bool ok, string msg)> AdjustAsync(
            int inventoryId, int newQuantity, string userEmail = null, string note = null)
        {
            if (newQuantity < 0) return (false, "Số lượng không được âm.");
            var inv = await _db.Inventories.FindAsync(inventoryId);
            if (inv == null) return (false, "Không tìm thấy kho.");
            var committed = inv.HoldQuantity + inv.RentedQuantity + inv.DamagedQuantity;
            if (newQuantity < committed)
                return (false, $"Không thể điều chỉnh dưới {committed} (treo:{inv.HoldQuantity} + thuê:{inv.RentedQuantity} + hỏng:{inv.DamagedQuantity}).");
            var old = inv.Quantity;
            inv.Quantity = newQuantity;
            inv.LastUpdated = DateTime.Now;
            Log(inv.ProductId, inv.FacilityId, TxType.Adjustment,
                Math.Abs(newQuantity - old), newQuantity,
                userEmail: userEmail, note: $"Điều chỉnh: {old} → {newQuantity}. {note}");
            await _db.SaveChangesAsync();
            return (true, $"Điều chỉnh: {old} → {newQuantity}.");
        }

        // ─────────────────────────────────────────────────────────────
        // 11. XUẤT KHO THỦ CÔNG
        // ─────────────────────────────────────────────────────────────
        public async Task<(bool ok, string msg)> StockOutAsync(
            int facilityId, int productId, int quantity,
            string reason = null, string userEmail = null, string note = null)
        {
            if (quantity <= 0) return (false, "Số lượng phải > 0.");
            var inv = await GetAsync(facilityId, productId);
            if (inv == null) return (false, "Sản phẩm chưa có trong kho.");
            if (inv.AvailableQuantity < quantity) return (false, $"Không đủ khả dụng ({inv.AvailableQuantity}).");
            inv.Quantity -= quantity;
            inv.LastUpdated = DateTime.Now;
            await DeductBatchFIFOAsync(inv.InventoryId, quantity);
            Log(productId, facilityId, TxType.StockOut, quantity, inv.Quantity,
                userEmail: userEmail, note: note ?? reason ?? "Xuất kho thủ công");
            await _db.SaveChangesAsync();
            return (true, $"Xuất kho: -{quantity}.");
        }

        // ─────────────────────────────────────────────────────────────
        // 12. CHUYỂN KHO
        // ─────────────────────────────────────────────────────────────
        public async Task<(bool ok, string msg)> TransferAsync(
            int fromInventoryId, int targetFacilityId, int quantity,
            string userEmail = null, string note = null)
        {
            var src = await _db.Inventories.FindAsync(fromInventoryId);
            if (src == null) return (false, "Không tìm thấy kho nguồn.");
            if (src.AvailableQuantity < quantity) return (false, $"Không đủ khả dụng ({src.AvailableQuantity}).");
            src.Quantity -= quantity;
            src.LastUpdated = DateTime.Now;
            var dst = await GetOrCreateAsync(targetFacilityId, src.ProductId);
            dst.Quantity += quantity;
            dst.LastUpdated = DateTime.Now;
            Log(src.ProductId, src.FacilityId, TxType.Transfer, quantity, src.Quantity,
                targetFacilityId: targetFacilityId, userEmail: userEmail, note: note);
            await _db.SaveChangesAsync();
            return (true, "Chuyển kho thành công.");
        }

        // ─────────────────────────────────────────────────────────────
        // CẢNH BÁO đơn treo quá lâu
        // ─────────────────────────────────────────────────────────────
        public async Task<List<InventoryTransaction>> GetStalledHoldsAsync(int olderThanMinutes = 120)
        {
            var threshold = DateTime.Now.AddMinutes(-olderThanMinutes);
            return await _db.InventoryTransactions
                .Include(t => t.Product)
                .Include(t => t.Facility)
                .Where(t => t.Type == TxType.SaleHold && t.TransactionDate < threshold)
                .OrderBy(t => t.TransactionDate)
                .ToListAsync();
        }

        // ─────────────────────────────────────────────────────────────
        // PRIVATE HELPERS
        // ─────────────────────────────────────────────────────────────
        private async Task<Inventory> GetAsync(int facilityId, int productId)
            => await _db.Inventories
                .FirstOrDefaultAsync(i => i.FacilityId == facilityId && i.ProductId == productId);

        private async Task<Inventory> GetOrCreateAsync(int facilityId, int productId)
        {
            var inv = await GetAsync(facilityId, productId);
            if (inv != null) return inv;
            inv = new Inventory { FacilityId = facilityId, ProductId = productId, MinQuantity = 10, MaxQuantity = 1000 };
            _db.Inventories.Add(inv);
            await _db.SaveChangesAsync();
            return inv;
        }

        private async Task DeductBatchFIFOAsync(int inventoryId, int quantity)
        {
            var batches = await _db.InventoryBatches
                .Where(b => b.InventoryId == inventoryId && b.Status == "Active" && b.RemainingQuantity > 0)
                .OrderBy(b => b.ExpiryDate ?? DateTime.MaxValue)
                .ThenBy(b => b.ReceivedDate)
                .ToListAsync();
            int rem = quantity;
            foreach (var b in batches)
            {
                if (rem <= 0) break;
                var take = Math.Min(rem, b.RemainingQuantity);
                b.RemainingQuantity -= take;
                if (b.RemainingQuantity == 0) b.Status = "Depleted";
                rem -= take;
            }
        }

        private void Log(int productId, int facilityId, string type, int qty, int qtyAfter,
            decimal costPrice = 0, decimal salePrice = 0,
            int? refId = null, string refType = null,
            int? targetFacilityId = null, string batchNumber = null,
            DateTime? expiryDate = null, string userEmail = null, string note = null)
        {
            _db.InventoryTransactions.Add(new InventoryTransaction
            {
                ProductId = productId,
                FacilityId = facilityId,
                Type = type,
                Quantity = qty,
                QuantityAfter = qtyAfter,
                CostPrice = costPrice,
                SalePrice = salePrice,
                ReferenceId = refId,
                ReferenceType = refType,
                TargetFacilityId = targetFacilityId,
                BatchNumber = batchNumber,
                ExpiryDate = expiryDate,
                UserEmail = userEmail,
                Note = note,
                TransactionDate = DateTime.Now
            });
        }
    }
}
