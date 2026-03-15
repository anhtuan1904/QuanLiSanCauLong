using Microsoft.EntityFrameworkCore;
using QuanLiSanCauLong.Models;

namespace QuanLiSanCauLong.Data
{
    /// <summary>
    /// Partial class — toàn bộ fluent configuration OnModelCreating
    /// Chia theo region/domain để dễ đọc và maintain
    /// </summary>
    public partial class ApplicationDbContext
    {
        protected override void OnModelCreating(ModelBuilder mb)
        {
            base.OnModelCreating(mb);
            ConfigureUsers(mb);
            ConfigureFacilitiesAndCourts(mb);
            ConfigureBookings(mb);
            ConfigureProducts(mb);
            ConfigureInventory(mb);
            ConfigureOrders(mb);
            ConfigureBlog(mb);
            ConfigureServicesAndJobs(mb);
            ConfigureMisc(mb);
            ConfigureShifts(mb);
            ConfigureReviews(mb);       // ← THÊM DÒNG NÀY
        }
        // ════════════════════════════════════════════════════════════════
        // USERS
        // ════════════════════════════════════════════════════════════════
        private static void ConfigureUsers(ModelBuilder mb)
        {
            mb.Entity<User>(e =>
            {
                e.HasIndex(u => u.Email).IsUnique();

                e.Property(u => u.Status).HasDefaultValue("Active");
                e.Property(u => u.IsActive).HasDefaultValue(true);
                e.Property(u => u.CreatedAt).HasDefaultValueSql("GETDATE()");
                e.Property(u => u.UpdatedAt).HasDefaultValueSql("GETDATE()");

                // AvatarUrl — nullable, max 500
                e.Property(u => u.AvatarUrl)
                    .HasMaxLength(500)
                    .IsRequired(false);

                e.HasOne(u => u.Facility).WithMany()
                    .HasForeignKey(u => u.FacilityId)
                    .OnDelete(DeleteBehavior.NoAction);
            });
        }

        // ════════════════════════════════════════════════════════════════
        // FACILITIES & COURTS
        // ════════════════════════════════════════════════════════════════
        private static void ConfigureFacilitiesAndCourts(ModelBuilder mb)
        {
            mb.Entity<Court>(e =>
            {
                e.HasOne(c => c.Facility).WithMany(f => f.Courts)
                    .HasForeignKey(c => c.FacilityId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            mb.Entity<PriceSlot>(e =>
            {
                e.Property(p => p.Price).HasPrecision(10, 2);
                e.HasOne(p => p.Facility).WithMany(f => f.PriceSlots)
                    .HasForeignKey(p => p.FacilityId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            mb.Entity<FacilityImage>(e =>
            {
                e.HasOne(fi => fi.Facility).WithMany(f => f.FacilityImages)
                    .HasForeignKey(fi => fi.FacilityId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        // ════════════════════════════════════════════════════════════════
        // BOOKINGS
        // ════════════════════════════════════════════════════════════════
        private static void ConfigureBookings(ModelBuilder mb)
        {
            mb.Entity<Booking>(e =>
            {
                e.HasIndex(b => b.BookingCode).IsUnique();

                e.Property(b => b.CourtPrice).HasPrecision(10, 2);
                e.Property(b => b.ServiceFee).HasPrecision(10, 2);
                e.Property(b => b.DiscountAmount).HasPrecision(10, 2);
                e.Property(b => b.TotalPrice).HasPrecision(10, 2);

                e.HasOne(b => b.User).WithMany(u => u.Bookings)
                    .HasForeignKey(b => b.UserId).OnDelete(DeleteBehavior.NoAction);
                e.HasOne(b => b.Court).WithMany(c => c.Bookings)
                    .HasForeignKey(b => b.CourtId).OnDelete(DeleteBehavior.NoAction);
                e.HasOne(b => b.CheckInStaff).WithMany()
                    .HasForeignKey(b => b.CheckInBy).OnDelete(DeleteBehavior.NoAction);
            });

            // ✅ MỚI: Cấu hình cho CourtReview (Đánh giá sân sau khi đặt)
            mb.Entity<CourtReview>(e =>
            {
                e.HasIndex(r => new { r.CourtId, r.UserId, r.BookingId })
                    .IsUnique(); // Mỗi booking chỉ được review 1 lần

                e.Property(r => r.Content).HasMaxLength(1000);
                e.Property(r => r.Rating).IsRequired();
            });
        }
        private void ConfigureReviews(ModelBuilder mb)
        {
            // Cấu hình Like cho Review
            mb.Entity<ReviewLike>(e =>
            {
                // Khóa ngoại trỏ đến User
                e.HasOne(l => l.User)
                    .WithMany()
                    .HasForeignKey(l => l.UserId)
                    .OnDelete(DeleteBehavior.NoAction);

                // Khóa ngoại trỏ đến Review (Sửa lại chỗ này)
                e.HasOne(l => l.Review)
                    .WithMany(r => r.Likes) // ← Phải chỉ định rõ r.Likes ở đây
                    .HasForeignKey(l => l.ReviewId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Đảm bảo mỗi user chỉ like 1 lần (Unique Index)
                e.HasIndex(l => new { l.ReviewId, l.UserId }).IsUnique();
            });
        }
        // ════════════════════════════════════════════════════════════════
        // PRODUCTS
        // ════════════════════════════════════════════════════════════════
        private static void ConfigureProducts(ModelBuilder mb)
        {
            mb.Entity<Product>(e =>
            {
                e.HasIndex(p => p.ProductCode).IsUnique();
                e.Property(p => p.Price).HasPrecision(10, 2);
                e.Property(p => p.CostPrice).HasPrecision(10, 2);
                e.Property(p => p.SalePrice).HasPrecision(10, 2);
                e.Property(p => p.COGSPrice).HasPrecision(10, 2);

                e.HasOne(p => p.Category).WithMany(c => c.Products)
                    .HasForeignKey(p => p.CategoryId).OnDelete(DeleteBehavior.NoAction);
            });

            mb.Entity<ProductVariant>(e =>
            {
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
        }

        // ════════════════════════════════════════════════════════════════
        // INVENTORY
        // ════════════════════════════════════════════════════════════════
        private static void ConfigureInventory(ModelBuilder mb)
        {
            mb.Entity<Inventory>(e =>
            {
                e.HasIndex(i => new { i.ProductId, i.FacilityId }).IsUnique();

                e.HasOne(i => i.Product).WithMany(p => p.Inventories)
                    .HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.NoAction);
                e.HasOne(i => i.Facility).WithMany()
                    .HasForeignKey(i => i.FacilityId).OnDelete(DeleteBehavior.NoAction);
            });

            mb.Entity<InventoryBatch>(e =>
            {
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

            mb.Entity<InventoryTransaction>(e =>
            {
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

            mb.Entity<RentalItem>(e =>
            {
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

            // Kho cũ
            mb.Entity<StockTransaction>(e =>
            {
                e.Property(st => st.TotalAmount).HasPrecision(10, 2);
                e.HasOne(st => st.Facility).WithMany()
                    .HasForeignKey(st => st.FacilityId).OnDelete(DeleteBehavior.NoAction);
                e.HasOne(st => st.Creator).WithMany()
                    .HasForeignKey(st => st.CreatedBy).OnDelete(DeleteBehavior.NoAction);
            });

            mb.Entity<StockTransactionDetail>(e =>
            {
                e.Property(std => std.UnitPrice).HasPrecision(10, 2);
                e.Property(std => std.TotalPrice).HasPrecision(10, 2);

                e.HasOne(std => std.Transaction).WithMany(st => st.Details)
                    .HasForeignKey(std => std.TransactionId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(std => std.Product).WithMany()
                    .HasForeignKey(std => std.ProductId).OnDelete(DeleteBehavior.NoAction);
            });
        }

        // ════════════════════════════════════════════════════════════════
        // ORDERS & VOUCHERS
        // ════════════════════════════════════════════════════════════════
        private static void ConfigureOrders(ModelBuilder mb)
        {
            mb.Entity<Order>(e =>
            {
                e.HasIndex(o => o.OrderCode).IsUnique();
                e.Property(o => o.SubTotal).HasPrecision(10, 2);
                e.Property(o => o.DiscountAmount).HasPrecision(10, 2);
                e.Property(o => o.TotalAmount).HasPrecision(10, 2);

                e.HasOne(o => o.Booking).WithMany(b => b.Orders)
                    .HasForeignKey(o => o.BookingId).OnDelete(DeleteBehavior.NoAction);
                e.HasOne(o => o.User).WithMany(u => u.Orders)
                    .HasForeignKey(o => o.UserId).OnDelete(DeleteBehavior.NoAction);
                e.HasOne(o => o.Facility).WithMany()
                    .HasForeignKey(o => o.FacilityId).OnDelete(DeleteBehavior.NoAction);
                e.HasOne(o => o.Creator).WithMany()
                    .HasForeignKey(o => o.CreatedBy).OnDelete(DeleteBehavior.NoAction);
            });

            mb.Entity<OrderDetail>(e =>
            {
                e.Property(od => od.UnitPrice).HasPrecision(10, 2);
                e.Property(od => od.DiscountAmount).HasPrecision(10, 2);
                e.Property(od => od.TotalPrice).HasPrecision(10, 2);

                e.HasOne(od => od.Order).WithMany(o => o.OrderDetails)
                    .HasForeignKey(od => od.OrderId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(od => od.Product).WithMany(p => p.OrderDetails)
                    .HasForeignKey(od => od.ProductId).OnDelete(DeleteBehavior.NoAction);
                e.HasOne(od => od.Variant).WithMany(v => v.OrderDetails)
                    .HasForeignKey(od => od.VariantId)
                    .OnDelete(DeleteBehavior.SetNull).IsRequired(false);
            });

            mb.Entity<Voucher>(e =>
            {
                e.HasIndex(v => v.VoucherCode).IsUnique();
                e.Property(v => v.DiscountValue).HasPrecision(10, 2);
                e.HasOne(v => v.Creator).WithMany()
                    .HasForeignKey(v => v.CreatedBy).OnDelete(DeleteBehavior.NoAction);
            });

            mb.Entity<VoucherUsage>(e =>
            {
                e.Property(vu => vu.DiscountAmount).HasPrecision(10, 2);

                e.HasOne(vu => vu.Voucher).WithMany(v => v.VoucherUsages)
                    .HasForeignKey(vu => vu.VoucherId).OnDelete(DeleteBehavior.NoAction);
                e.HasOne(vu => vu.User).WithMany()
                    .HasForeignKey(vu => vu.UserId).OnDelete(DeleteBehavior.NoAction);
                e.HasOne(vu => vu.Booking).WithMany(b => b.VoucherUsages)
                    .HasForeignKey(vu => vu.BookingId).OnDelete(DeleteBehavior.NoAction);
                e.HasOne(vu => vu.Order).WithMany(o => o.VoucherUsages)
                    .HasForeignKey(vu => vu.OrderId).OnDelete(DeleteBehavior.NoAction);
            });
        }

        // ════════════════════════════════════════════════════════════════
        // BLOG & CONTENT
        // ════════════════════════════════════════════════════════════════
        private static void ConfigureBlog(ModelBuilder mb)
        {
            mb.Entity<BlogReview>(e =>
            {
                e.HasIndex(r => r.BlogId);
                e.HasIndex(r => r.Status);
                e.HasIndex(r => r.CreatedAt);
                e.HasIndex(r => new { r.BlogId, r.Status });
                e.Property(r => r.Content).HasMaxLength(2000);
                e.Property(r => r.Status).HasDefaultValue("Pending");
                e.Property(r => r.Rating).HasDefaultValue(5);
            });

            mb.Entity<BlogReviewLike>(e =>
            {
                // ✅ Cập nhật: Thêm .IsUnique() để mỗi user chỉ like 1 lần
                // Filter đảm bảo không bị lỗi nếu UserId null (trong trường hợp cho phép khách vãng lai nhưng không dùng unique)
                e.HasIndex(l => new { l.ReviewId, l.UserId })
                    .IsUnique()
                    .HasFilter("[UserId] IS NOT NULL");

                e.HasOne(l => l.Review).WithMany(r => r.Likes)
                    .HasForeignKey(l => l.ReviewId).OnDelete(DeleteBehavior.Cascade);
            });
        }
        // ════════════════════════════════════════════════════════════════
        // SERVICES & JOBS
        // ════════════════════════════════════════════════════════════════
        private static void ConfigureServicesAndJobs(ModelBuilder mb)
        {
            mb.Entity<Service>(e =>
            {
                e.Property(s => s.Price).HasPrecision(18, 2);
                e.Property(s => s.DiscountPrice).HasPrecision(18, 2);
            });

            mb.Entity<JobPosting>(e =>
            {
                e.Property(j => j.SalaryMin).HasPrecision(18, 2);
                e.Property(j => j.SalaryMax).HasPrecision(18, 2);
            });

            mb.Entity<JobApplication>(e =>
            {
                e.Property(a => a.ExpectedSalary).HasPrecision(18, 2);
            });
        }

        // ════════════════════════════════════════════════════════════════
        // MISC (Course / Stringing / Tournament / System)
        // ════════════════════════════════════════════════════════════════
        private static void ConfigureMisc(ModelBuilder mb)
        {
            mb.Entity<Course>(e =>
            {
                e.HasKey(x => x.CourseId);
                e.Property(x => x.CourseName).IsRequired().HasMaxLength(200);
                e.Property(x => x.TuitionFee).HasPrecision(18, 2);
                e.Property(x => x.DiscountFee).HasPrecision(18, 2);
                e.Property(x => x.Status).HasDefaultValue("Active");
                e.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");
            });

            mb.Entity<StringingService>(e =>
            {
                e.HasKey(x => x.StringingId);
                e.Property(x => x.ServiceName).IsRequired().HasMaxLength(200);
                e.Property(x => x.Price).HasPrecision(18, 2);
                e.Property(x => x.Status).HasDefaultValue("Active");
                e.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");
            });

            mb.Entity<Tournament>(e =>
            {
                e.HasKey(x => x.TournamentId);
                e.Property(x => x.TournamentName).IsRequired().HasMaxLength(200);
                e.Property(x => x.EntryFee).HasPrecision(18, 2);
                e.Property(x => x.PrizeMoney).HasPrecision(18, 2);
                e.Property(x => x.Status).HasDefaultValue("Upcoming");
                e.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");
            });

            mb.Entity<SystemSetting>(e =>
            {
                e.HasIndex(s => s.SettingKey).IsUnique();
                e.HasOne(ss => ss.Updater).WithMany()
                    .HasForeignKey(ss => ss.UpdatedBy).OnDelete(DeleteBehavior.NoAction);
            });

            mb.Entity<ActivityLog>(e =>
            {
                e.HasOne(al => al.User).WithMany()
                    .HasForeignKey(al => al.UserId).OnDelete(DeleteBehavior.NoAction);
            });
        }


        // ════════════════════════════════════════════════════════════════
        // ✅ MỚI: SHIFTS & SHIFT ASSIGNMENTS
        // ════════════════════════════════════════════════════════════════
        private static void ConfigureShifts(ModelBuilder mb)
        {
            mb.Entity<Shift>(e =>
            {
                e.HasKey(s => s.ShiftId);
                e.Property(s => s.ShiftName).IsRequired().HasMaxLength(100);
                e.Property(s => s.Color).HasMaxLength(7).HasDefaultValue("#d4a017");
                e.Property(s => s.Description).HasMaxLength(500);
                e.Property(s => s.IsActive).HasDefaultValue(true);
                e.Property(s => s.CreatedAt).HasDefaultValueSql("GETDATE()");
            });

            mb.Entity<ShiftAssignment>(e =>
            {
                e.HasKey(a => a.AssignmentId);
                e.Property(a => a.Status).HasMaxLength(20).HasDefaultValue("Scheduled");
                e.Property(a => a.Note).HasMaxLength(500);
                e.Property(a => a.CreatedAt).HasDefaultValueSql("GETDATE()");
                e.Property(a => a.UpdatedAt).HasDefaultValueSql("GETDATE()");

                // Relationships
                e.HasOne(a => a.User).WithMany()
                    .HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(a => a.Shift).WithMany(s => s.Assignments)
                    .HasForeignKey(a => a.ShiftId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(a => a.Facility).WithMany()
                    .HasForeignKey(a => a.FacilityId).OnDelete(DeleteBehavior.NoAction);

                // Indexes — tối ưu query lịch phân ca
                e.HasIndex(a => new { a.UserId, a.WorkDate })
                    .HasDatabaseName("IX_ShiftAssignments_UserId_WorkDate");
                e.HasIndex(a => new { a.FacilityId, a.WorkDate })
                    .HasDatabaseName("IX_ShiftAssignments_FacilityId_WorkDate");
                e.HasIndex(a => a.Status)
                    .HasDatabaseName("IX_ShiftAssignments_Status");
            });
        }
    }
}
