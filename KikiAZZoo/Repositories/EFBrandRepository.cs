using KikiAZZoo.Models;
using Microsoft.EntityFrameworkCore;

namespace KikiAZZoo.Repositories
{
    public class EFBrandRepository : IBrandRepository
    {
        private readonly ApplicationDbContext _context;

        public EFBrandRepository(ApplicationDbContext context) => _context = context;

        public async Task<IEnumerable<Brand>> GetAllAsync() =>
            await _context.Brands.OrderBy(b => b.Name).ToListAsync();
    }
}
