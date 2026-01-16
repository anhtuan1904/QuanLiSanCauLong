using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using QRCoder;
using QuanLiSanCauLong.Services.Interfaces;
using System.ComponentModel;

namespace QuanLiSanCauLong.Services
{
    public class FileHelper : IFileHelper
    {
        private readonly IConfiguration _configuration;

        public FileHelper(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<string> UploadFileAsync(IFormFile file, string folder)
        {
            if (file == null || file.Length == 0)
                return null;

            var uploadPath = Path.Combine(_configuration["FileStorage:LocalPath"], folder);
            Directory.CreateDirectory(uploadPath);

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(uploadPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/{folder}/{fileName}";
        }

        public Task<bool> DeleteFileAsync(string filePath)
        {
            try
            {
                var fullPath = Path.Combine(_configuration["FileStorage:LocalPath"], filePath.TrimStart('/'));
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    return Task.FromResult(true);
                }
                return Task.FromResult(false);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        public async Task<byte[]> DownloadFileAsync(string filePath)
        {
            var fullPath = Path.Combine(_configuration["FileStorage:LocalPath"], filePath.TrimStart('/'));
            if (File.Exists(fullPath))
            {
                return await File.ReadAllBytesAsync(fullPath);
            }
            return null;
        }

        public string GetFileUrl(string filePath)
        {
            return _configuration["AppSettings:SiteUrl"] + filePath;
        }
    }

    // ===================================
    // EXCEL HELPER IMPLEMENTATION
    // ===================================

    public class ExcelHelper : IExcelHelper
    {
        public async Task<byte[]> ExportToExcelAsync<T>(List<T> data, string sheetName)
        {
            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add(sheetName);
                worksheet.Cells["A1"].LoadFromCollection(data, true);
                worksheet.Cells.AutoFitColumns();

                return await package.GetAsByteArrayAsync();
            }
        }

        public Task<List<T>> ImportFromExcelAsync<T>(IFormFile file)
        {
            throw new NotImplementedException();
        }
    }

    // ===================================
    // PDF HELPER IMPLEMENTATION
    // ===================================

    public class PdfHelper : IPdfHelper
    {
        public Task<byte[]> GenerateInvoicePdfAsync(QuanLiSanCauLong.Models.Booking booking, QuanLiSanCauLong.Models.Order order = null)
        {
            // TODO: Implement PDF generation using iTextSharp or similar
            throw new NotImplementedException("PDF generation not implemented yet");
        }

        public Task<byte[]> GenerateReportPdfAsync(string title, object data)
        {
            throw new NotImplementedException("PDF generation not implemented yet");
        }
    }

    // ===================================
    // QR CODE HELPER IMPLEMENTATION
    // ===================================

    public class QRCodeHelper : IQRCodeHelper
    {
        public byte[] GenerateQRCode(string content, int size = 300)
        {
            using (var qrGenerator = new QRCodeGenerator())
            {
                var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
                using (var qrCode = new PngByteQRCode(qrCodeData))
                {
                    return qrCode.GetGraphic(size / 100);
                }
            }
        }

        public string GenerateQRCodeBase64(string content, int size = 300)
        {
            var qrBytes = GenerateQRCode(content, size);
            return Convert.ToBase64String(qrBytes);
        }
    }
}
