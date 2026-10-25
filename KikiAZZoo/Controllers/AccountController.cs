using KikiAZZoo.Extensions;
using KikiAZZoo.Models;
using KikiAZZoo.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using KikiAZZoo.Services;

namespace KikiAZZoo.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly IInvoiceService _invoiceService;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            IInvoiceService invoiceService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _context = context;
            _invoiceService = invoiceService;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (!ModelState.IsValid)
                return View(model);

            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    var guestCart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart_Guest");
                    if (guestCart != null && guestCart.Items.Any())
                    {
                        var dbItems = await _context.UserCartItems.Where(c => c.UserId == user.Id).ToListAsync();
                        foreach (var gItem in guestCart.Items)
                        {
                            var existing = dbItems.FirstOrDefault(d => 
                                (d.ItemType == gItem.ItemType) &&
                                ((gItem.ItemType == "pet" && (d.PetId == gItem.PetId || d.ProductId == gItem.ProductId)) || 
                                 (gItem.ItemType != "pet" && d.ProductId == gItem.ProductId))
                            );
                            if (existing != null)
                            {
                                existing.Quantity += gItem.Quantity;
                            }
                            else
                            {
                                _context.UserCartItems.Add(new UserCartItem
                                {
                                    UserId = user.Id,
                                    ProductId = gItem.ProductId,
                                    PetId = gItem.PetId,
                                    ItemType = gItem.ItemType,
                                    Quantity = gItem.Quantity,
                                    CreatedDate = DateTime.Now
                                });
                            }
                        }
                        await _context.SaveChangesAsync();
                        HttpContext.Session.Remove("Cart_Guest");
                    }
                }
                return RedirectToLocal(returnUrl);
            }

            ModelState.AddModelError("", "Email hoặc mật khẩu không đúng.");
            return View(model);
        }

        [HttpGet]
        public IActionResult Register() => View(new RegisterViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, SD.Role_Customer);
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(model);
        }

        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            return View(new ProfileViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? "",
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                Age = user.Age,
                LoyaltyPoints = user.LoyaltyPoints
            });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            // Remove validation for properties not editable
            ModelState.Remove("Email");

            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;
            user.Address = model.Address;
            user.Age = model.Age;

            await _userManager.UpdateAsync(user);
            TempData["Success"] = "Cập nhật thông tin thành công!";
            return RedirectToAction(nameof(Profile));
        }

        [Authorize]
        public async Task<IActionResult> Orders(string? filterTime)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var query = _context.Orders
                .Include(o => o.OrderDetails!)
                .ThenInclude(d => d.Product)
                .Where(o => o.UserId == user.Id)
                .AsQueryable();

            if (!string.IsNullOrEmpty(filterTime))
            {
                var now = DateTime.Now;
                if (filterTime == "month")
                {
                    query = query.Where(o => o.OrderDate >= now.AddMonths(-1));
                }
                else if (filterTime == "3months")
                {
                    query = query.Where(o => o.OrderDate >= now.AddMonths(-3));
                }
                else if (filterTime == "year")
                {
                    query = query.Where(o => o.OrderDate >= new DateTime(now.Year, 1, 1));
                }
            }

            var orders = await query.OrderByDescending(o => o.OrderDate).ToListAsync();
            ViewBag.FilterTime = filterTime;

            return View(orders);
        }

        [Authorize]
        public async Task<IActionResult> OrderDetails(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var order = await _context.Orders
                .Include(o => o.OrderDetails!)
                .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == user.Id);

            if (order == null) return NotFound();
            return View(order);
        }

        [Authorize]
        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            // Nếu cần trong tương lai có gửi email, xử lý ở đây.
            if (user != null)
            {
                // Placeholder: the logic for generating reset tokens can be added here.
            }

            return RedirectToAction(nameof(ForgotPasswordConfirmation));
        }

        public IActionResult ForgotPasswordConfirmation() => View();

        public IActionResult AccessDenied() => View();

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ExportInvoice(int id, string? companyName, string? taxCode, string? companyAddress)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var order = await _context.Orders
                .Include(o => o.OrderDetails!)
                .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == user.Id);
            if (order == null) return NotFound();

            if (!string.IsNullOrEmpty(companyName) || !string.IsNullOrEmpty(taxCode) || !string.IsNullOrEmpty(companyAddress))
            {
                order.CompanyName = companyName;
                order.TaxCode = taxCode;
                order.CompanyAddress = companyAddress;
                await _context.SaveChangesAsync();
            }
            else
            {
                companyName = order.CompanyName;
                taxCode = order.TaxCode;
                companyAddress = order.CompanyAddress;
            }

            var html = _invoiceService.GenerateInvoiceHtml(order, user, companyName, taxCode, companyAddress);
            return Content(html, "text/html", System.Text.Encoding.UTF8);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendInvoiceEmail(int id, string recipientEmail, string? companyName, string? taxCode, string? companyAddress)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var order = await _context.Orders
                .Include(o => o.OrderDetails!)
                .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == user.Id);
            if (order == null) return NotFound();

            if (!string.IsNullOrEmpty(companyName) || !string.IsNullOrEmpty(taxCode) || !string.IsNullOrEmpty(companyAddress))
            {
                order.CompanyName = companyName;
                order.TaxCode = taxCode;
                order.CompanyAddress = companyAddress;
                await _context.SaveChangesAsync();
            }
            else
            {
                companyName = order.CompanyName;
                taxCode = order.TaxCode;
                companyAddress = order.CompanyAddress;
            }

            var success = await _invoiceService.SendInvoiceEmailAsync(order, user, recipientEmail, companyName, taxCode, companyAddress);

            TempData["Success"] = success ? "Hóa đơn đã được gửi tới email thành công!" : "Gửi email hóa đơn thất bại.";
            return RedirectToAction(nameof(OrderDetails), new { id });
        }
    }
}
