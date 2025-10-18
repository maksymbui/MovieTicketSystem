using MovieTickets.Core.Entities;

namespace MovieTickets.Core.Logic;

public sealed class DealService
{

    public Deal? GetDealForMovie(string movieId)
    {
        return DataStore.DealsData.FirstOrDefault(d => d.MovieId == movieId);
    }

    public IReadOnlyList<Deal> GetAll()
    {
        return DataStore.Deals
            .OrderBy(m => m.MovieId)
            .ToList();
    }

    public bool AddDeal(Deal deal)
    {
        foreach (var existingDeal in DataStore.DealsData)
        {
            if (existingDeal.MovieId == deal.MovieId)
            {
                return false;
            }
        }
        DataStore.DealsData.Add(deal);
        DataStore.SaveDeals();
        return true;
    }

    public void RemoveDeal(string dealId)
    {
        var deal = DataStore.DealsData.FirstOrDefault(d => d.Id == dealId);
        if (deal != null)
        {
            DataStore.DealsData.Remove(deal);
            DataStore.SaveDeals();
        }
    }

    public void UpdateDeal(Deal deal)
    {
        var existingDeal = DataStore.DealsData.FirstOrDefault(d => d.Id == deal.Id);
        if (existingDeal != null)
        {
            existingDeal.MovieId = deal.MovieId;
            existingDeal.Discount = deal.Discount;
        }
        DataStore.SaveDeals();
    }
}