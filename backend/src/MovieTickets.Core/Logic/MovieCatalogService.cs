using MovieTickets.Core.Entities;

namespace MovieTickets.Core.Logic;

// Read-only queries over the movie list. No caching, no side effects.
public sealed class MovieCatalogService
{
    public IReadOnlyList<Movie> GetAll()
        => DataStore.Movies
            .OrderBy(m => m.Title)
            .ToList();

    public IReadOnlyList<Movie> GetNowShowing(DateOnly date)
    {
        var screenings = DataStore.Screenings
            .Where(s => DateOnly.FromDateTime(s.StartUtc) == date)
            .Select(s => s.MovieId)
            .Distinct()
            .ToHashSet();

        return DataStore.Movies
            .Where(m => screenings.Contains(m.Id))
            .OrderBy(m => m.Title)
            .ToList();
    }

    public IReadOnlyList<Movie> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return GetAll();

        query = query.Trim();
        return DataStore.Movies
            .Where(m =>
                m.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                m.Genres.Any(g => g.Contains(query, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(m => m.Title)
            .ToList();
    }
}
