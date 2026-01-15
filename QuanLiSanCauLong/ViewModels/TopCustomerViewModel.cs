using Microsoft.AspNetCore.Mvc;

namespace QuanLiSanCauLong.ViewModels
{
    public class TopCustomerViewModel
    {
        public string CustomerName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public int BookingCount { get; set; }
        public decimal TotalSpent { get; set; }
    }
}
