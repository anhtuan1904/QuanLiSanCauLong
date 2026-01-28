using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiSanCauLong.Data;
using QuanLiSanCauLong.Models;
using QuanLiSanCauLong.ViewModels;

namespace QuanLiSanCauLong.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminPriceSlotController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminPriceSlotController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int? facilityId)
        {
            var query = _context.PriceSlots
                .Include(p => p.Court)
                    .ThenInclude(c => c.Facility)
                .AsQueryable();

            if (facilityId.HasValue)
                query = query.Where(p => p.Court.FacilityId == facilityId.Value);

            var priceSlots = await query
                .OrderBy(p => p.Court.Facility.FacilityName)
                .ThenBy(p => p.StartTime)
                .ToListAsync();

            ViewBag.Facilities = await _context.Facilities.ToListAsync();
            return View(priceSlots);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Facilities = await _context.Facilities.Where(f => f.IsActive).ToListAsync();
            ViewBag.Courts = new List<Court>();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PriceSlot model)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra trùng lặp khung giờ
                var existingSlot = await _context.PriceSlots
                    .AnyAsync(p => p.CourtId == model.CourtId &&
                                   p.StartTime == model.StartTime &&
                                   p.EndTime == model.EndTime);

                if (existingSlot)
                {
                    ModelState.AddModelError("", "Khung giờ này đã tồn tại cho sân này!");
                    var court = await _context.Courts.FindAsync(model.CourtId);
                    ViewBag.Facilities = await _context.Facilities.Where(f => f.IsActive).ToListAsync();
                    ViewBag.Courts = await _context.Courts
                        .Where(c => c.FacilityId == (court != null ? court.FacilityId : 0))
                        .ToListAsync();
                    return View(model);
                }

                model.IsActive = true;
                _context.PriceSlots.Add(model);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Tạo khung giờ thành công!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Facilities = await _context.Facilities.Where(f => f.IsActive).ToListAsync();
            ViewBag.Courts = new List<Court>();
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var priceSlot = await _context.PriceSlots
                .Include(p => p.Court)
                .FirstOrDefaultAsync(p => p.PriceSlotId == id);

            if (priceSlot == null)
                return NotFound();

            ViewBag.Facilities = await _context.Facilities.Where(f => f.IsActive).ToListAsync();
            ViewBag.Courts = await _context.Courts
                .Where(c => c.FacilityId == priceSlot.Court.FacilityId)
                .ToListAsync();

            return View(priceSlot);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(PriceSlot model)
        {
            if (ModelState.IsValid)
            {
                var priceSlot = await _context.PriceSlots.FindAsync(model.PriceSlotId);
                if (priceSlot != null)
                {
                    priceSlot.CourtId = model.CourtId;
                    priceSlot.StartTime = model.StartTime;
                    priceSlot.EndTime = model.EndTime;
                    priceSlot.Price = model.Price;
                    priceSlot.IsPeakHour = model.IsPeakHour;
                    priceSlot.IsActive = model.IsActive;

                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Cập nhật khung giờ thành công!";
                    return RedirectToAction(nameof(Index));
                }
            }

            var court = await _context.Courts.FindAsync(model.CourtId);
            ViewBag.Facilities = await _context.Facilities.Where(f => f.IsActive).ToListAsync();
            ViewBag.Courts = await _context.Courts
                .Where(c => c.FacilityId == (court != null ? court.FacilityId : 0))
                .ToListAsync();

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
                return Json(new { success = false, message = "ID không hợp lệ!" });

            var priceSlot = await _context.PriceSlots.FindAsync(id);
            if (priceSlot == null)
                return Json(new { success = false, message = "Không tìm thấy khung giờ!" });

            _context.PriceSlots.Remove(priceSlot);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Xóa khung giờ thành công!" });
        }

        [HttpGet]
        public async Task<IActionResult> BulkCreate()
        {
            ViewBag.Facilities = await _context.Facilities.Where(f => f.IsActive).ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkCreate([FromBody] BulkPriceSlotRequest request)
        {
            if (request.CourtIds == null || !request.CourtIds.Any() ||
                request.TimeSlots == null || !request.TimeSlots.Any())
            {
                return Json(new { success = false, message = "Dữ liệu không hợp lệ!" });
            }

            try
            {
                int created = 0;
                foreach (var courtId in request.CourtIds)
                {
                    foreach (var slot in request.TimeSlots)
                    {
                        // Kiểm tra trùng lặp
                        var exists = await _context.PriceSlots
                            .AnyAsync(p => p.CourtId == courtId &&
                                           p.StartTime == slot.StartTime &&
                                           p.EndTime == slot.EndTime);

                        if (!exists)
                        {
                            var priceSlot = new PriceSlot
                            {
                                CourtId = courtId,
                                StartTime = slot.StartTime,
                                EndTime = slot.EndTime,
                                Price = slot.Price,
                                IsPeakHour = slot.IsPeakHour,
                                IsActive = true
                            };

                            _context.PriceSlots.Add(priceSlot);
                            created++;
                        }
                    }
                }

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"Đã tạo {created} khung giờ thành công!"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }
    }
}