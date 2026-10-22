using KikiAZZoo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace KikiAZZoo.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class PetController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public PetController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index(
            string? search,
            int? petTypeId,
            int? breedId,
            string? status,
            int page = 1)
        {
            int pageSize = 10;
            var query = _context.Pets
                .Include(p => p.Breed)
                    .ThenInclude(b => b!.PetType)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(term) ||
                                         (p.SkuCode != null && p.SkuCode.ToLower().Contains(term)) ||
                                         (p.Color != null && p.Color.ToLower().Contains(term)));
            }

            if (petTypeId.HasValue)
            {
                query = query.Where(p => p.Breed != null && p.Breed.PetTypeId == petTypeId.Value);
            }

            if (breedId.HasValue)
            {
                query = query.Where(p => p.BreedId == breedId.Value);
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(p => p.Status == status);
            }

            query = query.OrderByDescending(p => p.CreatedDate);

            int totalCount = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            if (totalPages < 1) totalPages = 1;
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            var pets = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.PetTypes = await _context.PetTypes.OrderBy(p => p.SortOrder).ToListAsync();
            ViewBag.Breeds = await _context.Breeds.OrderBy(b => b.Name).ToListAsync();
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;
            ViewBag.Search = search;
            ViewBag.SelectedPetTypeId = petTypeId;
            ViewBag.SelectedBreedId = breedId;
            ViewBag.SelectedStatus = status;

            return View(pets);
        }

        [HttpGet]
        public async Task<IActionResult> Upsert(int? id)
        {
            await PopulateDropdowns();

            if (id == null || id == 0)
            {
                // Create
                return View(new Pet
                {
                    Status = "Available",
                    Quantity = 1,
                    AgeMonths = 2,
                    HealthGuaranteeDays = 30,
                    IsVaccinated = true,
                    IsDewormed = true,
                    CreatedDate = DateTime.Now
                });
            }

            // Edit
            var pet = await _context.Pets
                .Include(p => p.Breed)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pet == null) return NotFound();
            return View(pet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upsert(Pet pet, IFormFile? imageFile)
        {
            ModelState.Remove("Breed");
            ModelState.Remove("imageFile");

            if (!ModelState.IsValid)
            {
                await PopulateDropdowns();
                return View(pet);
            }

            if (imageFile != null)
            {
                string uploadDir = Path.Combine(_webHostEnvironment.WebRootPath, "images", "pets");
                if (!Directory.Exists(uploadDir))
                {
                    Directory.CreateDirectory(uploadDir);
                }

                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                string filePath = Path.Combine(uploadDir, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                pet.ImageUrl = "/images/pets/" + fileName;
            }

            if (pet.Id == 0)
            {
                pet.CreatedDate = DateTime.Now;
                _context.Pets.Add(pet);
                TempData["Success"] = $"Đã thêm bé '{pet.Name}' thành công!";
            }
            else
            {
                var existing = await _context.Pets.FindAsync(pet.Id);
                if (existing == null) return NotFound();

                existing.Name = pet.Name;
                existing.SkuCode = pet.SkuCode;
                existing.BreedId = pet.BreedId;
                existing.AgeMonths = pet.AgeMonths;
                existing.BirthDate = pet.BirthDate;
                existing.Gender = pet.Gender;
                existing.Color = pet.Color;
                existing.Price = pet.Price;
                existing.OriginalPrice = pet.OriginalPrice;
                existing.IsVaccinated = pet.IsVaccinated;
                existing.VaccinationDetails = pet.VaccinationDetails;
                existing.IsDewormed = pet.IsDewormed;
                existing.MicrochipCode = pet.MicrochipCode;
                existing.HealthGuaranteeDays = pet.HealthGuaranteeDays;
                existing.Status = pet.Status;
                existing.Quantity = pet.Quantity;
                existing.Description = pet.Description;

                if (!string.IsNullOrEmpty(pet.ImageUrl))
                {
                    existing.ImageUrl = pet.ImageUrl;
                }

                TempData["Success"] = $"Đã cập nhật thông tin bé '{pet.Name}' thành công!";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var pet = await _context.Pets.FindAsync(id);
            if (pet != null)
            {
                _context.Pets.Remove(pet);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Đã xóa bé '{pet.Name}' khỏi hệ thống.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var pet = await _context.Pets.FindAsync(id);
            if (pet == null) return Json(new { success = false, message = "Không tìm thấy thú cưng." });

            pet.Status = (pet.Status == "Available" || pet.Status == "Còn hàng") ? "Sold" : "Available";
            if (pet.Status == "Available" && pet.Quantity <= 0)
            {
                pet.Quantity = 1;
            }
            await _context.SaveChangesAsync();

            return Json(new { success = true, newStatus = pet.Status, isAvailable = pet.IsAvailable });
        }

        private async Task PopulateDropdowns()
        {
            var breeds = await _context.Breeds
                .Include(b => b.PetType)
                .OrderBy(b => b.PetTypeId)
                .ThenBy(b => b.Name)
                .Select(b => new {
                    b.Id,
                    DisplayName = $"{(b.PetType != null ? b.PetType.Name + " - " : "")}{b.Name}"
                })
                .ToListAsync();

            ViewBag.BreedList = new SelectList(breeds, "Id", "DisplayName");
        }
    }
}
