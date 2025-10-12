using System.Linq;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using MovieTickets.Core.Entities;
using MovieTickets.Core.Logic;

namespace MovieTickets.Api.Services;

public sealed class TmdbImportService
{
    readonly HttpClient _httpClient;
    readonly TmdbOptions _options;

    const string PosterBaseUrl = "https://image.tmdb.org/t/p/w500";

    public TmdbImportService(HttpClient httpClient, IOptions<TmdbOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.ApiKey);

    public async Task<IReadOnlyList<Movie>> ImportPopularAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("TMDB API key is not configured.");

        var genres = await FetchGenreLookup(cancellationToken);
        var movies = new List<Movie>();

        for (var page = 1; page <= Math.Max(1, _options.Pages); page++)
        {
            var response = await GetAsync<TmdbDiscoverResponse>(
                $"discover/movie?include_adult=false&include_video=false&language={_options.Language}&region={_options.Region}&sort_by=popularity.desc&page={page}",
                cancellationToken);

            if (response?.Results == null)
                continue;

            foreach (var item in response.Results)
            {
                if (item == null) continue;

                var details = await GetAsync<TmdbMovieDetails>(
                    $"movie/{item.Id}?language={_options.Language}&append_to_response=release_dates",
                    cancellationToken);

                var mappedGenres = details?.Genres?.Select(g => g.Name).ToList()
                    ?? item.GenreIds?.Select(id => genres.TryGetValue(id, out var name) ? name : null)
                        .Where(name => !string.IsNullOrWhiteSpace(name))
                        .Cast<string>()
                        .ToList()
                    ?? new List<string>();

                var rating = ExtractCertification(details, _options.Region) ?? (item.Adult ? "MA15+" : "PG");
                var runtime = details?.Runtime ?? 0;

                movies.Add(new Movie
                {
                    Id = $"tmdb-{item.Id}",
                    Title = item.Title ?? item.OriginalTitle ?? "Untitled",
                    Synopsis = item.Overview ?? "Synopsis unavailable.",
                    RuntimeMinutes = runtime > 0 ? runtime : 110,
                    Rating = string.IsNullOrWhiteSpace(rating) ? "NR" : rating,
                    PosterUrl = string.IsNullOrWhiteSpace(item.PosterPath) ? "" : $"{PosterBaseUrl}{item.PosterPath}",
                    Genres = mappedGenres.Count > 0 ? mappedGenres : new List<string> { "Feature" }
                });

                if (_options.MaxMovies > 0 && movies.Count >= _options.MaxMovies)
                    break;
            }

            if (_options.MaxMovies > 0 && movies.Count >= _options.MaxMovies)
                break;
        }

        if (movies.Count > 0)
            DataStore.ReplaceMovies(movies);

        return movies;
    }

    async Task<T?> GetAsync<T>(string path, CancellationToken cancellationToken)
    {
        var separator = path.Contains('?') ? '&' : '?';
        var url = $"{path}{separator}api_key={_options.ApiKey}";
        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
    }

    async Task<Dictionary<int, string>> FetchGenreLookup(CancellationToken cancellationToken)
    {
        var genresResponse = await GetAsync<TmdbGenresResponse>($"genre/movie/list?language={_options.Language}", cancellationToken);
        return genresResponse?.Genres?.Where(g => g != null)
                   .ToDictionary(g => g!.Id, g => g.Name, comparer: EqualityComparer<int>.Default)
               ?? new Dictionary<int, string>();
    }

    static string? ExtractCertification(TmdbMovieDetails? details, string region)
    {
        if (details?.ReleaseDates?.Results == null)
            return null;

        var regionMatch = details.ReleaseDates.Results
            .FirstOrDefault(r => string.Equals(r.Iso3166_1, region, StringComparison.OrdinalIgnoreCase))
            ?? details.ReleaseDates.Results.FirstOrDefault(r => string.Equals(r.Iso3166_1, "US", StringComparison.OrdinalIgnoreCase));

        var certification = regionMatch?.ReleaseDates?
            .FirstOrDefault(rd => !string.IsNullOrWhiteSpace(rd.Certification))?.Certification;

        return string.IsNullOrWhiteSpace(certification) ? null : certification;
    }

    public sealed class TmdbOptions
    {
        public string ApiKey { get; set; } = "";
        public string Language { get; set; } = "en-AU";
        public string Region { get; set; } = "AU";
        public int Pages { get; set; } = 1;
        public int MaxMovies { get; set; } = 20;
    }

    sealed class TmdbDiscoverResponse
    {
        [JsonPropertyName("results")]
        public List<TmdbMovieSummary>? Results { get; init; }
    }

    sealed class TmdbMovieSummary
    {
        [JsonPropertyName("id")]
        public int Id { get; init; }

        [JsonPropertyName("title")]
        public string? Title { get; init; }

        [JsonPropertyName("original_title")]
        public string? OriginalTitle { get; init; }

        [JsonPropertyName("overview")]
        public string? Overview { get; init; }

        [JsonPropertyName("poster_path")]
        public string? PosterPath { get; init; }

        [JsonPropertyName("adult")]
        public bool Adult { get; init; }

        [JsonPropertyName("genre_ids")]
        public List<int>? GenreIds { get; init; }
    }

    sealed class TmdbGenresResponse
    {
        [JsonPropertyName("genres")]
        public List<TmdbGenre>? Genres { get; init; }
    }

    sealed class TmdbGenre
    {
        [JsonPropertyName("id")]
        public int Id { get; init; }

        [JsonPropertyName("name")]
        public string Name { get; init; } = "";
    }

    sealed class TmdbMovieDetails
    {
        [JsonPropertyName("runtime")]
        public int? Runtime { get; init; }

        [JsonPropertyName("genres")]
        public List<TmdbGenre>? Genres { get; init; }

        [JsonPropertyName("release_dates")]
        public TmdbReleaseDates? ReleaseDates { get; init; }
    }

    sealed class TmdbReleaseDates
    {
        [JsonPropertyName("results")]
        public List<TmdbReleaseDatesResult>? Results { get; init; }
    }

    sealed class TmdbReleaseDatesResult
    {
        [JsonPropertyName("iso_3166_1")]
        public string Iso3166_1 { get; init; } = "";

        [JsonPropertyName("release_dates")]
        public List<TmdbReleaseDateEntry>? ReleaseDates { get; init; }
    }

    sealed class TmdbReleaseDateEntry
    {
        [JsonPropertyName("certification")]
        public string? Certification { get; init; }
    }
}
