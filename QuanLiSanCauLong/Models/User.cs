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

        // CẦN THIÊT: Thuộc tính Password để khớp với View Create/Register (Sửa lỗi image_45c11e.png)
        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        [Required]
        public string Role { get; set; } // Customer, Staff, Admin

        // CẦN THIẾT: Thuộc tính Status để khớp với View Index/Create (Sửa lỗi image_55b353.png)
        public string Status { get; set; } = "Active"; // Active, Locked

        public int? FacilityId { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [ForeignKey("FacilityId")]
        public virtual Facility? Facility { get; set; }
        public virtual ICollection<Booking>? Bookings { get; set; }
        public virtual ICollection<Order>? Orders { get; set; }
    }
}