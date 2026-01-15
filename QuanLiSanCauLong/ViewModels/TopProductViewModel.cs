using Microsoft.AspNetCore.Mvc;

namespace QuanLiSanCauLong.ViewModels
{
    public class TopProductViewModel
    {
        public string ProductName { get; set; }
        public string CategoryType { get; set; }
        public int QuantitySold { get; set; }
        public decimal Revenue { get; set; }
    }
}
