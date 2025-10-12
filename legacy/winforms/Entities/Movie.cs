using System.Text.Json.Serialization;

namespace WinFormsApp.Entities;

public sealed class Movie
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = "";
    public string Synopsis { get; set; } = "";
    public int RuntimeMinutes { get; set; }
    public string Rating { get; set; } = "PG";
    public string PosterUrl { get; set; } = "";

    [JsonPropertyName("genres")]
    public List<string> Genres { get; set; } = new();

    public override string ToString() => Title;
}
