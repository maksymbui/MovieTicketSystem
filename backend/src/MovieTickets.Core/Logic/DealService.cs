using MovieTickets.Core.Entities;

namespace MovieTickets.Core.Logic;

public sealed class DealService
{

    public Deal? GetDealForMovie(string movieId)
    {
        return DataStore.Deals.FirstOrDefault(d => d.MovieId == movieId);
    }

    public IReadOnlyList<Deal> GetAll()
    {
        return DataStore.Deals
            .OrderBy(m => m.MovieId)
            .ToList();
    }

    public bool AddDeal(Deal deal)
    {
        foreach (var existingDeal in DataStore.Deals)
        {
            if (existingDeal.MovieId == deal.MovieId)
            {
                return false;
            }
        }
        DataStore.AddDeal(deal);
        DataStore.SaveDeals();
        return true;
    }

    public void UpdateDeal(Deal deal)
    {
        var existingDeal = DataStore.Deals.FirstOrDefault(d => d.Id == deal.Id);
        if (existingDeal != null)
        {
            existingDeal.MovieId = deal.MovieId;
            existingDeal.Discount = deal.Discount;
        }
        DataStore.SaveDeals();
    }
}