using MovieTickets.Core.Enums;
using MovieTickets.Core.Models;

namespace MovieTickets.Core.Logic;

// Query helper for sessions. Keeps projection logic out of the UI/API layers.
public sealed class ScreeningService
{
    public IReadOnlyList<ScreeningSummary> GetByMovie(string movieId, DateOnly? date = null, string? cinemaId = null)
    {
        var query = DataStore.Screenings
            .Where(s => s.MovieId == movieId);

        if (date != null)
            query = query.Where(s => DateOnly.FromDateTime(s.StartUtc) == date);

        if (!string.IsNullOrWhiteSpace(cinemaId))
        {
            var auditoriumIds = DataStore.Auditoriums
                .Where(a => a.CinemaId == cinemaId)
                .Select(a => a.Id)
                .ToHashSet();

            query = query.Where(s => auditoriumIds.Contains(s.AuditoriumId));
        }

        var summaries = new List<ScreeningSummary>();
        foreach (var screening in query.OrderBy(s => s.StartUtc))
        {
            var auditorium = DataStore.GetAuditorium(screening.AuditoriumId);
            var cinema = auditorium != null ? DataStore.Cinemas.FirstOrDefault(c => c.Id == auditorium.CinemaId) : null;
            var movie = DataStore.GetMovie(screening.MovieId);

            summaries.Add(new ScreeningSummary
            {
                ScreeningId = screening.Id,
                StartUtc = screening.StartUtc,
                CinemaId = cinema?.Id ?? auditorium?.CinemaId ?? "",
                CinemaName = cinema?.Name ?? "",
                CinemaState = cinema?.State ?? "",
                AuditoriumName = auditorium?.Name ?? "",
                BasePrice = screening.BasePrice,
                Class = screening.Class,
                Movie = movie
            });
        }

        return summaries;
    }

    public IReadOnlyList<ScreeningSummary> GetByCinema(string cinemaId, DateOnly date)
    {
        return DataStore.Screenings
            .Where(s =>
            {
                var auditorium = DataStore.GetAuditorium(s.AuditoriumId);
                if (auditorium == null || auditorium.CinemaId != cinemaId) return false;
                return DateOnly.FromDateTime(s.StartUtc) == date;
            })
            .OrderBy(s => s.StartUtc)
            .Select(s =>
            {
                var auditorium = DataStore.GetAuditorium(s.AuditoriumId);
                var cinema = auditorium != null ? DataStore.Cinemas.FirstOrDefault(c => c.Id == auditorium.CinemaId) : null;
                return new ScreeningSummary
                {
                    ScreeningId = s.Id,
                    StartUtc = s.StartUtc,
                    CinemaId = cinema?.Id ?? auditorium?.CinemaId ?? "",
                    CinemaName = cinema?.Name ?? "",
                    CinemaState = cinema?.State ?? "",
                    AuditoriumName = auditorium?.Name ?? "",
                    BasePrice = s.BasePrice,
                    Class = s.Class,
                    Movie = DataStore.GetMovie(s.MovieId)
                };
            })
            .ToList();
    }
}
