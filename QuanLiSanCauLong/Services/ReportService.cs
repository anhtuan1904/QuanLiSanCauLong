using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using QuanLiSanCauLong.Data;
using QuanLiSanCauLong.Services.Interfaces;
using QuanLiSanCauLong.ViewModels;

namespace QuanLiSanCauLong.Services
{
    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _context;

        public ReportService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AdminDashboardViewModel> GetDashboardDataAsync(DateTime fromDate, DateTime toDate)
        {
            var model = new AdminDashboardViewModel { FromDate = fromDate, ToDate = toDate };

            var bookings = await _context.Bookings
                .Include(b => b.Court.Facility)
                .Include(b => b.User)
                .Where(b => b.BookingDate >= fromDate && b.BookingDate <= toDate)
                .ToListAsync();

            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                .Include(o => o.Facility)
                .Where(o => o.CreatedAt >= fromDate && o.CreatedAt <= toDate.AddDays(1))
                .ToListAsync();

            model.TotalBookings = bookings.Count;
            model.CompletedBookings = bookings.Count(b => b.Status == "Completed");
            model.CancelledBookings = bookings.Count(b => b.Status == "Cancelled");
            model.BookingRevenue = bookings.Where(b => b.Status != "Cancelled").Sum(b => b.TotalPrice);
            model.ProductRevenue = orders.Where(o => o.OrderStatus == "Completed").Sum(o => o.TotalAmount);
            model.TotalRevenue = model.BookingRevenue + model.ProductRevenue;
            model.TotalCustomers = await _context.Users.CountAsync(u => u.Role == "Customer");
            model.NewCustomers = await _context.Users.CountAsync(u => u.Role == "Customer" && u.CreatedAt >= fromDate);
            model.TotalOrders = orders.Count;

            model.RevenueByDate = await GetRevenueByDateAsync(fromDate, toDate);
            model.RevenueByFacility = await GetRevenueByFacilityAsync(fromDate, toDate);
            model.PopularTimeSlots = await GetPopularTimeSlotsAsync(fromDate, toDate);
            model.TopProducts = await GetTopProductsAsync(fromDate, toDate, 10);
            model.TopCustomers = await GetTopCustomersAsync(fromDate, toDate, 10);

            return model;
        }

        public async Task<List<RevenueByDateViewModel>> GetRevenueByDateAsync(DateTime fromDate, DateTime toDate)
        {
            var bookings = await _context.Bookings
                .Where(b => b.BookingDate >= fromDate && b.BookingDate <= toDate && b.Status != "Cancelled")
                .GroupBy(b => b.BookingDate)
                .Select(g => new { Date = g.Key, Revenue = g.Sum(b => b.TotalPrice) })
                .ToListAsync();

            var orders = await _context.Orders
                .Where(o => o.CreatedAt >= fromDate && o.CreatedAt <= toDate.AddDays(1) && o.OrderStatus == "Completed")
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new { Date = g.Key, Revenue = g.Sum(o => o.TotalAmount) })
                .ToListAsync();

            var result = new List<RevenueByDateViewModel>();
            for (var date = fromDate; date <= toDate; date = date.AddDays(1))
            {
                var bookingRev = bookings.FirstOrDefault(b => b.Date == date)?.Revenue ?? 0;
                var productRev = orders.FirstOrDefault(o => o.Date == date)?.Revenue ?? 0;
                result.Add(new RevenueByDateViewModel
                {
                    Date = date,
                    BookingRevenue = bookingRev,
                    ProductRevenue = productRev,
                    TotalRevenue = bookingRev + productRev
                });
            }
            return result;
        }

        public async Task<List<RevenueByFacilityViewModel>> GetRevenueByFacilityAsync(DateTime fromDate, DateTime toDate)
        {
            var bookingRevenue = await _context.Bookings
                .Include(b => b.Court.Facility)
                .Where(b => b.BookingDate >= fromDate && b.BookingDate <= toDate && b.Status != "Cancelled")
                .GroupBy(b => new { b.Court.Facility.FacilityId, b.Court.Facility.FacilityName })
                .Select(g => new { g.Key.FacilityId, g.Key.FacilityName, BookingCount = g.Count(), Revenue = g.Sum(b => b.TotalPrice) })
                .ToListAsync();

            var productRevenue = await _context.Orders
                .Include(o => o.Facility)
                .Where(o => o.CreatedAt >= fromDate && o.CreatedAt <= toDate.AddDays(1) && o.OrderStatus == "Completed")
                .GroupBy(o => o.FacilityId)
                .Select(g => new { FacilityId = g.Key, Revenue = g.Sum(o => o.TotalAmount) })
                .ToListAsync();

            return bookingRevenue.Select(b => new RevenueByFacilityViewModel
            {
                FacilityName = b.FacilityName,
                BookingCount = b.BookingCount,
                BookingRevenue = b.Revenue,
                ProductRevenue = productRevenue.FirstOrDefault(p => p.FacilityId == b.FacilityId)?.Revenue ?? 0,
                TotalRevenue = b.Revenue + (productRevenue.FirstOrDefault(p => p.FacilityId == b.FacilityId)?.Revenue ?? 0)
            }).ToList();
        }

        public async Task<List<TopProductViewModel>> GetTopProductsAsync(DateTime fromDate, DateTime toDate, int top = 10)
        {
            // ✅ FIX line 157: Xóa CategoryType → dùng BehaviorType
            return await _context.OrderDetails
                .Include(od => od.Product.Category)
                .Include(od => od.Order)
                .Where(od => od.Order.CreatedAt >= fromDate
                        && od.Order.CreatedAt <= toDate.AddDays(1)
                        && od.Order.OrderStatus == "Completed")
                .GroupBy(od => new
                {
                    od.Product.ProductName,
                    // ✅ Đổi CategoryType → BehaviorType
                    BehaviorType = od.Product.Category != null ? od.Product.Category.BehaviorType : "Retail"
                })
                .Select(g => new TopProductViewModel
                {
                    ProductName = g.Key.ProductName,
                    CategoryType = g.Key.BehaviorType, // giữ property name của ViewModel
                    QuantitySold = g.Sum(od => od.Quantity),
                    Revenue = g.Sum(od => od.TotalPrice)
                })
                .OrderByDescending(p => p.Revenue)
                .Take(top)
                .ToListAsync();
        }

        public async Task<List<TopCustomerViewModel>> GetTopCustomersAsync(DateTime fromDate, DateTime toDate, int top = 10)
        {
            return await _context.Bookings
                .Include(b => b.User)
                .Where(b => b.BookingDate >= fromDate && b.BookingDate <= toDate && b.Status != "Cancelled")
                .GroupBy(b => new { b.User.FullName, b.User.Email, b.User.Phone })
                .Select(g => new TopCustomerViewModel
                {
                    CustomerName = g.Key.FullName,
                    Email = g.Key.Email,
                    Phone = g.Key.Phone,
                    BookingCount = g.Count(),
                    TotalSpent = g.Sum(b => b.TotalPrice)
                })
                .OrderByDescending(c => c.TotalSpent)
                .Take(top)
                .ToListAsync();
        }

        public async Task<List<PopularTimeSlotViewModel>> GetPopularTimeSlotsAsync(DateTime fromDate, DateTime toDate)
        {
            return await _context.Bookings
                .Where(b => b.BookingDate >= fromDate && b.BookingDate <= toDate && b.Status != "Cancelled")
                .GroupBy(b => b.StartTime.Hours)
                .Select(g => new PopularTimeSlotViewModel
                {
                    TimeSlot = g.Key + ":00 - " + (g.Key + 1) + ":00",
                    BookingCount = g.Count(),
                    Revenue = g.Sum(b => b.TotalPrice)
                })
                .OrderByDescending(t => t.BookingCount)
                .Take(10)
                .ToListAsync();
        }

        public async Task<byte[]> ExportRevenueReportAsync(DateTime fromDate, DateTime toDate, string format)
        {
            var data = await GetRevenueByDateAsync(fromDate, toDate);
            if (format.ToLower() == "excel")
                return await ExportRevenueToExcelAsync(data, fromDate, toDate);
            throw new NotImplementedException("PDF export not implemented yet");
        }

        public async Task<byte[]> ExportBookingReportAsync(DateTime fromDate, DateTime toDate, string format)
        {
            var bookings = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Court.Facility)
                .Where(b => b.BookingDate >= fromDate && b.BookingDate <= toDate)
                .OrderBy(b => b.BookingDate).ThenBy(b => b.StartTime)
                .ToListAsync();

            if (format.ToLower() == "excel")
                return await ExportBookingsToExcelAsync(bookings);
            throw new NotImplementedException("PDF export not implemented yet");
        }

        private async Task<byte[]> ExportRevenueToExcelAsync(List<RevenueByDateViewModel> data, DateTime fromDate, DateTime toDate)
        {
            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Báo cáo doanh thu");

            ws.Cells["A1"].Value = "BÁO CÁO DOANH THU";
            ws.Cells["A1:D1"].Merge = true;
            ws.Cells["A1"].Style.Font.Size = 16;
            ws.Cells["A1"].Style.Font.Bold = true;
            ws.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            ws.Cells["A2"].Value = $"Từ ngày {fromDate:dd/MM/yyyy} đến {toDate:dd/MM/yyyy}";
            ws.Cells["A2:D2"].Merge = true;
            ws.Cells["A2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            ws.Cells["A4"].Value = "Ngày";
            ws.Cells["B4"].Value = "Doanh thu sân";
            ws.Cells["C4"].Value = "Doanh thu sản phẩm";
            ws.Cells["D4"].Value = "Tổng doanh thu";
            var hdr = ws.Cells["A4:D4"];
            hdr.Style.Font.Bold = true;
            hdr.Style.Fill.PatternType = ExcelFillStyle.Solid;
            hdr.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);

            int row = 5;
            foreach (var item in data)
            {
                ws.Cells[row, 1].Value = item.Date.ToString("dd/MM/yyyy");
                ws.Cells[row, 2].Value = item.BookingRevenue;
                ws.Cells[row, 3].Value = item.ProductRevenue;
                ws.Cells[row, 4].Value = item.TotalRevenue;
                ws.Cells[row, 2, row, 4].Style.Numberformat.Format = "#,##0";
                row++;
            }

            ws.Cells[row, 1].Value = "TỔNG CỘNG";
            ws.Cells[row, 1].Style.Font.Bold = true;
            ws.Cells[row, 2].Formula = $"SUM(B5:B{row - 1})";
            ws.Cells[row, 3].Formula = $"SUM(C5:C{row - 1})";
            ws.Cells[row, 4].Formula = $"SUM(D5:D{row - 1})";
            ws.Cells[row, 2, row, 4].Style.Font.Bold = true;
            ws.Cells[row, 2, row, 4].Style.Numberformat.Format = "#,##0";
            ws.Cells.AutoFitColumns();

            return await package.GetAsByteArrayAsync();
        }

        private async Task<byte[]> ExportBookingsToExcelAsync(List<QuanLiSanCauLong.Models.Booking> bookings)
        {
            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Danh sách đặt sân");

            ws.Cells["A1"].Value = "Mã đơn"; ws.Cells["B1"].Value = "Khách hàng";
            ws.Cells["C1"].Value = "Cơ sở"; ws.Cells["D1"].Value = "Sân";
            ws.Cells["E1"].Value = "Ngày chơi"; ws.Cells["F1"].Value = "Giờ";
            ws.Cells["G1"].Value = "Tổng tiền"; ws.Cells["H1"].Value = "Trạng thái";

            var hdr = ws.Cells["A1:H1"];
            hdr.Style.Font.Bold = true;
            hdr.Style.Fill.PatternType = ExcelFillStyle.Solid;
            hdr.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);

            int row = 2;
            foreach (var b in bookings)
            {
                ws.Cells[row, 1].Value = b.BookingCode;
                ws.Cells[row, 2].Value = b.User?.FullName;
                ws.Cells[row, 3].Value = b.Court?.Facility?.FacilityName;
                ws.Cells[row, 4].Value = b.Court?.CourtNumber;
                ws.Cells[row, 5].Value = b.BookingDate.ToString("dd/MM/yyyy");
                ws.Cells[row, 6].Value = $"{b.StartTime:hh\\:mm} - {b.EndTime:hh\\:mm}";
                ws.Cells[row, 7].Value = b.TotalPrice;
                ws.Cells[row, 8].Value = b.Status;
                ws.Cells[row, 7].Style.Numberformat.Format = "#,##0";
                row++;
            }

            ws.Cells.AutoFitColumns();
            return await package.GetAsByteArrayAsync();
        }
    }
}
