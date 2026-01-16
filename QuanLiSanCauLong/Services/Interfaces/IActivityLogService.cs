using Microsoft.AspNetCore.Mvc;
using QuanLiSanCauLong.Models;

namespace QuanLiSanCauLong.Services.Interfaces
{
    public interface IActivityLogService
    {
        Task LogAsync(int userId, string action, string tableName, int? recordId,
            string oldValue, string newValue, string ipAddress, string userAgent);

        Task<List<ActivityLog>> GetLogsAsync(int? userId = null, string tableName = null,
            DateTime? fromDate = null, DateTime? toDate = null, int pageSize = 100);

        Task<List<ActivityLog>> GetUserActivityAsync(int userId, int days = 30);

        Task<List<ActivityLog>> GetTableActivityAsync(string tableName, int? recordId = null);
    }
}
