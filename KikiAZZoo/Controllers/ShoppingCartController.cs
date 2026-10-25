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

        private string GetCartSessionKey()
        {
            var userId = _userManager.GetUserId(User);
            return !string.IsNullOrEmpty(userId) ? $"Cart_{userId}" : "Cart_Guest";
        }

        private async Task<ShoppingCart> GetCartAsync()
        {
            var cart = new ShoppingCart();
            var userId = _userManager.GetUserId(User);

            if (!string.IsNullOrEmpty(userId))
            {
                // 1. Authenticated user: Load from database UserCartItems table
                var dbItems = await _context.UserCartItems
                    .Where(c => c.UserId == userId)
                    .OrderBy(c => c.CreatedDate)
                    .ToListAsync();

                if (dbItems.Any())
                {
                    foreach (var d in dbItems)
                    {
                        if (d.ItemType == "pet" || d.PetId.HasValue)
                        {
                            int petId = d.PetId ?? d.ProductId;
                            var pet = await _context.Pets.FindAsync(petId);
                            if (pet == null)
                            {
                                cart.AddItem(new CartItem
                                {
                                    ProductId = d.ProductId,
                                    PetId = d.PetId,
                                    ItemType = "pet",
                                    Name = "Bé thú cưng (Ngừng kinh doanh)",
                                    ImageUrl = null,
                                    Price = 0,
                                    Quantity = d.Quantity,
                                    MaxStock = 0,
                                    IsOutOfStock = true,
                                    OutOfStockReason = "Thú cưng này không còn khả dụng hoặc đã bị gỡ."
                                });
                                continue;
                            }

                            bool isSoldOrEmpty = !pet.IsAvailable || pet.Quantity <= 0;
                            if (isSoldOrEmpty)
                            {
                                cart.AddItem(new CartItem
                                {
                                    ProductId = pet.Id,
                                    PetId = pet.Id,
                                    ItemType = "pet",
                                    Name = pet.Name,
                                    ImageUrl = pet.ImageUrl,
                                    Price = pet.Price,
                                    Quantity = d.Quantity,
                                    MaxStock = 0,
                                    IsOutOfStock = true,
                                    OutOfStockReason = "Bé đã về nhà mới (Đã có người mua trước)"
                                });
                                continue;
                            }

                            int effectiveQty = Math.Min(d.Quantity, pet.Quantity);
                            cart.AddItem(new CartItem
                            {
                                ProductId = pet.Id,
                                PetId = pet.Id,
                                ItemType = "pet",
                                Name = pet.Name,
                                ImageUrl = pet.ImageUrl,
                                Price = pet.Price,
                                Quantity = effectiveQty,
                                MaxStock = pet.Quantity,
                                IsOutOfStock = false
                            });
                        }
                        else
                        {
                            var product = await _context.Products.FindAsync(d.ProductId);
                            if (product == null)
                            {
                                cart.AddItem(new CartItem
                                {
                                    ProductId = d.ProductId,
                                    ItemType = "product",
                                    Name = "Sản phẩm (Ngừng kinh doanh)",
                                    ImageUrl = null,
                                    Price = 0,
                                    Quantity = d.Quantity,
                                    MaxStock = 0,
                                    IsOutOfStock = true,
                                    OutOfStockReason = "Sản phẩm này không còn tồn tại hoặc đã ngừng kinh doanh."
                                });
                                continue;
                            }

                            bool isProductOutOfStock = !product.IsActive || product.Stock <= 0;
                            if (isProductOutOfStock)
                            {
                                cart.AddItem(new CartItem
                                {
                                    ProductId = product.Id,
                                    ItemType = "product",
                                    Name = product.Name,
                                    ImageUrl = product.ImageUrl,
                                    Price = product.Price,
                                    Quantity = d.Quantity,
                                    MaxStock = 0,
                                    IsOutOfStock = true,
                                    OutOfStockReason = "Hết hàng (Đã có người mua trước)"
                                });
                                continue;
                            }

                            int effectiveQty = Math.Min(d.Quantity, product.Stock);
                            cart.AddItem(new CartItem
                            {
                                ProductId = product.Id,
                                ItemType = "product",
                                Name = product.Name,
                                ImageUrl = product.ImageUrl,
                                Price = product.Price,
                                Quantity = effectiveQty,
                                MaxStock = product.Stock,
                                IsOutOfStock = false
                            });
                        }
                    }

                    HttpContext.Session.SetObjectAsJson(GetCartSessionKey(), cart);
                    return cart;
                }

                // If DB had no items for this user, check if there was a cart in Session to persist
                var sessionCart = HttpContext.Session.GetObjectFromJson<ShoppingCart>(GetCartSessionKey());
                if (sessionCart != null && sessionCart.Items.Any())
                {
                    await SaveCartAsync(sessionCart);
                    return sessionCart;
                }

                return cart;
            }

            // Guest fallback (Session)
            var guestCart = HttpContext.Session.GetObjectFromJson<ShoppingCart>(GetCartSessionKey()) ?? new ShoppingCart();
            if (guestCart.Items.Any())
            {
                foreach (var item in guestCart.Items)
                {
                    if (item.ItemType == "pet" || item.PetId.HasValue)
                    {
                        var pet = await _context.Pets.FindAsync(item.PetId ?? item.ProductId);
                        if (pet == null || !pet.IsAvailable || pet.Quantity <= 0)
                        {
                            item.IsOutOfStock = true;
                            item.MaxStock = 0;
                            item.OutOfStockReason = "Bé đã về nhà mới (Đã có người mua trước)";
                        }
                        else
                        {
                            item.IsOutOfStock = false;
                            item.MaxStock = pet.Quantity;
                            item.Price = pet.Price;
                            item.Quantity = Math.Min(item.Quantity, pet.Quantity);
                        }
                    }
                    else
                    {
                        var prod = await _context.Products.FindAsync(item.ProductId);
                        if (prod == null || !prod.IsActive || prod.Stock <= 0)
                        {
                            item.IsOutOfStock = true;
                            item.MaxStock = 0;
                            item.OutOfStockReason = "Hết hàng (Đã có người mua trước)";
                        }
                        else
                        {
                            item.IsOutOfStock = false;
                            item.MaxStock = prod.Stock;
                            item.Price = prod.Price;
                            item.Quantity = Math.Min(item.Quantity, prod.Stock);
                        }
                    }
                }
            }
            return guestCart;
        }

        private async Task SaveCartAsync(ShoppingCart cart)
        {
            var sessionKey = GetCartSessionKey();
            HttpContext.Session.SetObjectAsJson(sessionKey, cart);

            var userId = _userManager.GetUserId(User);
            if (!string.IsNullOrEmpty(userId))
            {
                var existing = await _context.UserCartItems.Where(c => c.UserId == userId).ToListAsync();
                _context.UserCartItems.RemoveRange(existing);

                foreach (var item in cart.Items)
                {
                    _context.UserCartItems.Add(new UserCartItem
                    {
                        UserId = userId,
                        ProductId = item.ProductId,
                        PetId = item.PetId,
                        ItemType = item.ItemType,
                        Quantity = item.Quantity,
                        CreatedDate = DateTime.Now
                    });
                }
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, requireLogin = true, message = "Vui lòng đăng nhập để thêm sản phẩm vào giỏ hàng." });
                }
                TempData["Info"] = "Vui lòng đăng nhập để thêm sản phẩm vào giỏ hàng.";
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Details", "Product", new { id = productId }) });
            }

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

            var cart = await GetCartAsync();
            var existing = cart.Items.FirstOrDefault(i => i.ItemType != "pet" && i.ProductId == productId);
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
                ItemType = "product",
                Name = product.Name,
                ImageUrl = product.ImageUrl,
                Price = product.Price,
                Quantity = quantity,
                MaxStock = product.Stock
            });
            await SaveCartAsync(cart);

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
            if (User.Identity?.IsAuthenticated != true)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, requireLogin = true, message = "Vui lòng đăng nhập để thêm thú cưng vào giỏ hàng." });
                }
                TempData["Info"] = "Vui lòng đăng nhập để thêm thú cưng vào giỏ hàng.";
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Details", "PetShop", new { id = petId }) });
            }

            var pet = await _context.Pets.FindAsync(petId);
            if (pet == null || !pet.IsAvailable || pet.Quantity <= 0)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = "Thú cưng này hiện không còn sẵn sàng hoặc đã được đón về nhà mới!" });
                TempData["Error"] = "Thú cưng này hiện không còn sẵn sàng hoặc đã được đón về nhà mới!";
                return RedirectToAction("Index", "PetShop");
            }

            var cart = await GetCartAsync();
            var existing = cart.Items.FirstOrDefault(i => i.ItemType == "pet" && (i.PetId == petId || i.ProductId == petId));
            if (existing != null)
            {
                if (existing.Quantity >= pet.Quantity)
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        return Json(new { success = false, message = $"Bé '{pet.Name}' đã có trong giỏ hàng và đạt tối đa số lượng hiện có ({pet.Quantity} bé)!" });
                    TempData["Info"] = $"Bé '{pet.Name}' đã có trong giỏ hàng (tối đa {pet.Quantity} bé)!";
                    return RedirectToAction(nameof(Index));
                }
            }

            cart.AddItem(new CartItem
            {
                ProductId = pet.Id,
                PetId = pet.Id,
                ItemType = "pet",
                Name = pet.Name,
                ImageUrl = pet.ImageUrl,
                Price = pet.Price,
                Quantity = 1,
                MaxStock = pet.Quantity
            });
            await SaveCartAsync(cart);

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

        public async Task<IActionResult> Index()
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                TempData["Info"] = "Vui lòng đăng nhập để xem và quản lý giỏ hàng của bạn.";
                return RedirectToAction("Login", "Account", new { returnUrl = "/ShoppingCart" });
            }
            var cart = await GetCartAsync();
            var activeCoupons = await _context.Coupons
                .Where(c => c.IsActive && c.StartDate <= DateTime.Now && c.EndDate >= DateTime.Now)
                .OrderBy(c => c.MinOrderValue)
                .ToListAsync();
            ViewBag.ActiveCoupons = activeCoupons;
            return View(cart);
        }

        public async Task<IActionResult> GetMiniCart()
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return PartialView("_MiniCart", new ShoppingCart());
            }
            var cart = await GetCartAsync();
            return PartialView("_MiniCart", cart);
        }

        public async Task<IActionResult> UpdateQuantity(int productId, int quantity, string itemType = "product", int? petId = null)
        {
            int targetId = (itemType == "pet" ? (petId ?? productId) : productId);
            var cart = await GetCartAsync();

            var existingInCart = cart.Items.FirstOrDefault(i => 
                (itemType == "pet" && (i.PetId == targetId || i.ProductId == targetId)) ||
                (itemType != "pet" && i.ProductId == targetId)
            );
            if (existingInCart != null && existingInCart.IsOutOfStock)
            {
                TempData["Error"] = $"Sản phẩm/thú cưng '{existingInCart.Name}' đã hết hàng do có người mua trước. Bạn không thể tăng số lượng. Vui lòng bấm xóa khỏi giỏ.";
                return RedirectToAction("Index");
            }

            if (itemType == "pet")
            {
                var pet = await _context.Pets.FindAsync(targetId);
                if (pet == null || !pet.IsAvailable || pet.Quantity <= 0)
                {
                    TempData["Error"] = $"Bé thú cưng '{pet?.Name ?? "đã chọn"}' hiện đã hết hàng do có người mua trước. Vui lòng xóa khỏi giỏ hàng.";
                    return RedirectToAction("Index");
                }

                if (quantity > pet.Quantity)
                {
                    quantity = pet.Quantity;
                    TempData["Error"] = $"Bé thú cưng '{pet.Name}' chỉ có tối đa {pet.Quantity} bé trong kho. Bạn không thể tăng số lượng quá số lượng cửa hàng sở hữu.";
                }
                else if (quantity <= 0)
                {
                    cart.RemoveItem(targetId, "pet");
                    await SaveCartAsync(cart);
                    return RedirectToAction("Index");
                }

                cart.UpdateQuantity(targetId, quantity, "pet");
                await SaveCartAsync(cart);
                return RedirectToAction("Index");
            }
            else
            {
                var product = await _productRepository.GetByIdAsync(productId);
                if (product == null || !product.IsActive || product.Stock <= 0)
                {
                    TempData["Error"] = $"Sản phẩm '{product?.Name ?? "đã chọn"}' hiện đã hết hàng do có người mua trước. Vui lòng xóa khỏi giỏ hàng.";
                    return RedirectToAction("Index");
                }

                if (quantity > product.Stock)
                {
                    quantity = product.Stock;
                    TempData["Error"] = $"Số lượng sản phẩm '{product.Name}' đã được điều chỉnh về mức tối đa trong kho ({product.Stock} sản phẩm).";
                }
                else if (quantity <= 0)
                {
                    cart.RemoveItem(productId, "product");
                    await SaveCartAsync(cart);
                    return RedirectToAction("Index");
                }

                cart.UpdateQuantity(productId, quantity, "product");
                await SaveCartAsync(cart);
                return RedirectToAction("Index");
            }
        }

        public async Task<IActionResult> RemoveFromCart(int productId, string itemType = "product", int? petId = null)
        {
            int targetId = (itemType == "pet" ? (petId ?? productId) : productId);
            var cart = await GetCartAsync();
            cart.RemoveItem(targetId, itemType);
            await SaveCartAsync(cart);
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> RemoveOutOfStockItems()
        {
            var cart = await GetCartAsync();
            var outItems = cart.Items.Where(i => i.IsOutOfStock).ToList();
            foreach (var o in outItems)
            {
                cart.RemoveItem(o.PetId ?? o.ProductId, o.ItemType);
            }
            await SaveCartAsync(cart);
            TempData["Success"] = "Đã dọn dẹp các sản phẩm/thú cưng đã hết hàng khỏi giỏ hàng!";
            return RedirectToAction(nameof(Index));
        }

        [Authorize]
        public async Task<IActionResult> Reorder(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails!)
                .ThenInclude(d => d.Product)
                .Include(o => o.OrderDetails!)
                .ThenInclude(d => d.Pet)
                .FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null) return NotFound();

            var cart = await GetCartAsync();
            foreach (var detail in order.OrderDetails ?? new List<OrderDetail>())
            {
                bool isPet = detail.PetId.HasValue;
                int maxStock = isPet ? (detail.Pet?.Quantity ?? 1) : (detail.Product?.Stock ?? 999);
                cart.AddItem(new CartItem
                {
                    ProductId = detail.ProductId ?? (detail.PetId ?? 0),
                    PetId = detail.PetId,
                    ItemType = isPet ? "pet" : "product",
                    Name = detail.Product?.Name ?? detail.Pet?.Name ?? "Sản phẩm",
                    ImageUrl = detail.Product?.ImageUrl ?? detail.Pet?.ImageUrl,
                    Price = detail.Price,
                    Quantity = detail.Quantity,
                    MaxStock = maxStock
                });
            }
            await SaveCartAsync(cart);
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
            var cart = await GetCartAsync();
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
                        if (coupon.Code.ToUpper() == "FREESHIP")
                        {
                            discountAmount = shippingFee;
                            message = "Áp dụng mã Miễn phí vận chuyển thành công!";
                        }
                        else if (coupon.IsPercentage)
                        {
                            discountAmount = Math.Round(subTotal * (coupon.DiscountValue / 100m));
                            message = $"Áp dụng mã {coupon.Code} (-{coupon.DiscountValue}%) thành công!";
                        }
                        else
                        {
                            discountAmount = coupon.DiscountValue;
                            message = $"Áp dụng mã {coupon.Code} (-{coupon.DiscountValue:N0}đ) thành công!";
                        }
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

        [Authorize]
        public async Task<IActionResult> Checkout()
        {
            var cart = await GetCartAsync();
            if (!cart.Items.Any())
                return RedirectToAction(nameof(Index));

            if (cart.HasOutOfStockItems)
            {
                TempData["Error"] = "Trong giỏ hàng có sản phẩm/thú cưng đã hết hàng do người khác mua trước. Vui lòng bấm 'Xóa tất cả đồ hết hàng' để tiếp tục.";
                return RedirectToAction(nameof(Index));
            }

            // Check stock of all items
            foreach (var item in cart.Items)
            {
                if (item.ItemType == "pet" || item.PetId.HasValue)
                {
                    int targetPetId = item.PetId ?? item.ProductId;
                    var pet = await _context.Pets.FindAsync(targetPetId);
                    if (pet == null || !pet.IsAvailable || pet.Quantity <= 0)
                    {
                        item.IsOutOfStock = true;
                        item.MaxStock = 0;
                        item.OutOfStockReason = "Bé đã về nhà mới (Đã có người mua trước)";
                        await SaveCartAsync(cart);
                        TempData["Error"] = $"Bé thú cưng '{item.Name}' đã có người mua trước. Vui lòng xóa khỏi giỏ để tiếp tục.";
                        return RedirectToAction(nameof(Index));
                    }
                    if (item.Quantity > pet.Quantity)
                    {
                        item.Quantity = pet.Quantity;
                        await SaveCartAsync(cart);
                        TempData["Error"] = $"Bé thú cưng '{item.Name}' chỉ có tối đa {pet.Quantity} bé trong kho. Giỏ hàng đã được điều chỉnh về số lượng tối đa.";
                        return RedirectToAction(nameof(Index));
                    }
                }
                else
                {
                    var prod = await _context.Products.FindAsync(item.ProductId);
                    if (prod == null || !prod.IsActive || prod.Stock <= 0)
                    {
                        item.IsOutOfStock = true;
                        item.MaxStock = 0;
                        item.OutOfStockReason = "Hết hàng (Đã có người mua trước)";
                        await SaveCartAsync(cart);
                        TempData["Error"] = $"Sản phẩm '{item.Name}' hiện đã hết hàng do có người mua trước. Vui lòng xóa khỏi giỏ.";
                        return RedirectToAction(nameof(Index));
                    }
                    if (item.Quantity > prod.Stock)
                    {
                        item.Quantity = prod.Stock;
                        await SaveCartAsync(cart);
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

            // Active coupons to present in checkout
            var activeCoupons = await _context.Coupons
                .Where(c => c.IsActive && c.StartDate <= DateTime.Now && c.EndDate >= DateTime.Now)
                .OrderBy(c => c.MinOrderValue)
                .ToListAsync();
            ViewBag.ActiveCoupons = activeCoupons;

            // Auto-apply coupon only if there are active coupons and cart is eligible
            var freeCoupon = activeCoupons.FirstOrDefault(c => cart.SubTotal >= c.MinOrderValue);

            if (freeCoupon != null)
            {
                model.CouponCode = freeCoupon.Code;
                if (freeCoupon.Code.ToUpper() == "FREESHIP")
                {
                    model.DiscountAmount = model.ShippingFee;
                }
                else if (freeCoupon.IsPercentage)
                {
                    model.DiscountAmount = Math.Round(cart.SubTotal * (freeCoupon.DiscountValue / 100m));
                }
                else
                {
                    model.DiscountAmount = freeCoupon.DiscountValue;
                }
            }

            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(CheckoutViewModel model)
        {
            var cart = await GetCartAsync();
            if (!cart.Items.Any())
            {
                ModelState.AddModelError("", "Giỏ hàng trống.");
                model.Cart = cart;
                return View(model);
            }

            if (cart.HasOutOfStockItems)
            {
                TempData["Error"] = "Trong giỏ hàng có sản phẩm/thú cưng đã hết hàng do người khác mua trước. Vui lòng xóa khỏi giỏ trước khi thanh toán.";
                return RedirectToAction(nameof(Index));
            }

            // Check stock of all items
            foreach (var item in cart.Items)
            {
                if (item.ItemType == "pet" || item.PetId.HasValue)
                {
                    int targetPetId = item.PetId ?? item.ProductId;
                    var pet = await _context.Pets.FindAsync(targetPetId);
                    if (pet == null || !pet.IsAvailable || pet.Quantity <= 0)
                    {
                        item.IsOutOfStock = true;
                        item.MaxStock = 0;
                        item.OutOfStockReason = "Bé đã về nhà mới (Đã có người mua trước)";
                        await SaveCartAsync(cart);
                        TempData["Error"] = $"Bé thú cưng '{item.Name}' đã có người mua trước. Vui lòng xóa khỏi giỏ để tiếp tục.";
                        return RedirectToAction(nameof(Index));
                    }
                    if (item.Quantity > pet.Quantity)
                    {
                        item.Quantity = pet.Quantity;
                        await SaveCartAsync(cart);
                        TempData["Error"] = $"Bé thú cưng '{item.Name}' chỉ có tối đa {pet.Quantity} bé trong kho. Giỏ hàng đã được điều chỉnh về số lượng tối đa.";
                        return RedirectToAction(nameof(Index));
                    }
                }
                else
                {
                    var prod = await _context.Products.FindAsync(item.ProductId);
                    if (prod == null || !prod.IsActive || prod.Stock <= 0)
                    {
                        item.IsOutOfStock = true;
                        item.MaxStock = 0;
                        item.OutOfStockReason = "Hết hàng (Đã có người mua trước)";
                        await SaveCartAsync(cart);
                        TempData["Error"] = $"Sản phẩm '{item.Name}' hiện đã hết hàng do có người mua trước. Vui lòng xóa khỏi giỏ.";
                        return RedirectToAction(nameof(Index));
                    }
                    if (item.Quantity > prod.Stock)
                    {
                        item.Quantity = prod.Stock;
                        await SaveCartAsync(cart);
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
                ViewBag.ActiveCoupons = await _context.Coupons
                    .Where(c => c.IsActive && c.StartDate <= DateTime.Now && c.EndDate >= DateTime.Now)
                    .OrderBy(c => c.MinOrderValue)
                    .ToListAsync();
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

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
                    if (coupon.Code.ToUpper() == "FREESHIP")
                    {
                        discountAmount = shippingFee;
                    }
                    else if (coupon.IsPercentage)
                    {
                        discountAmount = Math.Round(subTotal * (coupon.DiscountValue / 100m));
                    }
                    else
                    {
                        discountAmount = coupon.DiscountValue;
                    }
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
                if (item.ItemType == "pet" || item.PetId.HasValue)
                {
                    int targetPetId = item.PetId ?? item.ProductId;
                    var pet = await _context.Pets.FindAsync(targetPetId);
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
                    ProductId = (i.ItemType == "pet" || i.PetId.HasValue) ? null : (int?)i.ProductId,
                    PetId = (i.ItemType == "pet" || i.PetId.HasValue) ? (i.PetId ?? i.ProductId) : null,
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

            // Clear database cart items for this user
            if (User.Identity?.IsAuthenticated == true)
            {
                var userCartRows = await _context.UserCartItems.Where(c => c.UserId == user.Id).ToListAsync();
                _context.UserCartItems.RemoveRange(userCartRows);
            }

            _context.Orders.Add(newOrder);
            await _context.SaveChangesAsync();

            HttpContext.Session.Remove(GetCartSessionKey());

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
