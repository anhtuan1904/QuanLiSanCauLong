using Microsoft.AspNetCore.Mvc;

namespace QuanLiSanCauLong.ViewModels
{
    public class SalesViewModel
    {
        public int? BookingId { get; set; }
        public int FacilityId { get; set; }
        public int UserId { get; set; }
        public List<ProductCategoryViewModel> ProductCategories { get; set; }
        public List<OrderItemViewModel> CartItems { get; set; }
        public decimal SubTotal => CartItems?.Sum(i => i.TotalPrice) ?? 0;
        public string VoucherCode { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount => SubTotal - DiscountAmount;
    }
}
