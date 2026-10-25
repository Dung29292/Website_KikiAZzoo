using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KikiAZZoo.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required, StringLength(200, MinimumLength = 5)]
        public string Name { get; set; } = string.Empty;

        [Range(0.01, 100000000)]
        [DisplayFormat(DataFormatString = "{0:0}", ApplyFormatInEditMode = true)]
        public decimal Price { get; set; }

        [DisplayFormat(DataFormatString = "{0:0}", ApplyFormatInEditMode = true)]
        public decimal? OriginalPrice { get; set; }

        [StringLength(2000, MinimumLength = 10, ErrorMessage = "Mô tả phải từ 10 đến 2000 ký tự")]
        public string Description { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Ingredients { get; set; }

        [StringLength(1000)]
        public string? UsageGuide { get; set; }

        public string? ImageUrl { get; set; }

        public double Rating { get; set; } = 5;

        public int SalesCount { get; set; } = 0;

        public int? DiscountPercent { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        public int? BrandId { get; set; }
        public Brand? Brand { get; set; }

        public int? PetTypeId { get; set; }
        public PetType? PetType { get; set; }

        public List<ProductImage>? Images { get; set; }
        public List<ProductFeature>? Features { get; set; }

        public string? TargetLifeStage { get; set; }
        public string? Unit { get; set; }

        [Column("Quantity")]
        public int Stock { get; set; } = 10;

        [NotMapped]
        public int Quantity
        {
            get => Stock;
            set => Stock = value;
        }

        public bool IsActive { get; set; } = true;
    }
}
