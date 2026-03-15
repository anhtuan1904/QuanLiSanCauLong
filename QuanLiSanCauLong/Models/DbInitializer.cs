using Microsoft.EntityFrameworkCore;
using QuanLiSanCauLong.Helpers;
using QuanLiSanCauLong.Models;

namespace QuanLiSanCauLong.Data
{
    /// <summary>
    /// Seed dữ liệu khởi tạo khi database còn trống.
    /// Gọi từ Program.cs sau MigrateAsync().
    /// </summary>
    public static class DbInitializer
    {
        public static async Task InitializeAsync(ApplicationDbContext context)
        {
            await SeedSystemSettingsAsync(context);
            await SeedAdminUserAsync(context);
            await SeedProductCategoriesAsync(context);
            await SeedShiftsAsync(context);
        }

        private static async Task SeedSystemSettingsAsync(ApplicationDbContext context)
        {
            if (await context.SystemSettings.AnyAsync()) return;
            context.SystemSettings.AddRange(
                new SystemSetting { SettingKey = "SiteName", SettingValue = "Hệ thống quản lý cầu lông", Category = "General", IsActive = true, UpdatedAt = DateTime.Now },
                new SystemSetting { SettingKey = "SupportEmail", SettingValue = "admin@badminton.com", Category = "General", IsActive = true, UpdatedAt = DateTime.Now },
                new SystemSetting { SettingKey = "SupportPhone", SettingValue = "0123 456 789", Category = "General", IsActive = true, UpdatedAt = DateTime.Now }
            );
            await context.SaveChangesAsync();
        }

        private static async Task SeedAdminUserAsync(ApplicationDbContext context)
        {
            if (await context.Users.AnyAsync(u => u.Email == "admin@badminton.com")) return;
            context.Users.Add(new User
            {
                FullName = "Administrator",
                Email = "admin@badminton.com",
                Phone = "0123456789",
                Password = string.Empty,
                PasswordHash = PasswordHelper.HashPassword("Admin@123"),
                Role = "Admin",
                Status = "Active",
                IsActive = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            });
            await context.SaveChangesAsync();
        }

        // ✅ FIX CS0103: Thêm method còn thiếu — seed danh mục sản phẩm mặc định
        private static async Task SeedProductCategoriesAsync(ApplicationDbContext context)
        {
            if (await context.ProductCategories.AnyAsync()) return;

            context.ProductCategories.AddRange(
                new ProductCategory { CategoryName = "Vợt cầu lông", Description = "Các loại vợt cầu lông", IsActive = true },
                new ProductCategory { CategoryName = "Cầu lông", Description = "Cầu lông các loại", IsActive = true },
                new ProductCategory { CategoryName = "Giày cầu lông", Description = "Giày chuyên dụng cầu lông", IsActive = true },
                new ProductCategory { CategoryName = "Túi & Balo", Description = "Túi đựng vợt và phụ kiện", IsActive = true },
                new ProductCategory { CategoryName = "Quần áo", Description = "Trang phục thi đấu cầu lông", IsActive = true },
                new ProductCategory { CategoryName = "Phụ kiện", Description = "Cuốn cán, kẹp tóc, băng tay…", IsActive = true }
            );
            await context.SaveChangesAsync();
        }

        private static async Task SeedShiftsAsync(ApplicationDbContext context)
        {
            if (await context.Shifts.AnyAsync()) return;

            context.Shifts.AddRange(
                new Shift { ShiftName = "Ca Sáng", StartTime = new TimeSpan(6, 0, 0), EndTime = new TimeSpan(12, 0, 0), Color = "#d4a017", Description = "6:00 – 12:00", IsActive = true, CreatedAt = DateTime.Now },
                new Shift { ShiftName = "Ca Chiều", StartTime = new TimeSpan(12, 0, 0), EndTime = new TimeSpan(18, 0, 0), Color = "#4f46e5", Description = "12:00 – 18:00", IsActive = true, CreatedAt = DateTime.Now },
                new Shift { ShiftName = "Ca Tối", StartTime = new TimeSpan(18, 0, 0), EndTime = new TimeSpan(22, 0, 0), Color = "#0891b2", Description = "18:00 – 22:00", IsActive = true, CreatedAt = DateTime.Now },
                new Shift { ShiftName = "Ca Toàn Ngày", StartTime = new TimeSpan(6, 0, 0), EndTime = new TimeSpan(22, 0, 0), Color = "#16a34a", Description = "6:00 – 22:00", IsActive = true, CreatedAt = DateTime.Now }
            );
            await context.SaveChangesAsync();
        }
    }
}
