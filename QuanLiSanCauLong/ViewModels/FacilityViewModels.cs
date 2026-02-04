using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace QuanLiSanCauLong.ViewModels
{
    /// <summary>
    /// ViewModel cho danh sách cơ sở (khách hàng xem)
    /// </summary>
    public class FacilityListViewModel
    {
        public List<FacilityCardViewModel> Facilities { get; set; } = new List<FacilityCardViewModel>();
        public string SearchCity { get; set; }
        public string SearchDistrict { get; set; }
        public string SearchKeyword { get; set; }
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    }

    /// <summary>
    /// ViewModel cho thẻ hiển thị cơ sở
    /// </summary>
    public class FacilityCardViewModel
    {
        public int FacilityId { get; set; }
        public string FacilityName { get; set; }
        public string Address { get; set; }
        public string District { get; set; }
        public string Description { get; set; }
        public string City { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string ImageUrl { get; set; }
        public TimeSpan? OpenTime { get; set; }
        public TimeSpan? CloseTime { get; set; }
        public int TotalCourts { get; set; }
        public decimal MinPrice { get; set; }
        public decimal MaxPrice { get; set; }
        public double? Rating { get; set; }
        public int ReviewCount { get; set; }
        public bool IsOpen { get; set; }
        public string OpenStatus => IsOpen ? "Đang mở cửa" : "Đã đóng cửa";
    }

    /// <summary>
    /// ViewModel cho chi tiết cơ sở
    /// </summary>
    public class FacilityDetailsViewModel
    {
        public int FacilityId { get; set; }
        public string FacilityName { get; set; }
        public string Description { get; set; }
        public string Address { get; set; }
        public string District { get; set; }
        public string City { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Website { get; set; }
        public List<string> ImageUrls { get; set; } = new List<string>();
        public TimeSpan? OpenTime { get; set; }
        public TimeSpan? CloseTime { get; set; }

        public DateTime SelectedDate { get; set; } = DateTime.Now;
        public List<RatingDistributionItem> RatingDistribution { get; set; } = new List<RatingDistributionItem>();

        // Thông tin sân - ĐÃ XÓA TRÙNG LẶP Ở ĐÂY
        public List<FacilityCourtViewModel> Courts { get; set; } = new List<FacilityCourtViewModel>();
        public int TotalCourts { get; set; }

        // Giá & dịch vụ
        public decimal MinPrice { get; set; }
        public decimal MaxPrice { get; set; }
        public List<FacilityServiceViewModel> Services { get; set; } = new List<FacilityServiceViewModel>();
        public List<FacilityAmenityViewModel> Amenities { get; set; } = new List<FacilityAmenityViewModel>();

        // Đánh giá
        public double? AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public List<FacilityReviewViewModel> RecentReviews { get; set; } = new List<FacilityReviewViewModel>();

        // Vị trí
        public string MapUrl { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        // Lịch đặt của khách hàng
        public List<UserBookingViewModel> UserBookings { get; set; } = new List<UserBookingViewModel>();
    }

    /// <summary>
    /// Class hỗ trợ hiển thị biểu đồ phân bổ đánh giá
    /// </summary>
    public class RatingDistributionItem
    {
        public int StarLevel { get; set; }
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    /// <summary>
    /// ViewModel cho sân trong cơ sở
    /// </summary>
    public class FacilityCourtViewModel
    {
        public int CourtId { get; set; }
        public string CourtNumber { get; set; }
        public string CourtType { get; set; }
        public string Status { get; set; }
        public string Description { get; set; }
        public List<string> Features { get; set; } = new List<string>();

        // Danh sách khung giờ trống
        public List<TimeSlotViewModel> Slots { get; set; } = new List<TimeSlotViewModel>();
    }

    /// <summary>
    /// ViewModel cho lịch đặt của user
    /// </summary>
    public class UserBookingViewModel
    {
        public int BookingId { get; set; }
        public string CourtName { get; set; }
        public string Time { get; set; }
        public DateTime Date { get; set; }
        public string Status { get; set; }
    }

    /// <summary>
    /// ViewModel cho dịch vụ của cơ sở
    /// </summary>
    public class FacilityServiceViewModel
    {
        public string ServiceName { get; set; }
        public string Description { get; set; }
        public decimal? Price { get; set; }
        public string Icon { get; set; }
    }

    /// <summary>
    /// ViewModel cho tiện ích của cơ sở
    /// </summary>
    public class FacilityAmenityViewModel
    {
        public string AmenityName { get; set; }
        public string Icon { get; set; }
        public bool IsAvailable { get; set; }
    }

    /// <summary>
    /// ViewModel cho đánh giá cơ sở
    /// </summary>
    public class FacilityReviewViewModel
    {
        public int ReviewId { get; set; }
        public string CustomerName { get; set; }
        public string CustomerAvatar { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public DateTime CreatedAt { get; set; }
        public string TimeAgo { get; set; }
    }

    /// <summary>
    /// ViewModel cho quản lý cơ sở (Admin)
    /// </summary>
    public class FacilityManageViewModel
    {
        [Required(ErrorMessage = "Tên cơ sở không được để trống")]
        [Display(Name = "Tên cơ sở")]
        public string FacilityName { get; set; }

        [Display(Name = "Mô tả")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Địa chỉ không được để trống")]
        [Display(Name = "Địa chỉ")]
        public string Address { get; set; }

        [Required(ErrorMessage = "Quận/Huyện không được để trống")]
        [Display(Name = "Quận/Huyện")]
        public string District { get; set; }

        [Required(ErrorMessage = "Thành phố không được để trống")]
        [Display(Name = "Thành phố")]
        public string City { get; set; }

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [Display(Name = "Số điện thoại")]
        public string Phone { get; set; }

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Display(Name = "Website")]
        public string Website { get; set; }

        [Required(ErrorMessage = "Giờ mở cửa không được để trống")]
        [Display(Name = "Giờ mở cửa")]
        public TimeSpan OpenTime { get; set; }

        [Required(ErrorMessage = "Giờ đóng cửa không được để trống")]
        [Display(Name = "Giờ đóng cửa")]
        public TimeSpan CloseTime { get; set; }

        [Display(Name = "Hình ảnh")]
        public List<string> ImageUrls { get; set; } = new List<string>();

        [Display(Name = "Vĩ độ")]
        public decimal? Latitude { get; set; }

        [Display(Name = "Kinh độ")]
        public decimal? Longitude { get; set; }

        [Display(Name = "Hoạt động")]
        public bool IsActive { get; set; }
    }
    public class FacilityMenuViewModel
    {
        public int FacilityId { get; set; }
        public string FacilityName { get; set; }
        public string FacilityAddress { get; set; }
        public string FacilityPhone { get; set; }

        // Khởi tạo sẵn List để tránh lỗi Null
        public List<MenuCategoryViewModel> MenuCategories { get; set; } = new();
        public List<MenuPromotionViewModel> ActivePromotions { get; set; } = new();
    }

}