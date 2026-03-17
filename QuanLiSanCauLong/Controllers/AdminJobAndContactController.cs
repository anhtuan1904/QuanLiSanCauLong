using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLiSanCauLong.Data;
using QuanLiSanCauLong.Models;

namespace QuanLiSanCauLong.Controllers
{
    // ============================================================
    //  ADMIN – TIN TUYỂN DỤNG
    // ============================================================
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

        // ----------------------------------------------------------
        //  Index – danh sách + thống kê
        // ----------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> Index(string? search, string? status, string? jobType, string? department)
        {
            var query = _context.JobPostings.Include(j => j.Applications).AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(j => j.JobTitle.Contains(search) || j.JobDescription.Contains(search));
            if (!string.IsNullOrEmpty(status))
                query = query.Where(j => j.Status == status);
            if (!string.IsNullOrEmpty(jobType))
                query = query.Where(j => j.JobType == jobType);
            if (!string.IsNullOrEmpty(department))
                query = query.Where(j => j.Department == department);

            var jobs = await query.OrderByDescending(j => j.IsFeatured)
                                  .ThenByDescending(j => j.CreatedAt)
                                  .ToListAsync();

            // Stats cho ViewBag
            ViewBag.TotalJobs = await _context.JobPostings.CountAsync();
            ViewBag.OpenJobs = await _context.JobPostings.CountAsync(j => j.Status == "Open");
            ViewBag.TotalApps = await _context.JobApplications.CountAsync();
            ViewBag.NewApps = await _context.JobApplications.CountAsync(a => a.Status == "New");
            ViewBag.TotalVacancy = await _context.JobPostings.Where(j => j.Status == "Open").SumAsync(j => j.VacancyCount);

            return View(jobs);
        }

        // ----------------------------------------------------------
        //  Create
        // ----------------------------------------------------------
        [HttpGet]
        public IActionResult Create() => PartialView();

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

        // ----------------------------------------------------------
        //  Edit
        // ----------------------------------------------------------
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
                        job.Department = model.Department;
                        job.SalaryMin = model.SalaryMin;
                        job.SalaryMax = model.SalaryMax;
                        job.SalaryType = model.SalaryType;
                        job.VacancyCount = model.VacancyCount;
                        job.Deadline = model.Deadline;
                        job.Status = model.Status;
                        job.IsFeatured = model.IsFeatured;
                        job.IsUrgent = model.IsUrgent;
                        job.ContactPerson = model.ContactPerson;
                        job.ContactEmail = model.ContactEmail;
                        job.ContactPhone = model.ContactPhone;
                        job.MetaDescription = model.MetaDescription;
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

        // ----------------------------------------------------------
        //  Details
        // ----------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var job = await _context.JobPostings
                .Include(j => j.Applications)
                .FirstOrDefaultAsync(j => j.JobId == id);
            if (job == null) return NotFound();
            return PartialView(job);
        }

        // ----------------------------------------------------------
        //  Delete
        // ----------------------------------------------------------
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

        // ----------------------------------------------------------
        //  Quick toggle status (AJAX)
        // ----------------------------------------------------------
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id, string status)
        {
            var job = await _context.JobPostings.FindAsync(id);
            if (job == null) return Json(new { success = false });

            job.Status = status;
            job.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return Json(new { success = true, status });
        }

        // ============================================================
        //  HỒ SƠ ỨNG TUYỂN
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> Applications(int? jobId, string? status, string? search)
        {
            var query = _context.JobApplications
                .Include(a => a.Job)
                .AsQueryable();

            if (jobId.HasValue) query = query.Where(a => a.JobId == jobId);
            if (!string.IsNullOrEmpty(status)) query = query.Where(a => a.Status == status);
            if (!string.IsNullOrEmpty(search))
                query = query.Where(a => a.FullName.Contains(search) || a.Email.Contains(search) || a.Phone.Contains(search));

            var applications = await query.OrderByDescending(a => a.CreatedAt).ToListAsync();

            ViewBag.Jobs = await _context.JobPostings.Where(j => j.Status == "Open").ToListAsync();
            ViewBag.JobId = jobId;
            ViewBag.Status = status;

            // Stats
            ViewBag.StatNew = await _context.JobApplications.CountAsync(a => a.Status == "New");
            ViewBag.StatReviewing = await _context.JobApplications.CountAsync(a => a.Status == "Reviewing");
            ViewBag.StatShortlisted = await _context.JobApplications.CountAsync(a => a.Status == "Shortlisted");
            ViewBag.StatInterview = await _context.JobApplications.CountAsync(a => a.Status == "Interview");
            ViewBag.StatAccepted = await _context.JobApplications.CountAsync(a => a.Status == "Accepted");
            ViewBag.StatRejected = await _context.JobApplications.CountAsync(a => a.Status == "Rejected");

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
        public async Task<IActionResult> UpdateApplicationStatus(int id, string status, int rating, string? notes, string? interviewDate, string? interviewNote)
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
                if (!string.IsNullOrEmpty(interviewDate)) application.InterviewDate = interviewDate;
                if (!string.IsNullOrEmpty(interviewNote)) application.InterviewNote = interviewNote;

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Cập nhật trạng thái thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // ----------------------------------------------------------
        //  Helper
        // ----------------------------------------------------------
        private static string GenerateSlug(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";

            // Bảng chuyển đổi tiếng Việt đầy đủ
            var map = new Dictionary<string, string>
            {
                {"à","a"},{"á","a"},{"ả","a"},{"ã","a"},{"ạ","a"},
                {"ă","a"},{"ằ","a"},{"ắ","a"},{"ẳ","a"},{"ẵ","a"},{"ặ","a"},
                {"â","a"},{"ầ","a"},{"ấ","a"},{"ẩ","a"},{"ẫ","a"},{"ậ","a"},
                {"đ","d"},
                {"è","e"},{"é","e"},{"ẻ","e"},{"ẽ","e"},{"ẹ","e"},
                {"ê","e"},{"ề","e"},{"ế","e"},{"ể","e"},{"ễ","e"},{"ệ","e"},
                {"ì","i"},{"í","i"},{"ỉ","i"},{"ĩ","i"},{"ị","i"},
                {"ò","o"},{"ó","o"},{"ỏ","o"},{"õ","o"},{"ọ","o"},
                {"ô","o"},{"ồ","o"},{"ố","o"},{"ổ","o"},{"ỗ","o"},{"ộ","o"},
                {"ơ","o"},{"ờ","o"},{"ớ","o"},{"ở","o"},{"ỡ","o"},{"ợ","o"},
                {"ù","u"},{"ú","u"},{"ủ","u"},{"ũ","u"},{"ụ","u"},
                {"ư","u"},{"ừ","u"},{"ứ","u"},{"ử","u"},{"ữ","u"},{"ự","u"},
                {"ỳ","y"},{"ý","y"},{"ỷ","y"},{"ỹ","y"},{"ỵ","y"},
            };

            var result = text.ToLower();
            foreach (var kv in map) result = result.Replace(kv.Key, kv.Value);

            result = System.Text.RegularExpressions.Regex.Replace(result, @"[^a-z0-9\s-]", "");
            result = System.Text.RegularExpressions.Regex.Replace(result, @"\s+", "-");
            result = System.Text.RegularExpressions.Regex.Replace(result, @"-+", "-");
            return result.Trim('-');
        }
    }

    // ============================================================
    //  ADMIN – LIÊN HỆ
    // ============================================================
    [Authorize(Roles = "Admin")]
    public class AdminContactController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminContactController(ApplicationDbContext context) => _context = context;

        [HttpGet]
        public async Task<IActionResult> Index(string? search, string? status, string? priority, string? category)
        {
            var query = _context.ContactMessages.AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(c => c.FullName.Contains(search) || c.Email.Contains(search) || c.Subject.Contains(search));
            if (!string.IsNullOrEmpty(status)) query = query.Where(c => c.Status == status);
            if (!string.IsNullOrEmpty(priority)) query = query.Where(c => c.Priority == priority);
            if (!string.IsNullOrEmpty(category)) query = query.Where(c => c.Category == category);

            var messages = await query.OrderByDescending(c => c.CreatedAt).ToListAsync();

            ViewBag.UnreadCount = await _context.ContactMessages.CountAsync(c => !c.IsRead);
            ViewBag.StarredCount = await _context.ContactMessages.CountAsync(c => c.IsStarred);
            ViewBag.UrgentCount = await _context.ContactMessages.CountAsync(c => c.Priority == "Urgent" && c.Status == "New");

            return View(messages);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var message = await _context.ContactMessages.FindAsync(id);
            if (message == null) return NotFound();

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
            if (message == null) return Json(new { success = false });

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

    // ============================================================
    //  PUBLIC – Ứng tuyển (người dùng)
    // ============================================================
    public class JobController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public JobController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        /// <summary>Trang danh sách việc làm công khai</summary>
        [HttpGet]
        public async Task<IActionResult> Index(string? search, string? jobType, string? level)
        {
            var query = _context.JobPostings
                .Where(j => j.Status == "Open")
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(j => j.JobTitle.Contains(search) || j.Department!.Contains(search));
            if (!string.IsNullOrEmpty(jobType)) query = query.Where(j => j.JobType == jobType);
            if (!string.IsNullOrEmpty(level)) query = query.Where(j => j.Level == level);

            var jobs = await query.OrderByDescending(j => j.IsFeatured)
                                  .ThenByDescending(j => j.CreatedAt)
                                  .ToListAsync();
            return View(jobs);
        }

        /// <summary>Trang chi tiết việc làm + form ứng tuyển</summary>
        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            var job = await _context.JobPostings.FindAsync(id);
            if (job == null || job.Status != "Open") return NotFound();

            job.ViewCount++;
            await _context.SaveChangesAsync();
            return View(job);
        }

        /// <summary>Xử lý nộp hồ sơ</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(JobApplication model, IFormFile? cvFile)
        {
            var job = await _context.JobPostings.FindAsync(model.JobId);
            if (job == null || job.Status != "Open")
                return RedirectToAction("Index");

            ModelState.Remove("Job");
            ModelState.Remove("CVFilePath");
            ModelState.Remove("CVFileName");

            if (ModelState.IsValid)
            {
                try
                {
                    // Upload CV
                    if (cvFile != null && cvFile.Length > 0)
                    {
                        var allowed = new[] { ".pdf", ".doc", ".docx" };
                        var ext = Path.GetExtension(cvFile.FileName).ToLower();
                        if (!allowed.Contains(ext))
                        {
                            ModelState.AddModelError("CVFilePath", "Chỉ chấp nhận file PDF, DOC, DOCX.");
                            return View("Detail", job);
                        }
                        if (cvFile.Length > 5 * 1024 * 1024)
                        {
                            ModelState.AddModelError("CVFilePath", "File không được vượt quá 5MB.");
                            return View("Detail", job);
                        }

                        var uploadDir = Path.Combine(_environment.WebRootPath, "uploads", "cv");
                        Directory.CreateDirectory(uploadDir);
                        var fileName = $"{Guid.NewGuid()}{ext}";
                        var filePath = Path.Combine(uploadDir, fileName);
                        await using var stream = new FileStream(filePath, FileMode.Create);
                        await cvFile.CopyToAsync(stream);

                        model.CVFilePath = $"/uploads/cv/{fileName}";
                        model.CVFileName = cvFile.FileName;
                    }

                    model.Status = "New";
                    model.CreatedAt = DateTime.Now;
                    model.UpdatedAt = DateTime.Now;

                    _context.JobApplications.Add(model);
                    await _context.SaveChangesAsync();

                    TempData["ApplySuccess"] = "Nộp hồ sơ thành công! Chúng tôi sẽ liên hệ bạn sớm nhất.";
                    return RedirectToAction("Detail", new { id = model.JobId });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi hệ thống: " + ex.Message);
                }
            }

            return View("Detail", job);
        }
    }
}
