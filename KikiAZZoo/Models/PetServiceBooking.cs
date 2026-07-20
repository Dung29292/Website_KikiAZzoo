using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace KikiAZZoo.Models
{
    public class PetServiceBooking
    {
        public int Id { get; set; }

        [Required]
        [ForeignKey(nameof(ApplicationUser))]
        public string UserId { get; set; } = string.Empty;

        [ValidateNever]
        public ApplicationUser? ApplicationUser { get; set; }

        [Required]
        public int ServiceId { get; set; }

        [ValidateNever]
        public Service? Service { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime BookingDate { get; set; }

        [Required]
        [StringLength(50)]
        public string TimeSlot { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string PetName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string PetType { get; set; } = "Chó"; // Chó, Mèo, Chim, Hamster, Thỏ

        [Range(0, 100000000)]
        public decimal Price { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Confirmed, Completed, Cancelled

        [StringLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}
