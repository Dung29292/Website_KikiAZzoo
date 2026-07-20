namespace KikiAZZoo.Models.ViewModels
{
    public class CheckoutViewModel
    {
        public Order Order { get; set; } = new();
        public ShoppingCart Cart { get; set; } = new();
        
        public decimal ShippingFee { get; set; }
        public decimal DiscountAmount { get; set; }
        public string? CouponCode { get; set; }
        
        public bool UseLoyaltyPoints { get; set; }
        public int AvailableLoyaltyPoints { get; set; }
        public decimal LoyaltyPointsDiscount { get; set; }
        
        public decimal Total => Math.Max(0, Cart.SubTotal + ShippingFee - DiscountAmount - LoyaltyPointsDiscount);

        public string? GuestFullName { get; set; }
        public string? GuestPhoneNumber { get; set; }
        public string? GuestEmail { get; set; }
    }
}
