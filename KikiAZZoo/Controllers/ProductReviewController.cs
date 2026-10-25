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

            // Enforce purchase requirement: Only customers who purchased this product can review
            bool hasPurchased = await _context.Orders
                .AnyAsync(o => o.UserId == userId && o.Status != "Cancelled" && o.OrderDetails!.Any(d => d.ProductId == productId));

            if (!hasPurchased && !User.IsInRole(SD.Role_Admin))
            {
                TempData["Error"] = "Chỉ khách hàng đã mua sản phẩm này mới có quyền gửi đánh giá.";
                return RedirectToAction("Details", "Product", new { id = productId });
            }

            if (rating < 1 || rating > 5 || string.IsNullOrWhiteSpace(content) || content.Trim().Length < 5)
            {
                TempData["Error"] = "Đánh giá không hợp lệ. Số sao phải từ 1-5 và bình luận phải từ 5 ký tự.";
                return RedirectToAction("Details", "Product", new { id = productId });
            }

            var review = new ProductReview
            {
                ProductId = productId,
                UserId = userId,
                Rating = rating,
                Content = content.Trim(),
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReply(int reviewId, int productId, string content)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            if (string.IsNullOrWhiteSpace(content) || content.Trim().Length < 2)
            {
                TempData["Error"] = "Nội dung phản hồi phải từ 2 ký tự trở lên.";
                return RedirectToAction("Details", "Product", new { id = productId });
            }

            var review = await _context.ProductReviews.FindAsync(reviewId);
            if (review == null)
            {
                TempData["Error"] = "Đánh giá không tồn tại hoặc đã bị xóa.";
                return RedirectToAction("Details", "Product", new { id = productId });
            }

            bool isStaff = User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee);

            var reply = new ProductReviewReply
            {
                ProductReviewId = reviewId,
                UserId = userId,
                Content = content.Trim(),
                IsStaffReply = isStaff,
                CreatedDate = DateTime.Now
            };

            _context.ProductReviewReplies.Add(reply);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã gửi câu trả lời đánh giá thành công!";
            return RedirectToAction("Details", "Product", new { id = productId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReply(int replyId, int productId)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var reply = await _context.ProductReviewReplies.FindAsync(replyId);
            if (reply != null)
            {
                if (reply.UserId == userId || User.IsInRole(SD.Role_Admin))
                {
                    _context.ProductReviewReplies.Remove(reply);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Đã xóa phản hồi.";
                }
            }
            return RedirectToAction("Details", "Product", new { id = productId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReview(int reviewId, int productId)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var review = await _context.ProductReviews.FindAsync(reviewId);
            if (review != null)
            {
                if (review.UserId == userId || User.IsInRole(SD.Role_Admin))
                {
                    _context.ProductReviews.Remove(review);
                    await _context.SaveChangesAsync();

                    // Recalculate average rating
                    var product = await _context.Products.FindAsync(productId);
                    if (product != null)
                    {
                        var reviews = await _context.ProductReviews.Where(r => r.ProductId == productId).ToListAsync();
                        product.Rating = reviews.Any() ? Math.Round(reviews.Average(r => r.Rating), 1) : 5.0;
                        _context.Products.Update(product);
                        await _context.SaveChangesAsync();
                    }

                    TempData["Success"] = "Đã xóa đánh giá thành công.";
                }
            }
            return RedirectToAction("Details", "Product", new { id = productId });
        }
    }
}
