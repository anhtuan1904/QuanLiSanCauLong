using Microsoft.EntityFrameworkCore;
using QuanLiSanCauLong.Data;
using QuanLiSanCauLong.Models;
using QuanLiSanCauLong.Services.Interfaces;

namespace QuanLiSanCauLong.Services
{
    public class ActivityLogService : IActivityLogService
    {
        private readonly ApplicationDbContext _db;

        // Chỉ để duy nhất một Constructor này
        public ActivityLogService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task LogAsync(int userId, string action, string tableName, int? recordId,
            string oldValue, string newValue, string ipAddress, string userAgent)
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

            _db.ActivityLogs.Add(log);
            await _db.SaveChangesAsync();
        }

        public async Task<List<ActivityLog>> GetLogsAsync(int? userId = null, string tableName = null,
            DateTime? fromDate = null, DateTime? toDate = null, int pageSize = 100)
        {
            var query = _db.ActivityLogs.AsQueryable();
            if (userId.HasValue) query = query.Where(l => l.UserId == userId);
            if (!string.IsNullOrEmpty(tableName)) query = query.Where(l => l.TableName == tableName);
            if (fromDate.HasValue) query = query.Where(l => l.CreatedAt >= fromDate);
            if (toDate.HasValue) query = query.Where(l => l.CreatedAt <= toDate);
            return await query.OrderByDescending(l => l.CreatedAt).Take(pageSize).ToListAsync();
        }

        public async Task<List<ActivityLog>> GetUserActivityAsync(int userId, int days = 30)
        {
            var dateThreshold = DateTime.Now.AddDays(-days);
            return await _db.ActivityLogs
                .Where(l => l.UserId == userId && l.CreatedAt >= dateThreshold)
                .OrderByDescending(l => l.CreatedAt).ToListAsync();
        }

        public async Task<List<ActivityLog>> GetTableActivityAsync(string tableName, int? recordId = null)
        {
            var query = _db.ActivityLogs.Where(l => l.TableName == tableName);
            if (recordId.HasValue) query = query.Where(l => l.RecordId == recordId);
            return await query.OrderByDescending(l => l.CreatedAt).ToListAsync();
        }
    }
}