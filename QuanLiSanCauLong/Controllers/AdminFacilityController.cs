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
            ViewBag.Search = search;
            ViewBag.SelectedCity = city;
            return View(facilities);
        }

        [HttpGet]
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            Facility model,
            List<IFormFile> FacilityImages,
            int PrimaryImageIndex = 0,
            List<string>? Amenities = null)
        {
            ModelState.Remove("ImageUrl");
            ModelState.Remove("Status");
            ModelState.Remove("Courts");
            ModelState.Remove("PriceSlots");
            ModelState.Remove("Inventories");
            ModelState.Remove("FacilityImages");
            ModelState.Remove("Facility");
            ModelState.Remove("Amenities");
            ModelState.Remove("Latitude");
            ModelState.Remove("Longitude");

            if (ModelState.IsValid)
            {
                try
                {
                    model.Status = "Active";
                    model.CreatedAt = DateTime.Now;
                    model.UpdatedAt = DateTime.Now;
                    model.IsActive = true;

                    model.Amenities = (Amenities != null && Amenities.Any())
                        ? string.Join(",", Amenities)
                        : null;

                    _context.Facilities.Add(model);
                    await _context.SaveChangesAsync();

                    if (FacilityImages != null && FacilityImages.Count > 0)
                    {
                        string folder = Path.Combine(_environment.WebRootPath, "images", "facilities");
                        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                        for (int i = 0; i < FacilityImages.Count; i++)
                        {
                            var file = FacilityImages[i];
                            if (file.Length == 0) continue;

                            string fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                            using (var stream = new FileStream(Path.Combine(folder, fileName), FileMode.Create))
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

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var facility = await _context.Facilities
                .Include(f => f.FacilityImages)
                .FirstOrDefaultAsync(f => f.FacilityId == id);

            if (facility == null) return NotFound();

            ViewBag.FacilityImages = facility.FacilityImages
                .OrderBy(i => i.DisplayOrder)
                .ToList();

            return View(facility);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            Facility model,
            List<IFormFile>? FacilityImages,       // new images from Create-style param
            List<IFormFile>? NewImageFiles,         // new images from Edit-style param
            string? DeletedImageIds,
            int? NewPrimaryImageId,
            int? PrimaryImageId,
            List<string>? Amenities = null)
        {
            ModelState.Remove("ImageUrl");
            ModelState.Remove("Courts");
            ModelState.Remove("PriceSlots");
            ModelState.Remove("Inventories");
            ModelState.Remove("FacilityImages");
            ModelState.Remove("Amenities");
            ModelState.Remove("Latitude");
            ModelState.Remove("Longitude");

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return Json(new { success = false, message = "Dữ liệu không hợp lệ!", errors });
            }

            try
            {
                var facility = await _context.Facilities
                    .Include(f => f.FacilityImages)
                    .FirstOrDefaultAsync(f => f.FacilityId == model.FacilityId);

                if (facility == null)
                    return Json(new { success = false, message = "Không tìm thấy cơ sở!" });

                facility.FacilityName = model.FacilityName;
                facility.Address = model.Address;
                facility.District = model.District;
                facility.City = model.City;
                facility.Phone = model.Phone;
                facility.Description = model.Description;
                facility.OpenTime = model.OpenTime;
                facility.CloseTime = model.CloseTime;
                facility.Latitude = model.Latitude;
                facility.Longitude = model.Longitude;
                facility.Status = model.Status;
                facility.IsActive = model.Status != "Inactive";
                facility.UpdatedAt = DateTime.Now;

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

                    foreach (var img in facility.FacilityImages.Where(img => idsToDelete.Contains(img.ImageId)).ToList())
                    {
                        string physicalPath = Path.Combine(_environment.WebRootPath, img.ImagePath.TrimStart('/'));
                        if (System.IO.File.Exists(physicalPath)) System.IO.File.Delete(physicalPath);
                        _context.FacilityImages.Remove(img);
                    }
                }

                // 2. Upload ảnh mới (nhận từ cả 2 param name)
                var uploadFiles = FacilityImages?.Where(f => f.Length > 0).ToList()
                               ?? NewImageFiles?.Where(f => f.Length > 0).ToList();

                if (uploadFiles != null && uploadFiles.Count > 0)
                {
                    string folder = Path.Combine(_environment.WebRootPath, "images", "facilities");
                    if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                    int currentMaxOrder = facility.FacilityImages.Any()
                        ? facility.FacilityImages.Max(img => img.DisplayOrder) : -1;

                    foreach (var file in uploadFiles)
                    {
                        string fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                        using (var stream = new FileStream(Path.Combine(folder, fileName), FileMode.Create))
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
                int primaryId = NewPrimaryImageId ?? PrimaryImageId ?? 0;
                if (primaryId > 0)
                {
                    await _context.Entry(facility).Collection(f => f.FacilityImages).LoadAsync();
                    foreach (var img in facility.FacilityImages) img.IsPrimary = false;

                    var newPrimary = facility.FacilityImages.FirstOrDefault(img => img.ImageId == primaryId);
                    if (newPrimary != null)
                    {
                        newPrimary.IsPrimary = true;
                        facility.ImageUrl = newPrimary.ImagePath;
                    }
                    else
                    {
                        // fallback to first image
                        var first = facility.FacilityImages.OrderBy(i => i.DisplayOrder).FirstOrDefault();
                        if (first != null) { first.IsPrimary = true; facility.ImageUrl = first.ImagePath; }
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

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var facility = await _context.Facilities
                .Include(f => f.FacilityImages)
                .Include(f => f.Courts)
                .FirstOrDefaultAsync(f => f.FacilityId == id);

            if (facility == null) return NotFound();

            var vm = new FacilityDetailsViewModel
            {
                FacilityId = facility.FacilityId,
                FacilityName = facility.FacilityName,
                Address = facility.Address,
                City = facility.City,
                District = facility.District,
                Phone = facility.Phone,
                Description = facility.Description,
                OpenTime = facility.OpenTime,
                CloseTime = facility.CloseTime,
                Latitude = facility.Latitude,
                Longitude = facility.Longitude,
                TotalCourts = facility.Courts?.Count ?? 0,
                SelectedDate = DateTime.Today,

                ImageUrls = facility.FacilityImages
                    .OrderBy(i => i.DisplayOrder)
                    .Select(i => i.ImagePath)
                    .ToList(),

                Amenities = string.IsNullOrEmpty(facility.Amenities)
                    ? new List<FacilityAmenityViewModel>()
                    : facility.Amenities
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(a => new FacilityAmenityViewModel { AmenityName = a.Trim(), IsAvailable = true })
                        .ToList(),

                Courts = facility.Courts?.Select(c => new FacilityCourtViewModel
                {
                    CourtId = c.CourtId,
                    CourtNumber = c.CourtNumber,
                    CourtType = c.CourtType,
                    CourtTypeLabel = c.CourtTypeLabel,
                    SurfaceType = c.SurfaceType,
                    SurfaceTypeLabel = c.SurfaceTypeLabel,
                    FloorNumber = c.FloorNumber,
                    HasLighting = c.HasLighting,
                    HasAC = c.HasAC,
                    Description = c.Description,
                    ImagePath = c.ImagePath,
                    HourlyRate = c.HourlyRate,
                    Status = c.Status,
                    StatusLabel = c.StatusLabel,
                    Slots = new List<TimeSlotViewModel>()
                }).ToList() ?? new List<FacilityCourtViewModel>(),

                UserBookings = new List<UserBookingViewModel>(),
                RecentReviews = new List<FacilityReviewViewModel>(),
            };

            return PartialView("Details", vm);
        }

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
