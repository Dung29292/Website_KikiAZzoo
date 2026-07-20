using KikiAZZoo.Services;
using Microsoft.AspNetCore.Mvc;

namespace KikiAZZoo.Controllers
{
    public class ServiceController : Controller
    {
        private readonly IContentService _contentService;

        public ServiceController(IContentService contentService)
        {
            _contentService = contentService;
        }

        public async Task<IActionResult> Index()
        {
            var services = await _contentService.GetAllServicesAsync();
            return View(services);
        }

        public async Task<IActionResult> Details(int id)
        {
            var service = await _contentService.GetServiceByIdAsync(id);
            if (service == null) return NotFound();
            return View(service);
        }
    }
}
