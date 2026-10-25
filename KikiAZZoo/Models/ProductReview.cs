using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace KikiAZZoo.Models
{
    public class ProductReview
    {
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        [ValidateNever]
        public Product? Product { get; set; }

        [Required]
        [ForeignKey(nameof(ApplicationUser))]
        public string UserId { get; set; } = string.Empty;

        [ValidateNever]
        public ApplicationUser? ApplicationUser { get; set; }

        [Range(1, 5, ErrorMessage = "Đánh giá phải từ 1 đến 5 sao")]
        public int Rating { get; set; }

        [Required, StringLength(1000, MinimumLength = 5, ErrorMessage = "Đánh giá phải từ 5 đến 1000 ký tự")]
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [ValidateNever]
        public List<ProductReviewReply> Replies { get; set; } = new();
    }
}
