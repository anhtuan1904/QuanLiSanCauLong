using Microsoft.AspNetCore.Mvc;

namespace QuanLiSanCauLong.ViewModels
{
    /// <summary>
    /// ViewModel cho sản phẩm trong đơn hàng/giỏ hàng
    /// </summary>
    public class OrderItemViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string CategoryType { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string Unit { get; set; }
        public decimal TotalPrice => Price * Quantity;
    }

    /// <summary>
    /// ViewModel cho đơn hàng đầy đủ
    /// </summary>
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

    /// <summary>
    /// ViewModel cho đơn hàng tóm tắt (dùng trong danh sách)
    /// </summary>
    public class OrderSummaryViewModel
    {
        public int OrderId { get; set; }
        public string OrderCode { get; set; }
        public decimal TotalAmount { get; set; }
        public string OrderStatus { get; set; }
        public string PaymentStatus { get; set; }
        public List<OrderDetailViewModel> OrderDetails { get; set; }
    }

    /// <summary>
    /// ViewModel cho chi tiết sản phẩm trong đơn hàng
    /// </summary>
    public class OrderDetailViewModel
    {
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public string Unit { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
