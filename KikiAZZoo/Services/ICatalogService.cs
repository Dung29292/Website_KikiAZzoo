using KikiAZZoo.Models;

namespace KikiAZZoo.Services
{
    /// <summary>
    /// Service để quản lý danh mục, category groups, pet types, brands
    /// Tái sử dụng logic lặp lại giữa Home, Product controllers
    /// </summary>
    public interface ICatalogService
    {
        Task<List<CategoryGroup>> GetCategoryGroupsAsync();
        Task<List<PetType>> GetPetTypesAsync();
        Task<List<Brand>> GetBrandsAsync();
        Task<List<PriceRange>> GetPriceRangesAsync();
        Task<List<Coupon>> GetActiveCouponsAsync();
        
        /// <summary>
        /// Lấy categories theo category group
        /// </summary>
        Task<List<Category>> GetCategoriesByGroupAsync(int groupId);
        
        /// <summary>
        /// Lọc categories/brands/petTypes dựa trên available IDs
        /// </summary>
        FilteredCatalogData FilterCatalogByAvailableIds(
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
            int? selectedBrandId = null);
    }

    public class FilteredCatalogData
    {
        public List<CategoryGroup> CategoryGroups { get; set; } = [];
        public List<Category> Categories { get; set; } = [];
        public List<PetType> PetTypes { get; set; } = [];
        public List<Brand> Brands { get; set; } = [];
    }
}
