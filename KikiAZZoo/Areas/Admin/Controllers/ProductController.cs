using KikiAZZoo.Models;
using KikiAZZoo.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace KikiAZZoo.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = $"{SD.Role_Admin},{SD.Role_Employee}")]
    public class ProductController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly ApplicationDbContext _context;

        public ProductController(IProductRepository productRepository, ICategoryRepository categoryRepository, ApplicationDbContext context)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _context = context;
        }

        public async Task<IActionResult> Index(
            string? search,
            int? categoryId,
            int? brandId,
            int? petTypeId,
            decimal? minPrice,
            decimal? maxPrice,
            string? sortBy,
            string? status,
            int page = 1)
        {
            int pageSize = 10;
            bool? isActiveFilter = null;
            if (status == "active") isActiveFilter = true;
            else if (status == "inactive") isActiveFilter = false;

            // Get products (including inactive products unless filtered)
            var products = await _productRepository.GetFilteredAsync(categoryId, null, petTypeId, brandId, minPrice, maxPrice, search, isActiveFilter);

            // Sorting
            switch (sortBy)
            {
                case "price_asc":
                    products = products.OrderBy(p => p.Price);
                    break;
                case "price_desc":
                    products = products.OrderByDescending(p => p.Price);
                    break;
                case "name_asc":
                    products = products.OrderBy(p => p.Name);
                    break;
                case "name_desc":
                    products = products.OrderByDescending(p => p.Name);
                    break;
                case "stock_asc":
                    products = products.OrderBy(p => p.Stock);
                    break;
                case "stock_desc":
                    products = products.OrderByDescending(p => p.Stock);
                    break;
                default:
                    products = products.OrderByDescending(p => p.Id);
                    break;
            }

            var totalItems = products.Count();
            var paginatedProducts = products.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.Search = search;
            ViewBag.CategoryId = categoryId;
            ViewBag.BrandId = brandId;
            ViewBag.PetTypeId = petTypeId;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.SortBy = sortBy;
            ViewBag.Status = status;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            // Populate filter options
            ViewBag.CategoriesList = (await _categoryRepository.GetAllAsync()).ToList();
            ViewBag.BrandsList = await _context.Brands.OrderBy(b => b.Name).ToListAsync();
            ViewBag.PetTypesList = await _context.PetTypes.OrderBy(p => p.SortOrder).ToListAsync();

            return View(paginatedProducts);
        }

        [Authorize(Roles = SD.Role_Admin)]
        public async Task<IActionResult> Add()
        {
            await LoadViewBagsAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = SD.Role_Admin)]
        public async Task<IActionResult> Add(Product product, IFormFile? imageUrl)
        {
            if (ModelState.IsValid)
            {
                product.OriginalPrice = product.Price;
                if (imageUrl != null)
                    product.ImageUrl = await SaveImageAsync(imageUrl);
                await _productRepository.AddAsync(product);
                return RedirectToAction(nameof(Index));
            }
            await LoadViewBagsAsync();
            return View(product);
        }

        public async Task<IActionResult> Display(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null) return NotFound();
            return View(product);
        }

        [Authorize(Roles = SD.Role_Admin)]
        public async Task<IActionResult> Update(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null) return NotFound();
            await LoadViewBagsAsync(product.CategoryId, product.BrandId, product.PetTypeId);
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = SD.Role_Admin)]
        public async Task<IActionResult> Update(int id, Product product, IFormFile? imageUrl)
        {
            ModelState.Remove("ImageUrl");
            if (id != product.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var existing = await _productRepository.GetByIdAsync(id);
                if (existing == null) return NotFound();

                existing.Name = product.Name;
                existing.Price = product.Price;
                existing.OriginalPrice = product.Price;
                existing.Description = product.Description;
                existing.Ingredients = product.Ingredients;
                existing.UsageGuide = product.UsageGuide;
                existing.CategoryId = product.CategoryId;
                existing.BrandId = product.BrandId;
                existing.Rating = product.Rating;
                existing.Stock = product.Stock;
                existing.IsActive = product.IsActive;

                if (imageUrl != null)
                    existing.ImageUrl = await SaveImageAsync(imageUrl);

                await _productRepository.UpdateAsync(existing);
                return RedirectToAction(nameof(Index));
            }
            await LoadViewBagsAsync(product.CategoryId, product.BrandId, product.PetTypeId);
            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id, bool active)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null) return NotFound();
            product.IsActive = active;
            await _productRepository.UpdateAsync(product);
            return Json(new { success = true, isActive = product.IsActive });
        }

        [Authorize(Roles = SD.Role_Admin)]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null) return NotFound();
            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = SD.Role_Admin)]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _productRepository.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }

        private async Task LoadViewBagsAsync(int? categoryId = null, int? brandId = null, int? petTypeId = null)
        {
            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categories.Select(c => new { c.Id, Name = $"{c.CategoryGroup?.Name} › {c.Name}" }), "Id", "Name", categoryId);
            ViewBag.Brands = new SelectList(await _context.Brands.OrderBy(b => b.Name).ToListAsync(), "Id", "Name", brandId);
            ViewBag.PetTypes = new SelectList(await _context.PetTypes.OrderBy(p => p.SortOrder).ToListAsync(), "Id", "Name", petTypeId);
        }

        private static async Task<string> SaveImageAsync(IFormFile image)
        {
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
            var savePath = Path.Combine("wwwroot/images", fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(savePath)!);
            await using var stream = new FileStream(savePath, FileMode.Create);
            await image.CopyToAsync(stream);
            return "/images/" + fileName;
        }
    }
}
