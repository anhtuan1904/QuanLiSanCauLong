using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiSanCauLong.Models;

namespace QuanLiSanCauLong.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        // ── Đặt sân ──
        public DbSet<User> Users { get; set; }
        public DbSet<Facility> Facilities { get; set; }
        public DbSet<Court> Courts { get; set; }
        public DbSet<PriceSlot> PriceSlots { get; set; }
        public DbSet<Booking> Bookings { get; set; }

        // ── Sản phẩm & Kho ──
        public DbSet<ProductCategory> ProductCategories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductVariant> ProductVariants { get; set; }
        public DbSet<Inventory> Inventories { get; set; }
        public DbSet<InventoryBatch> InventoryBatches { get; set; }  // MỚI
        public DbSet<InventoryTransaction> InventoryTransactions { get; set; }  // MỚI (Phase 1+2)
        public DbSet<RentalItem> RentalItems { get; set; }  // MỚI

        // ── Giao dịch kho cũ (giữ để không break migration cũ) ──
        public DbSet<StockTransaction> StockTransactions { get; set; }
        public DbSet<StockTransactionDetail> StockTransactionDetails { get; set; }

        // ── Đơn hàng ──
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<Voucher> Vouchers { get; set; }
        public DbSet<VoucherUsage> VoucherUsages { get; set; }

        // ── Hệ thống ──
        public DbSet<ActivityLog> ActivityLogs { get; set; }
        public DbSet<SystemSetting> SystemSettings { get; set; }

        // ── Nội dung ──
        public DbSet<Post> Posts { get; set; }
        public DbSet<BlogPost> BlogPosts { get; set; }
        public DbSet<BlogCategory> BlogCategories { get; set; }
        public DbSet<BlogComment> BlogComments { get; set; }
        public DbSet<BlogReview> BlogReviews { get; set; }
        public DbSet<BlogReviewLike> BlogReviewLikes { get; set; }

        // ── Hình ảnh ──
        public DbSet<CourtImage> CourtImages { get; set; }
        public DbSet<FacilityImage> FacilityImages { get; set; }
        public DbSet<ServiceImage> ServiceImages { get; set; }

        // ── Dịch vụ & Tuyển dụng ──
        public DbSet<Service> Services { get; set; }
        public DbSet<ServiceInquiry> ServiceInquiries { get; set; }
        public DbSet<JobPosting> JobPostings { get; set; }
        public DbSet<JobApplication> JobApplications { get; set; }
        public DbSet<ContactMessage> ContactMessages { get; set; }

        // ── Khác ──
        public DbSet<Course> Courses { get; set; }
        public DbSet<StringingService> StringingServices { get; set; }
        public DbSet<Tournament> Tournaments { get; set; }

        protected override void OnModelCreating(ModelBuilder mb)
        {
            base.OnModelCreating(mb);

            // ════════════════════════════════════════════════════════════
            // COURSE / STRINGING / TOURNAMENT
            // ════════════════════════════════════════════════════════════
            mb.Entity<Course>(e => {
                e.HasKey(x => x.CourseId);
                e.Property(x => x.CourseName).IsRequired().HasMaxLength(200);
                e.Property(x => x.TuitionFee).HasPrecision(18, 2);
                e.Property(x => x.DiscountFee).HasPrecision(18, 2);
                e.Property(x => x.Status).HasDefaultValue("Active");
                e.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");
            });

            mb.Entity<StringingService>(e => {
                e.HasKey(x => x.StringingId);
                e.Property(x => x.ServiceName).IsRequired().HasMaxLength(200);
                e.Property(x => x.Price).HasPrecision(18, 2);
                e.Property(x => x.Status).HasDefaultValue("Active");
                e.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");
            });

            mb.Entity<Tournament>(e => {
                e.HasKey(x => x.TournamentId);
                e.Property(x => x.TournamentName).IsRequired().HasMaxLength(200);
                e.Property(x => x.EntryFee).HasPrecision(18, 2);
                e.Property(x => x.PrizeMoney).HasPrecision(18, 2);
                e.Property(x => x.Status).HasDefaultValue("Upcoming");
                e.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");
            });

            // ════════════════════════════════════════════════════════════
            // BLOG REVIEW / LIKE
            // ════════════════════════════════════════════════════════════
            mb.Entity<BlogReview>(e => {
                e.HasIndex(r => r.BlogId);
                e.HasIndex(r => r.Status);
                e.HasIndex(r => r.CreatedAt);
                e.HasIndex(r => new { r.BlogId, r.Status });
                e.Property(r => r.Content).HasMaxLength(2000);
                e.Property(r => r.Status).HasDefaultValue("Pending");
                e.Property(r => r.Rating).HasDefaultValue(5);
            });

            mb.Entity<BlogReviewLike>(e => {
                e.HasIndex(l => new { l.ReviewId, l.UserId })
                 .IsUnique().HasFilter("[UserId] IS NOT NULL");
                e.HasOne(l => l.Review).WithMany(r => r.Likes)
                 .HasForeignKey(l => l.ReviewId).OnDelete(DeleteBehavior.Cascade);
            });

            // ════════════════════════════════════════════════════════════
            // PRODUCT VARIANT
            // ════════════════════════════════════════════════════════════
            mb.Entity<ProductVariant>(e => {
                e.HasKey(v => v.VariantId);
                e.Property(v => v.SizeName).HasMaxLength(50);
                e.Property(v => v.ColorName).HasMaxLength(50);
                e.Property(v => v.VariantSKU).HasMaxLength(100);
                e.Property(v => v.StockQuantity).HasDefaultValue(0);
                e.Property(v => v.ReservedQuantity).HasDefaultValue(0);
                e.Property(v => v.MinStockLevel).HasDefaultValue(0);
                e.Property(v => v.IsActive).HasDefaultValue(true);
                e.HasOne(v => v.Product).WithMany(p => p.Variants)
                 .HasForeignKey(v => v.ProductId).OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(v => v.ProductId);
            });

            // ════════════════════════════════════════════════════════════
            // INVENTORY BATCH  (MỚI – Phase 1+2)
            // ════════════════════════════════════════════════════════════
            mb.Entity<InventoryBatch>(e => {
                e.HasKey(b => b.BatchId);
                e.Property(b => b.BatchNumber).HasMaxLength(50);
                e.Property(b => b.DocumentReference).HasMaxLength(50);
                e.Property(b => b.Status).HasMaxLength(10).HasDefaultValue("Active");
                e.Property(b => b.CostPrice).HasPrecision(18, 0);
                e.Property(b => b.ReceivedDate).HasDefaultValueSql("GETDATE()");
                e.HasOne(b => b.Inventory).WithMany(i => i.Batches)
                 .HasForeignKey(b => b.InventoryId).OnDelete(DeleteBehavior.Restrict);
                e.HasIndex(b => b.InventoryId);
                e.HasIndex(b => b.Status);
                e.HasIndex(b => b.ExpiryDate);
            });

            // ════════════════════════════════════════════════════════════
            // INVENTORY TRANSACTION  (MỚI – Phase 1+2)
            // ════════════════════════════════════════════════════════════
            mb.Entity<InventoryTransaction>(e => {
                e.HasKey(t => t.TransactionId);
                e.Property(t => t.Type).IsRequired().HasMaxLength(20);
                e.Property(t => t.UserEmail).HasMaxLength(100);
                e.Property(t => t.Note).HasMaxLength(500);
                e.Property(t => t.BatchNumber).HasMaxLength(50);
                e.Property(t => t.ReferenceType).HasMaxLength(20);
                e.Property(t => t.CostPrice).HasPrecision(18, 0).HasDefaultValue(0m);
                e.Property(t => t.SalePrice).HasPrecision(18, 0).HasDefaultValue(0m);
                e.Property(t => t.TransactionDate).HasDefaultValueSql("GETDATE()");
                e.HasOne(t => t.Product).WithMany()
                 .HasForeignKey(t => t.ProductId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(t => t.Facility).WithMany()
                 .HasForeignKey(t => t.FacilityId).OnDelete(DeleteBehavior.Restrict);
                e.HasIndex(t => t.ProductId);
                e.HasIndex(t => t.FacilityId);
                e.HasIndex(t => t.Type);
                e.HasIndex(t => t.TransactionDate);
                e.HasIndex(t => new { t.ReferenceId, t.ReferenceType });
            });

            // ════════════════════════════════════════════════════════════
            // RENTAL ITEM  (MỚI – Phase 1+2)
            // ════════════════════════════════════════════════════════════
            mb.Entity<RentalItem>(e => {
                e.HasKey(r => r.RentalItemId);
                e.Property(r => r.CourtCode).HasMaxLength(20);
                e.Property(r => r.CustomerName).HasMaxLength(100);
                e.Property(r => r.CustomerPhone).HasMaxLength(20);
                e.Property(r => r.Size).HasMaxLength(10);
                e.Property(r => r.Status).HasMaxLength(15).HasDefaultValue("Active");
                e.Property(r => r.Note).HasMaxLength(300);
                e.Property(r => r.CreatedBy).HasMaxLength(100);
                e.Property(r => r.CleaningFeeCharged).HasPrecision(18, 0).HasDefaultValue(0m);
                e.Property(r => r.RentedAt).HasDefaultValueSql("GETDATE()");
                e.HasOne(r => r.Inventory).WithMany(i => i.RentalItems)
                 .HasForeignKey(r => r.InventoryId).OnDelete(DeleteBehavior.Restrict);
                e.HasIndex(r => r.InventoryId);
                e.HasIndex(r => r.Status);
                e.HasIndex(r => r.RentedAt);
            });

            // ════════════════════════════════════════════════════════════
            // PRECISION DECIMAL CÁC BẢNG CŨ
            // ════════════════════════════════════════════════════════════
            mb.Entity<Product>().Property(p => p.Price).HasPrecision(10, 2);
            mb.Entity<Product>().Property(p => p.CostPrice).HasPrecision(10, 2);
            mb.Entity<Product>().Property(p => p.SalePrice).HasPrecision(10, 2);
            mb.Entity<Product>().Property(p => p.COGSPrice).HasPrecision(10, 2);

            mb.Entity<JobPosting>().Property(j => j.SalaryMin).HasPrecision(18, 2);
            mb.Entity<JobPosting>().Property(j => j.SalaryMax).HasPrecision(18, 2);
            mb.Entity<JobApplication>().Property(a => a.ExpectedSalary).HasPrecision(18, 2);
            mb.Entity<Service>().Property(s => s.Price).HasPrecision(18, 2);
            mb.Entity<Service>().Property(s => s.DiscountPrice).HasPrecision(18, 2);

            mb.Entity<PriceSlot>().Property(p => p.Price).HasPrecision(10, 2);
            mb.Entity<Booking>().Property(b => b.CourtPrice).HasPrecision(10, 2);
            mb.Entity<Booking>().Property(b => b.ServiceFee).HasPrecision(10, 2);
            mb.Entity<Booking>().Property(b => b.DiscountAmount).HasPrecision(10, 2);
            mb.Entity<Booking>().Property(b => b.TotalPrice).HasPrecision(10, 2);
            mb.Entity<Order>().Property(o => o.SubTotal).HasPrecision(10, 2);
            mb.Entity<Order>().Property(o => o.DiscountAmount).HasPrecision(10, 2);
            mb.Entity<Order>().Property(o => o.TotalAmount).HasPrecision(10, 2);
            mb.Entity<OrderDetail>().Property(od => od.UnitPrice).HasPrecision(10, 2);
            mb.Entity<OrderDetail>().Property(od => od.DiscountAmount).HasPrecision(10, 2);
            mb.Entity<OrderDetail>().Property(od => od.TotalPrice).HasPrecision(10, 2);
            mb.Entity<Voucher>().Property(v => v.DiscountValue).HasPrecision(10, 2);
            mb.Entity<VoucherUsage>().Property(vu => vu.DiscountAmount).HasPrecision(10, 2);
            mb.Entity<StockTransaction>().Property(st => st.TotalAmount).HasPrecision(10, 2);
            mb.Entity<StockTransactionDetail>().Property(std => std.UnitPrice).HasPrecision(10, 2);
            mb.Entity<StockTransactionDetail>().Property(std => std.TotalPrice).HasPrecision(10, 2);

            // ════════════════════════════════════════════════════════════
            // UNIQUE INDEXES
            // ════════════════════════════════════════════════════════════
            mb.Entity<User>().HasIndex(u => u.Email).IsUnique();
            mb.Entity<Booking>().HasIndex(b => b.BookingCode).IsUnique();
            mb.Entity<Order>().HasIndex(o => o.OrderCode).IsUnique();
            mb.Entity<Product>().HasIndex(p => p.ProductCode).IsUnique();
            mb.Entity<Voucher>().HasIndex(v => v.VoucherCode).IsUnique();
            mb.Entity<SystemSetting>().HasIndex(s => s.SettingKey).IsUnique();
            mb.Entity<Inventory>().HasIndex(i => new { i.ProductId, i.FacilityId }).IsUnique();

            // ════════════════════════════════════════════════════════════
            // RELATIONSHIPS (giữ nguyên từ file gốc)
            // ════════════════════════════════════════════════════════════
            mb.Entity<User>().HasOne(u => u.Facility).WithMany()
              .HasForeignKey(u => u.FacilityId).OnDelete(DeleteBehavior.NoAction);

            mb.Entity<Court>().HasOne(c => c.Facility).WithMany(f => f.Courts)
              .HasForeignKey(c => c.FacilityId).OnDelete(DeleteBehavior.NoAction);

            mb.Entity<PriceSlot>().HasOne(p => p.Facility).WithMany(f => f.PriceSlots)
              .HasForeignKey(p => p.FacilityId).OnDelete(DeleteBehavior.NoAction);

            mb.Entity<Booking>().HasOne(b => b.User).WithMany(u => u.Bookings)
              .HasForeignKey(b => b.UserId).OnDelete(DeleteBehavior.NoAction);
            mb.Entity<Booking>().HasOne(b => b.Court).WithMany(c => c.Bookings)
              .HasForeignKey(b => b.CourtId).OnDelete(DeleteBehavior.NoAction);
            mb.Entity<Booking>().HasOne(b => b.CheckInStaff).WithMany()
              .HasForeignKey(b => b.CheckInBy).OnDelete(DeleteBehavior.NoAction);

            mb.Entity<Product>().HasOne(p => p.Category).WithMany(c => c.Products)
              .HasForeignKey(p => p.CategoryId).OnDelete(DeleteBehavior.NoAction);

            mb.Entity<Inventory>().HasOne(i => i.Product).WithMany(p => p.Inventories)
              .HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.NoAction);
            mb.Entity<Inventory>().HasOne(i => i.Facility).WithMany()
              .HasForeignKey(i => i.FacilityId).OnDelete(DeleteBehavior.NoAction);

            mb.Entity<StockTransaction>().HasOne(st => st.Facility).WithMany()
              .HasForeignKey(st => st.FacilityId).OnDelete(DeleteBehavior.NoAction);
            mb.Entity<StockTransaction>().HasOne(st => st.Creator).WithMany()
              .HasForeignKey(st => st.CreatedBy).OnDelete(DeleteBehavior.NoAction);

            mb.Entity<StockTransactionDetail>().HasOne(std => std.Transaction).WithMany(st => st.Details)
              .HasForeignKey(std => std.TransactionId).OnDelete(DeleteBehavior.Cascade);
            mb.Entity<StockTransactionDetail>().HasOne(std => std.Product).WithMany()
              .HasForeignKey(std => std.ProductId).OnDelete(DeleteBehavior.NoAction);

            mb.Entity<Order>().HasOne(o => o.Booking).WithMany(b => b.Orders)
              .HasForeignKey(o => o.BookingId).OnDelete(DeleteBehavior.NoAction);
            mb.Entity<Order>().HasOne(o => o.User).WithMany(u => u.Orders)
              .HasForeignKey(o => o.UserId).OnDelete(DeleteBehavior.NoAction);
            mb.Entity<Order>().HasOne(o => o.Facility).WithMany()
              .HasForeignKey(o => o.FacilityId).OnDelete(DeleteBehavior.NoAction);
            mb.Entity<Order>().HasOne(o => o.Creator).WithMany()
              .HasForeignKey(o => o.CreatedBy).OnDelete(DeleteBehavior.NoAction);

            mb.Entity<OrderDetail>().HasOne(od => od.Order).WithMany(o => o.OrderDetails)
              .HasForeignKey(od => od.OrderId).OnDelete(DeleteBehavior.Cascade);
            mb.Entity<OrderDetail>().HasOne(od => od.Product).WithMany(p => p.OrderDetails)
              .HasForeignKey(od => od.ProductId).OnDelete(DeleteBehavior.NoAction);
            mb.Entity<OrderDetail>().HasOne(od => od.Variant).WithMany(v => v.OrderDetails)
              .HasForeignKey(od => od.VariantId).OnDelete(DeleteBehavior.SetNull).IsRequired(false);

            mb.Entity<Voucher>().HasOne(v => v.Creator).WithMany()
              .HasForeignKey(v => v.CreatedBy).OnDelete(DeleteBehavior.NoAction);

            mb.Entity<VoucherUsage>().HasOne(vu => vu.Voucher).WithMany(v => v.VoucherUsages)
              .HasForeignKey(vu => vu.VoucherId).OnDelete(DeleteBehavior.NoAction);
            mb.Entity<VoucherUsage>().HasOne(vu => vu.User).WithMany()
              .HasForeignKey(vu => vu.UserId).OnDelete(DeleteBehavior.NoAction);
            mb.Entity<VoucherUsage>().HasOne(vu => vu.Booking).WithMany(b => b.VoucherUsages)
              .HasForeignKey(vu => vu.BookingId).OnDelete(DeleteBehavior.NoAction);
            mb.Entity<VoucherUsage>().HasOne(vu => vu.Order).WithMany(o => o.VoucherUsages)
              .HasForeignKey(vu => vu.OrderId).OnDelete(DeleteBehavior.NoAction);

            mb.Entity<ActivityLog>().HasOne(al => al.User).WithMany()
              .HasForeignKey(al => al.UserId).OnDelete(DeleteBehavior.NoAction);

            mb.Entity<SystemSetting>().HasOne(ss => ss.Updater).WithMany()
              .HasForeignKey(ss => ss.UpdatedBy).OnDelete(DeleteBehavior.NoAction);

            mb.Entity<FacilityImage>().HasOne(fi => fi.Facility).WithMany(f => f.FacilityImages)
              .HasForeignKey(fi => fi.FacilityId).OnDelete(DeleteBehavior.Cascade);
        }

        public static ApplicationDbContext Create(DbContextOptions<ApplicationDbContext> options)
            => new ApplicationDbContext(options);
    }
}
