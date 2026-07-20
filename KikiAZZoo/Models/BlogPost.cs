using System.ComponentModel.DataAnnotations;

namespace KikiAZZoo.Models
{
    public class BlogPost
    {
        public int Id { get; set; }

        [Required, StringLength(300, MinimumLength = 5)]
        public string Title { get; set; } = string.Empty;

        [Required, StringLength(500, MinimumLength = 10)]
        public string Summary { get; set; } = string.Empty;

        [Required, StringLength(5000, MinimumLength = 20)]
        public string Content { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }

        public DateTime PublishedDate { get; set; } = DateTime.Now;

        public bool IsFeatured { get; set; }
    }
}
