using KikiAZZoo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KikiAZZoo.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context) => _context = context;

        public async Task<IActionResult> Index()
        {
            // Stats
            ViewBag.ProductCount = await _context.Products.CountAsync();
            ViewBag.CategoryCount = await _context.Categories.CountAsync();
            ViewBag.OrderCount = await _context.Orders.CountAsync();
            ViewBag.UserCount = await _context.Users.CountAsync();

            // Alerts
            ViewBag.LowStockCount = await _context.Products.CountAsync(p => p.Stock <= 5);
            ViewBag.PendingOrdersCount = await _context.Orders.CountAsync(o => o.Status == "Pending");

            // Chart data - Last 6 months revenue
            var orders = await _context.Orders.ToListAsync();
            var chartLabels = new List<string>();
            var chartValues = new List<decimal>();
            for (int i = 5; i >= 0; i--)
            {
                var monthDate = DateTime.Now.AddMonths(-i);
                var label = monthDate.ToString("MM/yyyy");
                var total = orders.Where(o => o.OrderDate.Month == monthDate.Month && o.OrderDate.Year == monthDate.Year && o.Status != "Cancelled").Sum(o => o.TotalPrice);
                chartLabels.Add(label);
                chartValues.Add(total);
            }
            ViewBag.ChartLabels = chartLabels;
            ViewBag.ChartValues = chartValues;

            // Recent Orders
            var recentOrders = await _context.Orders
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .ToListAsync();

            var userIds = recentOrders.Select(o => o.UserId).ToList();
            var users = await _context.Users
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id);

            ViewBag.RecentOrders = recentOrders;
            ViewBag.OrderUsers = users;

            return View();
        }
    }
}
