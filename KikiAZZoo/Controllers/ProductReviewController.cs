using KikiAZZoo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KikiAZZoo.Controllers
{
    [Authorize]
    public class ProductReviewController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProductReviewController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReview(int productId, int rating, string content)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            if (rating < 1 || rating > 5 || string.IsNullOrEmpty(content) || content.Length < 5)
            {
                TempData["Error"] = "Đánh giá không hợp lệ. Số sao phải từ 1-5 và bình luận phải từ 5 ký tự.";
                return RedirectToAction("Details", "Product", new { id = productId });
            }

            var review = new ProductReview
            {
                ProductId = productId,
                UserId = userId,
                Rating = rating,
                Content = content,
                CreatedDate = DateTime.Now
            };

            _context.ProductReviews.Add(review);

            // Dynamically recalculate average rating for the product
            var product = await _context.Products.FindAsync(productId);
            if (product != null)
            {
                var reviews = await _context.ProductReviews.Where(r => r.ProductId == productId).ToListAsync();
                double totalRating = reviews.Sum(r => r.Rating) + rating;
                product.Rating = Math.Round(totalRating / (reviews.Count + 1), 1);
                _context.Products.Update(product);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Cảm ơn bạn đã gửi đánh giá sản phẩm!";
            
            return RedirectToAction("Details", "Product", new { id = productId });
        }
    }
}
