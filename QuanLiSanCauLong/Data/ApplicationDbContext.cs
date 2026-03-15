using Microsoft.EntityFrameworkCore;
using QuanLiSanCauLong.Models;

namespace QuanLiSanCauLong.Data
{
    // ⚠️ QUAN TRỌNG: Thêm từ khóa "partial" vì OnModelCreating
    // được định nghĩa trong file ApplicationDbContext.Configuration.cs
    public partial class ApplicationDbContext : DbContext
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
        public DbSet<InventoryBatch> InventoryBatches { get; set; }
        public DbSet<InventoryTransaction> InventoryTransactions { get; set; }
        public DbSet<RentalItem> RentalItems { get; set; }

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

        // ── Nhân viên & Ca làm việc ──
        public DbSet<Shift> Shifts { get; set; }
        public DbSet<ShiftAssignment> ShiftAssignments { get; set; }

        public DbSet<CourtReview> CourtReviews { get; set; }
        public DbSet<ReviewReply> ReviewReplies { get; set; }
        public DbSet<ReviewLike> ReviewLikes { get; set; }
        public DbSet<ReviewImage> ReviewImages { get; set; }

        public static ApplicationDbContext Create(DbContextOptions<ApplicationDbContext> options)
            => new ApplicationDbContext(options);
    }
}
