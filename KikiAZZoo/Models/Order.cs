using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace KikiAZZoo.Models
{
    public class Order
    {
        public int Id { get; set; }

        [ForeignKey(nameof(ApplicationUser))]
        public string UserId { get; set; } = string.Empty;

        public DateTime OrderDate { get; set; } = DateTime.Now;

        public decimal TotalPrice { get; set; }

        [Required, StringLength(500, MinimumLength = 10, ErrorMessage = "Địa chỉ giao hàng phải từ 10 đến 500 ký tự")]
        public string ShippingAddress { get; set; } = string.Empty;

        public string? Notes { get; set; }

        public string Status { get; set; } = "Pending";

        public string PaymentMethod { get; set; } = "COD";

        public string PaymentStatus { get; set; } = "Pending";

        public decimal ShippingFee { get; set; } = 0;

        public string? Province { get; set; }

        public string? District { get; set; }

        public string? Ward { get; set; }

        public string? CouponCode { get; set; }

        public decimal DiscountAmount { get; set; } = 0;

        public string? CompanyName { get; set; }
        public string? TaxCode { get; set; }
        public string? CompanyAddress { get; set; }

        [ValidateNever]
        public ApplicationUser? ApplicationUser { get; set; }

        public List<OrderDetail>? OrderDetails { get; set; }
    }
}
