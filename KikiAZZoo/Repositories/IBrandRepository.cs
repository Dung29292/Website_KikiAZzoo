using KikiAZZoo.Models;

namespace KikiAZZoo.Repositories
{
    public interface IBrandRepository
    {
        Task<IEnumerable<Brand>> GetAllAsync();
    }
}
