using KikiAZZoo.Models;
using Microsoft.EntityFrameworkCore;

namespace KikiAZZoo.Repositories
{
    public class EFPetRepository : IPetRepository
    {
        private readonly ApplicationDbContext _context;

        public EFPetRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        private IQueryable<Pet> BaseQuery() =>
            _context.Pets
                .Include(p => p.Breed)
                    .ThenInclude(b => b!.PetType);

        public async Task<IEnumerable<Pet>> GetAllAsync()
        {
            return await BaseQuery()
                .OrderByDescending(p => p.CreatedDate)
                .ToListAsync();
        }

        public async Task<Pet?> GetByIdAsync(int id)
        {
            return await BaseQuery()
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<IEnumerable<Pet>> GetFeaturedAsync(int count = 4)
        {
            return await BaseQuery()
                .Where(p => (p.Status == "Available" || p.Status == "Còn hàng") && p.Quantity > 0)
                .OrderByDescending(p => p.CreatedDate)
                .Take(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<Pet>> GetRelatedAsync(int petId, int? breedId, int? petTypeId, int count = 4)
        {
            var related = await BaseQuery()
                .Where(p => p.Id != petId && p.BreedId == breedId && (p.Status == "Available" || p.Status == "Còn hàng") && p.Quantity > 0)
                .Take(count)
                .ToListAsync();

            if (!related.Any() && petTypeId.HasValue)
            {
                related = await BaseQuery()
                    .Where(p => p.Id != petId && p.Breed != null && p.Breed.PetTypeId == petTypeId.Value && (p.Status == "Available" || p.Status == "Còn hàng") && p.Quantity > 0)
                    .Take(count)
                    .ToListAsync();
            }

            return related;
        }

        public async Task<IEnumerable<Pet>> SearchPetsAsync(string keyword, int limit = 5)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return Enumerable.Empty<Pet>();

            var term = keyword.Trim().ToLower();
            return await BaseQuery()
                .Where(p => (p.Status == "Available" || p.Status == "Còn hàng") && p.Quantity > 0 &&
                            (p.Name.ToLower().Contains(term) ||
                             (p.Breed != null && p.Breed.Name.ToLower().Contains(term)) ||
                             (p.Breed != null && p.Breed.PetType != null && p.Breed.PetType.Name.ToLower().Contains(term)) ||
                             (p.Color != null && p.Color.ToLower().Contains(term))))
                .OrderByDescending(p => p.CreatedDate)
                .Take(limit)
                .ToListAsync();
        }

        public async Task AddAsync(Pet pet)
        {
            _context.Pets.Add(pet);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Pet pet)
        {
            _context.Pets.Update(pet);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var pet = await _context.Pets.FindAsync(id);
            if (pet != null)
            {
                bool hasOrders = await _context.OrderDetails.AnyAsync(od => od.PetId == id);
                if (hasOrders)
                {
                    pet.Status = "Hidden";
                    pet.Quantity = 0;
                    await _context.SaveChangesAsync();
                    return;
                }

                var cartPets = await _context.UserCartItems.Where(c => c.PetId == id).ToListAsync();
                if (cartPets.Any()) _context.UserCartItems.RemoveRange(cartPets);

                _context.Pets.Remove(pet);
                await _context.SaveChangesAsync();
            }
        }
    }
}
