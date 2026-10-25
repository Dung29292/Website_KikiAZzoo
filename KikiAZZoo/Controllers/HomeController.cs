using KikiAZZoo.Models;
using KikiAZZoo.Repositories;
using KikiAZZoo.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KikiAZZoo.Controllers
{
    public class HomeController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly IPetRepository _petRepository;
        private readonly ICatalogService _catalogService;
        private readonly IContentService _contentService;

        public HomeController(
            IProductRepository productRepository,
            IPetRepository petRepository,
            ICatalogService catalogService,
            IContentService contentService)
        {
            _productRepository = productRepository;
            _petRepository = petRepository;
            _catalogService = catalogService;
            _contentService = contentService;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.Services = (await _contentService.GetAllServicesAsync()).Take(4);
            ViewBag.PetTypes = await _catalogService.GetPetTypesAsync();
            ViewBag.CategoryGroups = await _catalogService.GetCategoryGroupsAsync();
            ViewBag.FeaturedPets = (await _petRepository.GetFeaturedAsync(4)).ToList();
            ViewBag.ActiveCoupons = await _catalogService.GetActiveCouponsAsync();
            var products = (await _productRepository.GetAllAsync()).Take(8);
            return View(products);
        }

        public IActionResult About() => View();
        public IActionResult Contact() => View();
        public IActionResult Privacy() => View();
        public IActionResult FAQ() => View();
        public IActionResult ReturnPolicy() => View();
        public IActionResult ShippingPolicy() => View();
        public IActionResult ShoppingGuide() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => View(new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
    }
}
