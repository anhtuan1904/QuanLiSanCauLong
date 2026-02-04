using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuanLiSanCauLong.Data;
using QuanLiSanCauLong.Models;
using QuanLiSanCauLong.ViewModels;
using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
// Required using statements
using System.ComponentModel.DataAnnotations;

namespace QuanLiSanCauLong.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // GET: Home/Index
        public async Task<IActionResult> Index()
        {
            var model = new HomeViewModel
            {
                FeaturedFacilities = await _context.Facilities
                    .Where(f => f.IsActive)
                    .OrderByDescending(f => f.CreatedAt)
                    .Take(6)
                    .Select(f => new FacilityCardViewModel
                    {
                        FacilityId = f.FacilityId,
                        FacilityName = f.FacilityName,
                        Address = f.Address,
                        City = f.City,
                        District = f.District,
                        ImageUrl = f.ImageUrl ?? "/images/default-facility.jpg",
                        TotalCourts = f.Courts.Count(c => c.Status == "Available"),
                        OpenTime = f.OpenTime,
                        CloseTime = f.CloseTime
                    })
                    .ToListAsync(),

                Stats = new SystemStatsViewModel
                {
                    TotalFacilities = await _context.Facilities.CountAsync(f => f.IsActive),
                    TotalCourts = await _context.Courts.CountAsync(c => c.Status == "Available"),
                    TotalCustomers = await _context.Users.CountAsync(u => u.Role == "Customer"),
                    TotalBookingsToday = await _context.Bookings
                        .CountAsync(b => b.BookingDate == DateTime.Today && b.Status != "Cancelled")
                }
            };

            return View(model);
        }

        // GET: Home/About
        public IActionResult About()
        {
            var model = new AboutViewModel
            {
                CompanyName = "Hệ thống Đặt sân Cầu lông",
                Description = "Chúng tôi cung cấp dịch vụ đặt sân cầu lông trực tuyến tiện lợi, nhanh chóng với hệ thống sân chất lượng cao tại nhiều địa điểm.",
                Mission = "Mang đến trải nghiệm chơi cầu lông tốt nhất cho mọi người với dịch vụ đặt sân hiện đại, tiện lợi.",
                Vision = "Trở thành nền tảng đặt sân thể thao hàng đầu Việt Nam.",
                Features = new List<string>
                {
                    "Đặt sân trực tuyến 24/7",
                    "Thanh toán linh hoạt",
                    "Hệ thống sân hiện đại",
                    "Giá cả cạnh tranh",
                    "Dịch vụ chuyên nghiệp",
                    "Ưu đãi thành viên"
                }
            };

            return View(model);
        }

        // GET: Home/Contact
        public IActionResult Contact()
        {
            var setting = _context.SystemSettings
                .Where(s => s.IsActive)
                .ToDictionary(s => s.SettingKey, s => s.SettingValue);

            var model = new ContactViewModel
            {
                SupportEmail = setting.ContainsKey("SupportEmail") ? setting["SupportEmail"] : "support@badminton.com",
                SupportPhone = setting.ContainsKey("SupportPhone") ? setting["SupportPhone"] : "1900xxxx",
                Address = "Địa chỉ trụ sở chính",
                WorkingHours = "8:00 - 22:00 (Tất cả các ngày)"
            };

            return View(model);
        }
        [HttpPost]
        public IActionResult SendMessage(string fullName, string email, string phone, string subject, string message)
        {
            // Chỗ này mày có thể xử lý lưu vào DB hoặc gửi Email thực tế
            TempData["SuccessMessage"] = "Cảm ơn bạn! Tin nhắn đã được gửi thành công.";
            return RedirectToAction("Contact");
        }

        // POST: Home/Contact
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Contact(ContactFormViewModel model)
        {
            if (ModelState.IsValid)
            {
                // TODO: Send email to support
                // await _emailService.SendContactFormAsync(model);

                TempData["SuccessMessage"] = "Cảm ơn bạn đã liên hệ! Chúng tôi sẽ phản hồi sớm nhất.";
                return RedirectToAction(nameof(Contact));
            }

            return View("Contact", new ContactViewModel
            {
                ContactForm = model
            });
        }

        // GET: Home/Privacy
        public IActionResult Privacy()
        {
            return View();
        }

        // GET: Home/Terms
        public IActionResult Terms()
        {
            return View();
        }

        // GET: Home/FAQ
        public IActionResult FAQ()
        {
            return View();
        }
        // GET: Home/AccessDenied
        public IActionResult AccessDenied()
        {
            return View();
        }
        public async Task<IActionResult> Stringing()
        {
            var services = await GetServicesByType("Restring");
            return View("~/Views/Service/Stringing.cshtml", services);
        }
        public IActionResult Recruitment()
        {
            return View();
        }

        // GET: Service/Training (Khóa học cầu lông)
        public async Task<IActionResult> Training()
        {
            var services = await GetServicesByType("Course");
            return View("~/Views/Service/Training.cshtml", services);
        }
        public async Task<IActionResult> Blog()
        {
            var allPosts = await _context.Posts
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var viewModel = new BlogViewModel
            {
                FeaturedPosts = allPosts.Where(p => p.IsFeatured).Take(2)
                    .Select(p => MapToPostItem(p)).ToList(),

                OtherPosts = allPosts.Where(p => !p.IsFeatured)
                    .Select(p => MapToPostItem(p)).ToList()
            };

            return View(viewModel);
        }

        private PostItemViewModel MapToPostItem(Post p)
        {
            return new PostItemViewModel
            {
                PostId = p.PostId,
                Title = p.Title,
                Summary = p.Summary,
                ImageUrl = p.ImageUrl ?? "/images/blog-default.jpg",
                Category = p.Category,
                Author = p.Author,
                PublishDate = p.CreatedAt.ToString("dd 'Tháng' MM, yyyy")
            };
        }

        // GET: Service/Tournament (Giải đấu nội bộ)
        public async Task<IActionResult> Tournament()
        {
            var services = await GetServicesByType("Tournament");
            return View("~/Views/Service/Tournament.cshtml", services);
        }


        // Hàm bổ trợ để lấy dữ liệu từ Database và ánh xạ sang ServiceViewModel
        private async Task<List<ServiceViewModel>> GetServicesByType(string type)
        {
            return await _context.Products
                .Include(p => p.Category)
                // Lọc dựa trên Metadata đã thêm vào Model
                .Where(p => p.IsActive && p.Metadata == type)
                .Select(p => new ServiceViewModel
                {
                    Type = p.Metadata,
                    ServiceName = p.ProductName,
                    Description = p.Description,
                    Price = p.Price,
                    ImageUrl = p.ImageUrl ?? "/images/default-service.jpg",
                    Duration = p.Unit ?? "Liên hệ",
                    // Logic hiển thị tính năng tùy theo loại dịch vụ
                    Features = p.Metadata == "Course"
                        ? new List<string> { "HLV chuyên nghiệp", "Luyện tập di chuyển", "Nắm vững luật chơi" }
                        : p.Metadata == "Restring"
                            ? new List<string> { "Căng máy điện tử", "Bảo hành 1 tháng", "Tặng quấn cán" }
                            : new List<string> { "Giải thưởng hấp dẫn", "Trọng tài chuyên nghiệp", "Giao lưu kết nối" }
                })
                .ToListAsync();
        }


    }
}
