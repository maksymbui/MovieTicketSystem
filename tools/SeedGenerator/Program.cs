using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;

var (storageArg, moviesArg) = ParseArgs(args);
var storageDirectory = ResolveStorageDirectory(storageArg);
var moviesPath = ResolveMoviesPath(moviesArg, storageDirectory);

Console.WriteLine($"[seed] Using storage directory: {storageDirectory}");
Console.WriteLine($"[seed] Using movies file: {moviesPath}");

var jsonOptions = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    WriteIndented = true
};

var movies = JsonSerializer.Deserialize<List<MovieRecord>>(File.ReadAllText(moviesPath), jsonOptions)
             ?? new List<MovieRecord>();

if (movies.Count == 0)
{
    Console.Error.WriteLine("No movies found – run TMDB import first or provide a movies.json path.");
    return 1;
}

Console.WriteLine($"[seed] Loaded {movies.Count} movies");

var cinemaTemplates = CinemaTemplates.All;
var cinemaOutputs = new List<CinemaOutput>();
var auditoriumOutputs = new List<AuditoriumOutput>();
var auditoriumContexts = new List<AuditoriumContext>();

var cinemaIndex = 1;
var auditoriumIndex = 1;

foreach (var template in cinemaTemplates)
{
    var cinemaId = $"cinema-{cinemaIndex:000}";
    cinemaOutputs.Add(new CinemaOutput(cinemaId, template.Name, template.Suburb, template.State));

    foreach (var auditorium in template.Auditoriums)
    {
        var auditoriumId = $"aud-{auditoriumIndex:000}";
        auditoriumOutputs.Add(new AuditoriumOutput(
            auditoriumId,
            cinemaId,
            auditorium.Name,
            auditorium.RowCount,
            auditorium.ColumnCount,
            auditorium.PremiumRowCutoff));

        auditoriumContexts.Add(new AuditoriumContext(auditoriumId, cinemaId, auditorium.Class));
        auditoriumIndex++;
    }

    cinemaIndex++;
}

var auditoriumByCinema = auditoriumContexts
    .GroupBy(a => a.CinemaId)
    .ToDictionary(g => g.Key, g => g.ToList());
var auditoriumLookup = auditoriumOutputs.ToDictionary(a => a.Id);

var classPrice = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
{
    ["Standard"] = 19.50m,
    ["Deluxe"] = 23.00m,
    ["VMax"] = 26.50m,
    ["GoldClass"] = 34.00m
};

var random = new Random(2025);
var baseDateLocal = DateTime.Today;
var sessionsByCinema = cinemaOutputs.ToDictionary(c => c.Id, _ => new List<ScreeningOutput>());
var usedSlotsByCinema = cinemaOutputs.ToDictionary(c => c.Id, _ => new HashSet<DateTime>());
var nextSessionId = 1;

bool TryScheduleSessions(string cinemaId, string movieId, int desiredCount)
{
    if (desiredCount <= 0 || sessionsByCinema[cinemaId].Count >= GeneratorConfig.SessionsPerCinema)
        return false;

    var auditoriumPool = auditoriumByCinema[cinemaId];
    var added = 0;
    var attempts = 0;
    var maxAttempts = GeneratorConfig.SlotRetryLimit * Math.Max(1, desiredCount);
    var dayOffset = random.Next(0, GeneratorConfig.DaysToGenerate);
    var slotsUsedForDay = new HashSet<TimeSpan>();

    while (added < desiredCount && attempts < maxAttempts)
    {
        attempts++;

        if (attempts % GeneratorConfig.SlotRetryLimit == 0)
        {
            dayOffset = random.Next(0, GeneratorConfig.DaysToGenerate);
            slotsUsedForDay.Clear();
        }

        if (sessionsByCinema[cinemaId].Count >= GeneratorConfig.SessionsPerCinema)
            break;

        var auditorium = auditoriumPool[random.Next(auditoriumPool.Count)];
        var slot = GeneratorConfig.TimeSlots[random.Next(GeneratorConfig.TimeSlots.Length)];

        if (!slotsUsedForDay.Add(slot))
            continue;

        var localStart = baseDateLocal.AddDays(dayOffset).Add(slot);
        var start = DateTime.SpecifyKind(localStart, DateTimeKind.Local).ToUniversalTime();

        if (!usedSlotsByCinema[cinemaId].Add(start))
            continue;

        var classKey = auditorium.Class;
        var nominal = classPrice.TryGetValue(classKey, out var price) ? price : 19.50m;
        var delta = (decimal)(random.NextDouble() * 3 - 1.5);
        var finalPrice = Math.Round(nominal + delta, 2, MidpointRounding.AwayFromZero);

        sessionsByCinema[cinemaId].Add(new ScreeningOutput(
            $"session-{nextSessionId:0000}",
            movieId,
            auditorium.AuditoriumId,
            start.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture),
            finalPrice,
            classKey));

        nextSessionId++;
        added++;
    }

    return added > 0;
}

var cinemaIds = cinemaOutputs.Select(c => c.Id).ToList();

foreach (var movie in movies.OrderBy(_ => random.Next()))
{
    var maxAssignable = cinemaIds.Count;
    var minTarget = Math.Min(GeneratorConfig.MinCinemasPerMovie, maxAssignable);
    var maxTarget = Math.Min(GeneratorConfig.MaxCinemasPerMovie, maxAssignable);
    var target = random.Next(minTarget, maxTarget + 1);

    var shuffled = cinemaIds.OrderBy(_ => random.Next()).ToList();
    var assigned = 0;

    foreach (var cinemaId in shuffled)
    {
        if (assigned >= target)
            break;

        var remainingCapacity = GeneratorConfig.SessionsPerCinema - sessionsByCinema[cinemaId].Count;
        if (remainingCapacity <= 0)
            continue;

        var request = random.Next(GeneratorConfig.MinSessionsPerAssignment, GeneratorConfig.MaxSessionsPerAssignment + 1);
        request = Math.Max(1, Math.Min(request, remainingCapacity));

        if (TryScheduleSessions(cinemaId, movie.Id, request))
            assigned++;
    }
}

foreach (var cinemaId in cinemaIds)
{
    while (sessionsByCinema[cinemaId].Count < GeneratorConfig.SessionsPerCinema)
    {
        var movie = movies[random.Next(movies.Count)];
        var remaining = GeneratorConfig.SessionsPerCinema - sessionsByCinema[cinemaId].Count;
        if (remaining <= 0)
            break;

        var request = Math.Min(GeneratorConfig.MinSessionsPerAssignment, remaining);
        if (!TryScheduleSessions(cinemaId, movie.Id, request))
        {
            if (!TryScheduleSessions(cinemaId, movie.Id, 1))
                break;
        }
    }
}

static List<BookingOutput> GenerateSeedBookings(
    IEnumerable<ScreeningOutput> screenings,
    IReadOnlyDictionary<string, AuditoriumOutput> auditoriumLookup,
    Random random)
{
    var bookings = new List<BookingOutput>();

    foreach (var screening in screenings)
    {
        if (!auditoriumLookup.TryGetValue(screening.AuditoriumId, out var auditorium))
            continue;

        var totalSeats = auditorium.RowCount * auditorium.ColumnCount;
        if (totalSeats == 0)
            continue;

        var occupancy =
            GeneratorConfig.MinPrebookedProportion +
            random.NextDouble() * (GeneratorConfig.MaxPrebookedProportion - GeneratorConfig.MinPrebookedProportion);

        var targetSeatCount = Math.Clamp((int)Math.Round(totalSeats * occupancy), 0, totalSeats);
        if (targetSeatCount == 0)
            continue;

        var reservedSeats = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var maxAttempts = targetSeatCount * 20;
        var attempts = 0;

        while (reservedSeats.Count < targetSeatCount && attempts < maxAttempts)
        {
            attempts++;

            var rowIndex = random.Next(0, auditorium.RowCount);
            var startColumn = random.Next(1, auditorium.ColumnCount + 1);
            var clusterSize = random.Next(GeneratorConfig.MinClusterSize, GeneratorConfig.MaxClusterSize + 1);
            var seats = new List<string>(clusterSize);

            if (startColumn + clusterSize - 1 > auditorium.ColumnCount)
                continue;

            var validCluster = true;
            for (var offset = 0; offset < clusterSize; offset++)
            {
                var column = startColumn + offset;

                if (IsBlockedSeat(auditorium, rowIndex, column))
                {
                    validCluster = false;
                    break;
                }

                var seatLabel = BuildSeatLabel(rowIndex, column);
                if (reservedSeats.Contains(seatLabel))
                {
                    validCluster = false;
                    break;
                }

                seats.Add(seatLabel);
            }

            if (!validCluster)
                continue;

            foreach (var seat in seats)
                reservedSeats.Add(seat);

            var unitPrice = Math.Max(15m, Math.Round(screening.BasePrice, 2));
            var lines = seats
                .Select(seat => new BookingLineOutput(seat, GeneratorConfig.DefaultTicketType, unitPrice, 1))
                .ToList();

            var subtotal = lines.Sum(line => line.UnitPrice * line.Quantity);

            bookings.Add(new BookingOutput(
                $"seed-{screening.Id}-{bookings.Count + 1:000}",
                screening.Id,
                $"SD{random.Next(100000, 999999)}",
                "Seeded Guest",
                "seeded@example.com",
                "0000000000",
                DateTime.UtcNow,
                subtotal,
                0m,
                subtotal,
                lines
            ));
        }
    }

    return bookings;
}

static bool IsBlockedSeat(AuditoriumOutput auditorium, int rowIndex, int column)
{
    if (rowIndex == auditorium.RowCount - 1 && (column == 1 || column == 2))
        return true;
    return false;
}

static string BuildSeatLabel(int rowIndex, int column)
    => $"{(char)('A' + rowIndex)}{column}";

var screenings = sessionsByCinema.Values
    .SelectMany(s => s)
    .OrderBy(s => s.StartUtc, StringComparer.Ordinal)
    .ToList();

var bookings = GenerateSeedBookings(screenings, auditoriumLookup, random);

WriteJson(Path.Combine(storageDirectory, "cinemas.json"), cinemaOutputs, jsonOptions);
WriteJson(Path.Combine(storageDirectory, "auditoriums.json"), auditoriumOutputs, jsonOptions);
WriteJson(Path.Combine(storageDirectory, "screenings.json"), screenings, jsonOptions);
WriteJson(Path.Combine(storageDirectory, "bookings.json"), bookings, jsonOptions);

Console.WriteLine($"[seed] Wrote {cinemaOutputs.Count} cinemas, {auditoriumOutputs.Count} auditoriums, {screenings.Count} screenings, {bookings.Count} bookings");

return 0;

static (string? storage, string? movies) ParseArgs(string[] args)
{
    string? storage = null;
    string? movies = null;

    for (var i = 0; i < args.Length; i++)
    {
        switch (args[i])
        {
            case "--storage":
            case "-s":
                if (i + 1 >= args.Length)
                    throw new ArgumentException("Missing value for --storage");
                storage = args[++i];
                break;
            case "--movies":
            case "-m":
                if (i + 1 >= args.Length)
                    throw new ArgumentException("Missing value for --movies");
                movies = args[++i];
                break;
        }
    }

    return (storage, movies);
}

static string ResolveStorageDirectory(string? explicitPath)
{
    if (!string.IsNullOrWhiteSpace(explicitPath))
    {
        if (!Directory.Exists(explicitPath))
            throw new DirectoryNotFoundException($"Storage directory '{explicitPath}' not found.");
        return Path.GetFullPath(explicitPath);
    }

    var current = Environment.CurrentDirectory;
    while (current is not null)
    {
        var candidate = Path.Combine(current, "storage");
        if (Directory.Exists(candidate))
            return candidate;
        current = Directory.GetParent(current)?.FullName;
    }

    throw new InvalidOperationException("Unable to locate storage directory – pass --storage <path>.");
}

static string ResolveMoviesPath(string? explicitPath, string storageDirectory)
{
    var candidates = new List<string>();

    if (!string.IsNullOrWhiteSpace(explicitPath))
        candidates.Add(explicitPath);

    candidates.Add(Path.Combine(storageDirectory, "movies.json"));

    var repoRoot = Directory.GetParent(storageDirectory)?.FullName;
    if (repoRoot is not null)
    {
        candidates.Add(Path.Combine(repoRoot, "backend", "src", "MovieTickets.Api", "bin", "Debug", "net8.0", "storage", "movies.json"));
        candidates.Add(Path.Combine(repoRoot, "backend", "src", "MovieTickets.Api", "bin", "Release", "net8.0", "storage", "movies.json"));
    }

    string? bestPath = null;
    List<MovieRecord>? bestData = null;

    foreach (var candidate in candidates)
    {
        if (!File.Exists(candidate))
            continue;

        try
        {
            var data = JsonSerializer.Deserialize<List<MovieRecord>>(File.ReadAllText(candidate), new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (data is null || data.Count == 0)
                continue;

            if (bestData is null || data.Count > bestData.Count)
            {
                bestData = data;
                bestPath = candidate;
            }
        }
        catch
        {
            // ignore malformed files
        }
    }

    if (bestPath is null)
        throw new InvalidOperationException("Unable to locate movies.json. Provide a path via --movies.");

    if (bestPath != Path.Combine(storageDirectory, "movies.json"))
    {
        // Ensure storage copy is current so services stay in sync across rebuilds
        if (bestData is not null)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            File.WriteAllText(Path.Combine(storageDirectory, "movies.json"), JsonSerializer.Serialize(bestData, options));
        }
    }

    return Path.GetFullPath(bestPath);
}

static void WriteJson<T>(string path, T payload, JsonSerializerOptions options)
{
    var directory = Path.GetDirectoryName(path);
    if (!string.IsNullOrEmpty(directory))
        Directory.CreateDirectory(directory);

    File.WriteAllText(path, JsonSerializer.Serialize(payload, options));
}

record MovieRecord
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
}

record CinemaOutput(string Id, string Name, string Suburb, string State);

record AuditoriumOutput(string Id, string CinemaId, string Name, int RowCount, int ColumnCount, int PremiumRowCutoff);

record ScreeningOutput(string Id, string MovieId, string AuditoriumId, string StartUtc, decimal BasePrice, string Class);

record AuditoriumContext(string AuditoriumId, string CinemaId, string Class);

record CinemaTemplate(string Name, string Suburb, string State, IReadOnlyList<AuditoriumTemplate> Auditoriums)
{
    public CinemaTemplate(string name, string suburb, string state, params AuditoriumTemplate[] auditoriums)
        : this(name, suburb, state, (IReadOnlyList<AuditoriumTemplate>)auditoriums)
    {
    }
}

record AuditoriumTemplate(string Name, int RowCount, int ColumnCount, int PremiumRowCutoff, string Class);

record BookingOutput(
    string Id,
    string ScreeningId,
    string ReferenceCode,
    string CustomerName,
    string CustomerEmail,
    string CustomerPhone,
    DateTime CreatedUtc,
    decimal Subtotal,
    decimal Discount,
    decimal Total,
    List<BookingLineOutput> Lines);

record BookingLineOutput(string SeatLabel, string TicketTypeId, decimal UnitPrice, int Quantity);

static class GeneratorConfig
{
    public const int DaysToGenerate = 7; // today + next 6 days
    public const int SessionsPerCinema = 36;
    public const int MinCinemasPerMovie = 8;
    public const int MaxCinemasPerMovie = 14;
    public const int SlotRetryLimit = 60;
    public const int MinSessionsPerAssignment = 2;
    public const int MaxSessionsPerAssignment = 5;
    public const int MinClusterSize = 2;
    public const int MaxClusterSize = 4;
    public const double MinPrebookedProportion = 0.12;
    public const double MaxPrebookedProportion = 0.28;
    public const string DefaultTicketType = "t_adult";

    public static readonly TimeSpan[] TimeSlots =
    {
        new(8, 0, 0),
        new(9, 30, 0),
        new(11, 15, 0),
        new(13, 0, 0),
        new(14, 45, 0),
        new(16, 30, 0),
        new(18, 15, 0),
        new(20, 0, 0)
    };
}

static class CinemaTemplates
{
    public static IReadOnlyList<CinemaTemplate> All { get; } = new List<CinemaTemplate>
    {
        new("Harbour Lights Cinemas", "Sydney CBD", "NSW",
            new AuditoriumTemplate("Harbour Screen 1", 10, 16, 2, "Standard"),
            new AuditoriumTemplate("Harbour V-Max", 12, 18, 3, "VMax"),
            new AuditoriumTemplate("Harbour Gold Lounge", 6, 10, 1, "GoldClass")),
        new("Parramatta Central", "Parramatta", "NSW",
            new AuditoriumTemplate("Central Screen 1", 9, 14, 2, "Standard"),
            new AuditoriumTemplate("Central Premiere", 7, 12, 1, "Deluxe")),
        new("Hurstville Grand", "Hurstville", "NSW",
            new AuditoriumTemplate("Grand Screen", 10, 15, 2, "Standard"),
            new AuditoriumTemplate("Grand V-Max", 12, 18, 3, "VMax")),
        new("Newcastle Waterfront", "Newcastle", "NSW",
            new AuditoriumTemplate("Waterfront Screen", 9, 14, 2, "Standard"),
            new AuditoriumTemplate("Harbour Lounge", 7, 11, 1, "Deluxe")),
        new("Bondi Junction Luxe", "Bondi Junction", "NSW",
            new AuditoriumTemplate("Luxe Screen", 9, 14, 2, "Standard"),
            new AuditoriumTemplate("Luxe Gold", 6, 10, 1, "GoldClass")),
        new("Castle Hill Epic", "Castle Hill", "NSW",
            new AuditoriumTemplate("Epic Screen", 10, 16, 2, "Standard"),
            new AuditoriumTemplate("Epic V-Max", 12, 18, 3, "VMax")),
        new("Melbourne Central Screens", "Melbourne CBD", "VIC",
            new AuditoriumTemplate("Central Screen 1", 11, 18, 3, "Standard"),
            new AuditoriumTemplate("Central V-Max", 13, 20, 4, "VMax"),
            new AuditoriumTemplate("Central Gold Lounge", 6, 10, 1, "GoldClass")),
        new("Docklands Luxe", "Docklands", "VIC",
            new AuditoriumTemplate("Luxe Screen", 9, 14, 2, "Standard"),
            new AuditoriumTemplate("Luxe Premiere", 7, 12, 1, "Deluxe")),
        new("Southbank Premium", "Southbank", "VIC",
            new AuditoriumTemplate("Southbank Screen", 10, 16, 2, "Standard"),
            new AuditoriumTemplate("Southbank Gold", 6, 10, 1, "GoldClass")),
        new("Geelong Waterfront Screens", "Geelong", "VIC",
            new AuditoriumTemplate("Waterfront Screen", 9, 15, 2, "Standard"),
            new AuditoriumTemplate("Waterfront Lounge", 7, 11, 1, "Deluxe")),
        new("Chadstone Aurora", "Chadstone", "VIC",
            new AuditoriumTemplate("Aurora Screen", 10, 16, 2, "Standard"),
            new AuditoriumTemplate("Aurora V-Max", 12, 18, 3, "VMax")),
        new("Brisbane Riverfront Cinema", "Brisbane CBD", "QLD",
            new AuditoriumTemplate("Riverfront Screen", 10, 16, 2, "Standard"),
            new AuditoriumTemplate("Riverfront Premiere", 8, 12, 1, "Deluxe")),
        new("Pacific Fair Max", "Broadbeach", "QLD",
            new AuditoriumTemplate("Pacific Screen", 10, 16, 2, "Standard"),
            new AuditoriumTemplate("Pacific V-Max", 13, 19, 3, "VMax")),
        new("Cairns Northern Lights", "Cairns", "QLD",
            new AuditoriumTemplate("Northern Screen", 9, 15, 2, "Standard")),
        new("Townsville Coral Cinemas", "Townsville", "QLD",
            new AuditoriumTemplate("Coral Screen", 9, 14, 2, "Standard"),
            new AuditoriumTemplate("Coral Lounge", 7, 11, 1, "Deluxe")),
        new("Sunshine Coast Horizon", "Maroochydore", "QLD",
            new AuditoriumTemplate("Horizon Screen", 9, 15, 2, "Standard"),
            new AuditoriumTemplate("Horizon Luxe", 7, 11, 1, "GoldClass")),
        new("Adelaide City Cinema", "Adelaide", "SA",
            new AuditoriumTemplate("City Screen", 9, 15, 2, "Standard"),
            new AuditoriumTemplate("City Premiere", 7, 11, 1, "Deluxe")),
        new("Glenelg Seaside Screens", "Glenelg", "SA",
            new AuditoriumTemplate("Seaside Screen", 9, 14, 2, "Standard"),
            new AuditoriumTemplate("Seaside Gold Lounge", 6, 10, 1, "GoldClass")),
        new("Mount Barker Summit", "Mount Barker", "SA",
            new AuditoriumTemplate("Summit Screen", 8, 12, 2, "Standard")),
        new("Norwood ArtHouse", "Norwood", "SA",
            new AuditoriumTemplate("ArtHouse Screen", 8, 12, 2, "Standard"),
            new AuditoriumTemplate("ArtHouse Boutique", 6, 10, 1, "Deluxe")),
        new("Port Adelaide Docks Cinema", "Port Adelaide", "SA",
            new AuditoriumTemplate("Docks Screen", 9, 14, 2, "Standard"),
            new AuditoriumTemplate("Docks Lounge", 7, 11, 1, "Deluxe")),
        new("Perth Skyline Cinema", "Perth", "WA",
            new AuditoriumTemplate("Skyline Screen", 10, 16, 2, "Standard"),
            new AuditoriumTemplate("Skyline V-Max", 13, 20, 4, "VMax")),
        new("Fremantle Harbourhouse", "Fremantle", "WA",
            new AuditoriumTemplate("Harbourhouse Screen", 9, 14, 2, "Standard"),
            new AuditoriumTemplate("Harbourhouse Premiere", 7, 12, 1, "Deluxe")),
        new("Joondalup Lakeside", "Joondalup", "WA",
            new AuditoriumTemplate("Lakeside Screen", 9, 14, 2, "Standard")),
        new("Albany Coastline Cinema", "Albany", "WA",
            new AuditoriumTemplate("Coastline Screen", 9, 14, 2, "Standard"),
            new AuditoriumTemplate("Coastline Lounge", 7, 11, 1, "GoldClass"))
    };
}
