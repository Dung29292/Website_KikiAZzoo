namespace KikiAZZoo.Models
{
    public class ShoppingCart
    {
        public List<CartItem> Items { get; set; } = new();

        public void AddItem(CartItem item)
        {
            var existing = Items.FirstOrDefault(i => 
                (i.ItemType == item.ItemType) && 
                ((item.ItemType == "pet" && (i.PetId == item.PetId || i.ProductId == item.ProductId)) || 
                 (item.ItemType != "pet" && i.ProductId == item.ProductId))
            );
            if (existing != null)
            {
                existing.Quantity += item.Quantity;
                if (item.MaxStock > 0 && existing.Quantity > item.MaxStock)
                {
                    existing.Quantity = item.MaxStock;
                }
                existing.MaxStock = item.MaxStock;
            }
            else
            {
                if (item.MaxStock > 0 && item.Quantity > item.MaxStock)
                {
                    item.Quantity = item.MaxStock;
                }
                Items.Add(item);
            }
        }

        public void RemoveItem(int id, string itemType = "product")
        {
            if (itemType == "pet")
                Items.RemoveAll(i => i.ItemType == "pet" && (i.PetId == id || i.ProductId == id));
            else
                Items.RemoveAll(i => (i.ItemType != "pet") && i.ProductId == id);
        }

        public void UpdateQuantity(int id, int quantity, string itemType = "product")
        {
            var item = Items.FirstOrDefault(i => 
                (itemType == "pet" && i.ItemType == "pet" && (i.PetId == id || i.ProductId == id)) ||
                (itemType != "pet" && i.ItemType != "pet" && i.ProductId == id)
            );
            if (item != null)
            {
                if (quantity <= 0) RemoveItem(id, itemType);
                else
                {
                    if (item.MaxStock > 0 && quantity > item.MaxStock)
                    {
                        quantity = item.MaxStock;
                    }
                    item.Quantity = quantity;
                }
            }
        }

        public void RemoveOutOfStockItems()
        {
            Items.RemoveAll(i => i.IsOutOfStock);
        }

        public bool HasOutOfStockItems => Items.Any(i => i.IsOutOfStock);
        public decimal SubTotal => Items.Where(i => !i.IsOutOfStock).Sum(i => i.Price * i.Quantity);
        public int TotalItems => Items.Where(i => !i.IsOutOfStock).Sum(i => i.Quantity);

        public const decimal FreeShippingThreshold = 300_000m;
        public const decimal StandardShippingFee = 30_000m;

        public static decimal CalculateShipping(decimal subTotal) =>
            subTotal >= FreeShippingThreshold ? 0 : StandardShippingFee;
    }
}
