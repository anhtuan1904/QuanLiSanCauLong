using Microsoft.AspNetCore.Mvc;

namespace QuanLiSanCauLong.ViewModels
{
    public class RevenueByDateViewModel
    {
        public DateTime Date { get; set; }
        public decimal BookingRevenue { get; set; }
        public decimal ProductRevenue { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}
