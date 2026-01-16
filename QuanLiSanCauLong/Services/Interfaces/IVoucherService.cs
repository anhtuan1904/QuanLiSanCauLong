using Microsoft.AspNetCore.Mvc;
using QuanLiSanCauLong.Models;

namespace QuanLiSanCauLong.Services.Interfaces
{
    public interface IVoucherService
    {
        Task<(bool IsValid, int? VoucherId, decimal DiscountAmount, string Message)>
            ValidateVoucherAsync(string code, int userId, decimal orderAmount, string applicableFor);

        Task<bool> ApplyVoucherAsync(int voucherId, int userId, int? bookingId, int? orderId, decimal discountAmount);

        Task<List<Voucher>> GetActiveVouchersAsync(string applicableFor = null);

        Task<bool> CanUseVoucherAsync(int voucherId, int userId);

        Task<int> GetUserVoucherUsageCountAsync(int voucherId, int userId);
    }
}
