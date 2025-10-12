namespace WinFormsApp.Models;

public sealed class CheckoutRequest
{
    public string ScreeningId { get; init; } = "";
    public IReadOnlyList<SeatSelection> Seats { get; init; } = Array.Empty<SeatSelection>();
    public string CustomerName { get; init; } = "";
    public string CustomerEmail { get; init; } = "";
    public string CustomerPhone { get; init; } = "";
}
