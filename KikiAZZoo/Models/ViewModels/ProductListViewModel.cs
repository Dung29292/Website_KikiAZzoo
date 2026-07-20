using KikiAZZoo.Models;

namespace KikiAZZoo.Models.ViewModels
{
    public class ProductListViewModel
    {
        public IEnumerable<Product> Products { get; set; } = [];
        public IEnumerable<CategoryGroup> CategoryGroups { get; set; } = [];
        public IEnumerable<Category> Categories { get; set; } = [];
        public IEnumerable<PetType> PetTypes { get; set; } = [];
        public IEnumerable<Brand> Brands { get; set; } = [];
        public IEnumerable<PriceRange> PriceRanges { get; set; } = [];
        public int? CategoryId { get; set; }
        public int? CategoryGroupId { get; set; }
        public int? PetTypeId { get; set; }
        public int? BrandId { get; set; }
        public int? PriceRangeId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string? Search { get; set; }
        public string? SortBy { get; set; } = "default";
        public string? StatusFilter { get; set; }
        public int? RatingFilter { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int PageSize { get; set; } = 8;
        public int TotalCount { get; set; }
        public string? PetAge { get; set; }
        public string? FoodType { get; set; }
    }
}

