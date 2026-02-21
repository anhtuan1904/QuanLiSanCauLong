using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiSanCauLong.Data;
using QuanLiSanCauLong.Models;

namespace QuanLiSanCauLong.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminJobController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public AdminJobController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // ============ JOB POSTINGS ============
        [HttpGet]
        public async Task<IActionResult> Index(string search, string status)
        {
            var query = _context.JobPostings.AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(j => j.JobTitle.Contains(search) || j.JobDescription.Contains(search));

            if (!string.IsNullOrEmpty(status))
                query = query.Where(j => j.Status == status);

            var jobs = await query.OrderByDescending(j => j.CreatedAt).ToListAsync();
            return View(jobs);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return PartialView();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(JobPosting model)
        {
            ModelState.Remove("Applications");

            if (ModelState.IsValid)
            {
                try
                {
                    model.Slug = GenerateSlug(model.JobTitle);
                    model.CreatedAt = DateTime.Now;
                    model.UpdatedAt = DateTime.Now;

                    _context.JobPostings.Add(model);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Thêm tin tuyển dụng thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi: " + ex.Message);
                }
            }
            return PartialView(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var job = await _context.JobPostings.FindAsync(id);
            if (job == null) return NotFound();
            return PartialView(job);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(JobPosting model)
        {
            ModelState.Remove("Applications");

            if (ModelState.IsValid)
            {
                var job = await _context.JobPostings.FindAsync(model.JobId);
                if (job != null)
                {
                    try
                    {
                        job.JobTitle = model.JobTitle;
                        job.Slug = GenerateSlug(model.JobTitle);
                        job.JobDescription = model.JobDescription;
                        job.Requirements = model.Requirements;
                        job.Benefits = model.Benefits;
                        job.Location = model.Location;
                        job.JobType = model.JobType;
                        job.Level = model.Level;
                        job.SalaryMin = model.SalaryMin;
                        job.SalaryMax = model.SalaryMax;
                        job.VacancyCount = model.VacancyCount;
                        job.Deadline = model.Deadline;
                        job.Status = model.Status;
                        job.IsFeatured = model.IsFeatured;
                        job.ContactPerson = model.ContactPerson;
                        job.ContactEmail = model.ContactEmail;
                        job.ContactPhone = model.ContactPhone;
                        job.UpdatedAt = DateTime.Now;

                        await _context.SaveChangesAsync();
                        TempData["SuccessMessage"] = "Cập nhật tin tuyển dụng thành công!";
                        return RedirectToAction(nameof(Index));
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("", "Lỗi: " + ex.Message);
                    }
                }
            }
            return PartialView(model);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var job = await _context.JobPostings
                .Include(j => j.Applications)
                .FirstOrDefaultAsync(j => j.JobId == id);

            if (job == null)
                return Json(new { success = false, message = "Không tìm thấy tin tuyển dụng!" });

            try
            {
                _context.JobPostings.Remove(job);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Xóa tin tuyển dụng thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // ============ JOB APPLICATIONS ============
        [HttpGet]
        public async Task<IActionResult> Applications(int? jobId, string status)
        {
            var query = _context.JobApplications
                .Include(a => a.Job)
                .AsQueryable();

            if (jobId.HasValue)
                query = query.Where(a => a.JobId == jobId);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(a => a.Status == status);

            var applications = await query.OrderByDescending(a => a.CreatedAt).ToListAsync();
            ViewBag.Jobs = await _context.JobPostings.Where(j => j.Status == "Open").ToListAsync();
            return View(applications);
        }

        [HttpGet]
        public async Task<IActionResult> ApplicationDetail(int id)
        {
            var application = await _context.JobApplications
                .Include(a => a.Job)
                .FirstOrDefaultAsync(a => a.ApplicationId == id);

            if (application == null) return NotFound();
            return PartialView(application);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateApplicationStatus(int id, string status, int rating, string? notes)
        {
            var application = await _context.JobApplications.FindAsync(id);
            if (application == null)
                return Json(new { success = false, message = "Không tìm thấy hồ sơ!" });

            try
            {
                application.Status = status;
                application.Rating = rating;
                application.ReviewNotes = notes;
                application.ReviewedAt = DateTime.Now;
                application.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Cập nhật trạng thái thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        private string GenerateSlug(string text)
        {
            return text.ToLower()
                .Replace("đ", "d")
                .Replace("á", "a").Replace("à", "a").Replace("ả", "a").Replace("ã", "a").Replace("ạ", "a")
                .Replace(" ", "-")
                .Replace("---", "-").Replace("--", "-");
        }
    }

    // ============ CONTACT CONTROLLER ============
    [Authorize(Roles = "Admin")]
    public class AdminContactController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminContactController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string search, string status, string priority, string category)
        {
            var query = _context.ContactMessages.AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(c => c.FullName.Contains(search) || c.Email.Contains(search) || c.Subject.Contains(search));

            if (!string.IsNullOrEmpty(status))
                query = query.Where(c => c.Status == status);

            if (!string.IsNullOrEmpty(priority))
                query = query.Where(c => c.Priority == priority);

            if (!string.IsNullOrEmpty(category))
                query = query.Where(c => c.Category == category);

            var messages = await query.OrderByDescending(c => c.CreatedAt).ToListAsync();
            return View(messages);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var message = await _context.ContactMessages.FindAsync(id);
            if (message == null) return NotFound();

            // Mark as read
            if (!message.IsRead)
            {
                message.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return PartialView(message);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, string status, string? response)
        {
            var message = await _context.ContactMessages.FindAsync(id);
            if (message == null)
                return Json(new { success = false, message = "Không tìm thấy tin nhắn!" });

            try
            {
                message.Status = status;
                message.ResponseMessage = response;
                message.UpdatedAt = DateTime.Now;

                if (status == "Resolved" && !message.RespondedAt.HasValue)
                    message.RespondedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Cập nhật trạng thái thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ToggleStar(int id)
        {
            var message = await _context.ContactMessages.FindAsync(id);
            if (message == null)
                return Json(new { success = false });

            message.IsStarred = !message.IsStarred;
            await _context.SaveChangesAsync();

            return Json(new { success = true, isStarred = message.IsStarred });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var message = await _context.ContactMessages.FindAsync(id);
            if (message == null)
                return Json(new { success = false, message = "Không tìm thấy tin nhắn!" });

            try
            {
                _context.ContactMessages.Remove(message);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Xóa tin nhắn thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }
    }
}
