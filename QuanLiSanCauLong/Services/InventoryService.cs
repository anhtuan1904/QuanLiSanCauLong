using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiSanCauLong.Data;
using QuanLiSanCauLong.Models;
using QuanLiSanCauLong.Services.Interfaces;
using QuanLiSanCauLong.ViewModels;

namespace QuanLiSanCauLong.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;

        public InventoryService(
            ApplicationDbContext context,
            INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<bool> UpdateStockAsync(int productId, int facilityId, int quantity, string transactionType)
        {
            var inventory = await _context.Inventories
                .FirstOrDefaultAsync(i => i.ProductId == productId && i.FacilityId == facilityId);

            if (inventory == null)
            {
                // Tạo mới nếu chưa có
                inventory = new Inventory
                {
                    ProductId = productId,
                    FacilityId = facilityId,
                    Quantity = 0,
                    MinQuantity = 10,
                    MaxQuantity = 1000,
                    LastUpdated = DateTime.Now
                };
                _context.Inventories.Add(inventory);
            }

            // Cập nhật số lượng
            switch (transactionType.ToUpper())
            {
                case "IMPORT":
                case "ADJUSTMENT_IN":
                    inventory.Quantity += quantity;
                    break;

                case "EXPORT":
                case "SALE":
                case "ADJUSTMENT_OUT":
                    inventory.Quantity -= quantity;
                    if (inventory.Quantity < 0)
                    {
                        return false; // Không đủ hàng
                    }
                    break;

                case "SET":
                    inventory.Quantity = quantity;
                    break;

                default:
                    return false;
            }

            inventory.LastUpdated = DateTime.Now;
            await _context.SaveChangesAsync();

            // Kiểm tra cảnh báo hết hàng
            if (inventory.Quantity <= inventory.MinQuantity)
            {
                await _notificationService.NotifyLowStockAsync(inventory);
            }

            return true;
        }

        public async Task<StockTransaction> CreateStockTransactionAsync(
            int facilityId, string transactionType, int createdBy,
            List<StockTransactionItemViewModel> items, string note = null)
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // 1. Tạo phiếu
                    var stockTransaction = new StockTransaction
                    {
                        FacilityId = facilityId,
                        TransactionType = transactionType,
                        TransactionDate = DateTime.Now,
                        CreatedBy = createdBy,
                        Note = note,
                        TotalAmount = items.Sum(i => i.TotalPrice)
                    };

                    _context.StockTransactions.Add(stockTransaction);
                    await _context.SaveChangesAsync();

                    // 2. Tạo chi tiết
                    foreach (var item in items)
                    {
                        var detail = new StockTransactionDetail
                        {
                            TransactionId = stockTransaction.TransactionId,
                            ProductId = item.ProductId,
                            Quantity = item.Quantity,
                            UnitPrice = item.UnitPrice,
                            TotalPrice = item.TotalPrice
                        };

                        _context.StockTransactionDetails.Add(detail);

                        // 3. Cập nhật tồn kho
                        await UpdateStockAsync(item.ProductId, facilityId, item.Quantity, transactionType);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return stockTransaction;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }

        public async Task<List<InventoryItemViewModel>> GetLowStockItemsAsync(int? facilityId = null)
        {
            var query = _context.Inventories
                .Include(i => i.Product)
                    .ThenInclude(p => p.Category)
                .Include(i => i.Facility)
                .AsQueryable();

            if (facilityId.HasValue)
            {
                query = query.Where(i => i.FacilityId == facilityId.Value);
            }

            var lowStockItems = await query
                .Where(i => i.Quantity <= i.MinQuantity)
                .OrderBy(i => i.Quantity)
                .ToListAsync();

            return lowStockItems.Select(i => new InventoryItemViewModel
            {
                InventoryId = i.InventoryId,
                ProductId = i.ProductId,
                ProductName = i.Product.ProductName,
                ProductCode = i.Product.ProductCode,
                CategoryName = i.Product.Category.CategoryName,
                CategoryType = i.Product.Category.CategoryType,
                Quantity = i.Quantity,
                MinQuantity = i.MinQuantity,
                MaxQuantity = i.MaxQuantity,
                Unit = i.Product.Unit,
                Price = i.Product.Price,
                LastUpdated = i.LastUpdated
            }).ToList();
        }

        public async Task<int> GetStockQuantityAsync(int productId, int facilityId)
        {
            var inventory = await _context.Inventories
                .FirstOrDefaultAsync(i => i.ProductId == productId && i.FacilityId == facilityId);

            return inventory?.Quantity ?? 0;
        }

        public async Task<bool> CheckStockAsync(int productId, int facilityId, int requiredQuantity)
        {
            var availableQuantity = await GetStockQuantityAsync(productId, facilityId);
            return availableQuantity >= requiredQuantity;
        }

        public async Task<List<StockTransaction>> GetTransactionHistoryAsync(
            int? facilityId, DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.StockTransactions
                .Include(st => st.Facility)
                .Include(st => st.Creator)
                .Include(st => st.Details)
                    .ThenInclude(d => d.Product)
                .AsQueryable();

            if (facilityId.HasValue)
            {
                query = query.Where(st => st.FacilityId == facilityId.Value);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(st => st.TransactionDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(st => st.TransactionDate <= toDate.Value.AddDays(1));
            }

            return await query
                .OrderByDescending(st => st.TransactionDate)
                .Take(100)
                .ToListAsync();
        }
    }

    // ===================================
    // ACTIVITY LOG SERVICE IMPLEMENTATION
    // ===================================

    public class ActivityLogService : IActivityLogService
    {
        private readonly ApplicationDbContext _context;

        public ActivityLogService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task LogAsync(int userId, string action, string tableName, int? recordId,
            string oldValue, string newValue, string ipAddress, string userAgent)
        {
            try
            {
                var log = new ActivityLog
                {
                    UserId = userId,
                    Action = action,
                    TableName = tableName,
                    RecordId = recordId,
                    OldValue = oldValue,
                    NewValue = newValue,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    CreatedAt = DateTime.Now
                };

                _context.ActivityLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch
            {
                // Log error nhưng không throw để không ảnh hưởng nghiệp vụ chính
            }
        }

        public async Task<List<ActivityLog>> GetLogsAsync(int? userId = null, string tableName = null,
            DateTime? fromDate = null, DateTime? toDate = null, int pageSize = 100)
        {
            var query = _context.ActivityLogs
                .Include(al => al.User)
                .AsQueryable();

            if (userId.HasValue)
            {
                query = query.Where(al => al.UserId == userId.Value);
            }

            if (!string.IsNullOrEmpty(tableName))
            {
                query = query.Where(al => al.TableName == tableName);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(al => al.CreatedAt >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(al => al.CreatedAt <= toDate.Value.AddDays(1));
            }

            return await query
                .OrderByDescending(al => al.CreatedAt)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<ActivityLog>> GetUserActivityAsync(int userId, int days = 30)
        {
            var fromDate = DateTime.Now.AddDays(-days);

            return await _context.ActivityLogs
                .Include(al => al.User)
                .Where(al => al.UserId == userId && al.CreatedAt >= fromDate)
                .OrderByDescending(al => al.CreatedAt)
                .Take(100)
                .ToListAsync();
        }

        public async Task<List<ActivityLog>> GetTableActivityAsync(string tableName, int? recordId = null)
        {
            var query = _context.ActivityLogs
                .Include(al => al.User)
                .Where(al => al.TableName == tableName);

            if (recordId.HasValue)
            {
                query = query.Where(al => al.RecordId == recordId.Value);
            }

            return await query
                .OrderByDescending(al => al.CreatedAt)
                .Take(100)
                .ToListAsync();
        }
    }
}
