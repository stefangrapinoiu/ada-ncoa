using DaMaiDeparte.Web.Models;

namespace DaMaiDeparte.Web.Services;

public interface IReservationService
{
    /// <summary>
    /// Reserves an available, non-expired listing for a user. The same account can donate and
    /// reserve, so the only ownership rule is that nobody may reserve their own donation.
    /// </summary>
    Task<ServiceResult<int>> ReserveAsync(int donationId, string userId, CancellationToken cancellationToken = default);

    /// <summary>The user cancels their own active reservation; the food becomes Available again.</summary>
    Task<ServiceResult> CancelAsync(int reservationId, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The donor releases a reservation on their own listing — e.g. when the receiver stops
    /// responding. Same effect as CancelAsync (the food becomes Available again), but
    /// authorized against the donation's owner instead of the receiver.
    /// </summary>
    Task<ServiceResult> ReleaseAsync(int reservationId, string donatorId, CancellationToken cancellationToken = default);

    /// <summary>The donor sets pickup details on an active reservation of their own listing.</summary>
    Task<ServiceResult> SetPickupDetailsAsync(int reservationId, string donatorId, PickupDetailsInput input, CancellationToken cancellationToken = default);

    /// <summary>Reservation visible to the user who made it (includes donation, category and donor).</summary>
    Task<Reservation?> GetForReceiverAsync(int reservationId, string userId, CancellationToken cancellationToken = default);

    /// <summary>Reservation visible to the owning donor only (includes donation, category and receiver).</summary>
    Task<Reservation?> GetForDonatorAsync(int reservationId, string donatorId, CancellationToken cancellationToken = default);

    /// <summary>The user's active reservation for a donation, if any.</summary>
    Task<int?> GetActiveReservationIdAsync(int donationId, string userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MyReservationItem>> GetActiveReservationsAsync(string userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MyReservationItem>> GetReceivedHistoryAsync(string userId, CancellationToken cancellationToken = default);
}
