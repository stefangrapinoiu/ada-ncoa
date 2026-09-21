namespace DaMaiDeparte.Web.Models;

/// <summary>
/// An approved food category. The platform uses an allowlist: a donation can only be
/// published in a category that is both <see cref="IsActive"/> and <see cref="IsAllowed"/>.
/// Users cannot invent their own categories.
/// </summary>
public class FoodCategory
{
    public int Id { get; set; }

    /// <summary>Stable internal identifier (English), e.g. "PackagedBakery".</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Display name shown in the UI (Romanian).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Short Romanian explanation of what belongs in this category.</summary>
    public string? Description { get; set; }

    /// <summary>Visible in the application at all.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// May be used for a new donation. Prohibited categories (meat, dairy) are seeded with
    /// <c>false</c> so the rule lives in the data and in the business layer, not only in the UI.
    /// </summary>
    public bool IsAllowed { get; set; } = true;

    public int SortOrder { get; set; }

    public ICollection<DonationItem> Donations { get; set; } = new List<DonationItem>();
}
