
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
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

// ==== Account/Auth DI ====
builder.Services.Configure<JwtTokenService.JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddSingleton<IUserRepository, JsonUserRepository>();

var jwtOpt = builder.Configuration.GetSection("Jwt").Get<JwtTokenService.JwtOptions>() ?? new JwtTokenService.JwtOptions();
var key = Encoding.UTF8.GetBytes(jwtOpt.Secret);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOpt.Issuer,
            ValidAudience = jwtOpt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });
builder.Services.AddAuthorization();


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
builder.Services.AddSingleton<DealService>();
builder.Services.AddSingleton<MessageService>();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

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
        var quote = bookings.Preview(request.ScreeningId, request.Seats, request.PromoCode);
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


// ==== Auth endpoints ====
app.MapPost("/api/auth/register", async (RegisterRequest req, IUserRepository users, JwtTokenService jwt) =>
{
    if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
        return Results.BadRequest(new { message = "Email and password are required." });

    var existing = await users.FindByEmailAsync(req.Email);
    if (existing is not null)
        return Results.BadRequest(new { message = "Email already registered." });

    var user = new User
    {
        Email = req.Email.Trim(),
        DisplayName = string.IsNullOrWhiteSpace(req.DisplayName) ? req.Email.Trim() : req.DisplayName.Trim(),
        PasswordHash = PasswordHasher.Hash(req.Password),
        Role = "User"
    };
    user = await users.CreateAsync(user);
    var token = jwt.Create(user);
    return Results.Ok(new AuthResponse(token, user.Id, user.Email, user.DisplayName, user.Role));
});

app.MapPost("/api/auth/login", async (LoginRequest req, IUserRepository users, JwtTokenService jwt) =>
{
    var user = await users.FindByEmailAsync(req.Email);
    if (user is null) return Results.Unauthorized();
    if (!PasswordHasher.Verify(req.Password, user.PasswordHash)) return Results.Unauthorized();
    var token = jwt.Create(user);
    return Results.Ok(new AuthResponse(token, user.Id, user.Email, user.DisplayName, user.Role));
});

app.MapGet("/api/auth/me", [Authorize] (HttpContext ctx) =>
{
    var email = ctx.User.FindFirst(ClaimTypes.Email)?.Value
        ?? ctx.User.FindFirst("email")?.Value
        ?? "";
    var name = ctx.User.Identity?.Name ?? "";
    var id = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? ctx.User.FindFirst("sub")?.Value
        ?? "";
    var role = ctx.User.FindFirst(ClaimTypes.Role)?.Value ?? "User";
    return Results.Ok(new { id, email, displayName = name, role });
});

// ==== Bookings lookup ====
app.MapGet("/api/bookings/{reference}", (string reference) =>
{
    var booking = MovieTickets.Core.Logic.DataStore.Bookings.FirstOrDefault(b =>
        string.Equals(b.ReferenceCode, reference, StringComparison.OrdinalIgnoreCase));
    return booking is null ? Results.NotFound(new { message = "Booking not found." }) : Results.Ok(booking);
});

app.MapGet("/api/my/bookings", [Authorize] (HttpContext ctx) =>
{
    var email = ctx.User.FindFirst(ClaimTypes.Email)?.Value
        ?? ctx.User.FindFirst("email")?.Value;
    if (string.IsNullOrWhiteSpace(email)) return Results.Unauthorized();
    var bookings = MovieTickets.Core.Logic.DataStore.Bookings
        .Where(b => string.Equals(b.CustomerEmail, email, StringComparison.OrdinalIgnoreCase));
    return Results.Ok(bookings);
});

app.MapGet("/api/deals", (DealService dealService) =>
{
    var deals = dealService.GetAll();
    return Results.Ok(deals);
});

app.MapPost("/api/deals/add", (Deal deal, DealService dealService) =>
{
    if (dealService.AddDeal(deal))
    {
        return Results.Ok(deal);
    }
    return Results.BadRequest(new { message = "Deal already exists for this movie." });
});

app.MapPost("/api/deals/remove", (Deal deal, DealService dealService) =>
{
    MovieTickets.Core.Logic.DataStore.RemoveDeal(deal.Id);
    return Results.Ok(deal);
});

app.MapPost("/api/deals/update", (Deal deal, DealService dealService) =>
{
    dealService.UpdateDeal(deal);
    return Results.Ok(deal);
});

app.MapGet("/api/messages/{userId}", (string userId, MessageService messageService) =>
{
    var messages = messageService.GetAllMessagesForUser(userId);
    return Results.Ok(messages);
});

app.Run();

public sealed record QuoteRequest(string ScreeningId, IReadOnlyList<SeatSelection> Seats, string PromoCode);
