using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLiSanCauLong.Data;
using QuanLiSanCauLong.Models;

namespace QuanLiSanCauLong.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminCourtController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public AdminCourtController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int? facilityId, string courtType, string status)
        {
            var query = _context.Courts
                .Include(c => c.Facility)
                .Include(c => c.Bookings)
                .Include(c => c.CourtImages)
                .AsQueryable();

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

            ViewBag.Facilities = await _context.Facilities.Where(f => f.IsActive).ToListAsync();
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
        public async Task<IActionResult> Create(Court model, List<IFormFile> CourtImages, int PrimaryImageIndex = 0)
        {
            ModelState.Remove("Facility");
            ModelState.Remove("PriceSlots");
            ModelState.Remove("Bookings");
            ModelState.Remove("CourtImages");

            if (ModelState.IsValid)
            {
                try
                {
                    // Check duplicate
                    var isExist = await _context.Courts.AnyAsync(c =>
                        c.FacilityId == model.FacilityId && c.CourtNumber == model.CourtNumber);

                    if (isExist)
                    {
                        ModelState.AddModelError("CourtNumber", "Số sân này đã tồn tại ở cơ sở này!");
                    }
                    else
                    {
                        model.CreatedAt = DateTime.Now;
                        model.Status = model.Status ?? "Available";

                        // Add court first to get ID
                        _context.Courts.Add(model);
                        await _context.SaveChangesAsync();

                        // Handle multiple images
                        if (CourtImages != null && CourtImages.Count > 0)
                        {
                            string courtImageFolder = Path.Combine(_environment.WebRootPath, "images", "courts");
                            if (!Directory.Exists(courtImageFolder))
                                Directory.CreateDirectory(courtImageFolder);

                            for (int i = 0; i < CourtImages.Count; i++)
                            {
                                var file = CourtImages[i];
                                if (file.Length > 0)
                                {
                                    string fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                                    string filePath = Path.Combine(courtImageFolder, fileName);

                                    using (var stream = new FileStream(filePath, FileMode.Create))
                                    {
                                        await file.CopyToAsync(stream);
                                    }

                                    var courtImage = new CourtImage
                                    {
                                        CourtId = model.CourtId,
                                        ImagePath = $"/images/courts/{fileName}",
                                        IsPrimary = (i == PrimaryImageIndex),
                                        DisplayOrder = i,
                                        UploadedAt = DateTime.Now
                                    };

                                    _context.CourtImages.Add(courtImage);

                                    // Also set old ImagePath for backward compatibility
                                    if (i == PrimaryImageIndex)
                                    {
                                        model.ImagePath = courtImage.ImagePath;
                                    }
                                }
                            }

                            await _context.SaveChangesAsync();
                        }

                        TempData["SuccessMessage"] = "Thêm sân mới thành công!";
                        return RedirectToAction(nameof(Index));
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi: " + ex.Message);
                }
            }

            ViewBag.Facilities = await _context.Facilities.Where(f => f.IsActive).ToListAsync();
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var court = await _context.Courts
                .Include(c => c.CourtImages)
                .FirstOrDefaultAsync(c => c.CourtId == id);

            if (court == null)
                return NotFound();

            ViewBag.Facilities = await _context.Facilities
                .Where(f => f.IsActive)
                .Select(f => new SelectListItem
                {
                    Value = f.FacilityId.ToString(),
                    Text = f.FacilityName,
                    Selected = f.FacilityId == court.FacilityId
                }).ToListAsync();

            return View(court);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Court model,
            List<IFormFile>? CourtImages,
            string? DeletedImageIds,
            int? NewPrimaryImageId)
        {
            ModelState.Remove("Facility");
            ModelState.Remove("PriceSlots");
            ModelState.Remove("Bookings");
            ModelState.Remove("CourtImages");

            if (ModelState.IsValid)
            {
                var court = await _context.Courts
                    .Include(c => c.CourtImages)
                    .FirstOrDefaultAsync(c => c.CourtId == model.CourtId);

                if (court != null)
                {
                    try
                    {
                        // Check duplicate (exclude current)
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
                            // Update basic info
                            court.FacilityId = model.FacilityId;
                            court.CourtNumber = model.CourtNumber;
                            court.CourtType = model.CourtType;
                            court.Status = model.Status;
                            court.HourlyRate = model.HourlyRate;
                            court.HasLighting = model.HasLighting;
                            court.HasAC = model.HasAC;

                            // Handle deleted images
                            if (!string.IsNullOrEmpty(DeletedImageIds))
                            {
                                var idsToDelete = DeletedImageIds.Split(',')
                                    .Select(id => int.Parse(id.Trim()))
                                    .ToList();

                                var imagesToDelete = court.CourtImages?
                                    .Where(img => idsToDelete.Contains(img.ImageId))
                                    .ToList();

                                if (imagesToDelete != null && imagesToDelete.Any())
                                {
                                    foreach (var img in imagesToDelete)
                                    {
                                        // Delete physical file
                                        if (!string.IsNullOrEmpty(img.ImagePath))
                                        {
                                            string physicalPath = Path.Combine(_environment.WebRootPath, img.ImagePath.TrimStart('/'));
                                            if (System.IO.File.Exists(physicalPath))
                                            {
                                                System.IO.File.Delete(physicalPath);
                                            }
                                        }

                                        _context.CourtImages.Remove(img);
                                    }
                                }
                            }

                            // Handle new primary image selection
                            if (NewPrimaryImageId.HasValue && NewPrimaryImageId.Value > 0)
                            {
                                // Remove primary from all
                                if (court.CourtImages != null)
                                {
                                    foreach (var img in court.CourtImages)
                                    {
                                        img.IsPrimary = false;
                                    }

                                    // Set new primary
                                    var newPrimary = court.CourtImages
                                        .FirstOrDefault(img => img.ImageId == NewPrimaryImageId.Value);
                                    if (newPrimary != null)
                                    {
                                        newPrimary.IsPrimary = true;
                                        court.ImagePath = newPrimary.ImagePath; // Update backward compatibility
                                    }
                                }
                            }

                            // Handle new images upload
                            if (CourtImages != null && CourtImages.Count > 0)
                            {
                                string courtImageFolder = Path.Combine(_environment.WebRootPath, "images", "courts");
                                if (!Directory.Exists(courtImageFolder))
                                    Directory.CreateDirectory(courtImageFolder);

                                int currentMaxOrder = court.CourtImages?.Max(img => img.DisplayOrder) ?? -1;

                                for (int i = 0; i < CourtImages.Count; i++)
                                {
                                    var file = CourtImages[i];
                                    if (file.Length > 0)
                                    {
                                        string fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                                        string filePath = Path.Combine(courtImageFolder, fileName);

                                        using (var stream = new FileStream(filePath, FileMode.Create))
                                        {
                                            await file.CopyToAsync(stream);
                                        }

                                        var newImage = new CourtImage
                                        {
                                            CourtId = court.CourtId,
                                            ImagePath = $"/images/courts/{fileName}",
                                            IsPrimary = false, // New images are not primary by default
                                            DisplayOrder = currentMaxOrder + i + 1,
                                            UploadedAt = DateTime.Now
                                        };

                                        _context.CourtImages.Add(newImage);
                                    }
                                }
                            }

                            // Make sure there's at least one primary image
                            if (court.CourtImages != null && court.CourtImages.Any())
                            {
                                if (!court.CourtImages.Any(img => img.IsPrimary))
                                {
                                    var firstImage = court.CourtImages.OrderBy(img => img.DisplayOrder).First();
                                    firstImage.IsPrimary = true;
                                    court.ImagePath = firstImage.ImagePath;
                                }
                            }

                            await _context.SaveChangesAsync();
                            TempData["SuccessMessage"] = "Cập nhật sân thành công!";
                            return RedirectToAction(nameof(Index));
                        }
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("", "Lỗi: " + ex.Message);
                    }
                }
            }

            ViewBag.Facilities = await _context.Facilities
                .Where(f => f.IsActive)
                .Select(f => new SelectListItem
                {
                    Value = f.FacilityId.ToString(),
                    Text = f.FacilityName,
                    Selected = f.FacilityId == model.FacilityId
                }).ToListAsync();

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
                .Include(c => c.CourtImages)
                .FirstOrDefaultAsync(c => c.CourtId == id);

            if (court == null)
                return NotFound();

            return View(court);
        }

        [HttpGet]
        public async Task<IActionResult> GetCourtData(int id)
        {
            var court = await _context.Courts
                .Include(c => c.CourtImages)
                .FirstOrDefaultAsync(c => c.CourtId == id);

            if (court == null)
                return Json(new { success = false, message = "Không tìm thấy sân!" });

            var primaryImage = court.CourtImages?.FirstOrDefault(img => img.IsPrimary);

            return Json(new
            {
                success = true,
                court = new
                {
                    courtId = court.CourtId,
                    courtNumber = court.CourtNumber,
                    courtType = court.CourtType,
                    facilityId = court.FacilityId,
                    hourlyRate = court.HourlyRate,
                    status = court.Status,
                    hasLighting = court.HasLighting,
                    hasAC = court.HasAC,
                    imagePath = primaryImage?.ImagePath ?? court.ImagePath
                }
            });
        }

        [HttpPost]
        public async Task<IActionResult> CreateAjax([FromBody] Court model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.CourtNumber))
                    return Json(new { success = false, message = "Vui lòng nhập tên sân!" });

                if (model.FacilityId <= 0)
                    return Json(new { success = false, message = "Vui lòng chọn cơ sở!" });

                var isExist = await _context.Courts.AnyAsync(c =>
                    c.FacilityId == model.FacilityId &&
                    c.CourtNumber == model.CourtNumber);

                if (isExist)
                    return Json(new { success = false, message = "Số sân này đã tồn tại ở cơ sở này!" });

                model.CreatedAt = DateTime.Now;
                model.Status = model.Status ?? "Available";

                _context.Courts.Add(model);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Thêm sân mới thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> EditAjax([FromBody] Court model)
        {
            try
            {
                var court = await _context.Courts.FindAsync(model.CourtId);

                if (court == null)
                    return Json(new { success = false, message = "Không tìm thấy sân!" });

                var isExist = await _context.Courts.AnyAsync(c =>
                    c.FacilityId == model.FacilityId &&
                    c.CourtNumber == model.CourtNumber &&
                    c.CourtId != model.CourtId);

                if (isExist)
                    return Json(new { success = false, message = "Số sân này đã tồn tại ở cơ sở này!" });

                court.FacilityId = model.FacilityId;
                court.CourtNumber = model.CourtNumber;
                court.CourtType = model.CourtType;
                court.Status = model.Status;
                court.HourlyRate = model.HourlyRate;
                court.HasLighting = model.HasLighting;
                court.HasAC = model.HasAC;
                court.ImagePath = model.ImagePath;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Cập nhật sân thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
                return Json(new { success = false, message = "ID không hợp lệ!" });

            var court = await _context.Courts
                .Include(c => c.Bookings)
                .Include(c => c.PriceSlots)
                .Include(c => c.CourtImages)
                .FirstOrDefaultAsync(c => c.CourtId == id);

            if (court == null)
                return Json(new { success = false, message = "Không tìm thấy sân!" });

            if (court.Bookings != null && court.Bookings.Any())
                return Json(new { success = false, message = "Không thể xóa sân đã có lịch đặt!" });

            try
            {
                // Delete all images
                if (court.CourtImages != null && court.CourtImages.Any())
                {
                    foreach (var img in court.CourtImages)
                    {
                        if (!string.IsNullOrEmpty(img.ImagePath))
                        {
                            string physicalPath = Path.Combine(_environment.WebRootPath, img.ImagePath.TrimStart('/'));
                            if (System.IO.File.Exists(physicalPath))
                            {
                                System.IO.File.Delete(physicalPath);
                            }
                        }
                    }
                }

                _context.Courts.Remove(court);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Xóa sân thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetByFacility(int facilityId)
        {
            var courts = await _context.Courts
                .Where(c => c.FacilityId == facilityId && c.Status == "Available")
                .Select(c => new { c.CourtId, c.CourtNumber, c.CourtType })
                .ToListAsync();

            return Json(courts);
        }
    }
}
