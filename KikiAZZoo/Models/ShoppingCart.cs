namespace KikiAZZoo.Models
{
    public class ShoppingCart
    {
        public List<CartItem> Items { get; set; } = new();

        public void AddItem(CartItem item)
        {
            var existing = Items.FirstOrDefault(i => i.ProductId == item.ProductId);
            if (existing != null)
                existing.Quantity += item.Quantity;
            else
                Items.Add(item);
        }

        public void RemoveItem(int productId) => Items.RemoveAll(i => i.ProductId == productId);

        public void UpdateQuantity(int productId, int quantity)
        {
            var item = Items.FirstOrDefault(i => i.ProductId == productId);
            if (item != null)
            {
                if (quantity <= 0) RemoveItem(productId);
                else item.Quantity = quantity;
            }
        }

        public decimal SubTotal => Items.Sum(i => i.Price * i.Quantity);
        public int TotalItems => Items.Sum(i => i.Quantity);

        public const decimal FreeShippingThreshold = 300_000m;
        public const decimal StandardShippingFee = 30_000m;

        public static decimal CalculateShipping(decimal subTotal) =>
            subTotal >= FreeShippingThreshold ? 0 : StandardShippingFee;
    }
}
