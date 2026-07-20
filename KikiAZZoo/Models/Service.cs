using System.ComponentModel.DataAnnotations;

namespace KikiAZZoo.Models
{
    public class Service
    {
        public int Id { get; set; }

        [Required, StringLength(120)]
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string Icon { get; set; } = "bi-star";

        public decimal? PriceFrom { get; set; }

        public string? ImageUrl { get; set; }

        public int SortOrder { get; set; }
    }
}
