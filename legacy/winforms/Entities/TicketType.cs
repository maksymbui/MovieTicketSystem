using WinFormsApp.Enums;

namespace WinFormsApp.Entities;

public sealed class TicketType
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public TicketCategory Category { get; set; } = TicketCategory.Adult;
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public bool RequiresMembership { get; set; }

    public override string ToString() => $"{Name} ({Price:C})";
}
