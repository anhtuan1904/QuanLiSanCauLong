using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLiSanCauLong.Models
{
    public class SystemSetting
    {
        [Key]
        public int SettingId { get; set; }

        [Required]
        [StringLength(100)]
        public string SettingKey { get; set; }

        public string SettingValue { get; set; }

        [StringLength(50)]
        public string SettingType { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [StringLength(50)]
        public string Category { get; set; }

        public bool IsActive { get; set; } = true;

        // Bổ sung các trường này để sửa lỗi CS1061 trong Controller
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public int? CreatedBy { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public int? UpdatedBy { get; set; }

        // Thiết lập mối quan hệ với bảng User (Nếu cần)
        [ForeignKey("CreatedBy")]
        public virtual User Creator { get; set; }

        [ForeignKey("UpdatedBy")]
        public virtual User Updater { get; set; }
    }
}