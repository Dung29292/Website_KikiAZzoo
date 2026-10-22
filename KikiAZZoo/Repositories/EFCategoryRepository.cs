using KikiAZZoo.Models;
using Microsoft.EntityFrameworkCore;

namespace KikiAZZoo.Repositories
{
    public class EFCategoryRepository : ICategoryRepository
    {
        private readonly ApplicationDbContext _context;

        public EFCategoryRepository(ApplicationDbContext context) => _context = context;

        public async Task<IEnumerable<Category>> GetAllAsync() =>
            await _context.Categories
                .Include(c => c.CategoryGroup)
                .Include(c => c.Products)
                .Where(c => c.CategoryGroup == null || (c.CategoryGroupId != 1 && c.CategoryGroup.Name != "Thú cưng"))
                .OrderBy(c => c.CategoryGroup!.SortOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

        public async Task<Category?> GetByIdAsync(int id) =>
            await _context.Categories
                .Include(c => c.CategoryGroup)
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.Id == id && (c.CategoryGroup == null || (c.CategoryGroupId != 1 && c.CategoryGroup.Name != "Thú cưng")));

        public async Task AddAsync(Category category)
        {
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Category category)
        {
            _context.Categories.Update(category);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category != null)
            {
                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
            }
        }
    }
}
