using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiSanCauLong.Data;
using QuanLiSanCauLong.Models;

namespace QuanLiSanCauLong.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminFacilityController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminFacilityController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string search, string city, string status)
        {
            var query = _context.Facilities.AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(f => f.FacilityName.Contains(search) || f.Address.Contains(search));

            if (!string.IsNullOrEmpty(city))
                query = query.Where(f => f.City == city);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(f => f.Status == status);

            var facilities = await query.OrderBy(f => f.FacilityName).ToListAsync();
            ViewBag.Search = search;

            return View(facilities);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Facility model)
        {
            if (ModelState.IsValid)
            {
                model.CreatedAt = DateTime.Now;
                model.UpdatedAt = DateTime.Now;
                model.IsActive = true;

                _context.Facilities.Add(model);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Tạo cơ sở thành công!";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var facility = await _context.Facilities.FindAsync(id);
            if (facility == null)
                return NotFound();

            return View(facility);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Facility model)
        {
            if (ModelState.IsValid)
            {
                var facility = await _context.Facilities.FindAsync(model.FacilityId);
                if (facility != null)
                {
                    facility.FacilityName = model.FacilityName;
                    facility.Address = model.Address;
                    facility.District = model.District;
                    facility.City = model.City;
                    facility.Phone = model.Phone;
                    facility.Email = model.Email;
                    facility.Description = model.Description;
                    facility.OpenTime = model.OpenTime;
                    facility.CloseTime = model.CloseTime;
                    facility.Status = model.Status;
                    facility.ImageUrl = model.ImageUrl;
                    facility.UpdatedAt = DateTime.Now;

                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Cập nhật cơ sở thành công!";
                    return RedirectToAction(nameof(Index));
                }
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var facility = await _context.Facilities
                .Include(f => f.Courts)
                    .ThenInclude(c => c.PriceSlots)
                .Include(f => f.Courts)
                    .ThenInclude(c => c.Bookings)
                .FirstOrDefaultAsync(f => f.FacilityId == id);

            if (facility == null)
                return NotFound();

            return View(facility);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
                return Json(new { success = false, message = "ID không hợp lệ!" });

            var facility = await _context.Facilities
                .Include(f => f.Courts)
                .FirstOrDefaultAsync(f => f.FacilityId == id);

            if (facility == null)
                return Json(new { success = false, message = "Không tìm thấy cơ sở!" });

            if (facility.Courts.Any())
                return Json(new { success = false, message = "Không thể xóa cơ sở có sân!" });

            _context.Facilities.Remove(facility);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Xóa cơ sở thành công!" });
        }
    }
}