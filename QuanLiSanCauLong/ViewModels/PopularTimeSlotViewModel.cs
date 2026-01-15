using Microsoft.AspNetCore.Mvc;

namespace QuanLiSanCauLong.ViewModels
{
    public class PopularTimeSlotViewModel
    {
        public string TimeSlot { get; set; }
        public int BookingCount { get; set; }
        public decimal Revenue { get; set; }
    }
}
