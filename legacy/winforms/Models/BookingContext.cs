using WinFormsApp.Entities;

namespace WinFormsApp.Models;

// UI state hand-off. Keeps track of what the current user picked so the host can persist or reset.
public sealed class BookingContext
{
    public Movie? SelectedMovie { get; private set; }
    public string? SelectedScreeningId { get; private set; }
    readonly List<string> _selectedSeats = new();
    public IReadOnlyList<string> SelectedSeats => _selectedSeats;

    public void SetMovie(Movie movie) => SelectedMovie = movie;

    public void SetScreening(string screeningId)
    {
        SelectedScreeningId = screeningId;
        _selectedSeats.Clear();
    }

    public void SetSelectedSeats(IEnumerable<string> seatLabels)
    {
        _selectedSeats.Clear();
        _selectedSeats.AddRange(seatLabels);
    }
}
