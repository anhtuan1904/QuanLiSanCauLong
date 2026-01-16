using Microsoft.AspNetCore.Mvc;

namespace QuanLiSanCauLong.Services.Interfaces
{
    public interface IExcelHelper
    {
        Task<byte[]> ExportToExcelAsync<T>(List<T> data, string sheetName);
        Task<List<T>> ImportFromExcelAsync<T>(IFormFile file);
    }
}
