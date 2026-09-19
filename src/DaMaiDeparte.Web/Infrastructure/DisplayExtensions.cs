using DaMaiDeparte.Web.Models;
using DaMaiDeparte.Web.Resources;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DaMaiDeparte.Web.Infrastructure;

public static class DisplayExtensions
{
    public static string ToLabel(this DonationStatus status) => status switch
    {
        DonationStatus.Available => UiText.Status.Available,
        DonationStatus.Reserved => UiText.Status.Reserved,
        DonationStatus.Completed => UiText.Status.Completed,
        DonationStatus.Cancelled => UiText.Status.Cancelled,
        _ => string.Empty
    };

    /// <summary>Feminine form, used when the label qualifies "Donație".</summary>
    public static string ToDonationLabel(this DonationStatus status) => status switch
    {
        DonationStatus.Available => UiText.Status.AvailableFeminine,
        DonationStatus.Reserved => UiText.Status.ReservedFeminine,
        DonationStatus.Completed => UiText.Status.CompletedFeminine,
        DonationStatus.Cancelled => UiText.Status.CancelledFeminine,
        _ => string.Empty
    };

    public static string ToBadgeClass(this DonationStatus status) => status switch
    {
        DonationStatus.Available => "text-bg-success",
        DonationStatus.Reserved => "text-bg-warning",
        DonationStatus.Completed => "text-bg-primary",
        DonationStatus.Cancelled => "text-bg-secondary",
        _ => "text-bg-light"
    };

    public static string ToLabel(this ProductCondition condition) => condition switch
    {
        ProductCondition.New => UiText.Condition.New,
        ProductCondition.LikeNew => UiText.Condition.LikeNew,
        ProductCondition.Good => UiText.Condition.Good,
        ProductCondition.Used => UiText.Condition.Used,
        ProductCondition.NeedsRepair => UiText.Condition.NeedsRepair,
        _ => string.Empty
    };

    public static IEnumerable<SelectListItem> ConditionOptions(ProductCondition? selected = null) =>
        // Option values use the enum name so tag helpers can match the bound value.
        Enum.GetValues<ProductCondition>().Select(c => new SelectListItem(c.ToLabel(), c.ToString(), selected == c));

    /// <summary>Returns a root-relative URL for a stored image path, or null.</summary>
    public static string? ToImageUrl(this string? imagePath) =>
        string.IsNullOrWhiteSpace(imagePath) ? null : "/" + imagePath.TrimStart('/');
}
