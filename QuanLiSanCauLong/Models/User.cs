using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLiSanCauLong.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Họ tên không được để trống")]
        [StringLength(100)]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(100)]
        public string Email { get; set; }

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [StringLength(20)]
        public string Phone { get; set; }

        [NotMapped]
        public string PhoneNumber { get => Phone; set => Phone = value; }

        // ✅ MỚI: Đường dẫn ảnh đại diện (lưu relative path, VD: /uploads/avatars/user_5.webp)
        [StringLength(500)]
        public string? AvatarUrl { get; set; }

        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        [Required]
        public string Role { get; set; } // Customer, Staff, Admin

        public string Status { get; set; } = "Active"; // Active, Locked, Deleted

        public int? FacilityId { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [ForeignKey("FacilityId")]
        public virtual Facility? Facility { get; set; }
        public virtual ICollection<Booking>? Bookings { get; set; }
        public virtual ICollection<Order>? Orders { get; set; }

        // ─── Helper: lấy initials nếu chưa có avatar ─────────────────────────
        [NotMapped]
        public string Initials => string.Join("",
            FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .TakeLast(2)
                    .Select(x => char.ToUpper(x[0])));
    }
}
