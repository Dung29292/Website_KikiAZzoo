using KikiAZZoo.Models;
using Microsoft.EntityFrameworkCore;

namespace KikiAZZoo.Repositories
{
    public class EFProductRepository : IProductRepository
    {
        private readonly ApplicationDbContext _context;

        public EFProductRepository(ApplicationDbContext context) => _context = context;

        private IQueryable<Product> BaseQuery(bool? isActive = null)
        {
            var query = _context.Products
                .Where(p => p.Category == null || p.Category.CategoryGroup == null || (p.Category.CategoryGroupId != 1 && p.Category.CategoryGroup.Name != "Thú cưng"))
                .Include(p => p.Category!).ThenInclude(c => c.CategoryGroup)
                .Include(p => p.Brand)
                .Include(p => p.PetType)
                .Include(p => p.Images)
                .Include(p => p.Features)
                .AsQueryable();

            if (isActive.HasValue)
            {
                query = query.Where(p => p.IsActive == isActive.Value);
            }

            return query;
        }

        public async Task<IEnumerable<Product>> GetAllAsync(bool activeOnly = true) =>
            await BaseQuery(activeOnly ? true : null).OrderByDescending(p => p.Id).ToListAsync();

        public async Task<IEnumerable<Product>> GetFilteredAsync(int? categoryId, int? categoryGroupId, int? petTypeId, int? brandId, decimal? minPrice, decimal? maxPrice, string? search, bool? isActive = true)
        {
            var query = BaseQuery(isActive);

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId);
            else if (categoryGroupId.HasValue)
                query = query.Where(p => p.Category!.CategoryGroupId == categoryGroupId);
            if (petTypeId.HasValue) query = query.Where(p => p.PetTypeId == petTypeId);
            if (brandId.HasValue) query = query.Where(p => p.BrandId == brandId);
            if (minPrice.HasValue) query = query.Where(p => p.Price >= minPrice);
            if (maxPrice.HasValue) query = query.Where(p => p.Price <= maxPrice);
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchTerm = search.Trim();
                var searchPattern = $"%{searchTerm}%";
                query = query.Where(p =>
                    EF.Functions.Like(EF.Functions.Collate(p.Name, "Vietnamese_CI_AI"), searchPattern) ||
                    EF.Functions.Like(EF.Functions.Collate(p.Description, "Vietnamese_CI_AI"), searchPattern));
            }

            return await query.OrderByDescending(p => p.Id).ToListAsync();
        }

        public async Task<Product?> GetByIdAsync(int id, bool activeOnly = false) =>
            await BaseQuery(activeOnly ? true : null).FirstOrDefaultAsync(p => p.Id == id);

        public async Task AddAsync(Product product)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Product product)
        {
            _context.Products.Update(product);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }
        }
    }
}
