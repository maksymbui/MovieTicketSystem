namespace WinFormsApp.Entities;

public sealed class Cinema
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public string Suburb { get; set; } = "";
    public string State { get; set; } = "";

    public override string ToString() => $"{Name} ({Suburb})";
}
