using DaMaiDeparte.Web.Models;
using DaMaiDeparte.Web.Resources;
using DaMaiDeparte.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace DaMaiDeparte.Tests;

public class DonationServiceTests : IDisposable
{
    private readonly TestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Donator_can_create_donation()
    {
        await using var db = _database.CreateContext();
        var service = _database.CreateDonationService(db);

        var result = await service.CreateAsync(TestDatabase.DonatorId, _database.ValidInput(), null);

        Assert.True(result.Succeeded);
        Assert.Equal(UiText.Success.DonationPublished, result.Message);

        var stored = await db.DonationItems.AsNoTracking().SingleAsync(d => d.Id == result.Value);
        Assert.Equal(DonationStatus.Available, stored.Status);
        Assert.Equal(TestDatabase.DonatorId, stored.DonatorId);
        Assert.Equal(DateTimeKind.Utc, stored.CreatedAt.Kind);
    }

    [Fact]
    public async Task New_donation_appears_in_public_feed()
    {
        var id = await _database.CreateDonationAsync(title: "Lampă de birou");

        await using var db = _database.CreateContext();
        var feed = await _database.CreateDonationService(db).SearchAvailableAsync(new DonationSearchQuery { Search = "Lampă" });

        Assert.Contains(feed.Items, i => i.Id == id);
    }

    [Fact]
    public async Task Receiver_cannot_create_donation()
    {
        await using var db = _database.CreateContext();
        var service = _database.CreateDonationService(db);

        var result = await service.CreateAsync(TestDatabase.ReceiverId, _database.ValidInput(), null);

        Assert.False(result.Succeeded);
        Assert.Equal(ServiceError.Forbidden, result.Error);
        Assert.False(await db.DonationItems.AnyAsync());
    }

    [Fact]
    public async Task Create_rejects_unknown_category()
    {
        await using var db = _database.CreateContext();
        var service = _database.CreateDonationService(db);
        var input = _database.ValidInput() with { CategoryId = 99999 };

        var result = await service.CreateAsync(TestDatabase.DonatorId, input, null);

        Assert.False(result.Succeeded);
        Assert.Equal(ServiceError.Validation, result.Error);
    }

    [Fact]
    public async Task Donator_can_edit_own_available_donation()
    {
        var id = await _database.CreateDonationAsync();

        await using var db = _database.CreateContext();
        var result = await _database.CreateDonationService(db)
            .UpdateAsync(id, TestDatabase.DonatorId, _database.ValidInput("Scaun nou"), null, removeImage: false);

        Assert.True(result.Succeeded);
        var stored = await db.DonationItems.AsNoTracking().SingleAsync(d => d.Id == id);
        Assert.Equal("Scaun nou", stored.Title);
        Assert.NotNull(stored.UpdatedAt);
    }

    [Fact]
    public async Task Donator_cannot_edit_someone_elses_donation()
    {
        var id = await _database.CreateDonationAsync(TestDatabase.DonatorId);

        await using var db = _database.CreateContext();
        var result = await _database.CreateDonationService(db)
            .UpdateAsync(id, TestDatabase.OtherDonatorId, _database.ValidInput("Modificat"), null, removeImage: false);

        Assert.False(result.Succeeded);
        Assert.Equal(ServiceError.Forbidden, result.Error);

        var stored = await db.DonationItems.AsNoTracking().SingleAsync(d => d.Id == id);
        Assert.Equal("Scaun de birou", stored.Title);
    }

    [Fact]
    public async Task Reserved_donation_cannot_be_edited()
    {
        var (donationId, _) = await _database.CreateReservedDonationAsync();

        await using var db = _database.CreateContext();
        var result = await _database.CreateDonationService(db)
            .UpdateAsync(donationId, TestDatabase.DonatorId, _database.ValidInput("Modificat"), null, removeImage: false);

        Assert.False(result.Succeeded);
        Assert.Equal(ServiceError.InvalidState, result.Error);
        Assert.Equal(UiText.Errors.EditNotAllowed, result.Message);
    }

    [Fact]
    public async Task Cancelled_donation_is_archived_and_hidden()
    {
        var id = await _database.CreateDonationAsync();

        await using (var db = _database.CreateContext())
        {
            var result = await _database.CreateDonationService(db).CancelAsync(id, TestDatabase.DonatorId);
            Assert.True(result.Succeeded);
        }

        await using (var db = _database.CreateContext())
        {
            var service = _database.CreateDonationService(db);
            var stored = await db.DonationItems.AsNoTracking().SingleAsync(d => d.Id == id);
            Assert.Equal(DonationStatus.Cancelled, stored.Status);
            Assert.NotNull(stored.CancelledAt);

            var feed = await service.SearchAvailableAsync(new DonationSearchQuery());
            Assert.DoesNotContain(feed.Items, i => i.Id == id);
            Assert.Null(await service.GetPublicDetailsAsync(id));

            var history = await service.GetHistoryAsync(TestDatabase.DonatorId, DonationStatus.Cancelled);
            Assert.Contains(history, h => h.Id == id);
        }
    }

    [Fact]
    public async Task Other_donator_cannot_cancel_donation()
    {
        var id = await _database.CreateDonationAsync();

        await using var db = _database.CreateContext();
        var result = await _database.CreateDonationService(db).CancelAsync(id, TestDatabase.OtherDonatorId);

        Assert.Equal(ServiceError.Forbidden, result.Error);
    }

    [Fact]
    public async Task Search_filters_and_paginates_in_database()
    {
        for (var i = 1; i <= 5; i++)
        {
            await _database.CreateDonationAsync(title: $"Carte {i}");
        }

        await _database.CreateDonationAsync(title: "Masă de bucătărie");

        await using var db = _database.CreateContext();
        var service = _database.CreateDonationService(db);

        var page1 = await service.SearchAvailableAsync(new DonationSearchQuery { Search = "Carte", PageSize = 2, Page = 1 });
        var page3 = await service.SearchAvailableAsync(new DonationSearchQuery { Search = "Carte", PageSize = 2, Page = 3 });

        Assert.Equal(5, page1.TotalCount);
        Assert.Equal(3, page1.TotalPages);
        Assert.Equal(2, page1.Items.Count);
        Assert.Single(page3.Items);
        Assert.Equal("Carte 5", page1.Items[0].Title); // newest first

        var oldest = await service.SearchAvailableAsync(new DonationSearchQuery { Search = "Carte", Sort = DonationSort.Oldest });
        Assert.Equal("Carte 1", oldest.Items[0].Title);

        var byCondition = await service.SearchAvailableAsync(new DonationSearchQuery { Condition = ProductCondition.New });
        Assert.Equal(0, byCondition.TotalCount);
    }

    [Fact]
    public async Task Dashboard_counts_donations_by_status()
    {
        await _database.CreateDonationAsync();
        await _database.CreateReservedDonationAsync();

        await using var db = _database.CreateContext();
        var dashboard = await _database.CreateDonationService(db).GetDashboardAsync(TestDatabase.DonatorId);

        Assert.Equal("Ioana", dashboard.FirstName);
        Assert.Equal(1, dashboard.AvailableCount);
        Assert.Equal(1, dashboard.ReservedCount);
        Assert.Equal(0, dashboard.CompletedCount);
        Assert.Equal(2, dashboard.ActiveItems.Count);
        Assert.Contains(dashboard.ActiveItems, i => i.ActiveReservationId.HasValue && i.ReceiverFirstName == "Andrei");
    }
}
