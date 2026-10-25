using KikiAZZoo.Models;
using KikiAZZoo.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KikiAZZoo.Controllers
{
    [Route("thu-cung")]
    [Route("PetShop")]
    public class PetShopController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PetShopController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index(
            int? petTypeId,
            int? breedId,
            string? gender,
            string? ageRange,
            bool? isVaccinated,
            bool? hasMicrochip,
            decimal? minPrice,
            decimal? maxPrice,
            string? search,
            string? sortBy = "newest",
            string? statusFilter = "available",
            int page = 1)
        {
            const int pageSize = 9;

            var query = _context.Pets
                .Include(p => p.Breed)
                    .ThenInclude(b => b!.PetType)
                .AsQueryable();

            // Status filter
            if (statusFilter == "available")
            {
                query = query.Where(p => (p.Status == "Available" || p.Status == "Còn hàng") && p.Quantity > 0);
            }
            else if (statusFilter == "sale")
            {
                query = query.Where(p => p.OriginalPrice.HasValue && p.OriginalPrice > p.Price);
            }

            // Pet Type filter
            if (petTypeId.HasValue)
            {
                query = query.Where(p => p.Breed != null && p.Breed.PetTypeId == petTypeId.Value);
            }

            // Breed filter
            if (breedId.HasValue)
            {
                query = query.Where(p => p.BreedId == breedId.Value);
            }

            // Gender filter
            if (!string.IsNullOrEmpty(gender) && gender != "all")
            {
                query = query.Where(p => p.Gender == gender);
            }

            // Age Range filter
            if (!string.IsNullOrEmpty(ageRange))
            {
                query = ageRange switch
                {
                    "baby" => query.Where(p => p.AgeMonths <= 3),
                    "young" => query.Where(p => p.AgeMonths > 3 && p.AgeMonths <= 6),
                    "adult" => query.Where(p => p.AgeMonths > 6),
                    _ => query
                };
            }

            // Vaccination filter
            if (isVaccinated.HasValue && isVaccinated.Value)
            {
                query = query.Where(p => p.IsVaccinated);
            }

            // Microchip filter
            if (hasMicrochip.HasValue && hasMicrochip.Value)
            {
                query = query.Where(p => !string.IsNullOrEmpty(p.MicrochipCode));
            }

            // Price range filter
            if (minPrice.HasValue) query = query.Where(p => p.Price >= minPrice.Value);
            if (maxPrice.HasValue) query = query.Where(p => p.Price <= maxPrice.Value);

            // Search query
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(p =>
                    p.Name.ToLower().Contains(term) ||
                    (p.Description != null && p.Description.ToLower().Contains(term)) ||
                    (p.Breed != null && p.Breed.Name.ToLower().Contains(term)) ||
                    (p.Color != null && p.Color.ToLower().Contains(term)));
            }

            // Sorting
            query = sortBy switch
            {
                "price_asc" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                "name" => query.OrderBy(p => p.Name),
                "age_asc" => query.OrderBy(p => p.AgeMonths),
                _ => query.OrderByDescending(p => p.CreatedDate)
            };

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            if (totalPages < 1) totalPages = 1;
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            var pets = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var petTypes = await _context.PetTypes
                .OrderBy(pt => pt.SortOrder)
                .ToListAsync();

            var breedsQuery = _context.Breeds.AsQueryable();
            if (petTypeId.HasValue)
            {
                breedsQuery = breedsQuery.Where(b => b.PetTypeId == petTypeId.Value);
            }
            var breeds = await breedsQuery.OrderBy(b => b.Name).ToListAsync();

            var model = new PetListViewModel
            {
                Pets = pets,
                PetTypes = petTypes,
                Breeds = breeds,
                PetTypeId = petTypeId,
                BreedId = breedId,
                Gender = gender,
                AgeRange = ageRange,
                IsVaccinated = isVaccinated,
                HasMicrochip = hasMicrochip,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                Search = search,
                SortBy = sortBy,
                StatusFilter = statusFilter,
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            var now = DateTime.Now;
            ViewBag.ActiveCoupons = await _context.Coupons
                .Where(c => c.IsActive && c.StartDate <= now && c.EndDate >= now)
                .OrderBy(c => c.MinOrderValue)
                .ToListAsync();

            return View(model);
        }

        [HttpGet("chi-tiet/{id}")]
        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var pet = await _context.Pets
                .Include(p => p.Breed)
                    .ThenInclude(b => b!.PetType)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pet == null) return NotFound();

            // Related pets
            var relatedPets = await _context.Pets
                .Include(p => p.Breed)
                .Where(p => p.Id != id && p.BreedId == pet.BreedId && p.Status == "Available")
                .Take(4)
                .ToListAsync();

            if (!relatedPets.Any() && pet.Breed != null)
            {
                relatedPets = await _context.Pets
                    .Include(p => p.Breed)
                    .Where(p => p.Id != id && p.Breed!.PetTypeId == pet.Breed.PetTypeId && p.Status == "Available")
                    .Take(4)
                    .ToListAsync();
            }

            var activeCoupons = await _context.Coupons
                .Where(c => c.IsActive && c.StartDate <= DateTime.Now && c.EndDate >= DateTime.Now)
                .OrderBy(c => c.MinOrderValue)
                .ToListAsync();

            ViewBag.ActiveCoupons = activeCoupons;
            ViewBag.RelatedPets = relatedPets;
            return View(pet);
        }

        [HttpGet("QuickView/{id}")]
        public async Task<IActionResult> QuickView(int id)
        {
            var pet = await _context.Pets
                .Include(p => p.Breed)
                    .ThenInclude(b => b!.PetType)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pet == null) return NotFound();

            var activeCoupons = await _context.Coupons
                .Where(c => c.IsActive && c.StartDate <= DateTime.Now && c.EndDate >= DateTime.Now)
                .OrderBy(c => c.MinOrderValue)
                .ToListAsync();
            ViewBag.ActiveCoupons = activeCoupons;

            return PartialView("_PetQuickViewModal", pet);
        }

        [HttpGet("GetBreedsByPetType")]
        public async Task<IActionResult> GetBreedsByPetType(int? petTypeId)
        {
            var query = _context.Breeds.AsQueryable();
            if (petTypeId.HasValue)
            {
                query = query.Where(b => b.PetTypeId == petTypeId.Value);
            }
            var breeds = await query.Select(b => new { id = b.Id, name = b.Name }).ToListAsync();
            return Json(breeds);
        }
    }
}
