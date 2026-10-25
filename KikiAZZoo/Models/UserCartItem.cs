using System.ComponentModel.DataAnnotations;

namespace KikiAZZoo.Models
{
    public class UserCartItem
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public int ProductId { get; set; }

        public int? PetId { get; set; }

        [StringLength(50)]
        public string ItemType { get; set; } = "product"; // "product" | "pet"

        public int Quantity { get; set; } = 1;

        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}
