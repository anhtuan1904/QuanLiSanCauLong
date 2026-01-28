using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using QuanLiSanCauLong.Data;
using QuanLiSanCauLong.Helpers;
using QuanLiSanCauLong.Services;
using QuanLiSanCauLong.Services.Interfaces;
using QuanLiSanCauLong.Models;

var builder = WebApplication.CreateBuilder(args);

// ===================================
// 1. DATABASE CONFIGURATION
// ===================================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure()
    ));

// ===================================
// 2. AUTHENTICATION & AUTHORIZATION
// ===================================
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(2);
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

// ===================================
// 3. SERVICES REGISTRATION
// ===================================
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options => {
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Đăng ký các Interface và Service
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IVoucherService, VoucherService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IActivityLogService, ActivityLogService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ISmsService, SmsService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

builder.Services.AddSingleton<IFileHelper, FileHelper>();
builder.Services.AddSingleton<IExcelHelper, ExcelHelper>();
builder.Services.AddSingleton<IPdfHelper, PdfHelper>();
builder.Services.AddSingleton<IQRCodeHelper, QRCodeHelper>();

// CẤU HÌNH VIEW ENGINE ĐỂ TÌM TRONG THƯ MỤC VIEWS/ADMIN/
builder.Services.AddControllersWithViews()
    .AddRazorOptions(options =>
    {
        // {1} là Controller Name, {0} là Action Name
        options.ViewLocationFormats.Add("/Views/Admin/{1}/{0}.cshtml");
        options.ViewLocationFormats.Add("/Views/{1}/{0}.cshtml");
        options.ViewLocationFormats.Add("/Views/Shared/{0}.cshtml");
    })
    .AddJsonOptions(options => {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

builder.Services.AddRazorPages();

var app = builder.Build();

// ===================================
// 4. MIDDLEWARE PIPELINE
// ===================================
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// Routing mặc định
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// ===================================
// 5. DATABASE INITIALIZATION & SEEDING
// ===================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        await context.Database.MigrateAsync();
        await DbInitializer.Initialize(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Lỗi khi khởi tạo dữ liệu Database.");
    }
}

app.Run();

// ===================================
// 6. DB INITIALIZER
// ===================================
public static class DbInitializer
{
    public static async Task Initialize(ApplicationDbContext context)
    {
        if (!await context.SystemSettings.AnyAsync())
        {
            context.SystemSettings.AddRange(
                new SystemSetting { SettingKey = "SiteName", SettingValue = "Hệ thống cầu lông", Category = "General", IsActive = true, UpdatedAt = DateTime.Now },
                new SystemSetting { SettingKey = "SupportEmail", SettingValue = "admin@badminton.com", Category = "General", IsActive = true, UpdatedAt = DateTime.Now }
            );
            await context.SaveChangesAsync();
        }

        if (!await context.Users.AnyAsync(u => u.Email == "admin@badminton.com"))
        {
            var admin = new User
            {
                FullName = "Administrator",
                Email = "admin@badminton.com",
                Phone = "0123456789",
                PasswordHash = PasswordHelper.HashPassword("Admin@123"),
                Role = "Admin",
                IsActive = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            context.Users.Add(admin);
            await context.SaveChangesAsync();
        }

        if (!await context.ProductCategories.AnyAsync())
        {
            context.ProductCategories.AddRange(
                new ProductCategory { CategoryName = "Đồ ăn", CategoryType = "Food", IsActive = true },
                new ProductCategory { CategoryName = "Nước uống", CategoryType = "Beverage", IsActive = true },
                new ProductCategory { CategoryName = "Dụng cụ", CategoryType = "Equipment", IsActive = true }
            );
            await context.SaveChangesAsync();
        }
    }
}