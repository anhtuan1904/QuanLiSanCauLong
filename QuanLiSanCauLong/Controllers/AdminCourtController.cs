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
        // Thêm tham số lọc vào Index
        [HttpGet]
        public async Task<IActionResult> Index(int? facilityId, string courtType, string status)
        {
            var query = _context.Courts.Include(c => c.Facility).AsQueryable();

            if (facilityId.HasValue)
                query = query.Where(c => c.FacilityId == facilityId.Value);

            if (!string.IsNullOrEmpty(courtType))
                query = query.Where(c => c.CourtType == courtType);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(c => c.Status == status);

            var courts = await query
                .OrderBy(c => c.Facility.FacilityName)
                .ThenBy(c => c.CourtNumber)
                .ToListAsync();

            ViewBag.Facilities = await _context.Facilities.ToListAsync();

            // Lưu lại giá trị lọc để hiển thị trên UI
            ViewBag.SelectedFacility = facilityId;
            ViewBag.SelectedType = courtType;
            ViewBag.SelectedStatus = status;

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
        public async Task<IActionResult> Create(Court model, IFormFile? ImageFile) // Thêm ImageFile
        {
            // Bước 1: Xóa các lỗi xác thực tự động của các bảng liên quan
            ModelState.Remove("Facility");
            ModelState.Remove("PriceSlots");
            ModelState.Remove("Bookings");

            if (ModelState.IsValid)
            {
                try
                {
                    // Bước 2: Kiểm tra trùng tên sân trong cùng 1 chi nhánh
                    var isExist = await _context.Courts.AnyAsync(c =>
                        c.FacilityId == model.FacilityId && c.CourtNumber == model.CourtNumber);

                    if (isExist)
                    {
                        ModelState.AddModelError("CourtNumber", "Số sân này đã tồn tại ở cơ sở này rồi!");
                    }
                    else
                    {
                        // --- PHẦN CẬP NHẬT MỚI: XỬ LÝ UPLOAD ẢNH ---
                        if (ImageFile != null && ImageFile.Length > 0)
                        {
                            // Tạo thư mục nếu chưa có
                            string wwwRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                            string folderPath = Path.Combine(wwwRootPath, "images", "courts");
                            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

                            // Tạo tên file duy nhất
                            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile.FileName);
                            string fullPath = Path.Combine(folderPath, fileName);

                            // Lưu file vào thư mục wwwroot/images/courts
                            using (var fileStream = new FileStream(fullPath, FileMode.Create))
                            {
                                await ImageFile.CopyToAsync(fileStream);
                            }

                            // Lưu đường dẫn vào Model để lưu vào Database
                            model.ImagePath = "/images/courts/" + fileName;
                        }
                        // ------------------------------------------

                        model.CreatedAt = DateTime.Now;
                        _context.Courts.Add(model);
                        await _context.SaveChangesAsync();

                        TempData["SuccessMessage"] = "Thêm sân mới thành công!";
                        return RedirectToAction(nameof(Index), new { facilityId = model.FacilityId });
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi hệ thống: " + ex.Message);
                }
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
            ModelState.Remove("Facility");
            ModelState.Remove("PriceSlots");
            ModelState.Remove("Bookings");

            if (ModelState.IsValid)
            {
                var court = await _context.Courts.FindAsync(model.CourtId);
                if (court != null)
                {
                    // Kiểm tra trùng tên (trừ chính nó)
                    var isExist = await _context.Courts.AnyAsync(c =>
                        c.FacilityId == model.FacilityId &&
                        c.CourtNumber == model.CourtNumber &&
                        c.CourtId != model.CourtId);

                    if (isExist)
                    {
                        ModelState.AddModelError("CourtNumber", "Số sân này đã tồn tại ở cơ sở này!");
                    }
                    else
                    {
                        court.FacilityId = model.FacilityId;
                        court.CourtNumber = model.CourtNumber;
                        court.CourtType = model.CourtType;
                        court.Status = model.Status;

                        await _context.SaveChangesAsync();
                        TempData["SuccessMessage"] = "Cập nhật sân thành công!";
                        return RedirectToAction(nameof(Index));
                    }
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