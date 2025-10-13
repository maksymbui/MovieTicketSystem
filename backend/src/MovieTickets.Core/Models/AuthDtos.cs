namespace MovieTickets.Core.Models;

public sealed record RegisterRequest(string Email, string Password, string DisplayName);
public sealed record LoginRequest(string Email, string Password);
public sealed record AuthResponse(string Token, string UserId, string Email, string DisplayName, string Role);
