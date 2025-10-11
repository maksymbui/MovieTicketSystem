using System.Text.Json;
using System.Text.Json.Serialization;
using MovieTickets.Core.Entities;
using MovieTickets.Core.Enums;

namespace MovieTickets.Core.Logic;

// Lightweight JSON-backed store. Swap with EF/realtime later; services only talk to this API.
public static class DataStore
{
    static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    static readonly List<Movie> MoviesData = new();
    static readonly List<Cinema> CinemasData = new();
    static readonly List<Auditorium> AuditoriumsData = new();
    static readonly List<Screening> ScreeningsData = new();
    static readonly List<TicketType> TicketTypesData = new();
    static readonly List<Booking> BookingsData = new();
    static readonly Dictionary<string, HashSet<string>> BookedSeatLookup = new();

    static string RootPath = "";

    public static IReadOnlyList<Movie> Movies => MoviesData;
    public static IReadOnlyList<Cinema> Cinemas => CinemasData;
    public static IReadOnlyList<Auditorium> Auditoriums => AuditoriumsData;
    public static IReadOnlyList<Screening> Screenings => ScreeningsData;
    public static IReadOnlyList<TicketType> TicketTypes => TicketTypesData;
    public static IReadOnlyList<Booking> Bookings => BookingsData;

    public static void Load()
    {
        RootPath = Path.Combine(AppContext.BaseDirectory, "storage");
        Directory.CreateDirectory(RootPath);

        LoadInto(MoviesData, "movies.json");
        LoadInto(CinemasData, "cinemas.json");
        LoadInto(AuditoriumsData, "auditoriums.json");
        LoadInto(ScreeningsData, "screenings.json");
        LoadInto(TicketTypesData, "ticket_types.json");
        LoadInto(BookingsData, "bookings.json");

        BookedSeatLookup.Clear();
        foreach (var screening in ScreeningsData)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var matches = BookingsData.Where(b => b.ScreeningId == screening.Id);
            foreach (var booking in matches)
                foreach (var line in booking.Lines)
                    set.Add(line.SeatLabel);
            BookedSeatLookup[screening.Id] = set;
        }
    }

    public static IEnumerable<string> GetBookedSeats(string screeningId)
        => BookedSeatLookup.TryGetValue(screeningId, out var set)
            ? set
            : Array.Empty<string>();

    public static TicketType? GetTicketType(string ticketTypeId)
        => TicketTypesData.FirstOrDefault(t => t.Id == ticketTypeId);

    public static Screening? GetScreening(string screeningId)
        => ScreeningsData.FirstOrDefault(s => s.Id == screeningId);

    public static Auditorium? GetAuditorium(string auditoriumId)
        => AuditoriumsData.FirstOrDefault(a => a.Id == auditoriumId);

    public static Movie? GetMovie(string movieId)
        => MoviesData.FirstOrDefault(m => m.Id == movieId);

    public static void AddBooking(Booking booking)
    {
        if (!BookedSeatLookup.TryGetValue(booking.ScreeningId, out var set))
        {
            set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            BookedSeatLookup[booking.ScreeningId] = set;
        }

        foreach (var line in booking.Lines)
            set.Add(line.SeatLabel);

        BookingsData.Add(booking);
        SaveBookings();
    }

    public static void ReplaceMovies(IEnumerable<Movie> movies)
    {
        MoviesData.Clear();
        MoviesData.AddRange(movies);
        SaveMovies();
    }

    static void SaveBookings()
    {
        var path = Path.Combine(RootPath, "bookings.json");
        var json = JsonSerializer.Serialize(BookingsData, JsonOptions);
        File.WriteAllText(path, json);
    }

    static void SaveMovies()
    {
        var path = Path.Combine(RootPath, "movies.json");
        var json = JsonSerializer.Serialize(MoviesData, JsonOptions);
        File.WriteAllText(path, json);
    }

    static void LoadInto<T>(List<T> target, string fileName)
    {
        target.Clear();
        var path = Path.Combine(RootPath, fileName);
        if (!File.Exists(path))
        {
            File.WriteAllText(path, "[]");
            return;
        }

        var json = File.ReadAllText(path);
        var data = JsonSerializer.Deserialize<List<T>>(json, JsonOptions);
        if (data != null)
            target.AddRange(data);
    }

    public static SeatType GetSeatType(string screeningId, string seatLabel)
    {
        var screening = GetScreening(screeningId) ?? throw new InvalidOperationException("Screening not found.");
        return GetSeatTypeForAuditorium(screening.AuditoriumId, seatLabel);
    }

    static SeatType GetSeatTypeForAuditorium(string auditoriumId, string seatLabel)
    {
        var auditorium = GetAuditorium(auditoriumId)
            ?? throw new InvalidOperationException($"Auditorium '{auditoriumId}' not found.");

        if (seatLabel.Length < 2)
            return SeatType.Standard;

        var rowLetter = seatLabel[0];
        if (!int.TryParse(seatLabel.Substring(1), out var col))
            return SeatType.Standard;

        var rowIndex = rowLetter - 'A';

        if (rowIndex < auditorium.PremiumRowCutoff)
            return SeatType.Premium;

        if (rowIndex == auditorium.RowCount - 1 && (col == 1 || col == 2))
            return SeatType.Accessible;

        return SeatType.Standard;
    }

    public static IReadOnlyDictionary<string, SeatState> BuildSeatMap(string screeningId)
    {
        var screening = GetScreening(screeningId)
            ?? throw new InvalidOperationException($"Screening '{screeningId}' not found.");

        var auditorium = GetAuditorium(screening.AuditoriumId)
            ?? throw new InvalidOperationException($"Auditorium '{screening.AuditoriumId}' not found.");

        var booked = new HashSet<string>(GetBookedSeats(screeningId), StringComparer.OrdinalIgnoreCase);
        var map = new Dictionary<string, SeatState>(StringComparer.OrdinalIgnoreCase);

        for (var row = 0; row < auditorium.RowCount; row++)
        {
            var rowLetter = (char)('A' + row);
            for (var col = 1; col <= auditorium.ColumnCount; col++)
            {
                var label = $"{rowLetter}{col}";
                if (GetSeatTypeForAuditorium(auditorium.Id, label) == SeatType.Accessible && col == 1)
                {
                    map[label] = SeatState.Blocked;
                    continue;
                }

                map[label] = booked.Contains(label) ? SeatState.Booked : SeatState.Available;
            }
        }

        return map;
    }
}
