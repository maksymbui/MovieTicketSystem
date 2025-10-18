using MovieTickets.Core.Entities;
using MovieTickets.Core.Enums;
using MovieTickets.Core.Models;

namespace MovieTickets.Core.Logic;

// Booking orchestration: this module is the only place that understands
// the full “seat selection -> quote -> persisted booking” pipeline.
// UI/host layers should treat it as a black box.
public sealed class BookingService
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

    public OrderQuote Preview(string screeningId, IReadOnlyList<SeatSelection> seats, string promoCode)
    {
        EnsureSeatsAvailable(screeningId, seats);
        return _pricing.Calculate(screeningId, seats, promoCode);
    }

    public Booking Confirm(CheckoutRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerName))
            throw new ArgumentException("Name is required.", nameof(request));
        if (!request.Seats.Any())
            throw new ArgumentException("At least one seat must be selected.", nameof(request));

        EnsureSeatsAvailable(request.ScreeningId, request.Seats);
        var quote = _pricing.Calculate(request.ScreeningId, request.Seats, request.PromoCode);

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
                _ = DataStore.GetTicketType(seat.TicketTypeId)
                    ?? throw new InvalidOperationException($"Ticket type '{seat.TicketTypeId}' not found.");
                var quoteLine = quote.Lines.FirstOrDefault(l =>
                    l.SeatLabel.Equals(seat.SeatLabel, StringComparison.OrdinalIgnoreCase));
                if (quoteLine is null)
                    throw new InvalidOperationException($"Unable to locate quote line for seat {seat.SeatLabel}.");
                return new BookingLine
                {
                    SeatLabel = seat.SeatLabel,
                    TicketTypeId = seat.TicketTypeId,
                    UnitPrice = quoteLine.UnitPrice
                };
            }).ToList()
        };
        sendNotification(booking);
        RewardService rs = new RewardService();
        rs.RemoveReward(request.PromoCode);
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

    static void sendNotification(Booking booking)
    {
        var user = DataStore.GetUserByEmail(booking.CustomerEmail);
        if (user is null) return;
        var Content = $"Dear {user.DisplayName}, your booking with reference {booking.ReferenceCode} has been confirmed.";
        var ms = new MessageService();
        var rs = new RewardService();
        ms.SendMessageToUser(booking.CustomerEmail, Content);
        if (rs.IsEligibleForReward(user))
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();

            var reward = new Reward
            {
                RewardCode = new string(Enumerable.Range(0, 6)
                .Select(_ => chars[random.Next(chars.Length)]).ToArray()),
            };
            DataStore.AddReward(reward);
            var rewardContent = $"Congratulations {user.DisplayName}! You are now eligible for a reward. Please use this code to get 50% off: {reward.RewardCode}";
            ms.SendMessageToUser(booking.CustomerEmail, rewardContent);
        }
    }
}
