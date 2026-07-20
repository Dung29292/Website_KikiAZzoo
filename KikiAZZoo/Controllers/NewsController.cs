using KikiAZZoo.Services;
using Microsoft.AspNetCore.Mvc;

namespace KikiAZZoo.Controllers
{
    public class NewsController : Controller
    {
        private readonly IContentService _contentService;

        public NewsController(IContentService contentService)
        {
            _contentService = contentService;
        }

        public async Task<IActionResult> Index()
        {
            var posts = await _contentService.GetAllBlogPostsAsync();
            return View(posts);
        }

        public async Task<IActionResult> Details(int id)
        {
            var post = await _contentService.GetBlogPostByIdAsync(id);
            if (post == null) return NotFound();
            return View(post);
        }
    }
}
