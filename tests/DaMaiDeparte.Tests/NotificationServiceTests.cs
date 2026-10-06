using DaMaiDeparte.Web.Data;
using DaMaiDeparte.Web.Models;
using DaMaiDeparte.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace DaMaiDeparte.Tests;

/// <summary>
/// Unread-message notifications: the bell count, the "Mesaje noi" list and the per-reservation
/// badges. Messages are inserted with explicit timestamps (minutes in the past or future) so
/// "before / after the last read" is deterministic regardless of clock resolution.
/// </summary>
public class NotificationServiceTests : IDisposable
{
    private readonly TestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    private async Task AddMessageAsync(int reservationId, string senderId, string body, DateTime createdAt)
    {
        await using var db = _database.CreateContext();
        db.ReservationMessages.Add(new ReservationMessage
        {
            ReservationId = reservationId,
            SenderId = senderId,
            Body = body,
            CreatedAt = createdAt
        });
        await db.SaveChangesAsync();
    }

    private NotificationService Service(ApplicationDbContext db) => new(db);

    [Fact]
    public async Task Messages_from_the_other_participant_count_as_unread_for_both_sides()
    {
        var (_, reservationId) = await _database.CreateReservedDonationAsync(TestDatabase.ReceiverId);
        var now = DateTime.UtcNow;
        await AddMessageAsync(reservationId, TestDatabase.ReceiverId, "Bună, când pot trece?", now.AddMinutes(-3));
        await AddMessageAsync(reservationId, TestDatabase.ReceiverId, "Pot și diseară.", now.AddMinutes(-2));
        await AddMessageAsync(reservationId, TestDatabase.DonatorId, "Diseară la 18 e perfect.", now.AddMinutes(-1));

        await using var db = _database.CreateContext();
        var service = Service(db);

        Assert.Equal(2, await service.GetUnreadCountAsync(TestDatabase.DonatorId));
        Assert.Equal(1, await service.GetUnreadCountAsync(TestDatabase.ReceiverId));
    }

    [Fact]
    public async Task Opening_the_conversation_clears_only_that_conversation_for_that_user()
    {
        var (_, first) = await _database.CreateReservedDonationAsync(TestDatabase.ReceiverId);
        var (_, second) = await _database.CreateReservedDonationAsync(TestDatabase.OtherReceiverId);
        var past = DateTime.UtcNow.AddMinutes(-5);
        await AddMessageAsync(first, TestDatabase.ReceiverId, "Mesaj 1", past);
        await AddMessageAsync(second, TestDatabase.OtherReceiverId, "Mesaj 2", past);

        await using (var db = _database.CreateContext())
        {
            await Service(db).MarkReadAsync(first, TestDatabase.DonatorId);
        }

        await using var check = _database.CreateContext();
        var service = Service(check);
        Assert.Equal(1, await service.GetUnreadCountAsync(TestDatabase.DonatorId));
        var conversations = await service.GetUnreadConversationsAsync(TestDatabase.DonatorId);
        Assert.Equal(second, Assert.Single(conversations).ReservationId);
    }

    [Fact]
    public async Task A_message_arriving_after_the_last_read_is_unread_again()
    {
        var (_, reservationId) = await _database.CreateReservedDonationAsync(TestDatabase.ReceiverId);
        await AddMessageAsync(reservationId, TestDatabase.DonatorId, "Vechi", DateTime.UtcNow.AddMinutes(-5));

        await using (var db = _database.CreateContext())
        {
            await Service(db).MarkReadAsync(reservationId, TestDatabase.ReceiverId);
        }

        await AddMessageAsync(reservationId, TestDatabase.DonatorId, "Nou", DateTime.UtcNow.AddMinutes(5));

        await using var check = _database.CreateContext();
        var conversation = Assert.Single(await Service(check).GetUnreadConversationsAsync(TestDatabase.ReceiverId));
        Assert.Equal(1, conversation.UnreadCount);
        Assert.Equal("Nou", conversation.LastMessageBody);
    }

    [Fact]
    public async Task Cancelled_reservations_do_not_count()
    {
        var (_, reservationId) = await _database.CreateReservedDonationAsync(TestDatabase.ReceiverId);
        await AddMessageAsync(reservationId, TestDatabase.ReceiverId, "Mesaj", DateTime.UtcNow.AddMinutes(-1));

        await using (var db = _database.CreateContext())
        {
            var cancel = await _database.CreateReservationService(db).CancelAsync(reservationId, TestDatabase.ReceiverId);
            Assert.True(cancel.Succeeded, cancel.Message);
        }

        await using var check = _database.CreateContext();
        Assert.Equal(0, await Service(check).GetUnreadCountAsync(TestDatabase.DonatorId));
    }

    [Fact]
    public async Task Outsiders_see_nothing_and_cannot_mark_someone_elses_conversation()
    {
        var (_, reservationId) = await _database.CreateReservedDonationAsync(TestDatabase.ReceiverId);
        await AddMessageAsync(reservationId, TestDatabase.ReceiverId, "Privat", DateTime.UtcNow.AddMinutes(-1));

        await using (var db = _database.CreateContext())
        {
            var service = Service(db);
            Assert.Equal(0, await service.GetUnreadCountAsync(TestDatabase.OtherDonatorId));
            Assert.Empty(await service.GetUnreadConversationsAsync(TestDatabase.OtherDonatorId));
            await service.MarkReadAsync(reservationId, TestDatabase.OtherDonatorId);
        }

        await using var check = _database.CreateContext();
        var reservation = await check.Reservations.AsNoTracking().SingleAsync(r => r.Id == reservationId);
        Assert.Null(reservation.DonorLastReadAt);
        Assert.Null(reservation.ReceiverLastReadAt);
        Assert.Equal(1, await Service(check).GetUnreadCountAsync(TestDatabase.DonatorId));
    }

    [Fact]
    public async Task Conversation_list_shows_the_latest_message_and_per_reservation_counts()
    {
        var (_, reservationId) = await _database.CreateReservedDonationAsync(TestDatabase.ReceiverId);
        var now = DateTime.UtcNow;
        await AddMessageAsync(reservationId, TestDatabase.ReceiverId, "Primul", now.AddMinutes(-3));
        await AddMessageAsync(reservationId, TestDatabase.ReceiverId, "Ultimul", now.AddMinutes(-1));

        await using var db = _database.CreateContext();
        var service = Service(db);

        var conversation = Assert.Single(await service.GetUnreadConversationsAsync(TestDatabase.DonatorId));
        Assert.Equal(reservationId, conversation.ReservationId);
        Assert.Equal(2, conversation.UnreadCount);
        Assert.Equal("Ultimul", conversation.LastMessageBody);
        Assert.Equal("Andrei", conversation.LastSenderFirstName);
        Assert.Equal("Pâine integrală feliată", conversation.DonationTitle);

        var counts = await service.GetUnreadCountsByReservationAsync(TestDatabase.DonatorId);
        Assert.Equal(2, counts[reservationId]);
    }
}
