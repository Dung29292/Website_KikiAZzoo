using KikiAZZoo.Models;

namespace KikiAZZoo.Repositories
{
    public interface IProductRepository
    {
        Task<IEnumerable<Product>> GetAllAsync();
        Task<IEnumerable<Product>> GetFilteredAsync(int? categoryId, int? categoryGroupId, int? petTypeId, int? brandId, decimal? minPrice, decimal? maxPrice, string? search);
        Task<Product?> GetByIdAsync(int id);
        Task AddAsync(Product product);
        Task UpdateAsync(Product product);
        Task DeleteAsync(int id);
    }
}
