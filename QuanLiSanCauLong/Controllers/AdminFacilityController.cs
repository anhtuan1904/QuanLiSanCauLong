using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiSanCauLong.Data;
using QuanLiSanCauLong.Models;
using QuanLiSanCauLong.ViewModels;

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
        public async Task<IActionResult> Index(string search, string city)
        {
            var query = _context.Facilities.Where(f => f.IsActive).AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(f => f.FacilityName.Contains(search) || f.Address.Contains(search));

            if (!string.IsNullOrEmpty(city))
                query = query.Where(f => f.City == city);

            var facilities = await query.OrderByDescending(f => f.CreatedAt).ToListAsync();

            // Lấy danh sách thành phố để lọc (ViewBag)
            ViewBag.Cities = await _context.Facilities.Select(f => f.City).Distinct().ToListAsync();

            return View(facilities);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Facility model, IFormFile? ImageFile)
        {
            // 1. Loại bỏ các trường không nhập từ form để tránh lỗi Validation ảo
            ModelState.Remove("ImageUrl");
            ModelState.Remove("Courts");
            ModelState.Remove("PriceSlots");
            ModelState.Remove("Inventories");

            if (ModelState.IsValid)
            {
                try
                {
                    if (ImageFile != null && ImageFile.Length > 0)
                    {
                        // Xử lý đường dẫn thư mục
                        string wwwRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                        string folderPath = Path.Combine(wwwRootPath, "images", "facilities");

                        // Tạo thư mục nếu chưa tồn tại
                        if (!Directory.Exists(folderPath))
                        {
                            Directory.CreateDirectory(folderPath);
                        }

                        // Tạo tên file duy nhất
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile.FileName);
                        string filePath = Path.Combine(folderPath, fileName);

                        // Lưu file vào thư mục vật lý
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await ImageFile.CopyToAsync(stream);
                        }

                        // Lưu đường dẫn vào database
                        model.ImageUrl = "/images/facilities/" + fileName;
                    }

                    model.CreatedAt = DateTime.Now;
                    model.UpdatedAt = DateTime.Now;

                    _context.Facilities.Add(model);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Thêm mới cơ sở thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Có lỗi xảy ra khi lưu dữ liệu: " + ex.Message);
                }
            }

            // Nếu code chạy đến đây nghĩa là có lỗi (Validation hoặc Exception)
            // Trả về View cùng với model để người dùng không phải nhập lại các ô khác
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
        public async Task<IActionResult> Edit(Facility model, IFormFile? ImageFile)
        {
            ModelState.Remove("ImageUrl");
            ModelState.Remove("Courts");
            ModelState.Remove("PriceSlots");
            ModelState.Remove("Inventories");

            if (ModelState.IsValid)
            {
                try
                {
                    var facilityInDb = await _context.Facilities.AsNoTracking()
                        .FirstOrDefaultAsync(f => f.FacilityId == model.FacilityId);

                    if (facilityInDb == null) return NotFound();

                    if (ImageFile != null && ImageFile.Length > 0)
                    {
                        // Xử lý ảnh mới
                        string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "facilities");
                        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile.FileName);
                        string filePath = Path.Combine(folderPath, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await ImageFile.CopyToAsync(stream);
                        }

                        model.ImageUrl = "/images/facilities/" + fileName;

                        // (Tùy chọn) Xóa ảnh cũ trên server để tiết kiệm bộ nhớ
                        if (!string.IsNullOrEmpty(facilityInDb.ImageUrl))
                        {
                            var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", facilityInDb.ImageUrl.TrimStart('/'));
                            if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                        }
                    }
                    else
                    {
                        // Nếu không chọn ảnh mới, giữ lại đường dẫn ảnh cũ
                        model.ImageUrl = facilityInDb.ImageUrl;
                    }

                    model.UpdatedAt = DateTime.Now;
                    _context.Update(model);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Cập nhật thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi cập nhật: " + ex.Message);
                }
            }
            return View(model);
        }
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var facility = await _context.Facilities.FindAsync(id);
            if (facility == null) return NotFound();

            var model = new FacilityDetailsViewModel
            {
                FacilityId = facility.FacilityId,
                FacilityName = facility.FacilityName,
                // ... các trường khác ...

                // CÁCH SỬA: Tạo một List mới chứa 1 phần tử ảnh từ DB
                ImageUrls = new List<string> { facility.ImageUrl ?? "/images/default-facility.jpg" }
            };

            return View(model);
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