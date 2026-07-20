using System.ComponentModel.DataAnnotations;

namespace KikiAZZoo.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required, StringLength(80)]
        public string Name { get; set; } = string.Empty;

        public string Icon { get; set; } = "bi-tag";
        
        public int SortOrder { get; set; } = 0;

        public int CategoryGroupId { get; set; }
        public CategoryGroup? CategoryGroup { get; set; }

        public List<Product>? Products { get; set; }
    }
}
