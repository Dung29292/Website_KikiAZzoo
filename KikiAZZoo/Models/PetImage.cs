using System.ComponentModel.DataAnnotations;

namespace KikiAZZoo.Models
{
    public class PetImage
    {
        public int Id { get; set; }

        public int PetId { get; set; }
        public Pet? Pet { get; set; }

        [Required, StringLength(500)]
        public string ImageUrl { get; set; } = string.Empty;

        public int SortOrder { get; set; } = 0;
    }
}
