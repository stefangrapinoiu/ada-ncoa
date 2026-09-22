using DaMaiDeparte.Web.Models;
using DaMaiDeparte.Web.Resources;

namespace DaMaiDeparte.Web.Infrastructure;

public static class DisplayExtensions
{
    public static string ToLabel(this DonationStatus status) => status switch
    {
        DonationStatus.Available => UiText.Status.Available,
        DonationStatus.Reserved => UiText.Status.Reserved,
        DonationStatus.Completed => UiText.Status.Completed,
        DonationStatus.Cancelled => UiText.Status.Cancelled,
        DonationStatus.Expired => UiText.Status.Expired,
        _ => string.Empty
    };

    /// <summary>Feminine form, used when the label qualifies "Donație".</summary>
    public static string ToDonationLabel(this DonationStatus status) => status switch
    {
        DonationStatus.Available => UiText.Status.AvailableFeminine,
        DonationStatus.Reserved => UiText.Status.ReservedFeminine,
        DonationStatus.Completed => UiText.Status.CompletedFeminine,
        DonationStatus.Cancelled => UiText.Status.CancelledFeminine,
        DonationStatus.Expired => UiText.Status.ExpiredFeminine,
        _ => string.Empty
    };

    public static string ToBadgeClass(this DonationStatus status) => status switch
    {
        DonationStatus.Available => "text-bg-success",
        DonationStatus.Reserved => "text-bg-warning",
        DonationStatus.Completed => "text-bg-primary",
        DonationStatus.Cancelled => "text-bg-secondary",
        DonationStatus.Expired => "text-bg-dark",
        _ => "text-bg-light"
    };

    /// <summary>Returns a root-relative URL for a stored image path, or null.</summary>
    public static string? ToImageUrl(this string? imagePath) =>
        string.IsNullOrWhiteSpace(imagePath) ? null : "/" + imagePath.TrimStart('/');

    /// <summary>"Cluj-Napoca" or "Cluj-Napoca · Mărăști".</summary>
    public static string ToLocationLabel(string cityName, string? neighborhoodName) =>
        string.IsNullOrWhiteSpace(neighborhoodName) ? cityName : $"{cityName} · {neighborhoodName}";

    /// <summary>
    /// Bootstrap class for the expiry badge: red when the food expires within 24 hours,
    /// amber within three days, neutral otherwise.
    /// </summary>
    public static string ToExpiryClass(this DateOnly expiration)
    {
        var days = expiration.DayNumber - RoDate.Today.DayNumber;
        return days switch
        {
            <= 1 => "text-bg-danger",
            <= FoodRules.MinimumShelfLifeDays => "text-bg-warning",
            _ => "text-bg-light border"
        };
    }
}
