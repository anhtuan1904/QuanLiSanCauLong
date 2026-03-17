using System.ComponentModel.DataAnnotations;

namespace QuanLiSanCauLong.ViewModels
{
    // ════════════════════════════════════════════════════════════════
    // PATCH: Thêm các property mới vào OrderViewModel hiện có
    // Nếu dự án đã có OrderViewModel, chỉ cần thêm các property bên dưới
    // ════════════════════════════════════════════════════════════════

    public class OrderViewModel
    {
        public int OrderId { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public string? Note { get; set; }

        // Product | booking | service
        public string OrderType { get; set; } = "product";

        // ── MỚI: Thông tin dịch vụ ──
        public string? ServiceType { get; set; }   // "Khóa học" | "Căng vợt" | "Giải đấu"
        public string? ServiceInfo { get; set; }   // Tên dịch vụ cụ thể

        public int? BookingId { get; set; }
        public string? BookingCode { get; set; }
        public string? FacilityName { get; set; }

        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;

        public decimal SubTotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }

        public string OrderStatus { get; set; } = "Pending";
        public string PaymentStatus { get; set; } = "Unpaid";
        public string? PaymentMethod { get; set; }   // ── MỚI

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public List<OrderDetailViewModel> OrderDetails { get; set; } = new();

        // ── Helpers ──────────────────────────────────────────────────
        public bool IsServiceOrder => OrderType == "service";

        public string DisplayCustomer =>
            string.IsNullOrEmpty(CustomerPhone)
                ? CustomerName
                : $"{CustomerName} — {CustomerPhone}";
    }

    public class OrderDetailViewModel
    {
        public string ProductName { get; set; } = string.Empty;
        public string? VariantDisplay { get; set; }
        public int Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalPrice { get; set; }
    }
    public class OrderItemViewModel
    {
        [Required]
        public int ProductId { get; set; }

        /// <summary>Variant (size/màu) — null nếu sản phẩm không có phân loại.</summary>
        public int? VariantId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải >= 1")]
        public int Quantity { get; set; }

        /// <summary>Đơn giá tại thời điểm đặt hàng.</summary>
        [Required]
        public decimal Price { get; set; }

        /// <summary>= Price × Quantity (tính sẵn hoặc tính lại trong service).</summary>
        public decimal TotalPrice => Price * Quantity;

        // ── Thông tin phụ (dùng để hiển thị, không lưu DB) ─────────
        public string? ProductName { get; set; }
        public string? VariantName { get; set; }
        public string? Unit { get; set; }
    }

}
