namespace KikiAZZoo.Extensions
{
    public static class OrderStatusExtensions
    {
        private static readonly Dictionary<string, (string Label, string CssClass)> StatusMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Pending"] = ("Chờ xác nhận", "order-status-pending"),
            ["Processing"] = ("Đang xử lý", "order-status-processing"),
            ["Shipping"] = ("Đang giao hàng", "order-status-shipping"),
            ["Delivered"] = ("Đã giao hàng", "order-status-delivered"),
            ["Cancelled"] = ("Đã hủy", "order-status-cancelled"),
        };

        public static string GetStatusLabel(this string? status) =>
            status != null && StatusMap.TryGetValue(status, out var info) ? info.Label : status ?? "";

        public static string GetStatusCssClass(this string? status) =>
            status != null && StatusMap.TryGetValue(status, out var info) ? info.CssClass : "order-status-default";
    }
}
