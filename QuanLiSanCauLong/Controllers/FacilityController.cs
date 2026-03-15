using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiSanCauLong.Data;
using QuanLiSanCauLong.Models;
using QuanLiSanCauLong.ViewModels;

namespace QuanLiSanCauLong.Controllers
{
    public class FacilityController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public FacilityController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: Facility/Index
        [HttpGet]
        public async Task<IActionResult> Index(string search, string city, string district, string sortBy)
        {
            var query = _context.Facilities
                .Include(f => f.Courts)
                .Where(f => f.IsActive)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(f => f.FacilityName.Contains(search) || f.Address.Contains(search));

            if (!string.IsNullOrEmpty(city))
                query = query.Where(f => f.City == city);

            if (!string.IsNullOrEmpty(district))
                query = query.Where(f => f.District == district);

            query = sortBy switch
            {
                "name_asc" => query.OrderBy(f => f.FacilityName),
                "name_desc" => query.OrderByDescending(f => f.FacilityName),
                "newest" => query.OrderByDescending(f => f.CreatedAt),
                _ => query.OrderBy(f => f.FacilityName)
            };

            var facilities = await query.ToListAsync();

            var model = facilities.Select(f => new FacilityCardViewModel
            {
                FacilityId = f.FacilityId,
                FacilityName = f.FacilityName,
                Address = f.Address,
                City = f.City,
                District = f.District,
                Phone = f.Phone,
                ImageUrl = f.ImageUrl ?? "/images/default-facility.jpg",
                TotalCourts = f.Courts.Count,
                OpenTime = f.OpenTime,
                CloseTime = f.CloseTime,
                Description = f.Description
            }).ToList();

            ViewBag.Cities = await _context.Facilities
                .Where(f => f.IsActive && !string.IsNullOrEmpty(f.City))
                .Select(f => f.City).Distinct().OrderBy(c => c).ToListAsync();

            ViewBag.Districts = await _context.Facilities
                .Where(f => f.IsActive && !string.IsNullOrEmpty(f.District))
                .Select(f => f.District).Distinct().OrderBy(d => d).ToListAsync();

            ViewBag.Search = search;
            ViewBag.SelectedCity = city;
            ViewBag.SelectedDistrict = district;
            ViewBag.SortBy = sortBy;

            return View(model);
        }

        // GET: Facility/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id, DateTime? date)
        {
            var facility = await _context.Facilities
                .Include(f => f.Courts)
                    .ThenInclude(c => c.PriceSlots)
                .Include(f => f.Courts)
                    .ThenInclude(c => c.CourtImages)
                .Include(f => f.Inventories)
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p.Category)
                .Include(f => f.FacilityImages)
                .FirstOrDefaultAsync(f => f.FacilityId == id);

            if (facility == null) return NotFound();

            var selectedDate = date?.Date ?? DateTime.Today;
            var dayOfWeek = selectedDate.DayOfWeek;

            var bookedSlots = await _context.Bookings
                .Where(b => b.Court.FacilityId == id
                         && b.BookingDate.Date == selectedDate
                         && b.Status != "Cancelled")
                .Select(b => new { b.CourtId, b.StartTime, b.EndTime })
                .ToListAsync();

            var courts = facility.Courts
                .Where(c => c.Status == "Available")
                .OrderBy(c => c.CourtNumber)
                .Select(c => new FacilityCourtViewModel
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
                    HourlyRate = c.HourlyRate,
                    Status = c.Status,
                    StatusLabel = c.StatusLabel,
                    Description = c.Description,
                    ImagePath = c.ImagePath,

                    // ✅ FIX: Map CourtImage → CourtImageViewModel
                    // CourtImage.PK = ImageId (không phải CourtImageId)
                    CourtImages = c.CourtImages?
                        .OrderBy(i => i.IsPrimary ? 0 : 1)
                        .ThenBy(i => i.DisplayOrder)
                        .Select(img => new CourtImageViewModel
                        {
                            CourtImageId = img.ImageId,      // ✅ ImageId là PK thật
                            CourtId = img.CourtId,
                            ImagePath = img.ImagePath,
                            IsPrimary = img.IsPrimary,
                            DisplayOrder = img.DisplayOrder,
                            Caption = img.Caption
                        })
                        .ToList(),

                    Slots = c.PriceSlots
                        .Where(p => p.IsActive && (p.DayOfWeek == null || p.DayOfWeek == dayOfWeek))
                        .OrderBy(p => p.StartTime)
                        .Select(p => new TimeSlotViewModel
                        {
                            StartTime = p.StartTime,
                            EndTime = p.EndTime,
                            Price = p.Price,
                            IsPeakHour = p.IsPeakHour,
                            IsAvailable = !bookedSlots.Any(b =>
                                b.CourtId == c.CourtId
                             && b.StartTime < p.EndTime
                             && b.EndTime > p.StartTime)
                        }).ToList()
                }).ToList();

            var model = new FacilityDetailsViewModel
            {
                FacilityId = facility.FacilityId,
                FacilityName = facility.FacilityName,
                Description = facility.Description,
                Address = facility.Address,
                District = facility.District,
                City = facility.City,
                Phone = facility.Phone,
                OpenTime = facility.OpenTime,
                CloseTime = facility.CloseTime,
                SelectedDate = selectedDate,
                ImageUrls = facility.FacilityImages.Any()
                    ? facility.FacilityImages.OrderBy(i => i.DisplayOrder).Select(i => i.ImagePath).ToList()
                    : new List<string> { facility.ImageUrl ?? "/images/default-facility.jpg" },
                Courts = courts,
                TotalCourts = facility.Courts.Count,
                MinPrice = facility.Courts.SelectMany(c => c.PriceSlots).Any()
                               ? facility.Courts.SelectMany(c => c.PriceSlots).Min(p => p.Price) : 0,
                MaxPrice = facility.Courts.SelectMany(c => c.PriceSlots).Any()
                               ? facility.Courts.SelectMany(c => c.PriceSlots).Max(p => p.Price) : 0,
                Amenities = string.IsNullOrEmpty(facility.Amenities)
                    ? new List<FacilityAmenityViewModel>
                      {
                          new() {
                              AmenityName = "Canteen",
                              IsAvailable = facility.Inventories != null && facility.Inventories.Any(i => i.Quantity > 0),
                              Icon        = "fa-utensils"
                          }
                      }
                    : facility.Amenities
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(a => new FacilityAmenityViewModel
                        {
                            AmenityName = a.Trim(),
                            IsAvailable = true,
                            Icon = "fa-check"
                        })
                        .ToList()
            };

            // Review section
            int? currentUserId = null;
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int uid))
                currentUserId = uid;

            var reviewSvc = new ReviewController(_context, _env);
            model.ReviewSection = await reviewSvc.BuildSectionVm(id, currentUserId);
            model.TotalReviews = model.ReviewSection.TotalCount;
            model.AverageRating = model.ReviewSection.TotalCount > 0
                                   ? model.ReviewSection.AverageRating : (double?)null;

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return PartialView("_Details", model);

            return View(model);
        }

        // GET: Facility/GetAvailableCourts
        [HttpGet]
        public async Task<IActionResult> GetAvailableCourts(int facilityId, DateTime date, TimeSpan? startTime)
        {
            var courts = await _context.Courts
                .Include(c => c.PriceSlots)
                .Where(c => c.FacilityId == facilityId && c.Status == "Available")
                .ToListAsync();

            var bookedCourts = await _context.Bookings
                .Where(b => b.Court.FacilityId == facilityId
                         && b.BookingDate == date
                         && b.Status != "Cancelled")
                .Select(b => new { b.CourtId, b.StartTime, b.EndTime })
                .ToListAsync();

            var result = courts.Select(c => new
            {
                courtId = c.CourtId,
                courtNumber = c.CourtNumber,
                courtType = c.CourtType,
                minPrice = c.PriceSlots.Any() ? c.PriceSlots.Min(p => p.Price) : 0,
                maxPrice = c.PriceSlots.Any() ? c.PriceSlots.Max(p => p.Price) : 0,
                isAvailable = startTime.HasValue
                    ? !bookedCourts.Any(b => b.CourtId == c.CourtId
                                          && b.StartTime < startTime.Value.Add(TimeSpan.FromHours(1))
                                          && b.EndTime > startTime.Value)
                    : true
            }).ToList();

            return Json(result);
        }

        // GET: Facility/GetTimeSlots
        [HttpGet]
        public async Task<IActionResult> GetTimeSlots(int courtId, DateTime date)
        {
            var court = await _context.Courts
                .Include(c => c.PriceSlots)
                .FirstOrDefaultAsync(c => c.CourtId == courtId);

            if (court == null)
                return Json(new { success = false, message = "Không tìm thấy sân!" });

            var targetDate = date.Date;
            var dayOfWeek = targetDate.DayOfWeek;

            var bookedSlots = await _context.Bookings
                .Where(b => b.CourtId == courtId
                         && b.BookingDate.Date == targetDate
                         && b.Status != "Cancelled")
                .Select(b => new { b.StartTime, b.EndTime })
                .ToListAsync();

            var timeSlots = court.PriceSlots
                .Where(p => p.IsActive && (p.DayOfWeek == null || p.DayOfWeek == dayOfWeek))
                .OrderBy(p => p.StartTime)
                .Select(p => new
                {
                    startTime = p.StartTime.ToString(@"hh\:mm"),
                    endTime = p.EndTime.ToString(@"hh\:mm"),
                    price = p.Price,
                    isPeakHour = p.IsPeakHour,
                    isAvailable = !bookedSlots.Any(b => b.StartTime < p.EndTime && b.EndTime > p.StartTime)
                }).ToList();

            return Json(new { success = true, timeSlots });
        }
    }
}
