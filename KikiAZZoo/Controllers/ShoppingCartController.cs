using KikiAZZoo.Extensions;
using KikiAZZoo.Models;
using KikiAZZoo.Models.ViewModels;
using KikiAZZoo.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KikiAZZoo.Controllers
{
    public class ShoppingCartController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ShoppingCartController(IProductRepository productRepository, ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _productRepository = productRepository;
            _context = context;
            _userManager = userManager;
        }

        private ShoppingCart GetCart() =>
            HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart") ?? new ShoppingCart();

        private void SaveCart(ShoppingCart cart) =>
            HttpContext.Session.SetObjectAsJson("Cart", cart);

        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null) return NotFound();

            if (!product.IsActive || product.Stock <= 0)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Sản phẩm hiện đang tạm hết hàng." });
                }
                TempData["Error"] = "Sản phẩm hiện đang tạm hết hàng.";
                return RedirectToAction("Index");
            }

            var cart = GetCart();
            var existing = cart.Items.FirstOrDefault(i => i.ProductId == productId);
            var currentQty = existing?.Quantity ?? 0;
            if (currentQty + quantity > product.Stock)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = $"Chỉ còn {product.Stock} sản phẩm trong kho. Bạn đã có {currentQty} trong giỏ." });
                }
                TempData["Error"] = $"Chỉ còn {product.Stock} sản phẩm trong kho. Bạn đã có {currentQty} trong giỏ.";
                return RedirectToAction("Index");
            }

            cart.AddItem(new CartItem
            {
                ProductId = product.Id,
                Name = product.Name,
                ImageUrl = product.ImageUrl,
                Price = product.Price,
                Quantity = quantity
            });
            SaveCart(cart);

            // If AJAX request, return JSON
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new
                {
                    success = true,
                    message = $"Đã thêm '{product.Name}' vào giỏ hàng",
                    cartCount = cart.TotalItems
                });
            }

            // Otherwise redirect
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> AddToCartPet(int petId)
        {
            var pet = await _context.Pets.FindAsync(petId);
            if (pet == null || !pet.IsAvailable)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = "Thú cưng này hiện không còn sẵn sàng hoặc đã được bán!" });
                TempData["Error"] = "Thú cưng này hiện không còn sẵn sàng hoặc đã được bán!";
                return RedirectToAction("Index", "PetShop");
            }

            var cart = GetCart();
            var existing = cart.Items.FirstOrDefault(i => i.PetId == petId);
            if (existing != null)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = $"Bé '{pet.Name}' đã có trong giỏ hàng của bạn!" });
                TempData["Info"] = $"Bé '{pet.Name}' đã có trong giỏ hàng!";
                return RedirectToAction(nameof(Index));
            }

            cart.AddItem(new CartItem
            {
                ProductId = pet.Id,
                PetId = pet.Id,
                ItemType = "pet",
                Name = pet.Name,
                ImageUrl = pet.ImageUrl,
                Price = pet.Price,
                Quantity = 1
            });
            SaveCart(cart);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new
                {
                    success = true,
                    message = $"Đã thêm bé '{pet.Name}' vào giỏ hàng thành công!",
                    cartCount = cart.TotalItems
                });
            }

            TempData["Success"] = $"Đã thêm bé '{pet.Name}' vào giỏ hàng!";
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Index()
        {
            var cart = GetCart();
            return View(cart);
        }

        public IActionResult GetMiniCart()
        {
            var cart = GetCart();
            return PartialView("_MiniCart", cart);
        }

        public async Task<IActionResult> UpdateQuantity(int productId, int quantity)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null) return NotFound();

            if (!product.IsActive || product.Stock <= 0)
            {
                var cartTemp = GetCart();
                cartTemp.RemoveItem(productId);
                SaveCart(cartTemp);
                TempData["Error"] = $"Sản phẩm '{product.Name}' đã hết hàng và được xóa khỏi giỏ.";
                return RedirectToAction("Index");
            }

            if (quantity > product.Stock)
            {
                quantity = product.Stock;
                TempData["Error"] = $"Số lượng sản phẩm '{product.Name}' đã được điều chỉnh về mức tối đa trong kho ({product.Stock} sản phẩm).";
            }

            var cart = GetCart();
            cart.UpdateQuantity(productId, quantity);
            SaveCart(cart);
            return RedirectToAction("Index");
        }

        public IActionResult RemoveFromCart(int productId)
        {
            var cart = GetCart();
            cart.RemoveItem(productId);
            SaveCart(cart);
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Reorder(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails!)
                .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null) return NotFound();

            var cart = GetCart();
            foreach (var detail in order.OrderDetails ?? new List<OrderDetail>())
            {
                cart.AddItem(new CartItem
                {
                    ProductId = detail.ProductId ?? (detail.PetId ?? 0),
                    PetId = detail.PetId,
                    ItemType = detail.PetId.HasValue ? "pet" : "product",
                    Name = detail.Product?.Name ?? detail.Pet?.Name ?? "Sản phẩm",
                    ImageUrl = detail.Product?.ImageUrl ?? detail.Pet?.ImageUrl,
                    Price = detail.Price,
                    Quantity = detail.Quantity
                });
            }
            SaveCart(cart);
            return RedirectToAction(nameof(Index));
        }

        private decimal CalculateShippingFee(string? province, decimal subTotal)
        {
            if (string.IsNullOrEmpty(province))
                return subTotal >= 300000m ? 0 : 30000m;

            bool isMetro = province.Contains("Hà Nội", StringComparison.OrdinalIgnoreCase) || 
                           province.Contains("Hồ Chí Minh", StringComparison.OrdinalIgnoreCase) || 
                           province.Contains("HCM", StringComparison.OrdinalIgnoreCase);

            if (isMetro)
            {
                return subTotal >= 300000m ? 0 : 30000m;
            }
            else
            {
                return subTotal >= 500000m ? 0 : 50000m;
            }
        }

        [HttpPost]
        public async Task<IActionResult> CalculateCheckout(string? couponCode, string? province, bool useLoyaltyPoints)
        {
            var cart = GetCart();
            var user = await _userManager.GetUserAsync(User);
            var availablePoints = user?.LoyaltyPoints ?? 0;

            decimal subTotal = cart.SubTotal;
            decimal shippingFee = CalculateShippingFee(province, subTotal);
            decimal discountAmount = 0;
            decimal loyaltyDiscount = 0;
            string message = "";
            bool couponSuccess = false;

            if (!string.IsNullOrEmpty(couponCode))
            {
                var coupon = await _context.Coupons
                    .FirstOrDefaultAsync(c => c.Code.ToUpper() == couponCode.ToUpper() && c.IsActive && c.StartDate <= DateTime.Now && c.EndDate >= DateTime.Now);

                if (coupon != null)
                {
                    if (coupon.MaxUsage.HasValue && coupon.UsageCount >= coupon.MaxUsage.Value)
                    {
                        message = "Mã giảm giá này đã hết lượt sử dụng.";
                    }
                    else if (subTotal < coupon.MinOrderValue)
                    {
                        message = $"Đơn hàng tối thiểu phải từ {coupon.MinOrderValue:N0}đ để áp dụng mã này.";
                    }
                    else
                    {
                        couponSuccess = true;
                        if (coupon.IsPercentage)
                        {
                            discountAmount = Math.Round(subTotal * (coupon.DiscountValue / 100m));
                        }
                        else
                        {
                            discountAmount = coupon.DiscountValue;
                        }
                        message = "Áp dụng mã giảm giá thành công!";
                    }
                }
                else
                {
                    message = "Mã giảm giá không hợp lệ hoặc đã hết hạn.";
                }
            }

            if (useLoyaltyPoints && availablePoints > 0)
            {
                decimal pointsValue = availablePoints * 1000m;
                decimal maxDiscountAllowed = subTotal - discountAmount;
                if (pointsValue > maxDiscountAllowed)
                {
                    loyaltyDiscount = maxDiscountAllowed;
                }
                else
                {
                    loyaltyDiscount = pointsValue;
                }
            }

            decimal total = Math.Max(0, subTotal + shippingFee - discountAmount - loyaltyDiscount);

            return Json(new
            {
                success = true,
                subTotal = subTotal,
                shippingFee = shippingFee,
                discountAmount = discountAmount,
                loyaltyDiscount = loyaltyDiscount,
                total = total,
                couponSuccess = couponSuccess,
                message = message,
                availablePoints = availablePoints
            });
        }

        public async Task<IActionResult> Checkout()
        {
            var cart = GetCart();
            if (!cart.Items.Any())
                return RedirectToAction(nameof(Index));

            // Check stock of all items
            foreach (var item in cart.Items)
            {
                if (item.PetId.HasValue)
                {
                    var pet = await _context.Pets.FindAsync(item.PetId.Value);
                    if (pet == null || !pet.IsAvailable)
                    {
                        TempData["Error"] = $"Bé thú cưng '{item.Name}' hiện đã có chủ hoặc không còn sẵn sàng.";
                        return RedirectToAction(nameof(Index));
                    }
                }
                else
                {
                    var prod = await _context.Products.FindAsync(item.ProductId);
                    if (prod == null || !prod.IsActive || prod.Stock <= 0)
                    {
                        TempData["Error"] = $"Sản phẩm '{item.Name}' hiện đã hết hàng. Vui lòng cập nhật lại giỏ hàng.";
                        return RedirectToAction(nameof(Index));
                    }
                    if (item.Quantity > prod.Stock)
                    {
                        TempData["Error"] = $"Sản phẩm '{item.Name}' chỉ còn {prod.Stock} sản phẩm trong kho. Vui lòng cập nhật số lượng.";
                        return RedirectToAction(nameof(Index));
                    }
                }
            }

            ApplicationUser? user = null;
            if (User.Identity?.IsAuthenticated == true)
            {
                user = await _userManager.GetUserAsync(User);
            }

            var model = new CheckoutViewModel
            {
                Cart = cart,
                AvailableLoyaltyPoints = user?.LoyaltyPoints ?? 0,
                Order = new Order
                {
                    ShippingAddress = user?.Address ?? ""
                }
            };
            
            // Set initial shipping fee
            model.ShippingFee = CalculateShippingFee("", cart.SubTotal);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CheckoutViewModel model)
        {
            var cart = GetCart();
            if (!cart.Items.Any())
            {
                ModelState.AddModelError("", "Giỏ hàng trống.");
                model.Cart = cart;
                return View(model);
            }

            // Check stock of all items
            foreach (var item in cart.Items)
            {
                if (item.PetId.HasValue)
                {
                    var pet = await _context.Pets.FindAsync(item.PetId.Value);
                    if (pet == null || !pet.IsAvailable)
                    {
                        TempData["Error"] = $"Bé thú cưng '{item.Name}' hiện đã có chủ hoặc không còn sẵn sàng.";
                        return RedirectToAction(nameof(Index));
                    }
                }
                else
                {
                    var prod = await _context.Products.FindAsync(item.ProductId);
                    if (prod == null || !prod.IsActive || prod.Stock <= 0)
                    {
                        TempData["Error"] = $"Sản phẩm '{item.Name}' hiện đã hết hàng. Vui lòng cập nhật lại giỏ hàng.";
                        return RedirectToAction(nameof(Index));
                    }
                    if (item.Quantity > prod.Stock)
                    {
                        TempData["Error"] = $"Sản phẩm '{item.Name}' chỉ còn {prod.Stock} sản phẩm trong kho. Vui lòng cập nhật số lượng.";
                        return RedirectToAction(nameof(Index));
                    }
                }
            }

            // Remove Guest field validation if logged in
            if (User.Identity?.IsAuthenticated == true)
            {
                ModelState.Remove("GuestFullName");
                ModelState.Remove("GuestPhoneNumber");
                ModelState.Remove("GuestEmail");
            }

            // Remove OrderDetails validation if triggered
            ModelState.Remove("Order.OrderDetails");
            ModelState.Remove("Order.ApplicationUser");
            ModelState.Remove("Order.UserId");

            if (!ModelState.IsValid)
            {
                model.Cart = cart;
                ApplicationUser? currUser = null;
                if (User.Identity?.IsAuthenticated == true)
                {
                    currUser = await _userManager.GetUserAsync(User);
                }
                model.AvailableLoyaltyPoints = currUser?.LoyaltyPoints ?? 0;
                return View(model);
            }

            ApplicationUser? user = null;
            if (User.Identity?.IsAuthenticated == true)
            {
                user = await _userManager.GetUserAsync(User);
            }

            if (user == null)
            {
                user = await _userManager.FindByNameAsync("guest@kikiazoo.com");
                if (user == null)
                {
                    user = new ApplicationUser { UserName = "guest@kikiazoo.com", Email = "guest@kikiazoo.com", FullName = "Khách vãng lai" };
                }
            }

            decimal subTotal = cart.SubTotal;
            decimal shippingFee = CalculateShippingFee(model.Order.Province, subTotal);
            decimal discountAmount = 0;
            decimal loyaltyDiscount = 0;

            if (!string.IsNullOrEmpty(model.CouponCode))
            {
                var coupon = await _context.Coupons
                    .FirstOrDefaultAsync(c => c.Code.ToUpper() == model.CouponCode.ToUpper() && c.IsActive && c.StartDate <= DateTime.Now && c.EndDate >= DateTime.Now);
                if (coupon != null && subTotal >= coupon.MinOrderValue && !(coupon.MaxUsage.HasValue && coupon.UsageCount >= coupon.MaxUsage.Value))
                {
                    discountAmount = coupon.IsPercentage ? Math.Round(subTotal * (coupon.DiscountValue / 100m)) : coupon.DiscountValue;
                    coupon.UsageCount++;
                }
            }

            if (model.UseLoyaltyPoints && user.LoyaltyPoints > 0 && User.Identity?.IsAuthenticated == true)
            {
                decimal pointsValue = user.LoyaltyPoints * 1000m;
                decimal maxAllowed = subTotal - discountAmount;
                loyaltyDiscount = pointsValue > maxAllowed ? maxAllowed : pointsValue;
                int pointsUsed = (int)(loyaltyDiscount / 1000m);
                user.LoyaltyPoints -= pointsUsed;
            }

            decimal finalTotal = Math.Max(0, subTotal + shippingFee - discountAmount - loyaltyDiscount);

            string orderNotes = model.Order.Notes ?? "";
            if (User.Identity?.IsAuthenticated != true)
            {
                var guestInfo = $"[Khách mua nhanh: {model.GuestFullName} - ĐT: {model.GuestPhoneNumber} - Email: {model.GuestEmail}]";
                orderNotes = string.IsNullOrEmpty(orderNotes) ? guestInfo : $"{guestInfo} | Ghi chú: {orderNotes}";
            }

            // Deduct stock / mark pet as sold
            foreach (var item in cart.Items)
            {
                if (item.PetId.HasValue)
                {
                    var pet = await _context.Pets.FindAsync(item.PetId.Value);
                    if (pet != null)
                    {
                        pet.Quantity = Math.Max(0, pet.Quantity - item.Quantity);
                        if (pet.Quantity <= 0)
                        {
                            pet.Status = "Sold";
                        }
                    }
                }
                else
                {
                    var prod = await _context.Products.FindAsync(item.ProductId);
                    if (prod != null)
                    {
                        prod.Stock -= item.Quantity;
                    }
                }
            }

            var newOrder = new Order
            {
                UserId = user.Id,
                OrderDate = DateTime.Now,
                TotalPrice = finalTotal,
                ShippingAddress = $"{model.Order.ShippingAddress}, {model.Order.Ward}, {model.Order.District}, {model.Order.Province}",
                Province = model.Order.Province,
                District = model.Order.District,
                Ward = model.Order.Ward,
                Notes = orderNotes,
                Status = "Pending",
                PaymentMethod = model.Order.PaymentMethod ?? "COD",
                PaymentStatus = "Pending",
                ShippingFee = shippingFee,
                CouponCode = model.CouponCode,
                DiscountAmount = discountAmount + loyaltyDiscount,
                CompanyName = model.Order.CompanyName,
                TaxCode = model.Order.TaxCode,
                CompanyAddress = model.Order.CompanyAddress,
                OrderDetails = cart.Items.Select(i => new OrderDetail
                {
                    ProductId = i.PetId.HasValue ? null : (int?)i.ProductId,
                    PetId = i.PetId,
                    Quantity = i.Quantity,
                    Price = i.Price
                }).ToList()
            };

            // Award points for this purchase (1 point per 100k of total) only if logged in
            if (User.Identity?.IsAuthenticated == true)
            {
                int pointsEarned = (int)(finalTotal / 100000m);
                user.LoyaltyPoints += pointsEarned;
                await _userManager.UpdateAsync(user);
            }

            _context.Orders.Add(newOrder);
            await _context.SaveChangesAsync();

            HttpContext.Session.Remove("Cart");

            if (newOrder.PaymentMethod != "COD")
            {
                return RedirectToAction("Payment", new { orderId = newOrder.Id });
            }

            return RedirectToAction("OrderSuccess", new { id = newOrder.Id });
        }

        public async Task<IActionResult> Payment(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails!)
                .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                return NotFound();

            var isGuestOrder = order.Notes != null && order.Notes.Contains("[Khách mua nhanh:");
            if (!isGuestOrder && order.UserId != _userManager.GetUserId(User))
                return Challenge();

            if (order.PaymentStatus == "Paid")
                return RedirectToAction("OrderSuccess", new { id = orderId });

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmPayment(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
                return NotFound();

            var isGuestOrder = order.Notes != null && order.Notes.Contains("[Khách mua nhanh:");
            if (!isGuestOrder && order.UserId != _userManager.GetUserId(User))
                return Challenge();

            order.PaymentStatus = "Paid";
            await _context.SaveChangesAsync();

            return RedirectToAction("OrderSuccess", new { id = orderId });
        }

        public IActionResult OrderSuccess(int id) => View(id);
    }
}
