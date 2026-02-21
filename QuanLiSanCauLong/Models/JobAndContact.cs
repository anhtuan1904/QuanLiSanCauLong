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

        [Required]
        [StringLength(200)]
        public string JobTitle { get; set; } = string.Empty;

        [Required]
        [StringLength(250)]
        public string Slug { get; set; } = string.Empty;

        [Required]
        public string JobDescription { get; set; } = string.Empty;

        public string? Requirements { get; set; }
        public string? Benefits { get; set; }

        [StringLength(100)]
        public string? Location { get; set; }

        [StringLength(50)]
        public string? JobType { get; set; } // Full-time, Part-time, Contract

        [StringLength(50)]
        public string? Level { get; set; } // Intern, Junior, Senior, Manager

        [Column(TypeName = "decimal(18,2)")]
        public decimal? SalaryMin { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? SalaryMax { get; set; }

        public int VacancyCount { get; set; } = 1;

        public DateTime? Deadline { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "Open"; // Open, Closed, Filled

        public int ViewCount { get; set; } = 0;
        public bool IsFeatured { get; set; } = false;

        public string? ContactPerson { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Navigation
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

        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(15)]
        public string Phone { get; set; } = string.Empty;

        public DateTime? DateOfBirth { get; set; }

        [StringLength(200)]
        public string? Address { get; set; }

        [StringLength(100)]
        public string? CurrentPosition { get; set; }

        public int? YearsOfExperience { get; set; }

        [StringLength(100)]
        public string? Education { get; set; }

        public string? Skills { get; set; }

        public string? CoverLetter { get; set; }

        public string? CVFilePath { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? ExpectedSalary { get; set; }

        public DateTime? AvailableDate { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "New"; // New, Reviewing, Shortlisted, Interview, Rejected, Accepted

        public string? ReviewNotes { get; set; }
        public string? ReviewedBy { get; set; }
        public DateTime? ReviewedAt { get; set; }

        public int Rating { get; set; } = 0; // 0-5 stars

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Navigation
        public virtual JobPosting? Job { get; set; }
    }

    /// <summary>
    /// Liên hệ/Hỗ trợ
    /// </summary>
    public class ContactMessage
    {
        [Key]
        public int MessageId { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [StringLength(15)]
        public string? Phone { get; set; }

        [Required]
        [StringLength(200)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Category { get; set; } // Support, Feedback, Question, Complaint, Other

        [StringLength(20)]
        public string Status { get; set; } = "New"; // New, InProgress, Resolved, Closed

        [StringLength(10)]
        public string Priority { get; set; } = "Normal"; // Low, Normal, High, Urgent

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
