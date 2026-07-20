using System.ComponentModel.DataAnnotations;

namespace KikiAZZoo.Models
{
    public class CartItem
    {
        public int ProductId { get; set; }
        
        [Required, StringLength(200, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;
        
        public string? ImageUrl { get; set; }
        
        [Range(0.01, double.MaxValue, ErrorMessage = "Giá phải lớn hơn 0")]
        public decimal Price { get; set; }
        
        [Range(1, 999, ErrorMessage = "Số lượng phải từ 1 đến 999")]
        public int Quantity { get; set; }
    }
}
