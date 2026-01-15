using Microsoft.AspNetCore.Mvc;

namespace QuanLiSanCauLong.ViewModels
{
    public class OrderViewModel
    {
        public int OrderId { get; set; }
        public string OrderCode { get; set; }
        public string OrderType { get; set; }
        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string OrderStatus { get; set; }
        public string PaymentStatus { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<OrderDetailViewModel> OrderDetails { get; set; }
    }
}
