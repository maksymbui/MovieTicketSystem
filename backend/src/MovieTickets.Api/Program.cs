using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Models;
using MovieTickets.Api.Services;
using MovieTickets.Core.Entities;
using MovieTickets.Core.Logic;
using MovieTickets.Core.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<TmdbImportService.TmdbOptions>(builder.Configuration.GetSection("Tmdb"));
builder.Services.AddHttpClient<TmdbImportService>(client =>
{
    client.BaseAddress = new Uri("https://api.themoviedb.org/3/");
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Movie Tickets API",
        Version = "v1"
    });
});

builder.Services.AddSingleton<MovieCatalogService>();
builder.Services.AddSingleton<ScreeningService>();
builder.Services.AddSingleton<PricingService>();
builder.Services.AddSingleton<BookingService>();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var app = builder.Build();

DataStore.Load();

app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/api/movies", (MovieCatalogService catalog) =>
{
    return Results.Ok(catalog.GetAll());
});

app.MapGet("/api/movies/{movieId}/screenings", (string movieId, ScreeningService screenings) =>
{
    var summaries = screenings.GetByMovie(movieId);
    return summaries.Count == 0 ? Results.NotFound() : Results.Ok(summaries);
});

app.MapGet("/api/ticket-types", () =>
{
    return Results.Ok(DataStore.TicketTypes);
});

app.MapGet("/api/screenings/{screeningId}/seatmap", (string screeningId, BookingService bookings) =>
{
    try
    {
        var map = bookings.GetSeatMap(screeningId);
        return Results.Ok(map);
    }
    catch (Exception ex)
    {
        return Results.NotFound(new { message = ex.Message });
    }
});

app.MapPost("/api/quote", (QuoteRequest request, BookingService bookings) =>
{
    try
    {
        var quote = bookings.Preview(request.ScreeningId, request.Seats);
        return Results.Ok(quote);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
});

app.MapPost("/api/bookings", (CheckoutRequest request, BookingService bookings) =>
{
    try
    {
        var booking = bookings.Confirm(request);
        return Results.Ok(booking);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
});

app.MapPost("/api/admin/import/tmdb", async (TmdbImportService importer, CancellationToken cancellationToken) =>
{
    if (!importer.IsConfigured)
        return Results.BadRequest(new { message = "TMDB API key is not configured. Set Tmdb:ApiKey in configuration." });

    try
    {
        var movies = await importer.ImportPopularAsync(cancellationToken);
        return Results.Ok(new { imported = movies.Count });
    }
    catch (HttpRequestException ex)
    {
        return Results.Problem(
            title: "TMDB request failed",
            detail: ex.Message,
            statusCode: StatusCodes.Status502BadGateway);
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
});

app.Run();

public sealed record QuoteRequest(string ScreeningId, IReadOnlyList<SeatSelection> Seats);
