using Microsoft.AspNetCore.Mvc;

namespace QuanLiSanCauLong.Services
{
    public class PaymentService : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
