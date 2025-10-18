using MovieTickets.Core.Entities;

namespace MovieTickets.Core.Logic;

public sealed class RewardService
{
    public bool IsEligibleForReward(User user)
    {
        var totalBookingsPurchased = DataStore.Bookings
            .Where(b => b.CustomerEmail == user.Email);
        return totalBookingsPurchased.Count() % 3 == 0;
    }

    public void RemoveReward(string rewardCode)
    {
        var reward = DataStore.Rewards.FirstOrDefault(r => r.RewardCode == rewardCode);
        if (reward != null)
        {
            DataStore.RemoveReward(reward);
            DataStore.SaveRewards();
        }
    }

}