namespace MovieTickets.Core.Entities;

public sealed class CastMember
{
    public string Name { get; init; } = "";
    public string? Character { get; init; }
    public string? ProfileUrl { get; init; }
}
