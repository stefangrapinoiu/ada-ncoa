using DaMaiDeparte.Web.Models;

namespace DaMaiDeparte.Web.Services;

public interface IReservationService
{
    /// <summary>Reserves an available donation for a receiver. Returns the reservation id.</summary>
    Task<ServiceResult<int>> ReserveAsync(int donationId, string receiverId, CancellationToken cancellationToken = default);

    /// <summary>Receiver cancels their own active reservation; the donation becomes Available again.</summary>
    Task<ServiceResult> CancelAsync(int reservationId, string receiverId, CancellationToken cancellationToken = default);

    /// <summary>Donator sets pickup details on an active reservation of their own donation.</summary>
    Task<ServiceResult> SetPickupDetailsAsync(int reservationId, string donatorId, PickupDetailsInput input, CancellationToken cancellationToken = default);

    /// <summary>Reservation visible to its receiver only (includes donation, category and donator).</summary>
    Task<Reservation?> GetForReceiverAsync(int reservationId, string receiverId, CancellationToken cancellationToken = default);

    /// <summary>Reservation visible to the owning donator only (includes donation, category and receiver).</summary>
    Task<Reservation?> GetForDonatorAsync(int reservationId, string donatorId, CancellationToken cancellationToken = default);

    /// <summary>The receiver's active reservation for a donation, if any.</summary>
    Task<int?> GetActiveReservationIdAsync(int donationId, string receiverId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReceiverReservationItem>> GetActiveForReceiverAsync(string receiverId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReceiverReservationItem>> GetReceivedHistoryAsync(string receiverId, CancellationToken cancellationToken = default);
}
