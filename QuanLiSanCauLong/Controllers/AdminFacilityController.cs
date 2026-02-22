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
        private readonly IWebHostEnvironment _environment;

        public AdminFacilityController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // ──────────────────────────────────────────────────────────────
        // INDEX
        // ──────────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Index(string search, string city)
        {
            var query = _context.Facilities
                .Include(f => f.FacilityImages)
                .Include(f => f.Courts)
                .Where(f => f.IsActive)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(f => f.FacilityName.Contains(search) || f.Address.Contains(search));

            if (!string.IsNullOrEmpty(city))
                query = query.Where(f => f.City == city);

            var facilities = await query.OrderByDescending(f => f.CreatedAt).ToListAsync();
            ViewBag.Cities = await _context.Facilities.Select(f => f.City).Distinct().ToListAsync();

            return View(facilities);
        }

        // ──────────────────────────────────────────────────────────────
        // CREATE GET
        // ──────────────────────────────────────────────────────────────
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // ──────────────────────────────────────────────────────────────
        // CREATE POST
        // ──────────────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            Facility model,
            List<IFormFile> FacilityImages,
            int PrimaryImageIndex = 0,
            List<string>? Amenities = null)
        {
            // Loại bỏ kiểm tra các navigation property & trường không liên quan
            ModelState.Remove("ImageUrl");
            ModelState.Remove("Status");
            ModelState.Remove("Courts");
            ModelState.Remove("PriceSlots");
            ModelState.Remove("Inventories");
            ModelState.Remove("FacilityImages");
            ModelState.Remove("Facility");
            ModelState.Remove("Amenities");

            if (ModelState.IsValid)
            {
                try
                {
                    model.Status = "Active";
                    model.CreatedAt = DateTime.Now;
                    model.UpdatedAt = DateTime.Now;
                    model.IsActive = true;

                    // Lưu tiện ích đi kèm dạng comma-separated
                    model.Amenities = (Amenities != null && Amenities.Any())
                        ? string.Join(",", Amenities)
                        : null;

                    _context.Facilities.Add(model);
                    await _context.SaveChangesAsync();

                    // Xử lý upload ảnh
                    if (FacilityImages != null && FacilityImages.Count > 0)
                    {
                        string folder = Path.Combine(_environment.WebRootPath, "images", "facilities");
                        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                        for (int i = 0; i < FacilityImages.Count; i++)
                        {
                            var file = FacilityImages[i];
                            if (file.Length == 0) continue;

                            string fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                            string filePath = Path.Combine(folder, fileName);

                            using (var stream = new FileStream(filePath, FileMode.Create))
                                await file.CopyToAsync(stream);

                            var facilityImage = new FacilityImage
                            {
                                FacilityId = model.FacilityId,
                                ImagePath = $"/images/facilities/{fileName}",
                                IsPrimary = (i == PrimaryImageIndex),
                                DisplayOrder = i,
                                UploadedAt = DateTime.Now
                            };
                            _context.FacilityImages.Add(facilityImage);

                            if (i == PrimaryImageIndex)
                            {
                                model.ImageUrl = facilityImage.ImagePath;
                                _context.Entry(model).State = EntityState.Modified;
                            }
                        }
                        await _context.SaveChangesAsync();
                    }

                    return Json(new { success = true, message = "Thêm mới cơ sở thành công!" });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "Lỗi hệ thống: " + (ex.InnerException?.Message ?? ex.Message) });
                }
            }

            var errors = ModelState
                .Where(x => x.Value!.Errors.Count > 0)
                .SelectMany(x => x.Value!.Errors.Select(e => e.ErrorMessage));

            return Json(new { success = false, message = "Dữ liệu không hợp lệ!", errors });
        }

        // ──────────────────────────────────────────────────────────────
        // EDIT GET
        // ──────────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var facility = await _context.Facilities
                .Include(f => f.FacilityImages)
                .FirstOrDefaultAsync(f => f.FacilityId == id);

            if (facility == null) return NotFound();

            // ✅ FIX: Set ViewBag.FacilityImages để Edit.cshtml có thể render ảnh hiện tại
            ViewBag.FacilityImages = facility.FacilityImages
                .OrderBy(i => i.DisplayOrder)
                .ToList();

            return View(facility);
        }

        // ──────────────────────────────────────────────────────────────
        // EDIT POST
        // ──────────────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            Facility model,
            List<IFormFile>? FacilityImages,
            string? DeletedImageIds,
            int? NewPrimaryImageId,
            List<string>? Amenities = null)
        {
            ModelState.Remove("ImageUrl");
            ModelState.Remove("Courts");
            ModelState.Remove("PriceSlots");
            ModelState.Remove("Inventories");
            ModelState.Remove("FacilityImages");
            ModelState.Remove("Amenities");

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return Json(new { success = false, message = "Dữ liệu không hợp lệ!", errors });
            }

            var facility = await _context.Facilities
                .Include(f => f.FacilityImages)
                .FirstOrDefaultAsync(f => f.FacilityId == model.FacilityId);

            if (facility == null)
                return Json(new { success = false, message = "Không tìm thấy cơ sở!" });

            try
            {
                // Cập nhật thông tin cơ bản
                facility.FacilityName = model.FacilityName;
                facility.Address = model.Address;
                facility.Phone = model.Phone;
                facility.City = model.City;
                facility.District = model.District;
                facility.OpenTime = model.OpenTime;
                facility.CloseTime = model.CloseTime;
                facility.Description = model.Description;
                facility.Status = model.Status;
                facility.UpdatedAt = DateTime.Now;

                // ✅ FIX: Lưu Latitude & Longitude
                facility.Latitude = model.Latitude;
                facility.Longitude = model.Longitude;

                // ✅ FIX: Lưu Amenities
                facility.Amenities = (Amenities != null && Amenities.Any())
                    ? string.Join(",", Amenities)
                    : null;

                // 1. Xóa ảnh được đánh dấu xóa
                if (!string.IsNullOrEmpty(DeletedImageIds))
                {
                    var idsToDelete = DeletedImageIds
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(id => int.Parse(id.Trim()))
                        .ToList();

                    var imagesToDelete = facility.FacilityImages
                        .Where(img => idsToDelete.Contains(img.ImageId))
                        .ToList();

                    foreach (var img in imagesToDelete)
                    {
                        string physicalPath = Path.Combine(_environment.WebRootPath, img.ImagePath.TrimStart('/'));
                        if (System.IO.File.Exists(physicalPath))
                            System.IO.File.Delete(physicalPath);
                        _context.FacilityImages.Remove(img);
                    }
                }

                // 2. Upload ảnh mới
                if (FacilityImages != null && FacilityImages.Count > 0)
                {
                    string folder = Path.Combine(_environment.WebRootPath, "images", "facilities");
                    if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                    int currentMaxOrder = facility.FacilityImages.Any()
                        ? facility.FacilityImages.Max(img => img.DisplayOrder)
                        : -1;

                    foreach (var file in FacilityImages)
                    {
                        if (file.Length == 0) continue;

                        string fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                        string filePath = Path.Combine(folder, fileName);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                            await file.CopyToAsync(stream);

                        _context.FacilityImages.Add(new FacilityImage
                        {
                            FacilityId = facility.FacilityId,
                            ImagePath = $"/images/facilities/{fileName}",
                            IsPrimary = false,
                            DisplayOrder = ++currentMaxOrder,
                            UploadedAt = DateTime.Now
                        });
                    }
                }

                await _context.SaveChangesAsync();

                // 3. Cập nhật ảnh chính
                if (NewPrimaryImageId.HasValue && NewPrimaryImageId.Value > 0)
                {
                    // Reload lại danh sách ảnh sau khi đã lưu ảnh mới
                    await _context.Entry(facility).Collection(f => f.FacilityImages).LoadAsync();

                    foreach (var img in facility.FacilityImages)
                        img.IsPrimary = false;

                    var newPrimary = facility.FacilityImages
                        .FirstOrDefault(img => img.ImageId == NewPrimaryImageId.Value);

                    if (newPrimary != null)
                    {
                        newPrimary.IsPrimary = true;
                        facility.ImageUrl = newPrimary.ImagePath;
                    }

                    await _context.SaveChangesAsync();
                }

                return Json(new { success = true, message = "Cập nhật thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // ──────────────────────────────────────────────────────────────
        // DETAILS (trả về PartialView dùng trong Modal)
        // ──────────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var facility = await _context.Facilities
                .Include(f => f.FacilityImages)
                .Include(f => f.Courts)
                .FirstOrDefaultAsync(f => f.FacilityId == id);

            if (facility == null) return NotFound();

            // Truyền thẳng Facility model vào _Details partial view
            // (không dùng FacilityDetailsViewModel để tránh mapping phức tạp)
            ViewBag.FacilityImages = facility.FacilityImages
                .OrderBy(i => i.DisplayOrder)
                .ToList();

            return PartialView("_Details", facility);
        }

        // ──────────────────────────────────────────────────────────────
        // DELETE
        // ──────────────────────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var facility = await _context.Facilities
                .Include(f => f.Courts)
                .Include(f => f.FacilityImages)
                .FirstOrDefaultAsync(f => f.FacilityId == id);

            if (facility == null)
                return Json(new { success = false, message = "Không tìm thấy cơ sở!" });

            if (facility.Courts.Any())
                return Json(new { success = false, message = "Không thể xóa cơ sở đang có sân!" });

            try
            {
                foreach (var img in facility.FacilityImages)
                {
                    string path = Path.Combine(_environment.WebRootPath, img.ImagePath.TrimStart('/'));
                    if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
                }

                _context.Facilities.Remove(facility);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Xóa cơ sở thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }
    }
}
