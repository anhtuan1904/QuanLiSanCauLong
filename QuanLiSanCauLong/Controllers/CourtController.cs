using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiSanCauLong.Data;
using QuanLiSanCauLong.Models;

namespace QuanLiSanCauLong.Controllers
{
    public class CourtController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CourtController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ══════════════════════════════════════════════════════════
        //  DETAILS — Trang chi tiết sân (user xem trước khi đặt)
        // ══════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Details(int id, DateTime? bookingDate, string? startTime, string? endTime)
        {
            var court = await _context.Courts
                .Include(c => c.Facility)
                    .ThenInclude(f => f!.FacilityImages)
                .Include(c => c.CourtImages)
                .Include(c => c.PriceSlots)
                .FirstOrDefaultAsync(c => c.CourtId == id);

            if (court == null) return NotFound();

            // Truyền thông tin đặt sân để nút CTA dùng sau
            ViewBag.BookingDate = bookingDate?.ToString("yyyy-MM-dd") ?? DateTime.Today.ToString("yyyy-MM-dd");
            ViewBag.StartTime = startTime ?? "";
            ViewBag.EndTime = endTime ?? "";

            return View(court);
        }
    }
}
