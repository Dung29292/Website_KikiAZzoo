using KikiAZZoo.Models;

namespace KikiAZZoo.Repositories
{
    public interface IPetRepository
    {
        Task<IEnumerable<Pet>> GetAllAsync();
        Task<Pet?> GetByIdAsync(int id);
        Task<IEnumerable<Pet>> GetFeaturedAsync(int count = 4);
        Task<IEnumerable<Pet>> GetRelatedAsync(int petId, int? breedId, int? petTypeId, int count = 4);
        Task<IEnumerable<Pet>> SearchPetsAsync(string keyword, int limit = 5);
        Task AddAsync(Pet pet);
        Task UpdateAsync(Pet pet);
        Task DeleteAsync(int id);
    }
}
