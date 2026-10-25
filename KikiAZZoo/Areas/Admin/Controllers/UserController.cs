using KikiAZZoo.Models;
using KikiAZZoo.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KikiAZZoo.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class UserController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public UserController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        public async Task<IActionResult> Index(string? search, string? role)
        {
            var query = _userManager.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(u =>
                    (u.FullName != null && u.FullName.ToLower().Contains(term)) ||
                    (u.Email != null && u.Email.ToLower().Contains(term)) ||
                    (u.PhoneNumber != null && u.PhoneNumber.Contains(term))
                );
            }

            var users = await query.ToListAsync();
            var ordersCountByUser = await _context.Orders
                .GroupBy(o => o.UserId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.UserId, g => g.Count);

            var userViewModels = new List<UserItemViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var userRole = roles.FirstOrDefault() ?? SD.Role_Customer;

                if (!string.IsNullOrEmpty(role) && userRole != role)
                {
                    continue;
                }

                var isLocked = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow;

                userViewModels.Add(new UserItemViewModel
                {
                    Id = user.Id,
                    FullName = user.FullName ?? "Chưa đặt tên",
                    Email = user.Email ?? string.Empty,
                    PhoneNumber = user.PhoneNumber,
                    Role = userRole,
                    IsLockedOut = isLocked,
                    LoyaltyPoints = user.LoyaltyPoints,
                    OrdersCount = ordersCountByUser.TryGetValue(user.Id, out var count) ? count : 0
                });
            }

            ViewBag.Search = search;
            ViewBag.SelectedRole = role;
            ViewBag.AllRoles = new List<string> { SD.Role_Admin, SD.Role_Employee, SD.Role_Customer, SD.Role_Company };

            return View(userViewModels.OrderByDescending(u => u.Role == SD.Role_Admin)
                                      .ThenByDescending(u => u.Role == SD.Role_Employee)
                                      .ThenBy(u => u.FullName)
                                      .ToList());
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Roles = new List<string> { SD.Role_Employee, SD.Role_Admin, SD.Role_Customer, SD.Role_Company };
            return View(new CreateUserViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Roles = new List<string> { SD.Role_Employee, SD.Role_Admin, SD.Role_Customer, SD.Role_Company };
                return View(model);
            }

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "Email này đã được sử dụng trong hệ thống.");
                ViewBag.Roles = new List<string> { SD.Role_Employee, SD.Role_Admin, SD.Role_Customer, SD.Role_Company };
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                Address = model.Address,
                EmailConfirmed = true,
                LoyaltyPoints = 0
            };

            var createResult = await _userManager.CreateAsync(user, model.Password);
            if (!createResult.Succeeded)
            {
                foreach (var err in createResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, err.Description);
                }
                ViewBag.Roles = new List<string> { SD.Role_Employee, SD.Role_Admin, SD.Role_Customer, SD.Role_Company };
                return View(model);
            }

            if (!await _roleManager.RoleExistsAsync(model.Role))
            {
                await _roleManager.CreateAsync(new IdentityRole(model.Role));
            }

            await _userManager.AddToRoleAsync(user, model.Role);
            TempData["Success"] = $"Đã tạo tài khoản '{model.FullName}' với vai trò '{model.Role}' thành công!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeRole(string userId, string newRole)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var currentAdmin = await _userManager.GetUserAsync(User);
            if (currentAdmin?.Id == user.Id && newRole != SD.Role_Admin)
            {
                TempData["Error"] = "Bạn không thể tự hạ quyền Admin của chính mình!";
                return RedirectToAction(nameof(Index));
            }

            if (!await _roleManager.RoleExistsAsync(newRole))
            {
                await _roleManager.CreateAsync(new IdentityRole(newRole));
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, newRole);

            TempData["Success"] = $"Đã cập nhật vai trò của tài khoản '{user.Email}' thành '{newRole}'!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLock(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var currentAdmin = await _userManager.GetUserAsync(User);
            if (currentAdmin?.Id == user.Id)
            {
                TempData["Error"] = "Bạn không thể tự khóa tài khoản của chính mình!";
                return RedirectToAction(nameof(Index));
            }

            if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow)
            {
                // Unlock
                user.LockoutEnd = null;
                TempData["Success"] = $"Đã mở khóa tài khoản '{user.Email}'.";
            }
            else
            {
                // Lock for 100 years
                user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100);
                TempData["Success"] = $"Đã khóa tài khoản '{user.Email}'.";
            }

            await _userManager.UpdateAsync(user);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string userId, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            {
                TempData["Error"] = "Mật khẩu mới phải có tối thiểu 6 ký tự.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (result.Succeeded)
            {
                TempData["Success"] = $"Đã đặt lại mật khẩu mới cho '{user.Email}' thành công!";
            }
            else
            {
                TempData["Error"] = $"Không thể đặt lại mật khẩu: {string.Join(", ", result.Errors.Select(e => e.Description))}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
