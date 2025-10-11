using MovieTickets.Core.Enums;

namespace MovieTickets.Core.Models;

// Snapshot of a screening's seating grid. Services hand this to the UI layer.
public sealed class SeatMap
{
    public string ScreeningId { get; init; } = "";
    public int Rows { get; init; }
    public int Columns { get; init; }
    public IReadOnlyDictionary<string, SeatState> Seats { get; init; } = new Dictionary<string, SeatState>();

    public SeatState this[string seatLabel] => Seats.TryGetValue(seatLabel, out var state) ? state : SeatState.Blocked;
}
