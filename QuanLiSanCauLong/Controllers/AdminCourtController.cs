using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiSanCauLong.Data;
using QuanLiSanCauLong.Models;

namespace QuanLiSanCauLong.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminCourtController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminCourtController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int? facilityId)
        {
            var query = _context.Courts.Include(c => c.Facility).AsQueryable();

            if (facilityId.HasValue)
                query = query.Where(c => c.FacilityId == facilityId.Value);

            var courts = await query
                .OrderBy(c => c.Facility.FacilityName)
                .ThenBy(c => c.CourtNumber)
                .ToListAsync();

            ViewBag.Facilities = await _context.Facilities.ToListAsync();
            return View(courts);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Facilities = await _context.Facilities.Where(f => f.IsActive).ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Court model)
        {
            if (ModelState.IsValid)
            {
                model.CreatedAt = DateTime.Now;
                model.Status = "Available";

                _context.Courts.Add(model);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Tạo sân thành công!";
                return RedirectToAction(nameof(Index), new { facilityId = model.FacilityId });
            }

            ViewBag.Facilities = await _context.Facilities.Where(f => f.IsActive).ToListAsync();
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var court = await _context.Courts
                .Include(c => c.Facility)
                .Include(c => c.PriceSlots)
                .Include(c => c.Bookings)
                    .ThenInclude(b => b.User)
                .FirstOrDefaultAsync(c => c.CourtId == id);

            if (court == null)
                return NotFound();

            return View(court);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var court = await _context.Courts.FindAsync(id);
            if (court == null)
                return NotFound();

            ViewBag.Facilities = await _context.Facilities.Where(f => f.IsActive).ToListAsync();
            return View(court);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Court model)
        {
            if (ModelState.IsValid)
            {
                var court = await _context.Courts.FindAsync(model.CourtId);
                if (court != null)
                {
                    court.FacilityId = model.FacilityId;
                    court.CourtNumber = model.CourtNumber;
                    court.CourtType = model.CourtType;
                    court.Status = model.Status;

                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Cập nhật sân thành công!";
                    return RedirectToAction(nameof(Index), new { facilityId = court.FacilityId });
                }
            }

            ViewBag.Facilities = await _context.Facilities.Where(f => f.IsActive).ToListAsync();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
                return Json(new { success = false, message = "ID không hợp lệ!" });

            var court = await _context.Courts
                .Include(c => c.Bookings)
                .Include(c => c.PriceSlots)
                .FirstOrDefaultAsync(c => c.CourtId == id);

            if (court == null)
                return Json(new { success = false, message = "Không tìm thấy sân!" });

            if (court.Bookings.Any())
                return Json(new { success = false, message = "Không thể xóa sân đã có lịch đặt!" });

            _context.Courts.Remove(court);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Xóa sân thành công!" });
        }

        [HttpGet]
        public async Task<IActionResult> GetByFacility(int facilityId)
        {
            var courts = await _context.Courts
                .Where(c => c.FacilityId == facilityId)
                .Select(c => new { c.CourtId, c.CourtNumber, c.CourtType })
                .ToListAsync();

            return Json(courts);
        }
    }
}