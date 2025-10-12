using MovieTickets.Core.Entities;
using MovieTickets.Core.Enums;

namespace MovieTickets.Core.Models;

public sealed class ScreeningSummary
{
    public string ScreeningId { get; init; } = "";
    public DateTime StartUtc { get; init; }
    public string CinemaId { get; init; } = "";
    public string CinemaName { get; init; } = "";
    public string AuditoriumName { get; init; } = "";
    public decimal BasePrice { get; init; }
    public ScreeningClass Class { get; init; }

    public Movie? Movie { get; init; }
}
