using KikiAZZoo.Models;
using KikiAZZoo.Models.ViewModels;
using KikiAZZoo.Repositories;
using KikiAZZoo.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace KikiAZZoo.Controllers
{
    public class ProductController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly IPetRepository _petRepository;
        private readonly ICatalogService _catalogService;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProductController(
            IProductRepository productRepository,
            IPetRepository petRepository,
            ICatalogService catalogService,
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _productRepository = productRepository;
            _petRepository = petRepository;
            _catalogService = catalogService;
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(int? categoryId, int? categoryGroupId, int? petTypeId, int? brandId, int? priceRangeId, string? search, string? sortBy, string? statusFilter, int? ratingFilter, string? petAge, string? foodType, int page = 1)
        {
            const int pageSize = 8;
            decimal? minPrice = null, maxPrice = null;

            if (priceRangeId.HasValue)
            {
                var range = await _context.PriceRanges.FindAsync(priceRangeId.Value);
                if (range != null)
                {
                    minPrice = range.MinPrice;
                    maxPrice = range.MaxPrice;
                }
            }

            var allProducts = (await _productRepository.GetFilteredAsync(categoryId, categoryGroupId, petTypeId, brandId, minPrice, maxPrice, search)).ToList();

            // Apply pet age filter
            if (!string.IsNullOrEmpty(petAge))
            {
                if (petAge == "young")
                {
                    allProducts = allProducts.Where(p => 
                        p.Name.Contains("Puppy", StringComparison.OrdinalIgnoreCase) || 
                        p.Name.Contains("con", StringComparison.OrdinalIgnoreCase) || 
                        p.Name.Contains("tháng", StringComparison.OrdinalIgnoreCase) || 
                        p.Name.Contains("nhỏ", StringComparison.OrdinalIgnoreCase) || 
                        (p.Description != null && (p.Description.Contains("chó con", StringComparison.OrdinalIgnoreCase) || p.Description.Contains("mèo con", StringComparison.OrdinalIgnoreCase)))
                    ).ToList();
                }
                else if (petAge == "adult")
                {
                    allProducts = allProducts.Where(p => 
                        p.Name.Contains("Adult", StringComparison.OrdinalIgnoreCase) || 
                        p.Name.Contains("trưởng thành", StringComparison.OrdinalIgnoreCase) || 
                        p.Name.Contains("lớn", StringComparison.OrdinalIgnoreCase) || 
                        (p.Description != null && p.Description.Contains("trưởng thành", StringComparison.OrdinalIgnoreCase))
                    ).ToList();
                }
            }

            // Apply food type filter
            if (!string.IsNullOrEmpty(foodType))
            {
                if (foodType == "hat")
                {
                    allProducts = allProducts.Where(p => 
                        p.Name.Contains("hạt", StringComparison.OrdinalIgnoreCase) || 
                        p.Name.Contains("khô", StringComparison.OrdinalIgnoreCase) || 
                        (p.Description != null && p.Description.Contains("thức ăn khô", StringComparison.OrdinalIgnoreCase))
                    ).ToList();
                }
                else if (foodType == "pate")
                {
                    allProducts = allProducts.Where(p => 
                        p.Name.Contains("pate", StringComparison.OrdinalIgnoreCase) || 
                        p.Name.Contains("ướt", StringComparison.OrdinalIgnoreCase) || 
                        (p.Description != null && p.Description.Contains("thức ăn ướt", StringComparison.OrdinalIgnoreCase))
                    ).ToList();
                }
                else if (foodType == "snack")
                {
                    allProducts = allProducts.Where(p => 
                        p.Name.Contains("snack", StringComparison.OrdinalIgnoreCase) || 
                        p.Name.Contains("bánh thưởng", StringComparison.OrdinalIgnoreCase) || 
                        p.Name.Contains("súp thưởng", StringComparison.OrdinalIgnoreCase)
                    ).ToList();
                }
            }

            // Apply status filter
            if (!string.IsNullOrEmpty(statusFilter))
            {
                allProducts = statusFilter switch
                {
                    "sale" => allProducts.Where(p => p.OriginalPrice.HasValue && p.OriginalPrice.Value > p.Price).ToList(),
                    "bestseller" => allProducts.Where(p => p.SalesCount >= 40).ToList(),
                    "new" => allProducts.Where(p => p.CreatedDate >= DateTime.Now.AddDays(-30)).ToList(),
                    _ => allProducts
                };
            }

            // Apply rating filter
            if (ratingFilter.HasValue)
            {
                allProducts = allProducts.Where(p => p.Rating >= ratingFilter.Value).ToList();
            }

            // Apply sorting
            if (!string.IsNullOrEmpty(sortBy))
            {
                allProducts = sortBy switch
                {
                    "price_asc" => allProducts.OrderBy(p => p.Price).ToList(),
                    "price_desc" => allProducts.OrderByDescending(p => p.Price).ToList(),
                    "newest" => allProducts.OrderByDescending(p => p.CreatedDate).ToList(),
                    "best_selling" => allProducts.OrderByDescending(p => p.SalesCount).ToList(),
                    _ => allProducts
                };
            }

            var totalCount = allProducts.Count;
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));

            // Get all catalog data using service (excluding live pets category group)
            var categoryGroups = await _catalogService.GetCategoryGroupsAsync();
            var categories = await _context.Categories
                .Include(c => c.CategoryGroup)
                .Where(c => c.CategoryGroup == null || (c.CategoryGroupId != 1 && c.CategoryGroup.Name != "Thú cưng"))
                .OrderBy(c => c.SortOrder)
                .ToListAsync();
            var petTypes = await _catalogService.GetPetTypesAsync();
            var brands = await _catalogService.GetBrandsAsync();
            var priceRanges = await _catalogService.GetPriceRangesAsync();

            var matchingPets = new List<Pet>();
            var matchingPetsCount = 0;
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                var petQuery = _context.Pets
                    .Include(p => p.Breed)
                    .ThenInclude(b => b!.PetType)
                    .Where(p => (p.Status == "Available" || p.Status == "Còn hàng") && p.Quantity > 0 &&
                                (p.Name.ToLower().Contains(s) ||
                                 (p.Description != null && p.Description.ToLower().Contains(s)) ||
                                 (p.Color != null && p.Color.ToLower().Contains(s)) ||
                                 (p.Breed != null && p.Breed.Name.ToLower().Contains(s)) ||
                                 (p.Breed != null && p.Breed.PetType != null && p.Breed.PetType.Name.ToLower().Contains(s))));

                matchingPetsCount = await petQuery.CountAsync();
                matchingPets = await petQuery.Take(4).ToListAsync();
            }

            ViewBag.MatchingPetsCount = matchingPetsCount;
            ViewBag.MatchingPets = matchingPets;
            ViewBag.SearchKeyword = search;

            var model = new ProductListViewModel
            {
                Products = allProducts.Skip((page - 1) * pageSize).Take(pageSize),
                CategoryGroups = categoryGroups,
                Categories = categories,
                PetTypes = petTypes,
                Brands = brands,
                PriceRanges = priceRanges,
                CategoryId = categoryId,
                CategoryGroupId = categoryGroupId,
                PetTypeId = petTypeId,
                BrandId = brandId,
                PriceRangeId = priceRangeId,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                Search = search,
                SortBy = sortBy ?? "default",
                StatusFilter = statusFilter,
                RatingFilter = ratingFilter,
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = pageSize,
                TotalCount = totalCount,
                PetAge = petAge,
                FoodType = foodType
            };
            return View(model);
        }


        public async Task<IActionResult> Details(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null) return NotFound();

            var reviews = await _context.ProductReviews
                .Include(r => r.ApplicationUser)
                .Where(r => r.ProductId == id)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();

            bool hasPurchased = false;
            var userId = _userManager.GetUserId(User);
            if (!string.IsNullOrEmpty(userId))
            {
                hasPurchased = await _context.Orders
                    .AnyAsync(o => o.UserId == userId && o.OrderDetails!.Any(d => d.ProductId == id));
            }

            ViewBag.Reviews = reviews;
            ViewBag.HasPurchased = hasPurchased;
            return View(product);
        }

        [HttpGet]
        [Route("/api/products/{id}")]
        public async Task<IActionResult> GetProductJson(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null) return NotFound();

            return Json(new
            {
                id = product.Id,
                name = product.Name,
                price = product.Price,
                originalPrice = product.OriginalPrice,
                imageUrl = product.ImageUrl,
                category = product.Category?.Name ?? "",
                categoryGroup = product.Category?.CategoryGroup?.Name ?? "",
                description = product.Description ?? "",
                stock = product.Stock,
                rating = product.Rating,
                discountPercent = product.DiscountPercent
            });
        }

        [HttpGet]
        public async Task<IActionResult> Autocomplete(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
                return Json(new { term = "", totalProducts = 0, totalPets = 0, products = Array.Empty<object>(), pets = Array.Empty<object>() });

            var cleanTerm = term.Trim().ToLower();

            var productsQuery = _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive && (p.Category == null || p.Category.CategoryGroup == null || p.Category.CategoryGroup.Name != "Thú cưng") &&
                            (p.Name.ToLower().Contains(cleanTerm) ||
                             (p.Category != null && p.Category.Name.ToLower().Contains(cleanTerm))));

            var totalProducts = await productsQuery.CountAsync();
            var products = await productsQuery
                .Take(4)
                .Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    price = p.Price,
                    originalPrice = p.OriginalPrice,
                    imageUrl = p.ImageUrl ?? "/images/product-food.svg",
                    categoryName = p.Category != null ? p.Category.Name : "Sản phẩm"
                })
                .ToListAsync();

            var petsQuery = _context.Pets
                .Include(p => p.Breed)
                .ThenInclude(b => b!.PetType)
                .Where(p => (p.Status == "Available" || p.Status == "Còn hàng") && p.Quantity > 0 &&
                            (p.Name.ToLower().Contains(cleanTerm) ||
                             (p.Breed != null && p.Breed.Name.ToLower().Contains(cleanTerm)) ||
                             (p.Breed != null && p.Breed.PetType != null && p.Breed.PetType.Name.ToLower().Contains(cleanTerm))));

            var totalPets = await petsQuery.CountAsync();
            var pets = await petsQuery
                .Take(3)
                .Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    price = p.Price,
                    imageUrl = p.ImageUrl ?? "/images/pet-dog.svg",
                    breedName = p.Breed != null ? p.Breed.Name : "Thú cưng",
                    petTypeName = p.Breed != null && p.Breed.PetType != null ? p.Breed.PetType.Name : "Pet",
                    gender = p.Gender,
                    ageMonths = p.AgeMonths
                })
                .ToListAsync();

            return Json(new
            {
                term = term.Trim(),
                totalProducts,
                totalPets,
                products,
                pets
            });
        }
    }
}
