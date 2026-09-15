using DaMaiDeparte.Web.Data;
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

    public async Task<ServiceResult<int>> ReserveAsync(int donationId, string receiverId, CancellationToken cancellationToken = default)
    {
        var isReceiver = await _db.Users
            .AnyAsync(u => u.Id == receiverId && u.AccountType == AccountType.Receiver, cancellationToken);
        if (!isReceiver)
        {
            return ServiceResult<int>.Failure(ServiceError.Forbidden, UiText.Errors.OnlyReceiversCanReserve);
        }

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

            if (donation.Status != DonationStatus.Available)
            {
                return ServiceResult<int>.Failure(ServiceError.InvalidState, UiText.Errors.NotAvailableForReservation);
            }

            if (donation.DonatorId == receiverId)
            {
                return ServiceResult<int>.Failure(ServiceError.Forbidden, UiText.Errors.OnlyReceiversCanReserve);
            }

            var reservation = new Reservation
            {
                DonationItemId = donation.Id,
                ReceiverId = receiverId,
                ReservedAt = DateTime.UtcNow
            };

            _db.Reservations.Add(reservation);
            donation.Status = DonationStatus.Reserved;
            donation.UpdatedAt = reservation.ReservedAt;

            try
            {
                // The concurrency token on DonationItem and the filtered unique index on
                // Reservations(DonationItemId) guarantee only one receiver can win.
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

    public async Task<ServiceResult> CancelAsync(int reservationId, string receiverId, CancellationToken cancellationToken = default)
    {
        var reservation = await _db.Reservations
            .Include(r => r.DonationItem)
            .FirstOrDefaultAsync(r => r.Id == reservationId, cancellationToken);

        // Do not reveal whether someone else's reservation exists.
        if (reservation is null || reservation.ReceiverId != receiverId)
        {
            return ServiceResult.Failure(ServiceError.NotFound, UiText.Errors.ReservationNotFound);
        }

        if (reservation.CancelledAt is not null || reservation.DonationItem.Status != DonationStatus.Reserved)
        {
            return ServiceResult.Failure(ServiceError.InvalidState, UiText.Errors.ReservationNotActive);
        }

        var now = DateTime.UtcNow;
        reservation.CancelledAt = now;
        reservation.DonationItem.Status = DonationStatus.Available;
        reservation.DonationItem.UpdatedAt = now;

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // e.g. the donator completed the donation at the same moment.
            _logger.LogWarning("Concurrency conflict while cancelling reservation {ReservationId}", reservationId);
            return ServiceResult.Failure(ServiceError.Conflict, UiText.Errors.ReservationNotActive);
        }

        _logger.LogInformation("Reservation {ReservationId} cancelled by receiver", reservationId);
        return ServiceResult.Success(UiText.Success.ReservationCancelled);
    }

    public async Task<ServiceResult> SetPickupDetailsAsync(int reservationId, string donatorId, PickupDetailsInput input, CancellationToken cancellationToken = default)
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

    public Task<Reservation?> GetForReceiverAsync(int reservationId, string receiverId, CancellationToken cancellationToken = default) =>
        _db.Reservations
            .AsNoTracking()
            .Include(r => r.DonationItem).ThenInclude(d => d.Category)
            .Include(r => r.DonationItem).ThenInclude(d => d.Donator)
            .FirstOrDefaultAsync(r => r.Id == reservationId && r.ReceiverId == receiverId, cancellationToken);

    public Task<Reservation?> GetForDonatorAsync(int reservationId, string donatorId, CancellationToken cancellationToken = default) =>
        _db.Reservations
            .AsNoTracking()
            .Include(r => r.DonationItem).ThenInclude(d => d.Category)
            .Include(r => r.Receiver)
            .FirstOrDefaultAsync(r => r.Id == reservationId && r.DonationItem.DonatorId == donatorId, cancellationToken);

    public Task<int?> GetActiveReservationIdAsync(int donationId, string receiverId, CancellationToken cancellationToken = default) =>
        _db.Reservations
            .AsNoTracking()
            .Where(r => r.DonationItemId == donationId && r.ReceiverId == receiverId && r.CancelledAt == null)
            .Select(r => (int?)r.Id)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<ReceiverReservationItem>> GetActiveForReceiverAsync(string receiverId, CancellationToken cancellationToken = default) =>
        await _db.Reservations
            .AsNoTracking()
            .Where(r => r.ReceiverId == receiverId
                        && r.CancelledAt == null
                        && r.DonationItem.Status == DonationStatus.Reserved)
            .OrderByDescending(r => r.ReservedAt)
            .Select(r => new ReceiverReservationItem(
                r.Id,
                r.DonationItemId,
                r.DonationItem.Title,
                r.DonationItem.Category.Name,
                r.DonationItem.ImagePath,
                r.DonationItem.Donator.FirstName,
                r.ReservedAt,
                r.MeetingLocation,
                r.MeetingAt,
                r.DonationItem.CompletedAt))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ReceiverReservationItem>> GetReceivedHistoryAsync(string receiverId, CancellationToken cancellationToken = default) =>
        await _db.Reservations
            .AsNoTracking()
            .Where(r => r.ReceiverId == receiverId
                        && r.CancelledAt == null
                        && r.DonationItem.Status == DonationStatus.Completed)
            .OrderByDescending(r => r.DonationItem.CompletedAt)
            .Select(r => new ReceiverReservationItem(
                r.Id,
                r.DonationItemId,
                r.DonationItem.Title,
                r.DonationItem.Category.Name,
                r.DonationItem.ImagePath,
                r.DonationItem.Donator.FirstName,
                r.ReservedAt,
                r.MeetingLocation,
                r.MeetingAt,
                r.DonationItem.CompletedAt))
            .ToListAsync(cancellationToken);
}
