namespace WinFormsApp.Models;

public sealed class OrderQuote
{
    public string ScreeningId { get; init; } = "";
    public IReadOnlyList<OrderQuoteLine> Lines { get; init; } = Array.Empty<OrderQuoteLine>();
    public decimal Subtotal { get; init; }
    public decimal Discount { get; init; }
    public decimal Total => Subtotal - Discount;
}

public sealed class OrderQuoteLine
{
    public string Description { get; init; } = "";
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal LineTotal => UnitPrice * Quantity;
}
