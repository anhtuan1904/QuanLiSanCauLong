using Microsoft.AspNetCore.Mvc;

namespace QuanLiSanCauLong.ViewModels
{
    public class StaffDashboardViewModel
    {
        public DateTime SelectedDate { get; set; }
        public int FacilityId { get; set; }
        public string FacilityName { get; set; }

        public List<CourtScheduleViewModel> CourtSchedules { get; set; }
        public List<BookingItemViewModel> PendingBookings { get; set; }
        public List<BookingItemViewModel> UpcomingBookings { get; set; }
        public List<BookingItemViewModel> PlayingBookings { get; set; }

        public StatisticSummaryViewModel Statistics { get; set; }
    }
}
