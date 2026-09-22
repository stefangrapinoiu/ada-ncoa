using DaMaiDeparte.Web.Models;
using DaMaiDeparte.Web.Resources;
using DaMaiDeparte.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace DaMaiDeparte.Tests;

public class CompletionTests : IDisposable
{
    private readonly TestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Only_the_owning_donor_can_complete()
    {
        var (donationId, _) = await _database.CreateReservedDonationAsync();

        await using var db = _database.CreateContext();
        var service = _database.CreateDonationService(db);

        Assert.Equal(ServiceError.Forbidden, (await service.CompleteAsync(donationId, TestDatabase.OtherDonatorId)).Error);
        Assert.Equal(ServiceError.Forbidden, (await service.CompleteAsync(donationId, TestDatabase.ReceiverId)).Error);

        var donation = await db.DonationItems.AsNoTracking().SingleAsync(d => d.Id == donationId);
        Assert.Equal(DonationStatus.Reserved, donation.Status);
    }

    [Fact]
    public async Task Available_food_cannot_be_completed()
    {
        var donationId = await _database.CreateDonationAsync();

        await using var db = _database.CreateContext();
        var result = await _database.CreateDonationService(db).CompleteAsync(donationId, TestDatabase.DonatorId);

        Assert.Equal(ServiceError.InvalidState, result.Error);
        Assert.Equal(UiText.Errors.CompleteNotAllowed, result.Message);
    }

    [Fact]
    public async Task Completing_archives_the_donation_for_both_participants()
    {
        var (donationId, reservationId) = await _database.CreateReservedDonationAsync(TestDatabase.ReceiverId);

        await using (var db = _database.CreateContext())
        {
            var result = await _database.CreateDonationService(db).CompleteAsync(donationId, TestDatabase.DonatorId);
            Assert.True(result.Succeeded, result.Message);
            Assert.Equal(UiText.Success.DonationCompleted, result.Message);
        }

        await using var check = _database.CreateContext();
        var donations = _database.CreateDonationService(check);
        var reservations = _database.CreateReservationService(check);

        var donation = await check.DonationItems.AsNoTracking().SingleAsync(d => d.Id == donationId);
        Assert.Equal(DonationStatus.Completed, donation.Status);
        Assert.NotNull(donation.CompletedAt);

        // Completed food never appears in the public dashboard, under any filter.
        foreach (var filter in new[] { FeedFilter.All, FeedFilter.Available, FeedFilter.Reserved })
        {
            var feed = await donations.SearchFeedAsync(_database.Feed(filter: filter), TestDatabase.OtherReceiverId);
            Assert.DoesNotContain(feed.Items, i => i.Id == donationId);
        }

        // It stays in the donor's own list.
        var mine = await donations.GetMyDonationsAsync(TestDatabase.DonatorId, DonationStatus.Completed);
        var entry = Assert.Single(mine);
        Assert.Equal(donationId, entry.Id);

        // And in the receiver's history.
        var received = await reservations.GetReceivedHistoryAsync(TestDatabase.ReceiverId);
        var receivedEntry = Assert.Single(received);
        Assert.Equal(reservationId, receivedEntry.ReservationId);
        Assert.NotNull(receivedEntry.CompletedAt);

        // No longer active, and cannot be cancelled or completed again.
        Assert.Empty(await reservations.GetActiveReservationsAsync(TestDatabase.ReceiverId));
        Assert.Equal(ServiceError.InvalidState, (await reservations.CancelAsync(reservationId, TestDatabase.ReceiverId)).Error);
        Assert.Equal(ServiceError.InvalidState, (await donations.CompleteAsync(donationId, TestDatabase.DonatorId)).Error);

        // Other users do not see it in their history.
        Assert.Empty(await reservations.GetReceivedHistoryAsync(TestDatabase.OtherReceiverId));
    }

    [Fact]
    public async Task Completed_food_is_never_reservable_again()
    {
        var (donationId, _) = await _database.CreateReservedDonationAsync();

        await using var db = _database.CreateContext();
        await _database.CreateDonationService(db).CompleteAsync(donationId, TestDatabase.DonatorId);

        var result = await _database.CreateReservationService(db).ReserveAsync(donationId, TestDatabase.OtherReceiverId);

        Assert.False(result.Succeeded);
        Assert.Equal(ServiceError.InvalidState, result.Error);
    }

    [Fact]
    public async Task Summary_counts_donations_and_reservations()
    {
        await _database.CreateDonationAsync();
        await _database.CreateReservedDonationAsync();

        await using var db = _database.CreateContext();
        var donorSummary = await _database.CreateDonationService(db).GetSummaryAsync(TestDatabase.DonatorId);
        var receiverSummary = await _database.CreateDonationService(db).GetSummaryAsync(TestDatabase.ReceiverId);

        Assert.Equal("Ioana", donorSummary.FirstName);
        Assert.Equal(1, donorSummary.AvailableCount);
        Assert.Equal(1, donorSummary.ReservedCount);
        Assert.Equal(0, donorSummary.CompletedCount);
        Assert.Equal(0, donorSummary.ActiveReservationCount);

        // The same shape of summary works for the other side of the same transaction.
        Assert.Equal(1, receiverSummary.ActiveReservationCount);
        Assert.Equal(0, receiverSummary.AvailableCount);
    }
}
