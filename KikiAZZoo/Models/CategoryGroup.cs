using System.ComponentModel.DataAnnotations;

namespace KikiAZZoo.Models
{
    public class CategoryGroup
    {
        public int Id { get; set; }

        [Required, StringLength(80)]
        public string Name { get; set; } = string.Empty;

        public string Icon { get; set; } = "bi-grid";

        public int SortOrder { get; set; }

        public List<Category>? Categories { get; set; }
    }
}
