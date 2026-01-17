using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace QuanLiSanCauLong.ViewModels
{
    // ===================================
    // VIEW MODELS FOR HOME
    // ===================================

    public class HomeViewModel
    {
        public List<FacilityCardViewModel> FeaturedFacilities { get; set; }
        public SystemStatsViewModel Stats { get; set; }
    }

    public class FacilityCardViewModel
    {
        public int FacilityId { get; set; }
        public string FacilityName { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string District { get; set; }
        public string ImageUrl { get; set; }
        public int TotalCourts { get; set; }
        public TimeSpan? OpenTime { get; set; }
        public TimeSpan? CloseTime { get; set; }
    }

    public class SystemStatsViewModel
    {
        public int TotalFacilities { get; set; }
        public int TotalCourts { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalBookingsToday { get; set; }
    }

    public class AboutViewModel
    {
        public string CompanyName { get; set; }
        public string Description { get; set; }
        public string Mission { get; set; }
        public string Vision { get; set; }
        public List<string> Features { get; set; }
    }

    public class ContactViewModel
    {
        public string SupportEmail { get; set; }
        public string SupportPhone { get; set; }
        public string Address { get; set; }
        public string WorkingHours { get; set; }
        public ContactFormViewModel ContactForm { get; set; }
    }

    public class ContactFormViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; }

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tiêu đề")]
        public string Subject { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập nội dung")]
        public string Message { get; set; }
    }

    public class FaqItem
    {
        public string Question { get; set; }
        public string Answer { get; set; }
    }
}
