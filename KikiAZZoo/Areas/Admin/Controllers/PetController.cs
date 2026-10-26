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
        [Authorize(Roles = SD.Role_Admin)]
        public async Task<IActionResult> Upsert(int? id)
        {
            await PopulateDropdowns();

            if (id == null || id == 0)
            {
                // Create
                var newPet = new Pet
                {
                    Status = "Available",
                    Quantity = 1,
                    AgeMonths = 2,
                    HealthGuaranteeDays = 30,
                    IsVaccinated = true,
                    IsDewormed = true,
                    CreatedDate = DateTime.Now
                };

                var firstBreed = await _context.Breeds.Include(b => b.PetType).OrderBy(b => b.PetTypeId).ThenBy(b => b.Name).FirstOrDefaultAsync();
                if (firstBreed != null)
                {
                    newPet.BreedId = firstBreed.Id;
                    newPet.SkuCode = await GeneratePetSkuCode(firstBreed.Id);
                }

                return View(newPet);
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
        [Authorize(Roles = SD.Role_Admin)]
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
                if (string.IsNullOrWhiteSpace(pet.SkuCode))
                {
                    pet.SkuCode = await GeneratePetSkuCode(pet.BreedId);
                }
                else
                {
                    pet.SkuCode = pet.SkuCode.Trim();
                }

                pet.OriginalPrice = pet.Price;
                pet.CreatedDate = DateTime.Now;
                _context.Pets.Add(pet);
                TempData["Success"] = $"Đã thêm bé '{pet.Name}' thành công (Mã: {pet.SkuCode})!";
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
                existing.OriginalPrice = pet.Price;
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
        [Authorize(Roles = SD.Role_Admin)]
        public async Task<IActionResult> Delete(int id)
        {
            var pet = await _context.Pets.FindAsync(id);
            if (pet != null)
            {
                bool hasOrders = await _context.OrderDetails.AnyAsync(od => od.PetId == id);
                if (hasOrders)
                {
                    pet.Status = "Hidden";
                    pet.Quantity = 0;
                    await _context.SaveChangesAsync();

                    var cartPets = await _context.UserCartItems.Where(c => c.PetId == id).ToListAsync();
                    if (cartPets.Any())
                    {
                        _context.UserCartItems.RemoveRange(cartPets);
                        await _context.SaveChangesAsync();
                    }

                    TempData["Warning"] = $"Thú cưng '{pet.Name}' đã có trong lịch sử đơn hàng của khách. Hệ thống đã chuyển sang trạng thái [Tạm ẩn / Đã bán] để bảo toàn dữ liệu lịch sử đơn hàng.";
                    return RedirectToAction(nameof(Index));
                }

                try
                {
                    var cartPets = await _context.UserCartItems.Where(c => c.PetId == id).ToListAsync();
                    if (cartPets.Any()) _context.UserCartItems.RemoveRange(cartPets);

                    _context.Pets.Remove(pet);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Đã xóa bé '{pet.Name}' khỏi hệ thống.";
                }
                catch (Exception ex)
                {
                    pet.Status = "Hidden";
                    await _context.SaveChangesAsync();
                    TempData["Warning"] = $"Không thể xóa do ràng buộc dữ liệu ({ex.Message}). Bé '{pet.Name}' đã được chuyển sang trạng thái [Tạm ẩn].";
                }
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var pet = await _context.Pets.FindAsync(id);
            if (pet == null) return Json(new { success = false, message = "Không tìm thấy thú cưng." });

            pet.Status = (pet.Status == "Available" || pet.Status == "Còn hàng") ? "Hidden" : "Available";
            if (pet.Status == "Available" && pet.Quantity <= 0)
            {
                pet.Quantity = 1;
            }
            await _context.SaveChangesAsync();

            return Json(new { success = true, newStatus = pet.Status, isAvailable = pet.IsAvailable, statusText = pet.IsAvailable ? "Đang bán" : "Tạm ẩn" });
        }

        private async Task PopulateDropdowns()
        {
            var breeds = await _context.Breeds
                .Include(b => b.PetType)
                .OrderBy(b => b.PetTypeId)
                .ThenBy(b => b.Name)
                .Select(b => new {
                    b.Id,
                    DisplayName = $"{(b.PetType != null ? b.PetType.Name + " - " : "")}{b.Name}{(!string.IsNullOrEmpty(b.Code) ? $" [{b.Code}]" : "")}"
                })
                .ToListAsync();

            ViewBag.BreedList = new SelectList(breeds, "Id", "DisplayName");
        }

        [HttpGet]
        public async Task<IActionResult> GetNextSku(int breedId)
        {
            if (breedId <= 0)
            {
                return Json(new { success = false, sku = "" });
            }

            string nextSku = await GeneratePetSkuCode(breedId);
            return Json(new { success = true, sku = nextSku });
        }

        private async Task<string> GeneratePetSkuCode(int breedId)
        {
            var breed = await _context.Breeds.Include(b => b.PetType).FirstOrDefaultAsync(b => b.Id == breedId);
            
            // Middle code is the breed's Code, or fallback to pet type slug
            string middleCode = !string.IsNullOrWhiteSpace(breed?.Code)
                ? breed.Code.Trim().ToUpper()
                : (!string.IsNullOrWhiteSpace(breed?.PetType?.Name) ? GetPetTypeSlug(breed.PetType.Name).ToUpper() : "BRD");

            // Extract maximum sequence number from existing SkuCodes (e.g. PET-POD-001, PET-BRD-008, PET-HAM-009)
            var existingSkus = await _context.Pets
                .Where(p => !string.IsNullOrEmpty(p.SkuCode))
                .Select(p => p.SkuCode!)
                .ToListAsync();

            int totalExistingCount = await _context.Pets.CountAsync();

            int maxSeq = 0;
            var regex = new Regex(@"[-_](\d+)$");
            foreach (var sku in existingSkus)
            {
                var match = regex.Match(sku.Trim());
                if (match.Success && int.TryParse(match.Groups[1].Value, out int seq))
                {
                    if (seq > maxSeq) maxSeq = seq;
                }
            }

            int nextSeq = Math.Max(maxSeq, totalExistingCount) + 1;
            return $"PET-{middleCode}-{nextSeq:D3}";
        }

        public static string GetPetTypeSlug(string? petTypeName)
        {
            if (string.IsNullOrWhiteSpace(petTypeName)) return "pet";
            string lower = petTypeName.Trim().ToLower();
            if (lower.Contains("chó") || lower.Contains("cho")) return "cho";
            if (lower.Contains("mèo") || lower.Contains("meo")) return "meo";
            if (lower.Contains("cá") || lower.Contains("ca") || lower.Contains("thủy sinh")) return "ca";
            if (lower.Contains("chim")) return "chim";
            if (lower.Contains("hamster") || lower.Contains("chuột")) return "hamster";
            if (lower.Contains("thỏ") || lower.Contains("tho")) return "tho";

            string clean = Regex.Replace(RemoveDiacritics(lower), @"[^a-z0-9]+", "");
            return string.IsNullOrEmpty(clean) ? "pet" : clean;
        }

        public static string RemoveDiacritics(string text)
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
    }
}
