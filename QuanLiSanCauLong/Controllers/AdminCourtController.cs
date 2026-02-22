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

        // ✅ Số sân tối đa mặc định — thay đổi con số này tùy ý
        // Sau khi thêm MaxCourts vào Facility model: xem comment trong CheckCapacityAsync()
        private const int DEFAULT_MAX_COURTS = 6;

        public AdminCourtController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // ─── Helper: Đếm số sân hiện tại ────────────────────────────
        private async Task<int> GetCourtCountAsync(int facilityId, int excludeCourtId = 0)
            => await _context.Courts.CountAsync(c =>
                   c.FacilityId == facilityId &&
                   (excludeCourtId == 0 || c.CourtId != excludeCourtId));

        // ─── Helper: Kiểm tra còn slot không ────────────────────────
        private async Task<(bool canAdd, int current, int max)> CheckCapacityAsync(
            int facilityId, int excludeCourtId = 0)
        {
            var current = await GetCourtCountAsync(facilityId, excludeCourtId);

            // ─── NÂNG CẤP (tuỳ chọn): Dùng MaxCourts từ DB ──────────
            // Bước 1: Thêm vào Models/Facility.cs:
            //   public int MaxCourts { get; set; } = 0;
            // Bước 2: Chạy migration:
            //   Add-Migration AddMaxCourtsToFacility  +  Update-Database
            // Bước 3: Bỏ comment đoạn dưới, xóa dòng "int max = DEFAULT_MAX_COURTS"
            // ─────────────────────────────────────────────────────────
            // var facility = await _context.Facilities
            //     .AsNoTracking()
            //     .FirstOrDefaultAsync(f => f.FacilityId == facilityId);
            // int max = (facility?.MaxCourts > 0) ? facility.MaxCourts : DEFAULT_MAX_COURTS;
            // ─────────────────────────────────────────────────────────

            int max = DEFAULT_MAX_COURTS; // ← xóa dòng này khi dùng MaxCourts từ DB

            return (current < max, current, max);
        }

        // ════════════════════════════════════════════════════════════
        //  INDEX
        // ════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Index(int? facilityId, string courtType, string status)
        {
            var query = _context.Courts
                .Include(c => c.Facility)
                .Include(c => c.Bookings)
                .Include(c => c.CourtImages)
                .AsQueryable();

            if (facilityId.HasValue) query = query.Where(c => c.FacilityId == facilityId.Value);
            if (!string.IsNullOrEmpty(courtType)) query = query.Where(c => c.CourtType == courtType);
            if (!string.IsNullOrEmpty(status)) query = query.Where(c => c.Status == status);

            var courts = await query
                .OrderBy(c => c.Facility.FacilityName)
                .ThenBy(c => c.CourtNumber)
                .ToListAsync();

            ViewBag.Facilities = await _context.Facilities.Where(f => f.IsActive).ToListAsync();
            ViewBag.SelectedFacility = facilityId;
            ViewBag.SelectedType = courtType;
            ViewBag.SelectedStatus = status;
            ViewBag.MaxCourts = DEFAULT_MAX_COURTS;

            return View(courts);
        }

        // ════════════════════════════════════════════════════════════
        //  CREATE
        // ════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Facilities = await _context.Facilities.Where(f => f.IsActive).ToListAsync();
            ViewBag.MaxCourts = DEFAULT_MAX_COURTS;
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
                    // ✅ Kiểm tra giới hạn số sân
                    var (canAdd, current, max) = await CheckCapacityAsync(model.FacilityId);
                    if (!canAdd)
                    {
                        ModelState.AddModelError("",
                            $"Cơ sở này đã đạt giới hạn tối đa {max} sân (hiện có {current} sân). " +
                            "Không thể thêm sân mới.");
                    }
                    else
                    {
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

                            _context.Courts.Add(model);
                            await _context.SaveChangesAsync();

                            if (CourtImages != null && CourtImages.Count > 0)
                                await SaveCourtImagesAsync(model.CourtId, CourtImages, PrimaryImageIndex, model);

                            TempData["SuccessMessage"] =
                                $"Thêm sân mới thành công! Cơ sở hiện có {current + 1}/{max} sân.";
                            return RedirectToAction(nameof(Index));
                        }
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi: " + ex.Message);
                }
            }

            ViewBag.Facilities = await _context.Facilities.Where(f => f.IsActive).ToListAsync();
            ViewBag.MaxCourts = DEFAULT_MAX_COURTS;
            return View(model);
        }

        // ════════════════════════════════════════════════════════════
        //  EDIT
        // ════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var court = await _context.Courts
                .Include(c => c.CourtImages)
                .FirstOrDefaultAsync(c => c.CourtId == id);

            if (court == null) return NotFound();

            ViewBag.Facilities = await _context.Facilities
                .Where(f => f.IsActive)
                .Select(f => new SelectListItem
                {
                    Value = f.FacilityId.ToString(),
                    Text = f.FacilityName,
                    Selected = f.FacilityId == court.FacilityId
                }).ToListAsync();

            ViewBag.MaxCourts = DEFAULT_MAX_COURTS;
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
                        // ✅ Kiểm tra giới hạn nếu chuyển sang facility khác
                        if (court.FacilityId != model.FacilityId)
                        {
                            var (canAdd, current, max) =
                                await CheckCapacityAsync(model.FacilityId, excludeCourtId: model.CourtId);

                            if (!canAdd)
                            {
                                ModelState.AddModelError("",
                                    $"Cơ sở đích đã đạt giới hạn tối đa {max} sân (hiện có {current} sân)!");
                                goto returnView;
                            }
                        }

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
                            court.HourlyRate = model.HourlyRate;
                            court.HasLighting = model.HasLighting;
                            court.HasAC = model.HasAC;

                            // Xóa ảnh
                            if (!string.IsNullOrEmpty(DeletedImageIds))
                            {
                                var idsToDelete = DeletedImageIds.Split(',')
                                    .Where(s => int.TryParse(s.Trim(), out _))
                                    .Select(s => int.Parse(s.Trim())).ToList();

                                var toDelete = court.CourtImages?
                                    .Where(img => idsToDelete.Contains(img.ImageId)).ToList();

                                if (toDelete != null)
                                {
                                    foreach (var img in toDelete)
                                    {
                                        DeletePhysicalFile(img.ImagePath);
                                        _context.CourtImages.Remove(img);
                                    }
                                }
                            }

                            // Đổi ảnh chính
                            if (NewPrimaryImageId.HasValue && NewPrimaryImageId.Value > 0
                                && court.CourtImages != null)
                            {
                                foreach (var img in court.CourtImages) img.IsPrimary = false;
                                var primary = court.CourtImages
                                    .FirstOrDefault(img => img.ImageId == NewPrimaryImageId.Value);
                                if (primary != null)
                                {
                                    primary.IsPrimary = true;
                                    court.ImagePath = primary.ImagePath;
                                }
                            }

                            // Upload ảnh mới
                            if (CourtImages != null && CourtImages.Count > 0)
                            {
                                int maxOrder = court.CourtImages?.Any() == true
                                    ? court.CourtImages.Max(img => img.DisplayOrder) : -1;

                                string folder = GetCourtImageFolder();
                                for (int i = 0; i < CourtImages.Count; i++)
                                {
                                    var file = CourtImages[i];
                                    if (file.Length <= 0) continue;

                                    string fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                                    using (var stream = new FileStream(
                                        Path.Combine(folder, fileName), FileMode.Create))
                                        await file.CopyToAsync(stream);

                                    _context.CourtImages.Add(new CourtImage
                                    {
                                        CourtId = court.CourtId,
                                        ImagePath = $"/images/courts/{fileName}",
                                        IsPrimary = false,
                                        DisplayOrder = maxOrder + i + 1,
                                        UploadedAt = DateTime.Now
                                    });
                                }
                            }

                            // Đảm bảo luôn có ảnh chính
                            if (court.CourtImages?.Any() == true &&
                                !court.CourtImages.Any(img => img.IsPrimary))
                            {
                                var first = court.CourtImages.OrderBy(img => img.DisplayOrder).First();
                                first.IsPrimary = true;
                                court.ImagePath = first.ImagePath;
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

        returnView:
            ViewBag.Facilities = await _context.Facilities
                .Where(f => f.IsActive)
                .Select(f => new SelectListItem
                {
                    Value = f.FacilityId.ToString(),
                    Text = f.FacilityName,
                    Selected = f.FacilityId == model.FacilityId
                }).ToListAsync();
            ViewBag.MaxCourts = DEFAULT_MAX_COURTS;
            return View(model);
        }

        // ════════════════════════════════════════════════════════════
        //  DETAILS
        // ════════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var court = await _context.Courts
                .Include(c => c.Facility)
                .Include(c => c.PriceSlots)
                .Include(c => c.Bookings).ThenInclude(b => b.User)
                .Include(c => c.CourtImages)
                .FirstOrDefaultAsync(c => c.CourtId == id);

            if (court == null) return NotFound();
            return View(court);
        }

        // ════════════════════════════════════════════════════════════
        //  AJAX APIs
        // ════════════════════════════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> GetCourtData(int id)
        {
            var court = await _context.Courts
                .Include(c => c.CourtImages)
                .FirstOrDefaultAsync(c => c.CourtId == id);

            if (court == null)
                return Json(new { success = false, message = "Không tìm thấy sân!" });

            var primary = court.CourtImages?.FirstOrDefault(img => img.IsPrimary);

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
                    imagePath = primary?.ImagePath ?? court.ImagePath
                }
            });
        }

        // ✅ API: Kiểm tra capacity (dùng khi admin chọn facility trong form tạo sân)
        [HttpGet]
        public async Task<IActionResult> GetCourtCapacity(int facilityId)
        {
            var facility = await _context.Facilities
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.FacilityId == facilityId);

            if (facility == null)
                return Json(new { success = false, message = "Không tìm thấy cơ sở!" });

            var (canAdd, current, max) = await CheckCapacityAsync(facilityId);

            return Json(new
            {
                success = true,
                facilityName = facility.FacilityName,
                currentCount = current,
                maxCourts = max,
                remaining = Math.Max(0, max - current),
                canAddMore = canAdd
            });
        }

        // ✅ API: Tạo sân qua Ajax/Modal
        [HttpPost]
        public async Task<IActionResult> CreateAjax([FromBody] Court model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.CourtNumber))
                    return Json(new { success = false, message = "Vui lòng nhập tên sân!" });

                if (model.FacilityId <= 0)
                    return Json(new { success = false, message = "Vui lòng chọn cơ sở!" });

                // ✅ Kiểm tra giới hạn số sân
                var (canAdd, current, max) = await CheckCapacityAsync(model.FacilityId);
                if (!canAdd)
                    return Json(new
                    {
                        success = false,
                        message = $"Cơ sở này đã đạt giới hạn tối đa {max} sân " +
                                  $"(hiện có {current} sân). Không thể thêm sân mới."
                    });

                var isExist = await _context.Courts.AnyAsync(c =>
                    c.FacilityId == model.FacilityId && c.CourtNumber == model.CourtNumber);

                if (isExist)
                    return Json(new { success = false, message = "Số sân này đã tồn tại ở cơ sở này!" });

                model.CreatedAt = DateTime.Now;
                model.Status = model.Status ?? "Available";

                _context.Courts.Add(model);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"Thêm sân mới thành công! Cơ sở hiện có {current + 1}/{max} sân.",
                    currentCount = current + 1,
                    maxCourts = max,
                    remaining = max - (current + 1)
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // ✅ API: Sửa sân qua Ajax
        [HttpPost]
        public async Task<IActionResult> EditAjax([FromBody] Court model)
        {
            try
            {
                var court = await _context.Courts.FindAsync(model.CourtId);
                if (court == null)
                    return Json(new { success = false, message = "Không tìm thấy sân!" });

                if (court.FacilityId != model.FacilityId)
                {
                    var (canAdd, current, max) =
                        await CheckCapacityAsync(model.FacilityId, excludeCourtId: model.CourtId);

                    if (!canAdd)
                        return Json(new
                        {
                            success = false,
                            message = $"Cơ sở đích đã đạt giới hạn tối đa {max} sân!"
                        });
                }

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

        // ✅ API: Xóa sân
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
                if (court.CourtImages != null)
                    foreach (var img in court.CourtImages)
                        DeletePhysicalFile(img.ImagePath);

                _context.Courts.Remove(court);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Xóa sân thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // ✅ API: Lấy danh sách sân theo facility
        [HttpGet]
        public async Task<IActionResult> GetByFacility(int facilityId)
        {
            var courts = await _context.Courts
                .Where(c => c.FacilityId == facilityId && c.Status == "Available")
                .Select(c => new { c.CourtId, c.CourtNumber, c.CourtType })
                .ToListAsync();

            var (canAdd, current, max) = await CheckCapacityAsync(facilityId);

            return Json(new
            {
                courts,
                currentCount = current,
                maxCourts = max,
                canAddMore = canAdd,
                remaining = Math.Max(0, max - current)
            });
        }

        // ════════════════════════════════════════════════════════════
        //  PRIVATE HELPERS
        // ════════════════════════════════════════════════════════════

        private string GetCourtImageFolder()
        {
            string folder = Path.Combine(_environment.WebRootPath, "images", "courts");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            return folder;
        }

        private void DeletePhysicalFile(string? imagePath)
        {
            if (string.IsNullOrEmpty(imagePath)) return;
            string physPath = Path.Combine(_environment.WebRootPath, imagePath.TrimStart('/'));
            if (System.IO.File.Exists(physPath)) System.IO.File.Delete(physPath);
        }

        private async Task SaveCourtImagesAsync(
            int courtId, List<IFormFile> files, int primaryIndex, Court courtRef)
        {
            string folder = GetCourtImageFolder();

            for (int i = 0; i < files.Count; i++)
            {
                var file = files[i];
                if (file.Length <= 0) continue;

                string fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                using (var stream = new FileStream(Path.Combine(folder, fileName), FileMode.Create))
                    await file.CopyToAsync(stream);

                var img = new CourtImage
                {
                    CourtId = courtId,
                    ImagePath = $"/images/courts/{fileName}",
                    IsPrimary = (i == primaryIndex),
                    DisplayOrder = i,
                    UploadedAt = DateTime.Now
                };
                _context.CourtImages.Add(img);

                if (i == primaryIndex) courtRef.ImagePath = img.ImagePath;
            }

            await _context.SaveChangesAsync();
        }
    }
}
