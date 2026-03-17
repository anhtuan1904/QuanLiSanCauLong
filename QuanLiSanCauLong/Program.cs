using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using QuanLiSanCauLong.Data;
using QuanLiSanCauLong.Helpers;
using QuanLiSanCauLong.Services;
using QuanLiSanCauLong.Services.Interfaces;
using QuanLiSanCauLong.Models;

var builder = WebApplication.CreateBuilder(args);

// ===================================
// 1. DATABASE
// ===================================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.EnableRetryOnFailure()
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
// 3. SERVICES (DI)
// ===================================
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Register Services
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IVoucherService, VoucherService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<InventoryService>();

builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IActivityLogService, ActivityLogService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ISmsService, SmsService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

// Register Helpers
builder.Services.AddSingleton<IFileHelper, FileHelper>();
builder.Services.AddSingleton<IExcelHelper, ExcelHelper>();
builder.Services.AddSingleton<IPdfHelper, PdfHelper>();
builder.Services.AddSingleton<IQRCodeHelper, QRCodeHelper>();

// ===================================
// 4. MVC + RAZOR + API
// ===================================
builder.Services.AddControllersWithViews()
    .AddRazorOptions(options =>
    {
        options.ViewLocationFormats.Add("/Views/Admin/{1}/{0}.cshtml");
        options.ViewLocationFormats.Add("/Views/{1}/{0}.cshtml");
        options.ViewLocationFormats.Add("/Views/Shared/{0}.cshtml");
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

builder.Services.AddRazorPages();
builder.Services.AddControllers(); // Cần thiết cho ApiController

// ===================================
// 5. BUILD
// ===================================
var app = builder.Build();

// ===================================
// 6. MIDDLEWARE PIPELINE
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
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        if (ctx.File.Name.EndsWith(".webp", StringComparison.OrdinalIgnoreCase))
            ctx.Context.Response.Headers.Append("Cache-Control", "public, max-age=31536000, immutable");
    }
});

app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// ===================================
// 7. ROUTING
// ===================================

// Route riêng cho SePay Webhook (Ưu tiên đặt trước Default Route)
app.MapControllerRoute(
    name: "sepay-webhook",
    pattern: "ServicePayment/SePayWebhook",
    defaults: new { controller = "ServicePayment", action = "SePayWebhook" });

// Route mặc định cho MVC
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Hỗ trợ Attribute Routing cho các Controller có [Route("api/[controller]")]
app.MapControllers();

app.MapRazorPages();

// ===================================
// 8. DATABASE INIT & SEED
// ===================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        await context.Database.MigrateAsync();
        await DbInitializer.InitializeAsync(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Lỗi khi khởi tạo Database.");
    }
}

var avatarDir = Path.Combine(app.Environment.WebRootPath, "uploads", "avatars");
if (!Directory.Exists(avatarDir)) Directory.CreateDirectory(avatarDir);

app.Run();