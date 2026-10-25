using KikiAZZoo.Models;

namespace KikiAZZoo.Repositories
{
    public interface IProductRepository
    {
        Task<IEnumerable<Product>> GetAllAsync(bool activeOnly = true);
        Task<IEnumerable<Product>> GetFilteredAsync(int? categoryId, int? categoryGroupId, int? petTypeId, int? brandId, decimal? minPrice, decimal? maxPrice, string? search, bool? isActive = true);
        Task<Product?> GetByIdAsync(int id, bool activeOnly = false);
        Task AddAsync(Product product);
        Task UpdateAsync(Product product);
        Task DeleteAsync(int id);
    }
}
