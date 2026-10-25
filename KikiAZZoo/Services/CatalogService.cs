using KikiAZZoo.Models;
using KikiAZZoo.Repositories;
using Microsoft.EntityFrameworkCore;

namespace KikiAZZoo.Services
{
    public class CatalogService : ICatalogService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IBrandRepository _brandRepository;

        public CatalogService(ApplicationDbContext context, ICategoryRepository categoryRepository, IBrandRepository brandRepository)
        {
            _context = context;
            _categoryRepository = categoryRepository;
            _brandRepository = brandRepository;
        }

        public async Task<List<CategoryGroup>> GetCategoryGroupsAsync()
        {
            return await _context.CategoryGroups
                .Where(g => g.Name != "Thú cưng")
                .OrderBy(g => g.SortOrder)
                .Include(g => g.Categories)
                .ToListAsync();
        }

        public async Task<List<PetType>> GetPetTypesAsync()
        {
            return await _context.PetTypes.OrderBy(p => p.SortOrder).ToListAsync();
        }

        public async Task<List<Brand>> GetBrandsAsync()
        {
            return (await _brandRepository.GetAllAsync()).ToList();
        }

        public async Task<List<PriceRange>> GetPriceRangesAsync()
        {
            return await _context.PriceRanges.OrderBy(r => r.SortOrder).ToListAsync();
        }

        public async Task<List<Coupon>> GetActiveCouponsAsync()
        {
            var now = DateTime.Now;
            return await _context.Coupons
                .Where(c => c.IsActive && c.StartDate <= now && c.EndDate >= now)
                .OrderBy(c => c.MinOrderValue)
                .ToListAsync();
        }

        public async Task<List<Category>> GetCategoriesByGroupAsync(int groupId)
        {
            return await _context.Categories
                .Where(c => c.CategoryGroupId == groupId)
                .OrderBy(c => c.SortOrder)
                .ToListAsync();
        }

        public FilteredCatalogData FilterCatalogByAvailableIds(
            List<CategoryGroup> allGroups,
            List<Category> allCategories,
            List<PetType> allPetTypes,
            List<Brand> allBrands,
            HashSet<int> availableCategoryIds,
            HashSet<int> availableGroupIds,
            HashSet<int> availablePetTypeIds,
            HashSet<int> availableBrandIds,
            int? selectedCategoryGroupId = null,
            int? selectedCategoryId = null,
            int? selectedPetTypeId = null,
            int? selectedBrandId = null)
        {
            // Filter CategoryGroups và nested Categories
            var filteredGroups = allGroups
                .Where(g => availableGroupIds.Contains(g.Id) || (selectedCategoryGroupId.HasValue && selectedCategoryGroupId.Value == g.Id))
                .Select(g =>
                {
                    var categoriesForGroup = (g.Categories ?? Enumerable.Empty<Category>())
                        .Where(c => availableCategoryIds.Contains(c.Id) || (selectedCategoryId.HasValue && selectedCategoryId.Value == c.Id))
                        .ToList();
                    g.Categories = categoriesForGroup;
                    return g;
                })
                .Where(g => (g.Categories ?? Enumerable.Empty<Category>()).Any() || availableGroupIds.Contains(g.Id))
                .ToList();

            var filteredCategories = allCategories
                .Where(c => availableCategoryIds.Contains(c.Id) || (selectedCategoryId.HasValue && selectedCategoryId.Value == c.Id))
                .ToList();

            var filteredPetTypes = allPetTypes
                .Where(pt => availablePetTypeIds.Contains(pt.Id) || (selectedPetTypeId.HasValue && selectedPetTypeId.Value == pt.Id))
                .ToList();

            var filteredBrands = allBrands
                .Where(b => availableBrandIds.Contains(b.Id) || (selectedBrandId.HasValue && selectedBrandId.Value == b.Id))
                .ToList();

            return new FilteredCatalogData
            {
                CategoryGroups = filteredGroups,
                Categories = filteredCategories,
                PetTypes = filteredPetTypes,
                Brands = filteredBrands
            };
        }
    }
}
