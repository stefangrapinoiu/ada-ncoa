namespace DaMaiDeparte.Web.Models;

public class Category
{
    public int Id { get; set; }

    /// <summary>Stable internal identifier (English), e.g. "Furniture".</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Display name shown in the UI (Romanian for the MVP).</summary>
    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public ICollection<DonationItem> Donations { get; set; } = new List<DonationItem>();
}
