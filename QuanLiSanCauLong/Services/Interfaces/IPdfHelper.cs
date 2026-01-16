using Microsoft.AspNetCore.Mvc;
using QuanLiSanCauLong.Models;

namespace QuanLiSanCauLong.Services.Interfaces
{
    public interface IPdfHelper
    {
        Task<byte[]> GenerateInvoicePdfAsync(Booking booking, Order order = null);
        Task<byte[]> GenerateReportPdfAsync(string title, object data);
    }
}
