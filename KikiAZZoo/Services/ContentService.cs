using KikiAZZoo.Models;
using Microsoft.EntityFrameworkCore;

namespace KikiAZZoo.Services
{
    public class ContentService : IContentService
    {
        private readonly ApplicationDbContext _context;

        public ContentService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<BlogPost>> GetAllBlogPostsAsync()
        {
            return await _context.BlogPosts
                .OrderByDescending(p => p.PublishedDate)
                .ToListAsync();
        }

        public async Task<BlogPost?> GetBlogPostByIdAsync(int id)
        {
            return await _context.BlogPosts.FindAsync(id);
        }

        public async Task<List<Service>> GetAllServicesAsync()
        {
            return await _context.Services
                .OrderBy(s => s.SortOrder)
                .ToListAsync();
        }

        public async Task<Service?> GetServiceByIdAsync(int id)
        {
            return await _context.Services.FindAsync(id);
        }
    }
}
