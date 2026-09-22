using DaMaiDeparte.Web.Models;
using DaMaiDeparte.Web.Services;

namespace DaMaiDeparte.Tests;

public class LocationTests : IDisposable
{
    private readonly TestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task A_valid_country_city_pair_resolves()
    {
        await using var db = _database.CreateContext();

        var resolved = await _database.CreateLocationService(db)
            .ResolveAsync(_database.CountryId, _database.ClujId, null);

        Assert.NotNull(resolved);
        Assert.Equal("Cluj-Napoca", resolved!.CityName);
        Assert.Null(resolved.NeighborhoodId);
        Assert.Equal("Cluj-Napoca", resolved.Display);
    }

    [Fact]
    public async Task A_city_with_a_neighborhood_is_displayed_with_a_separator()
    {
        await using var db = _database.CreateContext();

        var resolved = await _database.CreateLocationService(db)
            .ResolveAsync(_database.CountryId, _database.ClujId, _database.MarastiId);

        Assert.Equal("Cluj-Napoca · Mărăști", resolved!.Display);
    }

    [Fact]
    public async Task A_neighborhood_from_another_city_does_not_resolve()
    {
        await using var db = _database.CreateContext();

        var resolved = await _database.CreateLocationService(db)
            .ResolveAsync(_database.CountryId, _database.ClujId, _database.BrasovCentruId);

        Assert.Null(resolved);
    }

    [Fact]
    public async Task A_city_from_another_country_does_not_resolve()
    {
        await using var db = _database.CreateContext();

        var resolved = await _database.CreateLocationService(db).ResolveAsync(9999, _database.ClujId, null);

        Assert.Null(resolved);
    }

    [Fact]
    public async Task Neighborhoods_are_scoped_to_their_city()
    {
        await using var db = _database.CreateContext();
        var service = _database.CreateLocationService(db);

        var cluj = await service.GetNeighborhoodsAsync(_database.ClujId);
        var brasov = await service.GetNeighborhoodsAsync(_database.BrasovId);

        Assert.Equal(new[] { "Gheorgheni", "Mărăști" }, cluj.Select(n => n.Name));
        Assert.Equal(new[] { "Centru" }, brasov.Select(n => n.Name));
    }

    [Fact]
    public async Task The_preferred_location_is_read_back_from_the_profile()
    {
        await using var db = _database.CreateContext();

        var preferred = await _database.CreateLocationService(db).ResolvePreferredAsync(TestDatabase.DonatorId);

        Assert.Equal("Cluj-Napoca · Mărăști", preferred!.Display);
    }

    [Fact]
    public async Task Saving_a_default_location_does_not_touch_other_users()
    {
        await using (var db = _database.CreateContext())
        {
            var brasov = await _database.CreateLocationService(db).ResolveAsync(_database.CountryId, _database.BrasovId, null);
            await _database.CreateLocationService(db).SavePreferredAsync(TestDatabase.DonatorId, brasov!);
        }

        await using var check = _database.CreateContext();
        var service = _database.CreateLocationService(check);

        Assert.Equal("Brașov", (await service.ResolvePreferredAsync(TestDatabase.DonatorId))!.Display);
        Assert.Equal("Cluj-Napoca · Gheorgheni", (await service.ResolvePreferredAsync(TestDatabase.ReceiverId))!.Display);
    }

    // ---------- The feed is scoped to the browsing city ----------

    [Fact]
    public async Task Switching_city_changes_the_listings()
    {
        var clujId = await _database.CreateDonationAsync(title: "Pâine din Cluj");
        var brasovId = await _database.CreateDonationAsync(
            title: "Paste din Brașov", cityId: _database.BrasovId, neighborhoodId: _database.BrasovCentruId);

        await using var db = _database.CreateContext();
        var service = _database.CreateDonationService(db);

        var cluj = await service.SearchFeedAsync(_database.Feed(_database.ClujId), TestDatabase.ReceiverId);
        var brasov = await service.SearchFeedAsync(_database.Feed(_database.BrasovId), TestDatabase.ReceiverId);

        Assert.Contains(cluj.Items, i => i.Id == clujId);
        Assert.DoesNotContain(cluj.Items, i => i.Id == brasovId);

        Assert.Contains(brasov.Items, i => i.Id == brasovId);
        Assert.DoesNotContain(brasov.Items, i => i.Id == clujId);
    }

    [Fact]
    public async Task The_neighborhood_filter_narrows_the_city_feed()
    {
        var marasti = await _database.CreateDonationAsync(title: "Pâine Mărăști", neighborhoodId: _database.MarastiId);
        var gheorgheni = await _database.CreateDonationAsync(title: "Paste Gheorgheni", neighborhoodId: _database.GheorgheniId);

        await using var db = _database.CreateContext();
        var service = _database.CreateDonationService(db);

        var all = await service.SearchFeedAsync(_database.Feed(), TestDatabase.ReceiverId);
        var filtered = await service.SearchFeedAsync(
            _database.Feed(neighborhoodId: _database.MarastiId), TestDatabase.ReceiverId);

        Assert.Equal(2, all.TotalCount);
        Assert.Equal(1, filtered.TotalCount);
        Assert.Equal(marasti, filtered.Items[0].Id);
        Assert.DoesNotContain(filtered.Items, i => i.Id == gheorgheni);
    }

    [Fact]
    public async Task The_feed_marks_the_viewers_own_listings()
    {
        var mine = await _database.CreateDonationAsync(TestDatabase.DonatorId, "Pâine de la mine");
        await _database.CreateDonationAsync(TestDatabase.OtherDonatorId, "Paste de la altcineva");

        await using var db = _database.CreateContext();
        var feed = await _database.CreateDonationService(db).SearchFeedAsync(_database.Feed(), TestDatabase.DonatorId);

        Assert.True(feed.Items.Single(i => i.Id == mine).IsOwn);
        Assert.Single(feed.Items.Where(i => i.IsOwn));
    }

    [Fact]
    public async Task The_feed_paginates_in_the_database()
    {
        for (var i = 1; i <= 5; i++)
        {
            await _database.CreateDonationAsync(title: $"Cutie de cereale {i}");
        }

        await using var db = _database.CreateContext();
        var service = _database.CreateDonationService(db);

        var page1 = await service.SearchFeedAsync(
            new FeedQuery { CityId = _database.ClujId, PageSize = 2, Page = 1 }, TestDatabase.ReceiverId);
        var page3 = await service.SearchFeedAsync(
            new FeedQuery { CityId = _database.ClujId, PageSize = 2, Page = 3 }, TestDatabase.ReceiverId);

        Assert.Equal(5, page1.TotalCount);
        Assert.Equal(3, page1.TotalPages);
        Assert.Equal(2, page1.Items.Count);
        Assert.Single(page3.Items);
        Assert.Equal("Cutie de cereale 5", page1.Items[0].Title); // newest first
    }

    [Fact]
    public async Task Only_allowed_categories_are_offered()
    {
        await using var db = _database.CreateContext();

        var categories = await _database.CreateDonationService(db).GetAllowedCategoriesAsync();

        Assert.All(categories, c => Assert.True(c.IsAllowed && c.IsActive));
        Assert.DoesNotContain(categories, c => c.Key is "Meat" or "Dairy" or "Fish" or "Alcohol" or "HomeCooked");
        Assert.Contains(categories, c => c.Key == "PackagedBakery");
    }

    [Fact]
    public async Task Cancelled_and_completed_food_never_appears_in_any_feed_filter()
    {
        var cancelled = await _database.CreateDonationAsync(title: "De anulat");
        var (completed, _) = await _database.CreateReservedDonationAsync();

        await using (var db = _database.CreateContext())
        {
            var service = _database.CreateDonationService(db);
            await service.CancelAsync(cancelled, TestDatabase.DonatorId);
            await service.CompleteAsync(completed, TestDatabase.DonatorId);
        }

        await using var check = _database.CreateContext();
        var feed = _database.CreateDonationService(check);

        foreach (var filter in new[] { FeedFilter.All, FeedFilter.Available, FeedFilter.Reserved })
        {
            var results = await feed.SearchFeedAsync(_database.Feed(filter: filter), TestDatabase.OtherReceiverId);
            Assert.DoesNotContain(results.Items, i => i.Id == cancelled || i.Id == completed);
        }
    }
}
