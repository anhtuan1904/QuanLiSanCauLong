using Microsoft.AspNetCore.Mvc;

namespace QuanLiSanCauLong.Services.Interfaces
{
    public interface IFileHelper
    {
        Task<string> UploadFileAsync(IFormFile file, string folder);
        Task<bool> DeleteFileAsync(string filePath);
        Task<byte[]> DownloadFileAsync(string filePath);
        string GetFileUrl(string filePath);
    }
}
