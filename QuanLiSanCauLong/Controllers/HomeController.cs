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
            var faqs = new List<FaqItem>
            {
                new FaqItem
                {
                    Question = "Làm thế nào để đặt sân?",
                    Answer = "Bạn cần đăng ký tài khoản, sau đó chọn cơ sở, chọn sân và khung giờ phù hợp, cuối cùng xác nhận đặt sân."
                },
                new FaqItem
                {
                    Question = "Tôi có thể hủy đặt sân không?",
                    Answer = "Có, bạn có thể hủy đặt sân trước giờ chơi ít nhất 2 giờ."
                },
                new FaqItem
                {
                    Question = "Hình thức thanh toán nào được chấp nhận?",
                    Answer = "Chúng tôi chấp nhận thanh toán tiền mặt, chuyển khoản, thẻ ATM, Momo, ZaloPay."
                },
                new FaqItem
                {
                    Question = "Có được đặt sân trước bao lâu?",
                    Answer = "Bạn có thể đặt sân trước tối đa 30 ngày."
                },
                new FaqItem
                {
                    Question = "Tôi có thể mua đồ ăn, nước uống không?",
                    Answer = "Có, chúng tôi cung cấp dịch vụ đồ ăn, nước uống và các sản phẩm thể thao."
                }
            };

            return View(faqs);
        }

        // GET: Home/AccessDenied
        public IActionResult AccessDenied()
        {
            return View();
        }

        // GET: Home/Error
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }

    // ===================================
    // VIEW MODELS FOR HOME
    // ===================================

    public class HomeViewModel
    {
        public List<FacilityCardViewModel> FeaturedFacilities { get; set; }
        public SystemStatsViewModel Stats { get; set; }
    }

    public class FacilityCardViewModel
    {
        public int FacilityId { get; set; }
        public string FacilityName { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string District { get; set; }
        public string ImageUrl { get; set; }
        public int TotalCourts { get; set; }
        public TimeSpan? OpenTime { get; set; }
        public TimeSpan? CloseTime { get; set; }
    }

    public class SystemStatsViewModel
    {
        public int TotalFacilities { get; set; }
        public int TotalCourts { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalBookingsToday { get; set; }
    }

    public class AboutViewModel
    {
        public string CompanyName { get; set; }
        public string Description { get; set; }
        public string Mission { get; set; }
        public string Vision { get; set; }
        public List<string> Features { get; set; }
    }

    public class ContactViewModel
    {
        public string SupportEmail { get; set; }
        public string SupportPhone { get; set; }
        public string Address { get; set; }
        public string WorkingHours { get; set; }
        public ContactFormViewModel ContactForm { get; set; }
    }

    public class ContactFormViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; }

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tiêu đề")]
        public string Subject { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập nội dung")]
        public string Message { get; set; }
    }

    public class FaqItem
    {
        public string Question { get; set; }
        public string Answer { get; set; }
    }

    public class ErrorViewModel
    {
        public string RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
