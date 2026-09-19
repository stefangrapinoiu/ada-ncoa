using DaMaiDeparte.Web.Models;
using DaMaiDeparte.Web.Resources;
using DaMaiDeparte.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace DaMaiDeparte.Tests;

public class ReservationServiceTests : IDisposable
{
    private readonly TestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Receiver_can_reserve_available_donation()
    {
        var donationId = await _database.CreateDonationAsync();

        await using var db = _database.CreateContext();
        var result = await _database.CreateReservationService(db).ReserveAsync(donationId, TestDatabase.ReceiverId);

        Assert.True(result.Succeeded);
        Assert.Equal(UiText.Success.ReservationCreated, result.Message);

        await using var check = _database.CreateContext();
        var donation = await check.DonationItems.AsNoTracking().SingleAsync(d => d.Id == donationId);
        var reservation = await check.Reservations.AsNoTracking().SingleAsync(r => r.Id == result.Value);
        Assert.Equal(DonationStatus.Reserved, donation.Status);
        Assert.Equal(TestDatabase.ReceiverId, reservation.ReceiverId);
        Assert.Null(reservation.CancelledAt);

        // Reserved items leave the public feed.
        var feed = await _database.CreateDonationService(check).SearchAvailableAsync(new DonationSearchQuery());
        Assert.DoesNotContain(feed.Items, i => i.Id == donationId);
    }

    [Fact]
    public async Task Reserved_donation_cannot_be_reserved_again()
    {
        var (donationId, _) = await _database.CreateReservedDonationAsync(TestDatabase.ReceiverId);

        await using var db = _database.CreateContext();
        var service = _database.CreateReservationService(db);

        var sameReceiver = await service.ReserveAsync(donationId, TestDatabase.ReceiverId);
        var otherReceiver = await service.ReserveAsync(donationId, TestDatabase.OtherReceiverId);

        Assert.False(sameReceiver.Succeeded);
        Assert.False(otherReceiver.Succeeded);
        Assert.Equal(UiText.Errors.NotAvailableForReservation, otherReceiver.Message);
        Assert.Equal(1, await db.Reservations.CountAsync(r => r.DonationItemId == donationId));
    }

    [Fact]
    public async Task Two_receivers_racing_cannot_both_reserve_via_concurrency_token()
    {
        var donationId = await _database.CreateDonationAsync();

        // Request B reads the donation while it is still available...
        await using var requestB = _database.CreateContext();
        var staleDonation = await requestB.DonationItems.SingleAsync(d => d.Id == donationId);
        Assert.Equal(DonationStatus.Available, staleDonation.Status);

        // ...request A reserves it first...
        await using (var requestA = _database.CreateContext())
        {
            var winner = await _database.CreateReservationService(requestA).ReserveAsync(donationId, TestDatabase.ReceiverId);
            Assert.True(winner.Succeeded);
        }

        // ...so request B's write, based on the stale state, must be rejected.
        requestB.Reservations.Add(new Reservation
        {
            DonationItemId = donationId,
            ReceiverId = TestDatabase.OtherReceiverId,
            ReservedAt = DateTime.UtcNow
        });
        staleDonation.Status = DonationStatus.Reserved;

        await Assert.ThrowsAnyAsync<DbUpdateException>(() => requestB.SaveChangesAsync());

        await using var check = _database.CreateContext();
        var active = await check.Reservations.Where(r => r.DonationItemId == donationId && r.CancelledAt == null).ToListAsync();
        Assert.Single(active);
        Assert.Equal(TestDatabase.ReceiverId, active[0].ReceiverId);
    }

    [Fact]
    public async Task Database_allows_only_one_active_reservation_per_donation()
    {
        var (donationId, _) = await _database.CreateReservedDonationAsync(TestDatabase.ReceiverId);

        await using var db = _database.CreateContext();
        db.Reservations.Add(new Reservation
        {
            DonationItemId = donationId,
            ReceiverId = TestDatabase.OtherReceiverId,
            ReservedAt = DateTime.UtcNow
        });

        // Filtered unique index IX_Reservations_DonationItemId_Active.
        await Assert.ThrowsAnyAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Donator_cannot_reserve()
    {
        var donationId = await _database.CreateDonationAsync(TestDatabase.DonatorId);

        await using var db = _database.CreateContext();
        var service = _database.CreateReservationService(db);

        var ownItem = await service.ReserveAsync(donationId, TestDatabase.DonatorId);
        var otherDonator = await service.ReserveAsync(donationId, TestDatabase.OtherDonatorId);

        Assert.Equal(ServiceError.Forbidden, ownItem.Error);
        Assert.Equal(ServiceError.Forbidden, otherDonator.Error);
        Assert.False(await db.Reservations.AnyAsync());
    }

    [Fact]
    public async Task Cancelled_or_missing_donation_cannot_be_reserved()
    {
        var donationId = await _database.CreateDonationAsync();
        await using (var db = _database.CreateContext())
        {
            await _database.CreateDonationService(db).CancelAsync(donationId, TestDatabase.DonatorId);
        }

        await using var check = _database.CreateContext();
        var service = _database.CreateReservationService(check);

        Assert.Equal(ServiceError.NotFound, (await service.ReserveAsync(donationId, TestDatabase.ReceiverId)).Error);
        Assert.Equal(ServiceError.NotFound, (await service.ReserveAsync(424242, TestDatabase.ReceiverId)).Error);
    }

    [Fact]
    public async Task Receiver_can_cancel_reservation_and_donation_becomes_available()
    {
        var (donationId, reservationId) = await _database.CreateReservedDonationAsync();

        await using (var db = _database.CreateContext())
        {
            var result = await _database.CreateReservationService(db).CancelAsync(reservationId, TestDatabase.ReceiverId);
            Assert.True(result.Succeeded);
            Assert.Equal(UiText.Success.ReservationCancelled, result.Message);
        }

        await using var check = _database.CreateContext();
        var donation = await check.DonationItems.AsNoTracking().SingleAsync(d => d.Id == donationId);
        var reservation = await check.Reservations.AsNoTracking().SingleAsync(r => r.Id == reservationId);

        Assert.Equal(DonationStatus.Available, donation.Status);
        Assert.NotNull(reservation.CancelledAt); // history is kept

        var feed = await _database.CreateDonationService(check).SearchAvailableAsync(new DonationSearchQuery());
        Assert.Contains(feed.Items, i => i.Id == donationId);

        // Someone else can now reserve it.
        var again = await _database.CreateReservationService(check).ReserveAsync(donationId, TestDatabase.OtherReceiverId);
        Assert.True(again.Succeeded);
        Assert.Equal(2, await check.Reservations.CountAsync(r => r.DonationItemId == donationId));
    }

    [Fact]
    public async Task Receiver_cannot_cancel_someone_elses_reservation()
    {
        var (donationId, reservationId) = await _database.CreateReservedDonationAsync(TestDatabase.ReceiverId);

        await using var db = _database.CreateContext();
        var result = await _database.CreateReservationService(db).CancelAsync(reservationId, TestDatabase.OtherReceiverId);

        Assert.Equal(ServiceError.NotFound, result.Error);
        var donation = await db.DonationItems.AsNoTracking().SingleAsync(d => d.Id == donationId);
        Assert.Equal(DonationStatus.Reserved, donation.Status);
    }

    [Fact]
    public async Task Private_reservation_details_are_visible_only_to_participants()
    {
        var (_, reservationId) = await _database.CreateReservedDonationAsync(TestDatabase.ReceiverId);

        await using var db = _database.CreateContext();
        var service = _database.CreateReservationService(db);

        Assert.NotNull(await service.GetForReceiverAsync(reservationId, TestDatabase.ReceiverId));
        Assert.NotNull(await service.GetForDonatorAsync(reservationId, TestDatabase.DonatorId));

        Assert.Null(await service.GetForReceiverAsync(reservationId, TestDatabase.OtherReceiverId));
        Assert.Null(await service.GetForDonatorAsync(reservationId, TestDatabase.OtherDonatorId));
        Assert.Null(await service.GetForReceiverAsync(reservationId, TestDatabase.DonatorId));
    }

    [Fact]
    public async Task Only_owning_donator_can_set_pickup_details()
    {
        var (_, reservationId) = await _database.CreateReservedDonationAsync();
        var input = new PickupDetailsInput("Intrarea principală Iulius Mall", DateTime.UtcNow.AddDays(1), "Te rog să mă suni când ajungi.");

        await using var db = _database.CreateContext();
        var service = _database.CreateReservationService(db);

        var denied = await service.SetPickupDetailsAsync(reservationId, TestDatabase.OtherDonatorId, input);
        Assert.Equal(ServiceError.NotFound, denied.Error);

        var pastDate = await service.SetPickupDetailsAsync(reservationId, TestDatabase.DonatorId, input with { MeetingAtUtc = DateTime.UtcNow.AddHours(-1) });
        Assert.Equal(ServiceError.Validation, pastDate.Error);

        var ok = await service.SetPickupDetailsAsync(reservationId, TestDatabase.DonatorId, input);
        Assert.True(ok.Succeeded);

        await using var check = _database.CreateContext();
        var receiverView = await _database.CreateReservationService(check).GetForReceiverAsync(reservationId, TestDatabase.ReceiverId);
        Assert.Equal("Intrarea principală Iulius Mall", receiverView!.MeetingLocation);
        Assert.NotNull(receiverView.MeetingAt);

        var active = await _database.CreateReservationService(check).GetActiveForReceiverAsync(TestDatabase.ReceiverId);
        Assert.Single(active);
    }
}
