using KikiAZZoo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KikiAZZoo.Controllers
{
    [Authorize]
    public class ServiceBookingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ServiceBookingController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var bookings = await _context.PetServiceBookings
                .Include(b => b.Service)
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.BookingDate)
                .ThenByDescending(b => b.CreatedDate)
                .ToListAsync();

            return View(bookings);
        }

        [HttpGet]
        public async Task<IActionResult> Book(int serviceId)
        {
            var service = await _context.Services.FindAsync(serviceId);
            if (service == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            var userPets = await _context.PetProfiles
                .Where(p => p.UserId == userId)
                .ToListAsync();

            ViewBag.Service = service;
            ViewBag.UserPets = userPets;
            ViewBag.TimeSlots = new[] { "08:00 - 09:00", "09:00 - 10:00", "10:00 - 11:00", "11:00 - 12:00", "14:00 - 15:00", "15:00 - 16:00", "16:00 - 17:00" };

            var model = new PetServiceBooking
            {
                ServiceId = serviceId,
                UserId = userId!,
                BookingDate = DateTime.Today.AddDays(1),
                Price = service.PriceFrom ?? 0
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(PetServiceBooking booking)
        {
            var userId = _userManager.GetUserId(User);
            booking.UserId = userId!;

            // Remove relations safety validation
            ModelState.Remove("ApplicationUser");
            ModelState.Remove("Service");

            var service = await _context.Services.FindAsync(booking.ServiceId);
            if (service == null) return NotFound();

            if (!ModelState.IsValid)
            {
                var userPets = await _context.PetProfiles
                    .Where(p => p.UserId == userId)
                    .ToListAsync();
                ViewBag.Service = service;
                ViewBag.UserPets = userPets;
                ViewBag.TimeSlots = new[] { "08:00 - 09:00", "09:00 - 10:00", "10:00 - 11:00", "11:00 - 12:00", "14:00 - 15:00", "15:00 - 16:00", "16:00 - 17:00" };
                return View(booking);
            }

            booking.Price = service.PriceFrom ?? 0;
            booking.Status = "Pending";
            booking.CreatedDate = DateTime.Now;

            _context.PetServiceBookings.Add(booking);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đặt lịch chăm sóc '{service.Name}' cho bé '{booking.PetName}' thành công!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = _userManager.GetUserId(User);
            var booking = await _context.PetServiceBookings
                .FirstOrDefaultAsync(b => b.Id == id && b.UserId == userId);

            if (booking == null) return NotFound();

            if (booking.Status == "Pending" || booking.Status == "Confirmed")
            {
                booking.Status = "Cancelled";
                _context.PetServiceBookings.Update(booking);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã huỷ lịch hẹn chăm sóc thành công.";
            }
            else
            {
                TempData["Error"] = "Không thể huỷ lịch hẹn đã hoàn thành hoặc đã bị huỷ trước đó.";
            }

            return RedirectToAction(nameof(Index));
        }
        [HttpGet]
        public async Task<IActionResult> GetBookedSlots(DateTime date)
        {
            var bookedSlots = await _context.PetServiceBookings
                .Where(b => b.BookingDate.Date == date.Date && b.Status != "Cancelled")
                .Select(b => b.TimeSlot)
                .ToListAsync();
            return Json(bookedSlots);
        }
    }
}
