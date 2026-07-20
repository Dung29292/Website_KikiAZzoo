using KikiAZZoo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KikiAZZoo.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BookingController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var bookings = await _context.PetServiceBookings
                .Include(b => b.Service)
                .OrderByDescending(b => b.BookingDate)
                .ThenByDescending(b => b.CreatedDate)
                .ToListAsync();

            var userIds = bookings.Select(b => b.UserId).Distinct().ToList();
            var users = await _context.Users
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id);

            var events = bookings.Select(b => {
                users.TryGetValue(b.UserId, out var u);
                var title = $"{b.TimeSlot} - {u?.FullName ?? "N/A"} ({b.Service?.Name})";
                var color = "#eab308"; // Pending
                if (b.Status == "Confirmed") color = "#3b82f6"; // Blue
                else if (b.Status == "Completed") color = "#10b981"; // Green
                else if (b.Status == "Cancelled") color = "#ef4444"; // Red

                return new {
                    id = b.Id,
                    title = title,
                    start = b.BookingDate.ToString("yyyy-MM-dd"),
                    color = color,
                    url = Url.Action("Details", new { id = b.Id })
                };
            }).ToList();

            ViewBag.Users = users;
            ViewBag.EventsJson = System.Text.Json.JsonSerializer.Serialize(events);
            return View(bookings);
        }

        public async Task<IActionResult> Details(int id)
        {
            var booking = await _context.PetServiceBookings
                .Include(b => b.Service)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null) return NotFound();

            ViewBag.User = await _context.Users.FindAsync(booking.UserId);
            return View(booking);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var booking = await _context.PetServiceBookings.FindAsync(id);
            if (booking == null) return NotFound();

            booking.Status = status;
            _context.PetServiceBookings.Update(booking);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Cập nhật trạng thái lịch đặt thành công!";
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
