using DaMaiDeparte.Web.Data;
using DaMaiDeparte.Web.Models;
using DaMaiDeparte.Web.Resources;
using Microsoft.EntityFrameworkCore;

namespace DaMaiDeparte.Web.Services;

public sealed class DonationService : IDonationService
{
    private const int MaxPageSize = 48;

    private readonly ApplicationDbContext _db;
    private readonly IFileStorageService _files;
    private readonly ILogger<DonationService> _logger;

    public DonationService(ApplicationDbContext db, IFileStorageService files, ILogger<DonationService> logger)
    {
        _db = db;
        _files = files;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
        await _db.Categories
            .AsNoTracking()
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);

    public async Task<PagedResult<DonationCard>> SearchAvailableAsync(DonationSearchQuery query, CancellationToken cancellationToken = default)
    {
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);
        var page = Math.Max(1, query.Page);

        var donations = _db.DonationItems
            .AsNoTracking()
            .Where(d => d.Status == DonationStatus.Available);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            donations = donations.Where(d => d.Title.Contains(term) || d.Description.Contains(term));
        }

        if (query.CategoryId.HasValue)
        {
            donations = donations.Where(d => d.CategoryId == query.CategoryId.Value);
        }

        if (query.Condition.HasValue)
        {
            donations = donations.Where(d => d.Condition == query.Condition.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Area))
        {
            var area = query.Area.Trim();
            donations = donations.Where(d => d.PickupArea.Contains(area));
        }

        donations = query.Sort == DonationSort.Oldest
            ? donations.OrderBy(d => d.CreatedAt).ThenBy(d => d.Id)
            : donations.OrderByDescending(d => d.CreatedAt).ThenByDescending(d => d.Id);

        var total = await donations.CountAsync(cancellationToken);

        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        page = Math.Min(page, totalPages);

        var items = await donations
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new DonationCard(
                d.Id, d.Title, d.Category.Name, d.Condition, d.PickupArea, d.Status, d.ImagePath, d.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<DonationCard>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public Task<DonationItem?> GetPublicDetailsAsync(int id, CancellationToken cancellationToken = default) =>
        _db.DonationItems
            .AsNoTracking()
            .Include(d => d.Category)
            .Include(d => d.Donator)
            .FirstOrDefaultAsync(d => d.Id == id && d.Status != DonationStatus.Cancelled, cancellationToken);

    public Task<DonationItem?> GetOwnedAsync(int id, string donatorId, CancellationToken cancellationToken = default) =>
        _db.DonationItems
            .AsNoTracking()
            .Include(d => d.Category)
            .Include(d => d.Reservations.Where(r => r.CancelledAt == null))
                .ThenInclude(r => r.Receiver)
            .FirstOrDefaultAsync(d => d.Id == id && d.DonatorId == donatorId, cancellationToken);

    public async Task<ServiceResult<int>> CreateAsync(string donatorId, DonationInput input, IFormFile? image, CancellationToken cancellationToken = default)
    {
        var isDonator = await _db.Users
            .AnyAsync(u => u.Id == donatorId && u.AccountType == AccountType.Donator, cancellationToken);
        if (!isDonator)
        {
            return ServiceResult<int>.Failure(ServiceError.Forbidden, UiText.Errors.OnlyDonatorsCanDonate);
        }

        var validation = await ValidateInputAsync(input, image, cancellationToken);
        if (validation is not null)
        {
            return ServiceResult<int>.Failure(ServiceError.Validation, validation);
        }

        string? imagePath = null;
        if (image is not null)
        {
            try
            {
                imagePath = await _files.SaveImageAsync(image, cancellationToken);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _logger.LogError(ex, "Image upload failed while creating a donation");
                return ServiceResult<int>.Failure(ServiceError.Validation, UiText.Errors.ImageSaveFailed);
            }
        }

        var donation = new DonationItem
        {
            Title = input.Title.Trim(),
            Description = input.Description.Trim(),
            CategoryId = input.CategoryId,
            Condition = input.Condition,
            PickupArea = input.PickupArea.Trim(),
            Status = DonationStatus.Available,
            DonatorId = donatorId,
            ImagePath = imagePath,
            CreatedAt = DateTime.UtcNow
        };

        _db.DonationItems.Add(donation);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            _files.DeleteImage(imagePath);
            throw;
        }

        _logger.LogInformation("Donation {DonationId} created", donation.Id);
        return ServiceResult<int>.Success(donation.Id, UiText.Success.DonationPublished);
    }

    public async Task<ServiceResult> UpdateAsync(int id, string donatorId, DonationInput input, IFormFile? newImage, bool removeImage, CancellationToken cancellationToken = default)
    {
        var donation = await _db.DonationItems.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (donation is null)
        {
            return ServiceResult.Failure(ServiceError.NotFound, UiText.Errors.DonationNotFound);
        }

        if (donation.DonatorId != donatorId)
        {
            return ServiceResult.Failure(ServiceError.Forbidden, UiText.Errors.NotOwner);
        }

        if (donation.Status != DonationStatus.Available)
        {
            return ServiceResult.Failure(ServiceError.InvalidState, UiText.Errors.EditNotAllowed);
        }

        var validation = await ValidateInputAsync(input, newImage, cancellationToken);
        if (validation is not null)
        {
            return ServiceResult.Failure(ServiceError.Validation, validation);
        }

        var oldImage = donation.ImagePath;
        string? savedImage = null;

        if (newImage is not null)
        {
            try
            {
                savedImage = await _files.SaveImageAsync(newImage, cancellationToken);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _logger.LogError(ex, "Image upload failed while updating donation {DonationId}", id);
                return ServiceResult.Failure(ServiceError.Validation, UiText.Errors.ImageSaveFailed);
            }

            donation.ImagePath = savedImage;
        }
        else if (removeImage)
        {
            donation.ImagePath = null;
        }

        donation.Title = input.Title.Trim();
        donation.Description = input.Description.Trim();
        donation.CategoryId = input.CategoryId;
        donation.Condition = input.Condition;
        donation.PickupArea = input.PickupArea.Trim();
        donation.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Most likely reserved by someone while the donator was editing.
            _files.DeleteImage(savedImage);
            _logger.LogWarning("Concurrency conflict while updating donation {DonationId}", id);
            return ServiceResult.Failure(ServiceError.Conflict, UiText.Errors.EditNotAllowed);
        }

        if (oldImage != donation.ImagePath)
        {
            _files.DeleteImage(oldImage);
        }

        return ServiceResult.Success(UiText.Success.DonationUpdated);
    }

    public async Task<ServiceResult> CancelAsync(int id, string donatorId, CancellationToken cancellationToken = default)
    {
        var donation = await _db.DonationItems.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (donation is null)
        {
            return ServiceResult.Failure(ServiceError.NotFound, UiText.Errors.DonationNotFound);
        }

        if (donation.DonatorId != donatorId)
        {
            return ServiceResult.Failure(ServiceError.Forbidden, UiText.Errors.NotOwner);
        }

        if (donation.Status != DonationStatus.Available)
        {
            return ServiceResult.Failure(ServiceError.InvalidState, UiText.Errors.CancelNotAllowed);
        }

        donation.Status = DonationStatus.Cancelled;
        donation.CancelledAt = DateTime.UtcNow;
        donation.UpdatedAt = donation.CancelledAt;

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning("Concurrency conflict while cancelling donation {DonationId}", id);
            return ServiceResult.Failure(ServiceError.Conflict, UiText.Errors.CancelNotAllowed);
        }

        _logger.LogInformation("Donation {DonationId} cancelled", id);
        return ServiceResult.Success(UiText.Success.DonationCancelled);
    }

    public async Task<ServiceResult> CompleteAsync(int id, string donatorId, CancellationToken cancellationToken = default)
    {
        var donation = await _db.DonationItems
            .Include(d => d.Reservations.Where(r => r.CancelledAt == null))
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        if (donation is null)
        {
            return ServiceResult.Failure(ServiceError.NotFound, UiText.Errors.DonationNotFound);
        }

        if (donation.DonatorId != donatorId)
        {
            return ServiceResult.Failure(ServiceError.Forbidden, UiText.Errors.NotOwner);
        }

        if (donation.Status != DonationStatus.Reserved || donation.Reservations.Count == 0)
        {
            return ServiceResult.Failure(ServiceError.InvalidState, UiText.Errors.CompleteNotAllowed);
        }

        donation.Status = DonationStatus.Completed;
        donation.CompletedAt = DateTime.UtcNow;
        donation.UpdatedAt = donation.CompletedAt;

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // e.g. the receiver cancelled at the same moment.
            _logger.LogWarning("Concurrency conflict while completing donation {DonationId}", id);
            return ServiceResult.Failure(ServiceError.Conflict, UiText.Errors.ConcurrentUpdate);
        }

        _logger.LogInformation("Donation {DonationId} completed", id);
        return ServiceResult.Success(UiText.Success.DonationCompleted);
    }

    public async Task<DonatorDashboard> GetDashboardAsync(string donatorId, CancellationToken cancellationToken = default)
    {
        var firstName = await _db.Users
            .Where(u => u.Id == donatorId)
            .Select(u => u.FirstName)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

        var counts = await _db.DonationItems
            .Where(d => d.DonatorId == donatorId)
            .GroupBy(d => d.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        int CountOf(DonationStatus status) => counts.FirstOrDefault(c => c.Status == status)?.Count ?? 0;

        var active = await GetMyDonationsAsync(donatorId, null, cancellationToken);

        return new DonatorDashboard(
            firstName,
            CountOf(DonationStatus.Available),
            CountOf(DonationStatus.Reserved),
            CountOf(DonationStatus.Completed),
            active);
    }

    /// <summary>
    /// Donator's own items. When <paramref name="status"/> is null, returns active items (Available and Reserved).
    /// </summary>
    public async Task<IReadOnlyList<DonatorActiveItem>> GetMyDonationsAsync(string donatorId, DonationStatus? status, CancellationToken cancellationToken = default)
    {
        var query = _db.DonationItems.AsNoTracking().Where(d => d.DonatorId == donatorId);

        query = status.HasValue
            ? query.Where(d => d.Status == status.Value)
            : query.Where(d => d.Status == DonationStatus.Available || d.Status == DonationStatus.Reserved);

        return await query
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => new DonatorActiveItem(
                d.Id,
                d.Title,
                d.Category.Name,
                d.Status,
                d.ImagePath,
                d.CreatedAt,
                d.Reservations.Where(r => r.CancelledAt == null).Select(r => (int?)r.Id).FirstOrDefault(),
                d.Reservations.Where(r => r.CancelledAt == null).Select(r => r.Receiver.FirstName).FirstOrDefault()))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DonatorHistoryItem>> GetHistoryAsync(string donatorId, DonationStatus status, CancellationToken cancellationToken = default)
    {
        var query = _db.DonationItems
            .AsNoTracking()
            .Where(d => d.DonatorId == donatorId && d.Status == status);

        query = status == DonationStatus.Completed
            ? query.OrderByDescending(d => d.CompletedAt)
            : query.OrderByDescending(d => d.CancelledAt ?? d.UpdatedAt ?? d.CreatedAt);

        return await query
            .Select(d => new DonatorHistoryItem(
                d.Id,
                d.Title,
                d.Category.Name,
                d.Status,
                d.Reservations.Where(r => r.CancelledAt == null)
                    .Select(r => r.Receiver.FirstName + " " + r.Receiver.LastName).FirstOrDefault(),
                d.Reservations.Where(r => r.CancelledAt == null)
                    .Select(r => (DateTime?)r.ReservedAt).FirstOrDefault(),
                d.CompletedAt,
                d.CancelledAt,
                d.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    private async Task<string?> ValidateInputAsync(DonationInput input, IFormFile? image, CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(input.Condition))
        {
            return UiText.Validation.InvalidCondition;
        }

        var categoryExists = await _db.Categories.AnyAsync(c => c.Id == input.CategoryId, cancellationToken);
        if (!categoryExists)
        {
            return UiText.Validation.InvalidCategory;
        }

        if (image is not null)
        {
            var imageError = await _files.ValidateImageAsync(image, cancellationToken);
            if (imageError is not null)
            {
                _logger.LogInformation("Rejected image upload: {Reason}", imageError);
                return imageError;
            }
        }

        return null;
    }
}
