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

        public FacilityController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Facility/Index
        [HttpGet]
        public async Task<IActionResult> Index(string search, string city, string district, string sortBy)
        {
            var query = _context.Facilities
                .Include(f => f.Courts)
                .Where(f => f.IsActive)
                .AsQueryable();

            // Search
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(f => f.FacilityName.Contains(search)
                                      || f.Address.Contains(search));
            }

            // Filter by City
            if (!string.IsNullOrEmpty(city))
            {
                query = query.Where(f => f.City == city);
            }

            // Filter by District
            if (!string.IsNullOrEmpty(district))
            {
                query = query.Where(f => f.District == district);
            }

            // Sorting
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
                TotalCourts = f.Courts.Count(c => c.Status == "Available"),
                OpenTime = f.OpenTime,
                CloseTime = f.CloseTime,
                Description = f.Description
            }).ToList();

            // ViewBag for filters
            ViewBag.Cities = await _context.Facilities
                .Where(f => f.IsActive && !string.IsNullOrEmpty(f.City))
                .Select(f => f.City)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            ViewBag.Districts = await _context.Facilities
                .Where(f => f.IsActive && !string.IsNullOrEmpty(f.District))
                .Select(f => f.District)
                .Distinct()
                .OrderBy(d => d)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.SelectedCity = city;
            ViewBag.SelectedDistrict = district;
            ViewBag.SortBy = sortBy;

            return View(model);
        }

        // GET: Facility/Details/5
        [HttpGet]
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            // 1. Sửa câu lệnh truy vấn: Đi xuyên qua Inventories để đến Product
            var facility = await _context.Facilities
                .Include(f => f.Courts)
                    .ThenInclude(c => c.PriceSlots)
                .Include(f => f.Inventories) 
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p.Category)
                .FirstOrDefaultAsync(f => f.FacilityId == id && f.IsActive);

            if (facility == null) return NotFound();

            var model = new FacilityDetailsViewModel
            {
                FacilityId = facility.FacilityId,
                FacilityName = facility.FacilityName,
                Description = facility.Description,
                Address = facility.Address,
                District = facility.District,
                City = facility.City,
                Phone = facility.Phone,
                Email = facility.Email,
                ImageUrl = new List<string> { facility.ImageUrl ?? "/images/default-facility.jpg" },
                OpenTime = facility.OpenTime,
                CloseTime = facility.CloseTime,

                Courts = facility.Courts
                    .Where(c => c.Status == "Available")
                    .OrderBy(c => c.CourtNumber)
                    .Select(c => new FacilityCourtViewModel
                    {
                        CourtId = c.CourtId,
                        CourtNumber = c.CourtNumber,
                        CourtType = c.CourtType,
                        Status = c.Status,
                        Description = "Sân tiêu chuẩn"
                    }).ToList(),

                TotalCourts = facility.Courts.Count(c => c.Status == "Available"),

                MinPrice = facility.Courts.SelectMany(c => c.PriceSlots).Any()
                           ? facility.Courts.SelectMany(c => c.PriceSlots).Min(p => p.Price) : 0,
                MaxPrice = facility.Courts.SelectMany(c => c.PriceSlots).Any()
                           ? facility.Courts.SelectMany(c => c.PriceSlots).Max(p => p.Price) : 0,

                Amenities = new List<FacilityAmenityViewModel>
        {
            new FacilityAmenityViewModel { AmenityName = "Bãi đỗ xe", IsAvailable = true, Icon = "fa-car" },
            new FacilityAmenityViewModel { AmenityName = "Wifi", IsAvailable = true, Icon = "fa-wifi" },
            // 2. Sửa kiểm tra Canteen: Kiểm tra trong danh sách Inventories
            new FacilityAmenityViewModel {
                AmenityName = "Canteen",
                IsAvailable = facility.Inventories != null && facility.Inventories.Any(i => i.Quantity > 0),
                Icon = "fa-utensils"
            }
        }
            };

            return View(model);
        }        // GET: Facility/GetAvailableCourts
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

            var availableCourts = courts.Select(c => new
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

            return Json(availableCourts);
        }

        // GET: Facility/GetTimeSlots
[HttpGet]
public async Task<IActionResult> GetTimeSlots(int courtId, DateTime date)
{
    var court = await _context.Courts
        .Include(c => c.PriceSlots)
        .FirstOrDefaultAsync(c => c.CourtId == courtId);

    if (court == null) return Json(new { success = false, message = "Không tìm thấy sân!" });

    // Lấy ngày chuẩn không chứa giờ
    var targetDate = date.Date;
    DayOfWeek dayOfWeekEnum = targetDate.DayOfWeek;

    var bookedSlots = await _context.Bookings
        .Where(b => b.CourtId == courtId
               && b.BookingDate.Date == targetDate
               && b.Status != "Cancelled")
        .Select(b => new { b.StartTime, b.EndTime })
        .ToListAsync();

    var timeSlots = court.PriceSlots
        .Where(p => p.IsActive && (p.DayOfWeek == null || p.DayOfWeek == dayOfWeekEnum))
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
}    }
}