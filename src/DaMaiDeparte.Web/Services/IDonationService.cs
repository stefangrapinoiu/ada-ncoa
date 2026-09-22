using DaMaiDeparte.Web.Models;

namespace DaMaiDeparte.Web.Services;

public interface IDonationService
{
    /// <summary>Categories a donation may actually be published in (active + allowed).</summary>
    Task<IReadOnlyList<FoodCategory>> GetAllowedCategoriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// The location-based feed. Filtered, sorted and paged in SQL: city (mandatory), optional
    /// neighborhood, Available/Reserved only, and never anything past its expiry date.
    /// </summary>
    Task<PagedResult<DonationCard>> SearchFeedAsync(FeedQuery query, string currentUserId, CancellationToken cancellationToken = default);

    /// <summary>Full details including category, location, images and donor.</summary>
    Task<DonationItem?> GetDetailsAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Owner view including images and the active reservation. Null if not owned.</summary>
    Task<DonationItem?> GetOwnedAsync(int id, string userId, CancellationToken cancellationToken = default);

    Task<ServiceResult<int>> CreateAsync(
        string userId,
        DonationInput input,
        IReadOnlyList<IFormFile> images,
        CancellationToken cancellationToken = default);

    Task<ServiceResult> UpdateAsync(
        string userId,
        int id,
        DonationInput input,
        IReadOnlyList<IFormFile> newImages,
        IReadOnlyCollection<int> removeImageIds,
        CancellationToken cancellationToken = default);

    Task<ServiceResult> CancelAsync(int id, string userId, CancellationToken cancellationToken = default);

    Task<ServiceResult> CompleteAsync(int id, string userId, CancellationToken cancellationToken = default);

    Task<UserSummary> GetSummaryAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>When <paramref name="status"/> is null, returns active items (Available + Reserved).</summary>
    Task<IReadOnlyList<MyDonationItem>> GetMyDonationsAsync(string userId, DonationStatus? status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves every available listing whose expiry date has passed to
    /// <see cref="DonationStatus.Expired"/>. Returns the number of rows changed.
    /// </summary>
    Task<int> ExpireDueDonationsAsync(CancellationToken cancellationToken = default);
}
