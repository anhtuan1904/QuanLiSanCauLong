using Microsoft.AspNetCore.Mvc;

namespace QuanLiSanCauLong.ViewModels
{
    public class CourtScheduleViewModel
    {
        public int CourtId { get; set; }
        public string CourtNumber { get; set; }
        public string CourtType { get; set; }
        public string CurrentStatus { get; set; } // Empty, Playing, Upcoming, Maintenance
        public BookingItemViewModel CurrentBooking { get; set; }
        public BookingItemViewModel NextBooking { get; set; }
        public List<TimeSlotBookingViewModel> TimeSlots { get; set; }
    }
}
