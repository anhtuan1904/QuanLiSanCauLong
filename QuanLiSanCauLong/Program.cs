using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using QuanLiSanCauLong.Data;
using QuanLiSanCauLong.Helpers;
using QuanLiSanCauLong.Services;
using QuanLiSanCauLong.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// ===================================
// 1. DATABASE CONFIGURATION
// ===================================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
        }
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
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("StaffOnly", policy => policy.RequireRole("Staff", "Admin"));
    options.AddPolicy("CustomerOnly", policy => policy.RequireRole("Customer", "Staff", "Admin"));
});

// ===================================
// 3. SESSION CONFIGURATION
// ===================================
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// ===================================
// 4. SERVICES REGISTRATION
// ===================================

// Application Services
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IVoucherService, VoucherService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IActivityLogService, ActivityLogService>();

// Notification Services
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ISmsService, SmsService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

// Helper Services
builder.Services.AddSingleton<IFileHelper, FileHelper>();
builder.Services.AddSingleton<IExcelHelper, ExcelHelper>();
builder.Services.AddSingleton<IPdfHelper, PdfHelper>();
builder.Services.AddSingleton<IQRCodeHelper, QRCodeHelper>();

// ===================================
// 5. MVC & ROUTING
// ===================================
builder.Services.AddControllersWithViews(options =>
{
    // Add global filters
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = null;
    options.JsonSerializerOptions.WriteIndented = true;
});

builder.Services.AddRazorPages();

// ===================================
// 6. CORS (if needed for API)
// ===================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin",
        builder => builder
            .WithOrigins("https://yourdomain.com")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
});

// ===================================
// 7. CACHING
// ===================================
builder.Services.AddMemoryCache();
builder.Services.AddResponseCaching();

// ===================================
// 8. HTTP CLIENT (for external APIs)
// ===================================
builder.Services.AddHttpClient();

// ===================================
// 9. LOGGING
// ===================================
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddEventSourceLogger();

if (builder.Environment.IsProduction())
{
    builder.Logging.AddAzureWebAppDiagnostics();
}

// ===================================
// BUILD APPLICATION
// ===================================
var app = builder.Build();

// ===================================
// 10. MIDDLEWARE PIPELINE
// ===================================

// Error Handling
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

// Static Files
app.UseStaticFiles();

// Routing
app.UseRouting();

// CORS
app.UseCors("AllowSpecificOrigin");

// Caching
app.UseResponseCaching();

// Session (must be before Authentication)
app.UseSession();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Custom Middleware (if any)
// app.UseMiddleware<RequestLoggingMiddleware>();
// app.UseMiddleware<ExceptionHandlingMiddleware>();

// ===================================
// 11. ENDPOINT ROUTING
// ===================================
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// ===================================
// 12. DATABASE INITIALIZATION
// ===================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();

        // Apply migrations
        if (app.Environment.IsDevelopment())
        {
            context.Database.EnsureCreated();
            // context.Database.Migrate();
        }

        // Seed initial data
        await DbInitializer.Initialize(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while initializing the database.");
    }
}

// ===================================
// 13. RUN APPLICATION
// ===================================
app.Run();

// ===================================
// DATABASE INITIALIZER
// ===================================
public static class DbInitializer
{
    public static async Task Initialize(ApplicationDbContext context)
    {
        // Check if database has been seeded
        if (context.Users.Any())
        {
            return; // Database has been seeded
        }

        // Seed System Settings
        var settings = new[]
        {
            new QuanLiSanCauLong.Models.SystemSetting
            {
                SettingKey = "BookingCancellationHours",
                SettingValue = "2",
                SettingType = "Number",
                Description = "Số giờ tối thiểu trước khi có thể hủy đặt sân",
                Category = "Booking",
                IsActive = true,
                UpdatedAt = DateTime.Now
            },
            new QuanLiSanCauLong.Models.SystemSetting
            {
                SettingKey = "MaxBookingDaysInAdvance",
                SettingValue = "30",
                SettingType = "Number",
                Description = "Số ngày tối đa có thể đặt sân trước",
                Category = "Booking",
                IsActive = true,
                UpdatedAt = DateTime.Now
            },
            new QuanLiSanCauLong.Models.SystemSetting
            {
                SettingKey = "DefaultServiceFee",
                SettingValue = "0",
                SettingType = "Number",
                Description = "Phí dịch vụ mặc định (%)",
                Category = "Booking",
                IsActive = true,
                UpdatedAt = DateTime.Now
            },
            new QuanLiSanCauLong.Models.SystemSetting
            {
                SettingKey = "SiteName",
                SettingValue = "Hệ thống đặt sân cầu lông",
                SettingType = "String",
                Description = "Tên website",
                Category = "General",
                IsActive = true,
                UpdatedAt = DateTime.Now
            },
            new QuanLiSanCauLong.Models.SystemSetting
            {
                SettingKey = "SupportEmail",
                SettingValue = "support@badminton.com",
                SettingType = "String",
                Description = "Email hỗ trợ",
                Category = "General",
                IsActive = true,
                UpdatedAt = DateTime.Now
            },
            new QuanLiSanCauLong.Models.SystemSetting
            {
                SettingKey = "SupportPhone",
                SettingValue = "1900xxxx",
                SettingType = "String",
                Description = "Hotline hỗ trợ",
                Category = "General",
                IsActive = true,
                UpdatedAt = DateTime.Now
            }
        };

        context.SystemSettings.AddRange(settings);

        // Seed Admin User
        var adminUser = new QuanLiSanCauLong.Models.User
        {
            FullName = "Administrator",
            Email = "admin@badminton.com",
            Phone = "0123456789",
            PasswordHash = QuanLiSanCauLong.Helpers.PasswordHelper.HashPassword("Admin@123"),
            Role = "Admin",
            IsActive = true,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        context.Users.Add(adminUser);

        // Seed Product Categories
        var categories = new[]
        {
            new QuanLiSanCauLong.Models.ProductCategory
            {
                CategoryName = "Đồ ăn",
                CategoryType = "Food",
                Description = "Các món ăn nhẹ",
                DisplayOrder = 1,
                IsActive = true
            },
            new QuanLiSanCauLong.Models.ProductCategory
            {
                CategoryName = "Nước uống",
                CategoryType = "Beverage",
                Description = "Nước ngọt, nước suối",
                DisplayOrder = 2,
                IsActive = true
            },
            new QuanLiSanCauLong.Models.ProductCategory
            {
                CategoryName = "Dụng cụ",
                CategoryType = "Equipment",
                Description = "Vợt, cầu, phụ kiện",
                DisplayOrder = 3,
                IsActive = true
            },
            new QuanLiSanCauLong.Models.ProductCategory
            {
                CategoryName = "Quần áo",
                CategoryType = "Apparel",
                Description = "Áo, quần thể thao",
                DisplayOrder = 4,
                IsActive = true
            }
        };

        context.ProductCategories.AddRange(categories);

        await context.SaveChangesAsync();
    }
}