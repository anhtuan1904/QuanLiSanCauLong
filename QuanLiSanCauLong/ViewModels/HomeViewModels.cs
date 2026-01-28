using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace QuanLiSanCauLong.ViewModels
{
    /// <summary>
    /// ViewModel cho trang chủ
    /// </summary>
    public class HomeViewModel
    {
        public List<FacilityCardViewModel> FeaturedFacilities { get; set; }
        public SystemStatsViewModel Stats { get; set; }
    }

    /// <summary>
    /// ViewModel cho thẻ hiển thị cơ sở trên trang chủ
    /// </summary>
/*    public class FacilityCardViewModel
    {
        public int FacilityId { get; set; }
        public string FacilityName { get; set; }
        public string Address { get; set; }
        public string District { get; set; }
        public string City { get; set; }
        public string Phone { get; set; }
        public string ImageUrl { get; set; }
        public TimeSpan? OpenTime { get; set; }
        public TimeSpan? CloseTime { get; set; }
        public int TotalCourts { get; set; }
        public decimal MinPrice { get; set; }
        public decimal MaxPrice { get; set; }
        public double? Rating { get; set; }
        public int ReviewCount { get; set; }
    }
*/
    /// <summary>
    /// ViewModel cho thống kê hệ thống trên trang chủ
    /// </summary>
    public class SystemStatsViewModel
    {
        public int TotalFacilities { get; set; }
        public int TotalCourts { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalBookingsToday { get; set; }
    }

    /// <summary>
    /// ViewModel cho trang giới thiệu
    /// </summary>
    public class AboutViewModel
    {
        public string CompanyName { get; set; }
        public string Description { get; set; }
        public string Mission { get; set; }
        public string Vision { get; set; }
        public List<string> Features { get; set; }
        public List<TeamMemberViewModel> TeamMembers { get; set; }
        public List<MilestoneViewModel> Milestones { get; set; }
    }

    /// <summary>
    /// ViewModel cho thành viên đội ngũ
    /// </summary>
    public class TeamMemberViewModel
    {
        public string Name { get; set; }
        public string Position { get; set; }
        public string Bio { get; set; }
        public string ImageUrl { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
    }

    /// <summary>
    /// ViewModel cho các mốc phát triển
    /// </summary>
    public class MilestoneViewModel
    {
        public int Year { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
    }

    /// <summary>
    /// ViewModel cho trang liên hệ
    /// </summary>
    public class ContactViewModel
    {
        public string SupportEmail { get; set; }
        public string SupportPhone { get; set; }
        public string Address { get; set; }
        public string WorkingHours { get; set; }
        public ContactFormViewModel ContactForm { get; set; }
    }

    /// <summary>
    /// ViewModel cho form liên hệ
    /// </summary>
    public class ContactFormViewModel
    {
        [Required(ErrorMessage = "Họ tên không được để trống")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [Display(Name = "Số điện thoại")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Chủ đề không được để trống")]
        [Display(Name = "Chủ đề")]
        public string Subject { get; set; }

        [Required(ErrorMessage = "Nội dung không được để trống")]
        [Display(Name = "Nội dung")]
        [StringLength(1000, ErrorMessage = "Nội dung không được quá 1000 ký tự")]
        public string Message { get; set; }
    }

    /// <summary>
    /// ViewModel cho câu hỏi thường gặp
    /// </summary>
    public class FaqItem
    {
        public int FaqId { get; set; }
        public string Question { get; set; }
        public string Answer { get; set; }
        public string Category { get; set; }
        public int DisplayOrder { get; set; }
    }

    /// <summary>
    /// ViewModel cho danh sách FAQ
    /// </summary>
    public class FaqViewModel
    {
        public List<FaqCategoryViewModel> Categories { get; set; }
    }

    /// <summary>
    /// ViewModel cho danh mục FAQ
    /// </summary>
    public class FaqCategoryViewModel
    {
        public string CategoryName { get; set; }
        public List<FaqItem> Items { get; set; }
    }

    /// <summary>
    /// ViewModel cho trang lỗi
    /// </summary>
}
