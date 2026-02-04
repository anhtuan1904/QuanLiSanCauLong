using Microsoft.AspNetCore.Mvc;

namespace QuanLiSanCauLong.ViewModels
{
    public class ServiceViewModel
    {
        public string Type { get; set; } // "Course", "Restring", "Tournament"
        public string ServiceName { get; set; }
        public string Description { get; set; }
        public string Duration { get; set; } // "8 buổi/tháng", "30 phút", "1 ngày"
        public decimal Price { get; set; }
        public string ImageUrl { get; set; }
        public List<string> Features { get; set; } // Các dòng check-list (Bao gồm dây, HLV chuyên nghiệp...)
    }
}
