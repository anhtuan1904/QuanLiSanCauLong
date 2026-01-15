using Microsoft.AspNetCore.Mvc;

namespace QuanLiSanCauLong.ViewModels
{
    public class StatisticSummaryViewModel
    {
        public int TodayBookings { get; set; }
        public int PendingBookings { get; set; }
        public int PlayingBookings { get; set; }
        public int CompletedBookings { get; set; }
        public decimal TodayRevenue { get; set; }
    }
}
