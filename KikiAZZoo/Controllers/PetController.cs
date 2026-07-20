using KikiAZZoo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KikiAZZoo.Controllers
{
    [Authorize]
    public class PetController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PetController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var pets = await _context.PetProfiles
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedDate)
                .ToListAsync();

            return View(pets);
        }

        [HttpGet]
        public async Task<IActionResult> Upsert(int? id)
        {
            var userId = _userManager.GetUserId(User);

            if (id == null || id == 0)
            {
                // Create mode
                return View(new PetProfile { UserId = userId! });
            }
            else
            {
                // Edit mode
                var pet = await _context.PetProfiles
                    .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

                if (pet == null) return NotFound();

                return View(pet);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upsert(PetProfile pet, IFormFile? imageFile)
        {
            var userId = _userManager.GetUserId(User);
            pet.UserId = userId!;

            // Remove safety validations for relations
            ModelState.Remove("ApplicationUser");
            ModelState.Remove("imageFile");

            if (!ModelState.IsValid)
            {
                return View(pet);
            }

            if (imageFile != null)
            {
                pet.ImageUrl = await SaveImageAsync(imageFile);
            }

            if (pet.Id == 0)
            {
                // Add mode
                _context.PetProfiles.Add(pet);
                TempData["Success"] = $"Đã thêm hồ sơ bé '{pet.Name}' thành công!";
            }
            else
            {
                // Edit mode
                var existing = await _context.PetProfiles
                    .FirstOrDefaultAsync(p => p.Id == pet.Id && p.UserId == userId);

                if (existing == null) return NotFound();

                existing.Name = pet.Name;
                existing.Species = pet.Species;
                existing.Breed = pet.Breed;
                existing.Age = pet.Age;
                existing.Weight = pet.Weight;
                existing.HealthNotes = pet.HealthNotes;
                
                if (pet.ImageUrl != null)
                {
                    existing.ImageUrl = pet.ImageUrl;
                }

                _context.PetProfiles.Update(existing);
                TempData["Success"] = $"Đã cập nhật hồ sơ bé '{pet.Name}' thành công!";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _userManager.GetUserId(User);
            var pet = await _context.PetProfiles
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

            if (pet == null) return NotFound();

            _context.PetProfiles.Remove(pet);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã xoá hồ sơ thú cưng thành công!";
            return RedirectToAction(nameof(Index));
        }

        private static async Task<string> SaveImageAsync(IFormFile image)
        {
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
            var savePath = Path.Combine("wwwroot/images/pets", fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(savePath)!);
            await using var stream = new FileStream(savePath, FileMode.Create);
            await image.CopyToAsync(stream);
            return "/images/pets/" + fileName;
        }
    }
}
