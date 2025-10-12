using WinFormsApp.Enums;

namespace WinFormsApp.Entities;

public sealed class Screening
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string MovieId { get; set; } = "";
    public string AuditoriumId { get; set; } = "";
    public DateTime StartUtc { get; set; }
    public decimal BasePrice { get; set; } = 18.00m;
    public ScreeningClass Class { get; set; } = ScreeningClass.Standard;

    public DateTime EndUtc => StartUtc.AddHours(2); // fallback if runtime unavailable
}
