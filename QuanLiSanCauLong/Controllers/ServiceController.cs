using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiSanCauLong.Data;
using QuanLiSanCauLong.Models;

namespace QuanLiSanCauLong.Controllers
{
    // ============================================================
    // ServiceController.cs  —  Public-facing (không phải Admin)
    // ============================================================
    public class ServiceController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ServiceController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ── AUTO-SYNC: Tự động chuyển Status="Full" khi lớp đầy ─────
        // Gọi trước khi load Training page và sau mỗi lần Enroll
        private async Task AutoSyncFullStatusAsync()
        {
            var courses = await _context.Courses
                .Where(c => c.MaxStudents.HasValue && c.Status != "Inactive")
                .ToListAsync();

            bool changed = false;
            foreach (var c in courses)
            {
                bool shouldBeFull = c.MaxStudents.HasValue
                    && c.CurrentStudents >= c.MaxStudents.Value;

                if (shouldBeFull && c.Status != "Full")
                {
                    c.Status = "Full";
                    c.UpdatedAt = DateTime.Now;
                    changed = true;
                }
                else if (!shouldBeFull && c.Status == "Full")
                {
                    // Admin giảm MaxStudents hoặc huỷ đăng ký → mở lại
                    c.Status = "Active";
                    c.UpdatedAt = DateTime.Now;
                    changed = true;
                }
            }
            if (changed) await _context.SaveChangesAsync();
        }

        // ── /Service/Training ────────────────────────────────────────
        public async Task<IActionResult> Training()
        {
            await AutoSyncFullStatusAsync();

            var courses = await _context.Courses
                .Include(c => c.CourseImages)
                .Where(c => c.Status != "Inactive")
                .OrderBy(c => c.IsFull ? 1 : 0)       // còn chỗ lên trước
                .ThenBy(c => c.IsPopular ? 0 : 1)      // popular đầu
                .ThenBy(c => c.DisplayOrder)
                .ToListAsync();

            return View(courses);
        }

        // ── /Service/Stringing ───────────────────────────────────────
        public async Task<IActionResult> Stringing()
        {
            var stringings = await _context.StringingServices
                .Include(s => s.StringingImages)
                .Where(s => s.Status == "Active")
                .OrderBy(s => s.IsPopular ? 0 : 1)
                .ThenBy(s => s.DisplayOrder)
                .ToListAsync();

            return View(stringings);
        }

        // ── /Service/Tournament ──────────────────────────────────────
        public async Task<IActionResult> Tournament()
        {
            var tournaments = await _context.Tournaments
                .Include(t => t.TournamentImages)
                .Where(t => t.Status != "Cancelled")
                .OrderBy(t => t.Status == "Ongoing" ? 0 :
                              t.Status == "Upcoming" ? 1 : 2)
                .ThenBy(t => t.StartDate)
                .ToListAsync();

            return View(tournaments);
        }

        // ── /Service/Index (tổng hợp) ────────────────────────────────
        public async Task<IActionResult> Index()
        {
            await AutoSyncFullStatusAsync();
            ViewBag.CourseCount = await _context.Courses.CountAsync(c => c.Status == "Active");
            ViewBag.StringingCount = await _context.StringingServices.CountAsync(s => s.Status == "Active");
            ViewBag.TournamentCount = await _context.Tournaments.CountAsync(t => t.Status != "Cancelled");
            return View();
        }

        // ── /Service/Enroll  [POST] ───────────────────────────────────
        // Xử lý đăng ký khóa học — tăng CurrentStudents và tự động
        // chuyển Status="Full" nếu lớp vừa đầy sau đăng ký này.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Enroll(
            int courseId,
            string customerName,
            string phone,
            string? email)
        {
            var course = await _context.Courses.FindAsync(courseId);

            if (course == null)
                return Json(new { success = false, message = "Không tìm thấy khóa học." });

            if (course.Status == "Inactive")
                return Json(new { success = false, message = "Khóa học hiện chưa mở đăng ký." });

            if (course.Status == "Full" || course.IsFull)
                return Json(new { success = false, message = "Khóa học đã đầy, không thể đăng ký." });

            if (string.IsNullOrWhiteSpace(customerName) || string.IsNullOrWhiteSpace(phone))
                return Json(new { success = false, message = "Vui lòng điền đầy đủ họ tên và số điện thoại." });

            // Tăng học viên
            course.CurrentStudents++;

            // Tự động chuyển Full nếu đã đạt sĩ số
            if (course.MaxStudents.HasValue && course.CurrentStudents >= course.MaxStudents.Value)
                course.Status = "Full";

            course.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            // TODO: Lưu vào bảng CourseEnrollment nếu có
            // TODO: Gửi email/SMS xác nhận cho học viên

            bool isFull = course.Status == "Full";
            int slots = (course.MaxStudents ?? 0) - course.CurrentStudents;

            return Json(new
            {
                success = true,
                message = isFull
                    ? "Đăng ký thành công! Bạn là học viên cuối cùng — lớp đã đầy."
                    : $"Đăng ký thành công! Còn {Math.Max(0, slots)} chỗ trống.",
                newStatus = course.Status,
                currentStudents = course.CurrentStudents,
                isFull
            });
        }
    }
}
