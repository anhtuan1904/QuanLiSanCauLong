using Microsoft.AspNetCore.Mvc;

namespace QuanLiSanCauLong.ViewModels
{
    public class ProductCategoryViewModel
    {
        public string CategoryName { get; set; }
        public string CategoryType { get; set; }
        public List<ProductItemViewModel> Products { get; set; }
    }
}
