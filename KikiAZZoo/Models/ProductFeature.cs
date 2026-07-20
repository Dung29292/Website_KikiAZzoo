namespace KikiAZZoo.Models
{
    public class ProductFeature
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string Icon { get; set; } = "bi-check-circle";
        public string Text { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public Product? Product { get; set; }
    }
}
