using WinFormsApp.Entities;
using WinFormsApp.Enums;
using WinFormsApp.Models;

namespace WinFormsApp.Logic;

// Booking orchestration: this module is the only place that understands
// the full “seat selection -> quote -> persisted booking” pipeline.
// UI/host layers should treat it as a black box.
internal sealed class BookingService
{
    readonly PricingService _pricing = new();

    public SeatMap GetSeatMap(string screeningId)
    {
        var screening = DataStore.GetScreening(screeningId)
            ?? throw new InvalidOperationException("Screening not found.");
        var auditorium = DataStore.GetAuditorium(screening.AuditoriumId)
            ?? throw new InvalidOperationException("Auditorium not found.");

        var seats = DataStore.BuildSeatMap(screeningId);
        return new SeatMap
        {
            ScreeningId = screeningId,
            Rows = auditorium.RowCount,
            Columns = auditorium.ColumnCount,
            Seats = seats
        };
    }

    public OrderQuote Preview(string screeningId, IReadOnlyList<SeatSelection> seats)
    {
        EnsureSeatsAvailable(screeningId, seats);
        return _pricing.Calculate(screeningId, seats);
    }

    public Booking Confirm(CheckoutRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerName))
            throw new ArgumentException("Name is required.", nameof(request));
        if (!request.Seats.Any())
            throw new ArgumentException("At least one seat must be selected.", nameof(request));

        EnsureSeatsAvailable(request.ScreeningId, request.Seats);
        var quote = _pricing.Calculate(request.ScreeningId, request.Seats);

        var booking = new Booking
        {
            ScreeningId = request.ScreeningId,
            ReferenceCode = GenerateReference(),
            CustomerName = request.CustomerName,
            CustomerEmail = request.CustomerEmail,
            CustomerPhone = request.CustomerPhone,
            Subtotal = quote.Subtotal,
            Discount = quote.Discount,
            Total = quote.Total,
            Lines = request.Seats.Select(seat =>
            {
                var ticketType = DataStore.GetTicketType(seat.TicketTypeId)!;
                var quoteLine = quote.Lines.First(l => l.Description.Contains(seat.SeatLabel, StringComparison.OrdinalIgnoreCase));
                return new BookingLine
                {
                    SeatLabel = seat.SeatLabel,
                    TicketTypeId = seat.TicketTypeId,
                    UnitPrice = quoteLine.UnitPrice
                };
            }).ToList()
        };

        DataStore.AddBooking(booking);
        return booking;
    }

    void EnsureSeatsAvailable(string screeningId, IReadOnlyList<SeatSelection> seats)
    {
        var seatMap = DataStore.BuildSeatMap(screeningId);
        foreach (var seat in seats)
        {
            if (!seatMap.TryGetValue(seat.SeatLabel, out var state))
                throw new InvalidOperationException($"Seat {seat.SeatLabel} does not exist.");
            if (state == SeatState.Booked)
                throw new InvalidOperationException($"Seat {seat.SeatLabel} is already booked.");
            if (state == SeatState.Blocked)
                throw new InvalidOperationException($"Seat {seat.SeatLabel} is blocked.");
            if (string.IsNullOrWhiteSpace(seat.TicketTypeId))
                throw new InvalidOperationException($"Seat {seat.SeatLabel} requires a ticket type.");
            if (DataStore.GetTicketType(seat.TicketTypeId) is null)
                throw new InvalidOperationException($"Ticket type '{seat.TicketTypeId}' not recognised.");
        }
    }

    static string GenerateReference()
        => $"BK{Random.Shared.Next(100000, 999999)}";
}
