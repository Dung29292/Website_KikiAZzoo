using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace KikiAZZoo.Models
{
    public class PetProfile
    {
        public int Id { get; set; }

        [Required]
        [ForeignKey(nameof(ApplicationUser))]
        public string UserId { get; set; } = string.Empty;

        [ValidateNever]
        public ApplicationUser? ApplicationUser { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Tên thú cưng phải từ 1 đến 100 ký tự")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Species { get; set; } = "Chó"; // Chó, Mèo, Chim, Hamster, Thỏ, Khác

        [StringLength(100)]
        public string? Breed { get; set; } // Giống loài

        [Range(0, 100, ErrorMessage = "Tuổi phải từ 0 đến 100")]
        public int Age { get; set; }

        [Range(0.01, 1000, ErrorMessage = "Cân nặng phải từ 0.01 đến 1000kg")]
        public double Weight { get; set; }

        public string? ImageUrl { get; set; }

        [StringLength(1000)]
        public string? HealthNotes { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}
