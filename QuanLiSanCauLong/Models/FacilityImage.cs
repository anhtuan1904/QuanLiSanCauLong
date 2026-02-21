using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLiSanCauLong.Models
{
    public class FacilityImage
    {
        [Key]
        public int ImageId { get; set; }
        public int FacilityId { get; set; }
        public string ImagePath { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
        public int DisplayOrder { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.Now;
        public virtual Facility? Facility { get; set; }
    }
}
