using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KikiAZZoo.Models
{
    public class Pet
    {
        public int Id { get; set; }

        [Required, StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(50)]
        public string? SkuCode { get; set; }

        public int BreedId { get; set; }
        public Breed? Breed { get; set; }

        public int AgeMonths { get; set; } = 2;

        public DateTime? BirthDate { get; set; }

        [Required, StringLength(10)]
        public string Gender { get; set; } = "Đực"; // Đực, Cái, Không rõ

        [Required, StringLength(50)]
        public string Color { get; set; } = "Đa sắc";

        [Range(0.01, 1000000000)]
        public decimal Price { get; set; }

        public decimal? OriginalPrice { get; set; }

        public bool IsVaccinated { get; set; } = true;

        [StringLength(500)]
        public string? VaccinationDetails { get; set; }

        public bool IsDewormed { get; set; } = true;

        [StringLength(100)]
        public string? MicrochipCode { get; set; }

        public int HealthGuaranteeDays { get; set; } = 30;

        [Required, StringLength(30)]
        public string Status { get; set; } = "Available"; // Available, Sold, Reserved

        public int Quantity { get; set; } = 1;

        public string? ImageUrl { get; set; }

        [StringLength(2000)]
        public string? Description { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [NotMapped]
        public bool IsAvailable => (Status == "Available" || Status == "Còn hàng") && Quantity > 0;
    }
}
