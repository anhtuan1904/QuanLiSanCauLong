using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using QuanLiSanCauLong.Models;
using System.Collections.Generic;
using System.Linq;

namespace QuanLiSanCauLong.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {

        }

        // DbSets - 16 bảng (Giữ nguyên)
        public DbSet<User> Users { get; set; }
        public DbSet<Facility> Facilities { get; set; }
        public DbSet<Court> Courts { get; set; }
        public DbSet<PriceSlot> PriceSlots { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<ProductCategory> ProductCategories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Inventory> Inventories { get; set; }
        public DbSet<StockTransaction> StockTransactions { get; set; }
        public DbSet<StockTransactionDetail> StockTransactionDetails { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<Voucher> Vouchers { get; set; }
        public DbSet<VoucherUsage> VoucherUsages { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }
        public DbSet<SystemSetting> SystemSettings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<PriceSlot>().Property(p => p.Price).HasPrecision(10, 2);
            modelBuilder.Entity<Booking>().Property(b => b.CourtPrice).HasPrecision(10, 2);
            modelBuilder.Entity<Booking>().Property(b => b.ServiceFee).HasPrecision(10, 2);
            modelBuilder.Entity<Booking>().Property(b => b.DiscountAmount).HasPrecision(10, 2);
            modelBuilder.Entity<Booking>().Property(b => b.TotalPrice).HasPrecision(10, 2);
            modelBuilder.Entity<Product>().Property(p => p.Price).HasPrecision(10, 2);
            modelBuilder.Entity<Product>().Property(p => p.CostPrice).HasPrecision(10, 2);
            modelBuilder.Entity<Order>().Property(o => o.SubTotal).HasPrecision(10, 2);
            modelBuilder.Entity<Order>().Property(o => o.DiscountAmount).HasPrecision(10, 2);
            modelBuilder.Entity<Order>().Property(o => o.TotalAmount).HasPrecision(10, 2);
            modelBuilder.Entity<OrderDetail>().Property(od => od.UnitPrice).HasPrecision(10, 2);
            modelBuilder.Entity<OrderDetail>().Property(od => od.DiscountAmount).HasPrecision(10, 2);
            modelBuilder.Entity<OrderDetail>().Property(od => od.TotalPrice).HasPrecision(10, 2);
            modelBuilder.Entity<Voucher>().Property(v => v.DiscountValue).HasPrecision(10, 2);
            modelBuilder.Entity<VoucherUsage>().Property(vu => vu.DiscountAmount).HasPrecision(10, 2);
            modelBuilder.Entity<StockTransaction>().Property(st => st.TotalAmount).HasPrecision(10, 2);
            modelBuilder.Entity<StockTransactionDetail>().Property(std => std.UnitPrice).HasPrecision(10, 2);
            modelBuilder.Entity<StockTransactionDetail>().Property(std => std.TotalPrice).HasPrecision(10, 2);

            modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
            modelBuilder.Entity<Booking>().HasIndex(b => b.BookingCode).IsUnique();
            modelBuilder.Entity<Order>().HasIndex(o => o.OrderCode).IsUnique();
            modelBuilder.Entity<Product>().HasIndex(p => p.ProductCode).IsUnique();
            modelBuilder.Entity<Voucher>().HasIndex(v => v.VoucherCode).IsUnique();
            modelBuilder.Entity<SystemSetting>().HasIndex(s => s.SettingKey).IsUnique();
            modelBuilder.Entity<Inventory>().HasIndex(i => new { i.ProductId, i.FacilityId }).IsUnique();

            // Relationships - User
            modelBuilder.Entity<User>()
                .HasOne(u => u.Facility).WithMany().HasForeignKey(u => u.FacilityId)
                .OnDelete(DeleteBehavior.NoAction);

            // Relationships - Court
            modelBuilder.Entity<Court>()
                .HasOne(c => c.Facility).WithMany(f => f.Courts).HasForeignKey(c => c.FacilityId)
                .OnDelete(DeleteBehavior.NoAction);

            // Relationships - PriceSlot
            modelBuilder.Entity<PriceSlot>()
                .HasOne(p => p.Facility).WithMany(f => f.PriceSlots).HasForeignKey(p => p.FacilityId)
                .OnDelete(DeleteBehavior.NoAction);

            // Relationships - Booking
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.User).WithMany(u => u.Bookings).HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Court).WithMany(c => c.Bookings).HasForeignKey(b => b.CourtId)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.CheckInStaff).WithMany().HasForeignKey(b => b.CheckInBy)
                .OnDelete(DeleteBehavior.NoAction);

            // Relationships - Product
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category).WithMany(c => c.Products).HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.NoAction);

            // Relationships - Inventory
            modelBuilder.Entity<Inventory>()
                .HasOne(i => i.Product).WithMany(p => p.Inventories).HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<Inventory>()
                .HasOne(i => i.Facility).WithMany().HasForeignKey(i => i.FacilityId)
                .OnDelete(DeleteBehavior.NoAction);

            // Relationships - StockTransaction
            modelBuilder.Entity<StockTransaction>()
                .HasOne(st => st.Facility).WithMany().HasForeignKey(st => st.FacilityId)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<StockTransaction>()
                .HasOne(st => st.Creator).WithMany().HasForeignKey(st => st.CreatedBy)
                .OnDelete(DeleteBehavior.NoAction);

            // Relationships - StockTransactionDetail
            modelBuilder.Entity<StockTransactionDetail>()
                .HasOne(std => std.Transaction).WithMany(st => st.Details).HasForeignKey(std => std.TransactionId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<StockTransactionDetail>()
                .HasOne(std => std.Product).WithMany().HasForeignKey(std => std.ProductId)
                .OnDelete(DeleteBehavior.NoAction);

            // Relationships - Order
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Booking).WithMany(b => b.Orders).HasForeignKey(o => o.BookingId)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<Order>()
                .HasOne(o => o.User).WithMany(u => u.Orders).HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Facility).WithMany().HasForeignKey(o => o.FacilityId)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Creator).WithMany().HasForeignKey(o => o.CreatedBy)
                .OnDelete(DeleteBehavior.NoAction);

            // Relationships - OrderDetail
            modelBuilder.Entity<OrderDetail>()
                .HasOne(od => od.Order).WithMany(o => o.OrderDetails).HasForeignKey(od => od.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<OrderDetail>()
                .HasOne(od => od.Product).WithMany(p => p.OrderDetails).HasForeignKey(od => od.ProductId)
                .OnDelete(DeleteBehavior.NoAction);

            // Relationships - Voucher
            modelBuilder.Entity<Voucher>()
                .HasOne(v => v.Creator).WithMany().HasForeignKey(v => v.CreatedBy)
                .OnDelete(DeleteBehavior.NoAction);

            // Relationships - VoucherUsage
            modelBuilder.Entity<VoucherUsage>()
                .HasOne(vu => vu.Voucher).WithMany(v => v.VoucherUsages).HasForeignKey(vu => vu.VoucherId)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<VoucherUsage>()
                .HasOne(vu => vu.User).WithMany().HasForeignKey(vu => vu.UserId)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<VoucherUsage>()
                .HasOne(vu => vu.Booking).WithMany(b => b.VoucherUsages).HasForeignKey(vu => vu.BookingId)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<VoucherUsage>()
                .HasOne(vu => vu.Order).WithMany(o => o.VoucherUsages).HasForeignKey(vu => vu.OrderId)
                .OnDelete(DeleteBehavior.NoAction);

            // Relationships - ActivityLog
            modelBuilder.Entity<ActivityLog>()
                .HasOne(al => al.User).WithMany().HasForeignKey(al => al.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            // Relationships - SystemSetting
            modelBuilder.Entity<SystemSetting>()
                .HasOne(ss => ss.Updater).WithMany().HasForeignKey(ss => ss.UpdatedBy)
                .OnDelete(DeleteBehavior.NoAction);
        }

        // Lưu ý: Phương thức Create() kiểu cũ này thường không dùng trong EF Core 
        // vì Context được khởi tạo qua Dependency Injection (DI) trong Program.cs.
        public static ApplicationDbContext Create(DbContextOptions<ApplicationDbContext> options)
        {
            return new ApplicationDbContext(options);
        }
    }
}