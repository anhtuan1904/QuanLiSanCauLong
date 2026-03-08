using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiSanCauLong.Data;

namespace QuanLiSanCauLong.Controllers
{
    public class CourtController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CourtController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Court/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var court = await _context.Courts
                .Include(c => c.Facility)
                .FirstOrDefaultAsync(c => c.CourtId == id);

            if (court == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy sân!";
                ViewBag.Court = null;
                return View();
            }

            ViewBag.CourtId = court.CourtId;
            ViewBag.Court = court;
            return View();
        }
    }
}
