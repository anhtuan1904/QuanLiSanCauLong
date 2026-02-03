using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiSanCauLong.Data;
using QuanLiSanCauLong.Models;
using QuanLiSanCauLong.ViewModels;

namespace QuanLiSanCauLong.Controllers
{
    [Authorize(Roles = "Admin")]
    // Thêm Route để khớp với đường dẫn /Admin/PriceSlot trong View
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
                .ThenBy(p => p.Court.CourtNumber) // Thêm sắp xếp theo sân cho đẹp
                .ThenBy(p => p.StartTime)
                .ToListAsync();

            ViewBag.Facilities = await _context.Facilities.ToListAsync();
            return View(priceSlots);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var courts = await _context.Courts
                .Include(c => c.Facility)
                .ToListAsync();

            ViewBag.CourtsList = courts;
            ViewBag.Facilities = await _context.Facilities.ToListAsync();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PriceSlot model)
        {
            // Loại bỏ các trường liên quan đến navigation để tránh lỗi IsValid giả
            ModelState.Remove("Court");
            ModelState.Remove("FacilityId");

            if (ModelState.IsValid)
            {
                var existingSlot = await _context.PriceSlots
                    .AnyAsync(p => p.CourtId == model.CourtId &&
                                   p.StartTime == model.StartTime &&
                                   p.EndTime == model.EndTime);

                if (existingSlot)
                {
                    ModelState.AddModelError("", "Khung giờ này đã tồn tại cho sân này!");
                    // Nạp lại dữ liệu cần thiết cho View
                    ViewBag.Facilities = await _context.Facilities.ToListAsync();
                    ViewBag.CourtsList = await _context.Courts.ToListAsync(); // Thêm dòng này
                    return View(model);
                }

                model.IsActive = true;
                _context.PriceSlots.Add(model);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Tạo khung giờ thành công!";
                return RedirectToAction(nameof(Index));
            }

            // NẾU LỖI: Cần nạp lại cả hai danh sách để giao diện hiển thị đúng
            ViewBag.Facilities = await _context.Facilities.ToListAsync();
            ViewBag.CourtsList = await _context.Courts.ToListAsync(); // Đảm bảo nạp lại danh sách sân
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var priceSlot = await _context.PriceSlots
                .Include(p => p.Court)
                    .ThenInclude(c => c.Facility)
                .FirstOrDefaultAsync(p => p.PriceSlotId == id);

            if (priceSlot == null)
                return NotFound();

            // Nạp danh sách cơ sở đang hoạt động
            ViewBag.Facilities = await _context.Facilities.Where(f => f.IsActive).ToListAsync();

            // Nạp danh sách sân thuộc cơ sở của PriceSlot này
            ViewBag.Courts = await _context.Courts
                .Where(c => c.FacilityId == priceSlot.Court.FacilityId)
                .ToListAsync();

            return View(priceSlot);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(PriceSlot model)
        {
            // Loại bỏ validation object Court để tránh lỗi ModelState false vô lý
            ModelState.Remove("Court");

            if (ModelState.IsValid)
            {
                var priceSlot = await _context.PriceSlots.FindAsync(model.PriceSlotId);
                if (priceSlot != null)
                {
                    // Kiểm tra trùng lặp (trừ chính nó)
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

                        await _context.SaveChangesAsync();
                        TempData["SuccessMessage"] = "Cập nhật khung giờ thành công!";
                        return RedirectToAction(nameof(Index));
                    }
                }
            }

            // Nếu có lỗi, nạp lại dữ liệu để người dùng sửa tiếp
            var currentCourt = await _context.Courts.Include(c => c.Facility).FirstOrDefaultAsync(c => c.CourtId == model.CourtId);
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
            if (request?.CourtIds == null || !request.CourtIds.Any() || request.TimeSlots == null || !request.TimeSlots.Any())
            {
                return Json(new { success = false, message = "Vui lòng chọn ít nhất một sân và một khung giờ!" });
            }

            try
            {
                int createdCount = 0;
                int skippedCount = 0;

                foreach (var courtId in request.CourtIds)
                {
                    foreach (var slot in request.TimeSlots)
                    {
                        // Kiểm tra trùng lặp trước khi thêm
                        bool exists = await _context.PriceSlots.AnyAsync(p =>
                            p.CourtId == courtId &&
                            p.StartTime == slot.StartTime &&
                            p.EndTime == slot.EndTime);

                        if (!exists)
                        {
                            _context.PriceSlots.Add(new PriceSlot
                            {
                                CourtId = courtId,
                                StartTime = slot.StartTime,
                                EndTime = slot.EndTime,
                                Price = slot.Price,
                                IsPeakHour = slot.IsPeakHour,
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
                {
                    await _context.SaveChangesAsync();
                }

                return Json(new
                {
                    success = true,
                    message = $"Thành công: {createdCount} khung giờ. Bỏ qua: {skippedCount} (đã tồn tại)."
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
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