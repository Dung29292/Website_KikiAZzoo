using System.ComponentModel.DataAnnotations;

namespace KikiAZZoo.Models
{
    public class Brand
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public List<Product>? Products { get; set; }
    }
}
