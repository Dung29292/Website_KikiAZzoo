using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace KikiAZZoo.Models
{
    public class ProductReviewReply
    {
        public int Id { get; set; }

        [Required]
        public int ProductReviewId { get; set; }

        [ValidateNever]
        [ForeignKey(nameof(ProductReviewId))]
        public ProductReview? ProductReview { get; set; }

        [Required]
        [ForeignKey(nameof(ApplicationUser))]
        public string UserId { get; set; } = string.Empty;

        [ValidateNever]
        public ApplicationUser? ApplicationUser { get; set; }

        [Required, StringLength(1000, MinimumLength = 2, ErrorMessage = "Nội dung phản hồi phải từ 2 đến 1000 ký tự")]
        public string Content { get; set; } = string.Empty;

        public bool IsStaffReply { get; set; } = false;

        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}
