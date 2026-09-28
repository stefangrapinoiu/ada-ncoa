using DaMaiDeparte.Web.Data;
using DaMaiDeparte.Web.Infrastructure;
using DaMaiDeparte.Web.Models;
using DaMaiDeparte.Web.Resources;
using Microsoft.EntityFrameworkCore;

namespace DaMaiDeparte.Web.Services;

public sealed class ReservationService : IReservationService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<ReservationService> _logger;

    public ReservationService(ApplicationDbContext db, ILogger<ReservationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ServiceResult<int>> ReserveAsync(int donationId, string userId, CancellationToken cancellationToken = default)
    {
        var strategy = _db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            _db.ChangeTracker.Clear();

            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

            // Always reload the current state from the database — never trust the browser.
            var donation = await _db.DonationItems.FirstOrDefaultAsync(d => d.Id == donationId, cancellationToken);
            if (donation is null || donation.Status == DonationStatus.Cancelled)
            {
                return ServiceResult<int>.Failure(ServiceError.NotFound, UiText.Errors.DonationNotFound);
            }

            // A user can donate and receive with the same account, so the only ownership rule
            // is this one. It is checked server-side, not just hidden in the UI.
            if (donation.DonatorId == userId)
            {
                return ServiceResult<int>.Failure(ServiceError.Forbidden, UiText.Errors.CannotReserveOwnDonation);
            }

            if (donation.Status == DonationStatus.Expired || donation.ExpirationDate < RoDate.Today)
            {
                return ServiceResult<int>.Failure(ServiceError.InvalidState, UiText.Errors.DonationExpired);
            }

            if (donation.Status != DonationStatus.Available)
            {
                return ServiceResult<int>.Failure(ServiceError.InvalidState, UiText.Errors.NotAvailableForReservation);
            }

            var reservation = new Reservation
            {
                DonationItemId = donation.Id,
                ReceiverId = userId,
                ReservedAt = DateTime.UtcNow,
                // The donor already stated where the handover happens when publishing, so the
                // receiver sees it immediately; only the exact time is still to be agreed.
                MeetingLocation = donation.PickupLocation,
                Notes = donation.PickupNotes
            };

            _db.Reservations.Add(reservation);
            donation.Status = DonationStatus.Reserved;
            donation.UpdatedAt = reservation.ReservedAt;

            try
            {
                // The concurrency token on DonationItem and the filtered unique index on
                // Reservations(DonationItemId) guarantee only one user can win.
                await _db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                // DbUpdateConcurrencyException derives from DbUpdateException.
                await transaction.RollbackAsync(CancellationToken.None);
                _db.ChangeTracker.Clear();
                _logger.LogWarning(ex, "Reservation conflict for donation {DonationId}", donationId);
                return ServiceResult<int>.Failure(ServiceError.Conflict, UiText.Errors.ReservationConflict);
            }

            _logger.LogInformation("Donation {DonationId} reserved (reservation {ReservationId})", donationId, reservation.Id);
            return ServiceResult<int>.Success(reservation.Id, UiText.Success.ReservationCreated);
        });
    }

    public async Task<ServiceResult> CancelAsync(int reservationId, string userId, CancellationToken cancellationToken = default)
    {
        var reservation = await _db.Reservations
            .Include(r => r.DonationItem)
            .FirstOrDefaultAsync(r => r.Id == reservationId, cancellationToken);

        // Do not reveal whether someone else's reservation exists.
        if (reservation is null || reservation.ReceiverId != userId)
        {
            return ServiceResult.Failure(ServiceError.NotFound, UiText.Errors.ReservationNotFound);
        }

        if (reservation.CancelledAt is not null || reservation.DonationItem.Status != DonationStatus.Reserved)
        {
            return ServiceResult.Failure(ServiceError.InvalidState, UiText.Errors.ReservationNotActive);
        }

        var now = DateTime.UtcNow;
        reservation.CancelledAt = now;

        // Food that expired while it was reserved must not go back into the feed as available.
        reservation.DonationItem.Status = reservation.DonationItem.ExpirationDate < RoDate.Today
            ? DonationStatus.Expired
            : DonationStatus.Available;

        if (reservation.DonationItem.Status == DonationStatus.Expired)
        {
            reservation.DonationItem.ExpiredAt = now;
        }

        reservation.DonationItem.UpdatedAt = now;

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // e.g. the donor completed the donation at the same moment.
            _logger.LogWarning("Concurrency conflict while cancelling reservation {ReservationId}", reservationId);
            return ServiceResult.Failure(ServiceError.Conflict, UiText.Errors.ReservationNotActive);
        }

        _logger.LogInformation("Reservation {ReservationId} cancelled", reservationId);
        return ServiceResult.Success(UiText.Success.ReservationCancelled);
    }

    public async Task<ServiceResult> ReleaseAsync(int reservationId, string donatorId, CancellationToken cancellationToken = default)
    {
        var reservation = await _db.Reservations
            .Include(r => r.DonationItem)
            .FirstOrDefaultAsync(r => r.Id == reservationId, cancellationToken);

        // Do not reveal whether someone else's reservation exists.
        if (reservation is null || reservation.DonationItem.DonatorId != donatorId)
        {
            return ServiceResult.Failure(ServiceError.NotFound, UiText.Errors.ReservationNotFound);
        }

        if (reservation.CancelledAt is not null || reservation.DonationItem.Status != DonationStatus.Reserved)
        {
            return ServiceResult.Failure(ServiceError.InvalidState, UiText.Errors.ReservationNotActive);
        }

        var now = DateTime.UtcNow;
        reservation.CancelledAt = now;

        // Food that expired while it was reserved must not go back into the feed as available.
        reservation.DonationItem.Status = reservation.DonationItem.ExpirationDate < RoDate.Today
            ? DonationStatus.Expired
            : DonationStatus.Available;

        if (reservation.DonationItem.Status == DonationStatus.Expired)
        {
            reservation.DonationItem.ExpiredAt = now;
        }

        reservation.DonationItem.UpdatedAt = now;

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // e.g. the receiver cancelled at the same moment.
            _logger.LogWarning("Concurrency conflict while releasing reservation {ReservationId}", reservationId);
            return ServiceResult.Failure(ServiceError.Conflict, UiText.Errors.ReservationNotActive);
        }

        _logger.LogInformation("Reservation {ReservationId} released by donor", reservationId);
        return ServiceResult.Success(UiText.Success.ReservationReleased);
    }

    public async Task<ServiceResult> SetPickupDetailsAsync(
        int reservationId,
        string donatorId,
        PickupDetailsInput input,
        CancellationToken cancellationToken = default)
    {
        var reservation = await _db.Reservations
            .Include(r => r.DonationItem)
            .FirstOrDefaultAsync(r => r.Id == reservationId, cancellationToken);

        if (reservation is null || reservation.DonationItem.DonatorId != donatorId)
        {
            return ServiceResult.Failure(ServiceError.NotFound, UiText.Errors.ReservationNotFound);
        }

        if (reservation.CancelledAt is not null || reservation.DonationItem.Status != DonationStatus.Reserved)
        {
            return ServiceResult.Failure(ServiceError.InvalidState, UiText.Errors.ReservationNotActive);
        }

        if (string.IsNullOrWhiteSpace(input.MeetingLocation))
        {
            return ServiceResult.Failure(ServiceError.Validation, UiText.Validation.MeetingLocationRequired);
        }

        if (input.MeetingAtUtc <= DateTime.UtcNow)
        {
            return ServiceResult.Failure(ServiceError.Validation, UiText.Validation.MeetingAtInPast);
        }

        reservation.MeetingLocation = input.MeetingLocation.Trim();
        reservation.MeetingAt = DateTime.SpecifyKind(input.MeetingAtUtc, DateTimeKind.Utc);
        reservation.Notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim();

        await _db.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success(UiText.Success.PickupSaved);
    }

    public Task<Reservation?> GetForReceiverAsync(int reservationId, string userId, CancellationToken cancellationToken = default) =>
        _db.Reservations
            .AsNoTracking()
            .Include(r => r.DonationItem).ThenInclude(d => d.FoodCategory)
            .Include(r => r.DonationItem).ThenInclude(d => d.City)
            .Include(r => r.DonationItem).ThenInclude(d => d.Neighborhood)
            .Include(r => r.DonationItem).ThenInclude(d => d.Donator)
            .Include(r => r.DonationItem).ThenInclude(d => d.Images)
            .FirstOrDefaultAsync(r => r.Id == reservationId && r.ReceiverId == userId, cancellationToken);

    public Task<Reservation?> GetForDonatorAsync(int reservationId, string donatorId, CancellationToken cancellationToken = default) =>
        _db.Reservations
            .AsNoTracking()
            .Include(r => r.DonationItem).ThenInclude(d => d.FoodCategory)
            .Include(r => r.DonationItem).ThenInclude(d => d.City)
            .Include(r => r.DonationItem).ThenInclude(d => d.Neighborhood)
            .Include(r => r.DonationItem).ThenInclude(d => d.Images)
            .Include(r => r.Receiver)
            .FirstOrDefaultAsync(r => r.Id == reservationId && r.DonationItem.DonatorId == donatorId, cancellationToken);

    public Task<int?> GetActiveReservationIdAsync(int donationId, string userId, CancellationToken cancellationToken = default) =>
        _db.Reservations
            .AsNoTracking()
            .Where(r => r.DonationItemId == donationId && r.ReceiverId == userId && r.CancelledAt == null)
            .Select(r => (int?)r.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<MyReservationItem>> GetActiveReservationsAsync(string userId, CancellationToken cancellationToken = default) =>
        await Project(_db.Reservations
                .AsNoTracking()
                .Where(r => r.ReceiverId == userId
                            && r.CancelledAt == null
                            && r.DonationItem.Status == DonationStatus.Reserved)
                .OrderByDescending(r => r.ReservedAt))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<MyReservationItem>> GetReceivedHistoryAsync(string userId, CancellationToken cancellationToken = default) =>
        await Project(_db.Reservations
                .AsNoTracking()
                .Where(r => r.ReceiverId == userId
                            && r.CancelledAt == null
                            && r.DonationItem.Status == DonationStatus.Completed)
                .OrderByDescending(r => r.DonationItem.CompletedAt))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ReservationMessageItem>> GetMessagesAsync(int reservationId, string viewerId, CancellationToken cancellationToken = default)
    {
        var isParticipant = await _db.Reservations
            .AsNoTracking()
            .AnyAsync(r => r.Id == reservationId && (r.ReceiverId == viewerId || r.DonationItem.DonatorId == viewerId), cancellationToken);

        if (!isParticipant)
        {
            return Array.Empty<ReservationMessageItem>();
        }

        return await _db.ReservationMessages
            .AsNoTracking()
            .Where(m => m.ReservationId == reservationId)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new ReservationMessageItem(m.Id, m.Sender.FirstName, m.SenderId == viewerId, m.Body, m.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<ServiceResult> SendMessageAsync(int reservationId, string senderId, string body, CancellationToken cancellationToken = default)
    {
        var trimmed = body.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return ServiceResult.Failure(ServiceError.Validation, UiText.Validation.MessageBodyRequired);
        }

        // Do not reveal whether someone else's reservation exists.
        var isParticipant = await _db.Reservations
            .AsNoTracking()
            .AnyAsync(r => r.Id == reservationId && (r.ReceiverId == senderId || r.DonationItem.DonatorId == senderId), cancellationToken);

        if (!isParticipant)
        {
            return ServiceResult.Failure(ServiceError.NotFound, UiText.Errors.ReservationNotFound);
        }

        _db.ReservationMessages.Add(new ReservationMessage
        {
            ReservationId = reservationId,
            SenderId = senderId,
            Body = trimmed,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success();
    }

    private static IQueryable<MyReservationItem> Project(IQueryable<Reservation> query) =>
        query.Select(r => new MyReservationItem(
            r.Id,
            r.DonationItemId,
            r.DonationItem.Title,
            r.DonationItem.FoodCategory.Name,
            r.DonationItem.ExpirationDate,
            r.DonationItem.Images.OrderBy(i => i.SortOrder).Select(i => i.Path).FirstOrDefault(),
            r.DonationItem.Donator.FirstName,
            r.DonationItem.City.Name,
            r.DonationItem.Neighborhood != null ? r.DonationItem.Neighborhood.Name : null,
            r.DonationItem.PickupLocation,
            r.DonationItem.PickupNotes,
            r.ReservedAt,
            r.MeetingLocation,
            r.MeetingAt,
            r.DonationItem.CompletedAt));
}
