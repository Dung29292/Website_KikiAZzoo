using KikiAZZoo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KikiAZZoo.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class CouponController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CouponController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var coupons = await _context.Coupons
                .OrderByDescending(c => c.Id)
                .ToListAsync();
            return View(coupons);
        }

        [HttpGet]
        public IActionResult Upsert(int? id)
        {
            if (id == null || id == 0)
            {
                return View(new Coupon
                {
                    StartDate = DateTime.Today,
                    EndDate = DateTime.Today.AddMonths(1),
                    IsActive = true,
                    IsPercentage = true,
                    MaxUsage = 100
                });
            }
            else
            {
                var coupon = _context.Coupons.Find(id);
                if (coupon == null) return NotFound();
                return View(coupon);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upsert(Coupon coupon)
        {
            if (!ModelState.IsValid)
                return View(coupon);

            coupon.Code = coupon.Code.ToUpper().Trim();

            if (coupon.Id == 0)
            {
                _context.Coupons.Add(coupon);
                TempData["Success"] = $"Đã tạo mã giảm giá '{coupon.Code}' thành công!";
            }
            else
            {
                _context.Coupons.Update(coupon);
                TempData["Success"] = $"Đã cập nhật mã giảm giá '{coupon.Code}' thành công!";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon == null) return NotFound();

            _context.Coupons.Remove(coupon);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã xoá mã giảm giá thành công!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon == null) return NotFound();

            coupon.IsActive = !coupon.IsActive;
            _context.Coupons.Update(coupon);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Mã '{coupon.Code}' đã {(coupon.IsActive ? "được kích hoạt" : "bị vô hiệu hóa")}.";
            return RedirectToAction(nameof(Index));
        }
    }
}
