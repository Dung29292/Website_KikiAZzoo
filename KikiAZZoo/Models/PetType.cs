using System.ComponentModel.DataAnnotations;

namespace KikiAZZoo.Models
{
    public class PetType
    {
        public int Id { get; set; }

        [Required, StringLength(50)]
        public string Name { get; set; } = string.Empty;

        public string Icon { get; set; } = "bi-heart";

        public int SortOrder { get; set; }

        public List<Product>? Products { get; set; }
        public List<Breed>? Breeds { get; set; }
    }
}
