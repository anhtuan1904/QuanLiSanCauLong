using Microsoft.AspNetCore.Mvc;

namespace QuanLiSanCauLong.ViewModels
{
    public class CourtSearchViewModel
    {
        public int? FacilityId { get; set; }
        public DateTime BookingDate { get; set; } = DateTime.Today;
        public TimeSpan? StartTime { get; set; }
        public string CourtType { get; set; }
        public string City { get; set; }
        public string District { get; set; }

        public List<FacilityAvailabilityViewModel> AvailableFacilities { get; set; }
    }
}
