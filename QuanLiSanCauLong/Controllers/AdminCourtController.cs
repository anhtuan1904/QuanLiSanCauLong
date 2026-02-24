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

        private const int DEFAULT_MAX_COURTS = 6;

        public AdminCourtController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // ─── Helper: Đếm số sân hiện tại ──────────────────────────
        private async Task<int> GetCourtCountAsync(int facilityId, int excludeCourtId = 0)
            => await _context.Courts.CountAsync(c =>
                   c.FacilityId == facilityId &&
                   (excludeCourtId == 0 || c.CourtId != excludeCourtId));

        // ─── Helper: Kiểm tra còn slot không ──────────────────────
        private async Task<(bool canAdd, int current, int max)> CheckCapacityAsync(
            int facilityId, int excludeCourtId = 0)
        {
            var current = await GetCourtCountAsync(facilityId, excludeCourtId);
            int max = DEFAULT_MAX_COURTS;
            return (current < max, current, max);
        }

        // ══════════════════════════════════════════════════════════
        //  INDEX
        // ══════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Index(int? facilityId, string? courtType, string? status)
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
                .OrderBy(c => c.Facility!.FacilityName)
                .ThenBy(c => c.CourtNumber)
                .ToListAsync();

            ViewBag.Facilities = await _context.Facilities.Where(f => f.IsActive).ToListAsync();
            ViewBag.SelectedFacility = facilityId;
            ViewBag.SelectedType = courtType;
            ViewBag.SelectedStatus = status;
            ViewBag.MaxCourts = DEFAULT_MAX_COURTS;

            return View(courts);
        }

        // ══════════════════════════════════════════════════════════
        //  CREATE
        // ══════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Facilities = await _context.Facilities.Where(f => f.IsActive).ToListAsync();
            ViewBag.MaxCourts = DEFAULT_MAX_COURTS;
            var model = new Court
            {
                CourtType = "Indoor",
                SurfaceType = "PVC",
                Status = "Available"
            };
            return View(model);
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
                            ModelState.AddModelError("CourtNumber", "Tên sân này đã tồn tại ở cơ sở này!");
                        else
                        {
                            model.CreatedAt = DateTime.Now;
                            model.Status ??= "Available";

                            _context.Courts.Add(model);
                            await _context.SaveChangesAsync();

                            if (CourtImages?.Count > 0)
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

        // ══════════════════════════════════════════════════════════
        //  EDIT
        // ══════════════════════════════════════════════════════════
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
                            ModelState.AddModelError("CourtNumber", "Tên sân này đã tồn tại ở cơ sở này!");
                        else
                        {
                            court.FacilityId = model.FacilityId;
                            court.CourtNumber = model.CourtNumber;
                            court.CourtType = model.CourtType;
                            court.SurfaceType = model.SurfaceType;
                            court.Status = model.Status;
                            court.FloorNumber = model.FloorNumber;
                            court.HasLighting = model.HasLighting;
                            court.HasAC = model.HasAC;
                            court.Description = model.Description;

                            if (!string.IsNullOrEmpty(DeletedImageIds))
                            {
                                var idsToDelete = DeletedImageIds.Split(',')
                                    .Where(s => int.TryParse(s.Trim(), out _))
                                    .Select(s => int.Parse(s.Trim())).ToList();

                                var toDelete = court.CourtImages?
                                    .Where(img => idsToDelete.Contains(img.ImageId)).ToList();

                                if (toDelete != null)
                                    foreach (var img in toDelete)
                                    {
                                        DeletePhysicalFile(img.ImagePath);
                                        _context.CourtImages.Remove(img);
                                    }
                            }

                            if (NewPrimaryImageId > 0 && court.CourtImages != null)
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

                            if (CourtImages?.Count > 0)
                            {
                                int maxOrder = court.CourtImages?.Any() == true
                                    ? court.CourtImages.Max(img => img.DisplayOrder) : -1;

                                string folder = GetCourtImageFolder();
                                for (int i = 0; i < CourtImages.Count; i++)
                                {
                                    var file = CourtImages[i];
                                    if (file.Length <= 0) continue;

                                    string fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                                    using var stream = new FileStream(
                                        Path.Combine(folder, fileName), FileMode.Create);
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

        // ══════════════════════════════════════════════════════════
        //  DETAILS
        // ══════════════════════════════════════════════════════════
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var court = await _context.Courts
                .Include(c => c.Facility)
                .Include(c => c.PriceSlots)
                .Include(c => c.Bookings!).ThenInclude(b => b.User)
                .Include(c => c.CourtImages)
                .FirstOrDefaultAsync(c => c.CourtId == id);

            if (court == null) return NotFound();
            return View(court);
        }

        // ══════════════════════════════════════════════════════════
        //  PRICESLOT MANAGEMENT — SỬA LỖI OVERLAP
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Lấy danh sách khung giờ của một sân (trả về JSON cho View)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetPriceSlots(int courtId)
        {
            var slots = await _context.PriceSlots
                .Where(ps => ps.CourtId == courtId)
                .OrderBy(ps => ps.DayOfWeek)
                .ThenBy(ps => ps.StartTime)
                .Select(ps => new
                {
                    SlotId = ps.PriceSlotId,
                    ps.CourtId,
                    DayOfWeek = (int?)ps.DayOfWeek,
                    DayLabel = ps.DayOfWeek == null ? "Tất cả" :
                                ps.DayOfWeek == System.DayOfWeek.Sunday ? "Chủ nhật" :
                                ps.DayOfWeek == System.DayOfWeek.Monday ? "Thứ Hai" :
                                ps.DayOfWeek == System.DayOfWeek.Tuesday ? "Thứ Ba" :
                                ps.DayOfWeek == System.DayOfWeek.Wednesday ? "Thứ Tư" :
                                ps.DayOfWeek == System.DayOfWeek.Thursday ? "Thứ Năm" :
                                ps.DayOfWeek == System.DayOfWeek.Friday ? "Thứ Sáu" :
                                ps.DayOfWeek == System.DayOfWeek.Saturday ? "Thứ Bảy" : "",
                    StartTime = ps.StartTime.ToString().Substring(0, 5),
                    EndTime = ps.EndTime.ToString().Substring(0, 5),
                    Price = ps.Price,
                    ps.IsActive
                })
                .ToListAsync();

            return Json(new { success = true, slots });
        }

        /// <summary>
        /// Thêm khung giờ — ĐÃ SỬA LOGIC OVERLAP
        ///
        /// Lỗi cũ: dùng điều kiện  startNew < endExisting && endNew > startExisting
        /// nhưng so sánh TimeSpan bằng >= thay vì > khiến 2 khung giờ liền nhau
        /// (6:00-7:00 và 7:00-8:00) bị coi là trùng.
        ///
        /// Fix: Hai khung giờ A và B chỉ THỰC SỰ TRÙNG khi:
        ///      A.Start < B.End  VÀ  A.End > B.Start
        /// Hai khung giờ liền nhau (A.End == B.Start) KHÔNG phải trùng.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AddPriceSlot(
            int courtId, int dayOfWeek,
            string startTime, string endTime,
            decimal price, bool isActive = true)
        {
            try
            {
                // Validate court tồn tại
                var court = await _context.Courts.FindAsync(courtId);
                if (court == null)
                    return Json(new { success = false, message = "Không tìm thấy sân!" });

                // Parse thời gian
                if (!TimeSpan.TryParse(startTime, out var start) ||
                    !TimeSpan.TryParse(endTime, out var end))
                    return Json(new { success = false, message = "Định dạng giờ không hợp lệ! (VD: 06:00)" });

                if (start >= end)
                    return Json(new { success = false, message = "Giờ bắt đầu phải nhỏ hơn giờ kết thúc!" });

                if (price < 0)
                    return Json(new { success = false, message = "Giá không được âm!" });

                // ✅ KIỂM TRA OVERLAP — ĐÃ SỬA
                // DayOfWeek trong model là DayOfWeek? (nullable enum) → cast int sang (DayOfWeek)
                var targetDay = (System.DayOfWeek)dayOfWeek;
                var overlap = await _context.PriceSlots
                    .Where(ps => ps.CourtId == courtId
                              && ps.DayOfWeek == targetDay
                              && start < ps.EndTime
                              && end > ps.StartTime)
                    .FirstOrDefaultAsync();

                if (overlap != null)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Khung giờ {start:hh\\:mm}–{end:hh\\:mm} bị trùng với " +
                                  $"khung giờ đã có ({overlap.StartTime:hh\\:mm}–{overlap.EndTime:hh\\:mm})!"
                    });
                }

                var slot = new PriceSlot
                {
                    CourtId = courtId,
                    FacilityId = court.FacilityId,
                    DayOfWeek = (System.DayOfWeek)dayOfWeek,
                    CourtType = court.CourtType,
                    StartTime = start,
                    EndTime = end,
                    Price = price,
                    IsActive = isActive
                };

                _context.PriceSlots.Add(slot);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"Thêm khung giờ {start:hh\\:mm}–{end:hh\\:mm} thành công!",
                    slot = new
                    {
                        SlotId = slot.PriceSlotId,
                        DayOfWeek = (int)slot.DayOfWeek!,
                        DayLabel = GetDayLabel(dayOfWeek),
                        StartTime = slot.StartTime.ToString(@"hh\:mm"),
                        EndTime = slot.EndTime.ToString(@"hh\:mm"),
                        slot.Price,
                        slot.IsActive
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        /// <summary>
        /// Sửa khung giờ — cũng áp dụng logic overlap đã fix, bỏ qua chính nó
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> EditPriceSlot(
            int slotId, int dayOfWeek,
            string startTime, string endTime,
            decimal price, bool isActive = true)
        {
            try
            {
                var slot = await _context.PriceSlots.FindAsync(slotId);
                if (slot == null)
                    return Json(new { success = false, message = "Không tìm thấy khung giờ!" });

                if (!TimeSpan.TryParse(startTime, out var start) ||
                    !TimeSpan.TryParse(endTime, out var end))
                    return Json(new { success = false, message = "Định dạng giờ không hợp lệ!" });

                if (start >= end)
                    return Json(new { success = false, message = "Giờ bắt đầu phải nhỏ hơn giờ kết thúc!" });

                // ✅ Kiểm tra overlap, BỎ QUA chính slot đang sửa
                var targetDayEdit = (System.DayOfWeek)dayOfWeek;
                var overlap = await _context.PriceSlots
                    .Where(ps => ps.CourtId == slot.CourtId
                              && ps.DayOfWeek == targetDayEdit
                              && ps.PriceSlotId != slotId
                              && start < ps.EndTime
                              && end > ps.StartTime)
                    .FirstOrDefaultAsync();

                if (overlap != null)
                    return Json(new
                    {
                        success = false,
                        message = $"Khung giờ bị trùng với ({overlap.StartTime:hh\\:mm}–{overlap.EndTime:hh\\:mm})!"
                    });

                slot.DayOfWeek = (System.DayOfWeek)dayOfWeek;
                slot.StartTime = start;
                slot.EndTime = end;
                slot.Price = price;
                slot.IsActive = isActive;

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Cập nhật khung giờ thành công!",
                    slot = new
                    {
                        SlotId = slot.PriceSlotId,
                        DayOfWeek = (int)slot.DayOfWeek!,
                        DayLabel = GetDayLabel(dayOfWeek),
                        StartTime = slot.StartTime.ToString(@"hh\:mm"),
                        EndTime = slot.EndTime.ToString(@"hh\:mm"),
                        slot.Price,
                        slot.IsActive
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        /// <summary>
        /// Xóa khung giờ — kiểm tra không có booking tương lai
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> DeletePriceSlot(int slotId)
        {
            try
            {
                var slot = await _context.PriceSlots.FindAsync(slotId);
                if (slot == null)
                    return Json(new { success = false, message = "Không tìm thấy khung giờ!" });

                // Kiểm tra booking tương lai dùng khung giờ này
                var hasFutureBooking = await _context.Bookings
                    .AnyAsync(b => b.CourtId == slot.CourtId
                               && b.BookingDate >= DateTime.Today
                               && b.StartTime == slot.StartTime
                               && b.Status != "Cancelled");

                if (hasFutureBooking)
                    return Json(new
                    {
                        success = false,
                        message = "Không thể xóa khung giờ đang có lịch đặt trong tương lai!"
                    });

                _context.PriceSlots.Remove(slot); await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Xóa khung giờ thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        /// <summary>
        /// Bật/tắt trạng thái khung giờ
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> TogglePriceSlot(int slotId)
        {
            var slot = await _context.PriceSlots.FindAsync(slotId);
            if (slot == null)
                return Json(new { success = false, message = "Không tìm thấy khung giờ!" });

            slot.IsActive = !slot.IsActive;
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                isActive = slot.IsActive,
                message = slot.IsActive ? "Đã kích hoạt khung giờ!" : "Đã tắt khung giờ!"
            });
        }

        /// <summary>
        /// Sao chép toàn bộ khung giờ từ một ngày sang ngày khác trong cùng sân
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CopyDaySlots(int courtId, int fromDay, int toDay)
        {
            try
            {
                if (fromDay == toDay)
                    return Json(new { success = false, message = "Không thể sao chép vào cùng ngày!" });

                var fromDayEnum = (System.DayOfWeek)fromDay;
                var toDayEnum = (System.DayOfWeek)toDay;

                var sourceSlots = await _context.PriceSlots
                    .Where(ps => ps.CourtId == courtId && ps.DayOfWeek == fromDayEnum)
                    .ToListAsync();

                if (!sourceSlots.Any())
                    return Json(new { success = false, message = "Ngày nguồn không có khung giờ nào!" });

                // Xóa khung giờ cũ của ngày đích
                var existingTarget = await _context.PriceSlots
                    .Where(ps => ps.CourtId == courtId && ps.DayOfWeek == toDayEnum)
                    .ToListAsync();
                _context.PriceSlots.RemoveRange(existingTarget);

                // Copy sang ngày đích
                foreach (var src in sourceSlots)
                {
                    _context.PriceSlots.Add(new PriceSlot
                    {
                        CourtId = courtId,
                        FacilityId = src.FacilityId,
                        DayOfWeek = toDayEnum,
                        CourtType = src.CourtType,
                        StartTime = src.StartTime,
                        EndTime = src.EndTime,
                        Price = src.Price,
                        IsActive = src.IsActive
                    });
                }

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"Đã sao chép {sourceSlots.Count} khung giờ từ {GetDayLabel(fromDay)} sang {GetDayLabel(toDay)}!"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // ══════════════════════════════════════════════════════════
        //  AJAX APIs (GIỮ NGUYÊN)
        // ══════════════════════════════════════════════════════════

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
                    surfaceType = court.SurfaceType,
                    facilityId = court.FacilityId,
                    status = court.Status,
                    floorNumber = court.FloorNumber,
                    hasLighting = court.HasLighting,
                    hasAC = court.HasAC,
                    description = court.Description,
                    imagePath = primary?.ImagePath ?? court.ImagePath
                }
            });
        }

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

        [HttpPost]
        public async Task<IActionResult> CreateAjax([FromBody] Court model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.CourtNumber))
                    return Json(new { success = false, message = "Vui lòng nhập tên sân!" });

                if (model.FacilityId <= 0)
                    return Json(new { success = false, message = "Vui lòng chọn cơ sở!" });

                var (canAdd, current, max) = await CheckCapacityAsync(model.FacilityId);
                if (!canAdd)
                    return Json(new
                    {
                        success = false,
                        message = $"Cơ sở này đã đạt giới hạn tối đa {max} sân ({current} sân). Không thể thêm."
                    });

                var isExist = await _context.Courts.AnyAsync(c =>
                    c.FacilityId == model.FacilityId && c.CourtNumber == model.CourtNumber);

                if (isExist)
                    return Json(new { success = false, message = "Tên sân này đã tồn tại ở cơ sở này!" });

                model.CreatedAt = DateTime.Now;
                model.Status ??= "Available";

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
                    var (canAdd, _, max) =
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
                    return Json(new { success = false, message = "Tên sân này đã tồn tại ở cơ sở này!" });

                court.FacilityId = model.FacilityId;
                court.CourtNumber = model.CourtNumber;
                court.CourtType = model.CourtType;
                court.SurfaceType = model.SurfaceType;
                court.Status = model.Status;
                court.FloorNumber = model.FloorNumber;
                court.HasLighting = model.HasLighting;
                court.HasAC = model.HasAC;
                court.Description = model.Description;

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

            if (court.Bookings?.Any() == true)
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

        [HttpGet]
        public async Task<IActionResult> GetByFacility(int facilityId)
        {
            var courts = await _context.Courts
                .Where(c => c.FacilityId == facilityId && c.Status != "Maintenance")
                .Select(c => new
                {
                    c.CourtId,
                    c.CourtNumber,
                    c.CourtType,
                    c.SurfaceType,
                    c.Status
                })
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

        // ══════════════════════════════════════════════════════════
        //  PRIVATE HELPERS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Chuyển số ngày → tên tiếng Việt
        /// 0=Chủ nhật, 1=Thứ 2 ... 6=Thứ 7 (theo DayOfWeek chuẩn .NET)
        /// </summary>
        private static string GetDayLabel(int day) => day switch
        {
            0 => "Chủ nhật",
            1 => "Thứ Hai",
            2 => "Thứ Ba",
            3 => "Thứ Tư",
            4 => "Thứ Năm",
            5 => "Thứ Sáu",
            6 => "Thứ Bảy",
            _ => $"Ngày {day}"
        };

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
                using var stream = new FileStream(Path.Combine(folder, fileName), FileMode.Create);
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
