using System.ComponentModel.DataAnnotations;

namespace KikiAZZoo.Models
{
    public class PriceRange
    {
        public int Id { get; set; }

        [Required, StringLength(80)]
        public string Label { get; set; } = string.Empty;

        public decimal MinPrice { get; set; }

        public decimal? MaxPrice { get; set; }

        public int SortOrder { get; set; }
    }
}
