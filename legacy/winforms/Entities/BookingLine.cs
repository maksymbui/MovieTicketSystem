namespace WinFormsApp.Entities;

public sealed class BookingLine
{
    public string SeatLabel { get; set; } = "";
    public string TicketTypeId { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal LineTotal => UnitPrice * Quantity;
}
