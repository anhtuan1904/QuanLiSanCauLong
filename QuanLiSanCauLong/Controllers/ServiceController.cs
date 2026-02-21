using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiSanCauLong.Data;
using QuanLiSanCauLong.Models;

namespace QuanLiSanCauLong.Controllers
{
    // ============================================================
    // ServiceController.cs  (public-facing, KHÔNG phải Admin)
    // Thay thế hoàn toàn controller cũ dùng model Service
    // ============================================================
    public class ServiceController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ServiceController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ── /Service/Training  →  Views/Service/Training.cshtml ──
        public async Task<IActionResult> Training()
        {
            var courses = await _context.Courses
                .Where(c => c.Status != "Inactive")
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();

            return View(courses);   // @model IEnumerable<Course>
        }

        // ── /Service/Stringing  →  Views/Service/Stringing.cshtml ──
        public async Task<IActionResult> Stringing()
        {
            var stringings = await _context.StringingServices
                .Where(s => s.Status == "Active")
                .OrderBy(s => s.DisplayOrder)
                .ToListAsync();

            return View(stringings);   // @model IEnumerable<StringingService>
        }

        // ── /Service/Tournament  →  Views/Service/Tournament.cshtml ──
        public async Task<IActionResult> Tournament()
        {
            var tournaments = await _context.Tournaments
                .Where(t => t.Status != "Cancelled")
                .OrderBy(t => t.DisplayOrder)
                .ToListAsync();

            return View(tournaments);   // @model IEnumerable<Tournament>
        }

        // ── /Service/Index  →  trang tổng hợp dịch vụ (tuỳ chọn) ──
        public async Task<IActionResult> Index()
        {
            ViewBag.CourseCount = await _context.Courses.CountAsync(c => c.Status == "Active");
            ViewBag.StringingCount = await _context.StringingServices.CountAsync(s => s.Status == "Active");
            ViewBag.TournamentCount = await _context.Tournaments.CountAsync(t => t.Status != "Cancelled");
            return View();
        }
    }
}
