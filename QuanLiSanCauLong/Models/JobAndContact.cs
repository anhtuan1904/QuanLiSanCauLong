using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLiSanCauLong.Models
{
    /// <summary>
    /// Tin tuyển dụng
    /// </summary>
    public class JobPosting
    {
        [Key]
        public int JobId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên vị trí")]
        [StringLength(200)]
        public string JobTitle { get; set; } = string.Empty;

        [StringLength(250)]
        public string Slug { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mô tả công việc")]
        public string JobDescription { get; set; } = string.Empty;

        public string? Requirements { get; set; }
        public string? Benefits { get; set; }

        [StringLength(150)]
        public string? Location { get; set; }

        /// <summary>Full-time | Part-time | Contract | Internship | Remote | Hybrid</summary>
        [StringLength(50)]
        public string? JobType { get; set; }

        /// <summary>Intern | Junior | Middle | Senior | Lead | Manager | Director</summary>
        [StringLength(50)]
        public string? Level { get; set; }

        /// <summary>Bộ phận: Vận hành, Kỹ thuật, Marketing, Kế toán, IT, Khác</summary>
        [StringLength(100)]
        public string? Department { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? SalaryMin { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? SalaryMax { get; set; }

        /// <summary>Fixed | Negotiable | Competitive</summary>
        [StringLength(30)]
        public string SalaryType { get; set; } = "Fixed";

        public int VacancyCount { get; set; } = 1;
        public DateTime? Deadline { get; set; }

        /// <summary>Open | Closed | Filled | Draft</summary>
        [StringLength(20)]
        public string Status { get; set; } = "Open";

        public int ViewCount { get; set; } = 0;
        public bool IsFeatured { get; set; } = false;
        public bool IsUrgent { get; set; } = false;

        public string? ContactPerson { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }

        [StringLength(500)]
        public string? MetaDescription { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public virtual ICollection<JobApplication>? Applications { get; set; }
    }

    /// <summary>
    /// Hồ sơ ứng tuyển
    /// </summary>
    public class JobApplication
    {
        [Key]
        public int ApplicationId { get; set; }

        [Required]
        public int JobId { get; set; }

        // ===== Thông tin cá nhân =====
        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [StringLength(100)]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [StringLength(15)]
        public string Phone { get; set; } = string.Empty;

        public DateTime? DateOfBirth { get; set; }

        /// <summary>Nam | Nữ | Khác</summary>
        [StringLength(10)]
        public string? Gender { get; set; }

        [StringLength(200)]
        public string? Address { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        // ===== Học vấn =====
        /// <summary>THPT | Cao đẳng | Đại học | Thạc sĩ | Tiến sĩ | Khác</summary>
        [StringLength(50)]
        public string? EducationLevel { get; set; }

        [StringLength(200)]
        public string? University { get; set; }

        [StringLength(100)]
        public string? Major { get; set; }

        /// <summary>Xuất sắc | Giỏi | Khá | Trung bình</summary>
        [StringLength(30)]
        public string? GraduationRank { get; set; }

        public int? GraduationYear { get; set; }

        // ===== Kinh nghiệm =====
        public int? YearsOfExperience { get; set; }

        [StringLength(150)]
        public string? CurrentPosition { get; set; }

        [StringLength(150)]
        public string? CurrentCompany { get; set; }

        // ===== Kỹ năng =====
        public string? TechnicalSkills { get; set; }  // Kỹ năng chuyên môn
        public string? SoftSkills { get; set; }        // Kỹ năng mềm
        public string? Languages { get; set; }         // Ngoại ngữ
        public string? Certificates { get; set; }      // Chứng chỉ

        // ===== Hồ sơ =====
        public string? CVFilePath { get; set; }
        public string? CVFileName { get; set; }

        [StringLength(300)]
        public string? PortfolioUrl { get; set; }

        [StringLength(300)]
        public string? LinkedInUrl { get; set; }

        // ===== Kỳ vọng =====
        public string? CoverLetter { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? ExpectedSalary { get; set; }

        public DateTime? AvailableDate { get; set; }

        /// <summary>Website | Facebook | LinkedIn | Giới thiệu | Khác</summary>
        [StringLength(50)]
        public string? ReferralSource { get; set; }

        public string? AdditionalInfo { get; set; }

        // ===== Admin =====
        /// <summary>New | Reviewing | Shortlisted | Interview | Rejected | Accepted</summary>
        [StringLength(20)]
        public string Status { get; set; } = "New";

        public string? ReviewNotes { get; set; }
        public string? ReviewedBy { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public int Rating { get; set; } = 0;
        public string? InterviewDate { get; set; }
        public string? InterviewNote { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public virtual JobPosting? Job { get; set; }
    }

    /// <summary>
    /// Liên hệ / Hỗ trợ
    /// </summary>
    public class ContactMessage
    {
        [Key]
        public int MessageId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [StringLength(100)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [StringLength(15)]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tiêu đề")]
        [StringLength(200)]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập nội dung")]
        public string Message { get; set; } = string.Empty;

        /// <summary>Support | Feedback | Question | Complaint | Booking | Other</summary>
        [StringLength(50)]
        public string? Category { get; set; }

        /// <summary>New | InProgress | Resolved | Closed</summary>
        [StringLength(20)]
        public string Status { get; set; } = "New";

        /// <summary>Low | Normal | High | Urgent</summary>
        [StringLength(10)]
        public string Priority { get; set; } = "Normal";

        public string? ResponseMessage { get; set; }
        public string? AssignedTo { get; set; }
        public DateTime? RespondedAt { get; set; }

        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }

        public bool IsRead { get; set; } = false;
        public bool IsStarred { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
