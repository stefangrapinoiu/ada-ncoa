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
    public async Task Only_owning_donator_can_complete()
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
    public async Task Available_donation_cannot_be_completed()
    {
        var donationId = await _database.CreateDonationAsync();

        await using var db = _database.CreateContext();
        var result = await _database.CreateDonationService(db).CompleteAsync(donationId, TestDatabase.DonatorId);

        Assert.Equal(ServiceError.InvalidState, result.Error);
    }

    [Fact]
    public async Task Completing_archives_donation_for_both_participants()
    {
        var (donationId, reservationId) = await _database.CreateReservedDonationAsync(TestDatabase.ReceiverId);

        await using (var db = _database.CreateContext())
        {
            var result = await _database.CreateDonationService(db).CompleteAsync(donationId, TestDatabase.DonatorId);
            Assert.True(result.Succeeded);
            Assert.Equal(UiText.Success.DonationCompleted, result.Message);
        }

        await using var check = _database.CreateContext();
        var donations = _database.CreateDonationService(check);
        var reservations = _database.CreateReservationService(check);

        // Status and timestamp.
        var donation = await check.DonationItems.AsNoTracking().SingleAsync(d => d.Id == donationId);
        Assert.Equal(DonationStatus.Completed, donation.Status);
        Assert.NotNull(donation.CompletedAt);

        // Gone from the public feed.
        var feed = await donations.SearchAvailableAsync(new DonationSearchQuery());
        Assert.DoesNotContain(feed.Items, i => i.Id == donationId);

        // Donator history.
        var donatorHistory = await donations.GetHistoryAsync(TestDatabase.DonatorId, DonationStatus.Completed);
        var entry = Assert.Single(donatorHistory);
        Assert.Equal(donationId, entry.Id);
        Assert.Equal("Andrei Test", entry.ReceiverName);
        Assert.NotNull(entry.ReservedAt);

        // Receiver history.
        var received = await reservations.GetReceivedHistoryAsync(TestDatabase.ReceiverId);
        var receivedEntry = Assert.Single(received);
        Assert.Equal(reservationId, receivedEntry.ReservationId);
        Assert.NotNull(receivedEntry.CompletedAt);

        // No longer an active reservation, and cannot be cancelled or completed again.
        Assert.Empty(await reservations.GetActiveForReceiverAsync(TestDatabase.ReceiverId));
        Assert.Equal(ServiceError.InvalidState, (await reservations.CancelAsync(reservationId, TestDatabase.ReceiverId)).Error);
        Assert.Equal(ServiceError.InvalidState, (await donations.CompleteAsync(donationId, TestDatabase.DonatorId)).Error);

        // Other receivers do not see it in their history.
        Assert.Empty(await reservations.GetReceivedHistoryAsync(TestDatabase.OtherReceiverId));
    }

    [Fact]
    public async Task Completed_donation_is_never_reservable_again()
    {
        var (donationId, _) = await _database.CreateReservedDonationAsync();

        await using var db = _database.CreateContext();
        await _database.CreateDonationService(db).CompleteAsync(donationId, TestDatabase.DonatorId);

        var result = await _database.CreateReservationService(db).ReserveAsync(donationId, TestDatabase.OtherReceiverId);
        Assert.False(result.Succeeded);
        Assert.Equal(ServiceError.InvalidState, result.Error);
    }
}
