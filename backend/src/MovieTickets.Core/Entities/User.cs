namespace MovieTickets.Core.Entities;

public sealed class User
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string Email { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "User";
    public DateTime CreatedUtc { get; init; } = DateTime.UtcNow;
}
