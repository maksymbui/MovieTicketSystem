using MovieTickets.Core.Entities;
using MovieTickets.Core.Enums;
using MovieTickets.Core.Models;

namespace MovieTickets.Core.Logic;

// Stateless price calculator. Feed it a screening + seat selections, it returns a trace.
public sealed class PricingService
{
    const decimal PremiumSeatSurcharge = 4.00m;
    const decimal AccessibleDiscount = 2.50m;

    public OrderQuote Calculate(string screeningId, IReadOnlyList<SeatSelection> seats)
    {
        var totalDiscount = 0m;

        if (seats.Count == 0)
            return new OrderQuote { ScreeningId = screeningId };

        var screening = DataStore.GetScreening(screeningId)
            ?? throw new InvalidOperationException("Screening not found.");

        var lines = new List<OrderQuoteLine>();
        decimal subtotal = 0m;

        foreach (var seatGroup in seats.GroupBy(s => s.TicketTypeId))
        {
            var ticketType = DataStore.GetTicketType(seatGroup.Key)
                ?? throw new InvalidOperationException($"Ticket type '{seatGroup.Key}' not found.");

            foreach (var seat in seatGroup)
            {
                var unit = CalculateSeatPrice(screening, ticketType, seat.SeatLabel);
                var seatDiscount = CalculateSeatDiscount(screening, unit);
                totalDiscount += seatDiscount;
                subtotal += unit;
                lines.Add(new OrderQuoteLine
                {
                    Description = $"{ticketType.Name} - Seat {seat.SeatLabel}",
                    Quantity = 1,
                    UnitPrice = unit,
                    SeatLabel = seat.SeatLabel,
                    TicketTypeId = seatGroup.Key
                });
            }
        }

        return new OrderQuote
        {
            ScreeningId = screeningId,
            Lines = lines,
            Subtotal = subtotal,
            Discount = totalDiscount
        };
    }

    decimal CalculateSeatPrice(Screening screening, TicketType ticketType, string seatLabel)
    {
        var price = ticketType.Price > 0 ? ticketType.Price : screening.BasePrice;

        var seatType = DataStore.GetSeatType(screening.Id, seatLabel);
        price += seatType switch
        {
            SeatType.Premium => PremiumSeatSurcharge,
            SeatType.Accessible => -AccessibleDiscount,
            _ => 0m
        };

        if (screening.Class == ScreeningClass.VMax)
            price += 2.50m;

        return Math.Round(price, 2, MidpointRounding.AwayFromZero);
    }

    decimal CalculateSeatDiscount(Screening screening, decimal seatPrice)
    {
        var deal = DataStore.DealsData.FirstOrDefault(d => d.MovieId == screening.MovieId && d.ExpiryDate > DateTime.UtcNow);
        if (deal != null)
        {
            return deal.Discount * seatPrice / 100;
        }
        return 0m;
    }
}
