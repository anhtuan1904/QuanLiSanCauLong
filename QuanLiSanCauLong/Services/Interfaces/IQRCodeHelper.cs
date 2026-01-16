using Microsoft.AspNetCore.Mvc;

namespace QuanLiSanCauLong.Services.Interfaces
{
    public interface IQRCodeHelper
    {
        byte[] GenerateQRCode(string content, int size = 300);
        string GenerateQRCodeBase64(string content, int size = 300);
    }
}
