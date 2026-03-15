using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiSanCauLong.Data;
using QuanLiSanCauLong.Models;
using QuanLiSanCauLong.ViewModels;

namespace QuanLiSanCauLong.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("Admin/PriceSlot/[action]/{id?}")]
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
                .ThenBy(p => p.Court.CourtNumber)
                .ThenBy(p => p.StartTime)
                .ToListAsync();

            ViewBag.Facilities = await _context.Facilities.ToListAsync();
            return View(priceSlots);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.CourtsList = await _context.Courts.Include(c => c.Facility).ToListAsync();
            ViewBag.Facilities = await _context.Facilities.ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PriceSlot model)
        {
            ModelState.Remove("Court");
            ModelState.Remove("Facility");
            ModelState.Remove("FacilityId");
            ModelState.Remove("CourtType");

            if (ModelState.IsValid)
            {
                var existingSlot = await _context.PriceSlots
                    .AnyAsync(p => p.CourtId == model.CourtId &&
                                   p.StartTime == model.StartTime &&
                                   p.EndTime == model.EndTime);

                if (existingSlot)
                {
                    ModelState.AddModelError("", "Khung giờ này đã tồn tại cho sân này!");
                }
                else
                {
                    model.IsActive = true;
                    _context.PriceSlots.Add(model);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Tạo khung giờ thành công!";
                    return RedirectToAction(nameof(Index));
                }
            }

            ViewBag.Facilities = await _context.Facilities.ToListAsync();
            ViewBag.CourtsList = await _context.Courts.Include(c => c.Facility).ToListAsync();
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var priceSlot = await _context.PriceSlots
                .Include(p => p.Court).ThenInclude(c => c.Facility)
                .FirstOrDefaultAsync(p => p.PriceSlotId == id);

            if (priceSlot == null) return NotFound();

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
            ModelState.Remove("Court");
            ModelState.Remove("Facility");
            ModelState.Remove("CourtType");

            if (ModelState.IsValid)
            {
                var priceSlot = await _context.PriceSlots.FindAsync(model.PriceSlotId);
                if (priceSlot != null)
                {
                    var isDuplicate = await _context.PriceSlots.AnyAsync(p =>
                        p.PriceSlotId != model.PriceSlotId &&
                        p.CourtId == model.CourtId &&
                        p.StartTime == model.StartTime &&
                        p.EndTime == model.EndTime);

                    if (isDuplicate)
                    {
                        ModelState.AddModelError("", "Khung giờ này đã tồn tại cho sân này!");
                    }
                    else
                    {
                        priceSlot.CourtId = model.CourtId;
                        priceSlot.StartTime = model.StartTime;
                        priceSlot.EndTime = model.EndTime;
                        priceSlot.Price = model.Price;
                        priceSlot.IsPeakHour = model.IsPeakHour;
                        priceSlot.IsActive = model.IsActive;
                        priceSlot.SlotName = model.SlotName;
                        priceSlot.CustomerType = model.CustomerType;
                        priceSlot.Surcharge = model.Surcharge;
                        priceSlot.SurchargeNote = model.SurchargeNote;
                        priceSlot.RoundingMinutes = model.RoundingMinutes;
                        priceSlot.AppliedDays = model.AppliedDays;
                        priceSlot.MemberDiscount = model.MemberDiscount;
                        priceSlot.MinDurationMinutes = model.MinDurationMinutes;
                        priceSlot.BufferMinutes = model.BufferMinutes;
                        priceSlot.Priority = model.Priority;

                        await _context.SaveChangesAsync();
                        TempData["SuccessMessage"] = "Cập nhật khung giờ thành công!";
                        return RedirectToAction(nameof(Index));
                    }
                }
            }

            var currentCourt = await _context.Courts
                .Include(c => c.Facility)
                .FirstOrDefaultAsync(c => c.CourtId == model.CourtId);
            ViewBag.Facilities = await _context.Facilities.ToListAsync();
            ViewBag.Courts = await _context.Courts
                .Where(c => c.FacilityId == (currentCourt != null ? currentCourt.FacilityId : 0))
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
        public async Task<IActionResult> BulkCreate([FromBody] BulkPriceSlotRequest request)
        {
            if (request?.CourtIds == null || !request.CourtIds.Any() ||
                request.TimeSlots == null || !request.TimeSlots.Any())
            {
                return Json(new { success = false, message = "Vui lòng chọn ít nhất một sân và một khung giờ!" });
            }

            try
            {
                int createdCount = 0;
                int skippedCount = 0;

                string? appliedDays = (request.Days != null && request.Days.Any())
                    ? string.Join(",", request.Days)
                    : null;

                foreach (var courtId in request.CourtIds)
                {
                    var court = await _context.Courts.FindAsync(courtId);
                    if (court == null) { skippedCount++; continue; }

                    foreach (var slot in request.TimeSlots)
                    {
                        // ✅ FIX: dùng StartTimeParsed / EndTimeParsed (TimeSpan)
                        // thay vì StartTime / EndTime (string)
                        var startTime = slot.StartTimeParsed;
                        var endTime = slot.EndTimeParsed;

                        // Bỏ qua nếu parse thất bại (cả 2 đều là Zero)
                        if (startTime == TimeSpan.Zero && endTime == TimeSpan.Zero)
                        {
                            skippedCount++;
                            continue;
                        }

                        bool exists = await _context.PriceSlots.AnyAsync(p =>
                            p.CourtId == courtId &&
                            p.StartTime == startTime &&
                            p.EndTime == endTime);

                        if (!exists)
                        {
                            _context.PriceSlots.Add(new PriceSlot
                            {
                                CourtId = courtId,
                                FacilityId = court.FacilityId,
                                CourtType = court.CourtType,
                                StartTime = startTime,   // ✅ TimeSpan
                                EndTime = endTime,     // ✅ TimeSpan
                                Price = slot.Price,
                                Surcharge = slot.Surcharge,
                                SlotName = slot.SlotName,
                                CustomerType = slot.CustType,
                                SurchargeNote = slot.SurNote,
                                IsPeakHour = slot.IsPeakHour,
                                AppliedDays = appliedDays,
                                IsActive = true
                            });
                            createdCount++;
                        }
                        else
                        {
                            skippedCount++;
                        }
                    }
                }

                if (createdCount > 0)
                    await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"Thành công: {createdCount} khung giờ. Bỏ qua: {skippedCount} (đã tồn tại)."
                });
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException?.InnerException?.Message
                          ?? ex.InnerException?.Message
                          ?? ex.Message;
                return Json(new { success = false, message = "Lỗi hệ thống: " + detail });
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetCourtsByFacility(int facilityId)
        {
            var courts = await _context.Courts
                .Where(c => c.FacilityId == facilityId)
                .Select(c => new {
                    courtId = c.CourtId,
                    courtNumber = c.CourtNumber,
                    courtType = c.CourtType
                })
                .ToListAsync();
            return Json(courts);
        }
    }
}
