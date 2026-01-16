using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiSanCauLong.Data;
using QuanLiSanCauLong.Models;
using QuanLiSanCauLong.Services.Interfaces;

namespace QuanLiSanCauLong.Services
{
    public class VoucherService : IVoucherService
    {
        private readonly ApplicationDbContext _context;

        public VoucherService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<(bool IsValid, int? VoucherId, decimal DiscountAmount, string Message)>
            ValidateVoucherAsync(string code, int userId, decimal orderAmount, string applicableFor)
        {
            // 1. Tìm voucher
            var voucher = await _context.Vouchers
                .FirstOrDefaultAsync(v => v.VoucherCode == code && v.IsActive);

            if (voucher == null)
            {
                return (false, null, 0, "Mã voucher không tồn tại!");
            }

            // 2. Kiểm tra thời gian
            var now = DateTime.Now;
            if (now < voucher.StartDate)
            {
                return (false, null, 0, "Voucher chưa có hiệu lực!");
            }

            if (now > voucher.EndDate)
            {
                return (false, null, 0, "Voucher đã hết hạn!");
            }

            // 3. Kiểm tra loại áp dụng
            if (voucher.ApplicableFor != "All" && voucher.ApplicableFor != applicableFor)
            {
                var applicableText = voucher.ApplicableFor == "Booking" ? "đặt sân" : "sản phẩm";
                return (false, null, 0, $"Voucher chỉ áp dụng cho {applicableText}!");
            }

            // 4. Kiểm tra giá trị đơn tối thiểu
            if (voucher.MinOrderAmount.HasValue && orderAmount < voucher.MinOrderAmount.Value)
            {
                return (false, null, 0,
                    $"Đơn hàng tối thiểu {voucher.MinOrderAmount.Value:N0}đ để sử dụng voucher!");
            }

            // 5. Kiểm tra số lượt sử dụng tổng
            if (voucher.UsageLimit.HasValue && voucher.UsedCount >= voucher.UsageLimit.Value)
            {
                return (false, null, 0, "Voucher đã hết lượt sử dụng!");
            }

            // 6. Kiểm tra số lượt sử dụng của user
            var userUsageCount = await GetUserVoucherUsageCountAsync(voucher.VoucherId, userId);
            if (userUsageCount >= voucher.UsageLimitPerUser)
            {
                return (false, null, 0, $"Bạn đã sử dụng tối đa voucher này ({voucher.UsageLimitPerUser} lần)!");
            }

            // 7. Tính giảm giá
            decimal discountAmount = 0;

            if (voucher.DiscountType == "Percentage")
            {
                discountAmount = orderAmount * voucher.DiscountValue / 100;

                // Kiểm tra giảm tối đa
                if (voucher.MaxDiscount.HasValue && discountAmount > voucher.MaxDiscount.Value)
                {
                    discountAmount = voucher.MaxDiscount.Value;
                }
            }
            else if (voucher.DiscountType == "FixedAmount")
            {
                discountAmount = voucher.DiscountValue;

                // Không cho giảm quá tổng đơn
                if (discountAmount > orderAmount)
                {
                    discountAmount = orderAmount;
                }
            }

            return (true, voucher.VoucherId, discountAmount, "Voucher hợp lệ!");
        }

        public async Task<bool> ApplyVoucherAsync(int voucherId, int userId, int? bookingId, int? orderId, decimal discountAmount)
        {
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // 1. Tạo bản ghi sử dụng voucher
                    var usage = new VoucherUsage
                    {
                        VoucherId = voucherId,
                        UserId = userId,
                        BookingId = bookingId,
                        OrderId = orderId,
                        DiscountAmount = discountAmount,
                        UsedAt = DateTime.Now
                    };

                    _context.VoucherUsages.Add(usage);

                    // 2. Cập nhật số lượt đã dùng
                    var voucher = await _context.Vouchers.FindAsync(voucherId);
                    if (voucher != null)
                    {
                        voucher.UsedCount++;
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return true;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    return false;
                }
            }
        }

        public async Task<List<Voucher>> GetActiveVouchersAsync(string applicableFor = null)
        {
            var now = DateTime.Now;
            var query = _context.Vouchers
                .Where(v => v.IsActive
                       && v.StartDate <= now
                       && v.EndDate >= now);

            if (!string.IsNullOrEmpty(applicableFor))
            {
                query = query.Where(v => v.ApplicableFor == applicableFor || v.ApplicableFor == "All");
            }

            // Lọc những voucher còn lượt sử dụng
            var vouchers = await query.ToListAsync();

            return vouchers
                .Where(v => !v.UsageLimit.HasValue || v.UsedCount < v.UsageLimit.Value)
                .OrderByDescending(v => v.DiscountValue)
                .ToList();
        }

        public async Task<bool> CanUseVoucherAsync(int voucherId, int userId)
        {
            var voucher = await _context.Vouchers.FindAsync(voucherId);
            if (voucher == null || !voucher.IsActive)
                return false;

            var now = DateTime.Now;
            if (now < voucher.StartDate || now > voucher.EndDate)
                return false;

            if (voucher.UsageLimit.HasValue && voucher.UsedCount >= voucher.UsageLimit.Value)
                return false;

            var userUsageCount = await GetUserVoucherUsageCountAsync(voucherId, userId);
            if (userUsageCount >= voucher.UsageLimitPerUser)
                return false;

            return true;
        }

        public async Task<int> GetUserVoucherUsageCountAsync(int voucherId, int userId)
        {
            return await _context.VoucherUsages
                .CountAsync(v => v.VoucherId == voucherId && v.UserId == userId);
        }
    }
}
