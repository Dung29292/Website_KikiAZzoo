using System.ComponentModel.DataAnnotations;

namespace KikiAZZoo.Models
{
    public class Coupon
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty; // Mã code viết hoa, VD: KIKI10

        [Range(0, 100000000)]
        public decimal DiscountValue { get; set; } // Số lượng giảm

        public bool IsPercentage { get; set; } = true; // True: Giảm theo %, False: Giảm tiền trực tiếp

        [Range(0, 100000000)]
        public decimal MinOrderValue { get; set; } = 0; // Giá trị đơn hàng tối thiểu để áp dụng

        public DateTime StartDate { get; set; } = DateTime.Now;

        public DateTime EndDate { get; set; } = DateTime.Now.AddDays(30);

        public bool IsActive { get; set; } = true;

        public int? MaxUsage { get; set; } = 100;

        public int UsageCount { get; set; } = 0;
    }
}
