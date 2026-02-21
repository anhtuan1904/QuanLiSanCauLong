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

        [HttpGet]
        public async Task<IActionResult> Index(string search, string city)
        {
            var query = _context.Facilities
                .Include(f => f.FacilityImages) // Include để lấy ảnh đại diện
                .Where(f => f.IsActive).AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(f => f.FacilityName.Contains(search) || f.Address.Contains(search));

            if (!string.IsNullOrEmpty(city))
                query = query.Where(f => f.City == city);

            var facilities = await query.OrderByDescending(f => f.CreatedAt).ToListAsync();
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
        public async Task<IActionResult> Create(Facility model, List<IFormFile> FacilityImages, int PrimaryImageIndex = 0)
        {
            // Cực kỳ quan trọng: Loại bỏ kiểm tra các trường không nhập từ Form
            ModelState.Remove("ImageUrl");
            ModelState.Remove("Status");
            ModelState.Remove("Courts");
            ModelState.Remove("PriceSlots");
            ModelState.Remove("Inventories");
            ModelState.Remove("FacilityImages");
            // Thêm các trường này nếu ModelState vẫn báo lỗi
            ModelState.Remove("Facility");

            if (ModelState.IsValid)
            {
                try
                {
                    // Gán giá trị mặc định cho Status (Vì trong Model bạn để là string)
                    model.Status = "Active";
                    model.CreatedAt = DateTime.Now;
                    model.UpdatedAt = DateTime.Now;
                    model.IsActive = true;

                    _context.Facilities.Add(model);
                    await _context.SaveChangesAsync();

                    // Xử lý Upload Ảnh
                    if (FacilityImages != null && FacilityImages.Count > 0)
                    {
                        string folder = Path.Combine(_environment.WebRootPath, "images", "facilities");
                        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                        for (int i = 0; i < FacilityImages.Count; i++)
                        {
                            var file = FacilityImages[i];
                            if (file.Length > 0)
                            {
                                string fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                                string filePath = Path.Combine(folder, fileName);

                                using (var stream = new FileStream(filePath, FileMode.Create))
                                {
                                    await file.CopyToAsync(stream);
                                }

                                var facilityImage = new FacilityImage
                                {
                                    FacilityId = model.FacilityId,
                                    ImagePath = $"/images/facilities/{fileName}",
                                    IsPrimary = (i == PrimaryImageIndex),
                                    DisplayOrder = i,
                                    UploadedAt = DateTime.Now
                                };

                                _context.FacilityImages.Add(facilityImage);

                                // Cập nhật đường dẫn ảnh đại diện cho bảng Facility
                                if (i == PrimaryImageIndex)
                                {
                                    model.ImageUrl = facilityImage.ImagePath;
                                    _context.Entry(model).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                                }
                            }
                        }
                        await _context.SaveChangesAsync();
                    }

                    return Json(new { success = true, message = "Thêm mới cơ sở thành công!" });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "Lỗi hệ thống: " + ex.InnerException?.Message ?? ex.Message });
                }
            }

            // Trả về danh sách lỗi cụ thể để Debug trên Browser console
            var errors = ModelState.Where(x => x.Value.Errors.Count > 0)
                                   .Select(x => new {
                                       key = x.Key,
                                       errors = x.Value.Errors.Select(e => e.ErrorMessage)
                                   });

            return Json(new
            {
                success = false,
                message = "Dữ liệu không hợp lệ!",
                errors = errors.SelectMany(x => x.errors)
            });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var facility = await _context.Facilities
                .Include(f => f.FacilityImages)
                .FirstOrDefaultAsync(f => f.FacilityId == id);

            if (facility == null) return NotFound();
            return View(facility);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Facility model, List<IFormFile>? FacilityImages, string? DeletedImageIds, int? NewPrimaryImageId)
        {
            // Loại bỏ kiểm tra các trường không liên quan để tránh ModelState.IsValid = false
            ModelState.Remove("ImageUrl");
            ModelState.Remove("Courts");
            ModelState.Remove("PriceSlots");
            ModelState.Remove("Inventories");
            ModelState.Remove("FacilityImages");

            if (ModelState.IsValid)
            {
                var facility = await _context.Facilities
                    .Include(f => f.FacilityImages)
                    .FirstOrDefaultAsync(f => f.FacilityId == model.FacilityId);

                if (facility == null) return Json(new { success = false, message = "Không tìm thấy cơ sở!" });

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
                    facility.UpdatedAt = DateTime.Now;

                    // 1. Xử lý xóa ảnh (giữ nguyên logic của bạn)
                    if (!string.IsNullOrEmpty(DeletedImageIds))
                    {
                        var idsToDelete = DeletedImageIds.Split(',').Select(id => int.Parse(id.Trim())).ToList();
                        var imagesToDelete = facility.FacilityImages.Where(img => idsToDelete.Contains(img.ImageId)).ToList();
                        foreach (var img in imagesToDelete)
                        {
                            string physicalPath = Path.Combine(_environment.WebRootPath, img.ImagePath.TrimStart('/'));
                            if (System.IO.File.Exists(physicalPath)) System.IO.File.Delete(physicalPath);
                            _context.FacilityImages.Remove(img);
                        }
                    }

                    // 2. Xử lý upload thêm ảnh mới (giữ nguyên logic của bạn)
                    if (FacilityImages != null && FacilityImages.Count > 0)
                    {
                        string folder = Path.Combine(_environment.WebRootPath, "images", "facilities");
                        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                        int currentMaxOrder = facility.FacilityImages.Any() ? facility.FacilityImages.Max(img => img.DisplayOrder) : -1;

                        foreach (var file in FacilityImages)
                        {
                            string fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                            string filePath = Path.Combine(folder, fileName);
                            using (var stream = new FileStream(filePath, FileMode.Create)) { await file.CopyToAsync(stream); }

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

                    await _context.SaveChangesAsync(); // Lưu để có ID cho ảnh mới nếu cần

                    // 3. Xử lý ảnh chính (NewPrimaryImageId có thể là ID của ảnh cũ hoặc xử lý logic ảnh mới)
                    if (NewPrimaryImageId.HasValue && NewPrimaryImageId.Value > 0)
                    {
                        foreach (var img in facility.FacilityImages) img.IsPrimary = false;
                        var newPrimary = facility.FacilityImages.FirstOrDefault(img => img.ImageId == NewPrimaryImageId.Value);
                        if (newPrimary != null)
                        {
                            newPrimary.IsPrimary = true;
                            facility.ImageUrl = newPrimary.ImagePath;
                        }
                    }

                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = "Cập nhật thành công!" });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "Lỗi: " + ex.Message });
                }
            }

            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            return Json(new { success = false, message = "Dữ color không hợp lệ!", errors = errors });
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var facility = await _context.Facilities
                .Include(f => f.FacilityImages)
                .Include(f => f.Courts)
                .FirstOrDefaultAsync(f => f.FacilityId == id);

            if (facility == null) return NotFound();

            var model = new FacilityDetailsViewModel
            {
                FacilityId = facility.FacilityId,
                FacilityName = facility.FacilityName,
                Address = facility.Address,
                Phone = facility.Phone,
                City = facility.City,
                District = facility.District,
                OpenTime = facility.OpenTime,
                CloseTime = facility.CloseTime,
                Description = facility.Description,
                TotalCourts = facility.Courts?.Count ?? 0,
                ImageUrls = facility.FacilityImages.OrderBy(i => i.DisplayOrder).Select(i => i.ImagePath).ToList()
            };

            // QUAN TRỌNG: Trả về PartialView để hiển thị trong Modal
            return PartialView("_Details", model);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var facility = await _context.Facilities
                .Include(f => f.Courts)
                .Include(f => f.FacilityImages)
                .FirstOrDefaultAsync(f => f.FacilityId == id);

            if (facility == null) return Json(new { success = false, message = "Không tìm thấy cơ sở!" });
            if (facility.Courts.Any()) return Json(new { success = false, message = "Không thể xóa cơ sở có sân!" });

            try
            {
                // Xóa file vật lý
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