using Microsoft.AspNetCore.Mvc;

namespace QuanLiSanCauLong.ViewModels
{
    public class BookingItemViewModel
    {
        public int BookingId { get; set; }
        public string BookingCode { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string CourtNumber { get; set; }
        public string CourtType { get; set; }
        public DateTime BookingDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int Duration { get; set; }
        public string Status { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime? CheckInTime { get; set; }
        public bool HasOrders { get; set; }
        public int OrderCount { get; set; }
    }
}
