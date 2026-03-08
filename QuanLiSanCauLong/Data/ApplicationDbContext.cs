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

        // --- DbSets Cũ (Giữ nguyên) ---
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
        public DbSet<Post> Posts { get; set; }
        public DbSet<CourtImage> CourtImages { get; set; }
        public DbSet<FacilityImage> FacilityImages { get; set; }

        // --- DbSets Mới Bổ sung ---
        public DbSet<BlogPost> BlogPosts { get; set; }
        public DbSet<BlogCategory> BlogCategories { get; set; }
        public DbSet<BlogComment> BlogComments { get; set; }
        public DbSet<JobPosting> JobPostings { get; set; }
        public DbSet<JobApplication> JobApplications { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<ServiceImage> ServiceImages { get; set; }
        public DbSet<ServiceInquiry> ServiceInquiries { get; set; }
        public DbSet<ContactMessage> ContactMessages { get; set; }

        public DbSet<Course> Courses { get; set; }
        public DbSet<StringingService> StringingServices { get; set; }
        public DbSet<Tournament> Tournaments { get; set; }
        public DbSet<BlogReview> BlogReviews { get; set; }
        public DbSet<BlogReviewLike> BlogReviewLikes { get; set; }

        // ── MỚI: Phân loại sản phẩm theo Size / Màu ──
        public DbSet<ProductVariant> ProductVariants { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ════════════════════════════════════════════
            // CẤU HÌNH CÁC BẢNG MỚI BỔ SUNG
            // ════════════════════════════════════════════

            // Course
            modelBuilder.Entity<Course>(entity =>
            {
                entity.HasKey(e => e.CourseId);
                entity.Property(e => e.CourseName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.TuitionFee).HasPrecision(18, 2);
                entity.Property(e => e.DiscountFee).HasPrecision(18, 2);
                entity.Property(e => e.Status).HasDefaultValue("Active");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
            });

            // StringingService
            modelBuilder.Entity<StringingService>(entity =>
            {
                entity.HasKey(e => e.StringingId);
                entity.Property(e => e.ServiceName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Price).HasPrecision(18, 2);
                entity.Property(e => e.Status).HasDefaultValue("Active");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
            });

            // Tournament
            modelBuilder.Entity<Tournament>(entity =>
            {
                entity.HasKey(e => e.TournamentId);
                entity.Property(e => e.TournamentName).IsRequired().HasMaxLength(200);
                entity.Property(e => e.EntryFee).HasPrecision(18, 2);
                entity.Property(e => e.PrizeMoney).HasPrecision(18, 2);
                entity.Property(e => e.Status).HasDefaultValue("Upcoming");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
            });

            // BlogReview
            modelBuilder.Entity<BlogReview>(entity =>
            {
                entity.HasIndex(r => r.BlogId);
                entity.HasIndex(r => r.Status);
                entity.HasIndex(r => r.CreatedAt);
                entity.HasIndex(r => new { r.BlogId, r.Status });
                entity.Property(r => r.Content).HasMaxLength(2000);
                entity.Property(r => r.Status).HasDefaultValue("Pending");
                entity.Property(r => r.Rating).HasDefaultValue(5);
            });

            // BlogReviewLike
            modelBuilder.Entity<BlogReviewLike>(entity =>
            {
                entity.HasIndex(l => new { l.ReviewId, l.UserId })
                      .IsUnique()
                      .HasFilter("[UserId] IS NOT NULL");

                entity.HasOne(l => l.Review)
                      .WithMany(r => r.Likes)
                      .HasForeignKey(l => l.ReviewId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ════════════════════════════════════════════
            // ── MỚI: ProductVariant ──
            // ════════════════════════════════════════════
            modelBuilder.Entity<ProductVariant>(entity =>
            {
                entity.HasKey(v => v.VariantId);

                entity.Property(v => v.SizeName).HasMaxLength(50);
                entity.Property(v => v.ColorName).HasMaxLength(50);
                entity.Property(v => v.VariantSKU).HasMaxLength(100);
                entity.Property(v => v.StockQuantity).HasDefaultValue(0);
                entity.Property(v => v.ReservedQuantity).HasDefaultValue(0);
                entity.Property(v => v.MinStockLevel).HasDefaultValue(0);
                entity.Property(v => v.IsActive).HasDefaultValue(true);

                entity.HasOne(v => v.Product)
                      .WithMany(p => p.Variants)
                      .HasForeignKey(v => v.ProductId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(v => v.ProductId);
                // AvailableQuantity, IsLowStock, DisplayName là [NotMapped] — không cần cấu hình
            });

            // ════════════════════════════════════════════
            // CẤU HÌNH PRECISION CÁC TRƯỜNG DECIMAL
            // ════════════════════════════════════════════
            modelBuilder.Entity<Product>().Property(p => p.SalePrice).HasPrecision(10, 2);
            modelBuilder.Entity<Product>().Property(p => p.COGSPrice).HasPrecision(10, 2);

            modelBuilder.Entity<JobPosting>().Property(j => j.SalaryMin).HasPrecision(18, 2);
            modelBuilder.Entity<JobPosting>().Property(j => j.SalaryMax).HasPrecision(18, 2);
            modelBuilder.Entity<JobApplication>().Property(a => a.ExpectedSalary).HasPrecision(18, 2);
            modelBuilder.Entity<Service>().Property(s => s.Price).HasPrecision(18, 2);
            modelBuilder.Entity<Service>().Property(s => s.DiscountPrice).HasPrecision(18, 2);

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

            // ════════════════════════════════════════════
            // UNIQUE INDEXES
            // ════════════════════════════════════════════
            modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
            modelBuilder.Entity<Booking>().HasIndex(b => b.BookingCode).IsUnique();
            modelBuilder.Entity<Order>().HasIndex(o => o.OrderCode).IsUnique();
            modelBuilder.Entity<Product>().HasIndex(p => p.ProductCode).IsUnique();
            modelBuilder.Entity<Voucher>().HasIndex(v => v.VoucherCode).IsUnique();
            modelBuilder.Entity<SystemSetting>().HasIndex(s => s.SettingKey).IsUnique();
            modelBuilder.Entity<Inventory>().HasIndex(i => new { i.ProductId, i.FacilityId }).IsUnique();

            // ════════════════════════════════════════════
            // RELATIONSHIPS
            // ════════════════════════════════════════════

            // User
            modelBuilder.Entity<User>()
                .HasOne(u => u.Facility).WithMany().HasForeignKey(u => u.FacilityId)
                .OnDelete(DeleteBehavior.NoAction);

            // Court
            modelBuilder.Entity<Court>()
                .HasOne(c => c.Facility).WithMany(f => f.Courts).HasForeignKey(c => c.FacilityId)
                .OnDelete(DeleteBehavior.NoAction);

            // PriceSlot
            modelBuilder.Entity<PriceSlot>()
                .HasOne(p => p.Facility).WithMany(f => f.PriceSlots).HasForeignKey(p => p.FacilityId)
                .OnDelete(DeleteBehavior.NoAction);

            // Booking
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.User).WithMany(u => u.Bookings).HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Court).WithMany(c => c.Bookings).HasForeignKey(b => b.CourtId)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.CheckInStaff).WithMany().HasForeignKey(b => b.CheckInBy)
                .OnDelete(DeleteBehavior.NoAction);

            // Product
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category).WithMany(c => c.Products).HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.NoAction);

            // Inventory
            modelBuilder.Entity<Inventory>()
                .HasOne(i => i.Product).WithMany(p => p.Inventories).HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<Inventory>()
                .HasOne(i => i.Facility).WithMany().HasForeignKey(i => i.FacilityId)
                .OnDelete(DeleteBehavior.NoAction);

            // StockTransaction
            modelBuilder.Entity<StockTransaction>()
                .HasOne(st => st.Facility).WithMany().HasForeignKey(st => st.FacilityId)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<StockTransaction>()
                .HasOne(st => st.Creator).WithMany().HasForeignKey(st => st.CreatedBy)
                .OnDelete(DeleteBehavior.NoAction);

            // StockTransactionDetail
            modelBuilder.Entity<StockTransactionDetail>()
                .HasOne(std => std.Transaction).WithMany(st => st.Details).HasForeignKey(std => std.TransactionId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<StockTransactionDetail>()
                .HasOne(std => std.Product).WithMany().HasForeignKey(std => std.ProductId)
                .OnDelete(DeleteBehavior.NoAction);

            // Order
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

            // OrderDetail — bao gồm FK mới tới ProductVariant
            modelBuilder.Entity<OrderDetail>()
                .HasOne(od => od.Order).WithMany(o => o.OrderDetails).HasForeignKey(od => od.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<OrderDetail>()
                .HasOne(od => od.Product).WithMany(p => p.OrderDetails).HasForeignKey(od => od.ProductId)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<OrderDetail>()
                .HasOne(od => od.Variant)
                .WithMany(v => v.OrderDetails)
                .HasForeignKey(od => od.VariantId)
                .OnDelete(DeleteBehavior.SetNull)   // Xóa variant → giữ OrderDetail, VariantId = null
                .IsRequired(false);

            // Voucher
            modelBuilder.Entity<Voucher>()
                .HasOne(v => v.Creator).WithMany().HasForeignKey(v => v.CreatedBy)
                .OnDelete(DeleteBehavior.NoAction);

            // VoucherUsage
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

            // ActivityLog
            modelBuilder.Entity<ActivityLog>()
                .HasOne(al => al.User).WithMany().HasForeignKey(al => al.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            // SystemSetting
            modelBuilder.Entity<SystemSetting>()
                .HasOne(ss => ss.Updater).WithMany().HasForeignKey(ss => ss.UpdatedBy)
                .OnDelete(DeleteBehavior.NoAction);

            // FacilityImage
            modelBuilder.Entity<FacilityImage>()
                .HasOne(fi => fi.Facility)
                .WithMany(f => f.FacilityImages)
                .HasForeignKey(fi => fi.FacilityId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        public static ApplicationDbContext Create(DbContextOptions<ApplicationDbContext> options)
        {
            return new ApplicationDbContext(options);
        }
    }
}
