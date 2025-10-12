namespace WinFormsApp.Entities;

public sealed class Booking
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string ScreeningId { get; set; } = "";
    public string ReferenceCode { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string CustomerEmail { get; set; } = "";
    public string CustomerPhone { get; set; } = "";
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    public List<BookingLine> Lines { get; set; } = new();
}
