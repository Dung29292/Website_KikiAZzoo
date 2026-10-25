using KikiAZZoo.Models;

namespace KikiAZZoo.Extensions
{
    public class DiscountInfo
    {
        public bool HasDiscount { get; set; }
        public decimal OriginalPrice { get; set; }
        public decimal FinalPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public Coupon? BestCoupon { get; set; }
        public string DiscountBadgeText { get; set; } = string.Empty;
    }

    public static class CouponHelper
    {
        public static DiscountInfo GetBestDiscount(decimal price, IEnumerable<Coupon>? activeCoupons)
        {
            var info = new DiscountInfo
            {
                HasDiscount = false,
                OriginalPrice = price,
                FinalPrice = price,
                DiscountAmount = 0,
                BestCoupon = null,
                DiscountBadgeText = string.Empty
            };

            if (activeCoupons == null || !activeCoupons.Any() || price <= 0)
            {
                return info;
            }

            var now = DateTime.Now;
            // Filter coupons that are active, within date range, and eligible for this price
            var eligible = activeCoupons
                .Where(c => c.IsActive && c.StartDate <= now && c.EndDate >= now && price >= c.MinOrderValue)
                .ToList();

            if (!eligible.Any())
            {
                return info;
            }

            Coupon? best = null;
            decimal maxDisc = 0;

            foreach (var c in eligible)
            {
                decimal disc = c.IsPercentage
                    ? Math.Round(price * (c.DiscountValue / 100m))
                    : c.DiscountValue;

                if (disc > maxDisc)
                {
                    maxDisc = disc;
                    best = c;
                }
            }

            if (best != null && maxDisc > 0)
            {
                info.HasDiscount = true;
                info.BestCoupon = best;
                info.DiscountAmount = Math.Min(maxDisc, price);
                info.FinalPrice = Math.Max(0, price - info.DiscountAmount);
                info.DiscountBadgeText = best.IsPercentage
                    ? $"-{best.DiscountValue}%"
                    : $"-{best.DiscountValue:#,##0}đ";
            }

            return info;
        }
    }
}
