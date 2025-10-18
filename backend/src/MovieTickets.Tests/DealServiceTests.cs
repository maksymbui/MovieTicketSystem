using NUnit.Framework;
using MovieTickets.Core.Logic;
using MovieTickets.Core.Entities;

namespace MovieTickets.Tests;

[TestFixture]
public class DealServiceTests
{
    private DealService DS;

    [SetUp]
    public void Setup()
    {
        DS = new DealService();
        DataStore.DealsData.Clear();
    }

    [Test]
    public void AddDeal_True()
    {
        // Arrange
        var deal = new Deal { MovieId = "movie-123", Discount = 15 };

        // Act
        var result = DS.AddDeal(deal);

        // Assert
        Assert.IsTrue(result);
        Assert.That(DataStore.DealsData.Count, Is.EqualTo(1));
    }

    [Test]
    public void GetDealForMovie_True()
    {
        // Arrange
        var deal = new Deal { MovieId = "movie-456", Discount = 20 };
        DataStore.DealsData.Add(deal);

        // Act
        var result = DS.GetDealForMovie("movie-456");

        // Assert
        Assert.IsNotNull(result);
        Assert.That(result.Discount, Is.EqualTo(20));
    }
}