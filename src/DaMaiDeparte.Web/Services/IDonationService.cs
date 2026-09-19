using DaMaiDeparte.Web.Models;

namespace DaMaiDeparte.Web.Services;

public interface IDonationService
{
    Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken cancellationToken = default);

    /// <summary>Public feed: only Available donations, filtered and paged in SQL.</summary>
    Task<PagedResult<DonationCard>> SearchAvailableAsync(DonationSearchQuery query, CancellationToken cancellationToken = default);

    /// <summary>Public details (Category and Donator loaded). Cancelled donations are not returned.</summary>
    Task<DonationItem?> GetPublicDetailsAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Owner view including reservations and receivers. Returns null if not found or not owned.</summary>
    Task<DonationItem?> GetOwnedAsync(int id, string donatorId, CancellationToken cancellationToken = default);

    Task<ServiceResult<int>> CreateAsync(string donatorId, DonationInput input, IFormFile? image, CancellationToken cancellationToken = default);

    Task<ServiceResult> UpdateAsync(int id, string donatorId, DonationInput input, IFormFile? newImage, bool removeImage, CancellationToken cancellationToken = default);

    Task<ServiceResult> CancelAsync(int id, string donatorId, CancellationToken cancellationToken = default);

    Task<ServiceResult> CompleteAsync(int id, string donatorId, CancellationToken cancellationToken = default);

    Task<DonatorDashboard> GetDashboardAsync(string donatorId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DonatorActiveItem>> GetMyDonationsAsync(string donatorId, DonationStatus? status, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DonatorHistoryItem>> GetHistoryAsync(string donatorId, DonationStatus status, CancellationToken cancellationToken = default);
}
