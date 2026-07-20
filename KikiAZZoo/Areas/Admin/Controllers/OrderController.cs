using KikiAZZoo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using KikiAZZoo.Services;

namespace KikiAZZoo.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IInvoiceService _invoiceService;

        public OrderController(ApplicationDbContext context, IInvoiceService invoiceService)
        {
            _context = context;
            _invoiceService = invoiceService;
        }

        public async Task<IActionResult> Index(string? search, string? status)
        {
            var query = _context.Orders
                .Include(o => o.OrderDetails!)
                .ThenInclude(d => d.Product)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(o => o.Status == status);
            }

            var orders = await query.OrderByDescending(o => o.OrderDate).ToListAsync();

            var userIds = orders.Select(o => o.UserId).Distinct().ToList();
            var users = await _context.Users.Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id);

            if (!string.IsNullOrEmpty(search))
            {
                var searchTerm = search.Trim().ToLower();
                orders = orders.Where(o => 
                    o.Id.ToString().Contains(searchTerm) || 
                    (users.TryGetValue(o.UserId, out var u) && ((u.FullName != null && u.FullName.ToLower().Contains(searchTerm)) || (u.Email != null && u.Email.ToLower().Contains(searchTerm))))
                ).ToList();
            }

            ViewBag.Users = users;
            ViewBag.Search = search;
            ViewBag.Status = status;
            return View(orders);
        }

        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails!)
                .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();
            ViewBag.User = await _context.Users.FindAsync(order.UserId);
            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();
            order.Status = status;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> ExportInvoice(int id, string? companyName, string? taxCode, string? companyAddress)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails!)
                .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(o => o.Id == id);
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

            var user = await _context.Users.FindAsync(order.UserId);
            var html = _invoiceService.GenerateInvoiceHtml(order, user, companyName, taxCode, companyAddress);
            return Content(html, "text/html", System.Text.Encoding.UTF8);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendInvoiceEmail(int id, string recipientEmail, string? companyName, string? taxCode, string? companyAddress)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails!)
                .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(o => o.Id == id);
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

            var user = await _context.Users.FindAsync(order.UserId);
            var success = await _invoiceService.SendInvoiceEmailAsync(order, user, recipientEmail, companyName, taxCode, companyAddress);

            TempData["Success"] = success ? "Hóa đơn đã được gửi tới email thành công!" : "Gửi email hóa đơn thất bại.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
