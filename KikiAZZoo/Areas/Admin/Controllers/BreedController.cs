using KikiAZZoo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace KikiAZZoo.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = $"{SD.Role_Admin},{SD.Role_Employee}")]
    public class BreedController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BreedController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? search, int? petTypeId, int page = 1)
        {
            int pageSize = 12;
            var query = _context.Breeds
                .Include(b => b.PetType)
                .Include(b => b.Pets)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(b => b.Name.ToLower().Contains(term) ||
                                         (b.Code != null && b.Code.ToLower().Contains(term)) ||
                                         (b.Origin != null && b.Origin.ToLower().Contains(term)) ||
                                         (b.PetType != null && b.PetType.Name.ToLower().Contains(term)));
            }

            if (petTypeId.HasValue && petTypeId.Value > 0)
            {
                query = query.Where(b => b.PetTypeId == petTypeId.Value);
            }

            query = query.OrderBy(b => b.PetTypeId).ThenBy(b => b.Name);

            int totalCount = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            if (totalPages < 1) totalPages = 1;
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            var breeds = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.PetTypes = await _context.PetTypes.OrderBy(p => p.SortOrder).ToListAsync();
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;
            ViewBag.Search = search;
            ViewBag.SelectedPetTypeId = petTypeId;

            return View(breeds);
        }

        [HttpGet]
        [Authorize(Roles = SD.Role_Admin)]
        public async Task<IActionResult> Upsert(int? id)
        {
            await PopulatePetTypesDropdown();

            if (id == null || id == 0)
            {
                // Create
                return View(new Breed());
            }

            // Edit
            var breed = await _context.Breeds
                .Include(b => b.PetType)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (breed == null) return NotFound();
            return View(breed);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = SD.Role_Admin)]
        public async Task<IActionResult> Upsert(Breed breed)
        {
            ModelState.Remove("PetType");
            ModelState.Remove("Pets");

            if (!ModelState.IsValid)
            {
                await PopulatePetTypesDropdown(breed.PetTypeId);
                return View(breed);
            }

            // Check duplicate name within the same pet type
            bool isDuplicate = await _context.Breeds.AnyAsync(b =>
                b.Id != breed.Id &&
                b.PetTypeId == breed.PetTypeId &&
                b.Name.ToLower() == breed.Name.Trim().ToLower());

            if (isDuplicate)
            {
                ModelState.AddModelError("Name", "Giống loài này đã tồn tại trong nhóm loài thú cưng đã chọn.");
                await PopulatePetTypesDropdown(breed.PetTypeId);
                return View(breed);
            }

            if (string.IsNullOrWhiteSpace(breed.Code))
            {
                breed.Code = GenerateBreedCode(breed.Name);
            }
            else
            {
                breed.Code = Regex.Replace(breed.Code.Trim().ToUpper(), @"[^A-Z0-9_-]", "");
            }

            if (breed.Id == 0)
            {
                breed.Name = breed.Name.Trim();
                breed.Origin = breed.Origin?.Trim();
                breed.Description = breed.Description?.Trim();

                _context.Breeds.Add(breed);
                TempData["Success"] = $"Đã thêm giống loài '{breed.Name}' (Mã: {breed.Code}) thành công!";
            }
            else
            {
                var existing = await _context.Breeds.FindAsync(breed.Id);
                if (existing == null) return NotFound();

                existing.Name = breed.Name.Trim();
                existing.Code = breed.Code;
                existing.PetTypeId = breed.PetTypeId;
                existing.Origin = breed.Origin?.Trim();
                existing.Description = breed.Description?.Trim();

                TempData["Success"] = $"Đã cập nhật giống loài '{breed.Name}' (Mã: {breed.Code}) thành công!";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private static string GenerateBreedCode(string breedName)
        {
            if (string.IsNullOrWhiteSpace(breedName)) return "BRD";
            string unaccented = RemoveDiacritics(breedName.Trim());
            var parts = unaccented.Split(new[] { ' ', '-', '(', ')' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
            {
                // Take up to 3 uppercase letters: first letter of each word
                string acronym = string.Concat(parts.Select(p => p[0])).ToUpper();
                if (acronym.Length >= 2 && acronym.Length <= 4) return acronym;
            }

            string clean = Regex.Replace(unaccented, @"[^a-zA-Z0-9]", "").ToUpper();
            if (clean.Length >= 3) return clean.Substring(0, 3);
            return clean.PadRight(3, 'X');
        }

        private static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }
            return sb.ToString().Normalize(NormalizationForm.FormC).Replace("đ", "d").Replace("Đ", "D");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = SD.Role_Admin)]
        public async Task<IActionResult> Delete(int id)
        {
            var breed = await _context.Breeds
                .Include(b => b.Pets)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (breed == null)
            {
                TempData["Error"] = "Không tìm thấy giống loài cần xóa.";
                return RedirectToAction(nameof(Index));
            }

            int petCount = breed.Pets?.Count ?? 0;
            if (petCount > 0)
            {
                TempData["Error"] = $"Không thể xóa giống loài '{breed.Name}' vì đang có {petCount} bé thú cưng liên kết!";
                return RedirectToAction(nameof(Index));
            }

            _context.Breeds.Remove(breed);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Đã xóa giống loài '{breed.Name}' thành công.";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulatePetTypesDropdown(int? selectedId = null)
        {
            var petTypes = await _context.PetTypes.OrderBy(p => p.SortOrder).ToListAsync();
            ViewBag.PetTypeList = new SelectList(petTypes, "Id", "Name", selectedId);
        }
    }
}
