using System.ComponentModel.DataAnnotations;

namespace KikiAZZoo.Models
{
    public class Breed
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Code { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(100)]
        public string? Origin { get; set; }

        public int PetTypeId { get; set; }
        public PetType? PetType { get; set; }

        public List<Pet>? Pets { get; set; }
    }
}
