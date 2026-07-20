using KikiAZZoo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KikiAZZoo.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class NewsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NewsController(ApplicationDbContext context) => _context = context;

        public async Task<IActionResult> Index()
        {
            var posts = await _context.BlogPosts.OrderByDescending(p => p.PublishedDate).ToListAsync();
            return View(posts);
        }

        public IActionResult Add() => View(new BlogPost { PublishedDate = DateTime.Now });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(BlogPost post, IFormFile? imageUrl)
        {
            if (ModelState.IsValid)
            {
                if (imageUrl != null)
                    post.ImageUrl = await SaveImageAsync(imageUrl);
                _context.BlogPosts.Add(post);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(post);
        }

        public async Task<IActionResult> Update(int id)
        {
            var post = await _context.BlogPosts.FindAsync(id);
            if (post == null) return NotFound();
            return View(post);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int id, BlogPost post, IFormFile? imageUrl)
        {
            if (id != post.Id) return NotFound();
            if (ModelState.IsValid)
            {
                var existing = await _context.BlogPosts.FindAsync(id);
                if (existing == null) return NotFound();

                existing.Title = post.Title;
                existing.Summary = post.Summary;
                existing.Content = post.Content;
                existing.PublishedDate = post.PublishedDate;
                existing.IsFeatured = post.IsFeatured;

                if (imageUrl != null)
                    existing.ImageUrl = await SaveImageAsync(imageUrl);

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(post);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var post = await _context.BlogPosts.FindAsync(id);
            if (post == null) return NotFound();
            return View(post);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var post = await _context.BlogPosts.FindAsync(id);
            if (post != null)
            {
                _context.BlogPosts.Remove(post);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private static async Task<string> SaveImageAsync(IFormFile image)
        {
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
            var savePath = Path.Combine("wwwroot/images", fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(savePath)!);
            await using var stream = new FileStream(savePath, FileMode.Create);
            await image.CopyToAsync(stream);
            return "/images/" + fileName;
        }
    }
}
