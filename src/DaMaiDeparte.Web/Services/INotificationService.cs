namespace DaMaiDeparte.Web.Services;

/// <summary>
/// In-app notifications for new reservation messages. A message is unread for a participant
/// when the other participant sent it after that participant last opened the conversation.
/// Only active (not cancelled) reservations count. Everything is scoped to the viewer: nobody
/// can see or change another user's unread state.
/// </summary>
public interface INotificationService
{
    /// <summary>Total unread messages across all of the user's conversations (the bell badge).</summary>
    Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>One row per conversation with unread messages, newest first.</summary>
    Task<IReadOnlyList<UnreadConversation>> GetUnreadConversationsAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Unread count per reservation id, for the badges on "Donațiile mele" / "Rezervările mele".</summary>
    Task<IReadOnlyDictionary<int, int>> GetUnreadCountsByReservationAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks the conversation as read for the user, if they take part in it; silently does
    /// nothing otherwise (never reveals whether someone else's reservation exists).
    /// </summary>
    Task MarkReadAsync(int reservationId, string userId, CancellationToken cancellationToken = default);
}
