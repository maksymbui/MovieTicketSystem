namespace MovieTickets.Core.Entities;

public sealed class Message
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string ToUserId { get; set; } = "";
    public string Content { get; set; } = "";
    public DateTime SentUtc { get; set; } = DateTime.UtcNow;

    public override string ToString() => $"{Id} (to {ToUserId}): {Content}";
}