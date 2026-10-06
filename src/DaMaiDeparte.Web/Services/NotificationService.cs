using DaMaiDeparte.Web.Data;
using DaMaiDeparte.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace DaMaiDeparte.Web.Services;

public sealed class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _db;

    public NotificationService(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// The single definition of "unread": sent by the other participant of an active reservation
    /// after the viewer's last read of that conversation (or ever, if they never opened it).
    /// The same account can't be both donor and receiver of one reservation, so exactly one of
    /// the two branches can apply per message.
    /// </summary>
    private IQueryable<ReservationMessage> Unread(string userId) =>
        _db.ReservationMessages
            .AsNoTracking()
            .Where(m => m.Reservation.CancelledAt == null
                        && m.SenderId != userId
                        && ((m.Reservation.ReceiverId == userId
                             && (m.Reservation.ReceiverLastReadAt == null || m.CreatedAt > m.Reservation.ReceiverLastReadAt))
                            || (m.Reservation.DonationItem.DonatorId == userId
                                && (m.Reservation.DonorLastReadAt == null || m.CreatedAt > m.Reservation.DonorLastReadAt))));

    public Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default) =>
        Unread(userId).CountAsync(cancellationToken);

    public async Task<IReadOnlyList<UnreadConversation>> GetUnreadConversationsAsync(string userId, CancellationToken cancellationToken = default)
    {
        // Unread volumes are tiny (a few pickup-coordination messages), so group in memory
        // rather than relying on provider-specific GroupBy translation.
        var rows = await Unread(userId)
            .Select(m => new
            {
                m.ReservationId,
                m.Reservation.DonationItem.Title,
                SenderFirstName = m.Sender.FirstName,
                m.Body,
                m.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(r => r.ReservationId)
            .Select(g =>
            {
                var last = g.OrderByDescending(r => r.CreatedAt).First();
                return new UnreadConversation(g.Key, last.Title, last.SenderFirstName, last.Body, last.CreatedAt, g.Count());
            })
            .OrderByDescending(c => c.LastMessageAt)
            .ToList();
    }

    public async Task<IReadOnlyDictionary<int, int>> GetUnreadCountsByReservationAsync(string userId, CancellationToken cancellationToken = default)
    {
        var ids = await Unread(userId).Select(m => m.ReservationId).ToListAsync(cancellationToken);
        return ids.GroupBy(id => id).ToDictionary(g => g.Key, g => g.Count());
    }

    public async Task MarkReadAsync(int reservationId, string userId, CancellationToken cancellationToken = default)
    {
        var participants = await _db.Reservations
            .AsNoTracking()
            .Where(r => r.Id == reservationId)
            .Select(r => new { r.ReceiverId, r.DonationItem.DonatorId })
            .FirstOrDefaultAsync(cancellationToken);

        if (participants is null)
        {
            return;
        }

        // Set-based update: touches only the one timestamp column, never the donation or its
        // concurrency token, and doesn't load or track any entity.
        var now = DateTime.UtcNow;
        var reservation = _db.Reservations.Where(r => r.Id == reservationId);
        if (participants.ReceiverId == userId)
        {
            await reservation.ExecuteUpdateAsync(s => s.SetProperty(r => r.ReceiverLastReadAt, (DateTime?)now), cancellationToken);
        }
        else if (participants.DonatorId == userId)
        {
            await reservation.ExecuteUpdateAsync(s => s.SetProperty(r => r.DonorLastReadAt, (DateTime?)now), cancellationToken);
        }
    }
}
