using Microsoft.AspNetCore.Mvc;
using QuanLiSanCauLong.ViewModels;

namespace QuanLiSanCauLong.Services.Interfaces
{
    public interface IReportService
    {
        Task<AdminDashboardViewModel> GetDashboardDataAsync(DateTime fromDate, DateTime toDate);

        Task<List<RevenueByDateViewModel>> GetRevenueByDateAsync(DateTime fromDate, DateTime toDate);

        Task<List<RevenueByFacilityViewModel>> GetRevenueByFacilityAsync(DateTime fromDate, DateTime toDate);

        Task<List<TopProductViewModel>> GetTopProductsAsync(DateTime fromDate, DateTime toDate, int top = 10);

        Task<List<TopCustomerViewModel>> GetTopCustomersAsync(DateTime fromDate, DateTime toDate, int top = 10);

        Task<List<PopularTimeSlotViewModel>> GetPopularTimeSlotsAsync(DateTime fromDate, DateTime toDate);

        Task<byte[]> ExportRevenueReportAsync(DateTime fromDate, DateTime toDate, string format);

        Task<byte[]> ExportBookingReportAsync(DateTime fromDate, DateTime toDate, string format);
    }
}
