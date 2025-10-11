namespace MovieTickets.Core.Entities;

public sealed class Auditorium
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string CinemaId { get; set; } = "";
    public string Name { get; set; } = "";
    public int RowCount { get; set; }
    public int ColumnCount { get; set; }
    public int PremiumRowCutoff { get; set; } = 2; // first rows premium by default
}
