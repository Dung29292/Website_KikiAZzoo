using KikiAZZoo.Models;

namespace KikiAZZoo.Models.ViewModels
{
    public class PetListViewModel
    {
        public IEnumerable<Pet> Pets { get; set; } = [];
        public IEnumerable<PetType> PetTypes { get; set; } = [];
        public IEnumerable<Breed> Breeds { get; set; } = [];

        public int? PetTypeId { get; set; }
        public int? BreedId { get; set; }
        public string? Gender { get; set; }
        public string? AgeRange { get; set; } // "baby", "young", "adult"
        public bool? IsVaccinated { get; set; }
        public bool? HasMicrochip { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string? Search { get; set; }
        public string? SortBy { get; set; } = "newest";
        public string? StatusFilter { get; set; } = "available";

        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int PageSize { get; set; } = 9;
        public int TotalCount { get; set; }
    }
}
