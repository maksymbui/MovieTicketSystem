namespace MovieTickets.Core.Entities;

public sealed class Deal
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string MovieId { get; set; } = "";
    public int Discount { get; set; } = 0;
    public DateTime ExpiryDate { get; set; } = DateTime.UtcNow.AddMonths(1);

    public override string ToString() => $"{MovieId} ({Discount}%)";
}
