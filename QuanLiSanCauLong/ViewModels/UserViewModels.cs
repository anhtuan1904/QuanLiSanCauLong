using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace QuanLiSanCauLong.ViewModels
{
    /// <summary>
    /// ViewModel cho danh sách người dùng (Admin)
    /// </summary>
    public class UserListViewModel
    {
        public List<UserItemViewModel> Users { get; set; }
        public string SearchKeyword { get; set; }
        public string FilterRole { get; set; }
        public string FilterStatus { get; set; }
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    }

    /// <summary>
    /// ViewModel cho từng user trong danh sách
    /// </summary>
    public class UserItemViewModel
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Role { get; set; }
        public string Status { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
        public int BookingCount { get; set; }
        public decimal TotalSpent { get; set; }
    }

    /// <summary>
    /// ViewModel cho quản lý user (Admin)
    /// </summary>
    public class UserManageViewModel
    {
        public int UserId { get; set; }

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

        [Required(ErrorMessage = "Vai trò không được để trống")]
        [Display(Name = "Vai trò")]
        public string Role { get; set; }

        [Display(Name = "Cơ sở làm việc")]
        public int? FacilityId { get; set; }

        [Display(Name = "Địa chỉ")]
        public string Address { get; set; }

        [Display(Name = "Ngày sinh")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [Display(Name = "Giới tính")]
        public string Gender { get; set; }

        [Display(Name = "Avatar")]
        public string AvatarUrl { get; set; }

        [Display(Name = "Hoạt động")]
        public bool IsActive { get; set; }

        // Mật khẩu (chỉ khi tạo mới)
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu")]
        [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        public string ConfirmPassword { get; set; }

        // Dropdown options
        public List<RoleOptionViewModel> AvailableRoles { get; set; }
        public List<FacilityOptionViewModel> AvailableFacilities { get; set; }
    }

    /// <summary>
    /// ViewModel cho chi tiết user
    /// </summary>
    public class UserDetailsViewModel
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Role { get; set; }
        public string FacilityName { get; set; }
        public string Address { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string Gender { get; set; }
        public string AvatarUrl { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }

        // Thống kê
        public int TotalBookings { get; set; }
        public int CompletedBookings { get; set; }
        public int CancelledBookings { get; set; }
        public decimal TotalSpent { get; set; }

        // Lịch sử gần đây
        public List<UserRecentBookingViewModel> RecentBookings { get; set; }
        public List<UserActivityLogViewModel> RecentActivities { get; set; }
    }

    /// <summary>
    /// ViewModel cho booking gần đây của user
    /// </summary>
    public class UserRecentBookingViewModel
    {
        public int BookingId { get; set; }
        public string BookingCode { get; set; }
        public string FacilityName { get; set; }
        public DateTime BookingDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public decimal TotalPrice { get; set; }
        public string Status { get; set; }
    }

    /// <summary>
    /// ViewModel cho hoạt động của user
    /// </summary>
    public class UserActivityLogViewModel
    {
        public DateTime Timestamp { get; set; }
        public string Action { get; set; }
        public string Description { get; set; }
        public string IpAddress { get; set; }
    }

    /// <summary>
    /// ViewModel cho phân quyền
    /// </summary>
    public class UserPermissionsViewModel
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }

        public List<PermissionGroupViewModel> PermissionGroups { get; set; }
    }

    /// <summary>
    /// ViewModel cho nhóm quyền
    /// </summary>
    public class PermissionGroupViewModel
    {
        public string GroupName { get; set; }
        public List<PermissionItemViewModel> Permissions { get; set; }
    }

    /// <summary>
    /// ViewModel cho từng quyền
    /// </summary>
    public class PermissionItemViewModel
    {
        public string PermissionCode { get; set; }
        public string PermissionName { get; set; }
        public string Description { get; set; }
        public bool IsGranted { get; set; }
    }

    /// <summary>
    /// ViewModel cho hồ sơ cá nhân
    /// </summary>
    public class ProfileViewModel
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

        [Display(Name = "Địa chỉ")]
        public string Address { get; set; }

        [Display(Name = "Ngày sinh")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [Display(Name = "Giới tính")]
        public string Gender { get; set; }

        [Display(Name = "Avatar")]
        public string AvatarUrl { get; set; }
    }

    /// <summary>
    /// ViewModel cho đổi mật khẩu
    /// </summary>
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Mật khẩu hiện tại không được để trống")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu hiện tại")]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "Mật khẩu mới không được để trống")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu mới")]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        public string ConfirmPassword { get; set; }
    }

    /// <summary>
    /// ViewModel cho quên mật khẩu
    /// </summary>
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; }
    }

    /// <summary>
    /// ViewModel cho đặt lại mật khẩu
    /// </summary>
    public class ResetPasswordViewModel
    {
        public string? Token { get; set; } // Cho phép null nếu không dùng token mail

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessage = "Mật khẩu mới không được để trống")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu")]
        [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        public string ConfirmPassword { get; set; }
    }

    /// <summary>
    /// ViewModel cho option vai trò
    /// </summary>
    public class RoleOptionViewModel
    {
        public string Value { get; set; }
        public string Text { get; set; }
    }

    /// <summary>
    /// ViewModel cho option cơ sở
    /// </summary>
    public class FacilityOptionViewModel
    {
        public int FacilityId { get; set; }
        public string FacilityName { get; set; }
    }
}
