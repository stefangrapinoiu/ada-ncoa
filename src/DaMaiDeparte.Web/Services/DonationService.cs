using DaMaiDeparte.Web.Data;
using DaMaiDeparte.Web.Infrastructure;
using DaMaiDeparte.Web.Models;
using DaMaiDeparte.Web.Resources;
using Microsoft.EntityFrameworkCore;

namespace DaMaiDeparte.Web.Services;

public sealed class DonationService : IDonationService
{
    private const int MaxPageSize = 48;

    /// <summary>
    /// Lowercases and strips Romanian diacritics, so a search term matches regardless of case
    /// or diacritics. Must mirror the Replace chain applied to Title in SearchFeedAsync exactly
    /// — this one runs in C# against the typed search term, that one is EF-translated to SQL and
    /// runs against the Title column, and the two need to fold to the same plain-ASCII result.
    /// </summary>
    private static string FoldDiacritics(string value) =>
        value.ToLowerInvariant()
            .Replace("ă", "a").Replace("â", "a").Replace("î", "i")
            .Replace("ș", "s").Replace("ş", "s")
            .Replace("ț", "t").Replace("ţ", "t");

    /// <summary>Sanity bound: a best-before date more than five years out is almost certainly a typo.</summary>
    private const int MaxShelfLifeYears = 5;

    private readonly ApplicationDbContext _db;
    private readonly IFileStorageService _files;
    private readonly ILogger<DonationService> _logger;

    public DonationService(ApplicationDbContext db, IFileStorageService files, ILogger<DonationService> logger)
    {
        _db = db;
        _files = files;
        _logger = logger;
    }

    public async Task<IReadOnlyList<FoodCategory>> GetAllowedCategoriesAsync(CancellationToken cancellationToken = default) =>
        await _db.FoodCategories
            .AsNoTracking()
            .Where(c => c.IsActive && c.IsAllowed)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);

    public async Task<PagedResult<DonationCard>> SearchFeedAsync(
        FeedQuery query,
        string currentUserId,
        CancellationToken cancellationToken = default)
    {
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);
        var page = Math.Max(1, query.Page);
        var today = RoDate.Today;

        // Everything below is composed into a single SQL statement — no in-memory filtering.
        var donations = _db.DonationItems
            .AsNoTracking()
            .Where(d => d.CityId == query.CityId)
            .Where(d => d.ExpirationDate >= today);

        donations = query.Filter switch
        {
            FeedFilter.Available => donations.Where(d => d.Status == DonationStatus.Available),
            FeedFilter.Reserved => donations.Where(d => d.Status == DonationStatus.Reserved),
            // "Toate" never means "everything": completed, cancelled and expired stay hidden.
            _ => donations.Where(d => d.Status == DonationStatus.Available || d.Status == DonationStatus.Reserved)
        };

        if (query.NeighborhoodId.HasValue)
        {
            donations = donations.Where(d => d.NeighborhoodId == query.NeighborhoodId.Value);
        }

        if (query.FoodCategoryId.HasValue)
        {
            donations = donations.Where(d => d.FoodCategoryId == query.FoodCategoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = FoldDiacritics(query.Search.Trim());

            // Fold both sides down to plain lowercase ASCII (no ă/â/î/ș/ț) instead of trusting a
            // named SQL Server collation's accent-folding rules — this chain is EF-translatable
            // (ToLower -> LOWER, each Replace -> REPLACE) so it still runs as one SQL query, but
            // its behavior only depends on the character substitutions written out below, not on
            // collation internals. Must mirror FoldDiacritics() exactly.
            donations = donations.Where(d =>
                d.Title.ToLower()
                    .Replace("ă", "a").Replace("â", "a").Replace("î", "i")
                    .Replace("ș", "s").Replace("ş", "s")
                    .Replace("ț", "t").Replace("ţ", "t")
                    .Contains(term));
        }

        donations = donations
            .OrderByDescending(d => d.CreatedAt)
            .ThenByDescending(d => d.Id);

        var total = await donations.CountAsync(cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        page = Math.Min(page, totalPages);

        var items = await donations
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new DonationCard(
                d.Id,
                d.Title,
                d.FoodCategory.Name,
                d.ExpirationDate,
                d.City.Name,
                d.Neighborhood != null ? d.Neighborhood.Name : null,
                d.PickupLocation,
                d.Status,
                d.Images.OrderBy(i => i.SortOrder).Select(i => i.Path).FirstOrDefault(),
                d.Images.Count,
                d.CreatedAt,
                d.DonatorId == currentUserId))
            .ToListAsync(cancellationToken);

        return new PagedResult<DonationCard>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public Task<DonationItem?> GetDetailsAsync(int id, CancellationToken cancellationToken = default) =>
        _db.DonationItems
            .AsNoTracking()
            .Include(d => d.FoodCategory)
            .Include(d => d.City)
            .Include(d => d.Neighborhood)
            .Include(d => d.Donator)
            .Include(d => d.Images.OrderBy(i => i.SortOrder))
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public Task<DonationItem?> GetOwnedAsync(int id, string userId, CancellationToken cancellationToken = default) =>
        _db.DonationItems
            .AsNoTracking()
            .Include(d => d.FoodCategory)
            .Include(d => d.City)
            .Include(d => d.Neighborhood)
            .Include(d => d.Images.OrderBy(i => i.SortOrder))
            .Include(d => d.Reservations.Where(r => r.CancelledAt == null))
                .ThenInclude(r => r.Receiver)
            .FirstOrDefaultAsync(d => d.Id == id && d.DonatorId == userId, cancellationToken);

    public async Task<ServiceResult<int>> CreateAsync(
        string userId,
        DonationInput input,
        IReadOnlyList<IFormFile> images,
        CancellationToken cancellationToken = default)
    {
        var validation = await ValidateAsync(input, images, existingImageCount: 0, cancellationToken);
        if (validation is not null)
        {
            return ServiceResult<int>.Failure(ServiceError.Validation, validation);
        }

        var saved = new List<string>();
        try
        {
            foreach (var image in images)
            {
                saved.Add(await _files.SaveImageAsync(image, cancellationToken));
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogError(ex, "Image upload failed while creating a donation");
            DeleteAll(saved);
            return ServiceResult<int>.Failure(ServiceError.Validation, UiText.Errors.ImageSaveFailed);
        }

        var now = DateTime.UtcNow;
        var donation = new DonationItem
        {
            Title = input.Title.Trim(),
            FoodCategoryId = input.FoodCategoryId,
            ExpirationDate = input.ExpirationDate,
            CountryId = input.CountryId,
            CityId = input.CityId,
            NeighborhoodId = input.NeighborhoodId,
            PickupLocation = input.PickupLocation.Trim(),
            PickupNotes = Normalize(input.PickupNotes),
            Status = DonationStatus.Available,
            DonatorId = userId,
            SafetyConfirmedAt = now,
            CreatedAt = now
        };

        for (var i = 0; i < saved.Count; i++)
        {
            donation.Images.Add(new DonationImage { Path = saved[i], SortOrder = i });
        }

        _db.DonationItems.Add(donation);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            DeleteAll(saved);
            throw;
        }

        _logger.LogInformation("Donation {DonationId} created in city {CityId}", donation.Id, donation.CityId);
        return ServiceResult<int>.Success(donation.Id, UiText.Success.DonationPublished);
    }

    public async Task<ServiceResult> UpdateAsync(
        string userId,
        int id,
        DonationInput input,
        IReadOnlyList<IFormFile> newImages,
        IReadOnlyCollection<int> removeImageIds,
        CancellationToken cancellationToken = default)
    {
        var donation = await _db.DonationItems
            .Include(d => d.Images)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        if (donation is null)
        {
            return ServiceResult.Failure(ServiceError.NotFound, UiText.Errors.DonationNotFound);
        }

        if (donation.DonatorId != userId)
        {
            return ServiceResult.Failure(ServiceError.Forbidden, UiText.Errors.NotOwner);
        }

        if (donation.Status != DonationStatus.Available)
        {
            return ServiceResult.Failure(ServiceError.InvalidState, UiText.Errors.EditNotAllowed);
        }

        var kept = donation.Images.Where(i => !removeImageIds.Contains(i.Id)).ToList();

        var validation = await ValidateAsync(input, newImages, kept.Count, cancellationToken);
        if (validation is not null)
        {
            return ServiceResult.Failure(ServiceError.Validation, validation);
        }

        var saved = new List<string>();
        try
        {
            foreach (var image in newImages)
            {
                saved.Add(await _files.SaveImageAsync(image, cancellationToken));
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogError(ex, "Image upload failed while updating donation {DonationId}", id);
            DeleteAll(saved);
            return ServiceResult.Failure(ServiceError.Validation, UiText.Errors.ImageSaveFailed);
        }

        var removed = donation.Images.Where(i => removeImageIds.Contains(i.Id)).ToList();
        foreach (var image in removed)
        {
            donation.Images.Remove(image);
            _db.DonationImages.Remove(image);
        }

        foreach (var path in saved)
        {
            donation.Images.Add(new DonationImage { Path = path, DonationItemId = donation.Id });
        }

        var order = 0;
        foreach (var image in kept.Concat(donation.Images.Where(i => i.Id == 0)))
        {
            image.SortOrder = order++;
        }

        donation.Title = input.Title.Trim();
        donation.FoodCategoryId = input.FoodCategoryId;
        donation.ExpirationDate = input.ExpirationDate;
        donation.CountryId = input.CountryId;
        donation.CityId = input.CityId;
        donation.NeighborhoodId = input.NeighborhoodId;
        donation.PickupLocation = input.PickupLocation.Trim();
        donation.PickupNotes = Normalize(input.PickupNotes);
        donation.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Most likely reserved by someone while the donor was editing.
            DeleteAll(saved);
            _logger.LogWarning("Concurrency conflict while updating donation {DonationId}", id);
            return ServiceResult.Failure(ServiceError.Conflict, UiText.Errors.EditNotAllowed);
        }

        DeleteAll(removed.Select(i => i.Path));
        return ServiceResult.Success(UiText.Success.DonationUpdated);
    }

    public async Task<ServiceResult> CancelAsync(int id, string userId, CancellationToken cancellationToken = default)
    {
        var donation = await _db.DonationItems.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (donation is null)
        {
            return ServiceResult.Failure(ServiceError.NotFound, UiText.Errors.DonationNotFound);
        }

        if (donation.DonatorId != userId)
        {
            return ServiceResult.Failure(ServiceError.Forbidden, UiText.Errors.NotOwner);
        }

        if (donation.Status is not (DonationStatus.Available or DonationStatus.Expired))
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

    public async Task<ServiceResult> CompleteAsync(int id, string userId, CancellationToken cancellationToken = default)
    {
        var donation = await _db.DonationItems
            .Include(d => d.Reservations.Where(r => r.CancelledAt == null))
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        if (donation is null)
        {
            return ServiceResult.Failure(ServiceError.NotFound, UiText.Errors.DonationNotFound);
        }

        if (donation.DonatorId != userId)
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
            _logger.LogWarning("Concurrency conflict while completing donation {DonationId}", id);
            return ServiceResult.Failure(ServiceError.Conflict, UiText.Errors.ConcurrentUpdate);
        }

        _logger.LogInformation("Donation {DonationId} completed", id);
        return ServiceResult.Success(UiText.Success.DonationCompleted);
    }

    public async Task<UserSummary> GetSummaryAsync(string userId, CancellationToken cancellationToken = default)
    {
        var firstName = await _db.Users
            .Where(u => u.Id == userId)
            .Select(u => u.FirstName)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

        var counts = await _db.DonationItems
            .Where(d => d.DonatorId == userId)
            .GroupBy(d => d.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        int CountOf(DonationStatus status) => counts.FirstOrDefault(c => c.Status == status)?.Count ?? 0;

        var activeReservations = await _db.Reservations
            .CountAsync(
                r => r.ReceiverId == userId && r.CancelledAt == null && r.DonationItem.Status == DonationStatus.Reserved,
                cancellationToken);

        return new UserSummary(
            firstName,
            CountOf(DonationStatus.Available),
            CountOf(DonationStatus.Reserved),
            CountOf(DonationStatus.Completed),
            activeReservations);
    }

    public async Task<IReadOnlyList<MyDonationItem>> GetMyDonationsAsync(
        string userId,
        DonationStatus? status,
        CancellationToken cancellationToken = default)
    {
        var query = _db.DonationItems.AsNoTracking().Where(d => d.DonatorId == userId);

        query = status.HasValue
            ? query.Where(d => d.Status == status.Value)
            : query.Where(d => d.Status == DonationStatus.Available || d.Status == DonationStatus.Reserved);

        return await query
            .OrderByDescending(d => d.CreatedAt)
            .ThenByDescending(d => d.Id)
            .Select(d => new MyDonationItem(
                d.Id,
                d.Title,
                d.FoodCategory.Name,
                d.ExpirationDate,
                d.City.Name,
                d.Neighborhood != null ? d.Neighborhood.Name : null,
                d.PickupLocation,
                d.Status,
                d.Images.OrderBy(i => i.SortOrder).Select(i => i.Path).FirstOrDefault(),
                d.CreatedAt,
                d.Reservations.Where(r => r.CancelledAt == null).Select(r => (int?)r.Id).FirstOrDefault(),
                d.Reservations.Where(r => r.CancelledAt == null).Select(r => r.Receiver.FirstName).FirstOrDefault()))
            .ToListAsync(cancellationToken);
    }

    public async Task<int> ExpireDueDonationsAsync(CancellationToken cancellationToken = default)
    {
        var today = RoDate.Today;

        var due = await _db.DonationItems
            .Where(d => d.Status == DonationStatus.Available && d.ExpirationDate < today)
            .ToListAsync(cancellationToken);

        if (due.Count == 0)
        {
            return 0;
        }

        var now = DateTime.UtcNow;
        foreach (var donation in due)
        {
            // Expired listings are archived, never deleted.
            donation.Status = DonationStatus.Expired;
            donation.ExpiredAt = now;
            donation.UpdatedAt = now;
        }

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Marked {Count} donation(s) as expired", due.Count);
        return due.Count;
    }

    /// <summary>
    /// Server-side gate for every write. HTML validation is a convenience only: nothing here
    /// depends on the browser having run.
    /// </summary>
    private async Task<string?> ValidateAsync(
        DonationInput input,
        IReadOnlyList<IFormFile> images,
        int existingImageCount,
        CancellationToken cancellationToken)
    {
        // ---- Title ----
        var title = input.Title?.Trim() ?? string.Empty;
        if (title.Length < 3 || title.Length > FoodRules.MaxTitleLength)
        {
            return UiText.Validation.TitleLength;
        }

        // ---- Handover details, collected when the listing is created ----
        var pickupLocation = input.PickupLocation?.Trim() ?? string.Empty;
        if (pickupLocation.Length == 0)
        {
            return UiText.Validation.PickupLocationRequired;
        }

        if (pickupLocation.Length > FoodRules.MaxPickupLocationLength)
        {
            return UiText.Validation.PickupLocationLength;
        }

        var pickupNotes = Normalize(input.PickupNotes);
        if (pickupNotes is { Length: > FoodRules.MaxPickupNotesLength })
        {
            return UiText.Validation.PickupNotesLength;
        }

        // ---- Category allowlist (the primary food-safety control) ----
        var category = await _db.FoodCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == input.FoodCategoryId, cancellationToken);

        if (category is null || !category.IsActive)
        {
            return UiText.Validation.InvalidCategory;
        }

        if (!category.IsAllowed)
        {
            return UiText.Validation.CategoryNotAllowed;
        }

        // ---- Prohibited groups (secondary keyword guard) ----
        // The free-text description is gone, so the screen reads the title and the handover notes.
        var prohibited = ProhibitedFoodScreen.Detect(title, pickupNotes);
        if (prohibited == ProhibitedFoodGroup.Meat)
        {
            return UiText.Errors.ProhibitedMeat;
        }

        if (prohibited == ProhibitedFoodGroup.Dairy)
        {
            return UiText.Errors.ProhibitedDairy;
        }

        // ---- Expiry (local Romanian calendar date, never UTC) ----
        var today = RoDate.Today;
        if (input.ExpirationDate < today)
        {
            return UiText.Validation.Expired;
        }

        if (input.ExpirationDate < today.AddDays(FoodRules.MinimumShelfLifeDays))
        {
            return UiText.Validation.ExpiresTooSoon;
        }

        if (input.ExpirationDate > today.AddYears(MaxShelfLifeYears))
        {
            return UiText.Validation.ExpirationTooFar;
        }

        // ---- Location ----
        var city = await _db.Cities
            .AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.Id == input.CityId && c.CountryId == input.CountryId && c.IsActive,
                cancellationToken);

        if (city is null)
        {
            return UiText.Validation.InvalidLocation;
        }

        if (input.NeighborhoodId.HasValue)
        {
            var belongs = await _db.Neighborhoods.AnyAsync(
                n => n.Id == input.NeighborhoodId.Value && n.CityId == input.CityId && n.IsActive,
                cancellationToken);

            if (!belongs)
            {
                return UiText.Validation.NeighborhoodNotInCity;
            }
        }

        // ---- Photos ----
        var total = existingImageCount + images.Count;
        if (total < FoodRules.MinImages)
        {
            return UiText.Validation.ImagesRequired;
        }

        if (total > FoodRules.MaxImages)
        {
            return UiText.Validation.TooManyImages;
        }

        foreach (var image in images)
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

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private void DeleteAll(IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            _files.DeleteImage(path);
        }
    }
}
